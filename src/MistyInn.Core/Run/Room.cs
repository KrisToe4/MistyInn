using MistyInn.Core.Board;
using MistyInn.Core.Entities;

namespace MistyInn.Core.Run;

/// <summary>A back room during the run (CMP-04): who is in it, and its door (CMP-05). Mutable; only Core changes it.</summary>
internal sealed class Room
{
    public Room(RoomLayout layout, double doorHealth)
    {
        Layout = layout;
        MaxDoorHealth = doorHealth;
        DoorHealth = doorHealth;
    }

    public RoomLayout Layout { get; }

    public string Zone => Layout.Zone;

    public Tile Door => Layout.Door;

    public double DoorHealth { get; set; }

    public double MaxDoorHealth { get; }

    /// <summary>A broken door closes the room until it is repaired at Dawn (D-069).</summary>
    public bool IsBroken => DoorHealth <= 0;

    public UnitId? Companion { get; set; }

    public CreatureId? Guest { get; set; }

    /// <summary>When the session inside ends; null while the pair is still walking there.</summary>
    public double? EndsAt { get; set; }

    public bool IsTaken => Companion is not null;

    /// <summary>The door is closed on a session (CMP-04); only then can a turned creature attack it (CMP-05).</summary>
    public bool InSession => EndsAt is not null;

    public void Clear()
    {
        Companion = null;
        Guest = null;
        EndsAt = null;
    }

    public RoomSnapshot Snapshot() => new(Zone, Door, DoorHealth, MaxDoorHealth, Companion, Guest, InSession);
}

/// <summary>A read-only view of a back room (ARC-03).</summary>
public sealed record RoomSnapshot(
    string Zone,
    Tile Door,
    double DoorHealth,
    double MaxDoorHealth,
    UnitId? Companion,
    CreatureId? Guest,
    bool InSession);
