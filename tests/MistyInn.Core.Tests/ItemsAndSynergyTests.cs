using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Randomness;
using MistyInn.Core.Run;
using static MistyInn.Core.Tests.TestGame;

namespace MistyInn.Core.Tests;

/// <summary>Items, inn items, adjacency, synergies and ancestry (sections 11.4, 12.2, 13.2, 13.3, TST-09).</summary>
public class ItemsAndSynergyTests
{
    private static readonly Tile RoomThree = new(21, 9);
    private static readonly Tile InFrontOfRoomThree = new(19, 9);

    /// <summary>Buys one item for free through the shop.</summary>
    private static ItemId Give(GameSession session, string itemId)
    {
        session.SetShopForTests(session.RunForTests.Shop.Staff.ToList(), [new ItemOffer(itemId, 0)]);
        Assert.That(session.Execute(new BuyOffer(ShopSection.Items, 0)).Failure, Is.EqualTo(CommandFailure.None), itemId);
        return session.Items.Last().Id;
    }

    private static UnitSnapshot Unit(GameSession session, string characterId) => session.Units.Single(u => u.StaffId == characterId);

    private static void Place(GameSession session, string characterId, Tile tile) =>
        Assert.That(session.Execute(new PlaceUnit(Unit(session, characterId).Id, tile)).Failure, Is.EqualTo(CommandFailure.None), characterId);

    [Test]
    [Property("Requirement", "ITM-02")]
    [Property("Requirement", "ITM-01")]
    [Property("Requirement", "TST-09")]
    public void StaffCarryTwoItemsAndStorageHoldsEight()
    {
        var session = NewSession();
        var fighter = Unit(session, "staff-09").Id;
        var a = Give(session, "brass-knuckles");
        var b = Give(session, "brass-knuckles");
        var c = Give(session, "lute");

        Assert.That(session.Execute(new EquipItem(a, fighter)).Succeeded, Is.True);
        Assert.That(session.Execute(new EquipItem(b, fighter)).Succeeded, Is.True);
        Assert.That(session.Execute(new EquipItem(c, fighter)).Failure, Is.EqualTo(CommandFailure.SlotsFull));
        Assert.That(session.Execute(new EquipItem(c, Innkeeper(session).Id)).Failure, Is.EqualTo(CommandFailure.NotStaff));
        Assert.That(Unit(session, "staff-09").ItemDefIds, Is.EqualTo(new[] { "brass-knuckles", "brass-knuckles" }));

        Assert.That(session.Execute(new EquipItem(b, Unit(session, "staff-01").Id)).Succeeded, Is.True, "items move freely at Dusk");
        Assert.That(Unit(session, "staff-09").Items, Is.EqualTo(new[] { a }));

        for (var i = 0; i < 7; i++)
        {
            Give(session, "lute");
        }

        Assert.That(session.Items.Count(i => i.IsInStorage), Is.EqualTo(8));
        session.SetShopForTests(session.RunForTests.Shop.Staff.ToList(), [new ItemOffer("lute", 0)]);
        Assert.That(session.Execute(new BuyOffer(ShopSection.Items, 0)).Failure, Is.EqualTo(CommandFailure.StorageFull));
        Assert.That(session.Execute(new UnequipItem(a)).Failure, Is.EqualTo(CommandFailure.StorageFull));
    }

    [Test]
    [Property("Requirement", "ITM-02")]
    public void ItemsCannotMoveDuringTheMist()
    {
        var session = NewSession();
        var item = Give(session, "lute");
        StartMist(session);

        Assert.That(session.Execute(new EquipItem(item, Unit(session, "staff-01").Id)).Failure, Is.EqualTo(CommandFailure.WrongPhase));
    }

    [Test]
    [Property("Requirement", "ITM-06")]
    public void BrassKnucklesAddAttackToFists()
    {
        var session = NewSession();
        var fighter = session.UnitForTests(Unit(session, "staff-09").Id);
        var fists = session.Content.Technique("fists");
        var before = fighter.AttackWith(fists);

        session.Execute(new EquipItem(Give(session, "brass-knuckles"), fighter.Id));

        Assert.That(fighter.AttackWith(fists), Is.EqualTo(before + 3));
        Assert.That(fighter.Attack, Is.EqualTo(6), "only Fists, not every attack");
    }

    [Test]
    [Property("Requirement", "ITM-06")]
    public void TheWardCharmGivesItsCarrierAnAura()
    {
        var session = NewSession();
        var carrier = session.UnitForTests(Unit(session, "staff-01").Id);

        session.Execute(new EquipItem(Give(session, "ward-charm"), carrier.Id));

        var aura = carrier.Effects.Single(e => e.Kind == EffectKind.Aura);
        Assert.That(aura.Radius, Is.EqualTo(2.0));
        Assert.That(aura.Multiplier, Is.EqualTo(0.9));
    }

    [Test]
    [Property("Requirement", "ITM-03")]
    [Property("Requirement", "STF-03")]
    [Property("Requirement", "SYN-01")]
    [Property("Requirement", "TST-09")]
    public void ClassTagsCountOnlyForSynergies()
    {
        var session = NewSession();
        Place(session, "staff-01", new Tile(12, 8));
        Place(session, "staff-09", new Tile(13, 8));
        Assert.That(session.Synergies.Single(s => s.SynergyId == "fighter").Count, Is.EqualTo(1));

        var knuckles = Give(session, "brass-knuckles");
        session.Execute(new EquipItem(knuckles, Unit(session, "staff-01").Id));

        var fighter = session.Synergies.Single(s => s.SynergyId == "fighter");
        Assert.That((fighter.Count, fighter.ActiveThreshold), Is.EqualTo((2, 2)), "the Fighter tag counts");
        var companion = Unit(session, "staff-01");
        Assert.That(companion.Classes, Is.EqualTo(new[] { StaffClass.CompanionServer }), "a tag is not a class");
        Assert.That(companion.Techniques, Is.EqualTo(new[] { "drink" }), "and gives no technique");

        session.Execute(new UnequipItem(knuckles));
        Assert.That(session.Synergies.Single(s => s.SynergyId == "fighter").IsActive, Is.False);
    }

    [Test]
    [Property("Requirement", "SYN-01")]
    [Property("Requirement", "SYN-02")]
    [Property("Requirement", "SYN-03")]
    [Property("Requirement", "UI-03")]
    [Property("Requirement", "TST-09")]
    public void SynergyThresholdsCountDistinctCharactersOnTheBoard()
    {
        // Staff 09 (Fighter, Hollow Wood), Staff 10 (Fighter, Mortal) and Staff 01 (Companion, Mortal).
        var session = NewSession(content: ContentWith(startingStaff: ["staff-09", "staff-10", "staff-01"]));
        Place(session, "staff-09", new Tile(12, 8));
        Place(session, "staff-10", new Tile(13, 8));
        Place(session, "staff-01", new Tile(14, 8));

        var fighter = session.Synergies.Single(s => s.SynergyId == "fighter");
        Assert.That((fighter.Count, fighter.ActiveThreshold, fighter.NextThreshold), Is.EqualTo((2, 2, (int?)4)));
        Assert.That(fighter.Thresholds, Is.EqualTo(new[] { 2, 4, 6 }));
        Assert.That(fighter.IsNearlyActive, Is.False);
        Assert.That(session.Synergies.Single(s => s.SynergyId == "mortal").ActiveThreshold, Is.EqualTo(2));
        var hollow = session.Synergies.Single(s => s.SynergyId == "hollow");
        Assert.That((hollow.Count, hollow.IsActive, hollow.IsNearlyActive), Is.EqualTo((1, false, true)), "nearly active");

        // Every staff member on the board gets the bonuses (D-077): Fighter +2 armour, Mortal +10 % health.
        Assert.That(Unit(session, "staff-01").Armour, Is.EqualTo(0 + 2));
        Assert.That(Unit(session, "staff-09").Armour, Is.EqualTo(2 + 2));
        Assert.That(Unit(session, "staff-01").MaxHealth, Is.EqualTo(18 * 1.1).Within(1e-9));

        Assert.That(session.Execute(new BenchUnit(Unit(session, "staff-10").Id)).Succeeded, Is.True);
        Assert.That(session.Synergies.Single(s => s.SynergyId == "fighter").Count, Is.EqualTo(1), "the bench does not count");
        Assert.That(Unit(session, "staff-01").Armour, Is.EqualTo(0));
        Assert.That(Unit(session, "staff-10").Armour, Is.EqualTo(session.Content.Character("staff-10").Stats.Armour), "nothing on the bench");
    }

    [Test]
    [Property("Requirement", "SYN-02")]
    public void GamblersAddToTributesAndEntertainersReachFurther()
    {
        var gamblers = NewSession(content: ContentWith(startingStaff: ["staff-05", "staff-06"]));
        Assert.That(gamblers.ModifiersForTests.InnWide(EffectKind.Tribute), Is.EqualTo(0));
        Place(gamblers, "staff-05", new Tile(12, 8));
        Place(gamblers, "staff-06", new Tile(13, 8));
        Assert.That(gamblers.ModifiersForTests.InnWide(EffectKind.Tribute), Is.EqualTo(1));

        var entertainers = NewSession(content: ContentWith(startingStaff: ["staff-13", "staff-14"]));
        Place(entertainers, "staff-13", new Tile(16, 6));
        Place(entertainers, "staff-14", new Tile(17, 7));
        var song = entertainers.Content.Technique("song");
        Assert.That(entertainers.UnitForTests(Unit(entertainers, "staff-13").Id).ExtraZones(song), Is.EqualTo(1));
    }

    [Test]
    [Property("Requirement", "ITM-04")]
    [Property("Requirement", "ITM-06")]
    [Property("Requirement", "BRD-01")]
    [Property("Requirement", "ART-17")]
    [Property("Requirement", "TST-09")]
    public void SpareKeysOpenTheThirdRoomAtOnce()
    {
        var session = NewSession();
        var staff = Unit(session, "staff-01").Id;
        Assert.That(session.Rooms.Select(r => r.Zone), Is.EqualTo(new[] { "room-1", "room-2" }));
        Assert.That(session.LockedRooms, Is.EquivalentTo(new[] { "room-3" }));
        Assert.That(session.CheckPlaceUnit(staff, RoomThree), Is.EqualTo(CommandFailure.TileBlocked), "nothing goes in a locked room");
        session.DrainEvents();

        var keys = Give(session, "spare-keys");

        Assert.That(session.Rooms.Select(r => r.Zone), Is.EqualTo(new[] { "room-1", "room-2", "room-3" }));
        Assert.That(session.LockedRooms, Is.Empty);
        Assert.That(session.CheckPlaceUnit(staff, RoomThree), Is.EqualTo(CommandFailure.None));
        var events = session.DrainEvents();
        Assert.That(events.OfType<InnItemAdded>().Single().DisplaySpot, Is.EqualTo("rooms"));
        Assert.That(session.Content.DisplaySpots.ContainsKey("rooms"), Is.True);
        Assert.That(events.OfType<RoomsChanged>().Single().OpenRooms, Has.Member("room-3"));

        Assert.That(session.Execute(new PlaceUnit(staff, RoomThree)).Succeeded, Is.True);
        Assert.That(session.Execute(new SellItem(keys)).Failure, Is.EqualTo(CommandFailure.TileOccupied), "someone is in the room");
        session.Execute(new BenchUnit(staff));
        Assert.That(session.Execute(new SellItem(keys)).Succeeded, Is.True);
        Assert.That(session.LockedRooms, Is.EquivalentTo(new[] { "room-3" }));
    }

    [Test]
    [Property("Requirement", "BRD-03")]
    [Property("Requirement", "BRD-01")]
    public void ALockedRoomIsLeftOutOfTheReachabilityRule()
    {
        var session = NewSession();
        var lantern = session.Fixtures.First(f => f.FixtureDefId == "lantern").Id;

        Assert.That(session.CheckPlaceFixture(lantern, InFrontOfRoomThree, 0), Is.EqualTo(CommandFailure.None), "room-3 is locked");

        Give(session, "spare-keys");
        Assert.That(session.CheckPlaceFixture(lantern, InFrontOfRoomThree, 0), Is.EqualTo(CommandFailure.TileBlocked), "now it must stay reachable");
    }

    [Test]
    [Property("Requirement", "ITM-04")]
    [Property("Requirement", "ITM-05")]
    [Property("Requirement", "ITM-06")]
    public void TheCursedCrownAddsABoardSlotAndHalvesTheInnkeeper()
    {
        var session = NewSession();
        var health = Innkeeper(session).MaxHealth;

        Give(session, "cursed-crown");

        Assert.That(session.MaxStaffOnBoard, Is.EqualTo(3 + 1));
        Assert.That(Innkeeper(session).MaxHealth, Is.EqualTo(health / 2));
        Assert.That(Innkeeper(session).Health, Is.EqualTo(health / 2));
    }

    [Test]
    [Property("Requirement", "ITM-04")]
    [Property("Requirement", "ITM-06")]
    public void InnItemsChangeTributesArrivalsAndFixtures()
    {
        var s = new Scenario();
        s.Place("hearth", new Tile(12, 7));
        bool HearthGivesLight() =>
            s.Session.SenseForTests.Working(EffectKind.Emits).Any(w => w.Fixture.Def.Id == "hearth" && w.Effect.Trigger == TriggerKind.Light);
        Assert.That(HearthGivesLight(), Is.False);

        Give(s.Session, "innkeepers-ledger");
        Give(s.Session, "drowned-bell");
        Give(s.Session, "ember-in-a-jar");

        var modifiers = s.Session.ModifiersForTests;
        Assert.That(modifiers.InnWide(EffectKind.Tribute), Is.EqualTo(1));
        Assert.That(modifiers.InnWide(EffectKind.ArrivalAgitation, "drowned-deep"), Is.EqualTo(-10));
        Assert.That(modifiers.InnWide(EffectKind.ArrivalAgitation, "ashen-waste"), Is.EqualTo(0));
        Assert.That(HearthGivesLight(), Is.True, "the hearth also counts as a lantern");
    }

    [Test]
    [Property("Requirement", "ITM-04")]
    [Property("Requirement", "TST-09")]
    public void AnOwnedInnItemIsNotOfferedAgainAndAtMostTwelveAreShown()
    {
        var session = NewSession();
        Give(session, "spare-keys");
        for (var i = 0; i < 40; i++)
        {
            session.ShopForTests.DrawAll();
            Assert.That(session.Shop.Items.Select(o => o?.Offer.ItemId), Has.None.EqualTo("spare-keys"));
        }

        var capped = NewSession(content: ContentWith(balance: b => b with { MaxInnItemsShown = 1 }));
        Give(capped, "drowned-bell");
        capped.SetShopForTests(capped.RunForTests.Shop.Staff.ToList(), [new ItemOffer("spare-keys", 0)]);
        Assert.That(capped.Execute(new BuyOffer(ShopSection.Items, 0)).Failure, Is.EqualTo(CommandFailure.InnItemsFull));
        Assert.That(session.Content.Balance.MaxInnItemsShown, Is.EqualTo(12));
    }

    [Test]
    [Property("Requirement", "ITM-07")]
    public void ItemsSellForHalfTheirPrice()
    {
        var session = NewSession();
        session.RunForTests.Silver = 10;
        session.SetShopForTests(session.RunForTests.Shop.Staff.ToList(), [new ItemOffer("lute", 3), new ItemOffer("loaded-coin", 5)]);
        session.Execute(new BuyOffer(ShopSection.Items, 0));
        session.Execute(new BuyOffer(ShopSection.Items, 1));

        foreach (var item in session.Items)
        {
            Assert.That(session.Execute(new SellItem(item.Id)).Succeeded, Is.True);
        }

        Assert.That(session.Silver, Is.EqualTo(10 - 3 - 5 + 1 + 2));
        Assert.That(session.Items, Is.Empty);
        Assert.That(session.Shop.RerollCost, Is.EqualTo(2), "a sold inn item stops working");
    }

    [Test]
    [Property("Requirement", "FIX-03")]
    [Property("Requirement", "FIX-04")]
    public void ALanternNextToTheStageAddsToItsReach()
    {
        var s = new Scenario();
        var stage = s.Place("stage", new Tile(16, 4));
        var lantern = s.Place("lantern", new Tile(18, 4));
        var sense = s.Session.SenseForTests;

        Assert.That(sense.AdjacentAmount(s.Session.FixtureForTests(stage), EffectKind.Radius), Is.EqualTo(1), "next to it, diagonals count");

        Assert.That(s.Session.Execute(new PlaceFixture(lantern, new Tile(12, 9), 0)).Succeeded, Is.True);
        Assert.That(sense.AdjacentAmount(s.Session.FixtureForTests(stage), EffectKind.Radius), Is.EqualTo(0));
        Assert.That(sense.Working(EffectKind.Radius), Is.Empty, "an adjacency effect works only while its rule holds");
    }

    [Test]
    [Property("Requirement", "FIX-03")]
    [Property("Requirement", "CMP-05")]
    public void AWardCircleByARoomDoorShieldsTheSessionInside()
    {
        var s = new Scenario();
        s.Place("ward-circle", new Tile(10, 8));
        s.Begin();

        var rooms = s.Session.RunForTests.Rooms;
        Assert.That(rooms.Single(r => r.Zone == "room-1").Shields, Is.EqualTo(1));
        Assert.That(rooms.Single(r => r.Zone == "room-2").Shields, Is.EqualTo(0));
    }

    [Test]
    [Property("Requirement", "STF-16")]
    [Property("Requirement", "INV-12")]
    [Property("Requirement", "TST-09")]
    public void FightingACreatureOfItsAncestralPlaneRevealsTheAncestry()
    {
        // Staff 10 has a hidden Ashen Waste ancestry; Creature 03 comes from the Ashen Waste.
        var s = new Scenario(startingStaff: ["staff-10"]);
        s.PlaceStaff("staff-10", new Tile(12, 8));
        s.Begin();
        Assert.That(s.Staff("staff-10").RevealedAncestry, Is.Null, "shown as ? until revealed");
        s.SpawnTurned("creature-03", new Tile(13, 9));

        s.RunUntilCollecting(() => s.Session.Ancestry.IsRevealed("staff-10"), 10);

        Assert.That(s.Events.OfType<AncestryRevealed>().Single().PlaneId, Is.EqualTo("ashen-waste"));
        Assert.That(s.Session.CycleLogEntries.Single(e => e.Kind == "ancestry.revealed").Cause, Is.EqualTo("plane.ashen-waste"));
        Assert.That(s.Staff("staff-10").RevealedAncestry, Is.EqualTo("ashen-waste"));
    }

    [Test]
    [Property("Requirement", "STF-16")]
    [Property("Requirement", "TST-09")]
    public void ARevealedAncestrySoothesItsPlaneBetter()
    {
        var s = new Scenario();
        s.PlaceStaff("staff-01", new Tile(12, 8));
        s.Begin();
        var pale = s.Creature(s.Spawn("creature-09", new Tile(12, 10)));
        var drowned = s.Creature(s.Spawn("creature-01", new Tile(13, 10)));
        var unit = s.Unit("staff-01");
        var drink = s.Content.Technique("drink");
        var service = s.Session.ServiceForTests;
        var paleBefore = service.SootheRate(unit, drink, pale);
        var drownedBefore = service.SootheRate(unit, drink, drowned);

        s.Session.Ancestry.Reveal("staff-01");

        Assert.That(service.SootheRate(unit, drink, pale), Is.EqualTo(paleBefore * 1.25).Within(1e-9));
        Assert.That(service.SootheRate(unit, drink, drowned), Is.EqualTo(drownedBefore).Within(1e-9));
    }

    [Test]
    [Property("Requirement", "STF-16")]
    [Property("Requirement", "SYN-01")]
    public void ARevealedAncestryIsKeptAcrossRunsAndCountsAsAnOrigin()
    {
        var content = ContentWith(startingStaff: ["staff-01", "staff-15"]);
        GameSession NewRun(AncestryRecord record)
        {
            var session = new GameSession(new SeededRandom(1), content: content, ancestry: record);
            Place(session, "staff-01", new Tile(12, 8));
            Place(session, "staff-15", new Tile(13, 8));
            return session;
        }

        var hidden = NewRun(new AncestryRecord());
        Assert.That(hidden.Synergies.Single(s => s.SynergyId == "pale").Count, Is.EqualTo(1), "a hidden ancestry is not an origin");

        var record = new AncestryRecord(["staff-01"]);
        var next = NewRun(record);
        Assert.That(next.Ancestry, Is.SameAs(record));
        Assert.That(Unit(next, "staff-01").RevealedAncestry, Is.EqualTo("pale-court"));
        Assert.That(next.Synergies.Single(s => s.SynergyId == "pale").ActiveThreshold, Is.EqualTo(2));
    }
}
