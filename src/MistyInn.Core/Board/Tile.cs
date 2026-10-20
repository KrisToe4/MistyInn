namespace MistyInn.Core.Board;

/// <summary>A cell of the board grid. X grows east, Y grows south (row order of the layout).</summary>
public readonly record struct Tile(int X, int Y)
{
    public override string ToString() => $"({X},{Y})";
}

/// <summary>What a tile is made of.</summary>
public enum TileKind
{
    /// <summary>The clearing around the inn: walkable, but staff and fixtures never go there.</summary>
    Outside,

    /// <summary>The inn's floor: walkable, and where staff and fixtures are placed.</summary>
    Floor,

    Wall,

    /// <summary>The front door: the only way in (BRD-05).</summary>
    Door,

    /// <summary>A mist gate at the edge of the clearing (PLN-04): walkable, outside.</summary>
    Gate,

    /// <summary>
    /// The door of a back room (BRD-01, CMP-04): walkable while the room is free, closed during a session. Its zone is
    /// the room's zone. Nothing is placed on it.
    /// </summary>
    RoomDoor,
}
