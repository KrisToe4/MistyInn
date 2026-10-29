using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using MistyInn.Core.Bestiary;
using MistyInn.Core.Content;
using MistyInn.Core.Meta;
using MistyInn.Core.Persistence;
using MistyInn.Core.Run;
using MistyInn.Data;

namespace MistyInn.Profile;

/// <summary>
/// What the title screen asks of the next run (UI-01, RUN-01): a seed, or null for a random one, and the chosen
/// starting staff, or null for the default (D-089). With <paramref name="Save"/>, it is Continue (SAV-03) instead.
/// </summary>
public sealed record RunRequest(ulong? Seed, IReadOnlyList<string>? StartingStaff, RunSave? Save = null);

/// <summary>What Continue would do with the run save (UI-01, D-098).</summary>
/// <param name="Save">The save, when it can be continued.</param>
/// <param name="Problem">Why it cannot, for the player; null when there is no save or it can be continued.</param>
public sealed record RunSaveState(bool Exists, RunSave? Save, string? Problem);

/// <summary>
/// The profile for this launch of the game: the content, the bestiary, the revealed ancestries and the meta profile
/// (Essence, unlocks, statistics). It outlives the scenes, so the title screen and every run share it (BST-04, MET-01).
/// It is read from <c>user://saves/profile.json</c> on first use and written back at every Dawn, when a run ends and
/// when an unlock is bought (SAV-02); the run save is <c>user://saves/run.json</c> (SAV-01). A launch with
/// <c>--new-run</c> (the smoke run) neither reads nor writes them, so it cannot touch the player's saves (D-095).
/// </summary>
public static class PlayerProfile
{
    public const string SaveFolder = "user://saves";

    private static GameContent? _content;
    private static ContentException? _contentError;
    private static LoadedProfile? _loaded;
    private static SaveFiles? _files;
    private static bool _profileWritable = true;

    /// <summary>The profile's problem on loading (quarantined or too new, SAV-04, SAV-06), for the title screen once.</summary>
    public static string? LoadMessage { get; private set; }

    /// <summary>True for the smoke run (<c>--new-run</c>): the save folder is neither read nor written (D-095).</summary>
    public static bool ReadOnly { get; } = OS.GetCmdlineUserArgs().Contains("--new-run");

    public static CreatureBestiary Bestiary => Loaded.Bestiary;

    public static AncestryRecord Ancestry => Loaded.Ancestry;

    public static MetaProfile Meta => Loaded.Meta;

    /// <summary>The first-launch content note has been read (RAT-10, SAV-02).</summary>
    public static bool ContentNoteAcknowledged { get; private set; }

    /// <summary>Set by the title screen just before it opens the game scene; read once by it.</summary>
    public static RunRequest? PendingRun { get; set; }

    private static SaveFiles Files => _files ??= new SaveFiles(new FolderSaveStore(ProjectSettings.GlobalizePath(SaveFolder)));

    private static LoadedProfile Loaded => _loaded ??= LoadProfile();

    /// <summary>
    /// The content from <c>res://data/</c>, loaded once and validated (DAT-02, DAT-04), or null with the error in
    /// <paramref name="error"/>. An export build falls back to the copy built into Core; a debug build reports the error.
    /// </summary>
    public static GameContent? TryContent(out ContentException? error)
    {
        if (_content is null && _contentError is null)
        {
            try
            {
                _content = GameContent.Create(ContentLoader.Load(new GodotContentSource()));
            }
            catch (ContentException e) when (!OS.IsDebugBuild())
            {
                GD.PushError($"Misty Inn: using the built-in content instead of res://data/. {e.Message}");
                _content = GameContent.Default;
            }
            catch (ContentException e)
            {
                GD.PushError(e.Message);
                _contentError = e;
            }
        }

        error = _contentError;
        return _content;
    }

    /// <summary>Takes the pending run request, leaving none.</summary>
    public static RunRequest? TakePendingRun()
    {
        var request = PendingRun;
        PendingRun = null;
        return request;
    }

    /// <summary>The player has read the content note (RAT-10); saved at once.</summary>
    public static void AcknowledgeContentNote()
    {
        ContentNoteAcknowledged = true;
        Save();
    }

    /// <summary>Writes the profile (SAV-02). Returns a message for the player if it could not be written.</summary>
    public static string? Save()
    {
        if (ReadOnly || !_profileWritable)
        {
            return null;
        }

        return Try(() => Files.SaveProfile(ProfileSaves.Capture(Bestiary, Ancestry, Meta, ContentNoteAcknowledged)), "The profile could not be saved");
    }

    /// <summary>
    /// The run save as Continue sees it (UI-01). A corrupt save is quarantined and reported (SAV-06); one that is too
    /// new (SAV-04) or names content this game does not have (D-097) is reported and kept.
    /// </summary>
    public static RunSaveState LoadRunSave(GameContent content)
    {
        if (ReadOnly || !Files.HasRun)
        {
            return new RunSaveState(false, null, null);
        }

        var result = Files.LoadRun();
        switch (result.Status)
        {
            case LoadStatus.Missing:
                return new RunSaveState(false, null, null);
            case LoadStatus.Corrupt:
                return new RunSaveState(false, null, result.Message);
            case LoadStatus.TooNew:
                return new RunSaveState(true, null, result.Message);
        }

        switch (RunSaves.Check(result.Value!, content))
        {
            case null:
                return new RunSaveState(true, result.Value, null);
            case { Problem: SaveProblem.Corrupt } corrupt:
                var moved = Files.QuarantineRun();
                return new RunSaveState(false, null, $"{corrupt.Message} It was moved to {moved}.");
            case { } problem:
                return new RunSaveState(true, null, problem.Message);
        }
    }

    /// <summary>Saves the run as the player leaves Dawn (SAV-01). Returns a message for the player if it failed.</summary>
    public static string? SaveRun(RunSave save) => ReadOnly ? null : Try(() => Files.SaveRun(save), "The run could not be saved");

    /// <summary>The run ended (SAV-06), or was given up for a new one (D-098): its save goes.</summary>
    public static void DeleteRun()
    {
        if (!ReadOnly)
        {
            Try(Files.DeleteRun, "The old run save could not be deleted");
        }
    }

    private static LoadedProfile LoadProfile()
    {
        var empty = new LoadedProfile(new CreatureBestiary(), new AncestryRecord(), new MetaProfile(), ContentNoteAcknowledged: false);
        if (ReadOnly)
        {
            // The smoke run neither reads nor writes the player's files (D-095).
            return empty;
        }

        LoadResult<ProfileSave> result;
        try
        {
            result = Files.LoadProfile();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Never written over: a profile that cannot be read is left alone for this launch (INV-07).
            _profileWritable = false;
            LoadMessage = $"The profile could not be read, so progress will not be saved this time. {e.Message}";
            return empty;
        }

        switch (result.Status)
        {
            case LoadStatus.Loaded:
                try
                {
                    var loaded = ProfileSaves.Restore(result.Value!);
                    ContentNoteAcknowledged = loaded.ContentNoteAcknowledged;
                    return loaded;
                }
                catch (SaveException e)
                {
                    LoadMessage = $"{e.Message} It was moved to {Files.QuarantineProfile()}; a new profile starts.";
                    return empty;
                }

            case LoadStatus.Corrupt:
                LoadMessage = $"{result.Message} A new profile starts.";
                return empty;
            case LoadStatus.TooNew:
                // Kept as it is, and not written over this launch (SAV-04, INV-07).
                _profileWritable = false;
                LoadMessage = $"{result.Message} Progress will not be saved until you play that version again.";
                return empty;
            default:
                return empty;
        }
    }

    private static string? Try(Action write, string what)
    {
        try
        {
            write();
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            GD.PushError($"Misty Inn: {what}: {e.Message}");
            return $"{what}: {e.Message}";
        }
    }
}
