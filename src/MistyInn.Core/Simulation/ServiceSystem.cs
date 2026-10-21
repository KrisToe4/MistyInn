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

    /// <summary>
    /// One step of service (STF-04): every unit on the floor that is free keeps or starts a session; runners walk to
    /// the best creature they can serve and go home when there is none.
    /// </summary>
    public void Step(double stepSeconds, double now)
    {
        _rates.Clear();
        var inSession = new HashSet<CreatureId>();

        foreach (var unit in Servers().OrderBy(u => u.Id.Value).ToList())
        {
            if (unit.Session is { } session)
            {
                session.Targets.RemoveAll(id => _run.FindCreature(id) is not { } c || !CanServe(unit, session.Technique, c, logInterruption: true));
                if (now >= session.EndsAt || session.Targets.Count == 0)
                {
                    unit.Session = null;
                }
            }

            if (unit.Session is null)
            {
                if (unit.IsRunner)
                {
                    Run(unit, stepSeconds, now, inSession);
                }
                else
                {
                    unit.Session = StartSession(unit, now, inSession);
                    unit.Activity = unit.Session is null ? UnitActivity.AtPost : UnitActivity.Serving;
                }
            }
            else
            {
                unit.Activity = UnitActivity.Serving;
            }

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

    /// <summary>
    /// Units that may serve this step: staff and the innkeeper on the floor, at home, serving or walking to serve, and
    /// runners on their way home (who may pick up work on the way).
    /// </summary>
    private IEnumerable<Unit> Servers() =>
        _run.Units.Where(u => u.Tile is not null && !u.IsFallen
                              && (u.Activity is UnitActivity.AtPost or UnitActivity.Serving or UnitActivity.Walking
                                  || (u.Activity == UnitActivity.Returning && u.IsRunner)));

    /// <summary>
    /// A runner with no session (STF-04): it serves what is in reach if nothing better waits elsewhere, otherwise walks
    /// to the best creature no other runner is going to, or goes home.
    /// </summary>
    private void Run(Unit unit, double stepSeconds, double now, HashSet<CreatureId> inSession)
    {
        var claimed = _run.Units.Where(u => u != unit && u.ServeTarget is not null).Select(u => u.ServeTarget!.Value).ToHashSet();
        var from = unit.Position.NearestTile;
        var nearBest = Candidates(unit, inSession, claimed, from).FirstOrDefault();
        var target = Candidates(unit, inSession, claimed, from: null)
            .FirstOrDefault(c => c.Creature.Id == unit.ServeTarget || _run.Board.FindPath(from, c.Creature.Position.NearestTile) is not null);

        if (nearBest.Creature is not null && (target.Creature is null || nearBest.Value >= target.Value))
        {
            unit.Session = StartSession(unit, now, inSession);
            if (unit.Session is not null)
            {
                unit.ServeTarget = null;
                unit.ClearPath();
                unit.Activity = UnitActivity.Serving;
                return;
            }
        }

        if (target.Creature is null)
        {
            unit.ServeTarget = null;
            var home = TilePosition.Center(unit.Tile!.Value);
            if (unit.Position == home)
            {
                unit.Activity = UnitActivity.AtPost;
            }
            else if (unit.Activity != UnitActivity.Returning)
            {
                // The combat system walks returning units home (STF-04).
                unit.Activity = UnitActivity.Returning;
                unit.ClearPath();
            }

            return;
        }

        if (unit.ServeTarget != target.Creature.Id || unit.Path.Count == 0 || now >= unit.RepathAt)
        {
            unit.SetPath(_run.Board.FindPath(from, target.Creature.Position.NearestTile) ?? []);
            unit.RepathAt = now + _content.Balance.Combat.RepathSeconds;
        }

        unit.ServeTarget = target.Creature.Id;
        unit.Activity = UnitActivity.Walking;
        unit.Advance(unit.Stats.Speed * stepSeconds);
    }

    /// <summary>
    /// The technique and creature pairs this unit could serve, best first: value, then the nearest, then the lowest
    /// creature ID (STF-04). With <paramref name="from"/> only pairs in reach from that tile; without it, every pair
    /// the unit could serve once there.
    /// </summary>
    private IEnumerable<(TechniqueDef Technique, Creature Creature, double Value)> Candidates(
        Unit unit, HashSet<CreatureId> inSession, HashSet<CreatureId> claimed, Tile? from)
    {
        var list = new List<(TechniqueDef Technique, Creature Creature, double Value)>();
        foreach (var technique in TechniquesOf(unit))
        {
            if (technique.Supply is { } supply && _run.Supplies[supply] <= 0)
            {
                continue;
            }

            foreach (var creature in _run.Creatures.Where(c => !inSession.Contains(c.Id) && !claimed.Contains(c.Id)))
            {
                var ok = from is { } at
                    ? CanServe(unit, technique, creature, from: at)
                    : Eligible(unit, technique, creature) && Seated(technique, creature);
                if (ok)
                {
                    list.Add((technique, creature, Strength(creature.Def, technique) * SootheValue(unit, technique)));
                }
            }
        }

        return list.OrderByDescending(c => c.Value)
            .ThenBy(c => unit.Position.DistanceTo(c.Creature.Position))
            .ThenBy(c => c.Creature.Id.Value);
    }

    /// <summary>
    /// What a unit can give: its techniques that answer an appetite, and basic service (AGI-11), which everyone can
    /// give at a table; the Food technique is the same service done better.
    /// </summary>
    public IEnumerable<TechniqueDef> TechniquesOf(Unit unit) =>
        unit.Techniques.Where(t => t.Answers.Count > 0 && t.Technique != Technique.Food).Append(BasicService);

    /// <summary>The Food technique stands for basic service (AGI-11).</summary>
    private TechniqueDef BasicService => _content.Techniques.First(t => t.Technique == Technique.Food);

    /// <summary>A technique's soothe for this unit: basic service is ×0.3, or the Food technique's soothe if it knows Food.</summary>
    public double SootheValue(Unit unit, TechniqueDef technique) =>
        technique.Technique != Technique.Food ? technique.Soothe
        : unit.Techniques.Any(t => t.Technique == Technique.Food) ? technique.Soothe
        : _content.Balance.BasicServiceSoothe;

    /// <summary>
    /// The strength of the strongest appetite <paramref name="technique"/> answers (AGI-05), or 0. Basic service counts as
    /// craved by everyone (AGI-11).
    /// </summary>
    public double Strength(CreatureDef creature, TechniqueDef technique) =>
        technique.Technique == Technique.Food ? 1 : creature.Appetites
            .Where(a => technique.Answers.Contains(a.Appetite))
            .Select(a => StrengthOf(a) * (a.PreferredTechnique == technique.Id ? Tuning.PreferredBonus : 1))
            .DefaultIfEmpty(0)
            .Max();

    public double StrengthOf(AppetiteDef appetite) => appetite.Strength == AppetiteStrength.Craves ? 1 : Tuning.LikesStrength;

    /// <summary>
    /// Whether this unit could use this technique on this creature now, from <paramref name="from"/> (by default its
    /// home, or where a runner stands) (STF-04 step 2): Company only for sapient adults the companion does not refuse
    /// (CMP-02), and never with violence close by (CMP-05).
    /// </summary>
    public bool CanServe(Unit unit, TechniqueDef technique, Creature creature, bool logInterruption = false, Tile? from = null)
    {
        if (unit.Tile is not { } post || !Eligible(unit, technique, creature, logInterruption))
        {
            return false;
        }

        return Reaches(from ?? (unit.IsRunner ? unit.Position.NearestTile : post), technique, creature, unit);
    }

    /// <summary>Everything in <see cref="CanServe"/> except reach.</summary>
    private bool Eligible(Unit unit, TechniqueDef technique, Creature creature, bool logInterruption = false)
    {
        if (creature.IsTurned
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

        return true;
    }

    /// <summary>A technique that needs a fixture can only serve a creature seated at one that enables it (D-064).</summary>
    private bool Seated(TechniqueDef technique, Creature creature) =>
        !technique.NeedsFixture
        || (creature.Spot is { } spot && creature.Position.NearestTile == spot
            && _sense.Working(EffectKind.Enables).Any(w => w.Effect.Technique == technique.Id && w.Fixture.Seats.Contains(spot)));

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
        unit.Stats.Soothe * unit.RankMultiplier * SootheValue(unit, technique)
        * unit.SootheBonus(technique, creature.Def.PlaneId) * _ancestry.SootheBonus(unit, creature.Def)
        * Strength(creature.Def, technique) * Tuning.SootheScale
        * (technique.Technique == Technique.Company && unit.Def.PrefersOrEmpty.Contains(creature.Def.Id)
            ? _content.Balance.Company.PreferredSoothe
            : 1)
        * (unit.Shaken ? _content.Balance.Combat.ShakenSoothe : 1);

    private ServiceSession? StartSession(Unit unit, double now, HashSet<CreatureId> taken)
    {
        (TechniqueDef Technique, double Value, int CreatureId)? best = null;

        foreach (var technique in TechniquesOf(unit))
        {
            if (technique.Supply is { } supply && _run.Supplies[supply] <= 0)
            {
                continue;
            }

            foreach (var creature in _run.Creatures.Where(c => !taken.Contains(c.Id) && CanServe(unit, technique, c)))
            {
                var value = Strength(creature.Def, technique) * SootheValue(unit, technique);
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
