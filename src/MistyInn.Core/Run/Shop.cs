namespace MistyInn.Core.Run;

/// <summary>Which row of the shop an offer is in (SHP-02).</summary>
public enum ShopSection
{
    Staff,
    Items,
}

/// <summary>A character at a rank, for hire or as an upgrade (SHP-02, STF-08, STF-10). The price is fixed when drawn (SHP-08).</summary>
public sealed record StaffOffer(string CharacterId, int Rank, int Price);

/// <summary>A staff item or an inn item (SHP-02, ITM-01).</summary>
public sealed record ItemOffer(string ItemId, int Price);

/// <summary>The shop during the run (section 13.1). Bought offers leave an empty slot (null) until the next draw.</summary>
internal sealed class ShopState
{
    public List<StaffOffer?> Staff { get; } = new();

    public List<ItemOffer?> Items { get; } = new();

    /// <summary>Keep these offers for the next Dusk instead of a new draw (SHP-06).</summary>
    public bool Locked { get; set; }

    /// <summary>Rerolls bought this cycle; the cost grows with each (SHP-05).</summary>
    public int RerollsThisCycle { get; set; }
}

/// <summary>A staff offer as the shop panel shows it (UI-03): the preset build at that rank, and whether it is an upgrade.</summary>
public sealed record StaffOfferView(
    StaffOffer Offer,
    string Name,
    bool IsUpgrade,
    bool CanBuy,
    IReadOnlyList<Content.StaffClass> Classes,
    IReadOnlyList<string> Techniques);

/// <summary>An item offer as the shop panel shows it (UI-03, UI-08: where an inn item will appear).</summary>
public sealed record ItemOfferView(ItemOffer Offer, string Name, Content.ItemKind Kind, Content.Rarity Rarity, string? DisplaySpot);

/// <summary>The Dusk shop as the presentation sees it (UI-03). Empty slots are null.</summary>
public sealed record ShopSnapshot(
    IReadOnlyList<StaffOfferView?> Staff,
    IReadOnlyList<ItemOfferView?> Items,
    bool Locked,
    int RerollCost);
