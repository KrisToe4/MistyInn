using MistyInn.Core.Board;
using MistyInn.Core.Entities;

namespace MistyInn.Core.Commands;

/// <summary>Ends Dusk and starts the mist (CYC-01). Needs the innkeeper on the board.</summary>
public sealed record BeginMist : GameCommand;

/// <summary>Leaves Dawn for the next cycle's Dusk (CYC-04).</summary>
public sealed record ContinueFromDawn : GameCommand;

/// <summary>Mist speed: 0 (paused), 1, 2 or 4 (SCP-13). The only command accepted during the mist (ARC-11).</summary>
public sealed record SetSpeed(int Multiplier) : GameCommand;

/// <summary>Puts a unit from the bench on a tile, or moves it to another tile (BRD-02, CAM-13).</summary>
public sealed record PlaceUnit(UnitId Unit, Tile Tile) : GameCommand;

/// <summary>Takes a staff member off the board (CAM-13). The innkeeper cannot be benched (ESC-06).</summary>
public sealed record BenchUnit(UnitId Unit) : GameCommand;
