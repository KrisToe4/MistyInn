using MistyInn.Core.Board;

namespace MistyInn.Core.Content;

/// <summary>
/// A unit as it joins the run: the innkeeper or a staff character at ★1 (RUN-01). The innkeeper knows no techniques
/// (ESC-06). <paramref name="Prefers"/> and <paramref name="Refuses"/> are creature IDs (CMP-01).
/// </summary>
public sealed record UnitTemplate(
    string Id,
    string Name,
    bool IsInnkeeper,
    StatsDef Stats,
    IReadOnlyList<string> TechniqueIds,
    IReadOnlyList<string>? Prefers = null,
    IReadOnlyList<string>? Refuses = null)
{
    public IReadOnlyList<string> PrefersOrEmpty => Prefers ?? Array.Empty<string>();

    public IReadOnlyList<string> RefusesOrEmpty => Refuses ?? Array.Empty<string>();
}

/// <summary>
/// Validated content (DAT-04), ready for the simulation: the parsed board and lookups by ID. Create it with
/// <see cref="Create"/> from a <see cref="ContentSet"/>; <see cref="Default"/> is the copy of <c>data/</c> built into
/// Core.
/// </summary>
public sealed class GameContent
{
    private static readonly Lazy<GameContent> BuiltIn = new(() => Create(ContentLoader.Load(new EmbeddedContentSource())));

    private readonly Dictionary<string, PlaneDef> _planes;
    private readonly Dictionary<string, CreatureDef> _creatures;
    private readonly Dictionary<string, StaffDef> _characters;
    private readonly Dictionary<string, TechniqueDef> _techniques;
    private readonly Dictionary<string, FixtureDef> _fixtures;

    private GameContent(ContentSet set, BoardLayout layout)
    {
        Set = set;
        Layout = layout;
        _planes = set.Planes.ToDictionary(p => p.Id);
        _creatures = set.Creatures.ToDictionary(c => c.Id);
        _characters = set.Staff.Characters.ToDictionary(c => c.Id);
        _techniques = set.Techniques.ToDictionary(t => t.Id);
        _fixtures = set.Fixtures.ToDictionary(f => f.Id);

        var keeper = set.Staff.Innkeeper;
        StartingUnits = new[] { new UnitTemplate(keeper.Id, keeper.Name, IsInnkeeper: true, keeper.Stats, Array.Empty<string>()) }
            .Concat(set.Balance.StartingStaff.Select(TemplateFor))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>The content built from <c>data/*.json</c> at compile time (DAT-02).</summary>
    public static GameContent Default => BuiltIn.Value;

    /// <summary>The raw files, as loaded.</summary>
    public ContentSet Set { get; }

    public BoardLayout Layout { get; }

    public BalanceDef Balance => Set.Balance;

    public IReadOnlyList<PlaneDef> Planes => Set.Planes;

    public IReadOnlyList<CreatureDef> Creatures => Set.Creatures;

    public IReadOnlyList<TechniqueDef> Techniques => Set.Techniques;

    public IReadOnlyList<StaffDef> Characters => Set.Staff.Characters;

    public InnkeeperDef Innkeeper => Set.Staff.Innkeeper;

    public IReadOnlyList<ItemDef> Items => Set.Items;

    public IReadOnlyList<FixtureDef> Fixtures => Set.Fixtures;

    public IReadOnlyList<FlavourDef> Flavour => Set.Flavour;

    public IReadOnlyList<string> UnlockedPlanes => Set.Balance.UnlockedPlanes;

    /// <summary>Where creatures wander when nothing calls to them (CRE-04).</summary>
    public IReadOnlyList<string> WanderZones => Set.Balance.WanderZones;

    /// <summary>The innkeeper first, then the starting staff (RUN-01).</summary>
    public IReadOnlyList<UnitTemplate> StartingUnits { get; }

    /// <exception cref="ContentException">The set breaks a validator rule (DAT-04).</exception>
    public static GameContent Create(ContentSet set)
    {
        var errors = ContentValidator.Validate(set);
        if (errors.Count > 0)
        {
            throw new ContentException(errors);
        }

        var legend = set.Inn.Legend.ToDictionary(e => e.Key[0], e => new LegendEntry(e.Value.Kind, e.Value.Zone ?? ""));
        return new GameContent(set, BoardLayout.Parse(set.Inn.Rows, legend));
    }

    public PlaneDef Plane(string id) =>
        _planes.TryGetValue(id, out var plane) ? plane : throw new KeyNotFoundException($"Unknown plane '{id}'.");

    /// <summary>A character as a unit at ★1, with its base technique only (D-065).</summary>
    public UnitTemplate TemplateFor(string characterId)
    {
        var c = Character(characterId);
        return new UnitTemplate(c.Id, c.Name, IsInnkeeper: false, c.Stats, new[] { c.BaseTechnique }, c.Prefers, c.Refuses);
    }

    public CreatureDef Creature(string id) =>
        _creatures.TryGetValue(id, out var creature) ? creature : throw new KeyNotFoundException($"Unknown creature '{id}'.");

    public StaffDef Character(string id) =>
        _characters.TryGetValue(id, out var character) ? character : throw new KeyNotFoundException($"Unknown character '{id}'.");

    public TechniqueDef Technique(string id) =>
        _techniques.TryGetValue(id, out var technique) ? technique : throw new KeyNotFoundException($"Unknown technique '{id}'.");

    public FixtureDef Fixture(string id) =>
        _fixtures.TryGetValue(id, out var fixture) ? fixture : throw new KeyNotFoundException($"Unknown fixture '{id}'.");

    /// <summary>The gate a plane's creatures walk out of: planes take the gates in map order (PLN-04).</summary>
    public Tile GateFor(string planeId)
    {
        for (var i = 0; i < Planes.Count; i++)
        {
            if (Planes[i].Id == planeId)
            {
                return Layout.Gates[i];
            }
        }

        throw new KeyNotFoundException($"Unknown plane '{planeId}'.");
    }
}
