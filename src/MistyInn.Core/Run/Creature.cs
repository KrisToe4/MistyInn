using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;

namespace MistyInn.Core.Run;

/// <summary>What a creature is doing (step 2: walking in, wandering, standing about).</summary>
public enum CreatureActivity
{
    /// <summary>Walking from its mist gate, through the front door, into the inn.</summary>
    Arriving,

    /// <summary>Walking to another spot in the common room.</summary>
    Wandering,

    /// <summary>Standing still for a moment.</summary>
    Idle,
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
    }

    public CreatureId Id { get; }

    public CreatureDef Def { get; }

    public TilePosition Position { get; set; }

    /// <summary>The position before the last step, for interpolation (SIM-03).</summary>
    public TilePosition PreviousPosition { get; set; }

    public CreatureActivity Activity { get; set; } = CreatureActivity.Arriving;

    /// <summary>Game seconds at which an idle creature picks a new spot.</summary>
    public double IdleUntil { get; set; }

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
        new(Id, Def.Id, Def.Name, Def.PlaneId, Position, PreviousPosition, Activity);
}

/// <summary>A read-only view of a creature for the presentation (ARC-03).</summary>
public sealed record CreatureSnapshot(
    CreatureId Id,
    string CreatureDefId,
    string Name,
    string PlaneId,
    TilePosition Position,
    TilePosition PreviousPosition,
    CreatureActivity Activity);
