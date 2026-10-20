using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;

namespace MistyInn.Core.Run;

/// <summary>What a creature is doing.</summary>
public enum CreatureActivity
{
    /// <summary>Walking from its mist gate, through the front door, into the inn.</summary>
    Arriving,

    /// <summary>Walking to a spot where one of its appetites can be served (CRE-04).</summary>
    Seeking,

    /// <summary>At such a spot, served or waiting to be.</summary>
    Waiting,

    /// <summary>Walking somewhere with nothing to seek.</summary>
    Wandering,

    /// <summary>Standing still for a moment.</summary>
    Idle,
}

/// <summary>A creature's agitation state (AGI-03).</summary>
public enum CreatureState
{
    Calm,
    Restless,
    Menacing,
    Turned,
}

/// <summary>A creature on the board. Mutable; only Core changes it.</summary>
internal sealed class Creature
{
    private readonly Queue<Tile> _path = new();

    public Creature(CreatureId id, CreatureDef def, Tile spawn)
    {
        Id = id;
        Def = def;
        Position = TilePosition.Center(spawn);
        PreviousPosition = Position;
        Agitation = def.Agitation.Arrival;
    }

    public CreatureId Id { get; }

    public CreatureDef Def { get; }

    public TilePosition Position { get; set; }

    /// <summary>The position before the last step, for interpolation (SIM-03).</summary>
    public TilePosition PreviousPosition { get; set; }

    public CreatureActivity Activity { get; set; } = CreatureActivity.Arriving;

    /// <summary>Game seconds at which an idle creature looks for something to do.</summary>
    public double IdleUntil { get; set; }

    /// <summary>0 to 100 (AGI-01).</summary>
    public double Agitation { get; set; }

    public CreatureState State { get; set; } = CreatureState.Calm;

    /// <summary>The service spot it is walking to or waiting at, reserved for it (CRE-04).</summary>
    public Tile? Spot { get; set; }

    /// <summary>Game seconds since it was last served at its spot, to know when to look again.</summary>
    public double UnservedSince { get; set; }

    /// <summary>Whether something served it in the current step, and what (for views and the log).</summary>
    public string? ServedBy { get; set; }

    /// <summary>Whether it was served at least once this cycle (BST-02).</summary>
    public bool WasServed { get; set; }

    /// <summary>Triggers holding in the last step, to spike only when one starts (AGI-08).</summary>
    public HashSet<TriggerKind> ActiveTriggers { get; } = new();

    public bool Frightened { get; set; }

    /// <summary>Game seconds at which a Menacing creature next breaks a fixture (AGI-03).</summary>
    public double NextBreakAt { get; set; }

    public bool IsTurned => State == CreatureState.Turned;

    /// <summary>The tiles still to walk, in order.</summary>
    public IReadOnlyCollection<Tile> Path => _path;

    /// <summary>Replaces the path; the first tile (where it stands) is skipped.</summary>
    public void SetPath(IReadOnlyList<Tile> path)
    {
        _path.Clear();
        foreach (var tile in path.Skip(1))
        {
            _path.Enqueue(tile);
        }
    }

    public void ClearPath() => _path.Clear();

    /// <summary>Walks up to <paramref name="distance"/> tiles along the path.</summary>
    public void Advance(double distance)
    {
        while (distance > 0 && _path.Count > 0)
        {
            var next = TilePosition.Center(_path.Peek());
            var gap = Position.DistanceTo(next);
            if (gap <= distance)
            {
                Position = next;
                distance -= gap;
                _path.Dequeue();
            }
            else
            {
                Position = Position.MoveTowards(next, distance);
                distance = 0;
            }
        }
    }

    public CreatureSnapshot Snapshot() =>
        new(Id, Def.Id, Def.Name, Def.PlaneId, Position, PreviousPosition, Activity, Agitation, State, ServedBy);
}

/// <summary>A read-only view of a creature for the presentation (ARC-03).</summary>
public sealed record CreatureSnapshot(
    CreatureId Id,
    string CreatureDefId,
    string Name,
    string PlaneId,
    TilePosition Position,
    TilePosition PreviousPosition,
    CreatureActivity Activity,
    double Agitation,
    CreatureState State,
    string? ServedBy);
