using MistyInn.Core.Content;

namespace MistyInn.Core.Bestiary;

/// <summary>
/// One creature on the title screen's Bestiary page (BST-01, UI-01): only what its reveal level shows. Unknown shows a
/// silhouette (everything null); Seen adds the name, plane and size; Studied the appetites; Understood the triggers,
/// combat stats and escalation profile.
/// </summary>
public sealed record BestiaryPageEntry(
    string CreatureId,
    BestiaryLevel Level,
    string? Name,
    string? PlaneName,
    int? Size,
    IReadOnlyList<AppetiteDef>? Appetites,
    IReadOnlyList<TriggerDef>? Triggers,
    CombatDef? Combat,
    EscalationDef? Escalation);

/// <summary>The Bestiary page (BST-01, BST-04): every creature in the content, in <c>creatures.json</c> order.</summary>
public static class BestiaryBook
{
    public static IReadOnlyList<BestiaryPageEntry> Entries(GameContent content, CreatureBestiary bestiary)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(bestiary);
        return content.Creatures.Select(c =>
        {
            var level = bestiary.LevelOf(c, content.Balance.Bestiary);
            var seen = level >= BestiaryLevel.Seen;
            var understood = level >= BestiaryLevel.Understood;
            return new BestiaryPageEntry(
                c.Id,
                level,
                seen ? c.Name : null,
                seen ? content.Plane(c.PlaneId).Name : null,
                seen ? c.Size : null,
                level >= BestiaryLevel.Studied ? c.Appetites : null,
                understood ? c.Triggers : null,
                understood ? c.Combat : null,
                understood ? c.Escalation : null);
        }).ToList();
    }

    /// <summary>How many creatures are at each level, for the run end screen (UI-10).</summary>
    public static IReadOnlyDictionary<BestiaryLevel, int> Progress(GameContent content, CreatureBestiary bestiary) =>
        Enum.GetValues<BestiaryLevel>().ToDictionary(
            level => level,
            level => Entries(content, bestiary).Count(e => e.Level == level));
}
