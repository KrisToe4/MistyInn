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

    /// <summary>Walking to a back room with a companion (CMP-04).</summary>
    GoingToRoom,

    /// <summary>In a back room: off the board, occupied (CMP-04).</summary>
    InRoom,
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
        Health = def.Combat.Health;
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

    public double Health { get; set; }

    public double MaxHealth => Def.Combat.Health;

    /// <summary>Off the board, in a back room (CMP-04): nothing outside can see it or reach it.</summary>
    public bool IsInRoom => Activity == CreatureActivity.InRoom;

    /// <summary>A turned creature's escalation stage (ESC-01).</summary>
    public EscalationStage Stage { get; set; }

    /// <summary>Game seconds at which the current stage began.</summary>
    public double StageSince { get; set; }

    /// <summary>Destroyed a fixture in its current stage (ESC-01).</summary>
    public bool BrokeSomething { get; set; }

    /// <summary>Made a unit or creature fall in its current stage (ESC-01).</summary>
    public bool MadeSomeoneFall { get; set; }

    /// <summary>The fighter it has turned on, after being hit (ESC-03).</summary>
    public UnitId? EngagedBy { get; set; }

    /// <summary>The fighter holding it with Grapple (STF-02).</summary>
    public UnitId? GrappledBy { get; set; }

    public double NextAttackAt { get; set; }

    public double RepathAt { get; set; }

    /// <summary>What it is going for, as a key, to notice when the target changes.</summary>
    public string? TargetKey { get; set; }

    /// <summary>The companion giving it floor company, and since when (CMP-04).</summary>
    public UnitId? CompanyWith { get; set; }

    public double CompanySince { get; set; }

    /// <summary>Has had a room session this cycle; it is not taken to a room again (D-071).</summary>
    public bool HadRoom { get; set; }

    /// <summary>The tiles still to walk, in order.</summary>
    public IReadOnlyCollection<Tile> Path => _path;

    /// <summary>Replaces the path; the first tile (where it stands) is skipped.</summary>
    public void SetPath(IReadOnlyList<Tile> path) => Walking.SetPath(_path, path);

    public void ClearPath() => _path.Clear();

    /// <summary>Walks up to <paramref name="distance"/> tiles along the path.</summary>
    public void Advance(double distance) => Position = Walking.Advance(Position, _path, distance);

    public CreatureSnapshot Snapshot() =>
        new(Id, Def.Id, Def.Name, Def.PlaneId, Position, PreviousPosition, Activity, Agitation, State, ServedBy,
            Health, MaxHealth, IsTurned ? Stage : (EscalationStage?)null);
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
    string? ServedBy,
    double Health,
    double MaxHealth,
    EscalationStage? Stage)
{
    /// <summary>Shown on the board: not inside a back room (CMP-04).</summary>
    public bool IsVisible => Activity != CreatureActivity.InRoom;
}
