using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Run;

namespace MistyInn.Core.Events;

/// <summary>A turned creature moved to another escalation stage (ESC-01).</summary>
public sealed record EscalationStageChanged(TimeSpan At, CreatureId Creature, EscalationStage Stage) : DomainEvent(At);

/// <summary>
/// One attack landed (ESC-05). <paramref name="Target"/> is a unit, a creature or a fixture; null for a wall or a room
/// door. Views show blood or a flash, by the Blood filter (RAT-09).
/// </summary>
public sealed record AttackLanded(TimeSpan At, EntityRef Source, EntityRef? Target, double Damage) : DomainEvent(At);

/// <summary>A staff member or the innkeeper fell (ESC-09). They stay on the floor until dawn (RAT-05).</summary>
public sealed record UnitFell(TimeSpan At, UnitId Unit, CreatureId By) : DomainEvent(At);

/// <summary>A creature that had not turned was struck down by a turned one; it dissolves with no tribute (ESC-04).</summary>
public sealed record CreatureFell(TimeSpan At, CreatureId Creature, CreatureId By) : DomainEvent(At);

/// <summary>A turned creature was banished (ESC-08); <paramref name="By"/> is the unit that struck last.</summary>
public sealed record CreatureBanished(TimeSpan At, CreatureId Creature, UnitId By) : DomainEvent(At);

/// <summary>At Dawn, a fallen staff member was only wounded (ESC-09); Shaken next cycle (STF-12).</summary>
public sealed record StaffWounded(TimeSpan At, UnitId Unit) : DomainEvent(At);

/// <summary>At Dawn, a fallen staff member died (ESC-09). Named in the Dawn summary (RAT-05).</summary>
public sealed record StaffDied(TimeSpan At, UnitId Unit, string StaffId, string Name) : DomainEvent(At);

/// <summary>A fixture was repaired at Dawn (BRD-07, D-072).</summary>
public sealed record FixtureRepaired(TimeSpan At, FixtureId Fixture) : DomainEvent(At);

/// <summary>Integrity was repaired at Dawn for silver (BRD-07).</summary>
public sealed record IntegrityRepaired(TimeSpan At, double Integrity, int Silver) : DomainEvent(At);

/// <summary>A companion and a guest went into a back room, and its door closed (CMP-04).</summary>
public sealed record RoomSessionStarted(TimeSpan At, string Room, UnitId Companion, CreatureId Guest) : DomainEvent(At);

/// <summary>A room session ended; <paramref name="Interrupted"/> when its door was attacked (CMP-05).</summary>
public sealed record RoomSessionEnded(TimeSpan At, string Room, bool Interrupted) : DomainEvent(At);

/// <summary>The run was lost (RUN-03). Nothing more happens in this session.</summary>
public sealed record RunEnded(TimeSpan At, RunEndReason Reason) : DomainEvent(At);

/// <summary>A staff member joined the run and sits on the bench (D-074, and the shop from step 6).</summary>
public sealed record UnitJoined(TimeSpan At, UnitId Unit) : DomainEvent(At);

/// <summary>The content filters changed (SET-04); views re-read anything they show filtered.</summary>
public sealed record ContentFiltersChanged(TimeSpan At, ContentFilters Filters) : DomainEvent(At);
