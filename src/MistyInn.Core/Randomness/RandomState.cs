namespace MistyInn.Core.Randomness;

/// <summary>The four 64-bit words of a <see cref="SeededRandom"/> generator. Never all zero.</summary>
public readonly record struct RandomState(ulong S0, ulong S1, ulong S2, ulong S3)
{
    public bool IsAllZero => (S0 | S1 | S2 | S3) == 0;
}
