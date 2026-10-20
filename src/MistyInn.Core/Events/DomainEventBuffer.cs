namespace MistyInn.Core.Events;

/// <summary>
/// A fixed-capacity buffer of domain events (ARC-12, INV-05), drained by the presentation once per frame. When it is
/// full, the oldest event is dropped and counted. Views use events only for presentation; the cycle log is the
/// authoritative record of what happened (INV-12), so a dropped event never changes the game.
/// </summary>
public sealed class DomainEventBuffer
{
    private readonly Queue<DomainEvent> _events;

    public DomainEventBuffer(int capacity)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be at least 1.");
        }

        Capacity = capacity;
        _events = new Queue<DomainEvent>(capacity);
    }

    public int Capacity { get; }

    public int Count => _events.Count;

    /// <summary>How many events were dropped because the buffer was full, since it was created.</summary>
    public long DroppedCount { get; private set; }

    public void Add(DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        if (_events.Count == Capacity)
        {
            _events.Dequeue();
            DroppedCount++;
        }

        _events.Enqueue(domainEvent);
    }

    /// <summary>Returns every buffered event, oldest first, and empties the buffer.</summary>
    public IReadOnlyList<DomainEvent> Drain()
    {
        if (_events.Count == 0)
        {
            return Array.Empty<DomainEvent>();
        }

        var drained = _events.ToArray();
        _events.Clear();
        return drained;
    }
}
