namespace MistyInn.Core.Events;

/// <summary>
/// The base of every event Core raises for the presentation (ARC-12). <see cref="At"/> is game time (SIM-01). The
/// concrete events are added by the steps that raise them.
/// </summary>
public abstract record DomainEvent(TimeSpan At);
