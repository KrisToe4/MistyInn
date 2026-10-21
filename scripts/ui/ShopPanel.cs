using System;
using System.Linq;
using Godot;
using MistyInn.Core;
using MistyInn.Core.Content;
using MistyInn.Core.Run;
using MistyInn.Core.Simulation;
using static MistyInn.UI.UiKit;

namespace MistyInn.UI;

/// <summary>
/// The Dusk shop tab (UI-03): staff offers with rank, classes, techniques and an Upgrade badge, item offers (with where
/// an inn item will appear, UI-08), Reroll with its cost, Lock, and the synergy panel with active and nearly-active
/// synergies (SYN-03). Built in code; rebuilt only when what it shows changes.
/// </summary>
public partial class ShopPanel : VBoxContainer
{
    private VBoxContainer _staff = null!;
    private VBoxContainer _items = null!;
    private Button _reroll = null!;
    private CheckButton _lock = null!;
    private Label _synergies = null!;
    private string _signature = "";
    private bool _settingLock;

    public event Action<ShopSection, int>? BuyPressed;

    public event Action? RerollPressed;

    public event Action<bool>? LockToggled;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(380, 0);
        AddLabel(this, 16).Text = Tr("Staff for hire");
        _staff = new VBoxContainer();
        AddChild(_staff);
        AddLabel(this, 16).Text = Tr("Items");
        _items = new VBoxContainer();
        AddChild(_items);

        var bar = new HBoxContainer();
        AddChild(bar);
        _reroll = AddButton(bar, Tr("Reroll"));
        _reroll.Pressed += () => RerollPressed?.Invoke();
        _lock = new CheckButton { Text = Tr("Lock for next Dusk") };
        _lock.Toggled += on =>
        {
            if (!_settingLock)
            {
                LockToggled?.Invoke(on);
            }
        };
        bar.AddChild(_lock);

        AddLabel(this, 16).Text = Tr("Synergies (staff on the floor)");
        _synergies = AddLabel(this, 15, new Color(0.85f, 0.9f, 0.75f));
    }

    public void Refresh(GameSession session)
    {
        var shop = session.Shop;
        var signature = string.Join("|",
            session.Silver,
            shop.Locked,
            shop.RerollCost,
            string.Join(",", shop.Staff.Select(o => o is null ? "-" : $"{o.Offer}{o.IsUpgrade}{o.CanBuy}")),
            string.Join(",", shop.Items.Select(o => o?.Offer.ToString() ?? "-")),
            string.Join(",", session.Synergies.Select(s => $"{s.SynergyId}{s.Count}")),
            session.Filters.NoSuggestive);
        if (signature == _signature)
        {
            return;
        }

        _signature = signature;
        Clear(_staff);
        for (var i = 0; i < shop.Staff.Count; i++)
        {
            var index = i;
            if (shop.Staff[i] is not { } offer)
            {
                AddLabel(_staff, 15).Text = Tr("(sold)");
                continue;
            }

            var badge = offer.IsUpgrade ? $"  [{Tr("Upgrade")}]" : "";
            var techniques = string.Join(", ", offer.Techniques.Select(session.TechniqueName));
            var classes = string.Join("/", offer.Classes.Select(ClassName));
            var button = AddButton(_staff, $"{offer.Name} ★{offer.Offer.Rank}{badge}  ·  {classes}: {techniques}  ·  {offer.Offer.Price}");
            button.Disabled = !offer.CanBuy;
            button.Pressed += () => BuyPressed?.Invoke(ShopSection.Staff, index);
        }

        Clear(_items);
        for (var i = 0; i < shop.Items.Count; i++)
        {
            var index = i;
            if (shop.Items[i] is not { } offer)
            {
                AddLabel(_items, 15).Text = Tr("(sold)");
                continue;
            }

            var where = offer.DisplaySpot is { } spot ? $", {Tr("shown at")} {Tr(spot.Replace('-', ' '))}" : "";
            var button = AddButton(_items, $"{offer.Name} ({Tr(offer.Kind.ToString())}, {Tr(offer.Rarity.ToString())}{where})  ·  {offer.Offer.Price}");
            button.Disabled = session.Silver < offer.Offer.Price;
            button.Pressed += () => BuyPressed?.Invoke(ShopSection.Items, index);
        }

        _reroll.Text = $"{Tr("Reroll")} ({shop.RerollCost})";
        _reroll.Disabled = session.Silver < shop.RerollCost;
        _settingLock = true;
        _lock.ButtonPressed = shop.Locked;
        _settingLock = false;
        _synergies.Text = Synergies(session);
    }

    /// <summary>Active synergies, then those one character short (SYN-03).</summary>
    private string Synergies(GameSession session)
    {
        var shown = session.Synergies.Where(s => s.IsActive || s.IsNearlyActive).ToList();
        if (shown.Count == 0)
        {
            return Tr("(none yet)");
        }

        return string.Join("\n", shown.Select(s =>
        {
            var steps = string.Join("/", s.Thresholds);
            var state = s.IsActive ? $"{Tr("active")} ({s.ActiveThreshold})" : Tr("one more");
            return $"{s.Name} {s.Count} [{steps}]  {state}";
        }));
    }

    public static string ClassName(StaffClass staffClass) => staffClass == StaffClass.CompanionServer ? "Companion/Server" : staffClass.ToString();
}
