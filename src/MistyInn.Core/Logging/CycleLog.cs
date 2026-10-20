using MistyInn.Core.Entities;

namespace MistyInn.Core.Logging;

/// <summary>
/// The record of every cause and effect in one cycle (INV-12), shown at Dawn as the timeline (UI-07). It is bounded
/// (INV-05): when only one slot is left, a <see cref="TruncatedKind"/> entry is written there and later records are
/// counted in <see cref="TruncatedCount"/> instead of stored, so the player can always see that the log was cut.
/// </summary>
public sealed class CycleLog
{
    public const string TruncatedKind = "log.truncated";
    public const string TruncatedCause = "log.capacity";

    private readonly List<CycleLogEntry> _entries;
    private long _lastSequence;

    public CycleLog(int capacity)
    {
        if (capacity < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity,
                "Capacity must be at least 2 (one entry plus the truncation marker).");
        }

        Capacity = capacity;
        _entries = new List<CycleLogEntry>();
        Entries = _entries.AsReadOnly();
    }

    public int Capacity { get; }

    /// <summary>A read-only view of the entries (ARC-03); it cannot be cast back to a mutable list.</summary>
    public IReadOnlyList<CycleLogEntry> Entries { get; }

    public bool IsTruncated => TruncatedCount > 0;

    /// <summary>Records that arrived after the log was full and were not stored.</summary>
    public long TruncatedCount { get; private set; }

    public void Record(
        TimeSpan at, string kind, string cause, EntityRef? subject = null, EntityRef? source = null, string? flavour = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(cause);

        if (_entries.Count == Capacity)
        {
            TruncatedCount++;
            return;
        }

        if (_entries.Count == Capacity - 1)
        {
            _entries.Add(new CycleLogEntry(++_lastSequence, at, TruncatedKind, TruncatedCause));
            TruncatedCount++;
            return;
        }

        _entries.Add(new CycleLogEntry(++_lastSequence, at, kind, cause, subject, source, flavour));
    }

    /// <summary>Empties the log for a new cycle.</summary>
    public void Clear()
    {
        _entries.Clear();
        _lastSequence = 0;
        TruncatedCount = 0;
    }
}
