using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MistyInn.Core;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Run;
using static MistyInn.UI.UiKit;

namespace MistyInn.UI;

/// <summary>
/// The Dusk staff and items tab (UI-03, ITM-02, ITM-07, STF-09): each staff member with rank, classes, ancestry and
/// carried items (Unequip, Sell), item storage (give to a staff member, Sell) and the inn items with their display spots.
/// </summary>
public partial class StaffItemsPanel : VBoxContainer
{
    private VBoxContainer _staff = null!;
    private VBoxContainer _storage = null!;
    private VBoxContainer _inn = null!;
    private string _signature = "";

    public event Action<UnitId>? SellUnitPressed;

    public event Action<ItemId, UnitId>? EquipPressed;

    public event Action<ItemId>? UnequipPressed;

    public event Action<ItemId>? SellItemPressed;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(380, 0);
        AddLabel(this, 16).Text = Tr("Staff");
        _staff = new VBoxContainer();
        AddChild(_staff);
        AddLabel(this, 16).Text = Tr("Item storage");
        _storage = new VBoxContainer();
        AddChild(_storage);
        AddLabel(this, 16).Text = Tr("Inn items");
        _inn = new VBoxContainer();
        AddChild(_inn);
    }

    public void Refresh(GameSession session)
    {
        var staff = session.Units.Where(u => !u.IsInnkeeper).ToList();
        var items = session.Items;
        var signature = string.Join("|",
            string.Join(",", staff.Select(u => $"{u.Id.Value}:{u.Rank}:{u.Tile}:{string.Join(".", u.Items.Select(i => i.Value))}:{u.RevealedAncestry}:{string.Join(".", u.Techniques)}")),
            string.Join(",", items.Select(i => $"{i.Id.Value}:{i.Carrier?.Value}")),
            session.Filters.NoSuggestive);
        if (signature == _signature)
        {
            return;
        }

        _signature = signature;
        var names = items.ToDictionary(i => i.Id, i => i.Name);

        Clear(_staff);
        foreach (var unit in staff)
        {
            var row = new HBoxContainer();
            _staff.AddChild(row);
            var ancestry = unit.RevealedAncestry is { } plane ? session.Content.Plane(plane).Name : "?";
            var place = unit.IsOnBench ? Tr("bench") : $"({unit.Tile!.Value.X},{unit.Tile!.Value.Y})";
            AddLabel(row, 15).Text =
                $"{unit.Name} ★{unit.Rank}  {string.Join("/", unit.Classes.Select(ShopPanel.ClassName))}  ·  {Tr("ancestry")} {ancestry}  ·  {place}";
            var id = unit.Id;
            AddButton(row, Tr("Sell")).Pressed += () => SellUnitPressed?.Invoke(id);
            foreach (var itemId in unit.Items)
            {
                var item = itemId;
                AddButton(row, $"{Tr("Unequip")} {names[item]}").Pressed += () => UnequipPressed?.Invoke(item);
            }
        }

        Clear(_storage);
        foreach (var item in items.Where(i => i.IsInStorage))
        {
            var row = new HBoxContainer();
            _storage.AddChild(row);
            AddLabel(row, 15).Text = item.Name;
            var give = new OptionButton();
            give.AddItem(Tr("Give to…"));
            var targets = new List<UnitId>();
            foreach (var unit in staff)
            {
                give.AddItem(unit.Name);
                targets.Add(unit.Id);
            }

            var id = item.Id;
            give.ItemSelected += index =>
            {
                if (index > 0)
                {
                    EquipPressed?.Invoke(id, targets[(int)index - 1]);
                }
            };
            row.AddChild(give);
            AddButton(row, $"{Tr("Sell")} ({item.SellPrice})").Pressed += () => SellItemPressed?.Invoke(id);
        }

        Clear(_inn);
        foreach (var item in items.Where(i => i.Kind == ItemKind.Inn))
        {
            var row = new HBoxContainer();
            _inn.AddChild(row);
            AddLabel(row, 15).Text = $"{item.Name}  ·  {Tr(item.DisplaySpot?.Replace('-', ' ') ?? "")}";
            var id = item.Id;
            AddButton(row, $"{Tr("Sell")} ({item.SellPrice})").Pressed += () => SellItemPressed?.Invoke(id);
        }

        if (!items.Any())
        {
            AddLabel(_storage, 15).Text = Tr("(no items yet)");
        }
    }
}
