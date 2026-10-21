using System.Text.Json;
using System.Text.Json.Serialization;

namespace MistyInn.Core.Content;

/// <summary>
/// Reads the content files through an <see cref="IContentSource"/> into a <see cref="ContentSet"/> (DAT-02). Unknown
/// properties and unknown kinds are errors (DAT-04); every file is tried, so one load reports all broken files.
/// </summary>
public static class ContentLoader
{
    public const string Balance = "balance.json";
    public const string Inn = "inn.json";
    public const string Planes = "planes.json";
    public const string Creatures = "creatures.json";
    public const string Techniques = "techniques.json";
    public const string Staff = "staff.json";
    public const string Items = "items.json";
    public const string Fixtures = "fixtures.json";
    public const string Flavour = "flavour.json";
    public const string Synergies = "synergies.json";

    public static IReadOnlyList<string> FileNames { get; } =
        [Balance, Inn, Planes, Creatures, Techniques, Staff, Items, Fixtures, Flavour, Synergies];

    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };

    /// <exception cref="ContentException">A file is missing, is not valid JSON, or does not match its schema.</exception>
    public static ContentSet Load(IContentSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var errors = new List<string>();

        T? Read<T>(string file)
            where T : class
        {
            try
            {
                var value = JsonSerializer.Deserialize<T>(source.ReadText(file), Options);
                if (value is null)
                {
                    errors.Add($"{file}: the file is empty.");
                }

                return value;
            }
            catch (FileNotFoundException)
            {
                errors.Add($"{file}: the file is missing.");
            }
            catch (JsonException e)
            {
                errors.Add($"{file}: {e.Message}");
            }

            return null;
        }

        var balance = Read<BalanceDef>(Balance);
        var inn = Read<InnDef>(Inn);
        var planes = Read<List<PlaneDef>>(Planes);
        var creatures = Read<List<CreatureDef>>(Creatures);
        var techniques = Read<List<TechniqueDef>>(Techniques);
        var staff = Read<StaffFileDef>(Staff);
        var items = Read<List<ItemDef>>(Items);
        var fixtures = Read<List<FixtureDef>>(Fixtures);
        var flavour = Read<List<FlavourDef>>(Flavour);
        var synergies = Read<List<SynergyDef>>(Synergies);

        if (errors.Count > 0)
        {
            throw new ContentException(errors);
        }

        return new ContentSet(balance!, inn!, planes!, creatures!, techniques!, staff!, items!, fixtures!, flavour!, synergies!);
    }
}
