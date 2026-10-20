using MistyInn.Core.Content;

namespace MistyInn.Core.Tests.Content;

/// <summary>Ways to break one thing in the real content, for validator tests (TST-07).</summary>
internal static class ContentTestKit
{
    public static ContentSet Set => GameContent.Default.Set;

    public static IReadOnlyList<string> Problems(ContentSet set) => ContentValidator.Validate(set);

    public static void AssertRejected(ContentSet set, string fragment)
    {
        var problems = Problems(set);
        Assert.That(problems, Has.Some.Contains(fragment), "problems found:\n" + string.Join("\n", problems));
        Assert.That(() => GameContent.Create(set), Throws.TypeOf<ContentException>());
    }

    public static ContentSet WithCharacter(string id, Func<StaffDef, StaffDef> change) =>
        Set with { Staff = Set.Staff with { Characters = Set.Staff.Characters.Select(c => c.Id == id ? change(c) : c).ToList() } };

    public static ContentSet WithCreature(string id, Func<CreatureDef, CreatureDef> change) =>
        Set with { Creatures = Set.Creatures.Select(c => c.Id == id ? change(c) : c).ToList() };

    public static ContentSet WithPlane(string id, Func<PlaneDef, PlaneDef> change) =>
        Set with { Planes = Set.Planes.Select(p => p.Id == id ? change(p) : p).ToList() };

    public static ContentSet WithTechnique(string id, Func<TechniqueDef, TechniqueDef> change) =>
        Set with { Techniques = Set.Techniques.Select(t => t.Id == id ? change(t) : t).ToList() };

    public static ContentSet WithFixture(string id, Func<FixtureDef, FixtureDef> change) =>
        Set with { Fixtures = Set.Fixtures.Select(f => f.Id == id ? change(f) : f).ToList() };

    public static ContentSet WithItem(string id, Func<ItemDef, ItemDef> change) =>
        Set with { Items = Set.Items.Select(i => i.Id == id ? change(i) : i).ToList() };

    /// <summary>Replaces one rank of a character's tree.</summary>
    public static StaffDef WithRank(StaffDef character, int rank, params TreeOptionDef[] options) =>
        character with
        {
            Tree = character.Tree.Select(r => r.Rank == rank ? r with { Options = options } : r).ToList(),
        };

    /// <summary>A content source that serves the built-in files, with some replaced or removed.</summary>
    public sealed class EditedSource : IContentSource
    {
        private readonly Dictionary<string, string?> _overrides;
        private readonly EmbeddedContentSource _embedded = new();

        public EditedSource(Dictionary<string, string?> overrides) => _overrides = overrides;

        public string ReadText(string fileName)
        {
            if (_overrides.TryGetValue(fileName, out var text))
            {
                return text ?? throw new FileNotFoundException(fileName);
            }

            return _embedded.ReadText(fileName);
        }

        public static string Embedded(string fileName) => new EmbeddedContentSource().ReadText(fileName);
    }
}
