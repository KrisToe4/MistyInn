using MistyInn.Core.Entities;

namespace MistyInn.Core.Logging;

/// <summary>
/// One cause-and-effect record in the cycle log (INV-12). <see cref="Kind"/> and <see cref="Cause"/> are
/// machine-readable keys (for example <c>agitation.rose</c> and <c>trigger.light</c>) that the UI turns into text; they
/// are never player-facing strings.
/// </summary>
/// <param name="Sequence">Position in the log, from 1; strictly increasing within a cycle.</param>
/// <param name="At">Game time of the record (SIM-01).</param>
/// <param name="Kind">What happened.</param>
/// <param name="Cause">Why it happened.</param>
/// <param name="Subject">The entity it happened to, if any.</param>
/// <param name="Source">The entity that caused it, if any.</param>
public sealed record CycleLogEntry(
    long Sequence,
    TimeSpan At,
    string Kind,
    string Cause,
    EntityRef? Subject = null,
    EntityRef? Source = null);
