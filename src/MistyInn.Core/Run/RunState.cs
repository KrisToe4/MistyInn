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
        foreach (var unit in Units.Where(u => !u.Def.IsInnkeeper))
        {
            // A starting character counts as hired at ★1 for its base cost, so selling it returns something (D-081).
            unit.Spent = content.Character(unit.Def.Id).BaseCost;
        }

        Silver = content.Balance.StartingSilver;
        Integrity = content.Balance.Integrity;
        UnlockedPlanes = content.UnlockedPlanes.ToList();
        _content = content;
        Rooms = new List<Room>();
        SyncRooms(0);
        foreach (var supply in Enum.GetValues<Supply>())
        {
            Supplies[supply] = content.Balance.StartingSupplies.TryGetValue(supply, out var n) ? n : 0;
        }

        foreach (var fixtureId in content.Balance.StartingFixtures)
        {
            Fixtures.Add(new Fixture(ids.NextFixture(), content.Fixture(fixtureId)));
        }
    }

    private readonly GameContent _content;

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

    /// <summary>The back rooms in use: <c>balance.json</c> <c>rooms</c> plus <c>RoomSlots</c> effects (section 6.4, ITM-06).</summary>
    public List<Room> Rooms { get; }

    /// <summary>Every item owned, in the order bought (ITM-01).</summary>
    public List<OwnedItem> Items { get; } = new();

    public ShopState Shop { get; } = new();

    public IEnumerable<OwnedItem> InnItems => Items.Where(i => i.IsInnItem);

    public IEnumerable<OwnedItem> Storage => Items.Where(i => i.IsInStorage);

    public IReadOnlyList<OwnedItem> CarriedBy(UnitId unit) => Items.Where(i => i.Carrier == unit).ToList();

    /// <summary>
    /// Opens the first <c>rooms + extra</c> back rooms of the layout (in zone order) and locks the rest: a locked room's
    /// door is shut and nothing may be placed inside (BRD-01, D-078). Only called at Dusk, when every room is empty.
    /// </summary>
    public void SyncRooms(int extra)
    {
        var count = Math.Clamp(_content.Balance.Rooms + extra, 0, _content.Layout.Rooms.Count);
        for (var i = 0; i < _content.Layout.Rooms.Count; i++)
        {
            var layout = _content.Layout.Rooms[i];
            var open = Rooms.FirstOrDefault(r => r.Zone == layout.Zone);
            if (i < count && open is null)
            {
                Board.UnlockRoom(layout);
                Rooms.Add(new Room(layout, _content.Balance.Company.DoorHealth));
            }
            else if (i >= count)
            {
                if (open is not null)
                {
                    Rooms.Remove(open);
                }

                Board.LockRoom(layout);
            }
        }

        Rooms.Sort((a, b) => string.CompareOrdinal(a.Zone, b.Zone));
    }

    /// <summary>Where and when recent attacks happened, for <c>Violence</c> (AGI-08, CMP-05). Bounded (INV-05).</summary>
    public List<(TilePosition At, double Time)> Attacks { get; } = new();

    /// <summary>Where units fell this mist, for <c>Blood</c> (AGI-08, ESC-04). Bounded (INV-05).</summary>
    public List<TilePosition> BloodMarks { get; } = new();

    /// <summary>Creature types already given Studied progress this cycle, at Dawn or by leaving satisfied (BST-02).</summary>
    public HashSet<string> StudiedThisCycle { get; } = new();

    /// <summary>Why the run ended, once it has (RUN-03).</summary>
    public RunEndReason? EndReason { get; set; }

    public static Unit NewUnit(GameContent content, IdAllocator ids, UnitTemplate def) =>
        new(ids.NextUnit(), def, def.TechniqueIds.Select(content.Technique).ToList(), content.Balance.RankStatMultipliers[0]);

    /// <summary>A character hired at <paramref name="rank"/> with its preset build for that rank (STF-10).</summary>
    public static Unit HireUnit(GameContent content, IdAllocator ids, string characterId, int rank)
    {
        var unit = NewUnit(content, ids, content.TemplateFor(characterId));
        var character = content.Character(characterId);
        if (rank > 1)
        {
            foreach (var optionId in character.Presets.First(p => p.Rank == rank).Choices)
            {
                ApplyOption(content, unit, character, optionId);
            }
        }

        unit.Rank = rank;
        unit.RankMultiplier = content.Balance.RankStatMultipliers[rank - 1];
        unit.Health = unit.MaxHealth;
        return unit;
    }

    /// <summary>Applies one upgrade option: a new technique, or the mastery of a known one (STF-06, STF-17).</summary>
    public static void ApplyOption(GameContent content, Unit unit, StaffDef character, string optionId)
    {
        var rank = UpgradeTrees.FirstUpgradeRank + unit.Chosen.Count;
        var option = UpgradeTrees.OptionsAt(character, rank).First(o => o.Id == optionId);
        unit.Chosen.Add(option.Id);
        if (option.Mastery)
        {
            unit.Mastered.Add(option.Technique);
        }
        else
        {
            unit.Learn(content.Technique(option.Technique));
        }
    }

    /// <summary>What a unit knows, as the upgrade-tree rules see it (STF-17).</summary>
    public static UpgradeTrees.Build BuildOf(Unit unit) =>
        new(new HashSet<string>(unit.Chosen), unit.Techniques.Select(t => t.Id).ToHashSet(), new HashSet<string>(unit.Mastered));

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
