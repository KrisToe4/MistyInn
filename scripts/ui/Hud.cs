using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MistyInn.Core;
using MistyInn.Core.Entities;
using MistyInn.Core.Run;

namespace MistyInn.UI;

/// <summary>
/// Placeholder HUD for step 2: cycle, phase and time to dawn (CYC-02), the forecast (CYC-01), the bench to drag staff
/// from (CAM-13), Begin the Mist, speed controls (SCP-13), Continue at Dawn (CYC-04), the selection line (CAM-12) and
/// the pause menu (CYC-10). Built in code; the UI pass (step 9) replaces it with designed screens.
/// </summary>
public partial class Hud : CanvasLayer
{
    private Label _status = null!;
    private Label _forecast = null!;
    private Label _slots = null!;
    private Label _seed = null!;
    private Label _message = null!;
    private Label _selection = null!;
    private Label _drag = null!;
    private PanelContainer _benchPanel = null!;
    private VBoxContainer _benchList = null!;
    private Button _beginMist = null!;
    private Button _continue = null!;
    private HBoxContainer _speedBar = null!;
    private Control _pauseMenu = null!;
    private string _benchSignature = "";
    private double _messageSeconds;

    public event Action? BeginMistPressed;

    public event Action? ContinuePressed;

    public event Action<int>? SpeedPressed;

    public event Action<UnitId>? BenchDragStarted;

    public event Action? ResumePressed;

    public event Action? QuitPressed;

    public bool IsPauseMenuOpen => _pauseMenu.Visible;

    public override void _Ready()
    {
        var info = new VBoxContainer { Position = new Vector2(16, 12), MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(info);
        _status = AddLabel(info, 22);
        _forecast = AddLabel(info, 17);
        _slots = AddLabel(info, 16);
        _seed = AddLabel(info, 14, new Color(0.7f, 0.7f, 0.75f));
        _message = AddLabel(info, 17, new Color(1f, 0.72f, 0.45f));
        _selection = AddLabel(info, 17, new Color(0.8f, 0.9f, 1f));

        _benchPanel = new PanelContainer();
        AddChild(_benchPanel);
        _benchPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft, Control.LayoutPresetMode.Minsize, 16);
        _benchPanel.GrowVertical = Control.GrowDirection.Begin;
        var benchBox = new VBoxContainer();
        _benchPanel.AddChild(benchBox);
        AddLabel(benchBox, 16).Text = Tr("Bench: drag onto the inn floor");
        _benchList = new VBoxContainer();
        benchBox.AddChild(_benchList);

        var actions = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(actions);
        actions.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight, Control.LayoutPresetMode.Minsize, 16);
        actions.GrowHorizontal = Control.GrowDirection.Begin;
        actions.GrowVertical = Control.GrowDirection.Begin;

        _speedBar = new HBoxContainer();
        actions.AddChild(_speedBar);
        foreach (var (label, speed) in new[] { ("Pause", 0), ("1×", 1), ("2×", 2), ("4×", 4) })
        {
            var button = AddButton(_speedBar, Tr(label));
            button.Pressed += () => SpeedPressed?.Invoke(speed);
        }

        _beginMist = AddButton(actions, Tr("Begin the Mist"));
        _beginMist.Pressed += () => BeginMistPressed?.Invoke();
        _continue = AddButton(actions, Tr("Continue to Dusk"));
        _continue.Pressed += () => ContinuePressed?.Invoke();

        _drag = new Label { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false, TopLevel = true };
        _drag.AddThemeFontSizeOverride("font_size", 18);
        AddChild(_drag);

        _pauseMenu = BuildPauseMenu();
        AddChild(_pauseMenu);
    }

    public override void _Process(double delta)
    {
        if (_drag.Visible)
        {
            _drag.Position = GetViewport().GetMousePosition() + new Vector2(18, 12);
        }

        if (_messageSeconds > 0)
        {
            _messageSeconds -= delta;
            if (_messageSeconds <= 0)
            {
                _message.Text = "";
            }
        }
    }

    public void SetSeed(ulong seed) => _seed.Text = $"{Tr("Seed")} {seed}";

    /// <summary>Updates every label and button from the session's read-only queries.</summary>
    public void Refresh(GameSession session)
    {
        var phase = session.Phase switch
        {
            Phase.Dusk => Tr("Dusk"),
            Phase.Mist => Tr("The Mist"),
            _ => Tr("Dawn"),
        };

        var status = $"{Tr("Cycle")} {session.Cycle}  ·  {phase}";
        if (session.Phase == Phase.Mist)
        {
            var speed = session.Speed == 0 ? Tr("paused") : $"{session.Speed}×";
            status += $"  ·  {Tr("dawn in")} {session.TimeToDawn:m\\:ss}  ·  {speed}";
        }

        _status.Text = status;
        _forecast.Text = $"{Tr("The mist parts toward")}: {string.Join(", ", session.ConvergingPlanes.Select(p => p.Name))}";
        _slots.Text = $"{Tr("Staff on the floor")}: {session.StaffOnBoard}/{session.MaxStaffOnBoard}";

        _benchPanel.Visible = session.Phase == Phase.Dusk;
        _beginMist.Visible = session.Phase == Phase.Dusk;
        _speedBar.Visible = session.Phase == Phase.Mist;
        _continue.Visible = session.Phase == Phase.Dawn;

        RefreshBench(session.Units);
    }

    public void ShowMessage(string text, double seconds = 4)
    {
        _message.Text = text;
        _messageSeconds = seconds;
    }

    public void SetSelection(string? text) => _selection.Text = text ?? "";

    /// <summary>Shows what is being dragged next to the mouse, or hides it.</summary>
    public void SetDragLabel(string? text)
    {
        _drag.Text = text ?? "";
        _drag.Visible = text is not null;
    }

    public bool IsOverBench(Vector2 screenPosition) =>
        _benchPanel.Visible && _benchPanel.GetGlobalRect().HasPoint(screenPosition);

    public void ShowPauseMenu(bool open) => _pauseMenu.Visible = open;

    private void RefreshBench(IReadOnlyList<UnitSnapshot> units)
    {
        var benched = units.Where(u => u.IsOnBench).ToList();
        var signature = string.Join(",", benched.Select(u => u.Id.Value));
        if (signature == _benchSignature)
        {
            return;
        }

        _benchSignature = signature;
        foreach (var child in _benchList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var unit in benched)
        {
            var button = AddButton(_benchList, unit.Name);
            var id = unit.Id;
            button.ButtonDown += () => BenchDragStarted?.Invoke(id);
        }

        if (benched.Count == 0)
        {
            AddLabel(_benchList, 15).Text = Tr("(everyone is on the floor)");
        }
    }

    private Control BuildPauseMenu()
    {
        var center = new CenterContainer { Visible = false };
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        center.MouseFilter = Control.MouseFilterEnum.Stop;

        var panel = new PanelContainer();
        center.AddChild(panel);
        var box = new VBoxContainer();
        panel.AddChild(box);
        var title = AddLabel(box, 26);
        title.Text = Tr("Paused");
        title.HorizontalAlignment = HorizontalAlignment.Center;

        var resume = AddButton(box, Tr("Resume"));
        resume.Pressed += () => ResumePressed?.Invoke();
        var quit = AddButton(box, Tr("Quit (the current cycle is lost)"));
        quit.Pressed += () => QuitPressed?.Invoke();
        return center;
    }

    private static Label AddLabel(Control parent, int fontSize, Color? color = null)
    {
        var label = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 6);
        if (color is { } c)
        {
            label.AddThemeColorOverride("font_color", c);
        }

        parent.AddChild(label);
        return label;
    }

    private static Button AddButton(Control parent, string text)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 36) };
        button.AddThemeFontSizeOverride("font_size", 18);
        parent.AddChild(button);
        return button;
    }
}
