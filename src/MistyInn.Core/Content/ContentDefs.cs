using MistyInn.Core.Board;

namespace MistyInn.Core.Content;

// The shape of the data/*.json files (DAT-01, DAT-02). Property names are camelCase in JSON; enums are their names.
// Required lists may come back null from a broken file; the validator reports that instead of crashing (DAT-04).

/// <summary>Everything read from <c>data/</c>, before validation.</summary>
public sealed record ContentSet(
    BalanceDef Balance,
    InnDef Inn,
    IReadOnlyList<PlaneDef> Planes,
    IReadOnlyList<CreatureDef> Creatures,
    IReadOnlyList<TechniqueDef> Techniques,
    StaffFileDef Staff,
    IReadOnlyList<ItemDef> Items,
    IReadOnlyList<FixtureDef> Fixtures,
    IReadOnlyList<FlavourDef> Flavour);

/// <summary><c>balance.json</c>: the tuning values of section 6.4 and the run's starting set-up.</summary>
/// <param name="PlanesPerCycle">Converging planes for cycles 1, 2, 3...; the last entry repeats; 0 means all unlocked.</param>
/// <param name="RankStatMultipliers">Base-stat multipliers for ★1 to ★5 (STF-06).</param>
public sealed record BalanceDef(
    int RunLength,
    double MistDurationSeconds,
    double ArrivalStopBeforeDawnSeconds,
    double BaseArrivalIntervalSeconds,
    double ArrivalIntervalPerCycle,
    double ConvergenceArrivalMultiplier,
    double ArrivalJitterMin,
    double ArrivalJitterMax,
    int MaxCreatures,
    int BaseStaffSlots,
    int MaxStaffSlots,
    int BenchSize,
    IReadOnlyList<int> PlanesPerCycle,
    double IdleMinSeconds,
    double IdleMaxSeconds,
    int StartingSilver,
    int IncomePerCycle,
    int Integrity,
    int Rooms,
    IReadOnlyList<string> StartingStaff,
    IReadOnlyList<string> UnlockedPlanes,
    IReadOnlyList<string> WanderZones,
    IReadOnlyList<double> RankStatMultipliers,
    double AncestrySoothe,
    int StaffItemSlots,
    int ItemStorage,
    int MaxInnItemsShown,
    ShopDef Shop);

/// <summary>The shop's tuning (section 13.1).</summary>
public sealed record ShopDef(
    int StaffOffers,
    int ItemOffers,
    int RerollBase,
    int RerollStep,
    double PityWeight,
    double HomePlaneWeight,
    int RankPriceStep,
    IReadOnlyList<RankOddsDef> RankOdds);

/// <summary>Rank odds (percent for ★1 to ★5) from a cycle onward (SHP-03).</summary>
public sealed record RankOddsDef(int FromCycle, IReadOnlyList<int> Odds);

/// <summary><c>inn.json</c>: the board map and how to read it (BRD-01, SIM-04). Legend keys are single characters.</summary>
public sealed record InnDef(IReadOnlyList<string> Rows, IReadOnlyDictionary<string, LegendDef> Legend);

public sealed record LegendDef(TileKind Kind, string Zone);

/// <summary>A plane (PLN-01). Planes take the gates in map order (PLN-04).</summary>
public sealed record PlaneDef(
    string Id,
    string Name,
    string MistColor,
    string Ambience,
    IReadOnlyList<CreatureWeightDef> Creatures,
    IReadOnlyList<EffectDef>? Rules = null);

public sealed record CreatureWeightDef(string Creature, double Weight);

/// <summary>A creature (CRE-03). <paramref name="Speed"/> is in tiles per second.</summary>
public sealed record CreatureDef(
    string Id,
    string Name,
    string PlaneId,
    int Size,
    bool Sapient,
    bool Adult,
    double Speed,
    IReadOnlyList<AppetiteDef> Appetites,
    IReadOnlyList<TriggerDef> Triggers,
    AgitationDef Agitation,
    CombatDef Combat,
    EscalationDef Escalation,
    double Tribute);

/// <summary>An appetite with its strength and an optional preferred technique ID (AGI-05).</summary>
public sealed record AppetiteDef(Appetite Appetite, AppetiteStrength Strength, string? PreferredTechnique = null);

/// <summary>A trigger (AGI-08): an agitation spike when it starts and a rate multiplier while it holds.</summary>
public sealed record TriggerDef(TriggerKind Trigger, double Spike = 0, double RateMultiplier = 1, string? Plane = null, int? Count = null);

public sealed record AgitationDef(double Arrival, double RatePerSecond);

public sealed record CombatDef(double Health, double Attack, double AttackInterval, double Range, double Armour);

/// <summary>An escalation profile (ESC-02).</summary>
public sealed record EscalationDef(IReadOnlyList<EscalationStage>? Skip = null, double WreckSeconds = 10, double PreySeconds = 15);

/// <summary>
/// <c>techniques.json</c>: one entry per technique (STF-02, STF-17). <paramref name="NeedsFixture"/> means it works only
/// at a fixture with an <see cref="EffectKind.Enables"/> effect for it (Drink at the bar counter, Dice at any table).
/// </summary>
public sealed record TechniqueDef(
    string Id,
    Technique Technique,
    string Name,
    StaffClass Class,
    IReadOnlyList<Appetite> Answers,
    double Soothe,
    int Capacity,
    double Range,
    MasteryDef Mastery,
    bool NeedsFixture = false,
    Supply? Supply = null,
    bool EngagesTurned = false,
    double AttackBonus = 0,
    double AttackIntervalMultiplier = 1);

/// <summary>What mastering a technique adds (STF-17).</summary>
public sealed record MasteryDef(double SootheMultiplier = 1, int CapacityBonus = 0, double RangeBonus = 0, double AttackBonus = 0);

/// <summary><c>staff.json</c>: the innkeeper and the roster of characters (STF-01, STF-14).</summary>
public sealed record StaffFileDef(InnkeeperDef Innkeeper, IReadOnlyList<StaffDef> Characters);

/// <summary>The innkeeper: a unit the player places, with no class or tree (ESC-06).</summary>
public sealed record InnkeeperDef(string Id, string Name, bool Adult, StatsDef Stats);

/// <summary>A staff character (STF-01). <paramref name="Origin"/> is a plane ID or <c>mortal-realm</c>.</summary>
public sealed record StaffDef(
    string Id,
    string Name,
    bool Adult,
    string Origin,
    string? Ancestry,
    int BaseCost,
    StaffClass MainClass,
    StaffClass? SecondClass,
    string BaseTechnique,
    StatsDef Stats,
    IReadOnlyList<TreeRankDef> Tree,
    IReadOnlyList<PresetDef> Presets);

public sealed record StatsDef(
    double Health,
    double Attack,
    double AttackInterval,
    double Range,
    double Armour,
    double Speed,
    double Soothe);

/// <summary>The options for one rank of an upgrade tree (STF-07).</summary>
public sealed record TreeRankDef(int Rank, IReadOnlyList<TreeOptionDef> Options);

/// <summary>
/// One upgrade option: a new technique, or the mastery of a known one (STF-17). <paramref name="Id"/> is unique within
/// the character; <paramref name="Requires"/> names an option of an earlier rank.
/// </summary>
public sealed record TreeOptionDef(string Id, string Technique, bool Mastery = false, string? Requires = null);

/// <summary>The predetermined picks of a character hired at <paramref name="Rank"/> (STF-10), one per rank from ★2.</summary>
public sealed record PresetDef(int Rank, IReadOnlyList<string> Choices);

/// <summary><c>items.json</c> (ITM-01).</summary>
public sealed record ItemDef(
    string Id,
    string Name,
    ItemKind Kind,
    Rarity Rarity,
    int Price,
    IReadOnlyList<EffectDef> Effects,
    IReadOnlyList<StaffClass>? ClassTags = null,
    string? DisplayModel = null,
    string? DisplaySpot = null);

/// <summary><c>fixtures.json</c> (FIX-01). <paramref name="Zone"/> limits where it may go; null means anywhere.</summary>
public sealed record FixtureDef(
    string Id,
    string Name,
    int Footprint,
    double Health,
    int Cost,
    IReadOnlyList<EffectDef> Effects,
    string? Zone = null);

/// <summary>
/// An effect (DAT-03): a kind and the parameters that kind needs. Plane and fixture values are IDs; technique values
/// are technique IDs.
/// </summary>
public sealed record EffectDef(
    EffectKind Kind,
    double? Multiplier = null,
    double? Amount = null,
    double? Radius = null,
    StatKind? Stat = null,
    Appetite? Appetite = null,
    TriggerKind? Trigger = null,
    string? Technique = null,
    string? Plane = null,
    string? Fixture = null,
    string? CountsAs = null,
    int? Seats = null,
    int? Zones = null,
    TriggerKind? CancelledBy = null,
    EffectTarget? Target = null);

/// <summary><c>flavour.json</c> (DAT-06). Both flags are required.</summary>
public sealed record FlavourDef(string Id, string Context, string Text, bool? FilterSafe, bool? BloodSafe);
