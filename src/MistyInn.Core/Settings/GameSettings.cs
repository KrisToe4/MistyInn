using MistyInn.Core.Run;

namespace MistyInn.Core.Settings;

/// <summary>How the game window is shown (SET-05).</summary>
public enum WindowMode
{
    Windowed,
    Borderless,
    Fullscreen,
}

/// <summary>
/// The player's settings (SET-01 to SET-06). They live in <c>user://settings.cfg</c>, outside the saves, and never
/// change what happens in a run: the content filters change only what is shown (CMP-06), and the mist speed and the
/// automatic pause only how it is watched (SET-06). The game reads and writes them with Godot's <c>ConfigFile</c>;
/// <see cref="Normalised"/> keeps a hand-edited or damaged file from breaking anything (INV-07, D-099).
/// </summary>
/// <param name="CameraPanSpeed">A multiplier on the camera's pan speed (SET-03).</param>
/// <param name="ZoomSpeed">A multiplier on the zoom step per wheel notch (SET-03).</param>
/// <param name="ResolutionScale">The 3D render scale, 0.5 to 1 (SET-05).</param>
/// <param name="MistSpeed">The speed each mist starts at: 1, 2 or 4 (SET-06).</param>
/// <param name="PauseWhenInnkeeperAttacked">Pause the mist the first time the innkeeper is hit in it (SET-06).</param>
public sealed record GameSettings(
    double MasterVolume = 1,
    double MusicVolume = 0.8,
    double SfxVolume = 1,
    double AmbienceVolume = 0.8,
    double CameraPanSpeed = 1,
    double ZoomSpeed = 1,
    bool ReducedBlood = false,
    bool NoSuggestive = false,
    WindowMode WindowMode = WindowMode.Windowed,
    bool VSync = true,
    double ResolutionScale = 1,
    int MistSpeed = 1,
    bool PauseWhenInnkeeperAttacked = true)
{
    public const double MinMultiplier = 0.25;
    public const double MaxMultiplier = 3;
    public const double MinResolutionScale = 0.5;

    /// <summary>The speeds a mist may start at; pause is not one (SCP-13).</summary>
    public static IReadOnlyList<int> MistSpeeds { get; } = [1, 2, 4];

    public static GameSettings Default { get; } = new();

    /// <summary>The content filters as Core's command takes them (SET-04, RAT-09).</summary>
    public ContentFilters Filters => new(ReducedBlood, NoSuggestive);

    /// <summary>
    /// The same settings with every value in range: volumes 0 to 1, multipliers 0.25 to 3, the render scale 0.5 to 1,
    /// a known window mode, and a mist speed of 1, 2 or 4. Values that are not numbers fall back to the defaults.
    /// </summary>
    public GameSettings Normalised() => this with
    {
        MasterVolume = Clamp(MasterVolume, 0, 1, Default.MasterVolume),
        MusicVolume = Clamp(MusicVolume, 0, 1, Default.MusicVolume),
        SfxVolume = Clamp(SfxVolume, 0, 1, Default.SfxVolume),
        AmbienceVolume = Clamp(AmbienceVolume, 0, 1, Default.AmbienceVolume),
        CameraPanSpeed = Clamp(CameraPanSpeed, MinMultiplier, MaxMultiplier, Default.CameraPanSpeed),
        ZoomSpeed = Clamp(ZoomSpeed, MinMultiplier, MaxMultiplier, Default.ZoomSpeed),
        WindowMode = Enum.IsDefined(WindowMode) ? WindowMode : Default.WindowMode,
        ResolutionScale = Clamp(ResolutionScale, MinResolutionScale, 1, Default.ResolutionScale),
        MistSpeed = MistSpeeds.Contains(MistSpeed) ? MistSpeed : Default.MistSpeed,
    };

    private static double Clamp(double value, double min, double max, double fallback) =>
        double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
}
