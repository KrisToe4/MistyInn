using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MistyInn.Board;
using MistyInn.Camera;
using MistyInn.Core;
using MistyInn.Core.Bestiary;
using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Randomness;
using MistyInn.Core.Run;
using MistyInn.Fixtures;
using MistyInn.Profile;
using MistyInn.UI;
using MistyInn.Units;

namespace MistyInn;

/// <summary>
/// Owns the only <see cref="GameSession"/> (ARC-02). Each frame it ticks the session (SIM-02), drains its events into
/// the views (ARC-12) and places the views from Core's positions (SIM-03). Player input becomes commands (ARC-10):
/// dragging staff and fixtures onto tiles at Dusk (CAM-13), the build menu (FIX-05), selection in the mist (CAM-12),
/// speed and the pause menu (SCP-13, CYC-10), integrity repairs at Dawn (BRD-07) and the debug dev panel (D-074).
/// Hits flash, and fallen units leave a blood mark unless the Blood filter is on (RAT-05, RAT-09). The run starts from
/// the title screen's request with the shared profile (RUN-01, MET-03), and ends on the run end panel, which returns to
/// the title screen (RUN-04).
/// </summary>
public partial class GameRoot : Node3D
{
    /// <summary>The run's seed when the title screen gives none; 0 picks a random one (RUN-01).</summary>
    [Export] public long Seed { get; set; }

    [Export] public InnView Inn { get; set; } = null!;

    [Export] public Node3D UnitsRoot { get; set; } = null!;

    [Export] public Node3D CreaturesRoot { get; set; } = null!;

    [Export] public Node3D FixturesRoot { get; set; } = null!;

    [Export] public IsoCameraRig CameraRig { get; set; } = null!;

    [Export] public Hud Hud { get; set; } = null!;

    [Export] public PackedScene StaffScene { get; set; } = null!;

    [Export] public PackedScene InnkeeperScene { get; set; } = null!;

    [Export] public PackedScene CreatureScene { get; set; } = null!;

    [Export] public PackedScene FixtureScene { get; set; } = null!;

    private readonly Dictionary<UnitId, UnitView> _units = new();
    private readonly Dictionary<CreatureId, CreatureView> _creatures = new();
    private readonly Dictionary<FixtureId, FixtureView> _fixtures = new();
    private readonly List<Node3D> _bloodMarks = new();
    private readonly List<string> _fellTonight = new();
    private readonly List<string> _diedTonight = new();

    private GameSession _session = null!;
    private UnitId? _dragging;
    private Vector2? _pendingSelect;
    private FixtureId? _draggingFixture;
    private int _rotation;
    private string _dragName = "";
    private Node3D? _selected;
    private bool _paused;
    private long _droppedEvents;
    private int _tributesTonight;

    public override void _EnterTree() => InputActions.EnsureDefaults();

    public override void _Ready()
    {
        if (PlayerProfile.TryContent(out var error) is not { } content)
        {
            // Debug builds stop with a clear message (DAT-04).
            Hud.ShowFatal($"{Tr("The game data in res://data/ is invalid")}:\n\n{error?.Message}");
            SetProcess(false);
            SetProcessInput(false);
            SetProcessUnhandledInput(false);
            return;
        }

        // The profile outlives the run and the scene (BST-04, STF-16, MET-01, D-091); step 8 saves it.
        var request = PlayerProfile.TakePendingRun();
        var seed = request?.Seed ?? (Seed != 0 ? (ulong)Seed : (ulong)System.Random.Shared.NextInt64(1, long.MaxValue));
        _session = new GameSession(
            new SeededRandom(seed),
            content: content,
            bestiary: PlayerProfile.Bestiary,
            ancestry: PlayerProfile.Ancestry,
            profile: PlayerProfile.Meta,
            startingStaff: request?.StartingStaff);

        Inn.Build(_session.Content);
        Inn.ShowGates(_session.ConvergingPlanes.Select(p => p.Id));
        Inn.ShowLockedRooms(_session.LockedRooms);
        var (min, max) = Inn.Bounds;
        CameraRig.SetBounds(min, max);

        foreach (var unit in _session.Units)
        {
            AddUnitView(unit);
        }

        RefreshFixtures();

        Hud.SetSeed(seed);
        Hud.BeginMistPressed += OnBeginMist;
        Hud.ContinuePressed += OnContinue;
        Hud.SpeedPressed += OnSpeed;
        Hud.BenchDragStarted += OnBenchDragStarted;
        Hud.StorageDragStarted += OnStorageDragStarted;
        Hud.SellPressed += OnSell;
        Hud.BuildPressed += OnBuild;
        Hud.BuySupplyPressed += OnBuySupply;
        Hud.ResumePressed += OnResume;
        Hud.QuitPressed += OnQuit;
        Hud.RepairPressed += OnRepair;
        Hud.TitlePressed += OnTitle;
        Hud.DevAddStaffPressed += OnDevAddStaff;
        Hud.DevPlanePressed += OnDevPlane;
        Hud.FiltersChanged += OnFiltersChanged;
        Hud.BuyOfferPressed += OnBuyOffer;
        Hud.RerollPressed += OnReroll;
        Hud.LockToggled += OnLock;
        Hud.ChooseUpgradePressed += OnChooseUpgrade;
        Hud.SellUnitPressed += OnSellUnit;
        Hud.EquipPressed += OnEquip;
        Hud.UnequipPressed += OnUnequip;
        Hud.SellItemPressed += OnSellItem;
        Hud.Refresh(_session);
        Hud.ShowMessage(Tr("Place the innkeeper, your staff and your fixtures, then begin the mist."), 8);
    }

    public override void _ExitTree()
    {
        Hud.BeginMistPressed -= OnBeginMist;
        Hud.ContinuePressed -= OnContinue;
        Hud.SpeedPressed -= OnSpeed;
        Hud.BenchDragStarted -= OnBenchDragStarted;
        Hud.StorageDragStarted -= OnStorageDragStarted;
        Hud.SellPressed -= OnSell;
        Hud.BuildPressed -= OnBuild;
        Hud.BuySupplyPressed -= OnBuySupply;
        Hud.ResumePressed -= OnResume;
        Hud.QuitPressed -= OnQuit;
        Hud.RepairPressed -= OnRepair;
        Hud.TitlePressed -= OnTitle;
        Hud.DevAddStaffPressed -= OnDevAddStaff;
        Hud.DevPlanePressed -= OnDevPlane;
        Hud.FiltersChanged -= OnFiltersChanged;
        Hud.BuyOfferPressed -= OnBuyOffer;
        Hud.RerollPressed -= OnReroll;
        Hud.LockToggled -= OnLock;
        Hud.ChooseUpgradePressed -= OnChooseUpgrade;
        Hud.SellUnitPressed -= OnSellUnit;
        Hud.EquipPressed -= OnEquip;
        Hud.UnequipPressed -= OnUnequip;
        Hud.SellItemPressed -= OnSellItem;
    }

    public override void _Process(double delta)
    {
        if (!_paused)
        {
            _session.Tick(TimeSpan.FromSeconds(delta));
        }

        foreach (var domainEvent in _session.DrainEvents())
        {
            Handle(domainEvent);
        }

        if (_session.DroppedEventCount != _droppedEvents)
        {
            GD.PushWarning($"Misty Inn: {_session.DroppedEventCount - _droppedEvents} domain events were dropped (D-039).");
            _droppedEvents = _session.DroppedEventCount;
        }

        var alpha = _session.InterpolationAlpha;
        foreach (var creature in _session.Creatures)
        {
            if (_creatures.TryGetValue(creature.Id, out var view))
            {
                view.Visible = creature.IsVisible;
                view.Position = Inn.PositionToWorld(creature.PreviousPosition.Lerp(creature.Position, alpha));
                view.SetStatus(creature);
            }
        }

        foreach (var unit in _session.Units)
        {
            if (_units.TryGetValue(unit.Id, out var view))
            {
                view.Visible = unit.IsVisible;
                if (unit.IsVisible)
                {
                    view.Position = Inn.PositionToWorld(unit.PreviousPosition.Lerp(unit.Position, alpha));
                }

                view.SetStatus(Activity(unit), unit.Health, unit.MaxHealth, unit.Activity == UnitActivity.Fallen);
            }
        }

        UpdateDragCursor();
        ShowSelection();
        Hud.Refresh(_session);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(InputActions.PauseMenu))
        {
            SetPaused(!_paused);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_paused)
        {
            return;
        }

        if (@event.IsActionPressed(InputActions.RotateFixture) && _draggingFixture is not null)
        {
            _rotation = (_rotation + 1) % 4;
            return;
        }

        if (@event.IsActionPressed(InputActions.Follow) && _selected is not null)
        {
            CameraRig.FollowTarget = _selected;
            return;
        }

        if (@event is not InputEventMouseButton { Pressed: true } click)
        {
            return;
        }

        if (_session.Phase == Phase.Dusk)
        {
            if (PickTile(click.Position) is not { } tile)
            {
                // Open ground outside the inn: a left drag pans the camera (CAM-10).
                if (click.ButtonIndex == MouseButton.Left)
                {
                    CameraRig.StartLeftPan(click.Position);
                }

                return;
            }

            if (UnitAtTile(tile) is { } unit)
            {
                if (click.ButtonIndex == MouseButton.Left)
                {
                    StartDrag(unit.Id, unit.Name);
                }
                else if (click.ButtonIndex == MouseButton.Right)
                {
                    Report(_session.Execute(new BenchUnit(unit.Id)));
                }
            }
            else if (FixtureAtTile(tile) is { } fixture)
            {
                if (click.ButtonIndex == MouseButton.Left)
                {
                    StartFixtureDrag(fixture.Id, fixture.Name, fixture.Rotation);
                }
                else if (click.ButtonIndex == MouseButton.Right)
                {
                    Report(_session.Execute(new StoreFixture(fixture.Id)));
                }
            }
            else if (click.ButtonIndex == MouseButton.Left)
            {
                CameraRig.StartLeftPan(click.Position);
            }
        }
        else if (click.ButtonIndex == MouseButton.Left)
        {
            // Selection happens on release, unless the press turned into a pan (CAM-10, CAM-12).
            _pendingSelect = click.Position;
            CameraRig.StartLeftPan(click.Position);
        }
    }

    public override void _Input(InputEvent @event)
    {
        // The drop is read here, before the GUI, so a drag that started on a panel button still ends on the board.
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release)
        {
            return;
        }

        var panned = CameraRig.EndLeftPan();
        if (_pendingSelect is { } pressedAt)
        {
            _pendingSelect = null;
            if (!panned && !_paused)
            {
                Select(pressedAt);
            }
        }

        if (_dragging is { } unitId)
        {
            EndDrag();
            if (Hud.IsOverBench(release.Position))
            {
                Report(_session.Execute(new BenchUnit(unitId)));
            }
            else if (PickTile(release.Position) is { } tile)
            {
                Report(_session.Execute(new PlaceUnit(unitId, tile)));
            }
        }
        else if (_draggingFixture is { } fixtureId)
        {
            var rotation = _rotation;
            EndDrag();
            if (Hud.IsOverBench(release.Position))
            {
                Report(_session.Execute(new StoreFixture(fixtureId)));
            }
            else if (PickTile(release.Position) is { } tile)
            {
                Report(_session.Execute(new PlaceFixture(fixtureId, tile, rotation)));
            }
        }
    }

    private void Handle(DomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case UnitPlaced placed when _units.TryGetValue(placed.Unit, out var view):
                ShowUnit(view, placed.Tile);
                break;
            case UnitBenched benched when _units.TryGetValue(benched.Unit, out var view):
                ShowUnit(view, null);
                break;
            case UnitJoined joined when _session.Units.FirstOrDefault(u => u.Id == joined.Unit) is { } unit:
                AddUnitView(unit);
                Hud.ShowMessage($"{unit.Name} {Tr("joins the bench.")}");
                break;
            case StaffSold sold when _units.Remove(sold.Unit, out var view):
                if (_selected == view)
                {
                    ClearSelection();
                }

                view.QueueFree();
                break;
            case StaffHired hired:
                Hud.ShowMessage($"{_session.Content.Character(hired.CharacterId).Name} {Tr("is hired at")} ★{hired.Rank}.");
                break;
            case UnitUpgraded upgraded when _units.TryGetValue(upgraded.Unit, out var view):
                Hud.ShowMessage($"{view.DisplayName} {Tr("is now")} ★{upgraded.Rank}.");
                break;
            case InnItemAdded or InnItemRemoved:
                Inn.ShowInnItems(_session.Items, _session.Content.DisplaySpots);
                break;
            case RoomsChanged:
                Inn.ShowLockedRooms(_session.LockedRooms);
                break;
            case AncestryRevealed revealed when _units.TryGetValue(revealed.Unit, out var view):
                Hud.ShowMessage($"{view.DisplayName}: {Tr("an old link to")} {_session.Content.Plane(revealed.PlaneId).Name} {Tr("comes to light.")}");
                break;
            case StaffDied died when _units.Remove(died.Unit, out var view):
                if (_selected == view)
                {
                    ClearSelection();
                }

                view.QueueFree();
                _diedTonight.Add(died.Name);
                break;
            case UnitFell fell when _units.TryGetValue(fell.Unit, out var fallen):
                BloodMarkAt(fallen.Position);
                _fellTonight.Add(fallen.DisplayName);
                Hud.ShowMessage($"{fallen.DisplayName} {Tr("has fallen!")}");
                break;
            case CreatureFell creatureFell when _creatures.TryGetValue(creatureFell.Creature, out var view):
                BloodMarkAt(view.Position);
                break;
            case AttackLanded hit:
                FlashHit(hit.Target);
                break;
            case CreatureBanished banished when _creatures.TryGetValue(banished.Creature, out var view):
                Hud.ShowMessage($"{view.DisplayName} {Tr("is banished into the mist.")}");
                break;
            case RoomSessionStarted room:
                ShowRoomStarted(room);
                break;
            case RoomSessionEnded { Interrupted: true }:
                Hud.ShowMessage(Tr("The door is attacked! Both come out at once."));
                break;
            case RunEnded ended:
                ShowRunEnd(ended);
                break;
            case OmenDrawn omen:
                Hud.ShowMessage($"{Tr("An omen")}: {_session.Content.Omen(omen.OmenId).Name}.", 8);
                break;
            case ContentFiltersChanged:
                ShowBloodMarks();
                break;
            case CreatureArrived arrived:
                SpawnCreature(arrived);
                break;
            case CreatureDissolved dissolved when _creatures.Remove(dissolved.Creature, out var view):
                if (_selected == view)
                {
                    ClearSelection();
                }

                view.QueueFree();
                break;
            case FixtureBuilt or FixturePlaced or FixtureStored or FixtureSold or FixtureBroken or FixtureRepaired:
                RefreshFixtures();
                break;
            case CreatureTurned turned when _creatures.TryGetValue(turned.Creature, out var view):
                Hud.ShowMessage($"{view.DisplayName} {Tr("has turned!")}");
                break;
            case CreatureSatisfied satisfied when _creatures.TryGetValue(satisfied.Creature, out var view):
                Hud.ShowMessage($"{view.DisplayName} {Tr("is satisfied, pays")} {satisfied.Silver} {Tr("silver and leaves.")}");
                break;
            case TributeLeft tribute:
                _tributesTonight += tribute.Silver;
                break;
            case BestiaryRevealed revealed when revealed.Level > BestiaryLevel.Seen:
                Hud.ShowMessage($"{Tr("The bestiary learns more about")} {_session.Content.Creature(revealed.CreatureDefId).Name}.");
                break;
            case ConvergingPlanesDrawn drawn:
                Inn.ShowGates(drawn.PlaneIds);
                break;
            case PhaseChanged { Phase: Phase.Mist }:
                _tributesTonight = 0;
                _fellTonight.Clear();
                _diedTonight.Clear();
                break;
            case PhaseChanged { Phase: Phase.Dawn }:
                ClearBloodMarks();
                Hud.ShowMessage(DawnSummary(), 10);
                break;
        }
    }

    private void RefreshFixtures()
    {
        var snapshots = _session.Fixtures;
        foreach (var gone in _fixtures.Keys.Where(id => snapshots.All(f => f.Id != id)).ToList())
        {
            _fixtures[gone].QueueFree();
            _fixtures.Remove(gone);
        }

        foreach (var fixture in snapshots)
        {
            if (!_fixtures.TryGetValue(fixture.Id, out var view))
            {
                view = FixtureScene.Instantiate<FixtureView>();
                FixturesRoot.AddChild(view);
                view.Setup(fixture, Inn.TileToWorld);
                _fixtures[fixture.Id] = view;
            }
            else
            {
                view.Refresh(fixture, Inn.TileToWorld);
            }
        }
    }

    private void SpawnCreature(CreatureArrived arrived)
    {
        var creature = _session.Creatures.FirstOrDefault(c => c.Id == arrived.Creature);
        if (creature is null)
        {
            return;
        }

        var view = CreatureScene.Instantiate<CreatureView>();
        CreaturesRoot.AddChild(view);
        view.Setup(arrived.Creature, creature.Name, new Color(_session.Content.Plane(arrived.PlaneId).MistColor));
        view.Position = Inn.PositionToWorld(creature.Position);
        _creatures[arrived.Creature] = view;
    }

    private void AddUnitView(UnitSnapshot unit)
    {
        var view = (unit.IsInnkeeper ? InnkeeperScene : StaffScene).Instantiate<UnitView>();
        UnitsRoot.AddChild(view);
        view.Setup(unit.Id, unit.Name);
        ShowUnit(view, unit.Tile);
        _units[unit.Id] = view;
    }

    /// <summary>What a unit's label says it is doing; Company shows as Hosting with the filter on (CMP-06).</summary>
    private string? Activity(UnitSnapshot unit) => unit.Activity switch
    {
        UnitActivity.Serving when unit.Serving is { } technique => $"{_session.TechniqueName(technique)} ×{unit.ServingCreatures.Count}",
        UnitActivity.Fighting => Tr("Fighting"),
        UnitActivity.Returning => Tr("Returning"),
        UnitActivity.GoingToRoom => _session.Filters.NoSuggestive ? Tr("To a private audience") : Tr("To a back room"),
        UnitActivity.Resting => Tr("Resting"),
        UnitActivity.Walking => Tr("Going to serve"),
        UnitActivity.Fallen => Tr("Fallen"),
        _ => unit.Shaken ? Tr("Shaken") : null,
    };

    private string DawnSummary()
    {
        var text = $"{Tr("Dawn breaks. The creatures that stayed calm left")} {_tributesTonight} {Tr("silver in tribute.")}";
        if (_diedTonight.Count > 0)
        {
            text += $"\n{Tr("Lost tonight")}: {string.Join(", ", _diedTonight)}.";
        }

        var wounded = _fellTonight.Except(_diedTonight).ToList();
        if (wounded.Count > 0)
        {
            text += $"\n{Tr("Wounded, and shaken for a night")}: {string.Join(", ", wounded)}.";
        }

        return text;
    }

    private void ShowRoomStarted(RoomSessionStarted room)
    {
        var companion = _units.TryGetValue(room.Companion, out var c) ? c.DisplayName : "";
        var guest = _creatures.TryGetValue(room.Guest, out var g) ? g.DisplayName : "";
        var text = _session.Filters.NoSuggestive
            ? $"{companion} {Tr("receives")} {guest} {Tr("for a private audience.")}"
            : $"{companion} {Tr("and")} {guest} {Tr("retire to a back room.")}";
        if (_session.CycleLogEntries.LastOrDefault(e => e.Kind == "company.room") is { } entry && _session.FlavourText(entry) is { } line)
        {
            text += $"\n{line}";
        }

        Hud.ShowMessage(text, 6);
    }

    private void FlashHit(EntityRef? target)
    {
        var color = _session.Filters.ReducedBlood ? Colors.White : new Color(1f, 0.25f, 0.2f);
        if (target is { Kind: EntityKind.Unit } u && _units.TryGetValue(new UnitId(u.Id), out var unit))
        {
            unit.Flash(color);
        }
        else if (target is { Kind: EntityKind.Creature } k && _creatures.TryGetValue(new CreatureId(k.Id), out var creature))
        {
            creature.Flash(color);
        }
    }

    /// <summary>A placeholder blood mark where someone fell, until dawn (RAT-05); hidden with the Blood filter on.</summary>
    private void BloodMarkAt(Vector3 position)
    {
        var mark = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.35f, BottomRadius = 0.35f, Height = 0.02f },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.35f, 0.02f, 0.03f) },
        };
        AddChild(mark);
        mark.Position = new Vector3(position.X, 0.06f, position.Z);
        _bloodMarks.Add(mark);
        ShowBloodMarks();
    }

    private void ShowBloodMarks()
    {
        foreach (var mark in _bloodMarks)
        {
            mark.Visible = !_session.Filters.ReducedBlood;
        }
    }

    private void ClearBloodMarks()
    {
        foreach (var mark in _bloodMarks)
        {
            mark.QueueFree();
        }

        _bloodMarks.Clear();
    }

    private void ShowUnit(UnitView view, Tile? tile)
    {
        view.Visible = tile is not null;
        if (tile is { } t)
        {
            view.Position = Inn.TileToWorld(t);
        }
    }

    private void OnBenchDragStarted(UnitId unitId)
    {
        var unit = _session.Units.FirstOrDefault(u => u.Id == unitId);
        if (unit is not null && _session.Phase == Phase.Dusk && !_paused)
        {
            StartDrag(unit.Id, unit.Name);
        }
    }

    private void OnStorageDragStarted(FixtureId fixtureId)
    {
        var fixture = _session.Fixtures.FirstOrDefault(f => f.Id == fixtureId);
        if (fixture is not null && _session.Phase == Phase.Dusk && !_paused)
        {
            StartFixtureDrag(fixture.Id, fixture.Name, 0);
        }
    }

    private void StartDrag(UnitId unitId, string name)
    {
        _dragging = unitId;
        _draggingFixture = null;
        _dragName = name;
        Hud.SetDragLabel(name);
    }

    private void StartFixtureDrag(FixtureId fixtureId, string name, int rotation)
    {
        _draggingFixture = fixtureId;
        _dragging = null;
        _rotation = rotation;
        _dragName = $"{name}  ({Tr("R to rotate")})";
        Hud.SetDragLabel(_dragName);
    }

    private void EndDrag()
    {
        _dragging = null;
        _draggingFixture = null;
        Hud.SetDragLabel(null);
        Inn.HideCursor();
    }

    private void UpdateDragCursor()
    {
        var mouse = GetViewport().GetMousePosition();
        var overBoard = !Hud.IsOverBench(mouse) && PickTile(mouse) is not null;

        // The tile under the mouse, so the demo instructions' (x,y) can be followed.
        var hovered = overBoard ? PickTile(mouse) : null;
        Hud.SetHoverTile(hovered is { } h ? $"{Tr("Tile")} ({h.X},{h.Y})  ·  {ZoneName(_session.Layout.ZoneAt(h))}" : null);
        if (_dragging is not null || _draggingFixture is not null)
        {
            Hud.SetDragLabel(hovered is { } at ? $"{_dragName}  →  ({at.X},{at.Y})" : _dragName);
        }

        if (_dragging is { } unitId && overBoard && PickTile(mouse) is { } tile)
        {
            Inn.ShowCursor(tile, _session.CheckPlaceUnit(unitId, tile) == CommandFailure.None);
        }
        else if (_draggingFixture is { } fixtureId && overBoard && PickTile(mouse) is { } origin)
        {
            var def = _session.Content.Fixture(_session.Fixtures.First(f => f.Id == fixtureId).FixtureDefId);
            var tiles = def.Shape.Select(o => Rotate(o[0], o[1], _rotation)).Select(o => new Tile(origin.X + o.X, origin.Y + o.Y)).ToList();
            Inn.ShowCursor(tiles, _session.CheckPlaceFixture(fixtureId, origin, _rotation) == CommandFailure.None);
        }
        else
        {
            Inn.HideCursor();
        }
    }

    private string ZoneName(string zone) => zone.Length == 0 ? Tr("wall") : Tr(zone.Replace('-', ' '));

    private static (int X, int Y) Rotate(int x, int y, int quarterTurns) => quarterTurns switch
    {
        1 => (-y, x),
        2 => (-x, -y),
        3 => (y, -x),
        _ => (x, y),
    };

    private void OnBeginMist()
    {
        if (Report(_session.Execute(new BeginMist())))
        {
            ClearSelection();
        }
    }

    private void OnContinue() => Report(_session.Execute(new ContinueFromDawn()));

    private void OnSpeed(int speed) => Report(_session.Execute(new SetSpeed(speed)));

    private void OnBuild(string fixtureDefId) => Report(_session.Execute(new BuildFixture(fixtureDefId)));

    private void OnBuySupply(Supply supply) => Report(_session.Execute(new BuySupplies(supply, 1)));

    private void OnSell(FixtureId fixtureId) => Report(_session.Execute(new SellFixture(fixtureId)));

    private void OnResume() => SetPaused(false);

    private void OnQuit() => GetTree().Quit();

    private void OnRepair(int silver) => Report(_session.Execute(new RepairIntegrity(Math.Max(1, Math.Min(silver, _session.Silver)))));

    /// <summary>Back to the title screen (RUN-04, CYC-10). A run left before it ends earns nothing (D-090).</summary>
    private void OnTitle() => GetTree().ChangeSceneToFile(TitleScreen.ScenePath);

    /// <summary>The run end panel (UI-10, placeholder): win or loss and its cause, cycles survived, Essence, bestiary.</summary>
    private void ShowRunEnd(RunEnded ended)
    {
        var summary = ended.Summary;
        var cause = ended.Reason switch
        {
            RunEndReason.Survived => Tr("Dawn breaks over the Convergence, and the inn still stands."),
            RunEndReason.InnkeeperFell => Tr("The innkeeper has fallen. The mist takes the inn."),
            _ => Tr("The inn is too broken to stand another night."),
        };

        var progress = BestiaryBook.Progress(_session.Content, _session.Bestiary);
        var text = string.Join("\n", new[]
        {
            cause,
            $"{Tr("Cycles survived")}: {summary.CyclesSurvived}/{_session.RunLength}",
            $"{Tr("Guests kept occupied")}: {summary.CreaturesOccupied}",
            $"{Tr("Mist Essence earned")}: {summary.EssenceEarned}  ({Tr("total")} {_session.Profile.Essence})",
            $"{Tr("Bestiary")}: {progress[BestiaryLevel.Seen]} {Tr("seen")}, {progress[BestiaryLevel.Studied]} {Tr("studied")}, "
            + $"{progress[BestiaryLevel.Understood]} {Tr("understood")} {Tr("of")} {_session.Content.Creatures.Count}",
        });
        Hud.ShowRunOver(summary.Won ? Tr("The inn survives") : Tr("The inn has fallen"), text);
    }

    private void OnDevAddStaff(string characterId) => Report(_session.Execute(new DevAddStaff(characterId)));

    private void OnDevPlane(string planeId) => Report(_session.Execute(new DevSetConvergingPlanes(new[] { planeId })));

    private void OnFiltersChanged(ContentFilters filters) => Report(_session.Execute(new SetContentFilters(filters)));

    private void OnBuyOffer(ShopSection section, int index) => Report(_session.Execute(new BuyOffer(section, index)));

    private void OnReroll() => Report(_session.Execute(new Reroll()));

    private void OnLock(bool locked) => Report(_session.Execute(new LockShop(locked)));

    private void OnChooseUpgrade(UnitId unit, string optionId) => Report(_session.Execute(new ChooseUpgrade(unit, optionId)));

    private void OnSellUnit(UnitId unit) => Report(_session.Execute(new SellUnit(unit)));

    private void OnEquip(ItemId item, UnitId unit) => Report(_session.Execute(new EquipItem(item, unit)));

    private void OnUnequip(ItemId item) => Report(_session.Execute(new UnequipItem(item)));

    private void OnSellItem(ItemId item) => Report(_session.Execute(new SellItem(item)));

    private void SetPaused(bool paused)
    {
        _paused = paused;
        EndDrag();
        Hud.ShowPauseMenu(paused);
    }

    private void Select(Vector2 screen)
    {
        if (PickGround(screen) is not { } hit)
        {
            return;
        }

        var nearest = _creatures.Values.Cast<Node3D>()
            .Concat(_units.Values.Where(u => u.Visible))
            .Select(node => (node, distance: new Vector2(node.Position.X - hit.X, node.Position.Z - hit.Z).Length()))
            .Where(x => x.distance < 0.8f)
            .OrderBy(x => x.distance)
            .Select(x => x.node)
            .FirstOrDefault();

        if (nearest is null)
        {
            ClearSelection();
            return;
        }

        _selected = nearest;
        ShowSelection();
    }

    /// <summary>The selection line (CAM-12, UI-06 until step 9); refreshed every frame so it follows the dev toggle.</summary>
    private void ShowSelection()
    {
        if (_selected is null)
        {
            return;
        }

        Hud.SetSelection(_selected switch
        {
            CreatureView creature => $"{Tr("Selected")}: {CreatureDetails(creature.Id) ?? creature.DisplayName}  ({Tr("F to follow")})",
            UnitView unit => $"{Tr("Selected")}: {UnitDetails(unit.Id) ?? unit.DisplayName}  ({Tr("F to follow")})",
            _ => null,
        });
    }

    /// <summary>
    /// A creature's appetites and triggers, as far as the bestiary has revealed them (UI-06, BST-02): appetites once
    /// Studied, triggers once Understood, "?" before. The debug-only dev toggle shows everything and changes nothing.
    /// </summary>
    private string? CreatureDetails(CreatureId id)
    {
        if (_session.Creatures.FirstOrDefault(c => c.Id == id) is not { } creature)
        {
            return null;
        }

        var def = _session.Content.Creature(creature.CreatureDefId);
        var level = _session.Bestiary.LevelOf(def, _session.Content.Balance.Bestiary);
        var all = Hud.ShowHiddenCreatureInfo;
        var appetites = all || level >= BestiaryLevel.Studied
            ? string.Join(", ", def.Appetites.Select(a =>
                $"{a.Appetite}{(a.Strength == AppetiteStrength.Likes ? " (likes)" : "")}{(a.PreferredTechnique is { } t ? $" [{_session.TechniqueName(t)}]" : "")}"))
            : "?";
        var triggers = all || level >= BestiaryLevel.Understood
            ? (def.Triggers.Count == 0 ? Tr("none") : string.Join(", ", def.Triggers.Select(t => t.Trigger)))
            : "?";
        return $"{creature.Name}  ·  {Tr("wants")} {appetites}  ·  {Tr("hates")} {triggers}  ·  {creature.State}";
    }

    /// <summary>
    /// A staff member's rank, classes, origin, ancestry ("?" until revealed), techniques and items (UI-06, placeholder).
    /// </summary>
    private string? UnitDetails(UnitId id)
    {
        if (_session.Units.FirstOrDefault(u => u.Id == id) is not { IsInnkeeper: false } unit)
        {
            return null;
        }

        var character = _session.Content.Character(unit.StaffId);
        var origin = character.Origin == ContentValidator.MortalRealm ? Tr("Mortal") : _session.Content.Plane(character.Origin).Name;
        var ancestry = unit.RevealedAncestry is { } plane ? _session.Content.Plane(plane).Name : "?";
        var techniques = string.Join(", ", unit.Techniques.Select(t => unit.Mastered.Contains(t) ? $"{_session.TechniqueName(t)}+" : _session.TechniqueName(t)));
        var items = unit.ItemDefIds.Count == 0 ? "" : $"  ·  {string.Join(", ", unit.ItemDefIds.Select(i => _session.Content.Item(i).Name))}";
        return $"{unit.Name} ★{unit.Rank}  ·  {string.Join("/", unit.Classes.Select(MistyInn.UI.ShopPanel.ClassName))}  ·  {origin}, {Tr("ancestry")} {ancestry}  ·  {techniques}{items}";
    }

    private void ClearSelection()
    {
        if (CameraRig.FollowTarget == _selected)
        {
            CameraRig.FollowTarget = null;
        }

        _selected = null;
        Hud.SetSelection(null);
    }

    private bool Report(CommandResult result)
    {
        if (!result.Succeeded)
        {
            Hud.ShowMessage(Describe(result.Failure));
        }

        return result.Succeeded;
    }

    private string Describe(CommandFailure failure) => failure switch
    {
        CommandFailure.TileBlocked => Tr("That spot is not free inn floor, or it would block the way in."),
        CommandFailure.TileOccupied => Tr("Something is already there."),
        CommandFailure.BoardFull => $"{Tr("No more staff fit on the floor this cycle")} ({_session.StaffOnBoard}/{_session.MaxStaffOnBoard}).",
        CommandFailure.CannotBench => Tr("The innkeeper never leaves the floor."),
        CommandFailure.InnkeeperNotPlaced => Tr("Place the innkeeper before the mist rolls in."),
        CommandFailure.NotEnoughSilver => Tr("Not enough silver."),
        CommandFailure.WrongZone => Tr("That fixture belongs in another room."),
        CommandFailure.WrongPhase => Tr("Not now."),
        CommandFailure.NothingToRepair => Tr("The inn needs no repairs."),
        CommandFailure.AlreadyOwned => Tr("They are already at the inn."),
        CommandFailure.BenchFull => Tr("The bench is full."),
        _ => $"{Tr("Refused")}: {failure}",
    };

    private UnitSnapshot? UnitAtTile(Tile tile) => _session.Units.FirstOrDefault(u => u.Tile == tile);

    private FixtureSnapshot? FixtureAtTile(Tile tile) => _session.Fixtures.FirstOrDefault(f => f.Tiles.Contains(tile));

    private Vector3? PickGround(Vector2 screen)
    {
        var camera = CameraRig.Camera;
        var from = camera.ProjectRayOrigin(screen);
        var direction = camera.ProjectRayNormal(screen);
        if (Mathf.Abs(direction.Y) < 1e-4f)
        {
            return null;
        }

        var distance = -from.Y / direction.Y;
        return distance < 0 ? null : from + direction * distance;
    }

    private Tile? PickTile(Vector2 screen)
    {
        if (PickGround(screen) is not { } hit)
        {
            return null;
        }

        var tile = Inn.WorldToTile(hit);
        return _session.Layout.Contains(tile) ? tile : null;
    }
}
