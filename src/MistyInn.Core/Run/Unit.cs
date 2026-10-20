using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;

namespace MistyInn.Core.Run;

/// <summary>What a unit is doing in the mist (STF-04, ESC-07, CMP-04).</summary>
public enum UnitActivity
{
    /// <summary>At its post, free.</summary>
    AtPost,

    /// <summary>At its post, giving a service session.</summary>
    Serving,

    /// <summary>A fighter going for, or striking, a turned creature (ESC-07).</summary>
    Fighting,

    /// <summary>Walking back to its post.</summary>
    Returning,

    /// <summary>A companion walking a guest to a back room (CMP-04).</summary>
    GoingToRoom,

    /// <summary>In a back room with a guest: off the board and out of play (CMP-04).</summary>
    InRoom,

    /// <summary>A companion's rest after a session, out of play (CMP-04).</summary>
    Resting,

    /// <summary>Reduced to 0 health; on the floor until dawn (ESC-09).</summary>
    Fallen,
}

/// <summary>A staff member or the innkeeper, on the board or on the bench. Mutable; only Core changes it.</summary>
internal sealed class Unit
{
    private readonly Queue<Tile> _path = new();

    public Unit(UnitId id, UnitTemplate def, IReadOnlyList<TechniqueDef> techniques, double rankMultiplier)
    {
        Id = id;
        Def = def;
        Techniques = techniques;
        RankMultiplier = rankMultiplier;
        Health = MaxHealth;
    }

    public UnitId Id { get; }

    public UnitTemplate Def { get; }

    public StatsDef Stats => Def.Stats;

    /// <summary>The rank's stat multiplier (STF-06); ★1 until step 6 (D-065).</summary>
    public double RankMultiplier { get; }

    public double MaxHealth => Stats.Health * RankMultiplier;

    public double Attack => Stats.Attack * RankMultiplier;

    /// <summary>What it knows, in the order it learned them (STF-02).</summary>
    public IReadOnlyList<TechniqueDef> Techniques { get; }

    /// <summary>Its first Fighter technique, if it has one (ESC-07).</summary>
    public TechniqueDef? FighterTechnique => Techniques.FirstOrDefault(t => t.EngagesTurned);

    /// <summary>Its post: the tile it was placed on, or null when it is on the bench.</summary>
    public Tile? Tile { get; set; }

    /// <summary>Where it is in the mist (SIM-03); at its post unless it fights or goes to a room.</summary>
    public TilePosition Position { get; set; }

    /// <summary>The position before the last step, for interpolation (SIM-03).</summary>
    public TilePosition PreviousPosition { get; set; }

    public double Health { get; set; }

    public UnitActivity Activity { get; set; } = UnitActivity.AtPost;

    public bool IsFallen => Activity == UnitActivity.Fallen;

    /// <summary>On the floor and free to serve: at its post, idle or serving (STF-04).</summary>
    public bool IsAvailable => Tile is not null && Activity is UnitActivity.AtPost or UnitActivity.Serving;

    /// <summary>Fell last cycle and survived: −20 % soothe this cycle (STF-12).</summary>
    public bool Shaken { get; set; }

    /// <summary>Fell in this mist; rolled for at Dawn (ESC-09).</summary>
    public bool FellThisCycle { get; set; }

    /// <summary>The turned creature a fighter is engaging (ESC-07).</summary>
    public CreatureId? Engaging { get; set; }

    public double NextAttackAt { get; set; }

    public double RepathAt { get; set; }

    /// <summary>When a companion's rest ends (CMP-04).</summary>
    public double RestUntil { get; set; }

    /// <summary>The service it is giving in the mist, if any (STF-04).</summary>
    public ServiceSession? Session { get; set; }

    /// <summary>The tiles still to walk, in order.</summary>
    public IReadOnlyCollection<Tile> Path => _path;

    public void SetPath(IReadOnlyList<Tile> path) => Walking.SetPath(_path, path);

    public void ClearPath() => _path.Clear();

    public void Advance(double distance) => Position = Walking.Advance(Position, _path, distance);

    /// <summary>Back at its post, free, with nothing pending; for Dusk and the start of the mist.</summary>
    public void ResetToPost()
    {
        Position = Tile is { } post ? TilePosition.Center(post) : default;
        PreviousPosition = Position;
        _path.Clear();
        Activity = UnitActivity.AtPost;
        Engaging = null;
        Session = null;
        NextAttackAt = 0;
        RepathAt = 0;
        RestUntil = 0;
    }

    public UnitSnapshot Snapshot() => new(
        Id, Def.Id, Def.Name, Def.IsInnkeeper, Tile,
        Techniques.Select(t => t.Id).ToList(),
        Session?.Technique.Id,
        Session?.Targets.ToList() ?? (IReadOnlyList<CreatureId>)Array.Empty<CreatureId>(),
        Position,
        PreviousPosition,
        Health,
        MaxHealth,
        Activity,
        Shaken);
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
    IReadOnlyList<CreatureId> ServingCreatures,
    TilePosition Position,
    TilePosition PreviousPosition,
    double Health,
    double MaxHealth,
    UnitActivity Activity,
    bool Shaken)
{
    public bool IsOnBench => Tile is null;

    /// <summary>Shown on the board: placed, and not inside a back room (CMP-04).</summary>
    public bool IsVisible => Tile is not null && Activity != UnitActivity.InRoom;
}
