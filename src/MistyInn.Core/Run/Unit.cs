using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;

namespace MistyInn.Core.Run;

/// <summary>A staff member or the innkeeper, on the board or on the bench. Mutable; only Core changes it.</summary>
internal sealed class Unit
{
    public Unit(UnitId id, UnitTemplate def)
    {
        Id = id;
        Def = def;
    }

    public UnitId Id { get; }

    public UnitTemplate Def { get; }

    /// <summary>The tile it stands on, or null when it is on the bench.</summary>
    public Tile? Tile { get; set; }

    public UnitSnapshot Snapshot() => new(Id, Def.Id, Def.Name, Def.IsInnkeeper, Tile);
}

/// <summary>A read-only view of a unit for the presentation (ARC-03).</summary>
public sealed record UnitSnapshot(UnitId Id, string StaffId, string Name, bool IsInnkeeper, Tile? Tile)
{
    public bool IsOnBench => Tile is null;
}
