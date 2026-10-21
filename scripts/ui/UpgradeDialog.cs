using System;
using System.Linq;
using Godot;
using MistyInn.Core;
using MistyInn.Core.Entities;
using static MistyInn.UI.UiKit;

namespace MistyInn.UI;

/// <summary>
/// The upgrade choice (UI-08, STF-08): shown while a unit waits for its pick. It lists the character's tree (chosen,
/// available and locked options) and a button for each available option of the new rank. Nothing else can be done
/// until a pick is made (D-081).
/// </summary>
public partial class UpgradeDialog : CenterContainer
{
    private Label _title = null!;
    private Label _tree = null!;
    private VBoxContainer _choices = null!;
    private string _signature = "";

    public event Action<UnitId, string>? ChoosePressed;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        var panel = new PanelContainer();
        AddChild(panel);
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(520, 0) };
        panel.AddChild(box);
        _title = AddLabel(box, 22);
        _tree = AddLabel(box, 15, new Color(0.8f, 0.8f, 0.85f));
        AddLabel(box, 16).Text = Tr("Choose one:");
        _choices = new VBoxContainer();
        box.AddChild(_choices);
    }

    public void Refresh(GameSession session)
    {
        var unit = session.Units.FirstOrDefault(u => u.UpgradePending);
        Visible = unit is not null;
        if (unit is null)
        {
            _signature = "";
            return;
        }

        var signature = $"{unit.Id.Value}:{unit.Rank}:{session.Filters.NoSuggestive}";
        if (signature == _signature)
        {
            return;
        }

        _signature = signature;
        var tree = session.UpgradeTree(unit.Id);
        _title.Text = $"{unit.Name} {Tr("rises to")} ★{unit.Rank}";

        string Option(UpgradeOptionView o) =>
            o.Mastery ? $"{Tr("Master")} {session.TechniqueName(o.TechniqueId)}" : $"{session.TechniqueName(o.TechniqueId)} ({ShopPanel.ClassName(o.Class)})";

        _tree.Text = string.Join("\n", tree.GroupBy(o => o.Rank).Select(rank =>
            $"★{rank.Key}: " + string.Join("   ", rank.Select(o => o.State switch
            {
                UpgradeOptionState.Chosen => $"[{Option(o)}]",
                UpgradeOptionState.Available => $"> {Option(o)}",
                _ => Option(o),
            }))));

        Clear(_choices);
        foreach (var option in tree.Where(o => o.State == UpgradeOptionState.Available))
        {
            var id = unit.Id;
            var optionId = option.OptionId;
            AddButton(_choices, Option(option)).Pressed += () => ChoosePressed?.Invoke(id, optionId);
        }
    }
}
