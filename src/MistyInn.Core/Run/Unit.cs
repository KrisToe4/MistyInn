using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;

namespace MistyInn.Core.Run;

/// <summary>A staff member or the innkeeper, on the board or on the bench. Mutable; only Core changes it.</summary>
internal sealed class Unit
{
    public Unit(UnitId id, UnitTemplate def, IReadOnlyList<TechniqueDef> techniques)
    {
        Id = id;
        Def = def;
        Techniques = techniques;
    }

    public UnitId Id { get; }

    public UnitTemplate Def { get; }

    public StatsDef Stats => Def.Stats;

    /// <summary>What it knows, in the order it learned them (STF-02).</summary>
    public IReadOnlyList<TechniqueDef> Techniques { get; }

    /// <summary>The tile it stands on, or null when it is on the bench.</summary>
    public Tile? Tile { get; set; }

    /// <summary>The service it is giving in the mist, if any (STF-04).</summary>
    public ServiceSession? Session { get; set; }

    public UnitSnapshot Snapshot() => new(
        Id, Def.Id, Def.Name, Def.IsInnkeeper, Tile,
        Techniques.Select(t => t.Id).ToList(),
        Session?.Technique.Id,
        Session?.Targets.ToList() ?? (IReadOnlyList<CreatureId>)Array.Empty<CreatureId>());
}

/// <summary>
/// One service session (STF-04): a technique given to up to its capacity of creatures until <see cref="EndsAt"/>.
/// </summary>
internal sealed class ServiceSession
{
    public ServiceSession(TechniqueDef technique, IReadOnlyList<CreatureId> targets, double endsAt)
    {
        Technique = technique;
        Targets = targets.ToList();
        EndsAt = endsAt;
    }

    public TechniqueDef Technique { get; }

    public List<CreatureId> Targets { get; }

    public double EndsAt { get; }
}

/// <summary>A read-only view of a unit for the presentation (ARC-03).</summary>
public sealed record UnitSnapshot(
    UnitId Id,
    string StaffId,
    string Name,
    bool IsInnkeeper,
    Tile? Tile,
    IReadOnlyList<string> Techniques,
    string? Serving,
    IReadOnlyList<CreatureId> ServingCreatures)
{
    public bool IsOnBench => Tile is null;
}
