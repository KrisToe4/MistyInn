using System.Collections.Generic;
using Godot;
using MistyInn.Core.Bestiary;
using MistyInn.Core.Content;
using MistyInn.Core.Meta;
using MistyInn.Core.Run;
using MistyInn.Data;

namespace MistyInn.Profile;

/// <summary>What the title screen asks of the next run (UI-01, RUN-01): a seed, or null for a random one, and the
/// chosen starting staff, or null for the default (D-089).</summary>
public sealed record RunRequest(ulong? Seed, IReadOnlyList<string>? StartingStaff);

/// <summary>
/// The profile for this launch of the game: the content, the bestiary, the revealed ancestries and the meta profile
/// (Essence, unlocks, statistics). It outlives the scenes, so the title screen and every run share it (BST-04, MET-01).
/// It lives in memory only; step 8 saves it to <c>user://saves/profile.json</c> (SAV-02, D-091).
/// </summary>
public static class PlayerProfile
{
    private static GameContent? _content;
    private static ContentException? _contentError;

    public static CreatureBestiary Bestiary { get; } = new();

    public static AncestryRecord Ancestry { get; } = new();

    public static MetaProfile Meta { get; } = new();

    /// <summary>Set by the title screen just before it opens the game scene; read once by it.</summary>
    public static RunRequest? PendingRun { get; set; }

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
}
