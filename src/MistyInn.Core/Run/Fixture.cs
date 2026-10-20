using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;

namespace MistyInn.Core.Run;

/// <summary>A fixture the player owns: in storage, or placed on the board (FIX-01, FIX-05). Mutable; only Core changes it.</summary>
internal sealed class Fixture
{
    public Fixture(FixtureId id, FixtureDef def)
    {
        Id = id;
        Def = def;
        Health = def.Health;
    }

    public FixtureId Id { get; }

    public FixtureDef Def { get; }

    /// <summary>The origin tile when placed; null in storage.</summary>
    public Tile? Origin { get; private set; }

    /// <summary>Quarter turns clockwise, 0 to 3.</summary>
    public int Rotation { get; private set; }

    /// <summary>The tiles it covers when placed; empty in storage.</summary>
    public IReadOnlyList<Tile> Tiles { get; private set; } = Array.Empty<Tile>();

    public double Health { get; set; }

    /// <summary>A destroyed fixture stays in place, blocking, but does nothing until repaired (BRD-04, ESC-10).</summary>
    public bool IsBroken => Health <= 0;

    public bool IsPlaced => Origin is not null;

    /// <summary>The fixture's seats on the current board (FIX-01), computed when it is placed.</summary>
    public IReadOnlyList<Tile> Seats { get; set; } = Array.Empty<Tile>();

    public void Place(Tile origin, int rotation)
    {
        Origin = origin;
        Rotation = rotation;
        Tiles = TilesAt(Def, origin, rotation);
    }

    public void Store()
    {
        Origin = null;
        Rotation = 0;
        Tiles = Array.Empty<Tile>();
        Seats = Array.Empty<Tile>();
    }

    /// <summary>The tiles a fixture would cover at <paramref name="origin"/>, turned <paramref name="rotation"/> quarter turns.</summary>
    public static IReadOnlyList<Tile> TilesAt(FixtureDef def, Tile origin, int rotation) =>
        def.Shape.Select(o => Rotate(o[0], o[1], rotation)).Select(o => new Tile(origin.X + o.X, origin.Y + o.Y)).ToList();

    private static (int X, int Y) Rotate(int x, int y, int quarterTurns) => (((quarterTurns % 4) + 4) % 4) switch
    {
        0 => (x, y),
        1 => (-y, x),
        2 => (-x, -y),
        _ => (y, -x),
    };

    public FixtureSnapshot Snapshot() => new(Id, Def.Id, Def.Name, Origin, Rotation, Tiles, Health, Def.Health, IsBroken);
}

/// <summary>A read-only view of a fixture (ARC-03). <see cref="Origin"/> is null while it is in storage.</summary>
public sealed record FixtureSnapshot(
    FixtureId Id,
    string FixtureDefId,
    string Name,
    Tile? Origin,
    int Rotation,
    IReadOnlyList<Tile> Tiles,
    double Health,
    double MaxHealth,
    bool IsBroken)
{
    public bool IsInStorage => Origin is null;
}
