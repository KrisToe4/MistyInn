using MistyInn.Core.Bestiary;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Run;

namespace MistyInn.Core.Events;

/// <summary>A creature's agitation crossed into another state (AGI-03).</summary>
public sealed record ThresholdCrossed(TimeSpan At, CreatureId Creature, CreatureState From, CreatureState To) : DomainEvent(At);

/// <summary>A creature reached 100 agitation and turned (AGI-03); contagion follows (AGI-09).</summary>
public sealed record CreatureTurned(TimeSpan At, CreatureId Creature) : DomainEvent(At);

/// <summary>A staff member began a service session (STF-04).</summary>
public sealed record ServiceStarted(TimeSpan At, UnitId Unit, string TechniqueId, IReadOnlyList<CreatureId> Creatures) : DomainEvent(At);

/// <summary>A guest stayed at 0 agitation long enough: it paid and is walking out (CRE-08).</summary>
public sealed record CreatureSatisfied(TimeSpan At, CreatureId Creature, int Silver) : DomainEvent(At);

/// <summary>A creature left a tribute at dawn (CRE-05).</summary>
public sealed record TributeLeft(TimeSpan At, CreatureId Creature, int Silver) : DomainEvent(At);

/// <summary>The bestiary learned more about a creature (BST-02).</summary>
public sealed record BestiaryRevealed(TimeSpan At, string CreatureDefId, BestiaryLevel Level) : DomainEvent(At);

public sealed record SilverChanged(TimeSpan At, int Silver) : DomainEvent(At);

public sealed record SuppliesChanged(TimeSpan At, Supply Supply, int Amount) : DomainEvent(At);

/// <summary>A fixture was bought from the build menu into storage (FIX-05).</summary>
public sealed record FixtureBuilt(TimeSpan At, FixtureId Fixture, string FixtureDefId) : DomainEvent(At);

public sealed record FixturePlaced(TimeSpan At, FixtureId Fixture) : DomainEvent(At);

public sealed record FixtureStored(TimeSpan At, FixtureId Fixture) : DomainEvent(At);

public sealed record FixtureSold(TimeSpan At, FixtureId Fixture, int Silver) : DomainEvent(At);

/// <summary>A fixture was destroyed; it stays in place, blocking, until repaired (BRD-04).</summary>
public sealed record FixtureBroken(TimeSpan At, FixtureId Fixture, CreatureId By) : DomainEvent(At);
