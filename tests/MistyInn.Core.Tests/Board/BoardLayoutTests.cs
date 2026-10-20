using MistyInn.Core.Board;
using MistyInn.Core.Content;

namespace MistyInn.Core.Tests.Board;

public class BoardLayoutTests
{
    private static BoardLayout Layout => DefaultContent.Create().Layout;

    [Test]
    [Property("Requirement", "BRD-01")]
    [Property("Requirement", "PLN-04")]
    public void DefaultLayoutHasItsDoorGatesAndZones()
    {
        var layout = Layout;

        Assert.That(layout.Width, Is.EqualTo(26));
        Assert.That(layout.Height, Is.EqualTo(20));
        Assert.That(layout.FrontDoor, Is.EqualTo(new Tile(13, 14)));
        Assert.That(layout.Gates, Is.EqualTo(new[]
        {
            new Tile(13, 0), new Tile(0, 10), new Tile(25, 10), new Tile(0, 19), new Tile(25, 19),
        }));
        Assert.That(layout.ZoneAt(new Tile(11, 12)), Is.EqualTo("entrance"));
        Assert.That(layout.ZoneAt(new Tile(7, 5)), Is.EqualTo("kitchen"));
        Assert.That(layout.ZoneAt(new Tile(7, 9)), Is.EqualTo("rooms"));
        Assert.That(layout.ZoneIds, Is.SupersetOf(new[]
        {
            "entrance", "common-room", "bar", "kitchen", "lounge", "stage", "rooms", "cellar-door",
        }));
    }

    [Test]
    [Property("Requirement", "BRD-01")]
    public void InnFloorIsFourteenByTen()
    {
        var floor = Layout.AllTiles().Where(t => Layout.KindAt(t) == TileKind.Floor).ToList();

        Assert.That(floor.Min(t => t.X), Is.EqualTo(6));
        Assert.That(floor.Max(t => t.X), Is.EqualTo(19));
        Assert.That(floor.Min(t => t.Y), Is.EqualTo(4));
        Assert.That(floor.Max(t => t.Y), Is.EqualTo(13));
    }

    [Test]
    [Property("Requirement", "BRD-05")]
    public void DoorApproachIsTheDoorAndTheTwoTilesInFront()
    {
        Assert.That(Layout.DoorApproach, Is.EqualTo(new[] { new Tile(13, 14), new Tile(13, 13), new Tile(13, 12) }));
    }

    [Test]
    [Property("Requirement", "BRD-01")]
    public void TilesOffTheMapAreWalls()
    {
        Assert.That(Layout.KindAt(new Tile(-1, 0)), Is.EqualTo(TileKind.Wall));
        Assert.That(Layout.KindAt(new Tile(0, 20)), Is.EqualTo(TileKind.Wall));
        Assert.That(Layout.ZoneAt(new Tile(99, 99)), Is.Empty);
    }

    [TestCase("..D|..", TestName = "Ragged rows")]
    [TestCase("..D|.?.", TestName = "Unknown character")]
    [TestCase("...|...", TestName = "No door")]
    [TestCase(".D.|.D.", TestName = "Two doors")]
    [TestCase("#D#|###", TestName = "Door with no floor beside it")]
    [Property("Requirement", "BRD-01")]
    public void BrokenLayoutsAreRejected(string map)
    {
        var rows = map.Split('|');
        var legend = new Dictionary<char, LegendEntry>
        {
            ['.'] = new(TileKind.Floor, "floor"),
            ['#'] = new(TileKind.Wall, ""),
            ['D'] = new(TileKind.Door, "door"),
        };

        Assert.That(() => BoardLayout.Parse(rows, legend), Throws.TypeOf<FormatException>());
    }
}
