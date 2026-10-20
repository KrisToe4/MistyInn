using MistyInn.Core.Content;

namespace MistyInn.Core.Bestiary;

/// <summary>How much the player knows about a creature (BST-01). The levels are ordered.</summary>
public enum BestiaryLevel
{
    /// <summary>Silhouette only.</summary>
    Unknown,

    /// <summary>Name, plane and size.</summary>
    Seen,

    /// <summary>Appetites, their strengths and preferred techniques.</summary>
    Studied,

    /// <summary>Triggers, combat stats and escalation profile.</summary>
    Understood,
}

/// <summary>What has been observed of one creature, across runs (BST-02, BST-04).</summary>
public sealed record BestiaryEntry(
    string CreatureId,
    bool Seen,
    int StudyPoints,
    IReadOnlySet<TriggerKind> TriggersSeen,
    int BanishPoints,
    BestiaryLevel Level);

/// <summary>
/// The bestiary (BST-01 to BST-04). It belongs to the profile, not the run: pass the same instance to every
/// <see cref="GameSession"/> so knowledge carries across runs. Saving it is step 8 (SAV-02).
/// </summary>
public sealed class CreatureBestiary
{
    private readonly Dictionary<string, Progress> _progress = new();

    /// <summary>The level of a creature, by the content's thresholds (BST-02).</summary>
    public BestiaryLevel LevelOf(CreatureDef creature, BestiaryBalanceDef balance)
    {
        if (!_progress.TryGetValue(creature.Id, out var p) || !p.Seen)
        {
            return BestiaryLevel.Unknown;
        }

        if (p.StudyPoints < balance.StudiedPoints)
        {
            return BestiaryLevel.Seen;
        }

        // Understood: every trigger seen firing, or banishings making up for the ones not yet seen.
        var needed = Math.Max(1, creature.Triggers.Count);
        return p.TriggersSeen.Count + p.BanishPoints >= needed ? BestiaryLevel.Understood : BestiaryLevel.Studied;
    }

    public BestiaryEntry Entry(CreatureDef creature, BestiaryBalanceDef balance)
    {
        _progress.TryGetValue(creature.Id, out var p);
        p ??= new Progress();
        return new BestiaryEntry(creature.Id, p.Seen, p.StudyPoints, p.TriggersSeen.ToHashSet(), p.BanishPoints,
            LevelOf(creature, balance));
    }

    /// <summary>A creature was seen arriving.</summary>
    internal void MarkSeen(string creatureId) => Get(creatureId).Seen = true;

    /// <summary>A creature that was served stayed calm until dawn this cycle.</summary>
    internal void AddStudyPoint(string creatureId) => Get(creatureId).StudyPoints++;

    /// <summary>One of a creature's triggers was seen firing.</summary>
    internal void SeeTrigger(string creatureId, TriggerKind trigger) => Get(creatureId).TriggersSeen.Add(trigger);

    /// <summary>A creature was banished (from step 5).</summary>
    internal void AddBanishPoint(string creatureId) => Get(creatureId).BanishPoints++;

    internal IEnumerable<(string Id, bool Seen, int Study, IReadOnlyCollection<TriggerKind> Triggers, int Banish)> State() =>
        _progress.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => (p.Key, p.Value.Seen, p.Value.StudyPoints, (IReadOnlyCollection<TriggerKind>)p.Value.TriggersSeen, p.Value.BanishPoints));

    private Progress Get(string creatureId)
    {
        if (!_progress.TryGetValue(creatureId, out var p))
        {
            p = new Progress();
            _progress[creatureId] = p;
        }

        return p;
    }

    private sealed class Progress
    {
        public bool Seen { get; set; }

        public int StudyPoints { get; set; }

        public SortedSet<TriggerKind> TriggersSeen { get; } = new();

        public int BanishPoints { get; set; }
    }
}
