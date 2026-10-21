using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Run;

namespace MistyInn.Core.Simulation;

/// <summary>
/// Staff service (STF-04, AGI-06, AGI-07) and fixtures that give appetites (FIX-02). Each step it settles who is being
/// served and at what soothe rate; the agitation system applies the rates.
/// </summary>
internal sealed class ServiceSystem
{
    private readonly GameContent _content;
    private readonly RunState _run;
    private readonly IEventSink _sink;
    private readonly InnSense _sense;
    private readonly AncestryObserver _ancestry;
    private readonly Dictionary<CreatureId, (double Rate, string By)> _rates = new();

    public ServiceSystem(GameContent content, RunState run, IEventSink sink, InnSense sense, AncestryObserver ancestry)
    {
        _content = content;
        _run = run;
        _sink = sink;
        _sense = sense;
        _ancestry = ancestry;
    }

    /// <summary>The soothe rate (agitation points per second) each served creature gets this step, and from what.</summary>
    public IReadOnlyDictionary<CreatureId, (double Rate, string By)> Rates => _rates;

    private AgitationBalanceDef Tuning => _content.Balance.Agitation;

    public void Step(double now)
    {
        _rates.Clear();
        var inSession = new HashSet<CreatureId>();

        foreach (var unit in _run.AvailableStaff.OrderBy(u => u.Id.Value))
        {
            if (unit.Session is { } session)
            {
                session.Targets.RemoveAll(id => _run.FindCreature(id) is not { } c || !CanServe(unit, session.Technique, c, logInterruption: true));
                if (now >= session.EndsAt || session.Targets.Count == 0)
                {
                    unit.Session = null;
                }
            }

            unit.Session ??= StartSession(unit, now, inSession);
            unit.Activity = unit.Session is null ? UnitActivity.AtPost : UnitActivity.Serving;

            if (unit.Session is { } active)
            {
                foreach (var id in active.Targets)
                {
                    inSession.Add(id);
                    var c = _run.FindCreature(id)!;
                    Offer(c, SootheRate(unit, active.Technique, c), $"{unit.Def.Id}:{active.Technique.Id}");
                }
            }
        }

        FixtureService();
    }

    /// <summary>The strength of the strongest appetite <paramref name="technique"/> answers (AGI-05), or 0.</summary>
    public double Strength(CreatureDef creature, TechniqueDef technique) =>
        creature.Appetites
            .Where(a => technique.Answers.Contains(a.Appetite))
            .Select(a => StrengthOf(a) * (a.PreferredTechnique == technique.Id ? Tuning.PreferredBonus : 1))
            .DefaultIfEmpty(0)
            .Max();

    public double StrengthOf(AppetiteDef appetite) => appetite.Strength == AppetiteStrength.Craves ? 1 : Tuning.LikesStrength;

    /// <summary>
    /// Whether this staff member could use this technique on this creature now (STF-04 step 2): Company only for
    /// sapient adults the companion does not refuse (CMP-02), and never with violence close by (CMP-05).
    /// </summary>
    public bool CanServe(Unit unit, TechniqueDef technique, Creature creature, bool logInterruption = false)
    {
        if (creature.IsTurned || unit.Tile is not { } post
            || creature.Activity is not (CreatureActivity.Waiting or CreatureActivity.Idle)
            || Strength(creature.Def, technique) <= 0)
        {
            return false;
        }

        if (technique.Technique == Technique.Company)
        {
            if (!(creature.Def.Sapient && creature.Def.Adult) || Refuses(unit, creature.Def))
            {
                return false;
            }

            if (_sense.ViolenceNear(creature.Position, _content.Balance.Company.InterruptRadius))
            {
                if (logInterruption)
                {
                    _sink.Log("company.interrupted", "violence", creature.Id.ToRef(), unit.Id.ToRef());
                }

                return false;
            }
        }

        return Reaches(post, technique, creature, unit);
    }

    /// <summary>A companion's refusals are always honoured (CMP-02, RAT-07).</summary>
    public static bool Refuses(Unit unit, CreatureDef creature) => unit.Def.RefusesOrEmpty.Contains(creature.Id);

    /// <summary>
    /// Reach (D-064): a technique that needs a fixture serves creatures seated at a working fixture that enables it,
    /// within the technique's range of the staff member; from beside the stage, Song and Dance reach the stage's zone
    /// and the zones next to it, plus a zone for an adjacent Lantern (FIX-03) and the Entertainer synergy (SYN-02);
    /// otherwise the technique reaches its range (with a mastery's bonus, STF-17).
    /// </summary>
    public bool Reaches(Tile post, TechniqueDef technique, Creature creature, Unit? unit = null)
    {
        var postPosition = TilePosition.Center(post);
        var range = unit?.RangeOf(technique) ?? technique.Range;

        if (technique.NeedsFixture)
        {
            return creature.Spot is { } spot && creature.Position.NearestTile == spot
                && _sense.Working(EffectKind.Enables).Any(w =>
                    w.Effect.Technique == technique.Id
                    && w.Fixture.Seats.Contains(spot)
                    && InnSense.DistanceToTiles(postPosition, w.Fixture.Tiles) <= range + 0.5);
        }

        foreach (var (fixture, effect) in _sense.Working(EffectKind.Enables))
        {
            if (effect.Technique == technique.Id && effect.Zones is > 0
                && fixture.Tiles.Any(t => Math.Max(Math.Abs(t.X - post.X), Math.Abs(t.Y - post.Y)) <= 1))
            {
                var levels = effect.Zones.Value + _sense.AdjacentAmount(fixture, EffectKind.Radius) + (unit?.ExtraZones(technique) ?? 0);
                var zones = _sense.ZonesAround(_content.Layout.ZoneAt(fixture.Tiles[0]), levels);
                if (zones.Contains(_sense.ZoneOf(creature.Position)))
                {
                    return true;
                }
            }
        }

        return postPosition.DistanceTo(creature.Position) <= range;
    }

    /// <summary>
    /// Agitation points per second this staff member soothes with this technique (AGI-06): the rank (STF-06), mastery,
    /// items and synergies (STF-17, ITM-06, SYN-02), a revealed ancestry (STF-16), ×1.5 for a companion's preferred
    /// guest (CMP-02) and ×0.8 while Shaken (STF-12).
    /// </summary>
    public double SootheRate(Unit unit, TechniqueDef technique, Creature creature) =>
        unit.Stats.Soothe * unit.RankMultiplier * technique.Soothe
        * unit.SootheBonus(technique, creature.Def.PlaneId) * _ancestry.SootheBonus(unit, creature.Def)
        * Strength(creature.Def, technique) * Tuning.SootheScale
        * (technique.Technique == Technique.Company && unit.Def.PrefersOrEmpty.Contains(creature.Def.Id)
            ? _content.Balance.Company.PreferredSoothe
            : 1)
        * (unit.Shaken ? _content.Balance.Combat.ShakenSoothe : 1);

    private ServiceSession? StartSession(Unit unit, double now, HashSet<CreatureId> taken)
    {
        (TechniqueDef Technique, double Value, int CreatureId)? best = null;

        foreach (var technique in unit.Techniques.Where(t => t.Answers.Count > 0))
        {
            if (technique.Supply is { } supply && _run.Supplies[supply] <= 0)
            {
                continue;
            }

            foreach (var creature in _run.Creatures.Where(c => !taken.Contains(c.Id) && CanServe(unit, technique, c)))
            {
                var value = Strength(creature.Def, technique) * technique.Soothe;
                if (best is null || value > best.Value.Value || (value == best.Value.Value && creature.Id.Value < best.Value.CreatureId))
                {
                    best = (technique, value, creature.Id.Value);
                }
            }
        }

        if (best is not { } choice)
        {
            return null;
        }

        var t = choice.Technique;
        var limit = unit.CapacityOf(t);
        if (t.Supply is { } s)
        {
            limit = Math.Min(limit, _run.Supplies[s]);
        }

        var targets = _run.Creatures
            .Where(c => !taken.Contains(c.Id) && CanServe(unit, t, c))
            .OrderByDescending(c => Strength(c.Def, t))
            .ThenBy(c => c.Id.Value)
            .Take(limit)
            .Select(c => c.Id)
            .ToList();

        if (t.Supply is { } used)
        {
            _run.Supplies[used] -= targets.Count;
            _sink.Raise(new SuppliesChanged(_sink.Time, used, _run.Supplies[used]));
        }

        _sink.Raise(new ServiceStarted(_sink.Time, unit.Id, t.Id, targets));
        foreach (var id in targets)
        {
            var served = _run.FindCreature(id)!;
            served.WasServed = true;
            _sink.Log("service.started", $"technique.{t.Id}", id.ToRef(), unit.Id.ToRef());
            _ancestry.Observe(unit, served.Def);
        }

        return new ServiceSession(t, targets, now + _content.Balance.ServiceSessionSeconds);
    }

    /// <summary>Fixtures that give an appetite soothe the creatures in their seats, unless cancelled (FIX-02).</summary>
    private void FixtureService()
    {
        foreach (var (fixture, effect) in _sense.Working(EffectKind.Provides))
        {
            if (effect.CancelledBy is { } cancel && _sense.TriggerReaches(cancel, fixture.Tiles, effect.Radius ?? 0))
            {
                continue;
            }

            foreach (var creature in _run.CreaturesOnTheFloor.Where(c => !c.IsTurned && fixture.Seats.Contains(c.Position.NearestTile)
                         && c.Position == TilePosition.Center(c.Position.NearestTile)))
            {
                var appetite = creature.Def.Appetites.FirstOrDefault(a => a.Appetite == effect.Appetite);
                if (appetite is null)
                {
                    continue;
                }

                creature.WasServed = true;
                Offer(creature, _content.Balance.FixtureSoothe * StrengthOf(appetite) * Tuning.SootheScale, $"fixture:{fixture.Def.Id}");
            }
        }
    }

    /// <summary>Offers a soothe rate; the strongest offer of the step wins (AGI-06).</summary>
    public void Offer(Creature creature, double rate, string by)
    {
        if (!_rates.TryGetValue(creature.Id, out var current) || rate > current.Rate)
        {
            _rates[creature.Id] = (rate, by);
        }
    }
}
