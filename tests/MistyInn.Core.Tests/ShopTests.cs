using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Content;
using MistyInn.Core.Events;
using MistyInn.Core.Run;
using static MistyInn.Core.Tests.TestGame;

namespace MistyInn.Core.Tests;

/// <summary>The shop, ranks, upgrades and selling (section 13.1, section 11.2, TST-09).</summary>
public class ShopTests
{
    // Staff 01: Mortal, hidden Pale Court ancestry, Drink, base cost 1, owned at the start. Staff 02: Mortal, no
    // ancestry, Food, base cost 1. Staff 03: born in the Drowned Deep. Staff 07: hidden Drowned ancestry. Staff 09
    // (Fighter) is the other starting character.
    private static GameSession Rich(GameContent? content = null)
    {
        var session = NewSession(content: content);
        session.RunForTests.Silver = 100;
        return session;
    }

    private static StaffOffer Offer(GameSession session, string characterId, int rank) =>
        new(characterId, rank, session.ShopForTests.PriceOf(session.Content.Character(characterId), rank));

    private static UnitSnapshot Unit(GameSession session, string characterId) => session.Units.Single(u => u.StaffId == characterId);

    [Test]
    [Property("Requirement", "SHP-02")]
    [Property("Requirement", "SHP-04")]
    [Property("Requirement", "UI-03")]
    [Property("Requirement", "TST-09")]
    public void EachDuskShowsThreeStaffAndThreeItemOffersWithoutRepeats()
    {
        var session = NewSession();

        for (var cycle = 0; cycle < 4; cycle++)
        {
            var shop = session.Shop;
            Assert.That(shop.Staff, Has.Count.EqualTo(3));
            Assert.That(shop.Items, Has.Count.EqualTo(3));
            Assert.That(shop.Staff.Select(o => o!.Offer.CharacterId), Is.Unique, "no character is shown twice");
            Assert.That(shop.Items.Select(o => o!.Offer.ItemId), Is.Unique);
            PlayCycle(session);
        }
    }

    [Test]
    [Property("Requirement", "SHP-03")]
    [Property("Requirement", "TST-09")]
    public void RankOddsGrowOverTheRun()
    {
        var shop = NewSession().ShopForTests;

        Assert.That(shop.OddsFor(1), Is.EqualTo(new[] { 85, 15, 0, 0, 0 }));
        Assert.That(shop.OddsFor(2), Is.EqualTo(new[] { 85, 15, 0, 0, 0 }));
        Assert.That(shop.OddsFor(4), Is.EqualTo(new[] { 65, 28, 7, 0, 0 }));
        Assert.That(shop.OddsFor(6), Is.EqualTo(new[] { 45, 35, 16, 4, 0 }));
        Assert.That(shop.OddsFor(8), Is.EqualTo(new[] { 30, 35, 24, 9, 2 }));
        Assert.That(shop.OddsFor(10), Is.EqualTo(new[] { 20, 30, 28, 16, 6 }));

        var ranks = Enumerable.Range(0, 4000).Select(_ => shop.DrawRank(1)).ToList();
        Assert.That(ranks.All(r => r is 1 or 2), Is.True, "only ★1 and ★2 in cycles 1-2");
        Assert.That(ranks.Count(r => r == 2) / 4000.0, Is.EqualTo(0.15).Within(0.03));

        var late = Enumerable.Range(0, 4000).Select(_ => shop.DrawRank(9)).ToList();
        Assert.That(late.Count(r => r == 5) / 4000.0, Is.EqualTo(0.06).Within(0.02));
    }

    [Test]
    [Property("Requirement", "SHP-04")]
    [Property("Requirement", "STF-15")]
    [Property("Requirement", "TST-09")]
    public void PityAndHomePlaneWeightsStackAndHiddenAncestryCounts()
    {
        var session = NewSession();
        Assert.That(session.Execute(new DevSetConvergingPlanes(["drowned-deep"])).Succeeded, Is.True);
        var shop = session.ShopForTests;

        Assert.That(shop.WeightOf(session.Content.Character("staff-02")), Is.EqualTo(1.0));
        Assert.That(shop.WeightOf(session.Content.Character("staff-01")), Is.EqualTo(1.5), "owned: pity");
        Assert.That(shop.WeightOf(session.Content.Character("staff-03")), Is.EqualTo(2.0), "born in a converging plane");
        Assert.That(shop.WeightOf(session.Content.Character("staff-07")), Is.EqualTo(2.0), "hidden Drowned ancestry");

        Assert.That(session.Execute(new DevSetConvergingPlanes(["pale-court"])).Succeeded, Is.True);
        Assert.That(shop.WeightOf(session.Content.Character("staff-01")), Is.EqualTo(3.0), "pity × home plane, ancestry still hidden");
    }

    [Test]
    [Property("Requirement", "SHP-04")]
    [Property("Requirement", "SHP-07")]
    [Property("Requirement", "STF-08")]
    [Property("Requirement", "TST-09")]
    public void AnyCharacterCanBeOfferedExceptOwnedFiveStarsAndThoseShowing()
    {
        var session = NewSession();
        var shop = session.ShopForTests;
        var none = new HashSet<string>();

        Assert.That(shop.CanBeDrawn(session.Content.Character("staff-01"), none), Is.True, "owned: an upgrade offer");
        Assert.That(shop.CanBeDrawn(session.Content.Character("staff-02"), none), Is.True, "never hired");
        Assert.That(shop.CanBeDrawn(session.Content.Character("staff-02"), new HashSet<string> { "staff-02" }), Is.False);

        session.UnitForTests(Unit(session, "staff-01").Id).Rank = 5;
        Assert.That(shop.CanBeDrawn(session.Content.Character("staff-01"), none), Is.False, "owned ★5");
    }

    [Test]
    [Property("Requirement", "SHP-05")]
    [Property("Requirement", "SHP-09")]
    [Property("Requirement", "TST-09")]
    public void RerollCostsTwoThenOneMoreEachTimeAndResetsAtDusk()
    {
        var session = Rich();

        Assert.That(session.Shop.RerollCost, Is.EqualTo(2));
        Assert.That(session.Execute(new Reroll()).Succeeded, Is.True);
        Assert.That(session.Silver, Is.EqualTo(98));
        Assert.That(session.Shop.RerollCost, Is.EqualTo(3));
        Assert.That(session.Execute(new Reroll()).Succeeded, Is.True);
        Assert.That(session.Silver, Is.EqualTo(95));
        Assert.That(session.Shop.RerollCost, Is.EqualTo(4));
        Assert.That(session.Shop.Staff.All(o => o is not null), Is.True, "a reroll fills every slot (D-080)");

        PlayCycle(session);
        Assert.That(session.Shop.RerollCost, Is.EqualTo(2));
        Assert.That(session.Silver, Is.GreaterThanOrEqualTo(95 + session.Content.Balance.IncomePerCycle), "silver carries over");
    }

    [Test]
    [Property("Requirement", "SHP-05")]
    [Property("Requirement", "ITM-04")]
    [Property("Requirement", "TST-09")]
    public void RerollDiscountsNeverGoBelowZero()
    {
        var session = Rich(ContentWith(balance: b => b with { Shop = b.Shop with { RerollBase = 0 } }));
        session.SetShopForTests(session.RunForTests.Shop.Staff.ToList(), [new ItemOffer("loaded-coin", 0)]);

        Assert.That(session.Execute(new BuyOffer(ShopSection.Items, 0)).Succeeded, Is.True);

        Assert.That(session.Shop.RerollCost, Is.EqualTo(0), "0 - 1 is still 0");
        Assert.That(session.Execute(new Reroll()).Succeeded, Is.True);
        Assert.That(session.Silver, Is.EqualTo(100));
        Assert.That(session.Shop.RerollCost, Is.EqualTo(0), "0 + 1 - 1");
    }

    [Test]
    [Property("Requirement", "SHP-05")]
    [Property("Requirement", "SHP-01")]
    public void ARerollYouCannotAffordIsRefused()
    {
        var session = NewSession();
        session.RunForTests.Silver = 1;
        var before = session.Shop;

        Assert.That(session.Execute(new Reroll()).Failure, Is.EqualTo(CommandFailure.NotEnoughSilver));
        Assert.That(session.Silver, Is.EqualTo(1));
        Assert.That(session.Shop.Staff.Select(o => o?.Offer), Is.EqualTo(before.Staff.Select(o => o?.Offer)));
    }

    [Test]
    [Property("Requirement", "SHP-06")]
    [Property("Requirement", "TST-09")]
    public void ALockedShopKeepsItsOffersForOneDusk()
    {
        var session = NewSession();
        var offers = session.Shop.Staff.Select(o => o?.Offer).ToList();
        var items = session.Shop.Items.Select(o => o?.Offer).ToList();

        Assert.That(session.Execute(new LockShop(true)).Succeeded, Is.True);
        Assert.That(session.Shop.Locked, Is.True);
        PlayCycle(session);

        Assert.That(session.Shop.Staff.Select(o => o?.Offer), Is.EqualTo(offers));
        Assert.That(session.Shop.Items.Select(o => o?.Offer), Is.EqualTo(items));
        Assert.That(session.Shop.Locked, Is.False, "the lock lasts one Dusk");
    }

    [Test]
    [Property("Requirement", "STF-10")]
    [Property("Requirement", "SHP-08")]
    [Property("Requirement", "STF-06")]
    [Property("Requirement", "TST-09")]
    public void HiringGivesTheCharacterAtTheOfferRankWithItsPresetBuild()
    {
        var session = Rich();
        var offer = Offer(session, "staff-02", 3);
        Assert.That(offer.Price, Is.EqualTo(1 + 2 * 2), "base cost + 2 per rank above ★1");
        session.SetShopForTests([offer]);

        Assert.That(session.Execute(new BuyOffer(ShopSection.Staff, 0)).Succeeded, Is.True);

        var hired = Unit(session, "staff-02");
        Assert.That(hired.IsOnBench, Is.True);
        Assert.That(hired.Rank, Is.EqualTo(3));
        Assert.That(hired.Techniques, Is.EqualTo(new[] { "food", "company", "comfort" }), "preset: r2-company, r3-comfort");
        Assert.That(hired.UpgradePending, Is.False, "a hire needs no pick");
        Assert.That(session.Silver, Is.EqualTo(100 - 5));
        Assert.That(session.Shop.Staff[0], Is.Null, "the slot is empty until the next draw");
        Assert.That(session.DrainEvents().OfType<StaffHired>().Single().Rank, Is.EqualTo(3));
        Assert.That(session.Execute(new BuyOffer(ShopSection.Staff, 0)).Failure, Is.EqualTo(CommandFailure.UnknownOffer));
    }

    [Test]
    [Property("Requirement", "STF-06")]
    public void ARankMultipliesHealthAttackAndSoothe()
    {
        var session = Rich();
        session.SetShopForTests([Offer(session, "staff-10", 3)]);
        Assert.That(session.Execute(new BuyOffer(ShopSection.Staff, 0)).Succeeded, Is.True);

        var unit = session.UnitForTests(Unit(session, "staff-10").Id);
        var stats = session.Content.Character("staff-10").Stats;
        var multiplier = session.Content.Balance.RankStatMultipliers[2];
        Assert.That(unit.RankMultiplier, Is.EqualTo(multiplier));
        Assert.That(unit.MaxHealth, Is.EqualTo(stats.Health * multiplier).Within(1e-9));
        Assert.That(unit.Attack, Is.EqualTo(stats.Attack * multiplier).Within(1e-9));
        Assert.That(unit.Armour, Is.EqualTo(stats.Armour), "armour does not scale (D-081)");
        Assert.That(unit.Health, Is.EqualTo(unit.MaxHealth));
    }

    [Test]
    [Property("Requirement", "STF-08")]
    [Property("Requirement", "SHP-08")]
    [Property("Requirement", "UI-03")]
    [Property("Requirement", "UI-08")]
    [Property("Requirement", "TST-09")]
    public void BuyingAnOwnedCharacterRaisesItExactlyOneRankAtTheOfferPrice()
    {
        var session = Rich();
        var offer = Offer(session, "staff-01", 3);
        session.SetShopForTests([offer]);
        Assert.That(session.Shop.Staff[0]!.IsUpgrade, Is.True, "the Upgrade badge");

        Assert.That(session.Execute(new BuyOffer(ShopSection.Staff, 0)).Succeeded, Is.True);

        var unit = Unit(session, "staff-01");
        Assert.That(unit.Rank, Is.EqualTo(2), "one rank, whatever rank the offer shows");
        Assert.That(session.Silver, Is.EqualTo(100 - offer.Price), "the ★3 price for a single rank");
        Assert.That(unit.UpgradePending, Is.True);

        var tree = session.UpgradeTree(unit.Id);
        Assert.That(tree.Where(o => o.State == UpgradeOptionState.Available).Select(o => o.OptionId),
            Is.EqualTo(new[] { "r2-food", "r2-song", "r2-master-drink" }), "the dialog shows the rank 2 options");
    }

    [Test]
    [Property("Requirement", "STF-08")]
    [Property("Requirement", "STF-07")]
    [Property("Requirement", "STF-17")]
    [Property("Requirement", "TST-09")]
    public void APendingPickBlocksEverythingElseAndCanBranchIntoASecondClass()
    {
        var session = Rich();
        session.SetShopForTests([Offer(session, "staff-01", 1)]);
        session.Execute(new BuyOffer(ShopSection.Staff, 0));
        var id = Unit(session, "staff-01").Id;

        Assert.That(session.Execute(new Reroll()).Failure, Is.EqualTo(CommandFailure.UpgradePending));
        Assert.That(session.Execute(new BeginMist()).Failure, Is.EqualTo(CommandFailure.UpgradePending));
        Assert.That(session.Execute(new SetSpeed(2)).Failure, Is.EqualTo(CommandFailure.UpgradePending));
        Assert.That(session.Execute(new SetContentFilters(new ContentFilters(true, false))).Succeeded, Is.True, "filters always work");
        Assert.That(session.Execute(new ChooseUpgrade(id, "r3-company")).Failure, Is.EqualTo(CommandFailure.OptionNotAvailable), "not this rank");
        Assert.That(session.Execute(new ChooseUpgrade(id, "nothing")).Failure, Is.EqualTo(CommandFailure.OptionNotAvailable));
        session.DrainEvents();

        Assert.That(session.Execute(new ChooseUpgrade(id, "r2-song")).Succeeded, Is.True);

        var unit = Unit(session, "staff-01");
        Assert.That(unit.UpgradePending, Is.False);
        Assert.That(unit.Techniques, Is.EqualTo(new[] { "drink", "song" }));
        Assert.That(unit.Classes, Is.EqualTo(new[] { StaffClass.CompanionServer, StaffClass.Entertainer }), "a second class");
        Assert.That(session.DrainEvents().OfType<UnitUpgraded>().Single().OptionId, Is.EqualTo("r2-song"));
        Assert.That(session.Execute(new ChooseUpgrade(id, "r2-food")).Failure, Is.EqualTo(CommandFailure.InvalidValue), "no pick pending");
        Assert.That(session.UpgradeTree(id).Single(o => o.OptionId == "r2-song").State, Is.EqualTo(UpgradeOptionState.Chosen));
    }

    [Test]
    [Property("Requirement", "STF-17")]
    public void AMasteryPickStrengthensAKnownTechnique()
    {
        var session = Rich();
        session.SetShopForTests([Offer(session, "staff-01", 1)]);
        session.Execute(new BuyOffer(ShopSection.Staff, 0));
        var id = Unit(session, "staff-01").Id;

        Assert.That(session.Execute(new ChooseUpgrade(id, "r2-master-drink")).Succeeded, Is.True);

        var unit = session.UnitForTests(id);
        var drink = session.Content.Technique("drink");
        Assert.That(unit.IsMastered(drink), Is.True);
        Assert.That(unit.CapacityOf(drink), Is.EqualTo(drink.Capacity + 1));
        Assert.That(unit.SootheBonus(drink, "drowned-deep"), Is.EqualTo(1.5));
    }

    [Test]
    [Property("Requirement", "STF-08")]
    [Property("Requirement", "TST-09")]
    public void OffersOfAnOwnedFiveStarCannotBeBought()
    {
        var session = Rich();
        session.UnitForTests(Unit(session, "staff-01").Id).Rank = 5;
        session.SetShopForTests([Offer(session, "staff-01", 1)]);

        Assert.That(session.Execute(new BuyOffer(ShopSection.Staff, 0)).Failure, Is.EqualTo(CommandFailure.MaxRank));
        Assert.That(session.Silver, Is.EqualTo(100));
        Assert.That(session.Shop.Staff[0]!.CanBuy, Is.False);
    }

    [Test]
    [Property("Requirement", "SHP-01")]
    public void APurchaseThatWouldTakeSilverBelowZeroIsRefused()
    {
        var session = NewSession();
        session.RunForTests.Silver = 2;
        session.SetShopForTests([Offer(session, "staff-02", 2)], [new ItemOffer("lute", 3)]);

        Assert.That(session.Execute(new BuyOffer(ShopSection.Staff, 0)).Failure, Is.EqualTo(CommandFailure.NotEnoughSilver));
        Assert.That(session.Execute(new BuyOffer(ShopSection.Items, 0)).Failure, Is.EqualTo(CommandFailure.NotEnoughSilver));
        Assert.That(session.Silver, Is.EqualTo(2));
        Assert.That(session.Units.Any(u => u.StaffId == "staff-02"), Is.False);
        Assert.That(session.Items, Is.Empty);
    }

    [Test]
    [Property("Requirement", "STF-10")]
    public void HiringWithAFullBenchIsRefusedButUpgradesStillWork()
    {
        var session = Rich();
        foreach (var id in new[] { "staff-03", "staff-04", "staff-05", "staff-06" })
        {
            Assert.That(session.Execute(new DevAddStaff(id)).Succeeded, Is.True, id);
        }

        session.SetShopForTests([Offer(session, "staff-02", 1), Offer(session, "staff-01", 1)]);

        Assert.That(session.Execute(new BuyOffer(ShopSection.Staff, 0)).Failure, Is.EqualTo(CommandFailure.BenchFull));
        Assert.That(session.Execute(new BuyOffer(ShopSection.Staff, 1)).Succeeded, Is.True);
    }

    [Test]
    [Property("Requirement", "STF-09")]
    [Property("Requirement", "ITM-02")]
    [Property("Requirement", "TST-09")]
    public void SellingReturnsHalfOfAllTheSilverSpentAndKeepsTheItems()
    {
        var session = Rich();
        session.SetShopForTests([Offer(session, "staff-02", 2), Offer(session, "staff-02", 1)], [new ItemOffer("silk-scarf", 0)]);
        session.Execute(new BuyOffer(ShopSection.Staff, 0)); // 3 silver, ★2
        session.Execute(new BuyOffer(ShopSection.Staff, 1)); // 1 silver, ★3
        var id = Unit(session, "staff-02").Id;
        session.Execute(new ChooseUpgrade(id, "r3-comfort"));
        session.Execute(new BuyOffer(ShopSection.Items, 0));
        var scarf = session.Items.Single().Id;
        Assert.That(session.Execute(new EquipItem(scarf, id)).Succeeded, Is.True);
        var before = session.Silver;

        Assert.That(session.Execute(new SellUnit(id)).Succeeded, Is.True);

        Assert.That(session.Silver, Is.EqualTo(before + (3 + 1) / 2));
        Assert.That(session.Units.Any(u => u.StaffId == "staff-02"), Is.False);
        Assert.That(session.Items.Single().IsInStorage, Is.True, "its items go to storage");

        Assert.That(session.Execute(new SellUnit(Innkeeper(session).Id)).Failure, Is.EqualTo(CommandFailure.NotStaff));
        var start = session.Silver;
        Assert.That(session.Execute(new SellUnit(Unit(session, "staff-01").Id)).Succeeded, Is.True);
        Assert.That(session.Silver, Is.EqualTo(start), "a starting character counts as 1 silver spent; half of 1 is 0 (D-081)");
    }

    [Test]
    [Property("Requirement", "STF-09")]
    public void ASoldStaffMemberLeavesTheBoard()
    {
        var session = Rich();
        var staff = Unit(session, "staff-09");
        Assert.That(session.Execute(new PlaceUnit(staff.Id, new Tile(12, 8))).Succeeded, Is.True);

        Assert.That(session.Execute(new SellUnit(staff.Id)).Succeeded, Is.True);

        Assert.That(session.StaffOnBoard, Is.EqualTo(0));
        Assert.That(session.CheckPlaceUnit(Unit(session, "staff-01").Id, new Tile(12, 8)), Is.EqualTo(CommandFailure.None));
    }

    [Test]
    [Property("Requirement", "INV-02")]
    [Property("Requirement", "SHP-02")]
    public void ShoppingNeverChangesHowTheMistPlaysOut()
    {
        static List<string> Arrivals(bool shop)
        {
            var session = NewSession(seed: 7);
            session.RunForTests.Silver = 100;
            if (shop)
            {
                session.Execute(new Reroll());
                session.Execute(new Reroll());
            }

            session.DrainEvents();
            StartMist(session);
            session.RunMistToDawn();
            return session.DrainEvents().OfType<CreatureArrived>().Select(a => $"{a.CreatureDefId}@{a.At}").ToList();
        }

        Assert.That(Arrivals(shop: true), Is.EqualTo(Arrivals(shop: false)), "the shop draws from its own stream (D-080)");
    }

    [Test]
    [Property("Requirement", "INV-02")]
    [Property("Requirement", "SHP-02")]
    public void TheSameSeedDrawsTheSameShop()
    {
        var a = NewSession(seed: 3);
        var b = NewSession(seed: 3);

        Assert.That(a.Shop.Staff.Select(o => o?.Offer), Is.EqualTo(b.Shop.Staff.Select(o => o?.Offer)));
        Assert.That(a.Shop.Items.Select(o => o?.Offer), Is.EqualTo(b.Shop.Items.Select(o => o?.Offer)));
        Assert.That(a.ComputeStateHash(), Is.EqualTo(b.ComputeStateHash()));
    }
}
