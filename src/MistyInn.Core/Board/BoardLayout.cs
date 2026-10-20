namespace MistyInn.Core.Board;

/// <summary>How one character of a layout map is read.</summary>
public readonly record struct LegendEntry(TileKind Kind, string Zone);

/// <summary>
/// The fixed shape of the board: the inn, its zones, the clearing around it and the mist gates (BRD-01, PLN-04).
/// Parsed from a character map; from step 3 the map comes from <c>data/inn.json</c> (SIM-04).
/// </summary>
public sealed class BoardLayout
{
    private readonly TileKind[] _kinds;
    private readonly string[] _zones;

    private BoardLayout(int width, int height, TileKind[] kinds, string[] zones)
    {
        Width = width;
        Height = height;
        _kinds = kinds;
        _zones = zones;

        var doors = AllTiles().Where(t => KindAt(t) == TileKind.Door).ToList();
        if (doors.Count != 1)
        {
            throw new FormatException($"A layout needs exactly one front door; found {doors.Count}.");
        }

        FrontDoor = doors[0];
        DoorApproach = FindDoorApproach(FrontDoor);
        Gates = AllTiles().Where(t => KindAt(t) == TileKind.Gate).ToList().AsReadOnly();
        ZoneIds = AllTiles()
            .Where(t => IsWalkableKind(KindAt(t)))
            .Select(ZoneAt)
            .Where(z => z.Length > 0)
            .Distinct()
            .ToList()
            .AsReadOnly();
        Rooms = FindRooms();
    }

    public int Width { get; }

    public int Height { get; }

    /// <summary>The only entrance (BRD-05).</summary>
    public Tile FrontDoor { get; }

    /// <summary>The door tile and the two floor tiles in front of it, inside; fixtures may not go there (BRD-05).</summary>
    public IReadOnlyList<Tile> DoorApproach { get; }

    /// <summary>The mist gates, in map order (top to bottom, left to right).</summary>
    public IReadOnlyList<Tile> Gates { get; }

    /// <summary>Every zone that has at least one walkable tile, in map order.</summary>
    public IReadOnlyList<string> ZoneIds { get; }

    /// <summary>The back rooms, each its own zone with one door, in map order of their doors (BRD-01, BRD-03).</summary>
    public IReadOnlyList<RoomLayout> Rooms { get; }

    public bool Contains(Tile tile) => tile.X >= 0 && tile.Y >= 0 && tile.X < Width && tile.Y < Height;

    /// <summary>The kind of a tile; anything off the map counts as wall.</summary>
    public TileKind KindAt(Tile tile) => Contains(tile) ? _kinds[Index(tile)] : TileKind.Wall;

    /// <summary>The zone of a tile, or an empty string (walls, off the map).</summary>
    public string ZoneAt(Tile tile) => Contains(tile) ? _zones[Index(tile)] : "";

    public IEnumerable<Tile> AllTiles()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                yield return new Tile(x, y);
            }
        }
    }

    public static bool IsWalkableKind(TileKind kind) =>
        kind is TileKind.Floor or TileKind.Door or TileKind.Outside or TileKind.Gate or TileKind.RoomDoor;

    public static BoardLayout Parse(IReadOnlyList<string> rows, IReadOnlyDictionary<char, LegendEntry> legend)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(legend);

        if (rows.Count == 0 || rows[0].Length == 0)
        {
            throw new FormatException("A layout needs at least one row and one column.");
        }

        var width = rows[0].Length;
        var height = rows.Count;
        var kinds = new TileKind[width * height];
        var zones = new string[width * height];

        for (var y = 0; y < height; y++)
        {
            if (rows[y].Length != width)
            {
                throw new FormatException($"Row {y} is {rows[y].Length} wide; every row must be {width} wide.");
            }

            for (var x = 0; x < width; x++)
            {
                var c = rows[y][x];
                if (!legend.TryGetValue(c, out var entry))
                {
                    throw new FormatException($"Unknown layout character '{c}' at ({x},{y}).");
                }

                kinds[y * width + x] = entry.Kind;
                zones[y * width + x] = entry.Zone;
            }
        }

        return new BoardLayout(width, height, kinds, zones);
    }

    private int Index(Tile tile) => tile.Y * Width + tile.X;

    private IReadOnlyList<RoomLayout> FindRooms()
    {
        var rooms = new List<RoomLayout>();
        foreach (var door in AllTiles().Where(t => KindAt(t) == TileKind.RoomDoor))
        {
            var zone = ZoneAt(door);
            if (zone.Length == 0)
            {
                throw new FormatException($"The room door at {door} has no zone.");
            }

            if (rooms.Any(r => r.Zone == zone))
            {
                throw new FormatException($"Room '{zone}' has more than one door.");
            }

            var tiles = AllTiles().Where(t => KindAt(t) == TileKind.Floor && ZoneAt(t) == zone).ToList();
            if (tiles.Count == 0)
            {
                throw new FormatException($"Room '{zone}' has a door at {door} but no floor.");
            }

            rooms.Add(new RoomLayout(zone, door, tiles.AsReadOnly()));
        }

        return rooms.AsReadOnly();
    }

    private IReadOnlyList<Tile> FindDoorApproach(Tile door)
    {
        (int Dx, int Dy)[] directions = [(0, -1), (1, 0), (0, 1), (-1, 0)];
        foreach (var (dx, dy) in directions)
        {
            var first = new Tile(door.X + dx, door.Y + dy);
            if (KindAt(first) != TileKind.Floor)
            {
                continue;
            }

            var approach = new List<Tile> { door, first };
            var second = new Tile(door.X + 2 * dx, door.Y + 2 * dy);
            if (KindAt(second) == TileKind.Floor)
            {
                approach.Add(second);
            }

            return approach.AsReadOnly();
        }

        throw new FormatException($"The front door at {door} has no floor tile next to it.");
    }
}

/// <summary>A back room (BRD-01): its zone, its door and its floor tiles. Company sessions happen inside (CMP-04).</summary>
public sealed record RoomLayout(string Zone, Tile Door, IReadOnlyList<Tile> Tiles);
