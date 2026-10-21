using MistyInn.Core.Content;
using MistyInn.Core.Events;
using MistyInn.Core.Run;

namespace MistyInn.Core.Simulation;

/// <summary>
/// Hidden ancestry (STF-15, STF-16): revealed the first time the character soothes or fights a creature of its
/// ancestral plane in the mist, and only then worth ×1.25 soothe with that plane's creatures.
/// </summary>
internal sealed class AncestryObserver
{
    private readonly GameContent _content;
    private readonly AncestryRecord _record;
    private readonly IEventSink _sink;

    public AncestryObserver(GameContent content, AncestryRecord record, IEventSink sink)
    {
        _content = content;
        _record = record;
        _sink = sink;
    }

    /// <summary>The character's ancestry plane, or null if it has none (the innkeeper, plane-born, most Mortals).</summary>
    public string? AncestryOf(Unit unit) => unit.Def.IsInnkeeper ? null : _content.Character(unit.Def.Id).Ancestry;

    /// <summary>The ancestry plane once revealed, otherwise null.</summary>
    public string? RevealedAncestry(Unit unit) =>
        AncestryOf(unit) is { } plane && _record.IsRevealed(unit.Def.Id) ? plane : null;

    public double SootheBonus(Unit unit, CreatureDef creature) =>
        RevealedAncestry(unit) == creature.PlaneId ? _content.Balance.AncestrySoothe : 1;

    /// <summary>The unit soothes or strikes this creature: a first meeting with its ancestral plane reveals the link.</summary>
    public void Observe(Unit unit, CreatureDef creature)
    {
        if (AncestryOf(unit) is not { } plane || plane != creature.PlaneId || !_record.Reveal(unit.Def.Id))
        {
            return;
        }

        _sink.Raise(new AncestryRevealed(_sink.Time, unit.Id, unit.Def.Id, plane));
        _sink.Log("ancestry.revealed", $"plane.{plane}", unit.Id.ToRef());
    }
}
