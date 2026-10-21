using MistyInn.Core.Board;
using MistyInn.Core.Entities;
using MistyInn.Core.Run;

namespace MistyInn.Core.Events;

public sealed record PhaseChanged(TimeSpan At, int Cycle, Phase Phase) : DomainEvent(At);

/// <summary>The forecast for a new Dusk (PLN-03); the views open these planes' gates (PLN-04).</summary>
public sealed record ConvergingPlanesDrawn(TimeSpan At, int Cycle, IReadOnlyList<string> PlaneIds) : DomainEvent(At);

/// <summary>A new Dusk drew an omen for this cycle (OMN-01); the forecast shows it.</summary>
public sealed record OmenDrawn(TimeSpan At, int Cycle, string OmenId) : DomainEvent(At);

public sealed record UnitPlaced(TimeSpan At, UnitId Unit, Tile Tile) : DomainEvent(At);

public sealed record UnitBenched(TimeSpan At, UnitId Unit) : DomainEvent(At);

public sealed record CreatureArrived(TimeSpan At, CreatureId Creature, string CreatureDefId, string PlaneId)
    : DomainEvent(At);

/// <summary>A creature went back into the mist (at dawn, CYC-03).</summary>
public sealed record CreatureDissolved(TimeSpan At, CreatureId Creature) : DomainEvent(At);

public sealed record SpeedChanged(TimeSpan At, int Multiplier) : DomainEvent(At);
