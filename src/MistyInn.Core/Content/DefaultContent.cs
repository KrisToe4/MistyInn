using MistyInn.Core.Board;

namespace MistyInn.Core.Content;

/// <summary>
/// Temporary built-in content for step 2: the test layout, the five planes, one placeholder creature per plane and the
/// tuning defaults from section 6.4. Step 3 replaces it with <c>data/*.json</c> (INV-04); tests may keep using it as
/// fixed content.
/// </summary>
public static class DefaultContent
{
    /// <summary>
    /// The clearing (26 × 20) with the inn (14 × 10 inside the walls) in the middle. Legend: <c>.</c> clearing,
    /// <c>G</c> mist gate, <c>#</c> wall, <c>D</c> front door, <c>E</c> entrance, <c>C</c> common room, <c>B</c> bar,
    /// <c>K</c> kitchen, <c>S</c> stage, <c>L</c> lounge, <c>R</c> rooms (<c>r</c> a room's doorway), <c>X</c> cellar
    /// door.
    /// </summary>
    public static IReadOnlyList<string> LayoutRows { get; } =
    [
        ".............G............",
        "..........................",
        "..........................",
        ".....################.....",
        ".....#KKKKBBBBBBSSSS#.....",
        ".....#KKKKBBBBBBSSSS#.....",
        ".....#KKKKCCCCCCSSSS#.....",
        ".....#KKKKCCCCCCSSSS#.....",
        ".....#####CCCCCCLLLL#.....",
        ".....#RRRrCCCCCCLLLL#.....",
        "G....#RRR#CCCCCCLLLL#....G",
        ".....#####CCCCCCCCCX#.....",
        ".....#RRRrCEEEEECCCC#.....",
        ".....#RRR#CEEEEECCCC#.....",
        ".....########D#######.....",
        "..........................",
        "..........................",
        "..........................",
        "..........................",
        "G........................G",
    ];

    public static IReadOnlyDictionary<char, LegendEntry> Legend { get; } = new Dictionary<char, LegendEntry>
    {
        ['.'] = new(TileKind.Outside, "outside"),
        ['G'] = new(TileKind.Gate, "outside"),
        ['#'] = new(TileKind.Wall, ""),
        ['D'] = new(TileKind.Door, "door"),
        ['E'] = new(TileKind.Floor, "entrance"),
        ['C'] = new(TileKind.Floor, "common-room"),
        ['B'] = new(TileKind.Floor, "bar"),
        ['K'] = new(TileKind.Floor, "kitchen"),
        ['S'] = new(TileKind.Floor, "stage"),
        ['L'] = new(TileKind.Floor, "lounge"),
        ['R'] = new(TileKind.Floor, "rooms"),
        ['r'] = new(TileKind.Floor, "rooms"),
        ['X'] = new(TileKind.Floor, "cellar-door"),
    };

    public static BalanceDef Balance { get; } = new(
        MistDurationSeconds: 120,
        ArrivalStopBeforeDawnSeconds: 20,
        BaseArrivalIntervalSeconds: 9,
        ArrivalIntervalPerCycle: 0.92,
        ArrivalJitterMin: 0.6,
        ArrivalJitterMax: 1.4,
        MaxCreatures: 12,
        BaseStaffSlots: 3,
        MaxStaffSlots: 9,
        BenchSize: 6,
        PlanesPerCycle: [1, 1, 2, 2, 2, 3, 3, 3, 3, 0],
        IdleMinSeconds: 1.5,
        IdleMaxSeconds: 3.5);

    public static GameContent Create() => new(
        BoardLayout.Parse(LayoutRows, Legend),
        planes:
        [
            new PlaneDef("drowned-deep", "The Drowned Deep", "#3f8fa6"),
            new PlaneDef("ashen-waste", "The Ashen Waste", "#d0602f"),
            new PlaneDef("hollow-wood", "The Hollow Wood", "#6e9a3f"),
            new PlaneDef("gearbound-ruin", "The Gearbound Ruin", "#b8933a"),
            new PlaneDef("pale-court", "The Pale Court", "#c8b4dc"),
        ],
        creatures:
        [
            new CreatureDef("drowned-shambler", "Drowned Shambler", "drowned-deep", 1.2),
            new CreatureDef("ash-brute", "Ash Brute", "ashen-waste", 1.7),
            new CreatureDef("root-wight", "Root Wight", "hollow-wood", 1.4),
            new CreatureDef("rust-automaton", "Rust Automaton", "gearbound-ruin", 1.3),
            new CreatureDef("pale-courtier", "Pale Courtier", "pale-court", 1.6),
        ],
        startingStaff:
        [
            new StaffDef("innkeeper", "The Innkeeper", IsInnkeeper: true),
            new StaffDef("mara", "Mara", IsInnkeeper: false),
            new StaffDef("tobin", "Tobin", IsInnkeeper: false),
        ],
        unlockedPlanes: ["drowned-deep", "ashen-waste", "hollow-wood"],
        wanderZones: ["entrance", "common-room"],
        balance: Balance);
}
