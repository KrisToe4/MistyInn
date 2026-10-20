namespace MistyInn.Core;

/// <summary>The fixed caps on a session's buffers (INV-05).</summary>
/// <param name="EventCapacity">Domain events held between drains (ARC-12).</param>
/// <param name="CycleLogCapacity">Cycle log entries per cycle, including the truncation marker (INV-12).</param>
public sealed record GameSessionLimits(int EventCapacity = 4096, int CycleLogCapacity = 20_000)
{
    public static GameSessionLimits Default { get; } = new();
}
