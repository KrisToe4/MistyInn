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
    private readonly List<TechniqueDef> _techniques;

    public Unit(UnitId id, UnitTemplate def, IEnumerable<TechniqueDef> techniques, double rankMultiplier)
    {
        Id = id;
        Def = def;
        _techniques = techniques.ToList();
        RankMultiplier = rankMultiplier;
        Health = MaxHealth;
    }

    public UnitId Id { get; }

    public UnitTemplate Def { get; }

    public StatsDef Stats => Def.Stats;

    /// <summary>★1 to ★5 (STF-06). The innkeeper stays at ★1.</summary>
    public int Rank { get; set; } = 1;

    /// <summary>The rank's multiplier for health, attack and soothe (STF-06, D-081).</summary>
    public double RankMultiplier { get; set; }

    /// <summary>The upgrade options picked, in rank order from ★2 (STF-07, STF-10).</summary>
    public List<string> Chosen { get; } = new();

    /// <summary>The technique IDs it has mastered (STF-17).</summary>
    public HashSet<string> Mastered { get; } = new();

    /// <summary>Silver spent on it: the hire and every upgrade; half comes back when it is sold (STF-09).</summary>
    public int Spent { get; set; }

    /// <summary>Raised a rank by an upgrade offer and waiting for <c>ChooseUpgrade</c> (STF-08).</summary>
    public bool UpgradePending { get; set; }

    /// <summary>
    /// The effects that apply to it, from carried items, active synergies and inn items (ITM-02, ITM-04, SYN-03); set by
    /// <see cref="Simulation.Modifiers"/>.
    /// </summary>
    public IReadOnlyList<EffectDef> Effects { get; set; } = Array.Empty<EffectDef>();

    public double MaxHealth =>
        Stats.Health * RankMultiplier * StatEffects(StatKind.Health, null).Aggregate(1.0, (m, e) => m * (e.Multiplier ?? 1))
        + StatEffects(StatKind.Health, null).Sum(e => e.Amount ?? 0);

    /// <summary>Attack with no technique (the innkeeper striking back, ESC-06).</summary>
    public double Attack => Stats.Attack * RankMultiplier + StatEffects(StatKind.Attack, null).Sum(e => e.Amount ?? 0);

    public double Armour => Stats.Armour + StatEffects(StatKind.Armour, null).Sum(e => e.Amount ?? 0);

    /// <summary>What it knows, in the order it learned them (STF-02).</summary>
    public IReadOnlyList<TechniqueDef> Techniques => _techniques;

    /// <summary>Its classes, from the techniques it knows (STF-03); item tags are counted only by synergies.</summary>
    public IReadOnlyList<StaffClass> Classes => _techniques.Select(t => t.Class).Distinct().Order().ToList();

    public bool IsMastered(TechniqueDef technique) => Mastered.Contains(technique.Id);

    public void Learn(TechniqueDef technique) => _techniques.Add(technique);

    /// <summary>Attack with a Fighter technique: its bonus, its mastery and items for it (STF-02, STF-17, ITM-06).</summary>
    public double AttackWith(TechniqueDef technique) =>
        Attack + technique.AttackBonus + (IsMastered(technique) ? technique.Mastery.AttackBonus : 0)
        + StatEffects(StatKind.Attack, technique.Id).Sum(e => e.Amount ?? 0);

    /// <summary>How many creatures one session can serve (AGI-07), with its mastery and items.</summary>
    public int CapacityOf(TechniqueDef technique) =>
        technique.Capacity + (IsMastered(technique) ? technique.Mastery.CapacityBonus : 0)
        + (int)StatEffects(StatKind.Capacity, technique.Id).Sum(e => e.Amount ?? 0);

    public double RangeOf(TechniqueDef technique) =>
        technique.Range + (IsMastered(technique) ? technique.Mastery.RangeBonus : 0);

    /// <summary>
    /// The soothe multiplier from its mastery, items and synergies for a creature of <paramref name="plane"/> (STF-17,
    /// ITM-06, SYN-02). The rank and the ancestry bonus are applied by the service rules.
    /// </summary>
    public double SootheBonus(TechniqueDef technique, string plane) =>
        (IsMastered(technique) ? technique.Mastery.SootheMultiplier : 1)
        * Effects.Where(e => e.Kind == EffectKind.Soothe && (e.Technique is null || e.Technique == technique.Id) && (e.Plane is null || e.Plane == plane))
            .Aggregate(1.0, (m, e) => m * (e.Multiplier ?? 1))
        * Effects.Where(e => e.Kind == EffectKind.Stat && e.Stat == StatKind.Soothe && (e.Technique is null || e.Technique == technique.Id))
            .Aggregate(1.0, (m, e) => m * (e.Multiplier ?? 1));

    /// <summary>Extra zones a technique reaches from beside the stage (the Entertainer synergy, SYN-02).</summary>
    public int ExtraZones(TechniqueDef technique) =>
        (int)Effects.Where(e => e.Kind == EffectKind.Radius && e.Technique == technique.Id).Sum(e => e.Amount ?? 0);

    /// <summary>Stat effects for one stat: general ones, plus those for <paramref name="techniqueId"/> when given.</summary>
    private IEnumerable<EffectDef> StatEffects(StatKind stat, string? techniqueId) =>
        Effects.Where(e => e.Kind == EffectKind.Stat && e.Stat == stat && e.Technique == techniqueId);

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

    public UnitSnapshot Snapshot(IReadOnlyList<OwnedItem> carried, string? revealedAncestry) => new(
        Id, Def.Id, Def.Name, Def.IsInnkeeper, Tile,
        Techniques.Select(t => t.Id).ToList(),
        Session?.Technique.Id,
        Session?.Targets.ToList() ?? (IReadOnlyList<CreatureId>)Array.Empty<CreatureId>(),
        Position,
        PreviousPosition,
        Health,
        MaxHealth,
        Activity,
        Shaken,
        Rank,
        Classes,
        Mastered.Order().ToList(),
        UpgradePending,
        carried.Select(i => i.Id).ToList(),
        carried.Select(i => i.Def.Id).ToList(),
        revealedAncestry,
        Attack,
        Armour);
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
    bool Shaken,
    int Rank,
    IReadOnlyList<StaffClass> Classes,
    IReadOnlyList<string> Mastered,
    bool UpgradePending,
    IReadOnlyList<ItemId> Items,
    IReadOnlyList<string> ItemDefIds,
    string? RevealedAncestry,
    double Attack,
    double Armour)
{
    public bool IsOnBench => Tile is null;

    /// <summary>Shown on the board: placed, and not inside a back room (CMP-04).</summary>
    public bool IsVisible => Tile is not null && Activity != UnitActivity.InRoom;
}
