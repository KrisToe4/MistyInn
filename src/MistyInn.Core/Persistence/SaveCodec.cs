using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MistyInn.Core.Persistence;

/// <summary>
/// Moves a save's JSON forward one schema version at a time (SAV-04): step <c>n</c> turns a version <c>n</c> document
/// into a version <c>n + 1</c> one. The first formats are version 1 and have no steps yet; each later format change adds
/// its step here (D-096).
/// </summary>
public sealed class SaveMigrator
{
    private readonly IReadOnlyDictionary<int, Func<JsonObject, JsonObject>> _steps;

    public SaveMigrator(int currentVersion, IReadOnlyDictionary<int, Func<JsonObject, JsonObject>>? steps = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(currentVersion, 1);
        CurrentVersion = currentVersion;
        _steps = steps ?? new Dictionary<int, Func<JsonObject, JsonObject>>();
    }

    /// <summary>The run save's migrations (<see cref="RunSaves.SchemaVersion"/>).</summary>
    public static SaveMigrator Run { get; } = new(RunSaves.SchemaVersion);

    /// <summary>The profile's migrations (<see cref="ProfileSaves.SchemaVersion"/>).</summary>
    public static SaveMigrator Profile { get; } = new(ProfileSaves.SchemaVersion);

    public int CurrentVersion { get; }

    /// <summary>Applies every step from <paramref name="version"/> to the current version, and sets <c>schemaVersion</c>.</summary>
    /// <exception cref="SaveException">A step is missing (<see cref="SaveProblem.Corrupt"/>).</exception>
    public JsonObject Migrate(JsonObject document, int version)
    {
        for (var v = version; v < CurrentVersion; v++)
        {
            if (!_steps.TryGetValue(v, out var step))
            {
                throw new SaveException(SaveProblem.Corrupt, $"There is no way to update a version {v} save.");
            }

            document = step(document);
            document["schemaVersion"] = v + 1;
        }

        return document;
    }
}

/// <summary>
/// Reads and writes saves as JSON (section 4: <c>System.Text.Json</c>): a version check, the migrations, then the
/// records (SAV-04). Anything that is not a valid save is <see cref="SaveProblem.Corrupt"/> (SAV-06); a newer version is
/// <see cref="SaveProblem.TooNew"/>.
/// </summary>
public static class SaveCodec
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };

    public static string Write(RunSave save) => JsonSerializer.Serialize(save, Options);

    public static string Write(ProfileSave save) => JsonSerializer.Serialize(save, Options);

    /// <exception cref="SaveException">The text is not a run save, or is too new.</exception>
    public static RunSave ReadRun(string text, SaveMigrator? migrator = null) => Read<RunSave>(text, migrator ?? SaveMigrator.Run, "run save");

    /// <exception cref="SaveException">The text is not a profile, or is too new.</exception>
    public static ProfileSave ReadProfile(string text, SaveMigrator? migrator = null) =>
        Read<ProfileSave>(text, migrator ?? SaveMigrator.Profile, "profile");

    private static T Read<T>(string text, SaveMigrator migrator, string what)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(text);
        try
        {
            if (JsonNode.Parse(text) is not JsonObject document)
            {
                throw new SaveException(SaveProblem.Corrupt, $"The {what} is not a JSON object.");
            }

            if (document["schemaVersion"] is not JsonValue v || !v.TryGetValue<int>(out var version) || version < 1)
            {
                throw new SaveException(SaveProblem.Corrupt, $"The {what} has no valid schemaVersion.");
            }

            if (version > migrator.CurrentVersion)
            {
                throw new SaveException(SaveProblem.TooNew,
                    $"The {what} was made by a newer version of Misty Inn (format {version}; this version reads up to {migrator.CurrentVersion}).");
            }

            document = migrator.Migrate(document, version);
            return document.Deserialize<T>(Options) ?? throw new SaveException(SaveProblem.Corrupt, $"The {what} is empty.");
        }
        catch (JsonException e)
        {
            throw new SaveException(SaveProblem.Corrupt, $"The {what} is damaged: {e.Message}");
        }
        catch (Exception e) when (e is FormatException or InvalidOperationException or ArgumentException or NotSupportedException)
        {
            throw new SaveException(SaveProblem.Corrupt, $"The {what} is damaged: {e.Message}");
        }
    }
}
