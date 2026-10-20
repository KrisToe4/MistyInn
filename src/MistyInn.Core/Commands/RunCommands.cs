using MistyInn.Core.Run;

namespace MistyInn.Core.Commands;

/// <summary>Spends up to <paramref name="Silver"/> on integrity at Dawn, 5 points per silver (BRD-07, D-072).</summary>
public sealed record RepairIntegrity(int Silver) : GameCommand;

/// <summary>Changes the content filters (SET-04). Accepted in every phase; it never changes what happens (CMP-06).</summary>
public sealed record SetContentFilters(ContentFilters Filters) : GameCommand;

/// <summary>
/// Development only (D-074): puts a character on the bench at ★1, as if hired. The game sends it only from the debug
/// dev panel; the shop arrives in step 6.
/// </summary>
public sealed record DevAddStaff(string CharacterId) : GameCommand;

/// <summary>
/// Development only (D-074): sets this Dusk's converging planes, unlocking them for the run if needed.
/// </summary>
public sealed record DevSetConvergingPlanes(IReadOnlyList<string> PlaneIds) : GameCommand;
