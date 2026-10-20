using MistyInn.Core.Run;

namespace MistyInn.Core.Content;

/// <summary>
/// Picks a flavour line for a context, honouring the content filters (DAT-06, RAT-09). The pick depends only on the
/// seed it is given, never on the run's random source, so the filters cannot change the simulation (CMP-06).
/// </summary>
public static class FlavourPicker
{
    public static FlavourDef? Pick(IReadOnlyList<FlavourDef> lines, string context, ContentFilters filters, long seed)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(filters);

        var eligible = lines
            .Where(l => l.Context == context
                        && (!filters.NoSuggestive || l.FilterSafe == true)
                        && (!filters.ReducedBlood || l.BloodSafe == true))
            .ToList();
        return eligible.Count == 0 ? null : eligible[(int)(Math.Abs(seed) % eligible.Count)];
    }
}
