using MistyInn.Core.Content;
using MistyInn.Core.Randomness;
using MistyInn.Core.Simulation;

namespace MistyInn.Core.Tests.Simulation;

public class PlaneDrawTests
{
    private static readonly string[] Unlocked = ["drowned-deep", "ashen-waste", "hollow-wood", "pale-court"];

    [TestCase(1, 1)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(5, 2)]
    [TestCase(6, 3)]
    [TestCase(9, 3)]
    [TestCase(10, 4, TestName = "The Convergence opens every unlocked plane")]
    [TestCase(14, 4, TestName = "Past the table, the last entry repeats")]
    [Property("Requirement", "PLN-03")]
    public void PlanesPerCycleFollowTheTable(int cycle, int expected)
    {
        Assert.That(PlaneDraw.CountFor(cycle, DefaultContent.Balance, Unlocked.Length), Is.EqualTo(expected));
    }

    [Test]
    [Property("Requirement", "PLN-03")]
    public void CountNeverExceedsTheUnlockedPlanes()
    {
        Assert.That(PlaneDraw.CountFor(6, DefaultContent.Balance, 2), Is.EqualTo(2));
    }

    [Test]
    [Property("Requirement", "PLN-03")]
    public void SameSetIsNeverDrawnTwiceInARow()
    {
        var random = new SeededRandom(11);
        IReadOnlyList<string> previous = Array.Empty<string>();

        for (var i = 0; i < 300; i++)
        {
            var count = 1 + i % 3;
            var drawn = PlaneDraw.Draw(random, Unlocked, count, previous);

            Assert.That(drawn, Has.Count.EqualTo(count));
            Assert.That(drawn, Is.Unique);
            Assert.That(drawn, Is.SubsetOf(Unlocked));
            Assert.That(drawn.ToHashSet().SetEquals(previous), Is.False, $"draw {i}");
            previous = drawn;
        }
    }

    [Test]
    [Property("Requirement", "PLN-03")]
    public void EverySetComesUp()
    {
        var random = new SeededRandom(5);
        var seen = new HashSet<string>();

        for (var i = 0; i < 400; i++)
        {
            seen.Add(string.Join(",", PlaneDraw.Draw(random, Unlocked, 2, Array.Empty<string>())));
        }

        Assert.That(seen, Has.Count.EqualTo(6), "all C(4,2) pairs");
    }

    [Test]
    [Property("Requirement", "PLN-03")]
    public void WhenOnlyOneSetIsPossibleItRepeats()
    {
        var all = PlaneDraw.Draw(new SeededRandom(1), Unlocked, 4, Unlocked);

        Assert.That(all, Is.EqualTo(Unlocked));
    }

    [Test]
    [Property("Requirement", "INV-02")]
    public void SameSeedDrawsTheSameSets()
    {
        var a = new SeededRandom(3);
        var b = new SeededRandom(3);

        for (var i = 0; i < 20; i++)
        {
            Assert.That(PlaneDraw.Draw(b, Unlocked, 2, Array.Empty<string>()),
                Is.EqualTo(PlaneDraw.Draw(a, Unlocked, 2, Array.Empty<string>())));
        }
    }

    [TestCase(0)]
    [TestCase(5)]
    [Property("Requirement", "PLN-03")]
    public void CountMustFitTheUnlockedPlanes(int count)
    {
        Assert.That(() => PlaneDraw.Draw(new SeededRandom(1), Unlocked, count, Array.Empty<string>()),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
