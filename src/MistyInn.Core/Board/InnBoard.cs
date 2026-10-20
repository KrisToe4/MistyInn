using MistyInn.Core.Entities;

namespace MistyInn.Core.Board;

/// <summary>
/// The live board: the layout plus what stands on it. Staff stand on floor tiles, one per tile, and do not block
/// movement (D-043). Blocking placements (barricades, from step 4) must leave a path from the door to every zone
/// (BRD-03).
/// </summary>
public sealed class InnBoard
{
    private readonly HashSet<Tile> _blocked = new();
    private readonly Dictionary<Tile, UnitId> _units = new();

    public InnBoard(BoardLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        Layout = layout;
    }

    public BoardLayout Layout { get; }

    public bool IsWalkable(Tile tile) => BoardLayout.IsWalkableKind(Layout.KindAt(tile)) && !_blocked.Contains(tile);

    /// <summary>True for inn floor tiles that nothing blocks: where staff may stand (BRD-02).</summary>
    public bool IsPlaceableFloor(Tile tile) => Layout.KindAt(tile) == TileKind.Floor && !_blocked.Contains(tile);

    /// <summary>Fixtures never go off the floor, on the door, or on the two tiles in front of it (BRD-05).</summary>
    public bool IsFixtureForbidden(Tile tile) =>
        Layout.KindAt(tile) != TileKind.Floor || Layout.DoorApproach.Contains(tile);

    public UnitId? UnitAt(Tile tile) => _units.TryGetValue(tile, out var id) ? id : null;

    /// <summary>
    /// True if blocking <paramref name="tile"/> would cut some zone off from the front door (BRD-03). Movement allows
    /// diagonals only past open corners, so four-way connectivity is the same as what the pathfinder can reach.
    /// </summary>
    public bool WouldBlockPath(Tile tile)
    {
        if (!IsWalkable(tile))
        {
            return false;
        }

        if (tile == Layout.FrontDoor)
        {
            return true;
        }

        var reachedZones = new HashSet<string>();
        var seen = new HashSet<Tile> { Layout.FrontDoor, tile };
        var queue = new Queue<Tile>();
        queue.Enqueue(Layout.FrontDoor);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            reachedZones.Add(Layout.ZoneAt(current));

            foreach (var next in Neighbours4(current))
            {
                if (IsWalkable(next) && seen.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return Layout.ZoneIds.Any(zone => !reachedZones.Contains(zone));
    }

    /// <summary>Walkable tiles in the given zones, in map order (deterministic for random picks).</summary>
    public IReadOnlyList<Tile> WalkableTilesIn(IReadOnlyCollection<string> zones) =>
        Layout.AllTiles().Where(t => IsWalkable(t) && zones.Contains(Layout.ZoneAt(t))).ToList().AsReadOnly();

    public IReadOnlyList<Tile>? FindPath(Tile from, Tile to) =>
        Pathfinder.FindPath(Layout.Width, Layout.Height, IsWalkable, from, to);

    internal void PutUnit(UnitId unit, Tile tile) => _units[tile] = unit;

    internal void RemoveUnit(Tile tile) => _units.Remove(tile);

    internal void Block(Tile tile) => _blocked.Add(tile);

    internal void Unblock(Tile tile) => _blocked.Remove(tile);

    private static IEnumerable<Tile> Neighbours4(Tile t)
    {
        yield return new Tile(t.X, t.Y - 1);
        yield return new Tile(t.X + 1, t.Y);
        yield return new Tile(t.X, t.Y + 1);
        yield return new Tile(t.X - 1, t.Y);
    }
}
