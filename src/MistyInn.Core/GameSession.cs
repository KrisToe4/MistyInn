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
/// and <see cref="Execute"/> (ARC-03); everything else is a read-only query. A new run is a new session.
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
    private readonly ArrivalSystem _arrivals;
    private readonly CreatureMovementSystem _movement;

    // Real time owed to the simulation, in TimeSpan ticks × StepsPerSecond, so whole steps are exact integers.
    private long _owed;
    private long _steps;

    public GameSession(IRandomSource random, GameSessionLimits? limits = null, GameContent? content = null)
    {
        ArgumentNullException.ThrowIfNull(random);

        limits ??= GameSessionLimits.Default;
        Random = random;
        Ids = new IdAllocator();
        _events = new DomainEventBuffer(limits.EventCapacity);
        _cycleLog = new CycleLog(limits.CycleLogCapacity);
        Content = content ?? DefaultContent.Create();
        _run = new RunState(Content, Ids);
        _arrivals = new ArrivalSystem(Content, _run, random, Ids, this);
        _movement = new CreatureMovementSystem(Content, _run, random, _arrivals.WanderTiles);

        // The first Dusk is the starting state; views read it through the queries, so no events are raised for it.
        StartCycle(1, raiseEvents: false);
    }

    public GameContent Content { get; }

    public BoardLayout Layout => Content.Layout;

    public Phase Phase => _run.Phase;

    public int Cycle => _run.Cycle;

    /// <summary>Mist speed: 0 (paused), 1, 2 or 4 (SCP-13).</summary>
    public int Speed => _run.Speed;

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

    public int StaffOnBoard => _run.StaffOnBoard;

    /// <summary>3 + 1 per two cycles survived, up to 9; the innkeeper does not count (section 6.4).</summary>
    public int MaxStaffOnBoard =>
        Math.Min(Content.Balance.MaxStaffSlots, Content.Balance.BaseStaffSlots + (Cycle - 1) / 2);

    /// <summary>The current (or last) mist phase's cycle log, read-only (INV-12).</summary>
    public IReadOnlyList<CycleLogEntry> CycleLogEntries => _cycleLog.Entries;

    /// <summary>Events dropped because the buffer filled up between drains (D-039).</summary>
    public long DroppedEventCount => _events.DroppedCount;

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

    /// <summary>Returns the events raised since the last drain, oldest first (ARC-12).</summary>
    public IReadOnlyList<DomainEvent> DrainEvents() => _events.Drain();

    /// <summary>A hash of the whole simulation state; equal seeds and commands give equal hashes (INV-02, TST-04).</summary>
    public ulong ComputeStateHash()
    {
        var h = new StateHasher()
            .Add((int)Phase).Add(Cycle).Add(_steps).Add(_owed).Add(_run.Speed)
            .Add(_arrivals.NextArrivalSeconds);

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
            h.Add(unit.Id.Value).Add(unit.Tile?.X ?? -1).Add(unit.Tile?.Y ?? -1);
        }

        foreach (var c in _run.Creatures)
        {
            h.Add(c.Id.Value).Add(c.Def.Id).Add((int)c.Activity).Add(c.IdleUntil)
                .Add(c.Position.X).Add(c.Position.Y).Add(c.PreviousPosition.X).Add(c.PreviousPosition.Y)
                .Add(c.Path.Count);
            foreach (var tile in c.Path)
            {
                h.Add(tile.X).Add(tile.Y);
            }
        }

        return h.Value;
    }

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
        _movement.Step(StepSeconds, now);

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

    private void BreakDawn()
    {
        foreach (var creature in _run.Creatures)
        {
            Raise(new CreatureDissolved(Time, creature.Id));
            Log("creature.dissolved", "dawn", creature.Id.ToRef());
        }

        _run.Creatures.Clear();
        _run.Phase = Phase.Dawn;
        Raise(new PhaseChanged(Time, Cycle, Phase.Dawn));
        Log("phase.dawn", "mist.timer");
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
}
