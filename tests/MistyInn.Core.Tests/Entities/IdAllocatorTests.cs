using MistyInn.Core.Entities;

namespace MistyInn.Core.Tests.Entities;

public class IdAllocatorTests
{
    [Test]
    [Property("Requirement", "ARC-13")]
    public void IdsStartAtOneAndCountUpPerKind()
    {
        var ids = new IdAllocator();

        Assert.That(ids.NextUnit(), Is.EqualTo(new UnitId(1)));
        Assert.That(ids.NextUnit(), Is.EqualTo(new UnitId(2)));
        Assert.That(ids.NextCreature(), Is.EqualTo(new CreatureId(1)));
        Assert.That(ids.NextFixture(), Is.EqualTo(new FixtureId(1)));
        Assert.That(ids.NextCreature(), Is.EqualTo(new CreatureId(2)));
    }

    [Test]
    [Property("Requirement", "ARC-13")]
    public void IdsAreNeverReused()
    {
        var ids = new IdAllocator();

        var issued = Enumerable.Range(0, 1000).Select(_ => ids.NextCreature()).ToList();

        Assert.That(issued, Is.Unique);
    }

    [Test]
    [Property("Requirement", "ARC-13")]
    public void RestoredAllocatorContinuesWhereItStopped()
    {
        var ids = new IdAllocator();
        ids.NextUnit();
        ids.NextUnit();
        ids.NextFixture();

        var restored = new IdAllocator(ids.GetState());

        Assert.That(restored.NextUnit(), Is.EqualTo(new UnitId(3)));
        Assert.That(restored.NextCreature(), Is.EqualTo(new CreatureId(1)));
        Assert.That(restored.NextFixture(), Is.EqualTo(new FixtureId(2)));
    }

    [Test]
    [Property("Requirement", "ARC-13")]
    public void NegativeCountersAreRejected()
    {
        Assert.That(() => new IdAllocator(new IdAllocatorState(-1, 0, 0)),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    [Property("Requirement", "ARC-13")]
    public void IdsOfDifferentKindsAreDistinguishable()
    {
        var unit = new UnitId(1).ToRef();
        var creature = new CreatureId(1).ToRef();

        Assert.That(unit, Is.Not.EqualTo(creature));
        Assert.That(unit.ToString(), Is.EqualTo("unit:1"));
        Assert.That(creature.ToString(), Is.EqualTo("creature:1"));
    }
}
