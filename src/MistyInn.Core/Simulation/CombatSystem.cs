using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Run;

namespace MistyInn.Core.Simulation;

/// <summary>
/// Escalation and combat (section 10): turned creatures go through Wreck, Prey and Hunt (ESC-01, ESC-02) and turn on
/// the fighter that hits them (ESC-03); fighters engage turned creatures in order of threat (ESC-07); the innkeeper
/// strikes back at what reaches it (ESC-06). Hits do <c>max(1, attack − armour)</c> (ESC-05); 0 health banishes a
/// turned creature (ESC-08) or fells anyone else (ESC-04, ESC-09). Every stage, attack, fall and banishing is logged
/// (INV-12).
/// </summary>
internal sealed class CombatSystem
{
    private static readonly EscalationStage[] Stages = [EscalationStage.Wreck, EscalationStage.Prey, EscalationStage.Hunt];

    private readonly GameContent _content;
    private readonly RunState _run;
    private readonly IEventSink _sink;
    private readonly InnDamage _damage;
    private readonly BestiaryRecorder _bestiary;
    private readonly CreatureBehaviourSystem _behaviour;
    private readonly CompanySystem _company;
    private readonly AncestryObserver _ancestry;
    private readonly IReadOnlyList<Tile> _walls;
    private readonly Dictionary<CreatureId, Target> _targets = new();

    public CombatSystem(
        GameContent content,
        RunState run,
        IEventSink sink,
        InnDamage damage,
        BestiaryRecorder bestiary,
        CreatureBehaviourSystem behaviour,
        CompanySystem company,
        AncestryObserver ancestry)
    {
        _ancestry = ancestry;
        _content = content;
        _run = run;
        _sink = sink;
        _damage = damage;
        _bestiary = bestiary;
        _behaviour = behaviour;
        _company = company;

        var layout = content.Layout;
        _walls = layout.AllTiles()
            .Where(t => layout.KindAt(t) == TileKind.Wall && Neighbours4(t).Any(n => layout.KindAt(n) == TileKind.Floor))
            .ToList();
    }

    private CombatBalanceDef Tuning => _content.Balance.Combat;

    private bool Over => _run.Phase != Phase.Mist;

    /// <summary>Starts a newly turned creature on the first stage its profile does not skip (ESC-01, ESC-02).</summary>
    public static void BeginEscalation(Creature creature, double now, IEventSink sink)
    {
        creature.Stage = NextStage(creature.Def.Escalation, after: null);
        StartStage(creature, now, sink);
    }

    public void Step(double stepSeconds, double now)
    {
        _run.Attacks.RemoveAll(a => now - a.Time > Tuning.ViolenceSeconds);
        ReleaseGrapples();

        foreach (var creature in _run.Creatures.Where(c => c.IsTurned && !c.IsInRoom).OrderBy(c => c.Id.Value).ToList())
        {
            if (Over)
            {
                return;
            }

            if (_run.FindCreature(creature.Id) is null)
            {
                continue;
            }

            Escalate(creature, now);
            Act(creature, stepSeconds, now);
        }

        foreach (var unit in _run.Units.Where(u => u.Tile is not null && !u.IsFallen).OrderBy(u => u.Id.Value).ToList())
        {
            if (Over)
            {
                return;
            }

            if (unit.Def.IsInnkeeper)
            {
                Defend(unit, now);
            }
            else if (unit.FighterTechnique is { } technique
                     && unit.Activity is UnitActivity.AtPost or UnitActivity.Serving or UnitActivity.Fighting or UnitActivity.Returning)
            {
                Fight(unit, technique, stepSeconds, now);
            }
            else if (unit.Activity == UnitActivity.Returning)
            {
                Return(unit, stepSeconds);
            }
        }
    }

    /// <summary>Clears what is left over at dawn.</summary>
    public void Reset() => _targets.Clear();

    private static EscalationStage NextStage(EscalationDef profile, EscalationStage? after)
    {
        foreach (var stage in Stages)
        {
            if ((after is null || stage > after) && !(profile.Skip ?? []).Contains(stage))
            {
                return stage;
            }
        }

        return EscalationStage.Hunt;
    }

    private static void StartStage(Creature creature, double now, IEventSink sink)
    {
        creature.StageSince = now;
        creature.BrokeSomething = false;
        creature.MadeSomeoneFall = false;
        sink.Raise(new EscalationStageChanged(sink.Time, creature.Id, creature.Stage));
        sink.Log("escalation.stage", $"stage.{creature.Stage.ToString().ToLowerInvariant()}", creature.Id.ToRef());
    }

    /// <summary>Wreck ends after one broken fixture or its time; Prey after one fall or its time (ESC-01).</summary>
    private void Escalate(Creature creature, double now)
    {
        var profile = creature.Def.Escalation;
        var done = creature.Stage switch
        {
            EscalationStage.Wreck => creature.BrokeSomething || now - creature.StageSince >= profile.WreckSeconds,
            EscalationStage.Prey => creature.MadeSomeoneFall || now - creature.StageSince >= profile.PreySeconds,
            _ => false,
        };

        if (done)
        {
            creature.Stage = NextStage(profile, creature.Stage);
            _targets.Remove(creature.Id);
            StartStage(creature, now, _sink);
        }
    }

    private void Act(Creature creature, double stepSeconds, double now)
    {
        var target = EngagedTarget(creature);
        if (target is null)
        {
            if (_targets.TryGetValue(creature.Id, out var cached) && IsValid(cached) && now < creature.RepathAt)
            {
                target = cached;
            }
            else
            {
                target = Choose(creature);
                creature.RepathAt = now + Tuning.RepathSeconds;
                if (target is null)
                {
                    _targets.Remove(creature.Id);
                }
                else
                {
                    _targets[creature.Id] = target;
                }
            }
        }

        if (target is null)
        {
            creature.ClearPath();
            creature.TargetKey = null;
            return;
        }

        if (InReach(creature.Position, target, creature.Def.Combat.Range))
        {
            creature.ClearPath();
            if (now >= creature.NextAttackAt)
            {
                Strike(creature, target, now);
                creature.NextAttackAt = now + creature.Def.Combat.AttackInterval * (creature.GrappledBy is null ? 1 : Tuning.GrappleSlow);
            }

            return;
        }

        if (creature.Path.Count == 0 || now >= creature.RepathAt || creature.TargetKey != target.Key)
        {
            creature.SetPath(PathTo(creature.Position, target) ?? []);
            creature.RepathAt = now + Tuning.RepathSeconds;
            creature.TargetKey = target.Key;
        }

        creature.Advance(creature.Def.Speed * stepSeconds);
    }

    /// <summary>The fighter a turned creature has turned on, while that fighter stands and stays in reach (ESC-03).</summary>
    private Target? EngagedTarget(Creature creature)
    {
        if (creature.EngagedBy is { } id && _run.FindUnit(id) is { } fighter && !fighter.IsFallen && fighter.Tile is not null
            && fighter.Activity == UnitActivity.Fighting
            && fighter.Position.DistanceTo(creature.Position) <= Tuning.GuardRadius)
        {
            return new Target($"unit:{fighter.Id.Value}", Unit: fighter);
        }

        creature.EngagedBy = null;
        return null;
    }

    private Target? Choose(Creature creature)
    {
        var candidates = creature.Stage switch
        {
            EscalationStage.Wreck => WreckCandidates(creature),
            EscalationStage.Prey => PreyCandidates(creature),
            _ => HuntCandidates(),
        };

        var target = FirstReachable(creature, candidates);
        if (target is not null)
        {
            return target;
        }

        // Nothing to wreck: the walls (D-070). Nobody to reach: the barricade in the way (FIX-02).
        return creature.Stage == EscalationStage.Wreck
            ? FirstReachable(creature, _walls
                .OrderBy(w => InnSense.Distance(creature.Position, w)).ThenBy(w => w.Y).ThenBy(w => w.X)
                .Select(w => new Target($"wall:{w.X},{w.Y}", Wall: w)))
            : FirstReachable(creature, Barricades(creature));
    }

    private IEnumerable<Target> WreckCandidates(Creature creature)
    {
        var fixtures = _run.PlacedFixtures.Where(f => !f.IsBroken)
            .Select(f => (Distance: InnSense.DistanceToTiles(creature.Position, f.Tiles), Order: f.Id.Value, Target: new Target($"fixture:{f.Id.Value}", Fixture: f)));
        var doors = _run.Rooms.Where(r => r.InSession && !r.IsBroken)
            .Select(r => (Distance: InnSense.Distance(creature.Position, r.Door), Order: int.MaxValue, Target: new Target($"door:{r.Zone}", Door: r)));
        return fixtures.Concat(doors).OrderBy(x => x.Distance).ThenBy(x => x.Order).Select(x => x.Target);
    }

    private IEnumerable<Target> PreyCandidates(Creature creature)
    {
        var creatures = _run.CreaturesOnTheFloor.Where(c => c != creature && !c.IsTurned)
            .Select(c => (Distance: c.Position.DistanceTo(creature.Position), Order: c.Id.Value, Target: new Target($"creature:{c.Id.Value}", Creature: c)));
        var staff = _run.StaffOnTheFloor.Where(u => !u.IsFallen && u.Activity != UnitActivity.InRoom)
            .Select(u => (Distance: u.Position.DistanceTo(creature.Position), Order: 100_000 + u.Id.Value, Target: new Target($"unit:{u.Id.Value}", Unit: u)));
        return creatures.Concat(staff).OrderBy(x => x.Distance).ThenBy(x => x.Order).Select(x => x.Target);
    }

    private IEnumerable<Target> HuntCandidates()
    {
        var keeper = _run.Innkeeper;
        if (keeper.Tile is null || keeper.IsFallen)
        {
            return [];
        }

        return [new Target($"unit:{keeper.Id.Value}", Unit: keeper)];
    }

    private IEnumerable<Target> Barricades(Creature creature) =>
        _run.PlacedFixtures
            .Where(f => !f.IsBroken && f.Def.Effects.Any(e => e.Kind == EffectKind.Blocks && e.AdjacentTo is null))
            .OrderBy(f => InnSense.DistanceToTiles(creature.Position, f.Tiles))
            .ThenBy(f => f.Id.Value)
            .Select(f => new Target($"fixture:{f.Id.Value}", Fixture: f));

    private Target? FirstReachable(Creature creature, IEnumerable<Target> candidates)
    {
        foreach (var candidate in candidates.Take(6))
        {
            if (InReach(creature.Position, candidate, creature.Def.Combat.Range) || PathTo(creature.Position, candidate) is not null)
            {
                return candidate;
            }
        }

        return null;
    }

    private bool IsValid(Target target) => target switch
    {
        { Unit: { } u } => !u.IsFallen && u.Tile is not null && u.Activity != UnitActivity.InRoom,
        { Creature: { } c } => _run.Creatures.Contains(c) && !c.IsTurned && !c.IsInRoom,
        { Fixture: { } f } => f.IsPlaced && !f.IsBroken,
        { Door: { } r } => r.InSession && !r.IsBroken,
        _ => true,
    };

    private static bool InReach(TilePosition from, Target target, double range) =>
        target.IsTile
            ? InnSense.DistanceToTiles(from, target.Tiles) <= range + 0.5
            : from.DistanceTo(target.Point) <= range;

    /// <summary>A path to the target: to the tile it stands on, or to a free tile next to a fixture, door or wall.</summary>
    private IReadOnlyList<Tile>? PathTo(TilePosition from, Target target)
    {
        var start = from.NearestTile;
        if (!target.IsTile)
        {
            return _run.Board.FindPath(start, target.Point.NearestTile);
        }

        var tiles = target.Tiles;
        var approaches = tiles
            .SelectMany(Neighbours8)
            .Where(t => !tiles.Contains(t) && _run.Board.IsWalkable(t))
            .Distinct()
            .OrderBy(t => InnSense.Distance(from, t))
            .ThenBy(t => t.Y)
            .ThenBy(t => t.X)
            .Take(4);

        foreach (var approach in approaches)
        {
            if (_run.Board.FindPath(start, approach) is { } path)
            {
                return path;
            }
        }

        return null;
    }

    private void Strike(Creature creature, Target target, double now)
    {
        var attack = creature.Def.Combat.Attack;
        switch (target)
        {
            case { Unit: { } unit }:
                HurtUnit(unit, Math.Max(1, attack - unit.Armour), creature, now);
                break;
            case { Creature: { } victim }:
                HurtCreature(victim, Math.Max(1, attack - victim.Def.Combat.Armour), creature, now);
                break;
            case { Fixture: { } fixture }:
                Landed(creature.Id.ToRef(), fixture.Id.ToRef(), Math.Max(1, attack), target.Point, now);
                _damage.Fixture(fixture, Math.Max(1, attack), creature, "combat.attack");
                break;
            case { Door: { } room }:
                // The session ends and both come out before any violence is shown (CMP-05, INV-06).
                _company.Interrupt(room, now);
                Landed(creature.Id.ToRef(), null, Math.Max(1, attack), target.Point, now);
                _damage.Door(room, Math.Max(1, attack), creature);
                break;
            case { Wall: { } wall }:
                Landed(creature.Id.ToRef(), null, Math.Max(1, attack), target.Point, now);
                _damage.Wall(wall, Math.Max(1, attack), creature);
                break;
        }
    }

    private void Landed(EntityRef source, EntityRef? target, double damage, TilePosition at, double now)
    {
        Bounded(_run.Attacks, (at, now));
        _sink.Raise(new AttackLanded(_sink.Time, source, target, damage));
        _sink.Log("combat.attack", "combat.hit", target, source);
    }

    private void HurtUnit(Unit unit, double damage, Creature by, double now)
    {
        unit.Health -= damage;
        Landed(by.Id.ToRef(), unit.Id.ToRef(), damage, unit.Position, now);
        if (unit.Health > 0)
        {
            return;
        }

        unit.Health = 0;
        unit.Activity = UnitActivity.Fallen;
        unit.FellThisCycle = true;
        unit.Session = null;
        unit.Engaging = null;
        unit.ClearPath();
        Bounded(_run.BloodMarks, unit.Position);
        by.MadeSomeoneFall = true;
        foreach (var c in _run.Creatures.Where(c => c.EngagedBy == unit.Id || c.GrappledBy == unit.Id))
        {
            c.EngagedBy = c.EngagedBy == unit.Id ? null : c.EngagedBy;
            c.GrappledBy = c.GrappledBy == unit.Id ? null : c.GrappledBy;
        }

        _sink.Raise(new UnitFell(_sink.Time, unit.Id, by.Id));
        _sink.Log("unit.fell", "combat.attack", unit.Id.ToRef(), by.Id.ToRef(), flavour: "unit.fell");

        if (unit.Def.IsInnkeeper)
        {
            _sink.EndRun(RunEndReason.InnkeeperFell);
        }
    }

    /// <summary>A creature that had not turned falls to a turned one: it dissolves, with no tribute (ESC-04).</summary>
    private void HurtCreature(Creature victim, double damage, Creature by, double now)
    {
        victim.Health -= damage;
        Landed(by.Id.ToRef(), victim.Id.ToRef(), damage, victim.Position, now);
        if (victim.Health > 0)
        {
            return;
        }

        Bounded(_run.BloodMarks, victim.Position);
        by.MadeSomeoneFall = true;
        _sink.Raise(new CreatureFell(_sink.Time, victim.Id, by.Id));
        _sink.Raise(new CreatureDissolved(_sink.Time, victim.Id));
        _sink.Log("creature.fell", "combat.attack", victim.Id.ToRef(), by.Id.ToRef(), flavour: "unit.fell");
        Remove(victim);
    }

    /// <summary>A unit strikes a turned creature; at 0 health it is banished (ESC-08).</summary>
    private void HitTurned(Creature creature, double damage, Unit by, double now)
    {
        creature.Health -= damage;
        _ancestry.Observe(by, creature.Def);
        Landed(by.Id.ToRef(), creature.Id.ToRef(), damage, creature.Position, now);
        if (creature.Health > 0)
        {
            return;
        }

        _sink.Raise(new CreatureBanished(_sink.Time, creature.Id, by.Id));
        _sink.Raise(new CreatureDissolved(_sink.Time, creature.Id));
        _sink.Log("creature.banished", "combat.attack", creature.Id.ToRef(), by.Id.ToRef(), flavour: "creature.banished");
        _bestiary.Banished(creature.Def);
        Remove(creature);
    }

    private void Remove(Creature creature)
    {
        _behaviour.Release(creature);
        _run.Creatures.Remove(creature);
        _targets.Remove(creature.Id);
    }

    /// <summary>
    /// A fighter engages a turned creature in its zone or guard radius, the most threatening first (ESC-07); this
    /// always comes before serving (STF-04). With nothing to fight it walks back to its post.
    /// </summary>
    private void Fight(Unit unit, TechniqueDef technique, double stepSeconds, double now)
    {
        var target = EngageTarget(unit);
        if (target is null)
        {
            if (unit.Activity == UnitActivity.Fighting)
            {
                unit.Engaging = null;
                unit.Activity = UnitActivity.Returning;
                unit.ClearPath();
            }

            if (unit.Activity == UnitActivity.Returning)
            {
                Return(unit, stepSeconds);
            }

            return;
        }

        unit.Session = null;
        unit.Activity = UnitActivity.Fighting;
        unit.Engaging = target.Id;

        if (unit.Position.DistanceTo(target.Position) <= Math.Max(unit.Stats.Range, unit.RangeOf(technique)))
        {
            unit.ClearPath();
            if (now >= unit.NextAttackAt)
            {
                unit.NextAttackAt = now + unit.Stats.AttackInterval * technique.AttackIntervalMultiplier;
                target.EngagedBy = unit.Id;
                if (technique.Technique == Technique.Grapple)
                {
                    target.GrappledBy = unit.Id;
                }

                HitTurned(target, Math.Max(1, unit.AttackWith(technique) - target.Def.Combat.Armour), unit, now);
            }

            return;
        }

        if (unit.Path.Count == 0 || now >= unit.RepathAt)
        {
            unit.SetPath(_run.Board.FindPath(unit.Position.NearestTile, target.Position.NearestTile) ?? []);
            unit.RepathAt = now + Tuning.RepathSeconds;
        }

        unit.Advance(unit.Stats.Speed * stepSeconds);
    }

    private Creature? EngageTarget(Unit unit)
    {
        var post = unit.Tile!.Value;
        bool InGuard(Creature c) =>
            !c.IsInRoom && c.IsTurned
            && (_content.Layout.ZoneAt(c.Position.NearestTile) == _content.Layout.ZoneAt(post)
                || InnSense.Distance(c.Position, post) <= Tuning.GuardRadius);

        if (unit.Engaging is { } id && _run.FindCreature(id) is { } current && InGuard(current))
        {
            return current;
        }

        return _run.Creatures
            .Where(InGuard)
            .OrderByDescending(c => c.Stage)
            .ThenBy(c => c.Position.DistanceTo(unit.Position))
            .ThenBy(c => c.Id.Value)
            .FirstOrDefault();
    }

    /// <summary>The innkeeper does not leave its post, but strikes a turned creature that reaches it (ESC-06).</summary>
    private void Defend(Unit keeper, double now)
    {
        if (now < keeper.NextAttackAt)
        {
            return;
        }

        var attacker = _run.CreaturesOnTheFloor
            .Where(c => c.IsTurned && c.Position.DistanceTo(keeper.Position) <= keeper.Stats.Range + 1e-6)
            .OrderBy(c => c.Position.DistanceTo(keeper.Position))
            .ThenBy(c => c.Id.Value)
            .FirstOrDefault();
        if (attacker is null)
        {
            return;
        }

        keeper.NextAttackAt = now + keeper.Stats.AttackInterval;
        HitTurned(attacker, Math.Max(1, keeper.Attack - attacker.Def.Combat.Armour), keeper, now);
    }

    /// <summary>Walks a unit back to its post; there it is free again (STF-04).</summary>
    private void Return(Unit unit, double stepSeconds)
    {
        var post = unit.Tile!.Value;
        var home = TilePosition.Center(post);
        if (unit.Path.Count == 0 && unit.Position != home)
        {
            if (_run.Board.FindPath(unit.Position.NearestTile, post) is { } path)
            {
                unit.SetPath(path);
            }
            else
            {
                unit.Position = home;
            }
        }

        unit.Advance(unit.Stats.Speed * stepSeconds);
        if (unit.Path.Count == 0 && unit.Position == home)
        {
            unit.Activity = UnitActivity.AtPost;
        }
    }

    private void ReleaseGrapples()
    {
        foreach (var creature in _run.Creatures.Where(c => c.GrappledBy is not null))
        {
            if (_run.FindUnit(creature.GrappledBy!.Value) is not { } unit || unit.IsFallen || unit.Engaging != creature.Id)
            {
                creature.GrappledBy = null;
            }
        }
    }

    private void Bounded<T>(List<T> list, T item)
    {
        if (list.Count >= Tuning.MaxMarks)
        {
            list.RemoveAt(0);
        }

        list.Add(item);
    }

    private static IEnumerable<Tile> Neighbours4(Tile t)
    {
        yield return new Tile(t.X, t.Y - 1);
        yield return new Tile(t.X + 1, t.Y);
        yield return new Tile(t.X, t.Y + 1);
        yield return new Tile(t.X - 1, t.Y);
    }

    private static IEnumerable<Tile> Neighbours8(Tile t)
    {
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx != 0 || dy != 0)
                {
                    yield return new Tile(t.X + dx, t.Y + dy);
                }
            }
        }
    }

    /// <summary>What a turned creature is going for: a unit, a creature, a fixture, a room door or a wall.</summary>
    private sealed record Target(
        string Key, Unit? Unit = null, Creature? Creature = null, Fixture? Fixture = null, Room? Door = null, Tile? Wall = null)
    {
        public bool IsTile => Unit is null && Creature is null;

        public IReadOnlyList<Tile> Tiles
        {
            get
            {
                if (Fixture is not null)
                {
                    return Fixture.Tiles;
                }

                if (Door is not null)
                {
                    return [Door.Door];
                }

                return Wall is { } wall ? [wall] : [];
            }
        }

        public TilePosition Point => Unit?.Position ?? Creature?.Position ?? TilePosition.Center(Tiles[0]);
    }
}
