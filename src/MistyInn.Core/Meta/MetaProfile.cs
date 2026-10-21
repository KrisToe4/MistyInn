using MistyInn.Core.Content;
using MistyInn.Core.Run;

namespace MistyInn.Core.Meta;

/// <summary>Why an unlock cannot be bought (MET-03).</summary>
public enum UnlockFailure
{
    None,
    UnknownUnlock,
    AlreadyOwned,
    NotEnoughEssence,
}

/// <summary>How a run ended and what it earned (RUN-04, MET-02, UI-10).</summary>
/// <param name="Reason">Why the run ended: survived the last cycle, or how it was lost (RUN-02, RUN-03).</param>
/// <param name="CyclesSurvived">Cycles whose mist the inn came through; the cycle it fell in does not count.</param>
/// <param name="CreaturesOccupied">Guests satisfied during the mist (CRE-08) or still calm at dawn (D-090).</param>
public sealed record RunSummary(RunEndReason Reason, int CyclesSurvived, int CreaturesOccupied, int EssenceEarned)
{
    public bool Won => Reason == RunEndReason.Survived;
}

/// <summary>Totals across every finished run (MET-01).</summary>
public sealed record RunStatistics(
    int RunsPlayed = 0,
    int RunsWon = 0,
    int BestCyclesSurvived = 0,
    int CreaturesOccupied = 0,
    int EssenceEarned = 0);

/// <summary>
/// The profile's meta-progression (MET-01 to MET-03): Mist Essence, the unlocks bought with it and the run statistics.
/// Like the bestiary it lives outside the session and is kept across runs: pass the same instance to each new
/// session; step 8 saves it with the profile (SAV-02, D-091). Essence is spent only on the title screen, between runs.
/// </summary>
public sealed class MetaProfile
{
    private readonly HashSet<string> _unlocked;

    public MetaProfile(int essence = 0, IEnumerable<string>? unlocked = null, RunStatistics? statistics = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(essence);
        Essence = essence;
        _unlocked = new HashSet<string>(unlocked ?? Array.Empty<string>());
        Statistics = statistics ?? new RunStatistics();
    }

    /// <summary>Mist Essence not spent yet (MET-02).</summary>
    public int Essence { get; private set; }

    /// <summary>The unlock IDs bought, sorted (for saves).</summary>
    public IReadOnlyList<string> Unlocked => _unlocked.Order(StringComparer.Ordinal).ToList();

    public RunStatistics Statistics { get; private set; }

    public bool Owns(string unlockId) => _unlocked.Contains(unlockId);

    /// <summary>Whether this content is locked: it is the target of an unlock that has not been bought (MET-03).</summary>
    public bool IsLocked(GameContent content, UnlockKind kind, string targetId)
    {
        ArgumentNullException.ThrowIfNull(content);
        return content.Unlocks.Any(u => u.Kind == kind && u.Target == targetId && !Owns(u.Id));
    }

    /// <summary>Whether New Run may choose its starting staff (MET-03, D-089).</summary>
    public bool CanChooseStartingStaff(GameContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return content.Unlocks.Any(u => u.Kind == UnlockKind.StartingStaff && Owns(u.Id));
    }

    /// <summary>The planes a new run draws from: the starting planes and the bought ones, in <c>planes.json</c> order (PLN-02).</summary>
    public IReadOnlyList<string> UnlockedPlanes(GameContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return content.Planes.Select(p => p.Id)
            .Where(id => content.UnlockedPlanes.Contains(id) || content.Unlocks.Any(u => u.Kind == UnlockKind.Plane && u.Target == id && Owns(u.Id)))
            .ToList();
    }

    /// <summary>The characters a new run may hire or start with (MET-03).</summary>
    public IReadOnlyList<StaffDef> AvailableCharacters(GameContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return content.Characters.Where(c => !IsLocked(content, UnlockKind.Character, c.Id)).ToList();
    }

    /// <summary>
    /// Why the starting staff could not be <paramref name="staffIds"/>, or null if they can (RUN-01, D-089): the
    /// content's default staff are always allowed; another choice needs the StartingStaff unlock and as many distinct,
    /// unlocked characters as the default.
    /// </summary>
    public string? CheckStartingStaff(GameContent content, IReadOnlyList<string> staffIds)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(staffIds);
        if (staffIds.SequenceEqual(content.Balance.StartingStaff))
        {
            return null;
        }

        if (!CanChooseStartingStaff(content))
        {
            return "Choosing the starting staff has not been unlocked.";
        }

        if (staffIds.Count != content.Balance.StartingStaff.Count || staffIds.Distinct().Count() != staffIds.Count)
        {
            return $"Choose {content.Balance.StartingStaff.Count} different characters.";
        }

        var available = AvailableCharacters(content).Select(c => c.Id).ToHashSet();
        return staffIds.All(available.Contains) ? null : "Every starting character must be unlocked.";
    }

    /// <summary>Why <paramref name="unlockId"/> cannot be bought now, or <see cref="UnlockFailure.None"/>.</summary>
    public UnlockFailure CheckBuy(GameContent content, string unlockId)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Unlocks.FirstOrDefault(u => u.Id == unlockId) is not { } unlock)
        {
            return UnlockFailure.UnknownUnlock;
        }

        if (Owns(unlockId))
        {
            return UnlockFailure.AlreadyOwned;
        }

        return Essence < unlock.Cost ? UnlockFailure.NotEnoughEssence : UnlockFailure.None;
    }

    /// <summary>Buys an unlock with Essence (MET-03). A refused purchase changes nothing.</summary>
    public UnlockFailure Buy(GameContent content, string unlockId)
    {
        var failure = CheckBuy(content, unlockId);
        if (failure != UnlockFailure.None)
        {
            return failure;
        }

        Essence -= content.Unlock(unlockId).Cost;
        _unlocked.Add(unlockId);
        return UnlockFailure.None;
    }

    /// <summary>Adds a finished run's Essence and statistics (RUN-04, MET-01).</summary>
    internal void Record(RunSummary summary)
    {
        Essence += summary.EssenceEarned;
        var s = Statistics;
        Statistics = s with
        {
            RunsPlayed = s.RunsPlayed + 1,
            RunsWon = s.RunsWon + (summary.Won ? 1 : 0),
            BestCyclesSurvived = Math.Max(s.BestCyclesSurvived, summary.CyclesSurvived),
            CreaturesOccupied = s.CreaturesOccupied + summary.CreaturesOccupied,
            EssenceEarned = s.EssenceEarned + summary.EssenceEarned,
        };
    }

    /// <summary>
    /// Mist Essence for a run (MET-02): cycles survived × 2 + creatures kept occupied ÷ 5 (rounded down) + 10 if won,
    /// with the factors from <c>balance.json</c>.
    /// </summary>
    public static int EssenceFor(EssenceDef formula, int cyclesSurvived, int creaturesOccupied, bool won)
    {
        ArgumentNullException.ThrowIfNull(formula);
        return cyclesSurvived * formula.PerCycleSurvived + creaturesOccupied / formula.OccupiedPerPoint + (won ? formula.WinBonus : 0);
    }
}
