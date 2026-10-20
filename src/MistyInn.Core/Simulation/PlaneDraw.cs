using MistyInn.Core.Content;
using MistyInn.Core.Randomness;

namespace MistyInn.Core.Simulation;

/// <summary>Draws each cycle's converging planes (PLN-03).</summary>
internal static class PlaneDraw
{
    /// <summary>How many planes converge in a cycle (section 6.4); 0 in the table means every unlocked plane.</summary>
    public static int CountFor(int cycle, BalanceDef balance, int unlockedCount)
    {
        var table = balance.PlanesPerCycle;
        var value = table[Math.Clamp(cycle - 1, 0, table.Count - 1)];
        return value <= 0 ? unlockedCount : Math.Min(value, unlockedCount);
    }

    /// <summary>
    /// A uniformly random set of <paramref name="count"/> unlocked planes, never the same set as
    /// <paramref name="previous"/> when another set is possible. The result keeps the unlocked order.
    /// </summary>
    public static IReadOnlyList<string> Draw(
        IRandomSource random, IReadOnlyList<string> unlocked, int count, IReadOnlyCollection<string> previous)
    {
        if (count < 1 || count > unlocked.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be between 1 and the unlocked count.");
        }

        var all = Combinations(unlocked, count).ToList();
        var candidates = all.Where(set => !SameSet(set, previous)).ToList();
        if (candidates.Count == 0)
        {
            candidates = all;
        }

        return candidates[random.NextInt(0, candidates.Count)];
    }

    private static bool SameSet(IReadOnlyCollection<string> a, IReadOnlyCollection<string> b) =>
        a.Count == b.Count && a.All(id => b.Contains(id));

    private static IEnumerable<IReadOnlyList<string>> Combinations(IReadOnlyList<string> items, int count)
    {
        var indices = Enumerable.Range(0, count).ToArray();
        while (true)
        {
            yield return indices.Select(i => items[i]).ToArray();

            var position = count - 1;
            while (position >= 0 && indices[position] == items.Count - count + position)
            {
                position--;
            }

            if (position < 0)
            {
                yield break;
            }

            indices[position]++;
            for (var i = position + 1; i < count; i++)
            {
                indices[i] = indices[i - 1] + 1;
            }
        }
    }
}
