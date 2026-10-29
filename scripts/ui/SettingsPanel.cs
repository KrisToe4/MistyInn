using System;
using Godot;
using MistyInn.Core.Settings;
using MistyInn.Settings;
using static MistyInn.UI.UiKit;

namespace MistyInn.UI;

/// <summary>
/// The placeholder Settings page (SET-02 to SET-06, D-099), shared by the title screen (UI-01) and the pause menu
/// (CYC-10). Every change is applied and saved at once through <see cref="SettingsStore"/>. The UI pass (step 9)
/// restyles it.
/// </summary>
public partial class SettingsPanel : CenterContainer
{
    private static readonly string[] WindowModes = ["Windowed", "Borderless", "Fullscreen"];

    private bool _building;

    /// <summary>The player pressed Back.</summary>
    public event Action? Closed;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        var panel = new PanelContainer();
        AddChild(panel);
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(620, 0) };
        box.AddThemeConstantOverride("separation", 6);
        panel.AddChild(box);

        var title = AddLabel(box, 30, new Color(0.85f, 0.9f, 1f));
        title.Text = Tr("Settings");

        _building = true;
        var s = SettingsStore.Current;

        Heading(box, Tr("Audio"));
        Slider(box, Tr("Master volume"), s.MasterVolume, 0, 1, v => SettingsStore.Current with { MasterVolume = v });
        Slider(box, Tr("Music volume"), s.MusicVolume, 0, 1, v => SettingsStore.Current with { MusicVolume = v });
        Slider(box, Tr("Sound effects volume"), s.SfxVolume, 0, 1, v => SettingsStore.Current with { SfxVolume = v });
        Slider(box, Tr("Ambience volume"), s.AmbienceVolume, 0, 1, v => SettingsStore.Current with { AmbienceVolume = v });

        Heading(box, Tr("Controls"));
        Slider(box, Tr("Camera pan speed"), s.CameraPanSpeed, GameSettings.MinMultiplier, GameSettings.MaxMultiplier,
            v => SettingsStore.Current with { CameraPanSpeed = v });
        Slider(box, Tr("Zoom speed"), s.ZoomSpeed, GameSettings.MinMultiplier, GameSettings.MaxMultiplier,
            v => SettingsStore.Current with { ZoomSpeed = v });

        Heading(box, Tr("Content"));
        Toggle(box, Tr("Reduced blood (hits flash instead)"), s.ReducedBlood, on => SettingsStore.Current with { ReducedBlood = on });
        Toggle(box, Tr("Suggestive content"), !s.NoSuggestive, on => SettingsStore.Current with { NoSuggestive = !on });

        Heading(box, Tr("Display"));
        var mode = Row(box, Tr("Window"));
        var modes = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var name in WindowModes)
        {
            modes.AddItem(Tr(name));
        }

        modes.Select((int)s.WindowMode);
        modes.ItemSelected += i => Change(SettingsStore.Current with { WindowMode = (WindowMode)(int)i });
        mode.AddChild(modes);
        Toggle(box, Tr("V-Sync"), s.VSync, on => SettingsStore.Current with { VSync = on });
        Slider(box, Tr("Resolution scale"), s.ResolutionScale, GameSettings.MinResolutionScale, 1,
            v => SettingsStore.Current with { ResolutionScale = v });

        Heading(box, Tr("Gameplay"));
        var speed = Row(box, Tr("Mist speed at the start"));
        var speeds = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var each in GameSettings.MistSpeeds)
        {
            speeds.AddItem($"{each}×");
            speeds.SetItemMetadata(speeds.ItemCount - 1, each);
            if (each == s.MistSpeed)
            {
                speeds.Select(speeds.ItemCount - 1);
            }
        }

        speeds.ItemSelected += i => Change(SettingsStore.Current with { MistSpeed = speeds.GetItemMetadata((int)i).AsInt32() });
        speed.AddChild(speeds);
        Toggle(box, Tr("Pause when the innkeeper is attacked"), s.PauseWhenInnkeeperAttacked,
            on => SettingsStore.Current with { PauseWhenInnkeeperAttacked = on });

        AddButton(box, Tr("Back")).Pressed += () => Closed?.Invoke();
        _building = false;
    }

    private void Change(GameSettings settings)
    {
        if (!_building)
        {
            SettingsStore.Set(settings);
        }
    }

    private static void Heading(Control parent, string text) => AddLabel(parent, 20, new Color(0.75f, 0.85f, 1f)).Text = text;

    private static HBoxContainer Row(Control parent, string text)
    {
        var row = new HBoxContainer();
        parent.AddChild(row);
        var label = AddLabel(row, 16);
        label.Text = text;
        label.CustomMinimumSize = new Vector2(300, 0);
        return row;
    }

    private void Slider(Control parent, string text, double value, double min, double max, Func<double, GameSettings> change)
    {
        var row = Row(parent, text);
        var slider = new HSlider
        {
            MinValue = min,
            MaxValue = max,
            Step = 0.05,
            Value = value,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        var shown = AddLabel(row, 16);
        shown.CustomMinimumSize = new Vector2(60, 0);
        shown.Text = $"{value:0.00}";
        slider.ValueChanged += v =>
        {
            shown.Text = $"{v:0.00}";
            Change(change(v));
        };
        row.AddChild(slider);
        row.MoveChild(slider, 1);
    }

    private void Toggle(Control parent, string text, bool on, Func<bool, GameSettings> change)
    {
        var box = new CheckBox { Text = text, ButtonPressed = on };
        box.AddThemeFontSizeOverride("font_size", 16);
        box.Toggled += pressed => Change(change(pressed));
        parent.AddChild(box);
    }
}
