using System;
using Godot;
using MistyInn.Core.Settings;

namespace MistyInn.Settings;

/// <summary>
/// The player's settings in <c>user://settings.cfg</c> (SET-01), a Godot <c>ConfigFile</c> outside the saves. A missing
/// or damaged file, or a wrong value, falls back to the defaults (INV-07, D-099). <see cref="Apply"/> sets what the
/// engine owns (audio buses, window, V-Sync, render scale); the game scene reads the rest (camera, filters, mist speed,
/// automatic pause) and listens to <see cref="Changed"/>. It holds no game state (ARC-02).
/// </summary>
public static class SettingsStore
{
    public const string Path = "user://settings.cfg";

    /// <summary>The audio buses of <c>default_bus_layout.tres</c> (AUD-10).</summary>
    public static readonly string[] Buses = ["Master", "Music", "SFX", "Ambience"];

    private static GameSettings? _current;

    /// <summary>Raised after <see cref="Set"/> changes the settings.</summary>
    public static event Action<GameSettings>? Changed;

    public static GameSettings Current => _current ??= Load();

    /// <summary>Changes, applies and saves the settings.</summary>
    public static void Set(GameSettings settings)
    {
        _current = settings.Normalised();
        Apply();
        Save();
        Changed?.Invoke(_current);
    }

    /// <summary>Puts the engine-side settings into effect (SET-02, SET-05).</summary>
    public static void Apply()
    {
        var s = Current;
        SetBus("Master", s.MasterVolume);
        SetBus("Music", s.MusicVolume);
        SetBus("SFX", s.SfxVolume);
        SetBus("Ambience", s.AmbienceVolume);

        // The headless smoke run has no window to change (TST-12).
        if (DisplayServer.GetName() != "headless")
        {
            // Godot's Fullscreen is a borderless window over the whole screen; ExclusiveFullscreen is true fullscreen.
            DisplayServer.WindowSetMode(s.WindowMode switch
            {
                WindowMode.Borderless => DisplayServer.WindowMode.Fullscreen,
                WindowMode.Fullscreen => DisplayServer.WindowMode.ExclusiveFullscreen,
                _ => DisplayServer.WindowMode.Windowed,
            });
            DisplayServer.WindowSetVsyncMode(s.VSync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
        }

        if (Engine.GetMainLoop() is SceneTree tree)
        {
            tree.Root.Scaling3DScale = (float)s.ResolutionScale;
        }
    }

    private static void SetBus(string name, double volume)
    {
        var index = AudioServer.GetBusIndex(name);
        if (index >= 0)
        {
            AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb((float)volume));
            AudioServer.SetBusMute(index, volume <= 0);
        }
    }

    private static GameSettings Load()
    {
        var file = new ConfigFile();
        if (file.Load(Path) != Error.Ok)
        {
            return GameSettings.Default;
        }

        var d = GameSettings.Default;
        return new GameSettings(
            MasterVolume: Number(file, "audio", "master", d.MasterVolume),
            MusicVolume: Number(file, "audio", "music", d.MusicVolume),
            SfxVolume: Number(file, "audio", "sfx", d.SfxVolume),
            AmbienceVolume: Number(file, "audio", "ambience", d.AmbienceVolume),
            CameraPanSpeed: Number(file, "controls", "camera_pan_speed", d.CameraPanSpeed),
            ZoomSpeed: Number(file, "controls", "zoom_speed", d.ZoomSpeed),
            ReducedBlood: Flag(file, "content", "reduced_blood", d.ReducedBlood),
            NoSuggestive: Flag(file, "content", "no_suggestive", d.NoSuggestive),
            WindowMode: Enum.TryParse<WindowMode>(Text(file, "display", "window_mode"), out var mode) ? mode : d.WindowMode,
            VSync: Flag(file, "display", "vsync", d.VSync),
            ResolutionScale: Number(file, "display", "resolution_scale", d.ResolutionScale),
            MistSpeed: (int)Number(file, "gameplay", "mist_speed", d.MistSpeed),
            PauseWhenInnkeeperAttacked: Flag(file, "gameplay", "pause_when_innkeeper_attacked", d.PauseWhenInnkeeperAttacked))
            .Normalised();
    }

    private static void Save()
    {
        var s = Current;
        var file = new ConfigFile();
        file.SetValue("audio", "master", s.MasterVolume);
        file.SetValue("audio", "music", s.MusicVolume);
        file.SetValue("audio", "sfx", s.SfxVolume);
        file.SetValue("audio", "ambience", s.AmbienceVolume);
        file.SetValue("controls", "camera_pan_speed", s.CameraPanSpeed);
        file.SetValue("controls", "zoom_speed", s.ZoomSpeed);
        file.SetValue("content", "reduced_blood", s.ReducedBlood);
        file.SetValue("content", "no_suggestive", s.NoSuggestive);
        file.SetValue("display", "window_mode", s.WindowMode.ToString());
        file.SetValue("display", "vsync", s.VSync);
        file.SetValue("display", "resolution_scale", s.ResolutionScale);
        file.SetValue("gameplay", "mist_speed", s.MistSpeed);
        file.SetValue("gameplay", "pause_when_innkeeper_attacked", s.PauseWhenInnkeeperAttacked);
        if (file.Save(Path) is var error and not Error.Ok)
        {
            GD.PushWarning($"Misty Inn: the settings could not be saved to {Path} ({error}).");
        }
    }

    private static double Number(ConfigFile file, string section, string key, double fallback)
    {
        var value = file.GetValue(section, key, fallback);
        return value.VariantType switch
        {
            Variant.Type.Float => value.AsDouble(),
            Variant.Type.Int => value.AsInt64(),
            _ => fallback,
        };
    }

    private static bool Flag(ConfigFile file, string section, string key, bool fallback)
    {
        var value = file.GetValue(section, key, fallback);
        return value.VariantType == Variant.Type.Bool ? value.AsBool() : fallback;
    }

    private static string Text(ConfigFile file, string section, string key)
    {
        var value = file.GetValue(section, key, "");
        return value.VariantType == Variant.Type.String ? value.AsString() : "";
    }
}
