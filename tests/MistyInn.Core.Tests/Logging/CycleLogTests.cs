using MistyInn.Core.Entities;
using MistyInn.Core.Logging;

namespace MistyInn.Core.Tests.Logging;

public class CycleLogTests
{
    [Test]
    [Property("Requirement", "INV-12")]
    public void RecordsKeepTheirCauseEntitiesAndOrder()
    {
        var log = new CycleLog(10);
        var creature = new CreatureId(3).ToRef();
        var lantern = new FixtureId(7).ToRef();

        log.Record(TimeSpan.FromSeconds(1), "agitation.rose", "trigger.light", creature, lantern);
        log.Record(TimeSpan.FromSeconds(2), "creature.turned", "agitation.max", creature);

        Assert.That(log.Entries, Has.Count.EqualTo(2));
        Assert.That(log.Entries[0], Is.EqualTo(new CycleLogEntry(
            1, TimeSpan.FromSeconds(1), "agitation.rose", "trigger.light", creature, lantern)));
        Assert.That(log.Entries[1].Sequence, Is.EqualTo(2));
        Assert.That(log.Entries[1].Source, Is.Null);
        Assert.That(log.IsTruncated, Is.False);
    }

    [Test]
    [Property("Requirement", "INV-05")]
    [Property("Requirement", "INV-12")]
    public void FullLogEndsWithATruncationMarkerAndCountsTheRest()
    {
        var log = new CycleLog(4);

        for (var i = 0; i < 10; i++)
        {
            log.Record(TimeSpan.FromSeconds(i), "test.kind", "test.cause");
        }

        Assert.That(log.Entries, Has.Count.EqualTo(4));
        Assert.That(log.Entries.Take(3).Select(e => e.Kind), Is.All.EqualTo("test.kind"));
        Assert.That(log.Entries[3].Kind, Is.EqualTo(CycleLog.TruncatedKind));
        Assert.That(log.Entries[3].At, Is.EqualTo(TimeSpan.FromSeconds(3)));
        Assert.That(log.IsTruncated, Is.True);
        Assert.That(log.TruncatedCount, Is.EqualTo(7), "the record replaced by the marker and the six after it");
    }

    [Test]
    [Property("Requirement", "INV-12")]
    public void ClearStartsANewCycle()
    {
        var log = new CycleLog(3);
        for (var i = 0; i < 5; i++)
        {
            log.Record(TimeSpan.Zero, "test.kind", "test.cause");
        }

        log.Clear();
        log.Record(TimeSpan.FromSeconds(1), "test.kind", "test.cause");

        Assert.That(log.Entries, Has.Count.EqualTo(1));
        Assert.That(log.Entries[0].Sequence, Is.EqualTo(1));
        Assert.That(log.IsTruncated, Is.False);
    }

    [TestCase(null, "cause")]
    [TestCase("", "cause")]
    [TestCase("kind", " ")]
    [Property("Requirement", "INV-12")]
    public void EveryRecordNeedsAKindAndACause(string? kind, string? cause)
    {
        var log = new CycleLog(3);

        Assert.That(() => log.Record(TimeSpan.Zero, kind!, cause!), Throws.InstanceOf<ArgumentException>());
        Assert.That(log.Entries, Is.Empty);
    }

    [TestCase(1)]
    [TestCase(0)]
    [Property("Requirement", "INV-05")]
    public void CapacityMustLeaveRoomForTheMarker(int capacity)
    {
        Assert.That(() => new CycleLog(capacity), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
