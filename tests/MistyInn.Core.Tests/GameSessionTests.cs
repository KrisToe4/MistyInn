using MistyInn.Core.Commands;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Logging;
using MistyInn.Core.Randomness;

namespace MistyInn.Core.Tests;

public class GameSessionTests
{
    private sealed record UnknownCommand : GameCommand;

    private sealed record TestEvent(TimeSpan At) : DomainEvent(At);

    private static GameSession NewSession(GameSessionLimits? limits = null) => new(new SeededRandom(1), limits);

    [Test]
    [Property("Requirement", "ARC-01")]
    [Property("Requirement", "INV-03")]
    public void TickAdvancesGameTime()
    {
        var session = NewSession();

        session.Tick(TimeSpan.FromSeconds(1.5));
        session.Tick(TimeSpan.FromSeconds(0.5));

        Assert.That(session.Time, Is.EqualTo(TimeSpan.FromSeconds(2)));
    }

    [Test]
    [Property("Requirement", "INV-03")]
    public void ZeroTickChangesNothing()
    {
        var session = NewSession();

        session.Tick(TimeSpan.Zero);

        Assert.That(session.Time, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    [Property("Requirement", "INV-03")]
    public void NegativeTickIsRejected()
    {
        var session = NewSession();

        Assert.That(() => session.Tick(TimeSpan.FromSeconds(-1)), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(session.Time, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    [Property("Requirement", "ARC-11")]
    [Property("Requirement", "ARC-03")]
    public void CommandWithoutAHandlerIsRefusedAndChangesNothing()
    {
        var session = NewSession();
        session.Tick(TimeSpan.FromSeconds(3));

        var result = session.Execute(new UnknownCommand());

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failure, Is.EqualTo(CommandFailure.NotSupported));
        Assert.That(session.Time, Is.EqualTo(TimeSpan.FromSeconds(3)));
        Assert.That(session.DrainEvents(), Is.Empty);
        Assert.That(session.CycleLogEntries, Is.Empty);
    }

    [Test]
    [Property("Requirement", "ARC-11")]
    public void NullCommandIsRejected()
    {
        var session = NewSession();

        Assert.That(() => session.Execute(null!), Throws.ArgumentNullException);
    }

    [Test]
    [Property("Requirement", "ARC-11")]
    public void FailureNeedsAReason()
    {
        Assert.That(CommandResult.Ok.Succeeded, Is.True);
        Assert.That(CommandResult.Fail(CommandFailure.WrongPhase).Succeeded, Is.False);
        Assert.That(() => CommandResult.Fail(CommandFailure.None), Throws.ArgumentException);
    }

    [Test]
    [Property("Requirement", "ARC-10")]
    public void CommandsAreValueEqualRecords()
    {
        Assert.That(new UnknownCommand(), Is.EqualTo(new UnknownCommand()));
    }

    [Test]
    [Property("Requirement", "ARC-12")]
    public void RaisedEventsAreDrainedOnce()
    {
        var session = NewSession();

        session.Raise(new TestEvent(TimeSpan.FromSeconds(1)));
        session.Raise(new TestEvent(TimeSpan.FromSeconds(2)));

        Assert.That(session.DrainEvents(), Has.Count.EqualTo(2));
        Assert.That(session.DrainEvents(), Is.Empty);
    }

    [Test]
    [Property("Requirement", "ARC-12")]
    [Property("Requirement", "INV-05")]
    public void EventCapacityComesFromTheLimits()
    {
        var session = NewSession(new GameSessionLimits(EventCapacity: 2));

        session.Raise(new TestEvent(TimeSpan.Zero));
        session.Raise(new TestEvent(TimeSpan.Zero));
        session.Raise(new TestEvent(TimeSpan.Zero));

        Assert.That(session.DrainEvents(), Has.Count.EqualTo(2));
        Assert.That(session.DroppedEventCount, Is.EqualTo(1));
    }

    [Test]
    [Property("Requirement", "INV-12")]
    public void LogEntriesAreStampedWithGameTime()
    {
        var session = NewSession();
        session.Tick(TimeSpan.FromSeconds(4));

        session.Log("test.kind", "test.cause", new UnitId(1).ToRef());

        Assert.That(session.CycleLogEntries, Has.Count.EqualTo(1));
        Assert.That(session.CycleLogEntries[0].At, Is.EqualTo(TimeSpan.FromSeconds(4)));
        Assert.That(session.CycleLogEntries[0].Subject, Is.EqualTo(new UnitId(1).ToRef()));
    }

    [Test]
    [Property("Requirement", "ARC-03")]
    public void CycleLogIsReadOnlyToCallers()
    {
        var session = NewSession();

        var entries = session.CycleLogEntries;

        Assert.That(entries, Is.Not.InstanceOf<List<CycleLogEntry>>());
        Assert.That(((ICollection<CycleLogEntry>)entries).IsReadOnly, Is.True, "callers cannot add to the log");
    }

    [Test]
    [Property("Requirement", "ARC-04")]
    public void SessionNeedsARandomSource()
    {
        Assert.That(() => new GameSession(null!), Throws.ArgumentNullException);
    }
}
