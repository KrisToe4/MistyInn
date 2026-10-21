using MistyInn.Core.Content;
using MistyInn.Core.Entities;

namespace MistyInn.Core.Run;

/// <summary>
/// An item the player owns (ITM-01). A staff item is carried by a staff member or sits in storage (ITM-02); an inn item
/// applies to the whole run from the moment it is bought (ITM-04). Mutable; only Core changes it.
/// </summary>
internal sealed class OwnedItem
{
    public OwnedItem(ItemId id, ItemDef def, int paid)
    {
        Id = id;
        Def = def;
        Paid = paid;
    }

    public ItemId Id { get; }

    public ItemDef Def { get; }

    /// <summary>What it cost; half comes back when it is sold (ITM-07).</summary>
    public int Paid { get; }

    /// <summary>The staff member carrying it, or null when it is in storage or is an inn item.</summary>
    public UnitId? Carrier { get; set; }

    public bool IsInnItem => Def.Kind == ItemKind.Inn;

    public bool IsInStorage => !IsInnItem && Carrier is null;

    public ItemSnapshot Snapshot() => new(Id, Def.Id, Def.Name, Def.Kind, Def.Rarity, Carrier, Def.DisplaySpot, Paid / 2);
}

/// <summary>A read-only view of an owned item (ARC-03). <paramref name="SellPrice"/> is half what it cost (ITM-07).</summary>
public sealed record ItemSnapshot(
    ItemId Id,
    string ItemDefId,
    string Name,
    ItemKind Kind,
    Rarity Rarity,
    UnitId? Carrier,
    string? DisplaySpot,
    int SellPrice)
{
    public bool IsInStorage => Kind == ItemKind.Staff && Carrier is null;
}
