using MistyInn.Core.Bestiary;
using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Logging;
using MistyInn.Core.Randomness;
using MistyInn.Core.Run;
using MistyInn.Core.Simulation;

namespace MistyInn.Core;

/// <summary>
/// The single entry point into Core (ARC-01). It holds the run, and its state changes only through <see cref="Tick"/>
/// and <see cref="Execute"/> (ARC-03); everything else is a read-only query. A new run is a new session; the bestiary
/// passed in outlives it (BST-04).
/// </summary>
public sealed class GameSession : IEventSink
{
    /// <summary>Fixed simulation steps per game second (SIM-02).</summary>
    public const int StepsPerSecond = 30;

    /// <summary>
    /// At most this many steps run in one <see cref="Tick"/>; a longer frame drops the backlog instead of freezing the
    /// game to catch up. Dropping real time never changes the outcome, only how fast it is shown (INV-02).
    /// </summary>
    public const int MaxStepsPerTick = 120;

    private const double StepSeconds = 1.0 / StepsPerSecond;

    private static readonly int[] AllowedSpeeds = [0, 1, 2, 4];

    private readonly DomainEventBuffer _events;
    private readonly CycleLog _cycleLog;
    private readonly RunState _run;
    private readonly InnSense _sense;
    private readonly BestiaryRecorder _bestiary;
    private readonly ServiceSystem _service;
    private readonly CreatureBehaviourSystem _behaviour;
    private readonly AgitationSystem _agitation;
    private readonly ArrivalSystem _arrivals;

    // Real time owed to the simulation, in TimeSpan ticks × StepsPerSecond, so whole steps are exact integers.
    private long _owed;
    private long _steps;

    public GameSession(
        IRandomSource random, GameSessionLimits? limits = null, GameContent? content = null, CreatureBestiary? bestiary = null)
    {
        ArgumentNullException.ThrowIfNull(random);

        limits ??= GameSessionLimits.Default;
        Random = random;
        Ids = new IdAllocator();
        _events = new DomainEventBuffer(limits.EventCapacity);
        _cycleLog = new CycleLog(limits.CycleLogCapacity);
        Content = content ?? GameContent.Default;
        Bestiary = bestiary ?? new CreatureBestiary();
        _run = new RunState(Content, Ids);

        var wanderTiles = _run.Board.WalkableTilesIn(Content.WanderZones);
        if (wanderTiles.Count == 0)
        {
            throw new ArgumentException("The wander zones have no walkable tiles.", nameof(content));
        }

        _sense = new InnSense(Content, _run);
        _bestiary = new BestiaryRecorder(Content, Bestiary, this);
        _service = new ServiceSystem(Content, _run, this, _sense);
        _behaviour = new CreatureBehaviourSystem(Content, _run, random, wanderTiles, _service, _sense);
        _agitation = new AgitationSystem(Content, _run, this, _sense, _bestiary, _behaviour);
        _arrivals = new ArrivalSystem(Content, _run, random, Ids, this, _behaviour, _bestiary);

        // The first Dusk is the starting state; views read it through the queries, so no events are raised for it.
        StartCycle(1, raiseEvents: false);
    }

    public GameContent Content { get; }

    /// <summary>The profile's bestiary (BST-01). Pass the same instance to the next run's session.</summary>
    public CreatureBestiary Bestiary { get; }

    public BoardLayout Layout => Content.Layout;

    public Phase Phase => _run.Phase;

    public int Cycle => _run.Cycle;

    /// <summary>Mist speed: 0 (paused), 1, 2 or 4 (SCP-13).</summary>
    public int Speed => _run.Speed;

    /// <summary>The run's silver (SHP-01).</summary>
    public int Silver => _run.Silver;

    /// <summary>Supplies in stock (FIX-06).</summary>
    public IReadOnlyDictionary<Supply, int> Supplies => _run.Supplies;

    /// <summary>Game time since the mist began (SIM-01); zero at Dusk.</summary>
    public TimeSpan Time => TimeSpan.FromTicks(_steps * TimeSpan.TicksPerSecond / StepsPerSecond);

    public TimeSpan MistDuration => TimeSpan.FromSeconds(Content.Balance.MistDurationSeconds);

    /// <summary>Time left until dawn: the whole mist at Dusk, nothing at Dawn (CYC-02).</summary>
    public TimeSpan TimeToDawn => Phase switch
    {
        Phase.Dusk => MistDuration,
        Phase.Mist => MistDuration > Time ? MistDuration - Time : TimeSpan.Zero,
        _ => TimeSpan.Zero,
    };

    /// <summary>How far the next step is due, 0 to 1, for interpolating views (SIM-03).</summary>
    public double InterpolationAlpha =>
        Phase == Phase.Mist ? Math.Clamp(_owed / (double)TimeSpan.TicksPerSecond, 0, 1) : 0;

    public IReadOnlyList<PlaneDef> ConvergingPlanes => _run.ConvergingPlanes.AsReadOnly();

    public IReadOnlyList<UnitSnapshot> Units => _run.Units.Select(u => u.Snapshot()).ToArray();

    public IReadOnlyList<CreatureSnapshot> Creatures => _run.Creatures.Select(c => c.Snapshot()).ToArray();

    /// <summary>Every fixture owned, placed or in storage (FIX-05).</summary>
    public IReadOnlyList<FixtureSnapshot> Fixtures => _run.Fixtures.Select(f => f.Snapshot()).ToArray();

    public int StaffOnBoard => _run.StaffOnBoard;

    /// <summary>3 + 1 per two cycles survived, up to 9; the innkeeper does not count (section 6.4).</summary>
    public int MaxStaffOnBoard =>
        Math.Min(Content.Balance.MaxStaffSlots, Content.Balance.BaseStaffSlots + (Cycle - 1) / 2);

    /// <summary>The current (or last) mist phase's cycle log, read-only (INV-12).</summary>
    public IReadOnlyList<CycleLogEntry> CycleLogEntries => _cycleLog.Entries;

    /// <summary>Events dropped because the buffer filled up between drains (D-039).</summary>
    public long DroppedEventCount => _events.DroppedCount;

    /// <summary>
    /// The Dusk forecast (BST-03): for each converging plane, the creatures it may send and what the bestiary knows of
    /// them. What is not known yet is null.
    /// </summary>
    public IReadOnlyList<ForecastPlane> Forecast =>
        _run.ConvergingPlanes.Select(plane =>
        {
            var total = plane.Creatures.Sum(c => c.Weight);
            return new ForecastPlane(plane, plane.Creatures.Select(w =>
            {
                var def = Content.Creature(w.Creature);
                var level = Bestiary.LevelOf(def, Content.Balance.Bestiary);
                return new ForecastCreature(
                    def.Id,
                    level,
                    w.Weight / total,
                    level >= BestiaryLevel.Seen ? def.Name : null,
                    level >= BestiaryLevel.Seen ? def.Size : null,
                    level >= BestiaryLevel.Studied ? def.Appetites : null,
                    level >= BestiaryLevel.Understood ? def.Triggers : null);
            }).ToList());
        }).ToList();

    /// <summary>The run's random source, for the systems that need it (ARC-04).</summary>
    internal IRandomSource Random { get; }

    /// <summary>Issues entity IDs (ARC-13).</summary>
    internal IdAllocator Ids { get; }

    /// <summary>
    /// Advances the mist by <paramref name="delta"/> of real time × the speed, in fixed steps (SIM-02, INV-03). Does
    /// nothing at Dusk, at Dawn or at speed 0. Pausing the game is simply not calling this.
    /// </summary>
    public void Tick(TimeSpan delta)
    {
        if (delta < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Game time never runs backwards.");
        }

        if (Phase != Phase.Mist || _run.Speed == 0)
        {
            return;
        }

        _owed += delta.Ticks * _run.Speed * StepsPerSecond;
        var steps = 0;
        while (_owed >= TimeSpan.TicksPerSecond && Phase == Phase.Mist)
        {
            if (steps == MaxStepsPerTick)
            {
                _owed = 0;
                break;
            }

            _owed -= TimeSpan.TicksPerSecond;
            Step();
            steps++;
        }

        if (Phase != Phase.Mist)
        {
            _owed = 0;
        }
    }

    /// <summary>Runs the rest of the mist phase at once, ignoring speed (ARC-05). For tests and tools.</summary>
    public void RunMistToDawn()
    {
        if (Phase != Phase.Mist)
        {
            throw new InvalidOperationException($"RunMistToDawn needs the mist phase; the phase is {Phase}.");
        }

        while (Phase == Phase.Mist)
        {
            Step();
        }

        _owed = 0;
    }

    /// <summary>Validates and applies a command (ARC-11). A refused command changes nothing.</summary>
    public CommandResult Execute(GameCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command is SetSpeed setSpeed)
        {
            return HandleSetSpeed(setSpeed);
        }

        if (Phase == Phase.Mist)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        return command switch
        {
            PlaceUnit place => HandlePlaceUnit(place),
            BenchUnit bench => HandleBenchUnit(bench),
            BeginMist => HandleBeginMist(),
            ContinueFromDawn => HandleContinueFromDawn(),
            BuildFixture build => HandleBuildFixture(build),
            PlaceFixture place => HandlePlaceFixture(place),
            StoreFixture store => HandleStoreFixture(store),
            SellFixture sell => HandleSellFixture(sell),
            BuySupplies buy => HandleBuySupplies(buy),
            _ => CommandResult.Fail(CommandFailure.NotSupported),
        };
    }

    /// <summary>Why <see cref="PlaceUnit"/> would be refused, or <see cref="CommandFailure.None"/>; for the drag cursor.</summary>
    public CommandFailure CheckPlaceUnit(UnitId unitId, Tile tile)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandFailure.WrongPhase;
        }

        var unit = _run.FindUnit(unitId);
        if (unit is null)
        {
            return CommandFailure.UnknownUnit;
        }

        if (!_run.Board.IsPlaceableFloor(tile))
        {
            return CommandFailure.TileBlocked;
        }

        var occupant = _run.Board.UnitAt(tile);
        if (occupant is { } other && other != unitId)
        {
            return CommandFailure.TileOccupied;
        }

        if (unit.Tile is null && !unit.Def.IsInnkeeper && _run.StaffOnBoard >= MaxStaffOnBoard)
        {
            return CommandFailure.BoardFull;
        }

        return CommandFailure.None;
    }

    /// <summary>
    /// Why <see cref="PlaceFixture"/> would be refused, or <see cref="CommandFailure.None"/>; for the drag cursor
    /// (BRD-02, BRD-03, BRD-05, FIX-01).
    /// </summary>
    public CommandFailure CheckPlaceFixture(FixtureId fixtureId, Tile origin, int rotation)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandFailure.WrongPhase;
        }

        var fixture = _run.FindFixture(fixtureId);
        if (fixture is null)
        {
            return CommandFailure.UnknownFixture;
        }

        if (rotation is < 0 or > 3)
        {
            return CommandFailure.InvalidValue;
        }

        var tiles = Fixture.TilesAt(fixture.Def, origin, rotation);
        var own = fixture.Tiles;
        _run.Board.RemoveFixture(own);
        try
        {
            foreach (var tile in tiles)
            {
                if (_run.Board.FixtureAt(tile) is not null || _run.Board.UnitAt(tile) is not null)
                {
                    return CommandFailure.TileOccupied;
                }

                if (_run.Board.IsFixtureForbidden(tile) || !_run.Board.IsPlaceableFloor(tile))
                {
                    return CommandFailure.TileBlocked;
                }

                if (fixture.Def.Zone is { } zone && Layout.ZoneAt(tile) != zone)
                {
                    return CommandFailure.WrongZone;
                }
            }

            return _run.Board.WouldBlockPath(tiles) ? CommandFailure.TileBlocked : CommandFailure.None;
        }
        finally
        {
            _run.Board.PutFixture(fixture.Id, own);
        }
    }

    /// <summary>Returns the events raised since the last drain, oldest first (ARC-12).</summary>
    public IReadOnlyList<DomainEvent> DrainEvents() => _events.Drain();

    /// <summary>A hash of the whole simulation state; equal seeds and commands give equal hashes (INV-02, TST-04).</summary>
    public ulong ComputeStateHash()
    {
        var h = new StateHasher()
            .Add((int)Phase).Add(Cycle).Add(_steps).Add(_owed).Add(_run.Speed).Add(_run.Silver)
            .Add(_arrivals.NextArrivalSeconds);

        foreach (var supply in Enum.GetValues<Supply>())
        {
            h.Add(_run.Supplies[supply]);
        }

        var rng = Random.GetState();
        h.Add(rng.S0).Add(rng.S1).Add(rng.S2).Add(rng.S3);

        var ids = Ids.GetState();
        h.Add(ids.LastUnit).Add(ids.LastCreature).Add(ids.LastFixture);

        foreach (var plane in _run.ConvergingPlanes)
        {
            h.Add(plane.Id);
        }

        foreach (var unit in _run.Units)
        {
            h.Add(unit.Id.Value).Add(unit.Tile?.X ?? -1).Add(unit.Tile?.Y ?? -1)
                .Add(unit.Session?.Technique.Id ?? "").Add(unit.Session?.EndsAt ?? -1);
            foreach (var target in unit.Session?.Targets ?? [])
            {
                h.Add(target.Value);
            }
        }

        foreach (var f in _run.Fixtures)
        {
            h.Add(f.Id.Value).Add(f.Def.Id).Add(f.Origin?.X ?? -1).Add(f.Origin?.Y ?? -1).Add(f.Rotation).Add(f.Health);
        }

        foreach (var c in _run.Creatures)
        {
            h.Add(c.Id.Value).Add(c.Def.Id).Add((int)c.Activity).Add(c.IdleUntil)
                .Add(c.Position.X).Add(c.Position.Y).Add(c.PreviousPosition.X).Add(c.PreviousPosition.Y)
                .Add(c.Agitation).Add((int)c.State).Add(c.Spot?.X ?? -1).Add(c.Spot?.Y ?? -1).Add(c.ServedBy ?? "")
                .Add(c.UnservedSince).Add(c.NextBreakAt).Add(c.WasServed).Add(c.Frightened)
                .Add(c.Path.Count);
            foreach (var tile in c.Path)
            {
                h.Add(tile.X).Add(tile.Y);
            }

            foreach (var trigger in c.ActiveTriggers.Order())
            {
                h.Add((int)trigger);
            }
        }

        foreach (var (id, seen, study, triggers, banish) in Bestiary.State())
        {
            h.Add(id).Add(seen).Add(study).Add(banish);
            foreach (var trigger in triggers)
            {
                h.Add((int)trigger);
            }
        }

        return h.Value;
    }

    /// <summary>The soothe rates settled in the last step (AGI-06), for tests.</summary>
    internal IReadOnlyDictionary<CreatureId, (double Rate, string By)> ServiceRates => _service.Rates;

    /// <summary>The service rules (STF-04), for tests.</summary>
    internal ServiceSystem ServiceForTests => _service;

    /// <summary>For tests: puts a creature on a tile during the mist, as if it had just arrived there (CRE-04).</summary>
    internal CreatureId SpawnCreatureForTests(string creatureDefId, Tile at)
    {
        if (Phase != Phase.Mist)
        {
            throw new InvalidOperationException("Creatures only come during the mist.");
        }

        var def = Content.Creature(creatureDefId);
        var creature = new Creature(Ids.NextCreature(), def, at);
        _run.Creatures.Add(creature);
        _bestiary.Seen(def);
        _behaviour.Decide(creature, MistSeconds, arriving: false);
        return creature.Id;
    }

    /// <summary>For tests: the live creature.</summary>
    internal Creature CreatureForTests(CreatureId id) => _run.FindCreature(id) ?? throw new KeyNotFoundException(id.ToString());

    /// <summary>For tests: the live fixture.</summary>
    internal Fixture FixtureForTests(FixtureId id) => _run.FindFixture(id) ?? throw new KeyNotFoundException(id.ToString());

    internal void Raise(DomainEvent domainEvent) => _events.Add(domainEvent);

    internal void Log(string kind, string cause, EntityRef? subject = null, EntityRef? source = null) =>
        _cycleLog.Record(Time, kind, cause, subject, source);

    void IEventSink.Raise(DomainEvent domainEvent) => Raise(domainEvent);

    void IEventSink.Log(string kind, string cause, EntityRef? subject, EntityRef? source) =>
        Log(kind, cause, subject, source);

    private double MistSeconds => _steps / (double)StepsPerSecond;

    private void Step()
    {
        _steps++;
        var now = MistSeconds;

        _arrivals.Update(now);
        _behaviour.Step(StepSeconds, now);
        _service.Step(now);
        _agitation.Step(StepSeconds, now, _service.Rates);

        if (now >= Content.Balance.MistDurationSeconds)
        {
            BreakDawn();
        }
    }

    private void StartCycle(int cycle, bool raiseEvents)
    {
        _run.Cycle = cycle;
        _run.Phase = Phase.Dusk;
        _steps = 0;
        _owed = 0;

        var count = PlaneDraw.CountFor(cycle, Content.Balance, Content.UnlockedPlanes.Count);
        var planeIds = PlaneDraw.Draw(Random, Content.UnlockedPlanes, count, _run.PreviousPlaneSet);
        _run.PreviousPlaneSet = planeIds;
        _run.ConvergingPlanes.Clear();
        _run.ConvergingPlanes.AddRange(planeIds.Select(Content.Plane));

        if (raiseEvents)
        {
            Raise(new PhaseChanged(Time, Cycle, Phase.Dusk));
            Raise(new ConvergingPlanesDrawn(Time, Cycle, planeIds));
        }
    }

    /// <summary>
    /// Dawn (CYC-03, CRE-05): creatures that stayed calm leave their tribute, served ones add to the bestiary (BST-02),
    /// the cycle's income arrives, and every creature dissolves.
    /// </summary>
    private void BreakDawn()
    {
        var studied = new HashSet<string>();
        foreach (var creature in _run.Creatures.Where(c => !c.IsTurned))
        {
            var tribute = (int)Math.Round(creature.Def.Tribute, MidpointRounding.AwayFromZero);
            _run.Silver += tribute;
            Raise(new TributeLeft(Time, creature.Id, tribute));
            Log("tribute.left", "dawn.calm", creature.Id.ToRef());

            if (creature.WasServed && studied.Add(creature.Def.Id))
            {
                _bestiary.Studied(creature.Def);
            }
        }

        _run.Silver += Content.Balance.IncomePerCycle;
        Log("silver.income", "dawn.income");
        Raise(new SilverChanged(Time, _run.Silver));

        foreach (var creature in _run.Creatures)
        {
            Raise(new CreatureDissolved(Time, creature.Id));
            Log("creature.dissolved", "dawn", creature.Id.ToRef());
        }

        _run.Creatures.Clear();
        _run.Reservations.Clear();
        foreach (var unit in _run.Units)
        {
            unit.Session = null;
        }

        _run.Phase = Phase.Dawn;
        Raise(new PhaseChanged(Time, Cycle, Phase.Dawn));
        Log("phase.dawn", "mist.timer");
    }

    private void RefreshSeats()
    {
        foreach (var fixture in _run.PlacedFixtures)
        {
            fixture.Seats = _run.Board.SeatsAround(fixture.Tiles, fixture.Def.Seats);
        }
    }

    private CommandResult HandleSetSpeed(SetSpeed command)
    {
        if (!AllowedSpeeds.Contains(command.Multiplier))
        {
            return CommandResult.Fail(CommandFailure.InvalidValue);
        }

        if (_run.Speed != command.Multiplier)
        {
            _run.Speed = command.Multiplier;
            Raise(new SpeedChanged(Time, command.Multiplier));
        }

        return CommandResult.Ok;
    }

    private CommandResult HandlePlaceUnit(PlaceUnit command)
    {
        var failure = CheckPlaceUnit(command.Unit, command.Tile);
        if (failure != CommandFailure.None)
        {
            return CommandResult.Fail(failure);
        }

        var unit = _run.FindUnit(command.Unit)!;
        if (unit.Tile == command.Tile)
        {
            return CommandResult.Ok;
        }

        if (unit.Tile is { } from)
        {
            _run.Board.RemoveUnit(from);
        }

        unit.Tile = command.Tile;
        _run.Board.PutUnit(unit.Id, command.Tile);
        Raise(new UnitPlaced(Time, unit.Id, command.Tile));
        return CommandResult.Ok;
    }

    private CommandResult HandleBenchUnit(BenchUnit command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        var unit = _run.FindUnit(command.Unit);
        if (unit is null)
        {
            return CommandResult.Fail(CommandFailure.UnknownUnit);
        }

        if (unit.Def.IsInnkeeper)
        {
            return CommandResult.Fail(CommandFailure.CannotBench);
        }

        if (unit.Tile is not { } tile)
        {
            return CommandResult.Ok;
        }

        _run.Board.RemoveUnit(tile);
        unit.Tile = null;
        Raise(new UnitBenched(Time, unit.Id));
        return CommandResult.Ok;
    }

    private CommandResult HandleBeginMist()
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        if (_run.Innkeeper.Tile is null)
        {
            return CommandResult.Fail(CommandFailure.InnkeeperNotPlaced);
        }

        _cycleLog.Clear();
        _steps = 0;
        _owed = 0;
        _run.Phase = Phase.Mist;
        RefreshSeats();
        _arrivals.StartMist();
        Raise(new PhaseChanged(Time, Cycle, Phase.Mist));
        Log("phase.mist", "player.began");
        return CommandResult.Ok;
    }

    private CommandResult HandleContinueFromDawn()
    {
        if (Phase != Phase.Dawn)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        StartCycle(Cycle + 1, raiseEvents: true);
        return CommandResult.Ok;
    }

    private CommandResult HandleBuildFixture(BuildFixture command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        if (Content.Fixtures.FirstOrDefault(f => f.Id == command.FixtureDefId) is not { } def)
        {
            return CommandResult.Fail(CommandFailure.UnknownFixture);
        }

        if (_run.Silver < def.Cost)
        {
            return CommandResult.Fail(CommandFailure.NotEnoughSilver);
        }

        _run.Silver -= def.Cost;
        var fixture = new Fixture(Ids.NextFixture(), def);
        _run.Fixtures.Add(fixture);
        Raise(new FixtureBuilt(Time, fixture.Id, def.Id));
        Raise(new SilverChanged(Time, _run.Silver));
        return CommandResult.Ok;
    }

    private CommandResult HandlePlaceFixture(PlaceFixture command)
    {
        var failure = CheckPlaceFixture(command.Fixture, command.Tile, command.Rotation);
        if (failure != CommandFailure.None)
        {
            return CommandResult.Fail(failure);
        }

        var fixture = _run.FindFixture(command.Fixture)!;
        _run.Board.RemoveFixture(fixture.Tiles);
        fixture.Place(command.Tile, command.Rotation);
        _run.Board.PutFixture(fixture.Id, fixture.Tiles);
        RefreshSeats();
        Raise(new FixturePlaced(Time, fixture.Id));
        return CommandResult.Ok;
    }

    private CommandResult HandleStoreFixture(StoreFixture command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        if (_run.FindFixture(command.Fixture) is not { } fixture)
        {
            return CommandResult.Fail(CommandFailure.UnknownFixture);
        }

        if (!fixture.IsPlaced)
        {
            return CommandResult.Ok;
        }

        _run.Board.RemoveFixture(fixture.Tiles);
        fixture.Store();
        RefreshSeats();
        Raise(new FixtureStored(Time, fixture.Id));
        return CommandResult.Ok;
    }

    private CommandResult HandleSellFixture(SellFixture command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        if (_run.FindFixture(command.Fixture) is not { } fixture)
        {
            return CommandResult.Fail(CommandFailure.UnknownFixture);
        }

        _run.Board.RemoveFixture(fixture.Tiles);
        _run.Fixtures.Remove(fixture);
        RefreshSeats();
        var refund = fixture.Def.Cost / 2;
        _run.Silver += refund;
        Raise(new FixtureSold(Time, fixture.Id, refund));
        Raise(new SilverChanged(Time, _run.Silver));
        return CommandResult.Ok;
    }

    private CommandResult HandleBuySupplies(BuySupplies command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        if (command.Bundles < 1 || !Content.Balance.Supplies.TryGetValue(command.Supply, out var bundle))
        {
            return CommandResult.Fail(CommandFailure.InvalidValue);
        }

        var price = bundle.Price * command.Bundles;
        if (_run.Silver < price)
        {
            return CommandResult.Fail(CommandFailure.NotEnoughSilver);
        }

        _run.Silver -= price;
        _run.Supplies[command.Supply] += bundle.Amount * command.Bundles;
        Raise(new SuppliesChanged(Time, command.Supply, _run.Supplies[command.Supply]));
        Raise(new SilverChanged(Time, _run.Silver));
        return CommandResult.Ok;
    }
}

/// <summary>One converging plane in the Dusk forecast (BST-03).</summary>
public sealed record ForecastPlane(PlaneDef Plane, IReadOnlyList<ForecastCreature> Creatures);

/// <summary>
/// A creature a plane may send, with what the bestiary knows (BST-03). <paramref name="Share"/> is its part of the
/// plane's weights. Unknown details are null and shown as "?".
/// </summary>
public sealed record ForecastCreature(
    string CreatureDefId,
    BestiaryLevel Level,
    double Share,
    string? Name,
    int? Size,
    IReadOnlyList<AppetiteDef>? Appetites,
    IReadOnlyList<TriggerDef>? Triggers);
