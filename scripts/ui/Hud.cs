using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MistyInn.Core;
using MistyInn.Core.Bestiary;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Run;

namespace MistyInn.UI;

/// <summary>
/// Placeholder HUD: cycle, phase and time to dawn (CYC-02), silver, supplies and integrity (BRD-06), the forecast with
/// what the bestiary knows (CYC-01, BST-03), the bench and the fixture storage to drag from (CAM-13), the build menu
/// (FIX-05, FIX-06), Begin the Mist, speed controls (SCP-13), Continue and integrity repairs at Dawn (CYC-04, BRD-07),
/// the run-over panel (RUN-03), the selection line (CAM-12), the pause menu (CYC-10) and, in debug builds only, the dev
/// panel (D-074). Built in code; the UI pass (step 9) replaces it with designed screens.
/// </summary>
public partial class Hud : CanvasLayer
{
    private Label _status = null!;
    private Label _purse = null!;
    private Label _forecast = null!;
    private Label _slots = null!;
    private Label _seed = null!;
    private Label _message = null!;
    private Label _selection = null!;
    private Label _hover = null!;
    private Label _drag = null!;
    private PanelContainer _benchPanel = null!;
    private VBoxContainer _benchList = null!;
    private VBoxContainer _storageList = null!;
    private PanelContainer _buildPanel = null!;
    private VBoxContainer _buildList = null!;
    private Button _beginMist = null!;
    private Button _continue = null!;
    private HBoxContainer _speedBar = null!;
    private Control _pauseMenu = null!;
    private HBoxContainer _repairBar = null!;
    private Button _repairAll = null!;
    private Control _runOver = null!;
    private Label _runOverText = null!;
    private PanelContainer _devPanel = null!;
    private VBoxContainer _devList = null!;
    private bool _devBuilt;
    private string _benchSignature = "";
    private string _storageSignature = "";
    private bool _buildMenuBuilt;
    private double _messageSeconds;

    public event Action? BeginMistPressed;

    public event Action? ContinuePressed;

    public event Action<int>? SpeedPressed;

    public event Action<UnitId>? BenchDragStarted;

    public event Action<FixtureId>? StorageDragStarted;

    public event Action<FixtureId>? SellPressed;

    public event Action<string>? BuildPressed;

    public event Action<Supply>? BuySupplyPressed;

    public event Action? ResumePressed;

    public event Action? QuitPressed;

    /// <summary>Spend up to this much silver on integrity (BRD-07).</summary>
    public event Action<int>? RepairPressed;

    public event Action? NewRunPressed;

    public event Action<string>? DevAddStaffPressed;

    public event Action<string>? DevPlanePressed;

    public event Action<ContentFilters>? FiltersChanged;

    public bool IsPauseMenuOpen => _pauseMenu.Visible;

    public override void _Ready()
    {
        var info = new VBoxContainer { Position = new Vector2(16, 12), MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(info);
        _status = AddLabel(info, 22);
        _purse = AddLabel(info, 17, new Color(0.95f, 0.85f, 0.55f));
        _forecast = AddLabel(info, 16);
        _slots = AddLabel(info, 16);
        _seed = AddLabel(info, 14, new Color(0.7f, 0.7f, 0.75f));
        _message = AddLabel(info, 17, new Color(1f, 0.72f, 0.45f));
        _selection = AddLabel(info, 17, new Color(0.8f, 0.9f, 1f));
        _hover = AddLabel(info, 15, new Color(0.75f, 0.8f, 0.85f));

        _benchPanel = new PanelContainer();
        AddChild(_benchPanel);
        _benchPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft, Control.LayoutPresetMode.Minsize, 16);
        _benchPanel.GrowVertical = Control.GrowDirection.Begin;
        var benchBox = new VBoxContainer();
        _benchPanel.AddChild(benchBox);
        AddLabel(benchBox, 16).Text = Tr("Bench: drag onto the inn floor");
        _benchList = new VBoxContainer();
        benchBox.AddChild(_benchList);
        AddLabel(benchBox, 16).Text = Tr("Storage: drag onto the floor (R rotates)");
        _storageList = new VBoxContainer();
        benchBox.AddChild(_storageList);

        _buildPanel = new PanelContainer();
        AddChild(_buildPanel);
        _buildPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight, Control.LayoutPresetMode.Minsize, 16);
        _buildPanel.GrowHorizontal = Control.GrowDirection.Begin;
        var buildBox = new VBoxContainer();
        _buildPanel.AddChild(buildBox);
        AddLabel(buildBox, 16).Text = Tr("Build menu");
        _buildList = new VBoxContainer();
        buildBox.AddChild(_buildList);

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

        _repairBar = new HBoxContainer();
        actions.AddChild(_repairBar);
        AddButton(_repairBar, Tr("Repair 5 integrity (1 silver)")).Pressed += () => RepairPressed?.Invoke(1);
        _repairAll = AddButton(_repairBar, Tr("Repair all"));
        _repairAll.Pressed += () => RepairPressed?.Invoke(int.MaxValue);

        _beginMist = AddButton(actions, Tr("Begin the Mist"));
        _beginMist.Pressed += () => BeginMistPressed?.Invoke();
        _continue = AddButton(actions, Tr("Continue to Dusk"));
        _continue.Pressed += () => ContinuePressed?.Invoke();

        _drag = new Label { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false, TopLevel = true };
        _drag.AddThemeFontSizeOverride("font_size", 18);
        AddChild(_drag);

        _pauseMenu = BuildPauseMenu();
        AddChild(_pauseMenu);
        _runOver = BuildRunOver();
        AddChild(_runOver);

        _devPanel = new PanelContainer { Visible = false };
        AddChild(_devPanel);
        _devPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterRight, Control.LayoutPresetMode.Minsize, 16);
        _devPanel.GrowHorizontal = Control.GrowDirection.Begin;
        var devScroll = new ScrollContainer { CustomMinimumSize = new Vector2(240, 360) };
        _devPanel.AddChild(devScroll);
        _devList = new VBoxContainer();
        devScroll.AddChild(_devList);
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
            Phase.Dawn => Tr("Dawn"),
            _ => Tr("The run is over"),
        };

        var status = $"{Tr("Cycle")} {session.Cycle}  ·  {phase}";
        if (session.Phase == Phase.Mist)
        {
            var speed = session.Speed == 0 ? Tr("paused") : $"{session.Speed}×";
            status += $"  ·  {Tr("dawn in")} {session.TimeToDawn:m\\:ss}  ·  {speed}";
        }

        _status.Text = status;
        _purse.Text = $"{Tr("Silver")} {session.Silver}  ·  {Tr("Drink")} {session.Supplies[Supply.Drink]}  ·  {Tr("Food")} {session.Supplies[Supply.Food]}"
                      + $"  ·  {Tr("Integrity")} {Mathf.CeilToInt(session.Integrity)}/{Mathf.CeilToInt(session.MaxIntegrity)}";
        _forecast.Text = Forecast(session);
        _slots.Text = $"{Tr("Staff on the floor")}: {session.StaffOnBoard}/{session.MaxStaffOnBoard}";

        var dusk = session.Phase == Phase.Dusk;
        _benchPanel.Visible = dusk;
        _buildPanel.Visible = dusk;
        _beginMist.Visible = dusk;
        _speedBar.Visible = session.Phase == Phase.Mist;
        _continue.Visible = session.Phase == Phase.Dawn;
        var missing = session.MaxIntegrity - session.Integrity;
        _repairBar.Visible = session.Phase == Phase.Dawn && missing > 0;
        _repairAll.Text = $"{Tr("Repair all")} ({Mathf.CeilToInt(missing / session.Content.Balance.Combat.IntegrityPerSilver)} {Tr("silver")})";
        _devPanel.Visible = dusk && OS.IsDebugBuild();
        DevPanelOnce(session);

        RefreshBench(session.Units);
        RefreshStorage(session.Fixtures);
        BuildMenuOnce(session);
    }

    public void ShowMessage(string text, double seconds = 4)
    {
        _message.Text = text;
        _messageSeconds = seconds;
    }

    public void SetSelection(string? text) => _selection.Text = text ?? "";

    /// <summary>The tile under the mouse and its zone, or nothing off the board.</summary>
    public void SetHoverTile(string? text) => _hover.Text = text ?? "";

    /// <summary>Shows what is being dragged next to the mouse, or hides it.</summary>
    public void SetDragLabel(string? text)
    {
        _drag.Text = text ?? "";
        _drag.Visible = text is not null;
    }

    /// <summary>The bench and storage panel: dropping a unit there benches it, a fixture stores it.</summary>
    public bool IsOverBench(Vector2 screenPosition) =>
        _benchPanel.Visible && _benchPanel.GetGlobalRect().HasPoint(screenPosition);

    public void ShowPauseMenu(bool open) => _pauseMenu.Visible = open;

    /// <summary>The run is lost (RUN-03): says why, and offers a new run.</summary>
    public void ShowRunOver(string text)
    {
        _runOverText.Text = text;
        _runOver.Visible = true;
    }

    /// <summary>Hides the game UI and shows an error that stops the game (DAT-04, debug builds).</summary>
    public void ShowFatal(string text)
    {
        foreach (var child in GetChildren())
        {
            if (child is CanvasItem item)
            {
                item.Visible = false;
            }
        }

        var panel = new PanelContainer();
        AddChild(panel);
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.KeepSize, 40);
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            VerticalAlignment = VerticalAlignment.Top,
        };
        label.AddThemeFontSizeOverride("font_size", 18);
        label.AddThemeColorOverride("font_color", new Color(1f, 0.6f, 0.5f));
        panel.AddChild(label);
    }

    /// <summary>The forecast (BST-03): each converging plane and what is known of the creatures it may send.</summary>
    private string Forecast(GameSession session)
    {
        var lines = new List<string> { Tr("The mist parts toward:") };
        foreach (var plane in session.Forecast)
        {
            var creatures = plane.Creatures.Select(c =>
            {
                if (c.Level == BestiaryLevel.Unknown)
                {
                    return "?";
                }

                var text = c.Name!;
                if (c.Appetites is { } appetites)
                {
                    text += $" (wants {string.Join(", ", appetites.Select(a => a.Strength == AppetiteStrength.Craves ? a.Appetite.ToString() : $"{a.Appetite}?"))})";
                }

                if (c.Triggers is { } triggers)
                {
                    text += $" [hates {string.Join(", ", triggers.Select(t => t.Trigger))}]";
                }

                return text;
            });
            lines.Add($"  {plane.Plane.Name}: {string.Join("; ", creatures)}");
        }

        return string.Join("\n", lines);
    }

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

    private void RefreshStorage(IReadOnlyList<FixtureSnapshot> fixtures)
    {
        var stored = fixtures.Where(f => f.IsInStorage).ToList();
        var signature = string.Join(",", stored.Select(f => f.Id.Value));
        if (signature == _storageSignature)
        {
            return;
        }

        _storageSignature = signature;
        foreach (var child in _storageList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var fixture in stored)
        {
            var row = new HBoxContainer();
            _storageList.AddChild(row);
            var id = fixture.Id;
            var drag = AddButton(row, fixture.Name);
            drag.ButtonDown += () => StorageDragStarted?.Invoke(id);
            var sell = AddButton(row, Tr("Sell"));
            sell.Pressed += () => SellPressed?.Invoke(id);
        }

        if (stored.Count == 0)
        {
            AddLabel(_storageList, 15).Text = Tr("(nothing in storage)");
        }
    }

    private void BuildMenuOnce(GameSession session)
    {
        if (_buildMenuBuilt)
        {
            return;
        }

        _buildMenuBuilt = true;
        foreach (var fixture in session.Content.Fixtures)
        {
            var id = fixture.Id;
            AddButton(_buildList, $"{fixture.Name}  ({fixture.Cost})").Pressed += () => BuildPressed?.Invoke(id);
        }

        foreach (var (supply, bundle) in session.Content.Balance.Supplies)
        {
            var s = supply;
            AddButton(_buildList, $"{bundle.Amount} {supply}  ({bundle.Price})").Pressed += () => BuySupplyPressed?.Invoke(s);
        }
    }

    /// <summary>
    /// Debug builds only (D-074): add any character to the bench, choose the converging planes, and try the content
    /// filters (SET-04) until the settings screen arrives in step 8.
    /// </summary>
    private void DevPanelOnce(GameSession session)
    {
        if (_devBuilt || !OS.IsDebugBuild())
        {
            return;
        }

        _devBuilt = true;
        AddLabel(_devList, 16).Text = Tr("Dev panel (debug builds)");

        var blood = new CheckButton { Text = Tr("Reduced blood") };
        var hosting = new CheckButton { Text = Tr("No suggestive content (Hosting)") };
        void Changed(bool _) => FiltersChanged?.Invoke(new ContentFilters(blood.ButtonPressed, hosting.ButtonPressed));
        blood.Toggled += Changed;
        hosting.Toggled += Changed;
        _devList.AddChild(blood);
        _devList.AddChild(hosting);

        AddLabel(_devList, 15).Text = Tr("Converge only:");
        foreach (var plane in session.Content.Planes)
        {
            var id = plane.Id;
            AddButton(_devList, plane.Name).Pressed += () => DevPlanePressed?.Invoke(id);
        }

        AddLabel(_devList, 15).Text = Tr("Add to the bench:");
        foreach (var character in session.Content.Characters)
        {
            var id = character.Id;
            AddButton(_devList, $"{character.Name} ({session.TechniqueName(character.BaseTechnique)})").Pressed +=
                () => DevAddStaffPressed?.Invoke(id);
        }
    }

    private Control BuildRunOver()
    {
        var center = new CenterContainer { Visible = false };
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        center.MouseFilter = Control.MouseFilterEnum.Stop;

        var panel = new PanelContainer();
        center.AddChild(panel);
        var box = new VBoxContainer();
        panel.AddChild(box);
        var title = AddLabel(box, 26);
        title.Text = Tr("The inn has fallen");
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _runOverText = AddLabel(box, 18);
        _runOverText.HorizontalAlignment = HorizontalAlignment.Center;

        AddButton(box, Tr("New run")).Pressed += () => NewRunPressed?.Invoke();
        AddButton(box, Tr("Quit")).Pressed += () => QuitPressed?.Invoke();
        return center;
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
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 32) };
        button.AddThemeFontSizeOverride("font_size", 16);
        parent.AddChild(button);
        return button;
    }
}
