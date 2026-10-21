using MistyInn.Core.Bestiary;
using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Logging;
using MistyInn.Core.Randomness;
using MistyInn.Core.Run;
using MistyInn.Core.Simulation;

namespace MistyInn.Core;

/// <summary>
/// The single entry point into Core (ARC-01). It holds the run, and its state changes only through <see cref="Tick"/>
/// and <see cref="Execute"/> (ARC-03); everything else is a read-only query. A new run is a new session; the bestiary
/// and the ancestry record passed in outlive it (BST-04, STF-16).
/// </summary>
public sealed class GameSession : IEventSink
{
    /// <summary>Fixed simulation steps per game second (SIM-02).</summary>
    public const int StepsPerSecond = 30;

    /// <summary>
    /// At most this many steps run in one <see cref="Tick"/>; a longer frame drops the backlog instead of freezing the
    /// game to catch up. Dropping real time never changes the outcome, only how fast it is shown (INV-02).
    /// </summary>
    public const int MaxStepsPerTick = 120;

    private const double StepSeconds = 1.0 / StepsPerSecond;

    /// <summary>Mixed into the run's starting random state to seed the shop's own stream (D-080).</summary>
    private const ulong ShopStreamSalt = 0x5348_4F50_5354_524DUL;

    private static readonly int[] AllowedSpeeds = [0, 1, 2, 4];

    private readonly DomainEventBuffer _events;
    private readonly CycleLog _cycleLog;
    private readonly RunState _run;
    private readonly InnSense _sense;
    private readonly BestiaryRecorder _bestiary;
    private readonly ServiceSystem _service;
    private readonly CreatureBehaviourSystem _behaviour;
    private readonly AgitationSystem _agitation;
    private readonly ArrivalSystem _arrivals;
    private readonly InnDamage _damage;
    private readonly CompanySystem _company;
    private readonly CombatSystem _combat;
    private readonly AncestryObserver _ancestry;
    private readonly Modifiers _modifiers;
    private readonly ShopSystem _shop;

    // Real time owed to the simulation, in TimeSpan ticks × StepsPerSecond, so whole steps are exact integers.
    private long _owed;
    private long _steps;

    public GameSession(
        IRandomSource random,
        GameSessionLimits? limits = null,
        GameContent? content = null,
        CreatureBestiary? bestiary = null,
        AncestryRecord? ancestry = null)
    {
        ArgumentNullException.ThrowIfNull(random);

        limits ??= GameSessionLimits.Default;
        Random = random;
        Ids = new IdAllocator();
        _events = new DomainEventBuffer(limits.EventCapacity);
        _cycleLog = new CycleLog(limits.CycleLogCapacity);
        Content = content ?? GameContent.Default;
        Bestiary = bestiary ?? new CreatureBestiary();
        Ancestry = ancestry ?? new AncestryRecord();
        _run = new RunState(Content, Ids);

        var wanderTiles = _run.Board.WalkableTilesIn(Content.WanderZones);
        if (wanderTiles.Count == 0)
        {
            throw new ArgumentException("The wander zones have no walkable tiles.", nameof(content));
        }

        _sense = new InnSense(Content, _run);
        _bestiary = new BestiaryRecorder(Content, Bestiary, this);
        _ancestry = new AncestryObserver(Content, Ancestry, this);
        _modifiers = new Modifiers(Content, _run, _ancestry);
        _service = new ServiceSystem(Content, _run, this, _sense, _ancestry);
        _behaviour = new CreatureBehaviourSystem(Content, _run, random, wanderTiles, _service, _sense);
        _damage = new InnDamage(Content, _run, this);
        _company = new CompanySystem(Content, _run, this, _sense, _service, _behaviour);
        _agitation = new AgitationSystem(Content, _run, this, _sense, _bestiary, _behaviour, _damage);
        _combat = new CombatSystem(Content, _run, this, _damage, _bestiary, _behaviour, _company, _ancestry);
        _arrivals = new ArrivalSystem(Content, _run, random, Ids, this, _behaviour, _bestiary, _modifiers);

        // The shop's own stream, derived from the run's starting state without drawing from it (D-080).
        var start = random.GetState();
        _shop = new ShopSystem(Content, _run, new SeededRandom(start.S0 ^ ((start.S2 << 17) | (start.S2 >> 47)) ^ ShopStreamSalt), _modifiers);

        // The first Dusk is the starting state; views read it through the queries, so no events are raised for it.
        StartCycle(1, raiseEvents: false);
    }

    public GameContent Content { get; }

    /// <summary>The profile's bestiary (BST-01). Pass the same instance to the next run's session.</summary>
    public CreatureBestiary Bestiary { get; }

    /// <summary>The profile's revealed ancestries (STF-16, D-082). Pass the same instance to the next run's session.</summary>
    public AncestryRecord Ancestry { get; }

    public BoardLayout Layout => Content.Layout;

    public Phase Phase => _run.Phase;

    public int Cycle => _run.Cycle;

    /// <summary>Mist speed: 0 (paused), 1, 2 or 4 (SCP-13).</summary>
    public int Speed => _run.Speed;

    /// <summary>The run's silver (SHP-01).</summary>
    public int Silver => _run.Silver;

    /// <summary>Supplies in stock (FIX-06).</summary>
    public IReadOnlyDictionary<Supply, int> Supplies => _run.Supplies;

    /// <summary>The inn's integrity (BRD-06); the run is lost if it is 0 at the end of a mist (RUN-03).</summary>
    public double Integrity => _run.Integrity;

    public double MaxIntegrity => Content.Balance.Integrity;

    /// <summary>Why the run ended, or null while it goes on (RUN-03).</summary>
    public RunEndReason? EndReason => _run.EndReason;

    /// <summary>The content filters in force (SET-04). They change what is shown, never what happens (CMP-06).</summary>
    public ContentFilters Filters { get; private set; } = ContentFilters.None;

    /// <summary>The back rooms and who is in them (CMP-04).</summary>
    public IReadOnlyList<RoomSnapshot> Rooms => _run.Rooms.Select(r => r.Snapshot()).ToArray();

    /// <summary>Game time since the mist began (SIM-01); zero at Dusk.</summary>
    public TimeSpan Time => TimeSpan.FromTicks(_steps * TimeSpan.TicksPerSecond / StepsPerSecond);

    public TimeSpan MistDuration => TimeSpan.FromSeconds(Content.Balance.MistDurationSeconds);

    /// <summary>Time left until dawn: the whole mist at Dusk, nothing at Dawn (CYC-02).</summary>
    public TimeSpan TimeToDawn => Phase switch
    {
        Phase.Dusk => MistDuration,
        Phase.Mist => MistDuration > Time ? MistDuration - Time : TimeSpan.Zero,
        _ => TimeSpan.Zero,
    };

    /// <summary>How far the next step is due, 0 to 1, for interpolating views (SIM-03).</summary>
    public double InterpolationAlpha =>
        Phase == Phase.Mist ? Math.Clamp(_owed / (double)TimeSpan.TicksPerSecond, 0, 1) : 0;

    public IReadOnlyList<PlaneDef> ConvergingPlanes => _run.ConvergingPlanes.AsReadOnly();

    public IReadOnlyList<UnitSnapshot> Units =>
        _run.Units.Select(u => u.Snapshot(_run.CarriedBy(u.Id), _ancestry.RevealedAncestry(u))).ToArray();

    /// <summary>Every item owned: staff items carried or in storage, and inn items (ITM-01).</summary>
    public IReadOnlyList<ItemSnapshot> Items => _run.Items.Select(i => i.Snapshot()).ToArray();

    /// <summary>The synergies with their counts on the board (SYN-01, SYN-03).</summary>
    public IReadOnlyList<SynergyStatus> Synergies => _modifiers.Synergies;

    /// <summary>The back rooms not in use yet (D-078).</summary>
    public IReadOnlySet<string> LockedRooms => _run.Board.LockedZones;

    /// <summary>The Dusk shop (UI-03).</summary>
    public ShopSnapshot Shop => new(
        _run.Shop.Staff.Select(o => o is null ? null : ViewOf(o)).ToList(),
        _run.Shop.Items.Select(o => o is null ? null : ViewOf(o)).ToList(),
        _run.Shop.Locked,
        _shop.RerollCost);

    /// <summary>
    /// A unit's upgrade tree for its panel and the upgrade dialog (UI-06, UI-08): every option, chosen, available (for
    /// the pending pick) or locked. Empty for the innkeeper.
    /// </summary>
    public IReadOnlyList<UpgradeOptionView> UpgradeTree(UnitId unitId)
    {
        if (_run.FindUnit(unitId) is not { Def.IsInnkeeper: false } unit)
        {
            return Array.Empty<UpgradeOptionView>();
        }

        var character = Content.Character(unit.Def.Id);
        var build = RunState.BuildOf(unit);
        return character.Tree.OrderBy(r => r.Rank).SelectMany(r => r.Options.Select(o => new UpgradeOptionView(
            r.Rank,
            o.Id,
            o.Technique,
            o.Mastery,
            Content.Technique(o.Technique).Class,
            unit.Chosen.Contains(o.Id) ? UpgradeOptionState.Chosen
            : unit.UpgradePending && r.Rank == unit.Rank && UpgradeTrees.IsAvailable(o, build) ? UpgradeOptionState.Available
            : UpgradeOptionState.Locked))).ToList();
    }

    public IReadOnlyList<CreatureSnapshot> Creatures => _run.Creatures.Select(c => c.Snapshot()).ToArray();

    /// <summary>Every fixture owned, placed or in storage (FIX-05).</summary>
    public IReadOnlyList<FixtureSnapshot> Fixtures => _run.Fixtures.Select(f => f.Snapshot()).ToArray();

    public int StaffOnBoard => _run.StaffOnBoard;

    /// <summary>3 + 1 per two cycles survived, up to 9; the innkeeper does not count (section 6.4).</summary>
    public int MaxStaffOnBoard =>
        Math.Min(Content.Balance.MaxStaffSlots, Content.Balance.BaseStaffSlots + (Cycle - 1) / 2)
        + _modifiers.InnWide(EffectKind.BoardSlots);

    /// <summary>The current (or last) mist phase's cycle log, read-only (INV-12).</summary>
    public IReadOnlyList<CycleLogEntry> CycleLogEntries => _cycleLog.Entries;

    /// <summary>Events dropped because the buffer filled up between drains (D-039).</summary>
    public long DroppedEventCount => _events.DroppedCount;

    /// <summary>
    /// The Dusk forecast (BST-03): for each converging plane, the creatures it may send and what the bestiary knows of
    /// them. What is not known yet is null.
    /// </summary>
    public IReadOnlyList<ForecastPlane> Forecast =>
        _run.ConvergingPlanes.Select(plane =>
        {
            var total = plane.Creatures.Sum(c => c.Weight);
            return new ForecastPlane(plane, plane.Creatures.Select(w =>
            {
                var def = Content.Creature(w.Creature);
                var level = Bestiary.LevelOf(def, Content.Balance.Bestiary);
                return new ForecastCreature(
                    def.Id,
                    level,
                    w.Weight / total,
                    level >= BestiaryLevel.Seen ? def.Name : null,
                    level >= BestiaryLevel.Seen ? def.Size : null,
                    level >= BestiaryLevel.Studied ? def.Appetites : null,
                    level >= BestiaryLevel.Understood ? def.Triggers : null);
            }).ToList());
        }).ToList();

    /// <summary>A technique's name as shown: Company becomes Hosting with the suggestive filter on (CMP-06).</summary>
    public string TechniqueName(string techniqueId)
    {
        var technique = Content.Technique(techniqueId);
        return technique.Technique == Technique.Company && Filters.NoSuggestive ? "Hosting" : technique.Name;
    }

    /// <summary>The flavour line for a log entry, picked with the filters in force now (RAT-09, DAT-06), or null.</summary>
    public string? FlavourText(CycleLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return entry.Flavour is { } context ? FlavourPicker.Pick(Content.Flavour, context, Filters, entry.Sequence)?.Text : null;
    }

    /// <summary>The run's random source, for the systems that need it (ARC-04).</summary>
    internal IRandomSource Random { get; }

    /// <summary>Issues entity IDs (ARC-13).</summary>
    internal IdAllocator Ids { get; }

    /// <summary>
    /// Advances the mist by <paramref name="delta"/> of real time × the speed, in fixed steps (SIM-02, INV-03). Does
    /// nothing at Dusk, at Dawn or at speed 0. Pausing the game is simply not calling this.
    /// </summary>
    public void Tick(TimeSpan delta)
    {
        if (delta < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Game time never runs backwards.");
        }

        if (Phase != Phase.Mist || _run.Speed == 0)
        {
            return;
        }

        _owed += delta.Ticks * _run.Speed * StepsPerSecond;
        var steps = 0;
        while (_owed >= TimeSpan.TicksPerSecond && Phase == Phase.Mist)
        {
            if (steps == MaxStepsPerTick)
            {
                _owed = 0;
                break;
            }

            _owed -= TimeSpan.TicksPerSecond;
            Step();
            steps++;
        }

        if (Phase != Phase.Mist)
        {
            _owed = 0;
        }
    }

    /// <summary>Runs the rest of the mist phase at once, ignoring speed (ARC-05). For tests and tools.</summary>
    public void RunMistToDawn()
    {
        if (Phase != Phase.Mist)
        {
            throw new InvalidOperationException($"RunMistToDawn needs the mist phase; the phase is {Phase}.");
        }

        while (Phase == Phase.Mist)
        {
            Step();
        }

        _owed = 0;
    }

    /// <summary>Validates and applies a command (ARC-11). A refused command changes nothing.</summary>
    public CommandResult Execute(GameCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command is SetContentFilters filters)
        {
            return HandleSetContentFilters(filters);
        }

        // While an upgrade pick is pending, nothing else happens until it is made (STF-08, D-081).
        if (_run.Units.Any(u => u.UpgradePending) && command is not ChooseUpgrade)
        {
            return CommandResult.Fail(CommandFailure.UpgradePending);
        }

        if (command is SetSpeed setSpeed)
        {
            return HandleSetSpeed(setSpeed);
        }

        if (Phase is Phase.Mist or Phase.RunOver)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        var result = command switch
        {
            PlaceUnit place => HandlePlaceUnit(place),
            BenchUnit bench => HandleBenchUnit(bench),
            BeginMist => HandleBeginMist(),
            ContinueFromDawn => HandleContinueFromDawn(),
            BuildFixture build => HandleBuildFixture(build),
            PlaceFixture place => HandlePlaceFixture(place),
            StoreFixture store => HandleStoreFixture(store),
            SellFixture sell => HandleSellFixture(sell),
            BuySupplies buy => HandleBuySupplies(buy),
            RepairIntegrity repair => HandleRepairIntegrity(repair),
            DevAddStaff add => HandleDevAddStaff(add),
            DevSetConvergingPlanes planes => HandleDevSetConvergingPlanes(planes),
            BuyOffer buy => HandleBuyOffer(buy),
            Reroll => HandleReroll(),
            LockShop lockShop => HandleLockShop(lockShop),
            ChooseUpgrade choose => HandleChooseUpgrade(choose),
            SellUnit sell => HandleSellUnit(sell),
            EquipItem equip => HandleEquipItem(equip),
            UnequipItem unequip => HandleUnequipItem(unequip),
            SellItem sell => HandleSellItem(sell),
            _ => CommandResult.Fail(CommandFailure.NotSupported),
        };

        // Items, the board and the roster decide synergies and effects; recount after every Dusk change (D-077).
        if (result.Succeeded && Phase == Phase.Dusk)
        {
            _modifiers.Refresh(heal: true);
        }

        return result;
    }

    /// <summary>Why <see cref="PlaceUnit"/> would be refused, or <see cref="CommandFailure.None"/>; for the drag cursor.</summary>
    public CommandFailure CheckPlaceUnit(UnitId unitId, Tile tile)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandFailure.WrongPhase;
        }

        var unit = _run.FindUnit(unitId);
        if (unit is null)
        {
            return CommandFailure.UnknownUnit;
        }

        if (!_run.Board.IsPlaceableFloor(tile))
        {
            return CommandFailure.TileBlocked;
        }

        var occupant = _run.Board.UnitAt(tile);
        if (occupant is { } other && other != unitId)
        {
            return CommandFailure.TileOccupied;
        }

        if (unit.Tile is null && !unit.Def.IsInnkeeper && _run.StaffOnBoard >= MaxStaffOnBoard)
        {
            return CommandFailure.BoardFull;
        }

        return CommandFailure.None;
    }

    /// <summary>
    /// Why <see cref="PlaceFixture"/> would be refused, or <see cref="CommandFailure.None"/>; for the drag cursor
    /// (BRD-02, BRD-03, BRD-05, FIX-01).
    /// </summary>
    public CommandFailure CheckPlaceFixture(FixtureId fixtureId, Tile origin, int rotation)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandFailure.WrongPhase;
        }

        var fixture = _run.FindFixture(fixtureId);
        if (fixture is null)
        {
            return CommandFailure.UnknownFixture;
        }

        if (rotation is < 0 or > 3)
        {
            return CommandFailure.InvalidValue;
        }

        var tiles = Fixture.TilesAt(fixture.Def, origin, rotation);
        var own = fixture.Tiles;
        _run.Board.RemoveFixture(own);
        try
        {
            foreach (var tile in tiles)
            {
                if (_run.Board.FixtureAt(tile) is not null || _run.Board.UnitAt(tile) is not null)
                {
                    return CommandFailure.TileOccupied;
                }

                if (_run.Board.IsFixtureForbidden(tile) || !_run.Board.IsPlaceableFloor(tile))
                {
                    return CommandFailure.TileBlocked;
                }

                if (fixture.Def.Zone is { } zone && Layout.ZoneAt(tile) != zone)
                {
                    return CommandFailure.WrongZone;
                }
            }

            return _run.Board.WouldBlockPath(tiles) ? CommandFailure.TileBlocked : CommandFailure.None;
        }
        finally
        {
            _run.Board.PutFixture(fixture.Id, own);
        }
    }

    /// <summary>Returns the events raised since the last drain, oldest first (ARC-12).</summary>
    public IReadOnlyList<DomainEvent> DrainEvents() => _events.Drain();

    /// <summary>A hash of the whole simulation state; equal seeds and commands give equal hashes (INV-02, TST-04).</summary>
    public ulong ComputeStateHash()
    {
        var h = new StateHasher()
            .Add((int)Phase).Add(Cycle).Add(_steps).Add(_owed).Add(_run.Speed).Add(_run.Silver)
            .Add(_arrivals.NextArrivalSeconds).Add(_run.Integrity).Add(_run.Attacks.Count).Add(_run.BloodMarks.Count);

        foreach (var supply in Enum.GetValues<Supply>())
        {
            h.Add(_run.Supplies[supply]);
        }

        var rng = Random.GetState();
        h.Add(rng.S0).Add(rng.S1).Add(rng.S2).Add(rng.S3);

        var ids = Ids.GetState();
        h.Add(ids.LastUnit).Add(ids.LastCreature).Add(ids.LastFixture).Add(ids.LastItem);

        var shopRng = _shop.Random.GetState();
        h.Add(shopRng.S0).Add(shopRng.S1).Add(shopRng.S2).Add(shopRng.S3)
            .Add(_run.Shop.Locked).Add(_run.Shop.RerollsThisCycle).Add(_run.Rooms.Count);
        foreach (var offer in _run.Shop.Staff)
        {
            h.Add(offer?.CharacterId ?? "").Add(offer?.Rank ?? 0).Add(offer?.Price ?? 0);
        }

        foreach (var offer in _run.Shop.Items)
        {
            h.Add(offer?.ItemId ?? "").Add(offer?.Price ?? 0);
        }

        foreach (var item in _run.Items)
        {
            h.Add(item.Id.Value).Add(item.Def.Id).Add(item.Carrier?.Value ?? -1).Add(item.Paid);
        }

        foreach (var revealed in Ancestry.Revealed)
        {
            h.Add(revealed);
        }

        foreach (var plane in _run.ConvergingPlanes)
        {
            h.Add(plane.Id);
        }

        foreach (var unit in _run.Units)
        {
            h.Add(unit.Id.Value).Add(unit.Tile?.X ?? -1).Add(unit.Tile?.Y ?? -1)
                .Add(unit.Session?.Technique.Id ?? "").Add(unit.Session?.EndsAt ?? -1)
                .Add(unit.Position.X).Add(unit.Position.Y).Add(unit.Health).Add((int)unit.Activity).Add(unit.Shaken)
                .Add(unit.Engaging?.Value ?? -1).Add(unit.NextAttackAt).Add(unit.RestUntil).Add(unit.Path.Count)
                .Add(unit.Rank).Add(unit.Spent).Add(unit.UpgradePending).Add(string.Join(",", unit.Chosen));
            foreach (var target in unit.Session?.Targets ?? [])
            {
                h.Add(target.Value);
            }
        }

        foreach (var f in _run.Fixtures)
        {
            h.Add(f.Id.Value).Add(f.Def.Id).Add(f.Origin?.X ?? -1).Add(f.Origin?.Y ?? -1).Add(f.Rotation).Add(f.Health);
        }

        foreach (var c in _run.Creatures)
        {
            h.Add(c.Id.Value).Add(c.Def.Id).Add((int)c.Activity).Add(c.IdleUntil)
                .Add(c.Position.X).Add(c.Position.Y).Add(c.PreviousPosition.X).Add(c.PreviousPosition.Y)
                .Add(c.Agitation).Add((int)c.State).Add(c.Spot?.X ?? -1).Add(c.Spot?.Y ?? -1).Add(c.ServedBy ?? "")
                .Add(c.UnservedSince).Add(c.NextBreakAt).Add(c.WasServed).Add(c.Frightened)
                .Add(c.Path.Count).Add(c.Health).Add((int)c.Stage).Add(c.StageSince).Add(c.EngagedBy?.Value ?? -1)
                .Add(c.GrappledBy?.Value ?? -1).Add(c.NextAttackAt).Add(c.CompanyWith?.Value ?? -1).Add(c.CompanySince)
                .Add(c.HadRoom);
            foreach (var tile in c.Path)
            {
                h.Add(tile.X).Add(tile.Y);
            }

            foreach (var trigger in c.ActiveTriggers.Order())
            {
                h.Add((int)trigger);
            }
        }

        foreach (var room in _run.Rooms)
        {
            h.Add(room.Zone).Add(room.DoorHealth).Add(room.Companion?.Value ?? -1).Add(room.Guest?.Value ?? -1)
                .Add(room.EndsAt ?? -1);
        }

        foreach (var (id, seen, study, triggers, banish) in Bestiary.State())
        {
            h.Add(id).Add(seen).Add(study).Add(banish);
            foreach (var trigger in triggers)
            {
                h.Add((int)trigger);
            }
        }

        return h.Value;
    }

    /// <summary>The soothe rates settled in the last step (AGI-06), for tests.</summary>
    internal IReadOnlyDictionary<CreatureId, (double Rate, string By)> ServiceRates => _service.Rates;

    /// <summary>The service rules (STF-04), for tests.</summary>
    internal ServiceSystem ServiceForTests => _service;

    /// <summary>For tests: puts a creature on a tile during the mist, as if it had just arrived there (CRE-04).</summary>
    internal CreatureId SpawnCreatureForTests(string creatureDefId, Tile at)
    {
        if (Phase != Phase.Mist)
        {
            throw new InvalidOperationException("Creatures only come during the mist.");
        }

        var def = Content.Creature(creatureDefId);
        var creature = new Creature(Ids.NextCreature(), def, at);
        _run.Creatures.Add(creature);
        _bestiary.Seen(def);
        _behaviour.Decide(creature, MistSeconds, arriving: false);
        return creature.Id;
    }

    /// <summary>For tests: the live creature.</summary>
    internal Creature CreatureForTests(CreatureId id) => _run.FindCreature(id) ?? throw new KeyNotFoundException(id.ToString());

    /// <summary>For tests: the live fixture.</summary>
    internal Fixture FixtureForTests(FixtureId id) => _run.FindFixture(id) ?? throw new KeyNotFoundException(id.ToString());

    /// <summary>For tests: the live unit.</summary>
    internal Unit UnitForTests(UnitId id) => _run.FindUnit(id) ?? throw new KeyNotFoundException(id.ToString());

    /// <summary>For tests: the run's state, to set integrity or look at rooms.</summary>
    internal RunState RunForTests => _run;

    /// <summary>For tests: the shop's draw rules (SHP-03, SHP-04).</summary>
    internal ShopSystem ShopForTests => _shop;

    /// <summary>For tests: inn-wide effects and synergy counts (ITM-04, SYN-01).</summary>
    internal Modifiers ModifiersForTests => _modifiers;

    /// <summary>For tests: working fixture effects and adjacency (FIX-03, FIX-04).</summary>
    internal InnSense SenseForTests => _sense;

    /// <summary>For tests: replaces the shop's offers with chosen ones (null for an empty slot).</summary>
    internal void SetShopForTests(IEnumerable<StaffOffer?> staff, IEnumerable<ItemOffer?>? items = null)
    {
        _run.Shop.Staff.Clear();
        _run.Shop.Staff.AddRange(staff);
        if (items is not null)
        {
            _run.Shop.Items.Clear();
            _run.Shop.Items.AddRange(items);
        }
    }

    internal void Raise(DomainEvent domainEvent) => _events.Add(domainEvent);

    internal void Log(string kind, string cause, EntityRef? subject = null, EntityRef? source = null, string? flavour = null) =>
        _cycleLog.Record(Time, kind, cause, subject, source, flavour);

    void IEventSink.Raise(DomainEvent domainEvent) => Raise(domainEvent);

    void IEventSink.Log(string kind, string cause, EntityRef? subject, EntityRef? source, string? flavour) =>
        Log(kind, cause, subject, source, flavour);

    void IEventSink.EndRun(RunEndReason reason) => EndRun(reason);

    /// <summary>The run is lost (RUN-03): nothing more happens in this session.</summary>
    private void EndRun(RunEndReason reason)
    {
        if (_run.Phase == Phase.RunOver)
        {
            return;
        }

        _run.Phase = Phase.RunOver;
        _run.EndReason = reason;
        Raise(new RunEnded(Time, reason));
        Log("run.lost", reason == RunEndReason.InnkeeperFell ? "innkeeper.fell" : "integrity.zero");
    }

    private double MistSeconds => _steps / (double)StepsPerSecond;

    private void Step()
    {
        _steps++;
        var now = MistSeconds;
        foreach (var unit in _run.Units)
        {
            unit.PreviousPosition = unit.Position;
        }

        _arrivals.Update(now);
        _behaviour.Step(StepSeconds, now);
        _service.Step(now);
        _company.Step(StepSeconds, now);
        _agitation.Step(StepSeconds, now, _service.Rates);
        _combat.Step(StepSeconds, now);

        if (Phase != Phase.Mist)
        {
            return;
        }

        if (now >= Content.Balance.MistDurationSeconds)
        {
            BreakDawn();
        }
    }

    private void StartCycle(int cycle, bool raiseEvents)
    {
        _run.Cycle = cycle;
        _run.Phase = Phase.Dusk;
        _steps = 0;
        _owed = 0;

        var count = PlaneDraw.CountFor(cycle, Content.Balance, _run.UnlockedPlanes.Count);
        var planeIds = PlaneDraw.Draw(Random, _run.UnlockedPlanes, count, _run.PreviousPlaneSet);
        _run.PreviousPlaneSet = planeIds;
        _run.ConvergingPlanes.Clear();
        _run.ConvergingPlanes.AddRange(planeIds.Select(Content.Plane));

        // After the planes, so the home-plane weight sees this cycle's planes (SHP-04).
        _shop.NewDusk();
        _modifiers.Refresh(heal: true);

        if (raiseEvents)
        {
            Raise(new PhaseChanged(Time, Cycle, Phase.Dusk));
            Raise(new ConvergingPlanesDrawn(Time, Cycle, planeIds));
            Raise(new ShopChanged(Time));
        }
    }

    /// <summary>
    /// Dawn (CYC-03, CRE-05, CYC-04): creatures that stayed calm leave their tribute, served ones add to the bestiary
    /// (BST-02), the cycle's income arrives and every creature dissolves. Fallen staff are wounded or dead (ESC-09);
    /// everyone else recovers (STF-12); fixtures are repaired for free (D-072). With no integrity left, the run is lost
    /// (RUN-03).
    /// </summary>
    private void BreakDawn()
    {
        _company.EndAll();
        var studied = new HashSet<string>();
        var tributeBonus = _modifiers.InnWide(EffectKind.Tribute);
        foreach (var creature in _run.Creatures.Where(c => !c.IsTurned))
        {
            // Inn items and the Gambler synergy add to every tribute (ITM-06, SYN-02).
            var tribute = Math.Max(0, (int)Math.Round(creature.Def.Tribute, MidpointRounding.AwayFromZero) + tributeBonus);
            _run.Silver += tribute;
            Raise(new TributeLeft(Time, creature.Id, tribute));
            Log("tribute.left", "dawn.calm", creature.Id.ToRef());

            if (creature.WasServed && studied.Add(creature.Def.Id))
            {
                _bestiary.Studied(creature.Def);
            }
        }

        _run.Silver += Math.Max(0, Content.Balance.IncomePerCycle + _modifiers.InnWide(EffectKind.Income));
        Log("silver.income", "dawn.income");
        Raise(new SilverChanged(Time, _run.Silver));

        foreach (var creature in _run.Creatures)
        {
            Raise(new CreatureDissolved(Time, creature.Id));
            Log("creature.dissolved", "dawn", creature.Id.ToRef());
        }

        _run.Creatures.Clear();
        _run.Reservations.Clear();
        _run.Attacks.Clear();
        _run.BloodMarks.Clear();
        _combat.Reset();

        foreach (var unit in _run.Units.Where(u => u.FellThisCycle && !u.Def.IsInnkeeper).OrderBy(u => u.Id.Value).ToList())
        {
            if (Random.NextDouble() < Content.Balance.Combat.WoundChance)
            {
                Raise(new StaffWounded(Time, unit.Id));
                Log("staff.wounded", "dawn.roll", unit.Id.ToRef());
                continue;
            }

            if (unit.Tile is { } post)
            {
                _run.Board.RemoveUnit(post);
            }

            _run.Units.Remove(unit);
            Raise(new StaffDied(Time, unit.Id, unit.Def.Id, unit.Def.Name));
            Log("staff.died", "dawn.roll", unit.Id.ToRef());
        }

        foreach (var unit in _run.Units)
        {
            unit.Shaken = unit.FellThisCycle;
            unit.FellThisCycle = false;
            unit.Health = unit.MaxHealth;
            unit.ResetToPost();
        }

        foreach (var fixture in _run.PlacedFixtures.Where(f => f.Health < f.Def.Health))
        {
            fixture.Health = fixture.Def.Health;
            Raise(new FixtureRepaired(Time, fixture.Id));
            Log("fixture.repaired", "dawn.free", fixture.Id.ToRef());
        }

        foreach (var room in _run.Rooms)
        {
            room.DoorHealth = room.MaxDoorHealth;
        }

        if (_run.Integrity <= 0)
        {
            EndRun(RunEndReason.InnDestroyed);
            return;
        }

        _run.Phase = Phase.Dawn;
        Raise(new PhaseChanged(Time, Cycle, Phase.Dawn));
        Log("phase.dawn", "mist.timer");
    }

    private void RefreshSeats()
    {
        foreach (var fixture in _run.PlacedFixtures)
        {
            fixture.Seats = _run.Board.SeatsAround(fixture.Tiles, fixture.Def.Seats);
        }
    }

    private CommandResult HandleSetSpeed(SetSpeed command)
    {
        if (!AllowedSpeeds.Contains(command.Multiplier))
        {
            return CommandResult.Fail(CommandFailure.InvalidValue);
        }

        if (_run.Speed != command.Multiplier)
        {
            _run.Speed = command.Multiplier;
            Raise(new SpeedChanged(Time, command.Multiplier));
        }

        return CommandResult.Ok;
    }

    private CommandResult HandlePlaceUnit(PlaceUnit command)
    {
        var failure = CheckPlaceUnit(command.Unit, command.Tile);
        if (failure != CommandFailure.None)
        {
            return CommandResult.Fail(failure);
        }

        var unit = _run.FindUnit(command.Unit)!;
        if (unit.Tile == command.Tile)
        {
            return CommandResult.Ok;
        }

        if (unit.Tile is { } from)
        {
            _run.Board.RemoveUnit(from);
        }

        unit.Tile = command.Tile;
        unit.ResetToPost();
        _run.Board.PutUnit(unit.Id, command.Tile);
        Raise(new UnitPlaced(Time, unit.Id, command.Tile));
        return CommandResult.Ok;
    }

    private CommandResult HandleBenchUnit(BenchUnit command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        var unit = _run.FindUnit(command.Unit);
        if (unit is null)
        {
            return CommandResult.Fail(CommandFailure.UnknownUnit);
        }

        if (unit.Def.IsInnkeeper)
        {
            return CommandResult.Fail(CommandFailure.CannotBench);
        }

        if (unit.Tile is not { } tile)
        {
            return CommandResult.Ok;
        }

        _run.Board.RemoveUnit(tile);
        unit.Tile = null;
        Raise(new UnitBenched(Time, unit.Id));
        return CommandResult.Ok;
    }

    private CommandResult HandleBeginMist()
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        if (_run.Innkeeper.Tile is null)
        {
            return CommandResult.Fail(CommandFailure.InnkeeperNotPlaced);
        }

        _cycleLog.Clear();
        _steps = 0;
        _owed = 0;
        _run.Phase = Phase.Mist;
        foreach (var unit in _run.Units)
        {
            unit.ResetToPost();
        }

        RefreshSeats();
        _modifiers.Refresh(heal: true);
        foreach (var room in _run.Rooms)
        {
            room.Shields = _sense.ShieldsFor(room);
        }

        _arrivals.StartMist();
        Raise(new PhaseChanged(Time, Cycle, Phase.Mist));
        Log("phase.mist", "player.began");
        return CommandResult.Ok;
    }

    private CommandResult HandleContinueFromDawn()
    {
        if (Phase != Phase.Dawn)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        StartCycle(Cycle + 1, raiseEvents: true);
        return CommandResult.Ok;
    }

    private CommandResult HandleBuildFixture(BuildFixture command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        if (Content.Fixtures.FirstOrDefault(f => f.Id == command.FixtureDefId) is not { } def)
        {
            return CommandResult.Fail(CommandFailure.UnknownFixture);
        }

        if (_run.Silver < def.Cost)
        {
            return CommandResult.Fail(CommandFailure.NotEnoughSilver);
        }

        _run.Silver -= def.Cost;
        var fixture = new Fixture(Ids.NextFixture(), def);
        _run.Fixtures.Add(fixture);
        Raise(new FixtureBuilt(Time, fixture.Id, def.Id));
        Raise(new SilverChanged(Time, _run.Silver));
        return CommandResult.Ok;
    }

    private CommandResult HandlePlaceFixture(PlaceFixture command)
    {
        var failure = CheckPlaceFixture(command.Fixture, command.Tile, command.Rotation);
        if (failure != CommandFailure.None)
        {
            return CommandResult.Fail(failure);
        }

        var fixture = _run.FindFixture(command.Fixture)!;
        _run.Board.RemoveFixture(fixture.Tiles);
        fixture.Place(command.Tile, command.Rotation);
        _run.Board.PutFixture(fixture.Id, fixture.Tiles);
        RefreshSeats();
        Raise(new FixturePlaced(Time, fixture.Id));
        return CommandResult.Ok;
    }

    private CommandResult HandleStoreFixture(StoreFixture command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        if (_run.FindFixture(command.Fixture) is not { } fixture)
        {
            return CommandResult.Fail(CommandFailure.UnknownFixture);
        }

        if (!fixture.IsPlaced)
        {
            return CommandResult.Ok;
        }

        _run.Board.RemoveFixture(fixture.Tiles);
        fixture.Store();
        RefreshSeats();
        Raise(new FixtureStored(Time, fixture.Id));
        return CommandResult.Ok;
    }

    private CommandResult HandleSellFixture(SellFixture command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        if (_run.FindFixture(command.Fixture) is not { } fixture)
        {
            return CommandResult.Fail(CommandFailure.UnknownFixture);
        }

        _run.Board.RemoveFixture(fixture.Tiles);
        _run.Fixtures.Remove(fixture);
        RefreshSeats();
        var refund = fixture.Def.Cost / 2;
        _run.Silver += refund;
        Raise(new FixtureSold(Time, fixture.Id, refund));
        Raise(new SilverChanged(Time, _run.Silver));
        return CommandResult.Ok;
    }

    private CommandResult HandleSetContentFilters(SetContentFilters command)
    {
        ArgumentNullException.ThrowIfNull(command.Filters);
        if (Filters != command.Filters)
        {
            Filters = command.Filters;
            Raise(new ContentFiltersChanged(Time, Filters));
        }

        return CommandResult.Ok;
    }

    /// <summary>Integrity costs 1 silver per 5 points at Dawn; only what is missing is charged (BRD-07, D-072).</summary>
    private CommandResult HandleRepairIntegrity(RepairIntegrity command)
    {
        if (Phase != Phase.Dawn)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        if (command.Silver < 1)
        {
            return CommandResult.Fail(CommandFailure.InvalidValue);
        }

        var missing = MaxIntegrity - _run.Integrity;
        if (missing <= 0)
        {
            return CommandResult.Fail(CommandFailure.NothingToRepair);
        }

        var perSilver = Content.Balance.Combat.IntegrityPerSilver;
        var spend = Math.Min(command.Silver, (int)Math.Ceiling(missing / perSilver));
        if (_run.Silver < spend)
        {
            return CommandResult.Fail(CommandFailure.NotEnoughSilver);
        }

        _run.Silver -= spend;
        _run.Integrity = Math.Min(MaxIntegrity, _run.Integrity + spend * perSilver);
        Raise(new IntegrityRepaired(Time, _run.Integrity, spend));
        Raise(new SilverChanged(Time, _run.Silver));
        return CommandResult.Ok;
    }

    private CommandResult HandleDevAddStaff(DevAddStaff command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        if (Content.Characters.All(c => c.Id != command.CharacterId))
        {
            return CommandResult.Fail(CommandFailure.InvalidValue);
        }

        if (_run.Units.Any(u => u.Def.Id == command.CharacterId))
        {
            return CommandResult.Fail(CommandFailure.AlreadyOwned);
        }

        if (_run.Units.Count(u => !u.Def.IsInnkeeper && u.Tile is null) >= Content.Balance.BenchSize)
        {
            return CommandResult.Fail(CommandFailure.BenchFull);
        }

        var unit = RunState.NewUnit(Content, Ids, Content.TemplateFor(command.CharacterId));
        unit.Spent = Content.Character(command.CharacterId).BaseCost;
        _run.Units.Add(unit);
        Raise(new UnitJoined(Time, unit.Id));
        return CommandResult.Ok;
    }

    private CommandResult HandleDevSetConvergingPlanes(DevSetConvergingPlanes command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        var ids = command.PlaneIds;
        if (ids is null || ids.Count == 0 || ids.Distinct().Count() != ids.Count || ids.Any(id => Content.Planes.All(p => p.Id != id)))
        {
            return CommandResult.Fail(CommandFailure.InvalidValue);
        }

        foreach (var id in ids.Where(id => !_run.UnlockedPlanes.Contains(id)))
        {
            _run.UnlockedPlanes.Add(id);
        }

        var ordered = Content.Planes.Where(p => ids.Contains(p.Id)).ToList();
        _run.ConvergingPlanes.Clear();
        _run.ConvergingPlanes.AddRange(ordered);
        _run.PreviousPlaneSet = ordered.Select(p => p.Id).ToList();
        Raise(new ConvergingPlanesDrawn(Time, Cycle, _run.PreviousPlaneSet));
        return CommandResult.Ok;
    }

    private CommandResult HandleBuySupplies(BuySupplies command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        if (command.Bundles < 1 || !Content.Balance.Supplies.TryGetValue(command.Supply, out var bundle))
        {
            return CommandResult.Fail(CommandFailure.InvalidValue);
        }

        var price = bundle.Price * command.Bundles;
        if (_run.Silver < price)
        {
            return CommandResult.Fail(CommandFailure.NotEnoughSilver);
        }

        _run.Silver -= price;
        _run.Supplies[command.Supply] += bundle.Amount * command.Bundles;
        Raise(new SuppliesChanged(Time, command.Supply, _run.Supplies[command.Supply]));
        Raise(new SilverChanged(Time, _run.Silver));
        return CommandResult.Ok;
    }

    private int Benched => _run.Units.Count(u => !u.Def.IsInnkeeper && u.Tile is null);

    private StaffOfferView ViewOf(StaffOffer offer)
    {
        var character = Content.Character(offer.CharacterId);
        var owned = _run.Units.FirstOrDefault(u => u.Def.Id == offer.CharacterId);
        var techniques = new List<string> { character.BaseTechnique };
        if (offer.Rank > 1)
        {
            foreach (var optionId in character.Presets.First(p => p.Rank == offer.Rank).Choices)
            {
                var option = character.Tree.SelectMany(r => r.Options).First(o => o.Id == optionId);
                if (!option.Mastery)
                {
                    techniques.Add(option.Technique);
                }
            }
        }

        var canBuy = _run.Silver >= offer.Price
                     && (owned is null ? Benched < Content.Balance.BenchSize : owned.Rank < UpgradeTrees.MaxRank);
        return new StaffOfferView(
            offer,
            character.Name,
            owned is not null,
            canBuy,
            techniques.Select(t => Content.Technique(t).Class).Distinct().Order().ToList(),
            techniques);
    }

    private ItemOfferView ViewOf(ItemOffer offer)
    {
        var item = Content.Item(offer.ItemId);
        return new ItemOfferView(offer, item.Name, item.Kind, item.Rarity, item.DisplaySpot);
    }

    private void Spend(int silver)
    {
        _run.Silver -= silver;
        Raise(new SilverChanged(Time, _run.Silver));
    }

    /// <summary>Buys a shop offer: a hire (STF-10), a one-rank upgrade (STF-08) or an item (ITM-02, ITM-04).</summary>
    private CommandResult HandleBuyOffer(BuyOffer command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        return command.Section == ShopSection.Staff ? BuyStaff(command.Index) : BuyItem(command.Index);
    }

    private CommandResult BuyStaff(int index)
    {
        if (index < 0 || index >= _run.Shop.Staff.Count || _run.Shop.Staff[index] is not { } offer)
        {
            return CommandResult.Fail(CommandFailure.UnknownOffer);
        }

        var owned = _run.Units.FirstOrDefault(u => u.Def.Id == offer.CharacterId);
        if (owned is not null && owned.Rank >= UpgradeTrees.MaxRank)
        {
            return CommandResult.Fail(CommandFailure.MaxRank);
        }

        if (owned is null && Benched >= Content.Balance.BenchSize)
        {
            return CommandResult.Fail(CommandFailure.BenchFull);
        }

        if (_run.Silver < offer.Price)
        {
            return CommandResult.Fail(CommandFailure.NotEnoughSilver);
        }

        Spend(offer.Price);
        _run.Shop.Staff[index] = null;
        if (owned is not null)
        {
            // Exactly one rank, whatever rank the offer shows, at the offer's price (STF-08, SHP-08).
            owned.Rank++;
            owned.RankMultiplier = Content.Balance.RankStatMultipliers[owned.Rank - 1];
            owned.Spent += offer.Price;
            owned.UpgradePending = true;
            Raise(new UpgradeOffered(Time, owned.Id, owned.Rank));
        }
        else
        {
            var unit = RunState.HireUnit(Content, Ids, offer.CharacterId, offer.Rank);
            unit.Spent = offer.Price;
            _run.Units.Add(unit);
            Raise(new UnitJoined(Time, unit.Id));
            Raise(new StaffHired(Time, unit.Id, offer.CharacterId, offer.Rank, offer.Price));
        }

        Raise(new ShopChanged(Time));
        return CommandResult.Ok;
    }

    private CommandResult BuyItem(int index)
    {
        if (index < 0 || index >= _run.Shop.Items.Count || _run.Shop.Items[index] is not { } offer)
        {
            return CommandResult.Fail(CommandFailure.UnknownOffer);
        }

        var def = Content.Item(offer.ItemId);
        if (def.Kind == ItemKind.Staff && _run.Storage.Count() >= Content.Balance.ItemStorage)
        {
            return CommandResult.Fail(CommandFailure.StorageFull);
        }

        if (def.Kind == ItemKind.Inn && _run.InnItems.Count() >= Content.Balance.MaxInnItemsShown)
        {
            return CommandResult.Fail(CommandFailure.InnItemsFull);
        }

        if (_run.Silver < offer.Price)
        {
            return CommandResult.Fail(CommandFailure.NotEnoughSilver);
        }

        Spend(offer.Price);
        _run.Shop.Items[index] = null;
        var item = new OwnedItem(Ids.NextItem(), def, offer.Price);
        _run.Items.Add(item);
        if (item.IsInnItem)
        {
            Raise(new InnItemAdded(Time, item.Id, def.Id, def.DisplaySpot!));
            SyncRooms();
        }

        Raise(new ItemsChanged(Time));
        Raise(new ShopChanged(Time));
        return CommandResult.Ok;
    }

    /// <summary>Opens or locks back rooms to match <c>RoomSlots</c> effects (ITM-06, D-078).</summary>
    private void SyncRooms()
    {
        var before = _run.Rooms.Count;
        _run.SyncRooms(_modifiers.InnWide(EffectKind.RoomSlots));
        if (_run.Rooms.Count != before)
        {
            Raise(new RoomsChanged(Time, _run.Rooms.Select(r => r.Zone).ToList()));
        }
    }

    /// <summary>Redraws every offer (SHP-05, D-080).</summary>
    private CommandResult HandleReroll()
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        var cost = _shop.RerollCost;
        if (_run.Silver < cost)
        {
            return CommandResult.Fail(CommandFailure.NotEnoughSilver);
        }

        Spend(cost);
        _run.Shop.RerollsThisCycle++;
        _shop.DrawAll();
        Raise(new ShopChanged(Time));
        return CommandResult.Ok;
    }

    private CommandResult HandleLockShop(LockShop command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        _run.Shop.Locked = command.Locked;
        Raise(new ShopChanged(Time));
        return CommandResult.Ok;
    }

    /// <summary>The pick for an upgraded unit's new rank (STF-07, STF-08, STF-17).</summary>
    private CommandResult HandleChooseUpgrade(ChooseUpgrade command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        if (_run.FindUnit(command.Unit) is not { } unit)
        {
            return CommandResult.Fail(CommandFailure.UnknownUnit);
        }

        if (!unit.UpgradePending)
        {
            return CommandResult.Fail(CommandFailure.InvalidValue);
        }

        var character = Content.Character(unit.Def.Id);
        var option = UpgradeTrees.OptionsAt(character, unit.Rank).FirstOrDefault(o => o.Id == command.OptionId);
        if (option is null || !UpgradeTrees.IsAvailable(option, RunState.BuildOf(unit)))
        {
            return CommandResult.Fail(CommandFailure.OptionNotAvailable);
        }

        RunState.ApplyOption(Content, unit, character, option.Id);
        unit.UpgradePending = false;
        Raise(new UnitUpgraded(Time, unit.Id, unit.Rank, option.Id));
        return CommandResult.Ok;
    }

    /// <summary>Half the silver spent on it comes back; its items go to storage (STF-09).</summary>
    private CommandResult HandleSellUnit(SellUnit command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        if (_run.FindUnit(command.Unit) is not { } unit)
        {
            return CommandResult.Fail(CommandFailure.UnknownUnit);
        }

        if (unit.Def.IsInnkeeper)
        {
            return CommandResult.Fail(CommandFailure.NotStaff);
        }

        var carried = _run.CarriedBy(unit.Id);
        if (_run.Storage.Count() + carried.Count > Content.Balance.ItemStorage)
        {
            return CommandResult.Fail(CommandFailure.StorageFull);
        }

        foreach (var item in carried)
        {
            item.Carrier = null;
        }

        if (unit.Tile is { } tile)
        {
            _run.Board.RemoveUnit(tile);
        }

        _run.Units.Remove(unit);
        var refund = unit.Spent / 2;
        _run.Silver += refund;
        Raise(new StaffSold(Time, unit.Id, refund));
        Raise(new SilverChanged(Time, _run.Silver));
        if (carried.Count > 0)
        {
            Raise(new ItemsChanged(Time));
        }

        return CommandResult.Ok;
    }

    private CommandResult HandleEquipItem(EquipItem command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        if (_run.Items.FirstOrDefault(i => i.Id == command.Item) is not { IsInnItem: false } item)
        {
            return CommandResult.Fail(CommandFailure.UnknownItem);
        }

        if (_run.FindUnit(command.Unit) is not { } unit)
        {
            return CommandResult.Fail(CommandFailure.UnknownUnit);
        }

        if (unit.Def.IsInnkeeper)
        {
            return CommandResult.Fail(CommandFailure.NotStaff);
        }

        if (item.Carrier == unit.Id)
        {
            return CommandResult.Ok;
        }

        if (_run.CarriedBy(unit.Id).Count >= Content.Balance.StaffItemSlots)
        {
            return CommandResult.Fail(CommandFailure.SlotsFull);
        }

        item.Carrier = unit.Id;
        Raise(new ItemsChanged(Time));
        return CommandResult.Ok;
    }

    private CommandResult HandleUnequipItem(UnequipItem command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        if (_run.Items.FirstOrDefault(i => i.Id == command.Item) is not { IsInnItem: false } item)
        {
            return CommandResult.Fail(CommandFailure.UnknownItem);
        }

        if (item.Carrier is null)
        {
            return CommandResult.Ok;
        }

        if (_run.Storage.Count() >= Content.Balance.ItemStorage)
        {
            return CommandResult.Fail(CommandFailure.StorageFull);
        }

        item.Carrier = null;
        Raise(new ItemsChanged(Time));
        return CommandResult.Ok;
    }

    /// <summary>Half the price comes back (ITM-07). An inn item that opened a room cannot be sold while the room is in use.</summary>
    private CommandResult HandleSellItem(SellItem command)
    {
        if (Phase != Phase.Dusk)
        {
            return CommandResult.Fail(CommandFailure.WrongPhase);
        }

        if (_run.Items.FirstOrDefault(i => i.Id == command.Item) is not { } item)
        {
            return CommandResult.Fail(CommandFailure.UnknownItem);
        }

        var lostRooms = (int)(item.Def.Effects.Where(e => e.Kind == EffectKind.RoomSlots).Sum(e => e.Amount ?? 0));
        if (item.IsInnItem && lostRooms > 0)
        {
            var keep = Math.Max(0, _run.Rooms.Count - lostRooms);
            var closing = _run.Rooms.Skip(keep).Select(r => r.Zone).ToHashSet();
            var occupied = Layout.AllTiles().Any(t => closing.Contains(Layout.ZoneAt(t))
                                                      && (_run.Board.UnitAt(t) is not null || _run.Board.FixtureAt(t) is not null));
            if (occupied)
            {
                return CommandResult.Fail(CommandFailure.TileOccupied);
            }
        }

        _run.Items.Remove(item);
        var refund = item.Paid / 2;
        _run.Silver += refund;
        if (item.IsInnItem)
        {
            Raise(new InnItemRemoved(Time, item.Id));
            SyncRooms();
        }

        Raise(new SilverChanged(Time, _run.Silver));
        Raise(new ItemsChanged(Time));
        return CommandResult.Ok;
    }
}

/// <summary>Where an upgrade option stands for a unit (UI-06, UI-08).</summary>
public enum UpgradeOptionState
{
    Chosen,
    Available,
    Locked,
}

/// <summary>One option of a unit's upgrade tree, for its panel and the upgrade dialog (UI-06, UI-08).</summary>
public sealed record UpgradeOptionView(
    int Rank,
    string OptionId,
    string TechniqueId,
    bool Mastery,
    StaffClass Class,
    UpgradeOptionState State);

/// <summary>One converging plane in the Dusk forecast (BST-03).</summary>
public sealed record ForecastPlane(PlaneDef Plane, IReadOnlyList<ForecastCreature> Creatures);

/// <summary>
/// A creature a plane may send, with what the bestiary knows (BST-03). <paramref name="Share"/> is its part of the
/// plane's weights. Unknown details are null and shown as "?".
/// </summary>
public sealed record ForecastCreature(
    string CreatureDefId,
    BestiaryLevel Level,
    double Share,
    string? Name,
    int? Size,
    IReadOnlyList<AppetiteDef>? Appetites,
    IReadOnlyList<TriggerDef>? Triggers);
