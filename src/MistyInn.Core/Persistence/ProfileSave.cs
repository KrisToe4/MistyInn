using MistyInn.Core.Bestiary;
using MistyInn.Core.Content;
using MistyInn.Core.Meta;
using MistyInn.Core.Run;

namespace MistyInn.Core.Persistence;

/// <summary>
/// The profile save (SAV-02): Mist Essence, unlocks, the bestiary, revealed ancestries, statistics and the content note
/// acknowledgement. It outlives every run (MET-01, BST-04). Entries for content that no longer exists are kept as they
/// are, so editing the data never loses progress (D-097).
/// </summary>
public sealed record ProfileSave(
    int SchemaVersion,
    int Essence,
    IReadOnlyList<string> Unlocked,
    RunStatistics Statistics,
    IReadOnlyList<BestiarySave> Bestiary,
    IReadOnlyList<string> RevealedAncestries,
    bool ContentNoteAcknowledged);

/// <summary>What has been observed of one creature (BST-02).</summary>
public sealed record BestiarySave(string CreatureId, bool Seen, int StudyPoints, IReadOnlyList<TriggerKind> TriggersSeen, int BanishPoints);

/// <summary>The profile in memory: the objects every session shares (D-091), and the content note flag (RAT-10).</summary>
public sealed record LoadedProfile(CreatureBestiary Bestiary, AncestryRecord Ancestry, MetaProfile Meta, bool ContentNoteAcknowledged);

/// <summary>Turns the profile into its save and back (SAV-02, SAV-04).</summary>
public static class ProfileSaves
{
    /// <summary>The current profile save format (SAV-04).</summary>
    public const int SchemaVersion = 1;

    public static ProfileSave Capture(CreatureBestiary bestiary, AncestryRecord ancestry, MetaProfile meta, bool contentNoteAcknowledged)
    {
        ArgumentNullException.ThrowIfNull(bestiary);
        ArgumentNullException.ThrowIfNull(ancestry);
        ArgumentNullException.ThrowIfNull(meta);
        return new ProfileSave(
            SchemaVersion,
            meta.Essence,
            meta.Unlocked,
            meta.Statistics,
            bestiary.State().Select(p => new BestiarySave(p.Id, p.Seen, p.Study, p.Triggers.ToList(), p.Banish)).ToList(),
            ancestry.Revealed,
            contentNoteAcknowledged);
    }

    /// <exception cref="SaveException">A value is missing or out of range (<see cref="SaveProblem.Corrupt"/>).</exception>
    public static LoadedProfile Restore(ProfileSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Unlocked is null || save.Statistics is null || save.Bestiary is null || save.RevealedAncestries is null
            || save.Bestiary.Any(b => b is null || b.CreatureId is null || b.TriggersSeen is null))
        {
            throw new SaveException(SaveProblem.Corrupt, "The profile is damaged: a list is missing.");
        }

        var s = save.Statistics;
        if (save.Essence < 0 || s.RunsPlayed < 0 || s.RunsWon < 0 || s.RunsWon > s.RunsPlayed || s.BestCyclesSurvived < 0
            || s.CreaturesOccupied < 0 || s.EssenceEarned < 0 || save.Bestiary.Any(b => b.StudyPoints < 0 || b.BanishPoints < 0))
        {
            throw new SaveException(SaveProblem.Corrupt, "The profile is damaged: a count is out of range.");
        }

        var bestiary = new CreatureBestiary();
        foreach (var entry in save.Bestiary)
        {
            bestiary.Load(entry.CreatureId, entry.Seen, entry.StudyPoints, entry.TriggersSeen, entry.BanishPoints);
        }

        return new LoadedProfile(
            bestiary,
            new AncestryRecord(save.RevealedAncestries),
            new MetaProfile(save.Essence, save.Unlocked, save.Statistics),
            save.ContentNoteAcknowledged);
    }
}
