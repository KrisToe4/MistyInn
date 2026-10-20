using MistyInn.Core.Events;

namespace MistyInn.Core.Tests.Events;

public class DomainEventBufferTests
{
    private sealed record TestEvent(TimeSpan At, int Number) : DomainEvent(At);

    private static TestEvent Event(int number) => new(TimeSpan.FromSeconds(number), number);

    [Test]
    [Property("Requirement", "ARC-12")]
    public void DrainReturnsEventsOldestFirstAndEmptiesTheBuffer()
    {
        var buffer = new DomainEventBuffer(10);
        buffer.Add(Event(1));
        buffer.Add(Event(2));
        buffer.Add(Event(3));

        var drained = buffer.Drain();

        Assert.That(drained, Is.EqualTo(new[] { Event(1), Event(2), Event(3) }));
        Assert.That(buffer.Count, Is.Zero);
        Assert.That(buffer.Drain(), Is.Empty);
    }

    [Test]
    [Property("Requirement", "ARC-12")]
    [Property("Requirement", "INV-05")]
    public void FullBufferDropsTheOldestAndCountsIt()
    {
        var buffer = new DomainEventBuffer(3);

        for (var i = 1; i <= 5; i++)
        {
            buffer.Add(Event(i));
        }

        Assert.That(buffer.Count, Is.EqualTo(3));
        Assert.That(buffer.DroppedCount, Is.EqualTo(2));
        Assert.That(buffer.Drain(), Is.EqualTo(new[] { Event(3), Event(4), Event(5) }));
    }

    [Test]
    [Property("Requirement", "INV-05")]
    public void BufferNeverGrowsPastItsCapacity()
    {
        var buffer = new DomainEventBuffer(64);

        for (var i = 0; i < 10_000; i++)
        {
            buffer.Add(Event(i));
            Assert.That(buffer.Count, Is.LessThanOrEqualTo(64));
        }

        Assert.That(buffer.DroppedCount, Is.EqualTo(10_000 - 64));
    }

    [Test]
    [Property("Requirement", "ARC-12")]
    public void DrainedListIsNotAffectedByLaterEvents()
    {
        var buffer = new DomainEventBuffer(4);
        buffer.Add(Event(1));

        var drained = buffer.Drain();
        buffer.Add(Event(2));

        Assert.That(drained, Is.EqualTo(new[] { Event(1) }));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [Property("Requirement", "INV-05")]
    public void CapacityMustBePositive(int capacity)
    {
        Assert.That(() => new DomainEventBuffer(capacity), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    [Property("Requirement", "ARC-12")]
    public void NullEventIsRejected()
    {
        var buffer = new DomainEventBuffer(1);

        Assert.That(() => buffer.Add(null!), Throws.ArgumentNullException);
    }
}
