using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using static MistyInn.Core.Tests.TestGame;

namespace MistyInn.Core.Tests;

/// <summary>The build menu, fixture placement, silver and supplies (FIX-01, FIX-05, FIX-06, BRD-02 to BRD-05, SHP-01).</summary>
public class BuildTests
{
    private static FixtureId Stored(GameSession session, string defId) =>
        session.Fixtures.First(f => f.FixtureDefId == defId && f.IsInStorage).Id;

    [Test]
    [Property("Requirement", "RUN-01")]
    [Property("Requirement", "FIX-05")]
    public void TheRunStartsWithItsKitInStorage()
    {
        var session = NewSession();

        Assert.That(session.Silver, Is.EqualTo(10));
        Assert.That(session.Supplies[Supply.Drink], Is.EqualTo(6));
        Assert.That(session.Supplies[Supply.Food], Is.EqualTo(4));
        Assert.That(session.Fixtures.Select(f => f.FixtureDefId),
            Is.EqualTo(new[] { "bar-counter", "table", "table", "hearth", "lantern" }));
        Assert.That(session.Fixtures.All(f => f.IsInStorage), Is.True);
    }

    [Test]
    [Property("Requirement", "FIX-05")]
    [Property("Requirement", "SHP-01")]
    public void BuildingCostsSilverAndNeverGoesBelowZero()
    {
        var session = NewSession();

        Assert.That(session.Execute(new BuildFixture("stage")).Succeeded, Is.True);
        Assert.That(session.Execute(new BuildFixture("stage")).Succeeded, Is.True);
        Assert.That(session.Silver, Is.Zero);
        Assert.That(session.Execute(new BuildFixture("lantern")).Failure, Is.EqualTo(CommandFailure.NotEnoughSilver));
        Assert.That(session.Execute(new BuildFixture("bonfire")).Failure, Is.EqualTo(CommandFailure.UnknownFixture));
        Assert.That(session.Fixtures.Count(f => f.FixtureDefId == "stage"), Is.EqualTo(2));
    }

    [Test]
    [Property("Requirement", "FIX-06")]
    public void SuppliesComeInBundles()
    {
        var session = NewSession();

        Assert.That(session.Execute(new BuySupplies(Supply.Drink, 1)).Succeeded, Is.True);

        Assert.That(session.Supplies[Supply.Drink], Is.EqualTo(10));
        Assert.That(session.Silver, Is.EqualTo(8));
        Assert.That(session.Execute(new BuySupplies(Supply.Food, 0)).Failure, Is.EqualTo(CommandFailure.InvalidValue));
        Assert.That(session.Execute(new BuySupplies(Supply.Food, 5)).Failure, Is.EqualTo(CommandFailure.NotEnoughSilver));
    }

    [Test]
    [Property("Requirement", "FIX-01")]
    [Property("Requirement", "BRD-02")]
    public void AFixtureCoversItsRotatedShape()
    {
        var session = NewSession();
        var hearth = Stored(session, "hearth");

        Assert.That(session.Execute(new PlaceFixture(hearth, new Tile(12, 7), 1)).Succeeded, Is.True);

        var placed = session.Fixtures.Single(f => f.Id == hearth);
        Assert.That(placed.Tiles, Is.EquivalentTo(new[] { new Tile(12, 7), new Tile(12, 8) }));
        Assert.That(session.Execute(new PlaceFixture(hearth, new Tile(12, 7), 4)).Failure, Is.EqualTo(CommandFailure.InvalidValue));
    }

    [Test]
    [Property("Requirement", "FIX-01")]
    public void SomeFixturesBelongToAZone()
    {
        var session = NewSession();

        Assert.That(session.Execute(new PlaceFixture(Stored(session, "bar-counter"), new Tile(11, 7), 0)).Failure,
            Is.EqualTo(CommandFailure.WrongZone));
        Assert.That(session.Execute(new PlaceFixture(Stored(session, "bar-counter"), new Tile(11, 4), 0)).Succeeded, Is.True);
    }

    [Test]
    [Property("Requirement", "BRD-05")]
    [Property("Requirement", "BRD-03")]
    public void FixturesStayOffTheDoorApproachAndNeverCutOffAZone()
    {
        var session = NewSession();
        var lantern = Stored(session, "lantern");

        Assert.That(session.Execute(new PlaceFixture(lantern, new Tile(13, 13), 0)).Failure, Is.EqualTo(CommandFailure.TileBlocked));
        Assert.That(session.Execute(new PlaceFixture(lantern, new Tile(19, 11), 0)).Failure, Is.EqualTo(CommandFailure.TileBlocked),
            "the only cellar-door tile");
        Assert.That(session.Execute(new PlaceFixture(lantern, new Tile(1, 1), 0)).Failure, Is.EqualTo(CommandFailure.TileBlocked),
            "outside the inn");
    }

    [Test]
    [Property("Requirement", "BRD-02")]
    [Property("Requirement", "FIX-01")]
    public void FixturesAndStaffNeverShareATile()
    {
        var session = NewSession();
        var lantern = Stored(session, "lantern");
        session.Execute(new PlaceUnit(Staff(session)[0].Id, new Tile(14, 9)));

        Assert.That(session.Execute(new PlaceFixture(lantern, new Tile(14, 9), 0)).Failure, Is.EqualTo(CommandFailure.TileOccupied));
        Assert.That(session.Execute(new PlaceFixture(lantern, new Tile(12, 9), 0)).Succeeded, Is.True);
        Assert.That(session.Execute(new PlaceFixture(Stored(session, "table"), new Tile(12, 9), 0)).Failure,
            Is.EqualTo(CommandFailure.TileOccupied));
        Assert.That(session.Execute(new PlaceUnit(Staff(session)[1].Id, new Tile(12, 9))).Failure, Is.EqualTo(CommandFailure.TileBlocked));
    }

    [Test]
    [Property("Requirement", "FIX-01")]
    public void FixturesBlockMovement()
    {
        var session = NewSession();
        session.Execute(new PlaceFixture(Stored(session, "lantern"), new Tile(12, 9), 0));

        var board = new InnBoard(session.Layout);
        Assert.That(board.IsWalkable(new Tile(12, 9)), Is.True, "a fresh board");
        Assert.That(session.Fixtures.Single(f => f.FixtureDefId == "lantern").Tiles, Is.EqualTo(new[] { new Tile(12, 9) }));
        Assert.That(session.CheckPlaceUnit(Staff(session)[0].Id, new Tile(12, 9)), Is.EqualTo(CommandFailure.TileBlocked));
    }

    [Test]
    [Property("Requirement", "BRD-04")]
    [Property("Requirement", "FIX-05")]
    public void FixturesMoveStoreAndSellForHalf()
    {
        var session = NewSession();
        var hearth = Stored(session, "hearth");
        session.Execute(new PlaceFixture(hearth, new Tile(12, 7), 0));

        Assert.That(session.Execute(new PlaceFixture(hearth, new Tile(13, 7), 0)).Succeeded, Is.True, "overlapping its own old place");
        Assert.That(session.Execute(new StoreFixture(hearth)).Succeeded, Is.True);
        Assert.That(session.Fixtures.Single(f => f.Id == hearth).IsInStorage, Is.True);

        session.DrainEvents();
        Assert.That(session.Execute(new SellFixture(hearth)).Succeeded, Is.True);
        Assert.That(session.Silver, Is.EqualTo(12), "the hearth cost 4");
        Assert.That(session.Fixtures.Any(f => f.Id == hearth), Is.False);
        Assert.That(session.DrainEvents().OfType<FixtureSold>().Single().Silver, Is.EqualTo(2));
    }

    [Test]
    [Property("Requirement", "BRD-04")]
    public void PlacedFixturesStayFromCycleToCycle()
    {
        var session = NewSession();
        session.Execute(new PlaceFixture(Stored(session, "hearth"), new Tile(12, 7), 0));

        PlayCycle(session);

        Assert.That(session.Fixtures.Single(f => f.FixtureDefId == "hearth").Origin, Is.EqualTo(new Tile(12, 7)));
    }

    [Test]
    [Property("Requirement", "ARC-11")]
    public void TheBuildMenuIsClosedDuringTheMist()
    {
        var session = NewSession();
        StartMist(session);

        Assert.That(session.Execute(new BuildFixture("lantern")).Failure, Is.EqualTo(CommandFailure.WrongPhase));
        Assert.That(session.Execute(new BuySupplies(Supply.Drink, 1)).Failure, Is.EqualTo(CommandFailure.WrongPhase));
        Assert.That(session.CheckPlaceFixture(Stored(session, "lantern"), new Tile(12, 9), 0), Is.EqualTo(CommandFailure.WrongPhase));
    }
}
