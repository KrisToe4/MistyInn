using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MistyInn.Board;
using MistyInn.Camera;
using MistyInn.Core;
using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Randomness;
using MistyInn.Core.Run;
using MistyInn.UI;
using MistyInn.Units;

namespace MistyInn;

/// <summary>
/// Owns the only <see cref="GameSession"/> (ARC-02). Each frame it ticks the session (SIM-02), drains its events into
/// the views (ARC-12) and places the views from Core's positions (SIM-03). Player input becomes commands (ARC-10):
/// dragging staff from the bench or the board onto tiles at Dusk (CAM-13), selection in the mist (CAM-12), speed and
/// the pause menu (SCP-13, CYC-10).
/// </summary>
public partial class GameRoot : Node3D
{
    /// <summary>The run's seed; 0 picks a random one (RUN-01).</summary>
    [Export] public long Seed { get; set; }

    [Export] public InnView Inn { get; set; } = null!;

    [Export] public Node3D UnitsRoot { get; set; } = null!;

    [Export] public Node3D CreaturesRoot { get; set; } = null!;

    [Export] public IsoCameraRig CameraRig { get; set; } = null!;

    [Export] public Hud Hud { get; set; } = null!;

    [Export] public PackedScene StaffScene { get; set; } = null!;

    [Export] public PackedScene InnkeeperScene { get; set; } = null!;

    [Export] public PackedScene CreatureScene { get; set; } = null!;

    private readonly Dictionary<UnitId, UnitView> _units = new();
    private readonly Dictionary<CreatureId, CreatureView> _creatures = new();
    private GameSession _session = null!;
    private UnitId? _dragging;
    private Node3D? _selected;
    private bool _paused;
    private long _droppedEvents;

    public override void _EnterTree() => InputActions.EnsureDefaults();

    public override void _Ready()
    {
        var seed = Seed != 0 ? (ulong)Seed : (ulong)System.Random.Shared.NextInt64(1, long.MaxValue);
        _session = new GameSession(new SeededRandom(seed));

        Inn.Build(_session.Content);
        Inn.ShowGates(_session.ConvergingPlanes.Select(p => p.Id));
        var (min, max) = Inn.Bounds;
        CameraRig.SetBounds(min, max);

        foreach (var unit in _session.Units)
        {
            var view = (unit.IsInnkeeper ? InnkeeperScene : StaffScene).Instantiate<UnitView>();
            UnitsRoot.AddChild(view);
            view.Setup(unit.Id, unit.Name);
            ShowUnit(view, unit.Tile);
            _units[unit.Id] = view;
        }

        Hud.SetSeed(seed);
        Hud.BeginMistPressed += OnBeginMist;
        Hud.ContinuePressed += OnContinue;
        Hud.SpeedPressed += OnSpeed;
        Hud.BenchDragStarted += OnBenchDragStarted;
        Hud.ResumePressed += OnResume;
        Hud.QuitPressed += OnQuit;
        Hud.Refresh(_session);
        Hud.ShowMessage(Tr("Drag the innkeeper and your staff onto the inn floor, then begin the mist."), 8);
    }

    public override void _ExitTree()
    {
        Hud.BeginMistPressed -= OnBeginMist;
        Hud.ContinuePressed -= OnContinue;
        Hud.SpeedPressed -= OnSpeed;
        Hud.BenchDragStarted -= OnBenchDragStarted;
        Hud.ResumePressed -= OnResume;
        Hud.QuitPressed -= OnQuit;
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
                view.Position = Inn.PositionToWorld(creature.PreviousPosition.Lerp(creature.Position, alpha));
            }
        }

        UpdateDragCursor();
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
            var unit = PickTile(click.Position) is { } tile ? UnitAtTile(tile) : null;
            if (unit is null)
            {
                return;
            }

            if (click.ButtonIndex == MouseButton.Left)
            {
                StartDrag(unit.Id, unit.Name);
            }
            else if (click.ButtonIndex == MouseButton.Right)
            {
                Report(_session.Execute(new BenchUnit(unit.Id)));
            }
        }
        else if (click.ButtonIndex == MouseButton.Left)
        {
            Select(click.Position);
        }
    }

    public override void _Input(InputEvent @event)
    {
        // The drop is read here, before the GUI, so a drag that started on a bench button still ends on the board.
        if (_dragging is not { } unitId || @event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release)
        {
            return;
        }

        _dragging = null;
        Hud.SetDragLabel(null);
        Inn.HideCursor();

        if (Hud.IsOverBench(release.Position))
        {
            Report(_session.Execute(new BenchUnit(unitId)));
        }
        else if (PickTile(release.Position) is { } tile)
        {
            Report(_session.Execute(new PlaceUnit(unitId, tile)));
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
            case ConvergingPlanesDrawn drawn:
                Inn.ShowGates(drawn.PlaneIds);
                break;
            case PhaseChanged { Phase: Phase.Dawn }:
                Hud.ShowMessage(Tr("Dawn breaks. The mist pulls back, and the creatures with it."), 6);
                break;
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

    private void StartDrag(UnitId unitId, string name)
    {
        _dragging = unitId;
        Hud.SetDragLabel(name);
    }

    private void UpdateDragCursor()
    {
        if (_dragging is not { } unitId)
        {
            return;
        }

        var mouse = GetViewport().GetMousePosition();
        if (!Hud.IsOverBench(mouse) && PickTile(mouse) is { } tile)
        {
            Inn.ShowCursor(tile, _session.CheckPlaceUnit(unitId, tile) == CommandFailure.None);
        }
        else
        {
            Inn.HideCursor();
        }
    }

    private void OnBeginMist()
    {
        if (Report(_session.Execute(new BeginMist())))
        {
            ClearSelection();
        }
    }

    private void OnContinue() => Report(_session.Execute(new ContinueFromDawn()));

    private void OnSpeed(int speed) => Report(_session.Execute(new SetSpeed(speed)));

    private void OnResume() => SetPaused(false);

    private void OnQuit() => GetTree().Quit();

    private void SetPaused(bool paused)
    {
        _paused = paused;
        _dragging = null;
        Hud.SetDragLabel(null);
        Inn.HideCursor();
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
        Hud.SetSelection(nearest switch
        {
            CreatureView creature => $"{Tr("Selected")}: {creature.DisplayName}  ({Tr("F to follow")})",
            UnitView unit => $"{Tr("Selected")}: {unit.DisplayName}  ({Tr("F to follow")})",
            _ => null,
        });
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
        CommandFailure.TileBlocked => Tr("Staff can only stand on the inn's floor."),
        CommandFailure.TileOccupied => Tr("Someone is already standing there."),
        CommandFailure.BoardFull => $"{Tr("No more staff fit on the floor this cycle")} ({_session.StaffOnBoard}/{_session.MaxStaffOnBoard}).",
        CommandFailure.CannotBench => Tr("The innkeeper never leaves the floor."),
        CommandFailure.InnkeeperNotPlaced => Tr("Place the innkeeper before the mist rolls in."),
        CommandFailure.WrongPhase => Tr("Not now."),
        _ => $"{Tr("Refused")}: {failure}",
    };

    private UnitSnapshot? UnitAtTile(Tile tile) => _session.Units.FirstOrDefault(u => u.Tile == tile);

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
