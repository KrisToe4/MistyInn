using MistyInn.Core.Bestiary;
using MistyInn.Core.Content;
using MistyInn.Core.Events;

namespace MistyInn.Core.Simulation;

/// <summary>Records observations in the bestiary and raises an event when a creature's level goes up (BST-02).</summary>
internal sealed class BestiaryRecorder
{
    private readonly GameContent _content;
    private readonly IEventSink _sink;

    public BestiaryRecorder(GameContent content, CreatureBestiary bestiary, IEventSink sink)
    {
        _content = content;
        Bestiary = bestiary;
        _sink = sink;
    }

    public CreatureBestiary Bestiary { get; }

    public void Seen(CreatureDef creature) => Observe(creature, () => Bestiary.MarkSeen(creature.Id));

    public void Studied(CreatureDef creature) => Observe(creature, () => Bestiary.AddStudyPoint(creature.Id));

    public void TriggerSeen(CreatureDef creature, TriggerKind trigger) =>
        Observe(creature, () => Bestiary.SeeTrigger(creature.Id, trigger));

    private void Observe(CreatureDef creature, Action record)
    {
        var before = Bestiary.LevelOf(creature, _content.Balance.Bestiary);
        record();
        var after = Bestiary.LevelOf(creature, _content.Balance.Bestiary);
        if (after > before)
        {
            _sink.Raise(new BestiaryRevealed(_sink.Time, creature.Id, after));
            _sink.Log("bestiary.revealed", $"level.{after.ToString().ToLowerInvariant()}");
        }
    }
}
