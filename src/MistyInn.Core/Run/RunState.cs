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
        Units = content.StartingUnits.Select(def => NewUnit(content, ids, def)).ToList();
        Silver = content.Balance.StartingSilver;
        Integrity = content.Balance.Integrity;
        UnlockedPlanes = content.UnlockedPlanes.ToList();
        Rooms = content.Layout.Rooms
            .Take(content.Balance.Rooms)
            .Select(r => new Room(r, content.Balance.Company.DoorHealth))
            .ToList();
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

    /// <summary>The inn's overall health, 0 to <see cref="BalanceDef.Integrity"/> (BRD-06).</summary>
    public double Integrity { get; set; }

    /// <summary>The planes this run draws from (PLN-03); the profile's unlocks, or more with the dev panel (D-074).</summary>
    public List<string> UnlockedPlanes { get; }

    /// <summary>The back rooms in use, from <c>balance.json</c> <c>rooms</c> (section 6.4).</summary>
    public List<Room> Rooms { get; }

    /// <summary>Where and when recent attacks happened, for <c>Violence</c> (AGI-08, CMP-05). Bounded (INV-05).</summary>
    public List<(TilePosition At, double Time)> Attacks { get; } = new();

    /// <summary>Where units fell this mist, for <c>Blood</c> (AGI-08, ESC-04). Bounded (INV-05).</summary>
    public List<TilePosition> BloodMarks { get; } = new();

    /// <summary>Why the run ended, once it has (RUN-03).</summary>
    public RunEndReason? EndReason { get; set; }

    public static Unit NewUnit(GameContent content, IdAllocator ids, UnitTemplate def) =>
        new(ids.NextUnit(), def, def.TechniqueIds.Select(content.Technique).ToList(), content.Balance.RankStatMultipliers[0]);

    public Unit? FindUnit(UnitId id) => Units.FirstOrDefault(u => u.Id == id);

    public Fixture? FindFixture(FixtureId id) => Fixtures.FirstOrDefault(f => f.Id == id);

    public Creature? FindCreature(CreatureId id) => Creatures.FirstOrDefault(c => c.Id == id);

    public Unit Innkeeper => Units.First(u => u.Def.IsInnkeeper);

    public int StaffOnBoard => Units.Count(u => !u.Def.IsInnkeeper && u.Tile is not null);

    public IEnumerable<Fixture> PlacedFixtures => Fixtures.Where(f => f.IsPlaced);

    public IEnumerable<Unit> StaffOnTheFloor => Units.Where(u => u.Tile is not null && !u.Def.IsInnkeeper);

    /// <summary>Staff at their posts and free to serve (STF-04).</summary>
    public IEnumerable<Unit> AvailableStaff => StaffOnTheFloor.Where(u => u.IsAvailable);

    /// <summary>Creatures on the board, not inside a back room (CMP-04).</summary>
    public IEnumerable<Creature> CreaturesOnTheFloor => Creatures.Where(c => !c.IsInRoom);

    public Room? RoomOf(UnitId companion) => Rooms.FirstOrDefault(r => r.Companion == companion);
}
