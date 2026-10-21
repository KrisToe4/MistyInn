namespace MistyInn.Core.Entities;

/// <summary>
/// Issues stable, sequential IDs per entity kind, starting at 1 (ARC-13). IDs are never reused within a run, so a view
/// can always map an ID to at most one node. The counters are part of the run's state and are saved with it.
/// </summary>
public sealed class IdAllocator
{
    private int _lastUnit;
    private int _lastCreature;
    private int _lastFixture;
    private int _lastItem;

    public IdAllocator()
    {
    }

    public IdAllocator(IdAllocatorState state)
    {
        if (state.LastUnit < 0 || state.LastCreature < 0 || state.LastFixture < 0 || state.LastItem < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(state), "ID counters cannot be negative.");
        }

        (_lastUnit, _lastCreature, _lastFixture, _lastItem) = (state.LastUnit, state.LastCreature, state.LastFixture, state.LastItem);
    }

    public UnitId NextUnit() => new(checked(++_lastUnit));

    public CreatureId NextCreature() => new(checked(++_lastCreature));

    public FixtureId NextFixture() => new(checked(++_lastFixture));

    public ItemId NextItem() => new(checked(++_lastItem));

    public IdAllocatorState GetState() => new(_lastUnit, _lastCreature, _lastFixture, _lastItem);
}

/// <summary>The last ID issued for each kind (0 when none has been issued).</summary>
public readonly record struct IdAllocatorState(int LastUnit, int LastCreature, int LastFixture, int LastItem = 0);
