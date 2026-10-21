using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MistyInn.Core.Bestiary;
using MistyInn.Core.Content;
using MistyInn.Core.Meta;
using MistyInn.Profile;
using static MistyInn.UI.UiKit;

namespace MistyInn.UI;

/// <summary>
/// Placeholder title screen (UI-01): New Run with an optional seed (RUN-01) and, once unlocked, a choice of starting
/// staff (MET-03, D-089); the Bestiary page (BST-01, BST-04) and the Unlocks page, where Mist Essence buys unlocks
/// (MET-03); and Quit. Continue and Settings arrive with saves in step 8; the UI pass (step 9) replaces this screen.
/// Started with the user argument <c>--new-run</c> (after <c>++</c>), it opens a run at once, for the smoke run (TST-12).
/// </summary>
public partial class TitleScreen : Control
{
    public const string ScenePath = "res://scenes/Title.tscn";

    private const string GameScenePath = "res://scenes/Main.tscn";

    private GameContent _content = null!;
    private Label _essence = null!;
    private Label _statistics = null!;
    private Label _message = null!;
    private LineEdit _seed = null!;
    private HBoxContainer _staffRow = null!;
    private readonly List<OptionButton> _staffPickers = new();
    private Control _menu = null!;
    private Control _bestiaryPage = null!;
    private VBoxContainer _bestiaryList = null!;
    private Control _unlocksPage = null!;
    private VBoxContainer _unlocksList = null!;
    private Label _unlocksEssence = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var background = new ColorRect { Color = new Color(0.06f, 0.07f, 0.1f), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(background);
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        if (PlayerProfile.TryContent(out var error) is not { } content)
        {
            var center = Centered();
            var box = new VBoxContainer();
            center.AddChild(box);
            var label = AddLabel(box, 18, new Color(1f, 0.6f, 0.5f));
            label.Text = $"{Tr("The game data in res://data/ is invalid")}:\n\n{error?.Message}";
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            label.CustomMinimumSize = new Vector2(900, 0);
            AddButton(box, Tr("Quit")).Pressed += () => GetTree().Quit();
            return;
        }

        _content = content;
        _menu = BuildMenu();
        _bestiaryPage = BuildPage(Tr("Bestiary"), out _bestiaryList, out _);
        _unlocksPage = BuildPage(Tr("Unlocks"), out _unlocksList, out _unlocksEssence);
        ShowPage(_menu);
        RefreshMenu();

        if (OS.GetCmdlineUserArgs().Contains("--new-run"))
        {
            Callable.From(StartRun).CallDeferred();
        }
    }

    private CenterContainer Centered()
    {
        var center = new CenterContainer();
        AddChild(center);
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        return center;
    }

    private Control BuildMenu()
    {
        var center = Centered();
        var panel = new PanelContainer();
        center.AddChild(panel);
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(520, 0) };
        box.AddThemeConstantOverride("separation", 10);
        panel.AddChild(box);

        var title = AddLabel(box, 44, new Color(0.85f, 0.9f, 1f));
        title.Text = Tr("Misty Inn");
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _essence = AddLabel(box, 18, new Color(0.75f, 0.85f, 1f));
        _statistics = AddLabel(box, 16, new Color(0.7f, 0.7f, 0.75f));

        var seedRow = new HBoxContainer();
        box.AddChild(seedRow);
        AddLabel(seedRow, 16).Text = Tr("Seed");
        _seed = new LineEdit { PlaceholderText = Tr("random"), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        seedRow.AddChild(_seed);

        _staffRow = new HBoxContainer();
        box.AddChild(_staffRow);
        AddLabel(_staffRow, 16).Text = Tr("Starting staff");
        for (var i = 0; i < _content.Balance.StartingStaff.Count; i++)
        {
            var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            _staffRow.AddChild(picker);
            _staffPickers.Add(picker);
        }

        AddButton(box, Tr("New Run")).Pressed += StartRun;
        AddButton(box, Tr("Bestiary")).Pressed += () =>
        {
            RefreshBestiary();
            ShowPage(_bestiaryPage);
        };
        AddButton(box, Tr("Unlocks")).Pressed += () =>
        {
            RefreshUnlocks();
            ShowPage(_unlocksPage);
        };
        AddButton(box, Tr("Quit")).Pressed += () => GetTree().Quit();

        _message = AddLabel(box, 16, new Color(1f, 0.72f, 0.45f));
        _message.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return center;
    }

    /// <summary>A full-screen page with a title, an optional line under it, a scrolling list and a Back button.</summary>
    private Control BuildPage(string heading, out VBoxContainer list, out Label subtitle)
    {
        var center = Centered();
        var panel = new PanelContainer();
        center.AddChild(panel);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 8);
        panel.AddChild(box);

        var title = AddLabel(box, 30, new Color(0.85f, 0.9f, 1f));
        title.Text = heading;
        subtitle = AddLabel(box, 18, new Color(0.75f, 0.85f, 1f));

        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(1100, 640) };
        box.AddChild(scroll);
        list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(list);

        AddButton(box, Tr("Back")).Pressed += () =>
        {
            RefreshMenu();
            ShowPage(_menu);
        };
        return center;
    }

    private void ShowPage(Control page)
    {
        foreach (var each in new[] { _menu, _bestiaryPage, _unlocksPage })
        {
            each.Visible = each == page;
        }
    }

    private void RefreshMenu()
    {
        var meta = PlayerProfile.Meta;
        var s = meta.Statistics;
        _essence.Text = $"{Tr("Mist Essence")}: {meta.Essence}";
        _statistics.Text = $"{Tr("Runs")} {s.RunsPlayed}  ·  {Tr("won")} {s.RunsWon}  ·  {Tr("best")} {s.BestCyclesSurvived}/{_content.Balance.RunLength} {Tr("cycles")}";

        _staffRow.Visible = meta.CanChooseStartingStaff(_content);
        var available = meta.AvailableCharacters(_content);
        for (var i = 0; i < _staffPickers.Count; i++)
        {
            var picker = _staffPickers[i];
            var chosen = picker.ItemCount > 0 ? picker.GetItemMetadata(picker.Selected).AsString() : _content.Balance.StartingStaff[i];
            picker.Clear();
            foreach (var character in available)
            {
                picker.AddItem($"{character.Name} ({character.MainClass})");
                picker.SetItemMetadata(picker.ItemCount - 1, character.Id);
                if (character.Id == chosen)
                {
                    picker.Select(picker.ItemCount - 1);
                }
            }
        }
    }

    private void StartRun()
    {
        ulong? seed = null;
        var text = _seed.Text.Trim();
        if (text.Length > 0)
        {
            if (!ulong.TryParse(text, out var parsed) || parsed == 0)
            {
                _message.Text = Tr("The seed must be a whole number above 0, or empty for a random one.");
                return;
            }

            seed = parsed;
        }

        IReadOnlyList<string>? staff = null;
        if (_staffRow.Visible)
        {
            staff = _staffPickers.Select(p => p.GetItemMetadata(p.Selected).AsString()).ToList();
            if (PlayerProfile.Meta.CheckStartingStaff(_content, staff) is { } problem)
            {
                _message.Text = Tr(problem);
                return;
            }
        }

        PlayerProfile.PendingRun = new RunRequest(seed, staff);
        GetTree().ChangeSceneToFile(GameScenePath);
    }

    /// <summary>Every creature, showing only what the bestiary has revealed (BST-01); the rest is "?".</summary>
    private void RefreshBestiary()
    {
        Clear(_bestiaryList);
        foreach (var entry in BestiaryBook.Entries(_content, PlayerProfile.Bestiary))
        {
            var lines = new List<string>();
            if (entry.Level == BestiaryLevel.Unknown)
            {
                lines.Add(Tr("? (a silhouette in the mist)"));
            }
            else
            {
                lines.Add($"{entry.Name}  ·  {entry.PlaneName}  ·  {Tr("size")} {entry.Size}  ·  {Tr(entry.Level.ToString())}");
                lines.Add(entry.Appetites is { } appetites
                    ? $"   {Tr("Wants")}: {string.Join(", ", appetites.Select(Describe))}"
                    : $"   {Tr("Wants")}: ?");
                lines.Add(entry.Triggers is { } triggers
                    ? $"   {Tr("Hates")}: {string.Join(", ", triggers.Select(t => t.Trigger.ToString()))}"
                    : $"   {Tr("Hates")}: ?");
                if (entry.Combat is { } c && entry.Escalation is { } e)
                {
                    var skips = e.Skip is { Count: > 0 } skip ? $"  ·  {Tr("skips")} {string.Join(", ", skip)}" : "";
                    lines.Add($"   {Tr("Health")} {c.Health}  ·  {Tr("attack")} {c.Attack} / {c.AttackInterval} s  ·  {Tr("armour")} {c.Armour}"
                              + $"  ·  {Tr("wrecks for")} {e.WreckSeconds} s, {Tr("preys for")} {e.PreySeconds} s{skips}");
                }
                else
                {
                    lines.Add($"   {Tr("Combat and escalation")}: ?");
                }
            }

            AddLabel(_bestiaryList, 18).Text = string.Join("\n", lines);
        }
    }

    private string Describe(AppetiteDef a)
    {
        var text = $"{a.Appetite} ({Tr(a.Strength == AppetiteStrength.Craves ? "craves" : "likes")})";
        return a.PreferredTechnique is { } t ? $"{text}, {Tr("best with")} {_content.Technique(t).Name}" : text;
    }

    /// <summary>What Essence can buy (MET-03); buying takes effect from the next run.</summary>
    private void RefreshUnlocks()
    {
        var meta = PlayerProfile.Meta;
        _unlocksEssence.Text = $"{Tr("Mist Essence")}: {meta.Essence}";
        Clear(_unlocksList);
        foreach (var unlock in _content.Unlocks)
        {
            var row = new HBoxContainer();
            _unlocksList.AddChild(row);
            var label = AddLabel(row, 18);
            label.Text = $"{_content.UnlockName(unlock)}  ·  {Tr(KindName(unlock.Kind))}";
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;

            var id = unlock.Id;
            var failure = meta.CheckBuy(_content, id);
            var button = AddButton(row, failure == UnlockFailure.AlreadyOwned ? Tr("Unlocked") : $"{Tr("Buy")} ({unlock.Cost} {Tr("Essence")})");
            button.Disabled = failure != UnlockFailure.None;
            button.Pressed += () =>
            {
                meta.Buy(_content, id);
                RefreshUnlocks();
            };
        }
    }

    private static string KindName(UnlockKind kind) => kind switch
    {
        UnlockKind.Plane => "a new plane",
        UnlockKind.Character => "a new character in the shop",
        UnlockKind.Item => "a new item in the shop",
        UnlockKind.Fixture => "a new fixture in the build menu",
        UnlockKind.StartingStaff => "choose your starting staff at New Run",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
