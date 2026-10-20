using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;

namespace MistyInn.Core.Tests.Board;

public class InnBoardTests
{
    private static InnBoard NewBoard() => new(GameContent.Default.Layout);

    [TestCase(11, 12, true, TestName = "Entrance floor")]
    [TestCase(7, 5, true, TestName = "Kitchen floor")]
    [TestCase(13, 13, true, TestName = "In front of the door (staff may stand there)")]
    [TestCase(1, 1, false, TestName = "Clearing outside")]
    [TestCase(5, 4, false, TestName = "Wall")]
    [TestCase(13, 14, false, TestName = "Front door")]
    [TestCase(0, 10, false, TestName = "Mist gate")]
    [Property("Requirement", "BRD-02")]
    public void StaffStandOnlyOnTheInnFloor(int x, int y, bool placeable)
    {
        Assert.That(NewBoard().IsPlaceableFloor(new Tile(x, y)), Is.EqualTo(placeable));
    }

    [TestCase(13, 14, true)]
    [TestCase(13, 13, true)]
    [TestCase(13, 12, true)]
    [TestCase(12, 12, false)]
    [TestCase(1, 1, true)]
    [Property("Requirement", "BRD-05")]
    public void FixturesStayOffTheDoorApproach(int x, int y, bool forbidden)
    {
        Assert.That(NewBoard().IsFixtureForbidden(new Tile(x, y)), Is.EqualTo(forbidden));
    }

    [TestCase(13, 13, true, TestName = "The tile in front of the door")]
    [TestCase(13, 14, true, TestName = "The door itself")]
    [TestCase(19, 11, true, TestName = "The only cellar-door tile")]
    [TestCase(7, 5, false, TestName = "A kitchen corner")]
    [TestCase(5, 4, false, TestName = "A wall is never in the way")]
    [Property("Requirement", "BRD-03")]
    public void BlockingTheWayInIsDetected(int x, int y, bool blocks)
    {
        Assert.That(NewBoard().WouldBlockPath(new Tile(x, y)), Is.EqualTo(blocks));
    }

    [Test]
    [Property("Requirement", "BRD-02")]
    public void StaffDoNotBlockMovement()
    {
        var board = NewBoard();
        var tile = new Tile(13, 13);

        board.PutUnit(new UnitId(1), tile);

        Assert.That(board.IsWalkable(tile), Is.True);
        Assert.That(board.UnitAt(tile), Is.EqualTo(new UnitId(1)));
        Assert.That(board.FindPath(new Tile(13, 0), new Tile(12, 9)), Is.Not.Null);
    }

    [Test]
    [Property("Requirement", "INV-02")]
    public void WalkableTilesComeInMapOrder()
    {
        var tiles = NewBoard().WalkableTilesIn(new[] { "entrance" });

        Assert.That(tiles, Has.Count.EqualTo(10));
        Assert.That(tiles, Is.Ordered.By(nameof(Tile.Y)).Then.By(nameof(Tile.X)));
    }
}
