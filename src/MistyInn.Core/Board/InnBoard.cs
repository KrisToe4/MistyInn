using MistyInn.Core.Entities;

namespace MistyInn.Core.Board;

/// <summary>
/// The live board: the layout plus what stands on it. Staff stand on floor tiles, one per tile, and do not block
/// movement (D-043). Fixtures cover their tiles and block movement (FIX-01), so a fixture placement must leave a path
/// from the door to every zone (BRD-03).
/// </summary>
public sealed class InnBoard
{
    private readonly HashSet<Tile> _blocked = new();
    private readonly Dictionary<Tile, UnitId> _units = new();
    private readonly Dictionary<Tile, FixtureId> _fixtures = new();
    private readonly HashSet<string> _lockedZones = new();

    public InnBoard(BoardLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        Layout = layout;
    }

    public BoardLayout Layout { get; }

    public bool IsWalkable(Tile tile) => BoardLayout.IsWalkableKind(Layout.KindAt(tile)) && !_blocked.Contains(tile);

    /// <summary>True for inn floor tiles that nothing blocks: where staff may stand (BRD-02).</summary>
    public bool IsPlaceableFloor(Tile tile) =>
        Layout.KindAt(tile) == TileKind.Floor && !_blocked.Contains(tile) && !_lockedZones.Contains(Layout.ZoneAt(tile));

    /// <summary>The back rooms not in use yet (D-078): shut, and left out of the reachability rule.</summary>
    public IReadOnlySet<string> LockedZones => _lockedZones;

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

        return WouldBlockPath(new[] { tile });
    }

    /// <summary>True if blocking all of <paramref name="tiles"/> would cut some zone off from the front door (BRD-03).</summary>
    public bool WouldBlockPath(IReadOnlyCollection<Tile> tiles)
    {
        var blocking = tiles.Where(IsWalkable).ToList();
        if (blocking.Count == 0)
        {
            return false;
        }

        if (blocking.Contains(Layout.FrontDoor))
        {
            return true;
        }

        var reachedZones = ZonesReachableFromDoor(blocking);
        return Layout.ZoneIds.Any(zone => !reachedZones.Contains(zone) && !_lockedZones.Contains(zone));
    }

    /// <summary>The zones a creature could reach from the front door, optionally with more tiles blocked.</summary>
    public IReadOnlySet<string> ZonesReachableFromDoor(IReadOnlyCollection<Tile>? alsoBlocked = null)
    {
        var reachedZones = new HashSet<string>();
        if (!IsWalkable(Layout.FrontDoor) || alsoBlocked?.Contains(Layout.FrontDoor) == true)
        {
            return reachedZones;
        }

        var seen = new HashSet<Tile> { Layout.FrontDoor };
        if (alsoBlocked is not null)
        {
            seen.UnionWith(alsoBlocked);
        }

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

        return reachedZones;
    }

    /// <summary>Walkable tiles in the given zones, in map order (deterministic for random picks).</summary>
    public IReadOnlyList<Tile> WalkableTilesIn(IReadOnlyCollection<string> zones) =>
        Layout.AllTiles().Where(t => IsWalkable(t) && zones.Contains(Layout.ZoneAt(t))).ToList().AsReadOnly();

    public IReadOnlyList<Tile>? FindPath(Tile from, Tile to) =>
        Pathfinder.FindPath(Layout.Width, Layout.Height, IsWalkable, from, to);

    public FixtureId? FixtureAt(Tile tile) => _fixtures.TryGetValue(tile, out var id) ? id : null;

    /// <summary>
    /// The free floor tiles next to a fixture's tiles (four neighbours, in map order), up to <paramref name="count"/>:
    /// its seats (FIX-01).
    /// </summary>
    public IReadOnlyList<Tile> SeatsAround(IReadOnlyList<Tile> fixtureTiles, int count) =>
        fixtureTiles
            .SelectMany(Neighbours4)
            .Distinct()
            .Where(t => IsPlaceableFloor(t) && !fixtureTiles.Contains(t))
            .OrderBy(t => t.Y)
            .ThenBy(t => t.X)
            .Take(count)
            .ToList()
            .AsReadOnly();

    internal void PutFixture(FixtureId fixture, IEnumerable<Tile> tiles)
    {
        foreach (var tile in tiles)
        {
            _fixtures[tile] = fixture;
            _blocked.Add(tile);
        }
    }

    internal void RemoveFixture(IEnumerable<Tile> tiles)
    {
        foreach (var tile in tiles)
        {
            _fixtures.Remove(tile);
            _blocked.Remove(tile);
        }
    }

    internal void PutUnit(UnitId unit, Tile tile) => _units[tile] = unit;

    internal void RemoveUnit(Tile tile) => _units.Remove(tile);

    internal void Block(Tile tile) => _blocked.Add(tile);

    /// <summary>Shuts a room that is not in use: its door blocks movement and nothing may be placed inside (D-078).</summary>
    internal void LockRoom(RoomLayout room)
    {
        if (_lockedZones.Add(room.Zone))
        {
            _blocked.Add(room.Door);
        }
    }

    internal void UnlockRoom(RoomLayout room)
    {
        if (_lockedZones.Remove(room.Zone))
        {
            _blocked.Remove(room.Door);
        }
    }

    internal void Unblock(Tile tile) => _blocked.Remove(tile);

    private static IEnumerable<Tile> Neighbours4(Tile t)
    {
        yield return new Tile(t.X, t.Y - 1);
        yield return new Tile(t.X + 1, t.Y);
        yield return new Tile(t.X, t.Y + 1);
        yield return new Tile(t.X - 1, t.Y);
    }
}
