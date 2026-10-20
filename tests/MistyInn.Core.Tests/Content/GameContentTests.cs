using MistyInn.Core.Content;

namespace MistyInn.Core.Tests.Content;

/// <summary>The checks the simulation relies on until the full validator in step 3 (DAT-04).</summary>
public class GameContentTests
{
    private static readonly GameContent Default = DefaultContent.Create();

    private static GameContent With(
        IReadOnlyList<PlaneDef>? planes = null,
        IReadOnlyList<CreatureDef>? creatures = null,
        IReadOnlyList<StaffDef>? staff = null,
        IReadOnlyList<string>? unlocked = null,
        IReadOnlyList<string>? wander = null) =>
        new(Default.Layout,
            planes ?? Default.Planes,
            creatures ?? Default.Creatures,
            staff ?? Default.StartingStaff,
            unlocked ?? Default.UnlockedPlanes,
            wander ?? Default.WanderZones,
            Default.Balance);

    [Test]
    [Property("Requirement", "PLN-02")]
    [Property("Requirement", "PLN-04")]
    public void DefaultContentHasFivePlanesThreeUnlockedAndAGateEach()
    {
        Assert.That(Default.Planes, Has.Count.EqualTo(5));
        Assert.That(Default.UnlockedPlanes, Has.Count.EqualTo(3));
        Assert.That(Default.Planes.Select(p => Default.GateFor(p.Id)), Is.Unique);
    }

    [Test]
    [Property("Requirement", "PLN-04")]
    public void MorePlanesThanGatesIsRejected()
    {
        var six = Default.Planes.Append(new PlaneDef("sixth", "Sixth", "#ffffff")).ToList();
        var creatures = Default.Creatures.Append(new CreatureDef("x", "X", "sixth", 1)).ToList();

        Assert.That(() => With(planes: six, creatures: creatures), Throws.ArgumentException);
    }

    [Test]
    [Property("Requirement", "CRE-01")]
    public void APlaneWithoutCreaturesIsRejected()
    {
        Assert.That(() => With(creatures: Default.Creatures.Skip(1).ToList()), Throws.ArgumentException);
    }

    [Test]
    [Property("Requirement", "PLN-03")]
    public void UnlockedPlanesMustExist()
    {
        Assert.That(() => With(unlocked: ["nowhere"]), Throws.ArgumentException);
        Assert.That(() => With(unlocked: []), Throws.ArgumentException);
    }

    [Test]
    [Property("Requirement", "RUN-01")]
    public void StartingStaffNeedExactlyOneInnkeeper()
    {
        Assert.That(() => With(staff: Default.StartingStaff.Where(s => !s.IsInnkeeper).ToList()), Throws.ArgumentException);
        Assert.That(() => With(staff: Default.StartingStaff.Append(new StaffDef("second", "Second", true)).ToList()),
            Throws.ArgumentException);
    }

    [Test]
    [Property("Requirement", "CRE-04")]
    public void WanderZonesMustBeInTheLayout()
    {
        Assert.That(() => With(wander: ["the-moon"]), Throws.ArgumentException);
    }

    [Test]
    [Property("Requirement", "PLN-04")]
    public void UnknownPlaneHasNoGate()
    {
        Assert.That(() => Default.GateFor("nowhere"), Throws.TypeOf<KeyNotFoundException>());
        Assert.That(() => Default.Plane("nowhere"), Throws.TypeOf<KeyNotFoundException>());
    }
}
