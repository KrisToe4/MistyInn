using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;

namespace MistyInn.Core.Commands;

/// <summary>Buys a fixture from the build menu into storage (FIX-05).</summary>
public sealed record BuildFixture(string FixtureDefId) : GameCommand;

/// <summary>
/// Places a fixture from storage, or moves a placed one, with its origin on <paramref name="Tile"/> and turned
/// <paramref name="Rotation"/> quarter turns clockwise (BRD-02, CAM-13).
/// </summary>
public sealed record PlaceFixture(FixtureId Fixture, Tile Tile, int Rotation) : GameCommand;

/// <summary>Takes a placed fixture back into storage.</summary>
public sealed record StoreFixture(FixtureId Fixture) : GameCommand;

/// <summary>Sells a fixture, placed or stored, for half its cost (FIX-05).</summary>
public sealed record SellFixture(FixtureId Fixture) : GameCommand;

/// <summary>Buys bundles of a supply from the build menu (FIX-06).</summary>
public sealed record BuySupplies(Supply Supply, int Bundles) : GameCommand;
