using MistyInn.Core.Content;
using static MistyInn.Core.Tests.Content.ContentTestKit;

namespace MistyInn.Core.Tests.Content;

/// <summary><c>omens.json</c>, <c>unlocks.json</c> and their validator rules (OMN-02, OMN-03, MET-03, DAT-04).</summary>
public class RoguelikeContentTests
{
    private static ContentSet WithOmen(OmenDef omen) => Set with { Omens = [omen] };

    private static ContentSet WithUnlocks(params UnlockDef[] unlocks) => Set with { Unlocks = unlocks };

    [Test]
    [Property("Requirement", "OMN-02")]
    [Property("Requirement", "DAT-01")]
    public void ThePrototypeShipsTheOmensThatFitTheEffectKinds()
    {
        var omens = GameContent.Default.Omens;

        // Thick Mist, Drought and Wanderers wait for effect kinds of their own (D-086).
        Assert.That(omens.Select(o => o.Id), Is.EqualTo(new[] { "blood-moon", "surge", "feast-night" }));
        var bloodMoon = GameContent.Default.Omen("blood-moon");
        Assert.That(bloodMoon.Effects.Single(), Is.EqualTo(new EffectDef(EffectKind.ArrivalAgitation, Amount: 20)));
        var feast = GameContent.Default.Omen("feast-night");
        Assert.That(feast.Effects, Is.EquivalentTo(new[]
        {
            new EffectDef(EffectKind.Tribute, Multiplier: 1.5),
            new EffectDef(EffectKind.ArrivalRate, Multiplier: 1.3),
        }));
        Assert.That(GameContent.Default.Omen("surge").Effects.Single(), Is.EqualTo(new EffectDef(EffectKind.PlaneWeight, Multiplier: 2)));
    }

    [Test]
    [Property("Requirement", "OMN-03")]
    [Property("Requirement", "DAT-03")]
    [Property("Requirement", "DAT-04")]
    public void OmensUseOnlyInnWideEffectKinds()
    {
        var omen = new OmenDef("odd", "Odd", "Something odd.", 1, [new EffectDef(EffectKind.ArrivalAgitation, Amount: 5)]);
        Assert.That(Problems(WithOmen(omen)), Is.Empty);

        AssertRejected(WithOmen(omen with { Effects = [new EffectDef(EffectKind.Provides, Appetite: Appetite.Quiet, Seats: 2)] }),
            "an omen can only use");
        AssertRejected(WithOmen(omen with { Effects = [new EffectDef(EffectKind.Tribute, Amount: 1, AdjacentTo: "hearth")] }),
            "an omen can only use");
        AssertRejected(WithOmen(omen with { Effects = [] }), "omen 'odd' has no effects");
        AssertRejected(WithOmen(omen with { Weight = 0 }), "weight must be above 0");
        AssertRejected(WithOmen(omen with { Description = " " }), "needs a name and a description");
        AssertRejected(WithOmen(omen with { Effects = [new EffectDef(EffectKind.ArrivalRate, Multiplier: 0)] }),
            "multiplier must be above 0");
        AssertRejected(Set with { Omens = [omen, omen] }, "duplicate id 'odd'");
    }

    [Test]
    [Property("Requirement", "OMN-03")]
    [Property("Requirement", "DAT-03")]
    public void OnlyAnOmenMayLeaveAPlaneWeightsPlaneOut()
    {
        var surge = new EffectDef(EffectKind.PlaneWeight, Multiplier: 2);
        var omen = new OmenDef("surgeish", "Surgeish", "One gate pours.", 1, [surge]);

        Assert.That(Problems(WithOmen(omen)), Is.Empty, "the omen draws the plane (D-087)");
        AssertRejected(WithItem("drowned-bell", i => i with { Effects = [surge] }), "a PlaneWeight effect is missing a parameter");
        Assert.That(Problems(WithItem("drowned-bell", i => i with { Effects = [surge with { Plane = "drowned-deep" }] })), Is.Empty);
    }

    [Test]
    [Property("Requirement", "MET-03")]
    [Property("Requirement", "DAT-01")]
    public void ThePrototypeLocksTwoPlanesFourCharactersThreeItemsAndThreeFixtures()
    {
        var unlocks = GameContent.Default.Unlocks;

        Assert.That(unlocks.Where(u => u.Kind == UnlockKind.Plane).Select(u => u.Target),
            Is.EquivalentTo(new[] { "gearbound-ruin", "pale-court" }));
        Assert.That(unlocks.Count(u => u.Kind == UnlockKind.Character), Is.EqualTo(4));
        Assert.That(unlocks.Count(u => u.Kind == UnlockKind.Item), Is.EqualTo(3));
        Assert.That(unlocks.Count(u => u.Kind == UnlockKind.Fixture), Is.EqualTo(3));
        Assert.That(unlocks.Count(u => u.Kind == UnlockKind.StartingStaff), Is.EqualTo(1));
        Assert.That(GameContent.Default.UnlockName(GameContent.Default.Unlock("unlock-pale-court")), Is.EqualTo("The Pale Court"));
        Assert.That(GameContent.Default.UnlockName(GameContent.Default.Unlock("unlock-starting-staff")), Is.EqualTo("Choose your starting staff"));
    }

    [Test]
    [Property("Requirement", "MET-03")]
    [Property("Requirement", "DAT-04")]
    public void UnlocksMustNameRealContent()
    {
        AssertRejected(WithUnlocks(new UnlockDef("unlock-moon", UnlockKind.Plane, 5, "the-moon")), "unknown Plane 'the-moon'");
        AssertRejected(WithUnlocks(new UnlockDef("unlock-nobody", UnlockKind.Character, 5, "nobody")), "unknown Character 'nobody'");
        AssertRejected(WithUnlocks(new UnlockDef("unlock-x", UnlockKind.Item, 5)), "needs a target");
        AssertRejected(WithUnlocks(new UnlockDef("unlock-x", UnlockKind.Item, -1, "lute")), "cost cannot be negative");
        AssertRejected(WithUnlocks(
                new UnlockDef("unlock-a", UnlockKind.Item, 5, "lute"),
                new UnlockDef("unlock-b", UnlockKind.Item, 5, "lute")),
            "'lute' is already locked by another unlock");
        AssertRejected(WithUnlocks(new UnlockDef("unlock-choose", UnlockKind.StartingStaff, 5, "staff-02", "Choose")),
            "a StartingStaff unlock has no target");
    }

    [Test]
    [Property("Requirement", "MET-03")]
    [Property("Requirement", "DAT-04")]
    public void UnlocksNeverMakeTheBaseGameUnwinnable()
    {
        AssertRejected(WithUnlocks(new UnlockDef("unlock-deep", UnlockKind.Plane, 5, "drowned-deep")),
            "'drowned-deep' is part of every run's start");
        AssertRejected(WithUnlocks(new UnlockDef("unlock-01", UnlockKind.Character, 5, "staff-01")),
            "'staff-01' is part of every run's start");
        AssertRejected(WithUnlocks(new UnlockDef("unlock-hearth", UnlockKind.Fixture, 5, "hearth")),
            "'hearth' is part of every run's start");

        // Quiet comes only from Comfort and the quiet corner: take both away and the Drowned Deep cannot be soothed.
        var noComfort = WithTechnique("comfort", t => t with { Answers = [] });
        AssertRejected(noComfort with { Unlocks = [new UnlockDef("unlock-quiet", UnlockKind.Fixture, 5, "quiet-corner")] },
            "nothing unlocked at the start answers Quiet");
    }
}
