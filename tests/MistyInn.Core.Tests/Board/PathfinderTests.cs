using MistyInn.Core.Board;
using MistyInn.Core.Content;

namespace MistyInn.Core.Tests.Board;

public class PathfinderTests
{
    private static Func<Tile, bool> OpenExcept(params Tile[] walls) => t => !walls.Contains(t);

    [Test]
    [Property("Requirement", "SIM-03")]
    public void DiagonalsMakeTheShortestPath()
    {
        var path = Pathfinder.FindPath(5, 5, _ => true, new Tile(0, 0), new Tile(4, 4));

        Assert.That(path, Is.EqualTo(new[]
        {
            new Tile(0, 0), new Tile(1, 1), new Tile(2, 2), new Tile(3, 3), new Tile(4, 4),
        }));
    }

    [Test]
    [Property("Requirement", "SIM-03")]
    public void PathsNeverCutAWallCorner()
    {
        // A wall at (1,0): going from (0,0) to (1,1) diagonally would clip it.
        var path = Pathfinder.FindPath(2, 2, OpenExcept(new Tile(1, 0)), new Tile(0, 0), new Tile(1, 1));

        Assert.That(path, Is.EqualTo(new[] { new Tile(0, 0), new Tile(0, 1), new Tile(1, 1) }));
    }

    [Test]
    [Property("Requirement", "SIM-03")]
    public void UnreachableGoalHasNoPath()
    {
        var walls = new[] { new Tile(1, 0), new Tile(1, 1), new Tile(1, 2) };

        Assert.That(Pathfinder.FindPath(3, 3, OpenExcept(walls), new Tile(0, 0), new Tile(2, 2)), Is.Null);
        Assert.That(Pathfinder.FindPath(3, 3, OpenExcept(walls), new Tile(0, 0), new Tile(1, 1)), Is.Null,
            "a wall is never a goal");
        Assert.That(Pathfinder.FindPath(3, 3, _ => true, new Tile(0, 0), new Tile(5, 5)), Is.Null, "off the map");
    }

    [Test]
    [Property("Requirement", "SIM-03")]
    public void StartingOnTheGoalIsAOneTilePath()
    {
        Assert.That(Pathfinder.FindPath(3, 3, _ => true, new Tile(1, 1), new Tile(1, 1)), Is.EqualTo(new[] { new Tile(1, 1) }));
    }

    [Test]
    [Property("Requirement", "INV-02")]
    public void SameQueryGivesTheSamePath()
    {
        var board = new InnBoard(DefaultContent.Create().Layout);

        var first = board.FindPath(new Tile(0, 19), new Tile(18, 9));
        var second = board.FindPath(new Tile(0, 19), new Tile(18, 9));

        Assert.That(first, Is.Not.Null);
        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    [Property("Requirement", "PLN-04")]
    [Property("Requirement", "CRE-04")]
    [Property("Requirement", "BRD-05")]
    public void EveryGateLeadsInThroughTheFrontDoor()
    {
        var layout = DefaultContent.Create().Layout;
        var board = new InnBoard(layout);

        foreach (var gate in layout.Gates)
        {
            var path = board.FindPath(gate, new Tile(12, 9));

            Assert.That(path, Is.Not.Null, $"from {gate}");
            var tiles = path!.ToList();
            var firstInside = tiles.FindIndex(t => layout.KindAt(t) == TileKind.Floor);
            Assert.That(tiles[firstInside - 1], Is.EqualTo(layout.FrontDoor), $"from {gate}");
        }
    }
}
