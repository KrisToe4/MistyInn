using MistyInn.Core.Content;

namespace MistyInn.Core.Tests.Content;

public class UpgradeTreesTests
{
    private static readonly StaffDef Character = GameContent.Default.Character("staff-01");

    private static UpgradeTrees.Build BuildOf(params string[] choices)
    {
        var build = UpgradeTrees.Start(Character);
        for (var i = 0; i < choices.Length; i++)
        {
            var option = UpgradeTrees.OptionsAt(Character, UpgradeTrees.FirstUpgradeRank + i).Single(o => o.Id == choices[i]);
            build = UpgradeTrees.Apply(option, build);
        }

        return build;
    }

    [Test]
    [Property("Requirement", "STF-17")]
    public void ANewTechniqueIsAvailableOnlyWhileUnknown()
    {
        var food = new TreeOptionDef("x", "food");

        Assert.That(UpgradeTrees.IsAvailable(food, BuildOf()), Is.True);
        Assert.That(UpgradeTrees.IsAvailable(food, BuildOf("r2-food")), Is.False);
    }

    [Test]
    [Property("Requirement", "STF-17")]
    public void AMasteryNeedsAKnownTechniqueAndHappensOnce()
    {
        var masterDrink = new TreeOptionDef("x", "drink", Mastery: true);
        var masterFood = new TreeOptionDef("y", "food", Mastery: true);

        Assert.That(UpgradeTrees.IsAvailable(masterDrink, BuildOf()), Is.True, "the base technique is known");
        Assert.That(UpgradeTrees.IsAvailable(masterFood, BuildOf()), Is.False, "Food is not known yet");
        Assert.That(UpgradeTrees.IsAvailable(masterFood, BuildOf("r2-food")), Is.True);
        Assert.That(UpgradeTrees.IsAvailable(masterDrink, BuildOf("r2-master-drink")), Is.False, "already mastered");
    }

    [Test]
    [Property("Requirement", "STF-07")]
    public void AnOptionWithARequirementWaitsForIt()
    {
        var needsSong = new TreeOptionDef("x", "dance", Requires: "r2-song");

        Assert.That(UpgradeTrees.IsAvailable(needsSong, BuildOf("r2-food")), Is.False);
        Assert.That(UpgradeTrees.IsAvailable(needsSong, BuildOf("r2-song")), Is.True);
    }

    [Test]
    [Property("Requirement", "STF-06")]
    [Property("Requirement", "STF-17")]
    public void AFiveStarBuildHasTheBaseAndFourPicks()
    {
        var build = BuildOf("r2-food", "r3-company", "r4-master-food", "r5-tales");

        Assert.That(build.Chosen, Has.Count.EqualTo(4));
        Assert.That(build.Known, Is.EquivalentTo(new[] { "drink", "food", "company", "tales" }));
        Assert.That(build.Mastered, Is.EquivalentTo(new[] { "food" }));
    }

    [Test]
    [Property("Requirement", "STF-10")]
    public void CheckChoicesExplainsABadPick()
    {
        Assert.That(UpgradeTrees.CheckChoices(Character, ["r2-food", "r3-company"]), Is.Empty);
        Assert.That(UpgradeTrees.CheckChoices(Character, ["r2-master-drink", "r3-master-drink"]).Single(),
            Does.Contain("not an option at rank 3").Or.Contain("not available at rank 3"));
        Assert.That(UpgradeTrees.CheckChoices(Character, ["r9-nothing"]).Single(), Does.Contain("not an option at rank 2"));
    }

    [Test]
    [Property("Requirement", "STF-10")]
    public void EveryPresetIsAValidBuild()
    {
        foreach (var character in GameContent.Default.Characters)
        {
            foreach (var preset in character.Presets)
            {
                Assert.That(UpgradeTrees.CheckChoices(character, preset.Choices), Is.Empty, $"{character.Id} rank {preset.Rank}");
                Assert.That(preset.Choices, Has.Count.EqualTo(preset.Rank - 1));
            }
        }
    }
}
