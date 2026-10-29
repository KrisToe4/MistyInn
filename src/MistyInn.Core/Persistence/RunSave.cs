using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Randomness;
using MistyInn.Core.Run;

namespace MistyInn.Core.Persistence;

/// <summary>
/// The run save (SAV-01), taken at the start of a Dusk, just after the player leaves Dawn: everything a restored session
/// needs to carry on exactly as the saved one would (INV-02). Mist state (creatures, sessions, timers) is never in it,
/// because saving happens only between cycles (SAV-03). IDs name content (DAT-05); a save whose IDs no longer exist is
/// refused (D-097).
/// </summary>
/// <param name="SchemaVersion">The save format (SAV-04); <see cref="RunSaves.SchemaVersion"/> when written.</param>
/// <param name="ContentVersion"><c>balance.json</c> <c>contentVersion</c> when written; for information (D-097).</param>
/// <param name="Seed">The seed the run started from, shown on the HUD.</param>
/// <param name="OmenEffectPlanes">The plane of each of the omen's effects as it applies this cycle (a Surge's drawn plane, D-087).</param>
public sealed record RunSave(
    int SchemaVersion,
    int ContentVersion,
    ulong Seed,
    int Cycle,
    RandomState Random,
    RandomState ShopRandom,
    RandomState OmenRandom,
    IdAllocatorState Ids,
    int Silver,
    double Integrity,
    IReadOnlyDictionary<Supply, int> Supplies,
    IReadOnlyList<string> StartingStaff,
    IReadOnlyList<string> UnlockedPlanes,
    IReadOnlyList<string> LockedCharacters,
    IReadOnlyList<string> LockedItems,
    IReadOnlyList<string> LockedFixtures,
    IReadOnlyList<string> ConvergingPlanes,
    IReadOnlyList<string> PreviousPlaneSet,
    string? Omen,
    IReadOnlyList<string?> OmenEffectPlanes,
    int CreaturesOccupied,
    IReadOnlyList<UnitSave> Units,
    IReadOnlyList<FixtureSave> Fixtures,
    IReadOnlyList<ItemSave> Items,
    ShopSave Shop);

/// <summary>A staff member or the innkeeper (SAV-01): rank, techniques, tree choices, silver spent and status.</summary>
/// <param name="DefId">The character ID, or the innkeeper's ID.</param>
/// <param name="Tile">Its home on the board, or null on the bench.</param>
/// <param name="Techniques">Technique IDs in the order learned (STF-02).</param>
/// <param name="Chosen">Upgrade options picked, from ★2 (STF-07).</param>
public sealed record UnitSave(
    int Id,
    string DefId,
    bool IsInnkeeper,
    Tile? Tile,
    int Rank,
    IReadOnlyList<string> Techniques,
    IReadOnlyList<string> Chosen,
    IReadOnlyList<string> Mastered,
    int Spent,
    bool UpgradePending,
    bool Shaken);

/// <summary>A fixture in storage (no origin) or on the board, with its damage (SAV-01).</summary>
public sealed record FixtureSave(int Id, string DefId, Tile? Origin, int Rotation, double Health);

/// <summary>An owned item: carried, in storage or an inn item (SAV-01, ITM-01).</summary>
public sealed record ItemSave(int Id, string DefId, int Paid, int? Carrier);

/// <summary>The shop's offers, lock and reroll count (SAV-01, SHP-05, SHP-06). Bought slots are null.</summary>
public sealed record ShopSave(IReadOnlyList<StaffOffer?> Staff, IReadOnlyList<ItemOffer?> Items, bool Locked, int Rerolls);

/// <summary>Why a run save cannot become a session (SAV-04, SAV-06, D-097).</summary>
public enum SaveProblem
{
    /// <summary>The file is not a valid save: it is quarantined (SAV-06).</summary>
    Corrupt,

    /// <summary>Written by a newer version of the game: not loaded, and kept (SAV-04).</summary>
    TooNew,

    /// <summary>Valid, but it names content this game's data no longer has: not loaded, and kept (D-097).</summary>
    Incompatible,
}

/// <summary>A run save that cannot be restored, and why.</summary>
public sealed class SaveException : Exception
{
    public SaveException(SaveProblem problem, string message)
        : base(message)
    {
        Problem = problem;
    }

    public SaveProblem Problem { get; }
}

/// <summary>The run save format and the checks a save passes before it becomes a session (SAV-01, SAV-04).</summary>
public static class RunSaves
{
    /// <summary>The current run save format (SAV-04). Raise it, and add a migration, whenever the format changes.</summary>
    public const int SchemaVersion = 1;

    /// <summary>
    /// Why <paramref name="save"/> cannot be restored with <paramref name="content"/>, or null if it can. Missing or
    /// out-of-range values make it <see cref="SaveProblem.Corrupt"/>; IDs or tiles this content does not have make it
    /// <see cref="SaveProblem.Incompatible"/> (D-097).
    /// </summary>
    public static SaveException? Check(RunSave save, GameContent content)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(content);
        try
        {
            CheckValues(save, content.Balance);
            CheckIds(save, content);
            return null;
        }
        catch (SaveException e)
        {
            return e;
        }
    }

    private static void CheckValues(RunSave save, BalanceDef balance)
    {
        static void Require(bool ok, string what)
        {
            if (!ok)
            {
                throw new SaveException(SaveProblem.Corrupt, $"The run save is damaged: {what}.");
            }
        }

        // System.Text.Json leaves missing properties null, whatever the nullable annotations say. The checks go through
        // AnyNull so the compiler keeps treating the values as non-null afterwards (an `is null` test would not).
        Require(!AnyNull(save.Supplies, save.StartingStaff, save.UnlockedPlanes, save.LockedCharacters, save.LockedItems,
                    save.LockedFixtures, save.ConvergingPlanes, save.PreviousPlaneSet, save.OmenEffectPlanes, save.Units,
                    save.Fixtures, save.Items, save.Shop)
                && !AnyNull(save.Shop.Staff, save.Shop.Items), "a list is missing");
        Require(save.Units.All(u => !AnyNull(u) && !AnyNull(u.DefId, u.Techniques, u.Chosen, u.Mastered))
                && save.Fixtures.All(f => !AnyNull(f) && !AnyNull(f.DefId))
                && save.Items.All(i => !AnyNull(i) && !AnyNull(i.DefId)), "an entry is missing a value");
        Require(save.Cycle >= 1 && save.Cycle <= balance.RunLength, "the cycle is out of range");
        Require(!save.Random.IsAllZero && !save.ShopRandom.IsAllZero && !save.OmenRandom.IsAllZero, "a random state is empty");
        Require(save.Ids.LastUnit >= 0 && save.Ids.LastCreature >= 0 && save.Ids.LastFixture >= 0 && save.Ids.LastItem >= 0, "an ID counter is negative");
        Require(save.Silver >= 0 && save.CreaturesOccupied >= 0 && save.Shop.Rerolls >= 0, "a count is negative");
        Require(save.Integrity > 0 && save.Integrity <= balance.Integrity, "the integrity is out of range");
        Require(save.Supplies.Values.All(n => n >= 0), "a supply is negative");
        Require(save.Units.Count(u => u.IsInnkeeper) == 1, "there must be exactly one innkeeper");
        Require(save.Units.All(u => u.Id >= 1 && u.Id <= save.Ids.LastUnit), "a unit ID is out of range");
        Require(save.Units.All(u => u.Rank >= 1 && u.Rank <= UpgradeTrees.MaxRank && u.Spent >= 0), "a rank is out of range");
        Require(save.Fixtures.All(f => f.Id >= 1 && f.Id <= save.Ids.LastFixture && f.Rotation is >= 0 and <= 3 && f.Health >= 0), "a fixture is out of range");
        Require(save.Items.All(i => i.Id >= 1 && i.Id <= save.Ids.LastItem && i.Paid >= 0), "an item is out of range");
        Require(Distinct(save.Units.Select(u => u.Id)) && Distinct(save.Fixtures.Select(f => f.Id)) && Distinct(save.Items.Select(i => i.Id)),
            "an ID is used twice");
        var unitIds = save.Units.Where(u => !u.IsInnkeeper).Select(u => u.Id).ToHashSet();
        Require(save.Items.All(i => i.Carrier is null || unitIds.Contains(i.Carrier.Value)), "an item is carried by a unit that does not exist");
    }

    private static void CheckIds(RunSave save, GameContent content)
    {
        var missing = new List<string>();

        void Known(IEnumerable<string?> ids, Func<string, bool> exists, string kind)
        {
            missing.AddRange(ids.OfType<string>().Where(id => !exists(id)).Distinct().Select(id => $"{kind} '{id}'"));
        }

        var planes = content.Planes.Select(p => p.Id).ToHashSet();
        var characters = content.Characters.Select(c => c.Id).ToHashSet();
        var techniques = content.Techniques.Select(t => t.Id).ToHashSet();
        var fixtures = content.Fixtures.Select(f => f.Id).ToHashSet();
        var items = content.Items.Select(i => i.Id).ToHashSet();

        Known(save.UnlockedPlanes.Concat(save.ConvergingPlanes).Concat(save.OmenEffectPlanes), planes.Contains, "plane");
        Known(save.StartingStaff.Concat(save.Units.Where(u => !u.IsInnkeeper).Select(u => u.DefId)), characters.Contains, "character");
        Known(save.Shop.Staff.Select(o => o?.CharacterId), characters.Contains, "character");
        Known(save.Units.SelectMany(u => u.Techniques.Concat(u.Mastered)), techniques.Contains, "technique");
        Known(save.Fixtures.Select(f => f.DefId), fixtures.Contains, "fixture");
        Known(save.Items.Select(i => i.DefId).Concat(save.Shop.Items.Select(o => o?.ItemId)), items.Contains, "item");
        Known(save.Units.Where(u => u.IsInnkeeper).Select(u => u.DefId), id => id == content.Innkeeper.Id, "innkeeper");
        Known([save.Omen], id => content.Omens.Any(o => o.Id == id), "omen");
        if (save.Omen is { } omen && content.Omens.FirstOrDefault(o => o.Id == omen) is { } def && def.Effects.Count != save.OmenEffectPlanes.Count)
        {
            missing.Add($"omen '{omen}' effects");
        }

        if (save.Units.Any(u => u.Tile is { } t && content.Layout.KindAt(t) != TileKind.Floor)
            || save.Fixtures.Any(f => f.Origin is { } o && fixtures.Contains(f.DefId)
                && Fixture.TilesAt(content.Fixture(f.DefId), o, f.Rotation).Any(t => content.Layout.KindAt(t) != TileKind.Floor)))
        {
            missing.Add("tiles on the inn's floor");
        }

        if (missing.Count > 0)
        {
            throw new SaveException(SaveProblem.Incompatible,
                $"The run save was made with different game data; it names {string.Join(", ", missing)}, which this game does not have.");
        }
    }

    private static bool AnyNull(params object?[] values) => values.Any(v => v is null);

    private static bool Distinct(IEnumerable<int> ids)
    {
        var seen = new HashSet<int>();
        return ids.All(seen.Add);
    }
}
