using MistyInn.Core.Commands;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Logging;
using MistyInn.Core.Randomness;

namespace MistyInn.Core;

/// <summary>
/// The single entry point into Core (ARC-01). State changes only through <see cref="Tick"/> and
/// <see cref="Execute"/> (ARC-03); everything else is a read-only query. The run state, the fixed-step tick and the
/// command handlers are added from step 2.
/// </summary>
public sealed class GameSession
{
    private readonly DomainEventBuffer _events;
    private readonly CycleLog _cycleLog;

    public GameSession(IRandomSource random, GameSessionLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(random);

        limits ??= GameSessionLimits.Default;
        Random = random;
        Ids = new IdAllocator();
        _events = new DomainEventBuffer(limits.EventCapacity);
        _cycleLog = new CycleLog(limits.CycleLogCapacity);
    }

    /// <summary>Game time advanced so far (SIM-01). Only <see cref="Tick"/> changes it (INV-03).</summary>
    public TimeSpan Time { get; private set; }

    /// <summary>The cycle log, read-only (INV-12).</summary>
    public IReadOnlyList<CycleLogEntry> CycleLogEntries => _cycleLog.Entries;

    /// <summary>Events dropped because the buffer filled up between drains.</summary>
    public long DroppedEventCount => _events.DroppedCount;

    /// <summary>The run's random source, for the systems that need it (ARC-04).</summary>
    internal IRandomSource Random { get; }

    /// <summary>Issues entity IDs (ARC-13).</summary>
    internal IdAllocator Ids { get; }

    /// <summary>Advances game time. Pausing is simply not calling this (INV-03).</summary>
    public void Tick(TimeSpan delta)
    {
        if (delta < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Game time never runs backwards.");
        }

        Time += delta;
    }

    /// <summary>Validates and applies a command (ARC-11). A refused command changes nothing.</summary>
    public CommandResult Execute(GameCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // No command has a handler yet; each later step adds the handlers it needs.
        return CommandResult.Fail(CommandFailure.NotSupported);
    }

    /// <summary>Returns the events raised since the last drain, oldest first (ARC-12).</summary>
    public IReadOnlyList<DomainEvent> DrainEvents() => _events.Drain();

    internal void Raise(DomainEvent domainEvent) => _events.Add(domainEvent);

    internal void Log(string kind, string cause, EntityRef? subject = null, EntityRef? source = null) =>
        _cycleLog.Record(Time, kind, cause, subject, source);
}
