namespace MistyInn.Core.Randomness;

/// <summary>
/// The only source of randomness in Core (INV-02). One instance is seeded per run, and its state is saved with the run
/// (SAV-01), so the same seed and commands always give the same results.
/// </summary>
public interface IRandomSource
{
    /// <summary>The next raw 64-bit value.</summary>
    ulong NextUInt64();

    /// <summary>A uniformly distributed integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).</summary>
    int NextInt(int minInclusive, int maxExclusive);

    /// <summary>A uniformly distributed double in [0, 1).</summary>
    double NextDouble();

    /// <summary>True with the given probability (clamped to [0, 1]).</summary>
    bool Chance(double probability);

    /// <summary>A snapshot of the generator's state, for saves.</summary>
    RandomState GetState();

    /// <summary>Restores a state taken with <see cref="GetState"/>; the sequence continues from that point.</summary>
    void SetState(RandomState state);
}
