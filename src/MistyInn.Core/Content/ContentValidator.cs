using System.Text.RegularExpressions;
using MistyInn.Core.Board;

namespace MistyInn.Core.Content;

/// <summary>
/// The content validator (DAT-04). It checks every rule and returns every problem, so one run of the tests (or one
/// launch of a debug build) shows them all. An empty list means the content is valid.
/// </summary>
public static partial class ContentValidator
{
    /// <summary>The origin of staff who come from no plane (STF-01).</summary>
    public const string MortalRealm = "mortal-realm";

    public static IReadOnlyList<string> Validate(ContentSet set)
    {
        ArgumentNullException.ThrowIfNull(set);
        var v = new Run(set);
        v.All();
        return v.Errors;
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex KebabCase();

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HtmlColor();

    private sealed class Run
    {
        private readonly ContentSet _set;
        private readonly HashSet<string> _planeIds = new();
        private readonly HashSet<string> _creatureIds = new();
        private readonly HashSet<string> _techniqueIds = new();
        private readonly HashSet<string> _fixtureIds = new();
        private readonly HashSet<string> _itemIds = new();
        private readonly HashSet<string> _characterIds = new();
        private readonly Dictionary<string, TechniqueDef> _techniques = new();
        private BoardLayout? _layout;

        public Run(ContentSet set) => _set = set;

        public List<string> Errors { get; } = new();

        public void All()
        {
            if (Missing(_set.Balance, "balance.json") | Missing(_set.Inn, "inn.json") | Missing(_set.Planes, "planes.json")
                | Missing(_set.Creatures, "creatures.json") | Missing(_set.Techniques, "techniques.json")
                | Missing(_set.Staff, "staff.json") | Missing(_set.Items, "items.json")
                | Missing(_set.Fixtures, "fixtures.json") | Missing(_set.Flavour, "flavour.json")
                | Missing(_set.Synergies, "synergies.json") | Missing(_set.Omens, "omens.json")
                | Missing(_set.Unlocks, "unlocks.json"))
            {
                return;
            }

            CollectIds();
            Inn();
            Planes();
            Creatures();
            Techniques();
            Staff();
            Fixtures();
            Items();
            Flavour();
            Synergies();
            Balance();
            Omens();
            Unlocks();
        }

        private bool Missing(object? value, string file)
        {
            if (value is null)
            {
                Errors.Add($"{file}: missing.");
                return true;
            }

            return false;
        }

        private void Error(string message) => Errors.Add(message);

        private void Id(string file, string? id, HashSet<string> seen)
        {
            if (string.IsNullOrEmpty(id))
            {
                Error($"{file}: an entry has no id.");
                return;
            }

            if (!KebabCase().IsMatch(id))
            {
                Error($"{file}: id '{id}' must be lowercase kebab-case (DAT-05).");
            }

            if (!seen.Add(id))
            {
                Error($"{file}: duplicate id '{id}'.");
            }
        }

        private void CollectIds()
        {
            foreach (var p in _set.Planes) Id("planes.json", p.Id, _planeIds);
            foreach (var c in _set.Creatures) Id("creatures.json", c.Id, _creatureIds);
            foreach (var t in _set.Techniques)
            {
                Id("techniques.json", t.Id, _techniqueIds);
                if (t.Id is not null)
                {
                    _techniques.TryAdd(t.Id, t);
                }
            }

            foreach (var f in _set.Fixtures) Id("fixtures.json", f.Id, _fixtureIds);

            var staffIds = new HashSet<string>();
            if (_set.Staff.Innkeeper is { } keeper) Id("staff.json", keeper.Id, staffIds);
            foreach (var s in _set.Staff.Characters ?? [])
            {
                Id("staff.json", s.Id, staffIds);
                if (s.Id is not null) _characterIds.Add(s.Id);
            }

            foreach (var i in _set.Items) Id("items.json", i.Id, _itemIds);

            var flavourIds = new HashSet<string>();
            foreach (var f in _set.Flavour) Id("flavour.json", f.Id, flavourIds);
        }

        private void Inn()
        {
            var inn = _set.Inn;
            if (inn.Rows is null || inn.Legend is null)
            {
                Error("inn.json: rows and legend are required.");
                return;
            }

            var legend = new Dictionary<char, LegendEntry>();
            foreach (var (key, entry) in inn.Legend)
            {
                if (key.Length != 1)
                {
                    Error($"inn.json: legend key '{key}' must be a single character.");
                    continue;
                }

                legend[key[0]] = new LegendEntry(entry.Kind, entry.Zone ?? "");
            }

            try
            {
                _layout = BoardLayout.Parse(inn.Rows, legend);
            }
            catch (FormatException e)
            {
                Error($"inn.json: {e.Message}");
                return;
            }

            if (_layout.Rooms.Count == 0)
            {
                Error("inn.json: the inn needs at least one back room with a RoomDoor (BRD-01).");
            }

            var board = new InnBoard(_layout);
            var unreachable = _layout.ZoneIds.Except(board.ZonesReachableFromDoor()).ToList();
            if (unreachable.Count > 0)
            {
                Error($"inn.json: no path from the front door to {string.Join(", ", unreachable)} (BRD-03).");
            }

            foreach (var (name, at) in inn.DisplaySpots ?? new Dictionary<string, IReadOnlyList<int>>())
            {
                if (at is null || at.Count != 2 || !_layout.Contains(new Tile(at[0], at[1])))
                    Error($"inn.json: display spot '{name}' must be an [x, y] tile on the map (ITM-04).");
            }

            if (_set.Planes.Count > _layout.Gates.Count)
            {
                Error($"inn.json: {_set.Planes.Count} planes need {_set.Planes.Count} mist gates; the map has {_layout.Gates.Count} (PLN-04).");
            }
        }

        private void Planes()
        {
            if (_set.Planes.Count == 0)
            {
                Error("planes.json: there are no planes.");
            }

            foreach (var plane in _set.Planes)
            {
                var where = $"planes.json: plane '{plane.Id}'";
                if (string.IsNullOrWhiteSpace(plane.Name)) Error($"{where} has no name.");
                if (plane.MistColor is null || !HtmlColor().IsMatch(plane.MistColor)) Error($"{where}: mistColor must be #rrggbb.");
                if (plane.Creatures is null || plane.Creatures.Count == 0)
                {
                    Error($"{where} has no creatures.");
                    continue;
                }

                foreach (var weight in plane.Creatures)
                {
                    if (!_creatureIds.Contains(weight.Creature)) Error($"{where}: unknown creature '{weight.Creature}'.");
                    else if (_set.Creatures.First(c => c.Id == weight.Creature).PlaneId != plane.Id)
                        Error($"{where}: creature '{weight.Creature}' belongs to another plane.");
                    if (weight.Weight <= 0) Error($"{where}: creature '{weight.Creature}' needs a weight above 0.");
                }

                foreach (var effect in plane.Rules ?? []) Effect(where, effect);
            }
        }

        private void Creatures()
        {
            foreach (var c in _set.Creatures)
            {
                var where = $"creatures.json: creature '{c.Id}'";
                if (string.IsNullOrWhiteSpace(c.Name)) Error($"{where} has no name.");
                if (!_planeIds.Contains(c.PlaneId ?? "")) Error($"{where}: unknown plane '{c.PlaneId}'.");
                if (c.Size is < 1 or > 2) Error($"{where}: size must be 1 or 2.");
                if (c.Speed <= 0) Error($"{where}: speed must be above 0.");
                if (c.Tribute < 0) Error($"{where}: tribute cannot be negative.");

                if (c.Appetites is null || c.Appetites.Count is < 1 or > 3)
                {
                    Error($"{where}: needs one to three appetites (AGI-05).");
                }
                else
                {
                    if (c.Appetites.Select(a => a.Appetite).Distinct().Count() != c.Appetites.Count)
                        Error($"{where}: an appetite is listed twice.");
                    foreach (var a in c.Appetites)
                    {
                        if (a.PreferredTechnique is { } t && !_techniqueIds.Contains(t))
                            Error($"{where}: unknown preferred technique '{t}'.");
                        else if (a.PreferredTechnique is { } known && _techniques.TryGetValue(known, out var def)
                                 && !(def.Answers ?? []).Contains(a.Appetite))
                            Error($"{where}: technique '{known}' does not answer {a.Appetite}.");
                    }

                    if (c.Appetites.Any(a => a.Appetite == Appetite.Company) && !(c.Sapient && c.Adult))
                        Error($"{where}: a creature that wants Company must be sapient and adult (CRE-07, RAT-01).");
                }

                foreach (var trigger in c.Triggers ?? [])
                {
                    if (trigger.RateMultiplier < 0 || trigger.Spike < 0) Error($"{where}: trigger {trigger.Trigger} values cannot be negative.");
                    if (trigger.Trigger == TriggerKind.RivalPlane && !_planeIds.Contains(trigger.Plane ?? ""))
                        Error($"{where}: a RivalPlane trigger needs a known plane.");
                    if (trigger.Trigger == TriggerKind.Crowding && trigger.Count is not > 0)
                        Error($"{where}: a Crowding trigger needs a count above 0.");
                }

                if (c.Agitation is null) Error($"{where}: agitation is required.");
                else
                {
                    if (c.Agitation.Arrival is < 0 or > 100) Error($"{where}: arrival agitation must be 0 to 100.");
                    if (c.Agitation.RatePerSecond < 0) Error($"{where}: the agitation rate cannot be negative.");
                }

                if (c.Combat is null) Error($"{where}: combat stats are required.");
                else if (c.Combat.Health <= 0 || c.Combat.AttackInterval <= 0 || c.Combat.Range <= 0 || c.Combat.Attack < 0 || c.Combat.Armour < 0)
                    Error($"{where}: combat stats are out of range.");

                if (c.Escalation is null) Error($"{where}: an escalation profile is required.");
                else if (c.Escalation.WreckSeconds <= 0 || c.Escalation.PreySeconds <= 0
                         || (c.Escalation.Skip ?? []).Contains(EscalationStage.Hunt))
                    Error($"{where}: escalation timings must be above 0, and Hunt cannot be skipped.");
            }
        }

        private void Techniques()
        {
            foreach (var kind in Enum.GetValues<Technique>())
            {
                var count = _set.Techniques.Count(t => t.Technique == kind);
                if (count != 1) Error($"techniques.json: technique {kind} must appear exactly once; found {count}.");
            }

            foreach (var t in _set.Techniques)
            {
                var where = $"techniques.json: technique '{t.Id}'";
                if (t.Class != TechniqueCatalog.ClassOf(t.Technique))
                    Error($"{where}: {t.Technique} belongs to {TechniqueCatalog.ClassOf(t.Technique)}, not {t.Class} (STF-02).");
                if ((t.Answers is null || t.Answers.Count == 0) && !t.EngagesTurned && t.Technique != Technique.Food)
                    Error($"{where}: answers no appetite and engages no turned creature.");
                if (t.Technique == Technique.Food && (t.Answers is { Count: > 0 } || t.Supply is not null || !t.NeedsFixture))
                    Error($"{where}: Food is basic service at tables: it answers no appetite, uses no supply and needs a table (AGI-11).");
                if (t.Soothe < 0 || t.Capacity < 1 || t.Range <= 0) Error($"{where}: soothe, capacity or range is out of range.");
                if (t.NeedsFixture && !_set.Fixtures.Any(f => (f.Effects ?? []).Any(e => e.Kind == EffectKind.Enables && e.Technique == t.Id)))
                    Error($"{where}: needs a fixture, but no fixture enables it (BRD-02).");
                if (t.Mastery is null) Error($"{where}: mastery values are required (STF-17).");
                else if (t.Mastery.SootheMultiplier < 1 || t.Mastery.CapacityBonus < 0 || t.Mastery.RangeBonus < 0 || t.Mastery.AttackBonus < 0)
                    Error($"{where}: a mastery must not make the technique weaker.");
            }
        }

        private void Staff()
        {
            var staff = _set.Staff;
            if (staff.Innkeeper is null)
            {
                Error("staff.json: the innkeeper is required.");
            }
            else
            {
                if (!staff.Innkeeper.Adult) Error($"staff.json: '{staff.Innkeeper.Id}' must be an adult (RAT-01).");
                Stats($"staff.json: '{staff.Innkeeper.Id}'", staff.Innkeeper.Stats);
            }

            if (staff.Characters is null || staff.Characters.Count == 0)
            {
                Error("staff.json: there are no characters.");
                return;
            }

            foreach (var s in staff.Characters)
            {
                Character(s);
            }
        }

        private void Character(StaffDef s)
        {
            var where = $"staff.json: '{s.Id}'";
            if (!s.Adult) Error($"{where} must be an adult (STF-13, RAT-01).");
            var prefers = s.Prefers ?? [];
            var refuses = s.Refuses ?? [];
            foreach (var id in prefers.Concat(refuses).Where(id => !_creatureIds.Contains(id)))
                Error($"{where}: prefers or refuses unknown creature '{id}' (CMP-01).");
            if (prefers.Intersect(refuses).Any())
                Error($"{where}: a creature cannot be both preferred and refused (CMP-01).");
            if (string.IsNullOrWhiteSpace(s.Name)) Error($"{where} has no name.");
            if (s.Origin != MortalRealm && !_planeIds.Contains(s.Origin ?? "")) Error($"{where}: unknown origin '{s.Origin}'.");
            if (s.Ancestry is { } a)
            {
                if (!_planeIds.Contains(a)) Error($"{where}: unknown ancestry plane '{a}' (STF-15).");
                if (s.Origin != MortalRealm) Error($"{where}: only Mortal characters have a separate ancestry (STF-15).");
            }

            if (s.BaseCost is < 1 or > 3) Error($"{where}: base cost must be 1 to 3.");
            if (s.SecondClass == s.MainClass) Error($"{where}: the second class must differ from the main class.");
            Stats(where, s.Stats);

            var classes = new HashSet<StaffClass> { s.MainClass };
            if (s.SecondClass is { } second) classes.Add(second);

            StaffClass? ClassOfTechnique(string id) =>
                _techniques.TryGetValue(id, out var def) ? def.Class : null;

            if (ClassOfTechnique(s.BaseTechnique ?? "") is not { } baseClass) Error($"{where}: unknown base technique '{s.BaseTechnique}'.");
            else if (baseClass != s.MainClass) Error($"{where}: the base technique must belong to the main class.");

            if (s.Tree is null || s.Presets is null)
            {
                Error($"{where}: tree and presets are required.");
                return;
            }

            var optionIds = new HashSet<string>();
            var techniquesSoFar = new HashSet<string> { s.BaseTechnique ?? "" };
            var ranks = s.Tree.Select(r => r.Rank).ToList();
            if (!ranks.Order().SequenceEqual(Enumerable.Range(UpgradeTrees.FirstUpgradeRank, UpgradeTrees.MaxRank - 1)))
                Error($"{where}: the tree needs exactly one entry for each rank from 2 to 5 (STF-07).");

            foreach (var rank in s.Tree.OrderBy(r => r.Rank))
            {
                var at = $"{where} rank {rank.Rank}";
                if (rank.Options is null || rank.Options.Count is < 2 or > 3)
                {
                    Error($"{at}: needs two or three options (STF-07).");
                    continue;
                }

                var newThisRank = new List<string>();
                foreach (var option in rank.Options)
                {
                    if (!KebabCase().IsMatch(option.Id ?? "")) Error($"{at}: option id '{option.Id}' must be kebab-case.");
                    if (!optionIds.Add(option.Id ?? "")) Error($"{at}: duplicate option id '{option.Id}'.");

                    if (ClassOfTechnique(option.Technique ?? "") is not { } c) Error($"{at}: unknown technique '{option.Technique}'.");
                    else if (!classes.Contains(c)) Error($"{at}: '{option.Technique}' is outside the character's two classes (STF-07).");

                    if (option.Requires is { } r && !optionIds.Contains(r) || option.Requires is { } same && rank.Options.Any(o => o.Id == same))
                        Error($"{at}: option '{option.Id}' requires '{option.Requires}', which is not an earlier-rank option.");

                    if (option.Mastery && !techniquesSoFar.Contains(option.Technique ?? ""))
                        Error($"{at}: '{option.Id}' masters '{option.Technique}', which cannot be known by this rank (STF-17).");
                    if (!option.Mastery && option.Technique == s.BaseTechnique)
                        Error($"{at}: '{option.Id}' offers the base technique again.");

                    if (!option.Mastery) newThisRank.Add(option.Technique ?? "");
                }

                techniquesSoFar.UnionWith(newThisRank);
            }

            foreach (var deadEnd in UpgradeTrees.FindDeadEnds(s))
            {
                Error($"{where}: {deadEnd} (STF-17).");
            }

            var presetRanks = s.Presets.Select(p => p.Rank).ToList();
            if (!presetRanks.Order().SequenceEqual(Enumerable.Range(UpgradeTrees.FirstUpgradeRank, UpgradeTrees.MaxRank - 1)))
                Error($"{where}: needs one preset build for each rank from 2 to 5 (STF-10).");

            foreach (var preset in s.Presets)
            {
                if (preset.Choices is null || preset.Choices.Count != preset.Rank - 1)
                {
                    Error($"{where}: the rank {preset.Rank} preset needs {preset.Rank - 1} picks.");
                    continue;
                }

                foreach (var problem in UpgradeTrees.CheckChoices(s, preset.Choices))
                {
                    Error($"{where}: the rank {preset.Rank} preset is invalid: {problem}.");
                }
            }
        }

        private void Stats(string where, StatsDef? stats)
        {
            if (stats is null)
            {
                Error($"{where}: stats are required.");
                return;
            }

            if (stats.Health <= 0 || stats.AttackInterval <= 0 || stats.Range <= 0 || stats.Speed <= 0
                || stats.Attack < 0 || stats.Armour < 0 || stats.Soothe < 0)
                Error($"{where}: stats are out of range.");
        }

        private void Fixtures()
        {
            foreach (var f in _set.Fixtures)
            {
                var where = $"fixtures.json: fixture '{f.Id}'";
                if (string.IsNullOrWhiteSpace(f.Name)) Error($"{where} has no name.");
                if (f.Health <= 0 || f.Cost < 0) Error($"{where}: health or cost is out of range.");
                if (f.Shape is null || f.Shape.Count == 0 || f.Shape.Any(o => o is null || o.Count != 2))
                    Error($"{where}: the shape must be a non-empty list of [x, y] offsets (FIX-01).");
                else if (!f.Shape.Any(o => o[0] == 0 && o[1] == 0) || f.Shape.Select(o => (o[0], o[1])).Distinct().Count() != f.Shape.Count)
                    Error($"{where}: the shape must include [0, 0] and list each offset once (FIX-01).");
                if (f.Zone is { } zone && _layout is not null && !_layout.ZoneIds.Contains(zone)) Error($"{where}: unknown zone '{zone}'.");
                foreach (var effect in f.Effects ?? []) Effect(where, effect);
            }
        }

        private void Items()
        {
            foreach (var i in _set.Items)
            {
                var where = $"items.json: item '{i.Id}'";
                if (string.IsNullOrWhiteSpace(i.Name)) Error($"{where} has no name.");
                if (i.Price < 0) Error($"{where}: price cannot be negative.");
                if (i.Effects is null || i.Effects.Count == 0) Error($"{where} has no effects.");
                if (i.Kind == ItemKind.Inn && (string.IsNullOrWhiteSpace(i.DisplaySpot) || string.IsNullOrWhiteSpace(i.DisplayModel)))
                    Error($"{where}: an inn item needs a display model and a display spot (ITM-04).");
                if (i.Kind == ItemKind.Inn && i.DisplaySpot is { } spot && _set.Inn.DisplaySpots?.ContainsKey(spot) != true)
                    Error($"{where}: display spot '{spot}' is not in inn.json (ITM-04).");
                if (i.ClassTags is { } tags && tags.Distinct().Count() != tags.Count)
                    Error($"{where}: a class tag is listed twice (ITM-03).");
                if (i.Kind == ItemKind.Staff && i.DisplaySpot is not null)
                    Error($"{where}: a staff item has no display spot.");
                foreach (var effect in i.Effects ?? []) Effect(where, effect);
            }
        }

        private void Flavour()
        {
            foreach (var f in _set.Flavour)
            {
                var where = $"flavour.json: line '{f.Id}'";
                if (string.IsNullOrWhiteSpace(f.Text) || string.IsNullOrWhiteSpace(f.Context)) Error($"{where} needs a context and text.");
                if (f.FilterSafe is null || f.BloodSafe is null) Error($"{where}: filterSafe and bloodSafe are both required (DAT-06).");
            }
        }

        /// <summary>SYN-01: a class synergy has tiers at 2, 4 and 6 (or some of them), an origin synergy at 2 and 4.</summary>
        private void Synergies()
        {
            var ids = new HashSet<string>();
            foreach (var syn in _set.Synergies)
            {
                Id("synergies.json", syn.Id, ids);
                var where = $"synergies.json: synergy '{syn.Id}'";
                if (string.IsNullOrWhiteSpace(syn.Name)) Error($"{where} has no name.");
                if ((syn.Class is null) == (syn.Origin is null)) Error($"{where} needs either a class or an origin (SYN-01).");
                if (syn.Origin is { } origin && origin != MortalRealm && !_planeIds.Contains(origin))
                    Error($"{where}: unknown origin '{origin}'.");

                var allowed = syn.Class is not null ? new[] { 2, 4, 6 } : new[] { 2, 4 };
                if (syn.Tiers is null || syn.Tiers.Count == 0)
                {
                    Error($"{where} needs at least one tier.");
                    continue;
                }

                for (var i = 0; i < syn.Tiers.Count; i++)
                {
                    var tier = syn.Tiers[i];
                    if (!allowed.Contains(tier.Count) || (i > 0 && tier.Count <= syn.Tiers[i - 1].Count))
                        Error($"{where}: tier counts must be from {string.Join(", ", allowed)}, in increasing order (SYN-01).");
                    if (tier.Effects is null || tier.Effects.Count == 0) Error($"{where}: the tier at {tier.Count} has no effects.");
                    foreach (var effect in tier.Effects ?? []) Effect(where, effect);
                }
            }
        }

        private void Balance()
        {
            var b = _set.Balance;
            var where = "balance.json";
            if (b.RunLength < 1 || b.MistDurationSeconds <= 0 || b.BaseArrivalIntervalSeconds <= 0 || b.ArrivalIntervalPerCycle <= 0
                || b.ConvergenceArrivalMultiplier <= 0 || b.MaxCreatures < 1 || b.BaseStaffSlots < 1
                || b.MaxStaffSlots < b.BaseStaffSlots || b.BenchSize < 1 || b.Integrity < 1 || b.Rooms < 0
                || b.StaffItemSlots < 0 || b.ItemStorage < 0 || b.MaxInnItemsShown < 0 || b.AncestrySoothe < 1
                || b.BasicServiceSoothe is <= 0 or > 1 || b.SatisfiedAfterSeconds <= 0)
                Error($"{where}: a value is out of range.");
            if (b.ArrivalStopBeforeDawnSeconds < 0 || b.ArrivalStopBeforeDawnSeconds >= b.MistDurationSeconds)
                Error($"{where}: arrivals must stop between 0 s and the end of the mist.");
            if (b.ArrivalJitterMin <= 0 || b.ArrivalJitterMax < b.ArrivalJitterMin)
                Error($"{where}: the arrival jitter must be above 0, with min ≤ max.");
            if (b.IdleMinSeconds <= 0 || b.IdleMaxSeconds < b.IdleMinSeconds)
                Error($"{where}: idle times must be above 0, with min ≤ max.");
            if (b.PlanesPerCycle is null || b.PlanesPerCycle.Count == 0 || b.PlanesPerCycle.Any(n => n < 0))
                Error($"{where}: planesPerCycle needs at least one entry, none negative.");
            if (b.RankStatMultipliers is null || b.RankStatMultipliers.Count != UpgradeTrees.MaxRank || b.RankStatMultipliers.Any(m => m <= 0))
                Error($"{where}: rankStatMultipliers needs five values above 0.");

            if (b.UnlockedPlanes is null || b.UnlockedPlanes.Count == 0 || b.UnlockedPlanes.Any(p => !_planeIds.Contains(p)))
                Error($"{where}: unlockedPlanes must be a non-empty list of known planes.");
            if (b.WanderZones is null || b.WanderZones.Count == 0 || (_layout is not null && b.WanderZones.Any(z => !_layout.ZoneIds.Contains(z))))
                Error($"{where}: wanderZones must be a non-empty list of zones in inn.json.");

            var characterIds = (_set.Staff.Characters ?? []).Select(c => c.Id).ToHashSet();
            if (b.StartingStaff is null || b.StartingStaff.Any(id => !characterIds.Contains(id)) || b.StartingStaff.Distinct().Count() != b.StartingStaff.Count)
                Error($"{where}: startingStaff must list distinct known characters (RUN-01).");
            else if (b.StartingStaff.Count > b.BenchSize)
                Error($"{where}: the starting staff do not fit on the bench.");

            if (b.StartingFixtures is null || b.StartingFixtures.Any(id => !_fixtureIds.Contains(id)))
                Error($"{where}: startingFixtures must list known fixtures (RUN-01).");
            if (b.Supplies is null || Enum.GetValues<Supply>().Any(s => !b.Supplies.ContainsKey(s))
                || b.Supplies.Values.Any(s => s is null || s.Amount < 1 || s.Price < 0))
                Error($"{where}: supplies needs a bundle (amount ≥ 1, price ≥ 0) for every supply (FIX-06).");
            if (b.StartingSupplies is null || b.StartingSupplies.Values.Any(n => n < 0))
                Error($"{where}: startingSupplies cannot be negative.");
            if (b.ServiceSessionSeconds <= 0 || b.FixtureSoothe < 0)
                Error($"{where}: serviceSessionSeconds must be above 0 and fixtureSoothe not negative.");

            var a = b.Agitation;
            if (a is null)
                Error($"{where}: agitation is required.");
            else if (!(0 < a.RestlessAt && a.RestlessAt < a.MenacingAt && a.MenacingAt < 100)
                     || a.RestlessRateMultiplier < 1 || a.MenacingBreakSeconds <= 0 || a.FrightenRadius < 0
                     || a.FrightenRateMultiplier < 1 || a.RestlessDamagePerSecond < 0 || a.ContagionRadius < 0
                     || a.ContagionAgitation < 0 || a.SootheScale <= 0 || a.PreferredBonus < 1
                     || a.LikesStrength is <= 0 or > 1 || a.ColdRadius < 0 || a.ReseekAfterSeconds <= 0)
                Error($"{where}: an agitation value is out of range (section 9).");

            if (b.Bestiary is null || b.Bestiary.StudiedPoints < 1)
                Error($"{where}: bestiary.studiedPoints must be at least 1 (BST-02).");
            if (b.Omens is null || b.Omens.FromCycle < 1 || b.Omens.Chance is < 0 or > 1 || b.Omens.ConvergenceChance is < 0 or > 1)
                Error($"{where}: omens needs fromCycle of at least 1 and chances from 0 to 1 (OMN-01).");
            if (b.Essence is null || b.Essence.PerCycleSurvived < 0 || b.Essence.OccupiedPerPoint < 1 || b.Essence.WinBonus < 0)
                Error($"{where}: essence needs perCycleSurvived and winBonus of at least 0 and occupiedPerPoint of at least 1 (MET-02).");

            if (_layout is not null && b.Rooms > _layout.Rooms.Count)
                Error($"{where}: rooms is {b.Rooms}, but inn.json has {_layout.Rooms.Count} back rooms (BRD-01).");

            var combat = b.Combat;
            if (combat is null)
                Error($"{where}: combat is required.");
            else if (combat.GuardRadius < 0 || combat.IntegrityShare is < 0 or > 1 || combat.WallDamageMultiplier < 0 || combat.ViolenceRadius < 0
                     || combat.ViolenceSeconds <= 0 || combat.BloodRadius < 0 || combat.WoundChance is < 0 or > 1 || combat.ShakenSoothe is < 0 or > 1
                     || combat.GrappleSlow < 1 || combat.RepathSeconds <= 0 || combat.IntegrityPerSilver < 1 || combat.MaxMarks < 1)
                Error($"{where}: a combat value is out of range (section 10).");

            var company = b.Company;
            if (company is null)
                Error($"{where}: company is required.");
            else if (company.RoomAfterSeconds < 0 || company.RoomSessionSeconds <= 0 || company.RoomRestSeconds < 0 || company.RoomSootheMultiplier < 1
                     || company.PreferredSoothe < 1 || company.InterruptRadius < 0 || company.DoorHealth <= 0)
                Error($"{where}: a company value is out of range (section 11.5).");

            var shop = b.Shop;
            if (shop is null)
            {
                Error($"{where}: shop is required.");
                return;
            }

            if (shop.StaffOffers < 1 || shop.ItemOffers < 0 || shop.RerollBase < 0 || shop.RerollStep < 0
                || shop.PityWeight < 1 || shop.HomePlaneWeight < 1 || shop.RankPriceStep < 0)
                Error($"{where}: a shop value is out of range.");
            if (shop.ItemWeights is null || Enum.GetValues<Rarity>().Any(r => !shop.ItemWeights.TryGetValue(r, out var w) || w < 0)
                || shop.ItemWeights.Values.Sum() <= 0)
                Error($"{where}: shop.itemWeights needs a weight of at least 0 for every rarity, not all 0 (D-080).");
            if (shop.RankOdds is null || shop.RankOdds.Count == 0 || shop.RankOdds[0].FromCycle != 1)
                Error($"{where}: rankOdds must start from cycle 1.");
            else
            {
                for (var i = 0; i < shop.RankOdds.Count; i++)
                {
                    var odds = shop.RankOdds[i];
                    if (odds.Odds is null || odds.Odds.Count != UpgradeTrees.MaxRank || odds.Odds.Any(o => o < 0) || odds.Odds.Sum() != 100)
                        Error($"{where}: the rank odds from cycle {odds.FromCycle} need five values summing to 100 (SHP-03).");
                    if (i > 0 && odds.FromCycle <= shop.RankOdds[i - 1].FromCycle)
                        Error($"{where}: rankOdds must be in increasing cycle order.");
                }
            }
        }

        /// <summary>The inn-wide kinds an omen may use (OMN-03): an omen has no fixture or carrier to put the others on.</summary>
        private static readonly EffectKind[] OmenKinds =
        [
            EffectKind.ArrivalAgitation, EffectKind.ArrivalRate, EffectKind.PlaneWeight, EffectKind.Tribute,
            EffectKind.Income, EffectKind.RerollCost,
        ];

        private void Effect(string where, EffectDef e, bool omen = false)
        {
            bool Has(object? value) => value is not null;

            var ok = e.Kind switch
            {
                EffectKind.AgitationRate or EffectKind.Soothe or EffectKind.ArrivalRate => Has(e.Multiplier),
                EffectKind.ArrivalAgitation or EffectKind.Radius or EffectKind.BoardSlots
                    or EffectKind.Income or EffectKind.Supplies or EffectKind.RerollCost or EffectKind.RoomSlots => Has(e.Amount),
                EffectKind.Tribute => Has(e.Amount) || Has(e.Multiplier),

                // An omen's PlaneWeight may leave the plane out: one converging plane is drawn with the omen (D-087).
                EffectKind.PlaneWeight => (Has(e.Plane) || omen) && Has(e.Multiplier),
                EffectKind.Stat => Has(e.Stat) && (Has(e.Amount) || Has(e.Multiplier)),
                EffectKind.ShopOdds => Has(e.Amount) || Has(e.Multiplier),
                EffectKind.Blocks or EffectKind.PlaneBarrier => true,
                EffectKind.Aura => Has(e.Radius) && Has(e.Multiplier),
                EffectKind.Provides => Has(e.Appetite) && Has(e.Seats),
                EffectKind.Enables => Has(e.Technique),
                EffectKind.Emits => Has(e.Trigger) && Has(e.Radius),
                EffectKind.CountsAs => Has(e.Fixture) && Has(e.CountsAs),
                _ => false,
            };

            if (!ok) Error($"{where}: a {e.Kind} effect is missing a parameter it needs (DAT-03).");
            if (e.Plane is { } p && !_planeIds.Contains(p)) Error($"{where}: unknown plane '{p}'.");
            if (e.Technique is { } t && !_techniqueIds.Contains(t)) Error($"{where}: unknown technique '{t}'.");
            if (e.Fixture is { } f && !_fixtureIds.Contains(f)) Error($"{where}: unknown fixture '{f}'.");
            if (e.CountsAs is { } ca && !_fixtureIds.Contains(ca)) Error($"{where}: unknown fixture '{ca}'.");
            if (e.AdjacentTo is { } adj && adj != EffectDef.AdjacentToRoomDoor && !_fixtureIds.Contains(adj))
                Error($"{where}: adjacentTo must be a fixture or {EffectDef.AdjacentToRoomDoor}, not '{adj}' (FIX-03).");
            if (e.Kind == EffectKind.Emits && e.Trigger is not (TriggerKind.Light or TriggerKind.Noise or TriggerKind.Heat or TriggerKind.Cold))
                Error($"{where}: a fixture can only give off Light, Noise, Heat or Cold.");
            if (e.Radius is < 0 || e.Seats is < 1 || e.Zones is < 0) Error($"{where}: a {e.Kind} effect has an out-of-range value.");
            if (e.Kind is EffectKind.ArrivalRate or EffectKind.PlaneWeight or EffectKind.Tribute && e.Multiplier is <= 0)
                Error($"{where}: a {e.Kind} multiplier must be above 0.");
            if (omen && (!OmenKinds.Contains(e.Kind) || e.AdjacentTo is not null || e.Target is not null))
                Error($"{where}: an omen can only use {string.Join(", ", OmenKinds)}, with no adjacentTo or target (OMN-03).");
        }

        private void Omens()
        {
            var ids = new HashSet<string>();
            foreach (var omen in _set.Omens)
            {
                Id("omens.json", omen.Id, ids);
                var where = $"omens.json: omen '{omen.Id}'";
                if (string.IsNullOrWhiteSpace(omen.Name) || string.IsNullOrWhiteSpace(omen.Description))
                    Error($"{where} needs a name and a description (CYC-01).");
                if (omen.Weight <= 0) Error($"{where}: weight must be above 0 (OMN-01).");
                if (omen.Effects is null || omen.Effects.Count == 0) Error($"{where} has no effects.");
                foreach (var effect in omen.Effects ?? []) Effect(where, effect, omen: true);
            }
        }

        /// <summary>
        /// MET-03: every unlock names something that exists, nothing is locked twice, and the base game stays winnable:
        /// the starting planes, staff and fixtures are never locked, and every appetite of the starting planes'
        /// creatures can still be answered by a technique of an unlocked character or by an unlocked fixture.
        /// </summary>
        private void Unlocks()
        {
            var ids = new HashSet<string>();
            var targets = new HashSet<(UnlockKind, string)>();
            var b = _set.Balance;
            foreach (var u in _set.Unlocks)
            {
                Id("unlocks.json", u.Id, ids);
                var where = $"unlocks.json: unlock '{u.Id}'";
                if (u.Cost < 0) Error($"{where}: cost cannot be negative.");
                if (u.Kind == UnlockKind.StartingStaff)
                {
                    if (u.Target is not null) Error($"{where}: a StartingStaff unlock has no target.");
                    if (string.IsNullOrWhiteSpace(u.Name)) Error($"{where}: a StartingStaff unlock needs a name.");
                    if (!targets.Add((u.Kind, ""))) Error($"{where}: only one StartingStaff unlock is allowed.");
                    continue;
                }

                if (u.Target is not { } target)
                {
                    Error($"{where} needs a target.");
                    continue;
                }

                var known = u.Kind switch
                {
                    UnlockKind.Plane => _planeIds.Contains(target),
                    UnlockKind.Character => _characterIds.Contains(target),
                    UnlockKind.Item => _itemIds.Contains(target),
                    UnlockKind.Fixture => _fixtureIds.Contains(target),
                    _ => false,
                };
                if (!known) Error($"{where}: unknown {u.Kind} '{target}'.");
                if (!targets.Add((u.Kind, target))) Error($"{where}: '{target}' is already locked by another unlock.");

                var starting = u.Kind switch
                {
                    UnlockKind.Plane => b.UnlockedPlanes?.Contains(target) == true,
                    UnlockKind.Character => b.StartingStaff?.Contains(target) == true,
                    UnlockKind.Fixture => b.StartingFixtures?.Contains(target) == true,
                    _ => false,
                };
                if (starting) Error($"{where}: '{target}' is part of every run's start and cannot be locked (MET-03).");
            }

            Winnable(targets);
        }

        private void Winnable(HashSet<(UnlockKind Kind, string Target)> locked)
        {
            if (_set.Balance.UnlockedPlanes is null || _set.Staff.Characters is null)
            {
                return;
            }

            var answered = new HashSet<Appetite>();
            foreach (var character in _set.Staff.Characters.Where(c => !locked.Contains((UnlockKind.Character, c.Id))))
            {
                var techniques = new[] { character.BaseTechnique }
                    .Concat((character.Tree ?? []).SelectMany(r => r.Options ?? []).Select(o => o.Technique));
                foreach (var id in techniques)
                {
                    if (id is not null && _techniques.TryGetValue(id, out var technique))
                    {
                        answered.UnionWith(technique.Answers ?? []);
                    }
                }
            }

            foreach (var fixture in _set.Fixtures.Where(f => !locked.Contains((UnlockKind.Fixture, f.Id))))
            {
                answered.UnionWith((fixture.Effects ?? []).Where(e => e.Kind == EffectKind.Provides && e.Appetite is not null)
                    .Select(e => e.Appetite!.Value));
            }

            var basePlanes = _set.Balance.UnlockedPlanes.ToHashSet();
            foreach (var creature in _set.Creatures.Where(c => basePlanes.Contains(c.PlaneId)))
            {
                foreach (var appetite in (creature.Appetites ?? []).Where(a => !answered.Contains(a.Appetite)))
                {
                    Error($"unlocks.json: nothing unlocked at the start answers {appetite.Appetite} for '{creature.Id}' (MET-03).");
                }
            }
        }
    }
}
