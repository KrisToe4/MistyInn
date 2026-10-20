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

    /// <summary>The run is over (RUN-03): nothing more happens in this session.</summary>
    RunOver,
}

/// <summary>Why a run was lost (RUN-03).</summary>
public enum RunEndReason
{
    /// <summary>The innkeeper fell (ESC-06).</summary>
    InnkeeperFell,

    /// <summary>The inn's integrity was 0 at the end of the mist (BRD-06).</summary>
    InnDestroyed,
}

/// <summary>
/// The content filters (RAT-09, SET-04). They change only what is shown, never what happens: the simulation runs the
/// same with any setting (CMP-06).
/// </summary>
/// <param name="ReducedBlood">No blood: only flavour marked <c>bloodSafe</c>; views flash hits instead of decals.</param>
/// <param name="NoSuggestive">Companions become hosts: only flavour marked <c>filterSafe</c> (CMP-06).</param>
public sealed record ContentFilters(bool ReducedBlood = false, bool NoSuggestive = false)
{
    public static ContentFilters None { get; } = new();
}
