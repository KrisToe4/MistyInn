namespace MistyInn.Core.Content;

/// <summary>
/// The rules of upgrade trees (STF-06, STF-07, STF-17), shared by the validator and, from step 6, the shop: when an
/// option is available, what a build knows, and whether every path through a tree always offers an option.
/// </summary>
public static class UpgradeTrees
{
    public const int FirstUpgradeRank = 2;
    public const int MaxRank = 5;

    /// <summary>What a character knows after a sequence of picks.</summary>
    public sealed record Build(IReadOnlySet<string> Chosen, IReadOnlySet<string> Known, IReadOnlySet<string> Mastered);

    public static Build Start(StaffDef character) =>
        new(new HashSet<string>(), new HashSet<string> { character.BaseTechnique }, new HashSet<string>());

    /// <summary>
    /// An option is available when its requirement was chosen, and it is a new technique not yet known, or a mastery
    /// of a known technique not yet mastered (STF-17).
    /// </summary>
    public static bool IsAvailable(TreeOptionDef option, Build build)
    {
        if (option.Requires is { } required && !build.Chosen.Contains(required))
        {
            return false;
        }

        return option.Mastery
            ? build.Known.Contains(option.Technique) && !build.Mastered.Contains(option.Technique)
            : !build.Known.Contains(option.Technique);
    }

    public static Build Apply(TreeOptionDef option, Build build)
    {
        var chosen = new HashSet<string>(build.Chosen) { option.Id };
        var known = new HashSet<string>(build.Known);
        var mastered = new HashSet<string>(build.Mastered);
        if (option.Mastery)
        {
            mastered.Add(option.Technique);
        }
        else
        {
            known.Add(option.Technique);
        }

        return new Build(chosen, known, mastered);
    }

    /// <summary>The options of one rank, or none if the tree has no such rank.</summary>
    public static IReadOnlyList<TreeOptionDef> OptionsAt(StaffDef character, int rank) =>
        character.Tree?.FirstOrDefault(r => r.Rank == rank)?.Options ?? (IReadOnlyList<TreeOptionDef>)Array.Empty<TreeOptionDef>();

    /// <summary>Applies the picks of a build in rank order; returns the problems, or none if every pick was available.</summary>
    public static IReadOnlyList<string> CheckChoices(StaffDef character, IReadOnlyList<string> choices)
    {
        var problems = new List<string>();
        var build = Start(character);

        for (var i = 0; i < choices.Count; i++)
        {
            var rank = FirstUpgradeRank + i;
            var option = OptionsAt(character, rank).FirstOrDefault(o => o.Id == choices[i]);
            if (option is null)
            {
                problems.Add($"'{choices[i]}' is not an option at rank {rank}");
                return problems;
            }

            if (!IsAvailable(option, build))
            {
                problems.Add($"'{choices[i]}' is not available at rank {rank} after the earlier picks");
                return problems;
            }

            build = Apply(option, build);
        }

        return problems;
    }

    /// <summary>
    /// Every path of earlier picks after which a rank offers no available option (STF-17). Trees have at most three
    /// options over four ranks, so all 81 paths are checked.
    /// </summary>
    public static IReadOnlyList<string> FindDeadEnds(StaffDef character)
    {
        var deadEnds = new List<string>();

        void Walk(int rank, Build build, string path)
        {
            if (rank > MaxRank)
            {
                return;
            }

            var available = OptionsAt(character, rank).Where(o => IsAvailable(o, build)).ToList();
            if (available.Count == 0)
            {
                deadEnds.Add($"no option is available at rank {rank} after [{path}]");
                return;
            }

            foreach (var option in available)
            {
                Walk(rank + 1, Apply(option, build), path.Length == 0 ? option.Id : $"{path}, {option.Id}");
            }
        }

        Walk(FirstUpgradeRank, Start(character), "");
        return deadEnds;
    }
}
