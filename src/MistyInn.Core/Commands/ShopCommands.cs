using MistyInn.Core.Entities;
using MistyInn.Core.Run;

namespace MistyInn.Core.Commands;

/// <summary>
/// Buys a shop offer (SHP-02): hires a character at the offer's rank (STF-10), raises an owned one by one rank
/// (STF-08), or buys an item (ITM-01). <paramref name="Index"/> counts from 0 within the section (D-080).
/// </summary>
public sealed record BuyOffer(ShopSection Section, int Index) : GameCommand;

/// <summary>Redraws the shop's offers for the current reroll cost (SHP-05).</summary>
public sealed record Reroll : GameCommand;

/// <summary>Keeps (or stops keeping) the current offers for the next Dusk (SHP-06).</summary>
public sealed record LockShop(bool Locked) : GameCommand;

/// <summary>The pick for an upgraded unit's new rank: an option ID from its tree (STF-07, STF-08).</summary>
public sealed record ChooseUpgrade(UnitId Unit, string OptionId) : GameCommand;

/// <summary>Sells a staff member for half the silver spent on it; its items go to storage (STF-09).</summary>
public sealed record SellUnit(UnitId Unit) : GameCommand;

/// <summary>Gives a staff item to a staff member, from storage or from another carrier (ITM-02).</summary>
public sealed record EquipItem(ItemId Item, UnitId Unit) : GameCommand;

/// <summary>Takes a staff item back into storage (ITM-02).</summary>
public sealed record UnequipItem(ItemId Item) : GameCommand;

/// <summary>Sells an item for half its price (ITM-07).</summary>
public sealed record SellItem(ItemId Item) : GameCommand;
