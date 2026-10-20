namespace MistyInn.Core.Simulation;

/// <summary>FNV-1a over the values that make up a session's state, for determinism checks (TST-04).</summary>
internal sealed class StateHasher
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public ulong Value { get; private set; } = OffsetBasis;

    public StateHasher Add(ulong value)
    {
        for (var i = 0; i < 8; i++)
        {
            Value ^= (byte)(value >> (8 * i));
            Value *= Prime;
        }

        return this;
    }

    public StateHasher Add(long value) => Add(unchecked((ulong)value));

    public StateHasher Add(int value) => Add((long)value);

    public StateHasher Add(bool value) => Add(value ? 1L : 0L);

    public StateHasher Add(double value) => Add(BitConverter.DoubleToInt64Bits(value));

    public StateHasher Add(string value)
    {
        Add(value.Length);
        foreach (var c in value)
        {
            Add((int)c);
        }

        return this;
    }
}
