using MistyInn.Core.Entities;
using MistyInn.Core.Events;

namespace MistyInn.Core.Simulation;

/// <summary>What a system may tell the session: events for the views (ARC-12) and causes for the log (INV-12).</summary>
internal interface IEventSink
{
    /// <summary>Game time of the current step (SIM-01).</summary>
    TimeSpan Time { get; }

    void Raise(DomainEvent domainEvent);

    void Log(string kind, string cause, EntityRef? subject = null, EntityRef? source = null);
}
