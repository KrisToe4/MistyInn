using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Run;

namespace MistyInn.Core.Simulation;

/// <summary>
/// Agitation (section 9): soothing while served, rising otherwise (AGI-02, AGI-06), triggers (AGI-08), the state
/// thresholds and their behaviours (AGI-03), turning, and contagion (AGI-09). Every cause goes to the cycle log
/// (INV-12).
/// </summary>
internal sealed class AgitationSystem
{
    private readonly GameContent _content;
    private readonly RunState _run;
    private readonly IEventSink _sink;
    private readonly InnSense _sense;
    private readonly BestiaryRecorder _bestiary;
    private readonly CreatureBehaviourSystem _behaviour;
    private readonly InnDamage _damage;

    public AgitationSystem(
        GameContent content,
        RunState run,
        IEventSink sink,
        InnSense sense,
        BestiaryRecorder bestiary,
        CreatureBehaviourSystem behaviour,
        InnDamage damage)
    {
        _content = content;
        _run = run;
        _sink = sink;
        _sense = sense;
        _bestiary = bestiary;
        _behaviour = behaviour;
        _damage = damage;
    }

    private AgitationBalanceDef Tuning => _content.Balance.Agitation;

    public void Step(double stepSeconds, double now, IReadOnlyDictionary<CreatureId, (double Rate, string By)> rates)
    {
        var turnedNow = new Queue<Creature>();

        // A satisfied guest on its way out is beyond agitation (CRE-08).
        foreach (var creature in _run.Creatures.Where(c => !c.IsTurned && !c.IsLeaving))
        {
            var previousServer = creature.ServedBy;
            creature.ServedBy = rates.TryGetValue(creature.Id, out var served) ? served.By : null;
            if (creature.ServedBy is { } by && by != previousServer && by.StartsWith("fixture:", StringComparison.Ordinal))
            {
                _sink.Log("service.started", by.Replace("fixture:", "fixture.", StringComparison.Ordinal), creature.Id.ToRef());
            }

            // A creature in a back room is off the board: nothing outside reaches it (CMP-04).
            var holding = creature.IsInRoom ? new List<TriggerDef>() : Triggers(creature);
            foreach (var trigger in holding.Where(t => !creature.ActiveTriggers.Contains(t.Trigger)))
            {
                creature.Agitation += trigger.Spike;
                _sink.Log("trigger.fired", $"trigger.{trigger.Trigger.ToString().ToLowerInvariant()}", creature.Id.ToRef());
                _bestiary.TriggerSeen(creature.Def, trigger.Trigger);
            }

            creature.ActiveTriggers.Clear();
            creature.ActiveTriggers.UnionWith(holding.Select(t => t.Trigger));

            var frightened = !creature.IsInRoom && _run.CreaturesOnTheFloor.Any(o => o != creature && o.State == CreatureState.Menacing
                && o.Position.DistanceTo(creature.Position) <= Tuning.FrightenRadius);
            if (frightened && !creature.Frightened)
            {
                _sink.Log("agitation.frightened", "nearby.menacing", creature.Id.ToRef());
            }

            creature.Frightened = frightened;

            if (creature.ServedBy is not null)
            {
                creature.Agitation -= served.Rate * stepSeconds;
            }
            else
            {
                var rate = creature.Def.Agitation.RatePerSecond
                           * (creature.State >= CreatureState.Restless ? Tuning.RestlessRateMultiplier : 1)
                           * holding.Aggregate(1.0, (m, t) => m * t.RateMultiplier)
                           * WardMultiplier(creature)
                           * (frightened ? Tuning.FrightenRateMultiplier : 1);
                creature.Agitation += rate * stepSeconds;
            }

            UpdateState(creature, now, turnedNow);
        }

        Contagion(now, turnedNow);
        Damage(stepSeconds, now);
    }

    /// <summary>The creature's triggers whose condition holds now (AGI-08).</summary>
    private List<TriggerDef> Triggers(Creature creature)
    {
        var zone = _sense.ZoneOf(creature.Position);
        var floor = _run.CreaturesOnTheFloor;
        return creature.Def.Triggers.Where(t => t.Trigger switch
        {
            TriggerKind.Light or TriggerKind.Noise or TriggerKind.Heat or TriggerKind.Cold => _sense.TriggerReaches(t.Trigger, creature.Position),
            TriggerKind.Crowding => floor.Count(c => _sense.ZoneOf(c.Position) == zone) > (t.Count ?? int.MaxValue),
            TriggerKind.RivalPlane => floor.Any(c => c != creature && c.Def.PlaneId == t.Plane && _sense.ZoneOf(c.Position) == zone),
            TriggerKind.Violence => _sense.ViolenceNear(creature.Position, _content.Balance.Combat.ViolenceRadius),
            TriggerKind.Blood => _sense.BloodNear(creature.Position),
            _ => false,
        }).ToList();
    }

    /// <summary>Ward circles (FIX-02) and the auras of carried items such as the Ward Charm (ITM-06).</summary>
    private double WardMultiplier(Creature creature) =>
        _sense.Working(EffectKind.AgitationRate)
            .Where(w => InnSense.DistanceToTiles(creature.Position, w.Fixture.Tiles) <= (w.Effect.Radius ?? 0))
            .Aggregate(1.0, (m, w) => m * (w.Effect.Multiplier ?? 1))
        * _run.StaffOnTheFloor
            .Where(u => !u.IsFallen && u.Activity != UnitActivity.InRoom)
            .SelectMany(u => u.Effects.Where(e => e.Kind == EffectKind.Aura).Select(e => (Unit: u, Effect: e)))
            .Where(a => a.Unit.Position.DistanceTo(creature.Position) <= (a.Effect.Radius ?? 0))
            .Aggregate(1.0, (m, a) => m * (a.Effect.Multiplier ?? 1));

    private CreatureState StateFor(double agitation) =>
        agitation >= 100 ? CreatureState.Turned
        : agitation >= Tuning.MenacingAt ? CreatureState.Menacing
        : agitation >= Tuning.RestlessAt ? CreatureState.Restless
        : CreatureState.Calm;

    private void UpdateState(Creature creature, double now, Queue<Creature> turnedNow)
    {
        creature.Agitation = Math.Clamp(creature.Agitation, 0, 100);
        var state = StateFor(creature.Agitation);
        if (state == creature.State)
        {
            return;
        }

        var from = creature.State;
        creature.State = state;
        _sink.Raise(new ThresholdCrossed(_sink.Time, creature.Id, from, state));
        _sink.Log("agitation.state", $"state.{state.ToString().ToLowerInvariant()}", creature.Id.ToRef());

        if (state == CreatureState.Menacing)
        {
            creature.NextBreakAt = now + Tuning.MenacingBreakSeconds;
        }

        if (state == CreatureState.Turned)
        {
            creature.Agitation = 100;
            creature.ServedBy = null;
            _behaviour.Release(creature);
            creature.ClearPath();
            creature.Activity = CreatureActivity.Idle;
            creature.IdleUntil = now;
            _sink.Raise(new CreatureTurned(_sink.Time, creature.Id));
            _sink.Log("creature.turned", "agitation.max", creature.Id.ToRef());
            CombatSystem.BeginEscalation(creature, now, _sink);
            turnedNow.Enqueue(creature);
        }
    }

    /// <summary>A creature that turns spreads agitation to everyone close by (AGI-09), which can turn them too.</summary>
    private void Contagion(double now, Queue<Creature> turnedNow)
    {
        while (turnedNow.Count > 0)
        {
            var turned = turnedNow.Dequeue();
            foreach (var other in _run.CreaturesOnTheFloor.Where(c => !c.IsTurned && !c.IsLeaving && c.Position.DistanceTo(turned.Position) <= Tuning.ContagionRadius).ToList())
            {
                other.Agitation += Tuning.ContagionAgitation;
                _sink.Log("agitation.contagion", "creature.turned", other.Id.ToRef(), turned.Id.ToRef());

                if (other.Def.Triggers.FirstOrDefault(t => t.Trigger == TriggerKind.Violence) is { } violence)
                {
                    other.Agitation += violence.Spike;
                    _sink.Log("trigger.fired", "trigger.violence", other.Id.ToRef(), turned.Id.ToRef());
                    _bestiary.TriggerSeen(other.Def, TriggerKind.Violence);
                }

                UpdateState(other, now, turnedNow);
            }
        }
    }

    /// <summary>Restless creatures knock fixtures about; Menacing ones break one beside them every few seconds (AGI-03).</summary>
    private void Damage(double stepSeconds, double now)
    {
        foreach (var creature in _run.CreaturesOnTheFloor.Where(c => c.State is CreatureState.Restless or CreatureState.Menacing))
        {
            var here = creature.Position.NearestTile;
            var beside = _run.PlacedFixtures
                .Where(f => !f.IsBroken && f.Tiles.Any(t => Math.Max(Math.Abs(t.X - here.X), Math.Abs(t.Y - here.Y)) <= 1))
                .OrderBy(f => f.Id.Value)
                .FirstOrDefault();
            if (beside is null)
            {
                continue;
            }

            if (creature.State == CreatureState.Restless)
            {
                _damage.Fixture(beside, Tuning.RestlessDamagePerSecond * stepSeconds, creature, "agitation.restless");
            }
            else if (now >= creature.NextBreakAt)
            {
                creature.NextBreakAt = now + Tuning.MenacingBreakSeconds;
                _damage.Fixture(beside, beside.Health, creature, "agitation.menacing");
            }
        }
    }
}
