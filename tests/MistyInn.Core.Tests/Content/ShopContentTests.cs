using MistyInn.Core.Content;
using static MistyInn.Core.Tests.Content.ContentTestKit;

namespace MistyInn.Core.Tests.Content;

/// <summary>Step 6 content: synergies, items, display spots, adjacency and the shop's tuning (DAT-04, TST-07, TST-09).</summary>
public class ShopContentTests
{
    private static GameContent Content => GameContent.Default;

    private static ContentSet WithSynergy(string id, Func<SynergyDef, SynergyDef> change) =>
        Set with { Synergies = Set.Synergies.Select(s => s.Id == id ? change(s) : s).ToList() };

    [Test]
    [Property("Requirement", "SYN-01")]
    [Property("Requirement", "SYN-02")]
    public void EveryClassAndOriginHasASynergyWithTheRightThresholds()
    {
        foreach (var staffClass in Enum.GetValues<StaffClass>())
        {
            var synergy = Content.Synergies.Single(s => s.Class == staffClass);
            Assert.That(synergy.Tiers.Select(t => t.Count), Is.SubsetOf(new[] { 2, 4, 6 }), staffClass.ToString());
        }

        var origins = Content.Planes.Select(p => p.Id).Append(ContentValidator.MortalRealm);
        foreach (var origin in origins)
        {
            var synergy = Content.Synergies.Single(s => s.Origin == origin);
            Assert.That(synergy.Tiers.Select(t => t.Count), Is.EqualTo(new[] { 2, 4 }), origin);
        }
    }

    [Test]
    [Property("Requirement", "SYN-01")]
    [Property("Requirement", "DAT-04")]
    public void TheValidatorChecksSynergies()
    {
        AssertRejected(WithSynergy("fighter", s => s with { Tiers = [s.Tiers[0] with { Count = 3 }] }), "tier counts must be from 2, 4, 6");
        AssertRejected(WithSynergy("mortal", s => s with { Tiers = [s.Tiers[0] with { Count = 6 }] }), "tier counts must be from 2, 4");
        AssertRejected(WithSynergy("fighter", s => s with { Origin = "mortal-realm" }), "needs either a class or an origin");
        AssertRejected(WithSynergy("mortal", s => s with { Origin = "the-moon" }), "unknown origin 'the-moon'");
        AssertRejected(WithSynergy("gambler", s => s with { Tiers = [s.Tiers[1], s.Tiers[0]] }), "in increasing order");
    }

    [Test]
    [Property("Requirement", "ITM-01")]
    [Property("Requirement", "ITM-06")]
    public void ThePrototypeItemsAreInTheData()
    {
        Assert.That(Content.Items.Select(i => i.Id), Is.EquivalentTo(new[]
        {
            "brass-knuckles", "lucky-dice", "silk-scarf", "lute", "ward-charm", "drowned-bell", "innkeepers-ledger",
            "loaded-coin", "ember-in-a-jar", "spare-keys", "cursed-crown",
        }));
        Assert.That(Content.Item("cursed-crown").Rarity, Is.EqualTo(Rarity.Cursed));
        Assert.That(Content.Items.Where(i => i.Kind == ItemKind.Inn).Select(i => i.DisplaySpot), Has.All.Not.Null);
    }

    [Test]
    [Property("Requirement", "ART-17")]
    [Property("Requirement", "ITM-04")]
    public void EveryInnItemHasADisplaySpotInTheInn()
    {
        foreach (var item in Content.Items.Where(i => i.Kind == ItemKind.Inn))
        {
            Assert.That(Content.DisplaySpots.ContainsKey(item.DisplaySpot!), Is.True, item.Id);
            Assert.That(Content.Layout.Contains(Content.DisplaySpots[item.DisplaySpot!]), Is.True, item.Id);
        }

        AssertRejected(WithItem("spare-keys", i => i with { DisplaySpot = "attic" }), "display spot 'attic' is not in inn.json");
    }

    [Test]
    [Property("Requirement", "FIX-03")]
    [Property("Requirement", "DAT-04")]
    public void AdjacencyNamesAFixtureOrARoomDoor()
    {
        Assert.That(Content.Fixture("lantern").Effects.Any(e => e.AdjacentTo == "stage"), Is.True);
        Assert.That(Content.Fixture("ward-circle").Effects.Any(e => e.AdjacentTo == EffectDef.AdjacentToRoomDoor), Is.True);

        AssertRejected(
            WithFixture("lantern", f => f with { Effects = [new EffectDef(EffectKind.Radius, Amount: 1, AdjacentTo: "moon-gate")] }),
            "adjacentTo must be a fixture or RoomDoor");
    }

    [Test]
    [Property("Requirement", "SHP-02")]
    [Property("Requirement", "DAT-04")]
    public void EveryRarityNeedsAnItemWeight()
    {
        Assert.That(Content.Balance.Shop.ItemWeights.Keys, Is.EquivalentTo(Enum.GetValues<Rarity>()));

        var weights = new Dictionary<Rarity, double> { [Rarity.Common] = 1, [Rarity.Rare] = 1 };
        AssertRejected(Set with { Balance = Set.Balance with { Shop = Set.Balance.Shop with { ItemWeights = weights } } }, "itemWeights");
    }

    [Test]
    [Property("Requirement", "STF-07")]
    [Property("Requirement", "STF-10")]
    [Property("Requirement", "TST-09")]
    public void EveryTreeSpansAtMostTwoClassesOnEveryPath()
    {
        foreach (var character in Content.Characters)
        {
            var classes = character.Tree.SelectMany(r => r.Options).Select(o => Content.Technique(o.Technique).Class)
                .Append(Content.Technique(character.BaseTechnique).Class)
                .Distinct()
                .ToList();
            Assert.That(classes, Has.Count.LessThanOrEqualTo(2), character.Id);
            Assert.That(UpgradeTrees.FindDeadEnds(character), Is.Empty, character.Id);
            foreach (var preset in character.Presets)
            {
                Assert.That(UpgradeTrees.CheckChoices(character, preset.Choices), Is.Empty, $"{character.Id} ★{preset.Rank}");
            }
        }
    }
}
