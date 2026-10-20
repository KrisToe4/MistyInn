using MistyInn.Core.Content;

namespace MistyInn.Core.Tests.Content;

/// <summary>Guidelines the real data in <c>data/</c> follows (TST-07).</summary>
public class RealContentTests
{
    private static GameContent Content => GameContent.Default;

    [Test]
    [Property("Requirement", "STF-14")]
    public void TheRosterIsSixteenCharactersFourPerClass()
    {
        Assert.That(Content.Characters, Has.Count.EqualTo(16));
        foreach (var staffClass in Enum.GetValues<StaffClass>())
        {
            Assert.That(Content.Characters.Count(c => c.MainClass == staffClass), Is.EqualTo(4), staffClass.ToString());
        }
    }

    [Test]
    [Property("Requirement", "STF-14")]
    [Property("Requirement", "STF-15")]
    public void OriginsLeanTowardsTheMortalRealm()
    {
        var mortal = Content.Characters.Where(c => c.Origin == ContentValidator.MortalRealm).ToList();
        var planeBorn = Content.Characters.Where(c => c.Origin != ContentValidator.MortalRealm).ToList();

        Assert.That(mortal, Has.Count.EqualTo(10));
        Assert.That(planeBorn, Has.Count.EqualTo(6));
        Assert.That(planeBorn.Select(c => c.Origin).Distinct().Count(), Is.EqualTo(5), "every plane has someone");
        Assert.That(mortal.Count(c => c.Ancestry is not null), Is.InRange(4, 6), "about half the Mortals");
        Assert.That(mortal.Any(c => c.Ancestry is { } a && !Content.UnlockedPlanes.Contains(a)), Is.True,
            "some ancestries are of locked planes");
    }

    [Test]
    [Property("Requirement", "STF-13")]
    [Property("Requirement", "RAT-01")]
    public void EveryCharacterIsAnAdult()
    {
        Assert.That(Content.Innkeeper.Adult, Is.True);
        Assert.That(Content.Characters.All(c => c.Adult), Is.True);
    }

    [Test]
    [Property("Requirement", "CRE-07")]
    [Property("Requirement", "RAT-01")]
    public void OnlySapientAdultCreaturesWantCompany()
    {
        var wantCompany = Content.Creatures.Where(c => c.Appetites.Any(a => a.Appetite == Appetite.Company)).ToList();

        Assert.That(wantCompany, Is.Not.Empty);
        Assert.That(wantCompany.All(c => c.Sapient && c.Adult), Is.True);
    }

    [Test]
    [Property("Requirement", "STF-06")]
    [Property("Requirement", "STF-07")]
    [Property("Requirement", "STF-17")]
    public void EveryTreeFillsFiveRanksWithinTwoClasses()
    {
        foreach (var character in Content.Characters)
        {
            var classes = character.Tree.SelectMany(r => r.Options)
                .Select(o => Content.Techniques.Single(t => t.Id == o.Technique).Class)
                .Append(character.MainClass)
                .Distinct()
                .ToList();

            Assert.That(classes, Has.Count.LessThanOrEqualTo(2), character.Id);
            Assert.That(UpgradeTrees.FindDeadEnds(character), Is.Empty, character.Id);
            Assert.That(character.Presets.Single(p => p.Rank == 5).Choices, Has.Count.EqualTo(4), character.Id);
        }
    }

    [Test]
    [Property("Requirement", "PLN-01")]
    [Property("Requirement", "PLN-02")]
    [Property("Requirement", "PLN-04")]
    public void FivePlanesThreeUnlockedEachWithCreaturesAndAGate()
    {
        Assert.That(Content.Planes.Select(p => p.Name), Is.EqualTo(new[]
        {
            "The Drowned Deep", "The Ashen Waste", "The Hollow Wood", "The Gearbound Ruin", "The Pale Court",
        }));
        Assert.That(Content.UnlockedPlanes, Has.Count.EqualTo(3));
        Assert.That(Content.Planes.All(p => p.Creatures.Count > 0), Is.True);
        Assert.That(Content.Planes.Select(p => Content.GateFor(p.Id)), Is.Unique);
    }

    [Test]
    [Property("Requirement", "FIX-01")]
    [Property("Requirement", "FIX-02")]
    public void TheBuildMenuHasThePrototypeFixtures()
    {
        Assert.That(Content.Fixtures.Select(f => f.Id), Is.EquivalentTo(new[]
        {
            "bar-counter", "table", "card-table", "stage", "hearth", "quiet-corner", "lantern", "ward-circle",
            "shadowed-alcove", "ledger-desk", "barricade", "salt-line",
        }));
    }

    [Test]
    [Property("Requirement", "ITM-01")]
    [Property("Requirement", "ITM-06")]
    public void TheShopHasThePrototypeItems()
    {
        Assert.That(Content.Items.Count(i => i.Kind == ItemKind.Staff), Is.EqualTo(5));
        Assert.That(Content.Items.Count(i => i.Kind == ItemKind.Inn), Is.EqualTo(6));
        Assert.That(Content.Items.Where(i => i.Kind == ItemKind.Inn).All(i => i.DisplaySpot is not null), Is.True);
    }

    [Test]
    [Property("Requirement", "SIM-04")]
    [Property("Requirement", "BRD-01")]
    public void TheBoardComesFromInnJson()
    {
        Assert.That(Content.Layout.Height, Is.EqualTo(Content.Set.Inn.Rows.Count));
        Assert.That(Content.Layout.Width, Is.EqualTo(Content.Set.Inn.Rows[0].Length));
        Assert.That(Content.Layout.Gates, Has.Count.GreaterThanOrEqualTo(Content.Planes.Count));
    }

    [Test]
    [Property("Requirement", "RUN-01")]
    public void TheRunStartsWithTheInnkeeperAndTwoStaff()
    {
        Assert.That(Content.StartingUnits, Has.Count.EqualTo(3));
        Assert.That(Content.StartingUnits[0].IsInnkeeper, Is.True);
        Assert.That(Content.StartingUnits.Skip(1).All(u => !u.IsInnkeeper), Is.True);
    }

    [Test]
    [Property("Requirement", "DAT-04")]
    public void UnknownIdsAreNotFound()
    {
        Assert.That(() => Content.Plane("nowhere"), Throws.TypeOf<KeyNotFoundException>());
        Assert.That(() => Content.GateFor("nowhere"), Throws.TypeOf<KeyNotFoundException>());
        Assert.That(() => Content.Creature("nobody"), Throws.TypeOf<KeyNotFoundException>());
        Assert.That(() => Content.Character("nobody"), Throws.TypeOf<KeyNotFoundException>());
    }
}
