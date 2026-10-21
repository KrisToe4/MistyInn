using MistyInn.Core.Entities;

namespace MistyInn.Core.Events;

/// <summary>The shop's offers, lock or reroll cost changed (UI-03).</summary>
public sealed record ShopChanged(TimeSpan At) : DomainEvent(At);

/// <summary>A character was hired from the shop at a rank with its preset build (STF-10).</summary>
public sealed record StaffHired(TimeSpan At, UnitId Unit, string CharacterId, int Rank, int Price) : DomainEvent(At);

/// <summary>An owned character rose one rank from an upgrade offer and waits for its pick (STF-08, UI-08).</summary>
public sealed record UpgradeOffered(TimeSpan At, UnitId Unit, int Rank) : DomainEvent(At);

/// <summary>The pick for a new rank was made (STF-08, ARC-12).</summary>
public sealed record UnitUpgraded(TimeSpan At, UnitId Unit, int Rank, string OptionId) : DomainEvent(At);

/// <summary>A staff member was sold for half the silver spent on it (STF-09).</summary>
public sealed record StaffSold(TimeSpan At, UnitId Unit, int Silver) : DomainEvent(At);

/// <summary>An item was bought, moved, or sold (ITM-02, ITM-04, ITM-07).</summary>
public sealed record ItemsChanged(TimeSpan At) : DomainEvent(At);

/// <summary>An inn item was bought: it now shows at its display spot (ITM-04, ART-17).</summary>
public sealed record InnItemAdded(TimeSpan At, ItemId Item, string ItemDefId, string DisplaySpot) : DomainEvent(At);

/// <summary>An inn item was sold and leaves its display spot.</summary>
public sealed record InnItemRemoved(TimeSpan At, ItemId Item) : DomainEvent(At);

/// <summary>The back rooms in use changed (a <c>RoomSlots</c> item, D-078).</summary>
public sealed record RoomsChanged(TimeSpan At, IReadOnlyList<string> OpenRooms) : DomainEvent(At);

/// <summary>A character's hidden ancestry was revealed by observation (STF-16).</summary>
public sealed record AncestryRevealed(TimeSpan At, UnitId Unit, string CharacterId, string PlaneId) : DomainEvent(At);
