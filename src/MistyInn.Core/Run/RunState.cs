using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;

namespace MistyInn.Core.Run;

/// <summary>The state of the current run (ARC-01). Owned by <see cref="GameSession"/>; changed only by its systems.</summary>
internal sealed class RunState
{
    public RunState(GameContent content, IdAllocator ids)
    {
        Board = new InnBoard(content.Layout);
        Units = content.StartingUnits.Select(def => new Unit(ids.NextUnit(), def)).ToList();
    }

    public int Cycle { get; set; }

    public Phase Phase { get; set; } = Phase.Dusk;

    public InnBoard Board { get; }

    public List<Unit> Units { get; }

    public List<Creature> Creatures { get; } = new();

    public List<PlaneDef> ConvergingPlanes { get; } = new();

    /// <summary>The previous cycle's plane IDs, so the same set is not drawn twice in a row (PLN-03).</summary>
    public IReadOnlyList<string> PreviousPlaneSet { get; set; } = Array.Empty<string>();

    /// <summary>Mist speed: 0 (paused), 1, 2 or 4 (SCP-13).</summary>
    public int Speed { get; set; } = 1;

    public Unit? FindUnit(UnitId id) => Units.FirstOrDefault(u => u.Id == id);

    public Unit Innkeeper => Units.First(u => u.Def.IsInnkeeper);

    public int StaffOnBoard => Units.Count(u => !u.Def.IsInnkeeper && u.Tile is not null);
}
