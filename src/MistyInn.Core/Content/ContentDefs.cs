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
    IReadOnlyList<FlavourDef> Flavour,
    IReadOnlyList<SynergyDef> Synergies,
    IReadOnlyList<OmenDef> Omens,
    IReadOnlyList<UnlockDef> Unlocks);

/// <summary><c>balance.json</c>: the tuning values of section 6.4 and the run's starting set-up.</summary>
/// <param name="PlanesPerCycle">Converging planes for cycles 1, 2, 3...; the last entry repeats; 0 means all unlocked.</param>
/// <param name="RankStatMultipliers">Base-stat multipliers for ★1 to ★5 (STF-06).</param>
/// <param name="BasicServiceSoothe">Basic service's soothe for a server who does not know Food (AGI-11).</param>
/// <param name="SatisfiedAfterSeconds">How long agitation must stay at 0 before a guest is satisfied (CRE-08).</param>
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
    IReadOnlyList<string> StartingFixtures,
    IReadOnlyDictionary<Supply, int> StartingSupplies,
    IReadOnlyDictionary<Supply, SupplyBundleDef> Supplies,
    double ServiceSessionSeconds,
    double FixtureSoothe,
    AgitationBalanceDef Agitation,
    BestiaryBalanceDef Bestiary,
    OmenBalanceDef Omens,
    EssenceDef Essence,
    CombatBalanceDef Combat,
    CompanyBalanceDef Company,
    ShopDef Shop,
    double BasicServiceSoothe = 0.3,
    double SatisfiedAfterSeconds = 5);

/// <summary>A bundle of supplies in the build menu (FIX-06).</summary>
public sealed record SupplyBundleDef(int Amount, int Price);

/// <summary>The agitation tuning of section 9 (AGI-01 to AGI-09).</summary>
/// <param name="SootheScale">Agitation points per second for a soothe of 1.0 (AGI-06).</param>
/// <param name="ColdRadius">Tiles from the front door that count as <c>Cold</c> during the mist (AGI-08).</param>
/// <param name="ReseekAfterSeconds">A creature waiting at a spot this long without being served looks again (CRE-04).</param>
public sealed record AgitationBalanceDef(
    double RestlessAt = 50,
    double MenacingAt = 75,
    double RestlessRateMultiplier = 1.5,
    double MenacingBreakSeconds = 8,
    double FrightenRadius = 3,
    double FrightenRateMultiplier = 1.2,
    double RestlessDamagePerSecond = 0.5,
    double ContagionRadius = 4,
    double ContagionAgitation = 15,
    double SootheScale = 2,
    double PreferredBonus = 1.25,
    double LikesStrength = 0.5,
    double ColdRadius = 3,
    double ReseekAfterSeconds = 8);

/// <summary>When omens come (OMN-01).</summary>
/// <param name="FromCycle">The first cycle that may have an omen.</param>
/// <param name="Chance">The chance of an omen at a Dusk from <paramref name="FromCycle"/>, 0 to 1.</param>
/// <param name="ConvergenceChance">The chance on the Convergence, the last cycle (RUN-02).</param>
public sealed record OmenBalanceDef(int FromCycle = 2, double Chance = 0.6, double ConvergenceChance = 1);

/// <summary>
/// The Mist Essence formula (MET-02): cycles survived × <paramref name="PerCycleSurvived"/> + creatures kept occupied ÷
/// <paramref name="OccupiedPerPoint"/> (rounded down) + <paramref name="WinBonus"/> if the run was won.
/// </summary>
public sealed record EssenceDef(int PerCycleSurvived = 2, int OccupiedPerPoint = 5, int WinBonus = 10);

/// <summary>The combat and damage tuning of section 10 and BRD-06 (D-070).</summary>
/// <param name="GuardRadius">Fighters engage turned creatures in their zone or within this many tiles of their post (ESC-07).</param>
/// <param name="IntegrityShare">The part of the damage a fixture or room door takes that also comes off integrity (BRD-06).</param>
/// <param name="WallDamageMultiplier">Integrity lost per point of damage dealt to a wall (BRD-06).</param>
/// <param name="ViolenceRadius">An attack sets off <c>Violence</c> triggers within this many tiles (AGI-08).</param>
/// <param name="ViolenceSeconds">How long an attack counts as violence nearby (AGI-08, CMP-05).</param>
/// <param name="BloodRadius">A fallen unit sets off <c>Blood</c> triggers within this many tiles (AGI-08).</param>
/// <param name="WoundChance">The chance a fallen staff member is only wounded at Dawn; otherwise they die (ESC-09).</param>
/// <param name="ShakenSoothe">The soothe multiplier of a staff member who fell and survived, for the next cycle (STF-12).</param>
/// <param name="GrappleSlow">A grappled creature's attack interval is multiplied by this (STF-02).</param>
/// <param name="RepathSeconds">How often a moving fighter or turned creature looks for a new path.</param>
/// <param name="IntegrityPerSilver">Integrity points one silver repairs at Dawn (BRD-07).</param>
/// <param name="MaxMarks">At most this many recent attacks and blood marks are kept (INV-05).</param>
public sealed record CombatBalanceDef(
    double GuardRadius = 4,
    double IntegrityShare = 0.25,
    double WallDamageMultiplier = 1,
    double ViolenceRadius = 4,
    double ViolenceSeconds = 2,
    double BloodRadius = 4,
    double WoundChance = 0.7,
    double ShakenSoothe = 0.8,
    double GrappleSlow = 1.5,
    double RepathSeconds = 0.5,
    int IntegrityPerSilver = 5,
    int MaxMarks = 64);

/// <summary>The Company tuning of section 11.5 (D-071).</summary>
/// <param name="RoomAfterSeconds">Floor company a creature must have had before the pair goes to a room (CMP-04).</param>
/// <param name="RoomSessionSeconds">How long a room session lasts (CMP-04).</param>
/// <param name="RoomRestSeconds">The companion's rest after a session, out of play (CMP-04).</param>
/// <param name="RoomSootheMultiplier">The room soothes at this many times the floor rate (CMP-04).</param>
/// <param name="PreferredSoothe">A preferred guest is soothed this many times more (CMP-02).</param>
/// <param name="InterruptRadius">Violence this close ends floor company (CMP-05).</param>
/// <param name="DoorHealth">A room door's health; a broken door closes the room until Dawn (CMP-05, D-069).</param>
public sealed record CompanyBalanceDef(
    double RoomAfterSeconds = 8,
    double RoomSessionSeconds = 25,
    double RoomRestSeconds = 10,
    double RoomSootheMultiplier = 3,
    double PreferredSoothe = 1.5,
    double InterruptRadius = 4,
    double DoorHealth = 40);

/// <summary>The bestiary's reveal thresholds (BST-02).</summary>
/// <param name="StudiedPoints">Cycles of being served and staying calm until dawn before a creature is Studied.</param>
public sealed record BestiaryBalanceDef(int StudiedPoints = 3);

/// <summary>The shop's tuning (section 13.1). <paramref name="ItemWeights"/> are the item draw weights by rarity (D-080).</summary>
public sealed record ShopDef(
    int StaffOffers,
    int ItemOffers,
    int RerollBase,
    int RerollStep,
    double PityWeight,
    double HomePlaneWeight,
    int RankPriceStep,
    IReadOnlyList<RankOddsDef> RankOdds,
    IReadOnlyDictionary<Rarity, double> ItemWeights);

/// <summary>Rank odds (percent for ★1 to ★5) from a cycle onward (SHP-03).</summary>
public sealed record RankOddsDef(int FromCycle, IReadOnlyList<int> Odds);

/// <summary>
/// <c>inn.json</c>: the board map and how to read it (BRD-01, SIM-04). Legend keys are single characters.
/// <paramref name="DisplaySpots"/> names the [x, y] tiles where inn items are shown (ITM-04, ART-17, D-078).
/// </summary>
public sealed record InnDef(
    IReadOnlyList<string> Rows,
    IReadOnlyDictionary<string, LegendDef> Legend,
    IReadOnlyDictionary<string, IReadOnlyList<int>>? DisplaySpots = null);

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

/// <summary>
/// A staff character (STF-01). <paramref name="Origin"/> is a plane ID or <c>mortal-realm</c>. <paramref name="Prefers"/>
/// and <paramref name="Refuses"/> are creature IDs a companion likes to entertain or will not entertain (CMP-01).
/// </summary>
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
    IReadOnlyList<PresetDef> Presets,
    IReadOnlyList<string>? Prefers = null,
    IReadOnlyList<string>? Refuses = null);

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

/// <summary>
/// <c>fixtures.json</c> (FIX-01). <paramref name="Shape"/> is a list of [x, y] tile offsets from the origin, rotated in
/// 90° steps when placed. <paramref name="Zone"/> limits where it may go; null means anywhere.
/// </summary>
public sealed record FixtureDef(
    string Id,
    string Name,
    IReadOnlyList<IReadOnlyList<int>> Shape,
    double Health,
    int Cost,
    IReadOnlyList<EffectDef> Effects,
    string? Zone = null)
{
    /// <summary>How many seats its <c>Provides</c> or <c>Enables</c> effects offer (the largest of them), or 0.</summary>
    public int Seats => Effects?
        .Where(e => e.Kind is EffectKind.Provides or EffectKind.Enables)
        .Select(e => e.Seats ?? 0)
        .DefaultIfEmpty(0)
        .Max() ?? 0;
}

/// <summary>
/// An effect (DAT-03): a kind and the parameters that kind needs. Plane and fixture values are IDs; technique values
/// are technique IDs. <paramref name="AdjacentTo"/> makes a fixture's effect work only while the fixture stands next to
/// (8 neighbours) a working fixture of that ID, or a room door for <see cref="AdjacentToRoomDoor"/> (FIX-03, D-079).
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
    EffectTarget? Target = null,
    string? AdjacentTo = null)
{
    /// <summary>The <see cref="AdjacentTo"/> value that means a back room's door (D-079).</summary>
    public const string AdjacentToRoomDoor = "RoomDoor";
}

/// <summary>
/// <c>synergies.json</c> (SYN-01, SYN-02): a bonus for distinct characters on the board with one class, or of one origin
/// (a plane ID or <c>mortal-realm</c>). Exactly one of <paramref name="Class"/> and <paramref name="Origin"/> is set.
/// Only the highest tier reached applies; its effects go to every staff member on the board, or to the inn for
/// inn-wide kinds such as <c>Tribute</c> (D-077).
/// </summary>
public sealed record SynergyDef(
    string Id,
    string Name,
    IReadOnlyList<SynergyTierDef> Tiers,
    StaffClass? Class = null,
    string? Origin = null);

/// <summary>One threshold of a synergy and what it gives.</summary>
public sealed record SynergyTierDef(int Count, IReadOnlyList<EffectDef> Effects);

/// <summary><c>flavour.json</c> (DAT-06). Both flags are required.</summary>
public sealed record FlavourDef(string Id, string Context, string Text, bool? FilterSafe, bool? BloodSafe);

/// <summary>
/// <c>omens.json</c> (OMN-01 to OMN-03): a one-cycle event drawn by <paramref name="Weight"/>. Its effects use the DAT-03
/// kinds and apply to the whole inn. A <c>PlaneWeight</c> effect without a plane goes to one converging plane, drawn
/// with the omen (D-087).
/// </summary>
public sealed record OmenDef(string Id, string Name, string Description, double Weight, IReadOnlyList<EffectDef> Effects);

/// <summary>
/// <c>unlocks.json</c> (MET-03): something bought with Mist Essence on the title screen. <paramref name="Target"/> is the
/// plane, character, item or fixture it unlocks (none for <see cref="UnlockKind.StartingStaff"/>); content that is the
/// target of an unlock is locked until it is bought. <paramref name="Name"/> defaults to the target's name.
/// </summary>
public sealed record UnlockDef(string Id, UnlockKind Kind, int Cost, string? Target = null, string? Name = null);
