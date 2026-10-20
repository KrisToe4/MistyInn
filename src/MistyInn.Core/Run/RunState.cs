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
        Units = content.StartingUnits
            .Select(def => new Unit(ids.NextUnit(), def, def.TechniqueIds.Select(content.Technique).ToList()))
            .ToList();
        Silver = content.Balance.StartingSilver;
        foreach (var supply in Enum.GetValues<Supply>())
        {
            Supplies[supply] = content.Balance.StartingSupplies.TryGetValue(supply, out var n) ? n : 0;
        }

        foreach (var fixtureId in content.Balance.StartingFixtures)
        {
            Fixtures.Add(new Fixture(ids.NextFixture(), content.Fixture(fixtureId)));
        }
    }

    public int Cycle { get; set; }

    public Phase Phase { get; set; } = Phase.Dusk;

    public InnBoard Board { get; }

    public List<Unit> Units { get; }

    public List<Creature> Creatures { get; } = new();

    /// <summary>Every fixture owned, in storage or placed, in the order acquired (FIX-05).</summary>
    public List<Fixture> Fixtures { get; } = new();

    public List<PlaneDef> ConvergingPlanes { get; } = new();

    /// <summary>The previous cycle's plane IDs, so the same set is not drawn twice in a row (PLN-03).</summary>
    public IReadOnlyList<string> PreviousPlaneSet { get; set; } = Array.Empty<string>();

    /// <summary>Mist speed: 0 (paused), 1, 2 or 4 (SCP-13).</summary>
    public int Speed { get; set; } = 1;

    /// <summary>The run's currency (SHP-01).</summary>
    public int Silver { get; set; }

    /// <summary>Supplies in stock (FIX-06).</summary>
    public Dictionary<Supply, int> Supplies { get; } = new();

    /// <summary>Service spots reserved by creatures walking to or waiting at them (CRE-04).</summary>
    public Dictionary<Tile, CreatureId> Reservations { get; } = new();

    public Unit? FindUnit(UnitId id) => Units.FirstOrDefault(u => u.Id == id);

    public Fixture? FindFixture(FixtureId id) => Fixtures.FirstOrDefault(f => f.Id == id);

    public Creature? FindCreature(CreatureId id) => Creatures.FirstOrDefault(c => c.Id == id);

    public Unit Innkeeper => Units.First(u => u.Def.IsInnkeeper);

    public int StaffOnBoard => Units.Count(u => !u.Def.IsInnkeeper && u.Tile is not null);

    public IEnumerable<Fixture> PlacedFixtures => Fixtures.Where(f => f.IsPlaced);

    public IEnumerable<Unit> StaffOnTheFloor => Units.Where(u => u.Tile is not null && !u.Def.IsInnkeeper);
}
