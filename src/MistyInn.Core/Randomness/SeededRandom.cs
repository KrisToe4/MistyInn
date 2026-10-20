using System.Numerics;

namespace MistyInn.Core.Randomness;

/// <summary>
/// xoshiro256** seeded through SplitMix64. The algorithm is fixed: saves store its state (SAV-01) and seeded runs must
/// replay identically (INV-02), so changing it is a save-breaking change that needs a decision.
/// </summary>
public sealed class SeededRandom : IRandomSource
{
    private ulong _s0;
    private ulong _s1;
    private ulong _s2;
    private ulong _s3;

    public SeededRandom(ulong seed)
    {
        var x = seed;
        _s0 = SplitMix64(ref x);
        _s1 = SplitMix64(ref x);
        _s2 = SplitMix64(ref x);
        _s3 = SplitMix64(ref x);
    }

    public SeededRandom(RandomState state)
    {
        SetState(state);
    }

    public ulong NextUInt64()
    {
        var result = BitOperations.RotateLeft(_s1 * 5, 7) * 9;
        var t = _s1 << 17;

        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = BitOperations.RotateLeft(_s3, 45);

        return result;
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive,
                "maxExclusive must be greater than minInclusive.");
        }

        var range = (ulong)((long)maxExclusive - minInclusive);

        // Reject the low values that would bias the modulo, so every result is equally likely.
        var threshold = (0UL - range) % range;
        while (true)
        {
            var r = NextUInt64();
            if (r >= threshold)
            {
                return (int)(minInclusive + (long)(r % range));
            }
        }
    }

    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    public bool Chance(double probability)
    {
        if (probability <= 0)
        {
            return false;
        }

        if (probability >= 1)
        {
            return true;
        }

        return NextDouble() < probability;
    }

    public RandomState GetState() => new(_s0, _s1, _s2, _s3);

    public void SetState(RandomState state)
    {
        if (state.IsAllZero)
        {
            throw new ArgumentException("An all-zero state is not valid for xoshiro256**.", nameof(state));
        }

        (_s0, _s1, _s2, _s3) = (state.S0, state.S1, state.S2, state.S3);
    }

    private static ulong SplitMix64(ref ulong x)
    {
        x += 0x9E3779B97F4A7C15UL;
        var z = x;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
