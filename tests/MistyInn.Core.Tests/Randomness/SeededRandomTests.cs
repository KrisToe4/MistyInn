using MistyInn.Core.Randomness;

namespace MistyInn.Core.Tests.Randomness;

public class SeededRandomTests
{
    [Test]
    [Property("Requirement", "INV-02")]
    [Property("Requirement", "TST-01")]
    public void SameSeedGivesSameSequence()
    {
        var a = new SeededRandom(1234);
        var b = new SeededRandom(1234);

        var first = Enumerable.Range(0, 1000).Select(_ => a.NextUInt64()).ToArray();
        var second = Enumerable.Range(0, 1000).Select(_ => b.NextUInt64()).ToArray();

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    [Property("Requirement", "INV-02")]
    public void DifferentSeedsGiveDifferentSequences()
    {
        var a = new SeededRandom(1);
        var b = new SeededRandom(2);

        var first = Enumerable.Range(0, 16).Select(_ => a.NextUInt64()).ToArray();
        var second = Enumerable.Range(0, 16).Select(_ => b.NextUInt64()).ToArray();

        Assert.That(second, Is.Not.EqualTo(first));
    }

    /// <summary>
    /// Pins the algorithm (xoshiro256** seeded by SplitMix64). Saves store the generator state and seeded runs must
    /// replay identically, so any change to these values is a save-breaking change.
    /// </summary>
    [Test]
    [Property("Requirement", "INV-02")]
    public void SeedFortyTwoMatchesTheReferenceValues()
    {
        var random = new SeededRandom(42);

        Assert.That(random.GetState(), Is.EqualTo(new RandomState(
            0xBDD732262FEB6E95UL, 0x28EFE333B266F103UL, 0x47526757130F9F52UL, 0x581CE1FF0E4AE394UL)));
        Assert.That(random.NextUInt64(), Is.EqualTo(1546998764402558742UL));
        Assert.That(random.NextUInt64(), Is.EqualTo(6990951692964543102UL));
        Assert.That(random.NextUInt64(), Is.EqualTo(12544586762248559009UL));
    }

    [Test]
    [Property("Requirement", "INV-02")]
    public void NextIntMatchesTheReferenceValues()
    {
        var random = new SeededRandom(42);

        var values = Enumerable.Range(0, 5).Select(_ => random.NextInt(0, 100)).ToArray();

        Assert.That(values, Is.EqualTo(new[] { 42, 2, 9, 93, 76 }));
    }

    [Test]
    [Property("Requirement", "INV-02")]
    public void NextIntStaysWithinItsBounds()
    {
        var random = new SeededRandom(7);
        var seen = new HashSet<int>();

        for (var i = 0; i < 10_000; i++)
        {
            var value = random.NextInt(-3, 4);
            Assert.That(value, Is.InRange(-3, 3));
            seen.Add(value);
        }

        Assert.That(seen, Is.EquivalentTo(new[] { -3, -2, -1, 0, 1, 2, 3 }), "every value in the range comes up");
    }

    [Test]
    [Property("Requirement", "INV-02")]
    public void NextIntHandlesTheFullIntRange()
    {
        var random = new SeededRandom(9);

        for (var i = 0; i < 1000; i++)
        {
            Assert.That(random.NextInt(int.MinValue, int.MaxValue), Is.LessThan(int.MaxValue));
        }
    }

    [TestCase(5, 5)]
    [TestCase(5, 4)]
    [Property("Requirement", "INV-02")]
    public void NextIntRejectsAnEmptyRange(int min, int max)
    {
        var random = new SeededRandom(1);

        Assert.That(() => random.NextInt(min, max), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    [Property("Requirement", "INV-02")]
    public void NextDoubleIsInTheUnitInterval()
    {
        var random = new SeededRandom(3);

        for (var i = 0; i < 10_000; i++)
        {
            Assert.That(random.NextDouble(), Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
        }
    }

    [Test]
    [Property("Requirement", "INV-02")]
    public void ChanceAtTheLimitsIsCertain()
    {
        var random = new SeededRandom(5);

        for (var i = 0; i < 100; i++)
        {
            Assert.That(random.Chance(0), Is.False);
            Assert.That(random.Chance(-1), Is.False);
            Assert.That(random.Chance(1), Is.True);
            Assert.That(random.Chance(2), Is.True);
        }
    }

    [Test]
    [Property("Requirement", "INV-02")]
    public void ChanceAtTheLimitsDoesNotConsumeTheSequence()
    {
        var random = new SeededRandom(5);
        var untouched = new SeededRandom(5);

        random.Chance(0);
        random.Chance(1);

        Assert.That(random.NextUInt64(), Is.EqualTo(untouched.NextUInt64()));
    }

    [Test]
    [Property("Requirement", "INV-02")]
    public void RestoredStateContinuesTheSameSequence()
    {
        var original = new SeededRandom(99);
        for (var i = 0; i < 17; i++)
        {
            original.NextUInt64();
        }

        var state = original.GetState();
        var restored = new SeededRandom(state);
        var reset = new SeededRandom(1);
        reset.SetState(state);

        var expected = Enumerable.Range(0, 50).Select(_ => original.NextUInt64()).ToArray();

        Assert.That(Enumerable.Range(0, 50).Select(_ => restored.NextUInt64()), Is.EqualTo(expected));
        Assert.That(Enumerable.Range(0, 50).Select(_ => reset.NextUInt64()), Is.EqualTo(expected));
    }

    [Test]
    [Property("Requirement", "INV-02")]
    public void AllZeroStateIsRejected()
    {
        Assert.That(() => new SeededRandom(new RandomState(0, 0, 0, 0)), Throws.ArgumentException);
        Assert.That(() => new SeededRandom(1).SetState(default), Throws.ArgumentException);
    }
}
