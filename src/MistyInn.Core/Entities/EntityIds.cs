namespace MistyInn.Core.Entities;

/// <summary>The kinds of entity Core issues IDs for (ARC-13).</summary>
public enum EntityKind
{
    Unit,
    Creature,
    Fixture,
    Item,
}

/// <summary>A staff member or the innkeeper.</summary>
public readonly record struct UnitId(int Value)
{
    public EntityRef ToRef() => new(EntityKind.Unit, Value);

    public override string ToString() => $"unit:{Value}";
}

/// <summary>A creature from one of the planes.</summary>
public readonly record struct CreatureId(int Value)
{
    public EntityRef ToRef() => new(EntityKind.Creature, Value);

    public override string ToString() => $"creature:{Value}";
}

/// <summary>A fixture placed on, or stored for, the inn board.</summary>
public readonly record struct FixtureId(int Value)
{
    public EntityRef ToRef() => new(EntityKind.Fixture, Value);

    public override string ToString() => $"fixture:{Value}";
}

/// <summary>An item the player owns: a staff item (carried or in storage) or an inn item (ITM-01).</summary>
public readonly record struct ItemId(int Value)
{
    public EntityRef ToRef() => new(EntityKind.Item, Value);

    public override string ToString() => $"item:{Value}";
}

/// <summary>A reference to any entity, for logs and events that may point at more than one kind.</summary>
public readonly record struct EntityRef(EntityKind Kind, int Id)
{
    public override string ToString() => $"{Kind.ToString().ToLowerInvariant()}:{Id}";
}
