namespace MistyInn.Core.Run;

/// <summary>The three phases of a cycle (section 6.2).</summary>
public enum Phase
{
    /// <summary>Setup: untimed; the player places staff and starts the mist (CYC-01).</summary>
    Dusk,

    /// <summary>The automatic, timed simulation (CYC-02).</summary>
    Mist,

    /// <summary>Aftermath: untimed; the player moves on to the next Dusk (CYC-04).</summary>
    Dawn,
}
