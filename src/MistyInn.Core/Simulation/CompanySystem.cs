using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Run;

namespace MistyInn.Core.Simulation;

/// <summary>
/// Company in the back rooms (CMP-04, CMP-05). After enough floor company, a guest that still craves it goes to a free
/// room with its companion. The session happens off-screen behind the closed door, soothes three times as much, and
/// the guest comes out calm; the companion then rests. Violence on the way, or an attack on the door, ends it at once
/// (CMP-05). The log records each step with neutral keys; the UI shows them as Company or Hosting (CMP-06).
/// </summary>
internal sealed class CompanySystem
{
    private readonly GameContent _content;
    private readonly RunState _run;
    private readonly IEventSink _sink;
    private readonly InnSense _sense;
    private readonly ServiceSystem _service;
    private readonly CreatureBehaviourSystem _behaviour;

    public CompanySystem(
        GameContent content, RunState run, IEventSink sink, InnSense sense, ServiceSystem service, CreatureBehaviourSystem behaviour)
    {
        _content = content;
        _run = run;
        _sink = sink;
        _sense = sense;
        _service = service;
        _behaviour = behaviour;
    }

    private CompanyBalanceDef Tuning => _content.Balance.Company;

    public void Step(double stepSeconds, double now)
    {
        TrackFloorCompany(now);

        foreach (var room in _run.Rooms.Where(r => r.IsTaken).ToList())
        {
            Update(room, stepSeconds, now);
        }

        foreach (var unit in _run.Units.Where(u => u.Activity == UnitActivity.Resting && now >= u.RestUntil))
        {
            unit.Activity = UnitActivity.Returning;
            unit.ClearPath();
        }

        StartRoomVisits(now);
    }

    /// <summary>An attack on an occupied room's door ends the session before the blow lands (CMP-05).</summary>
    public void Interrupt(Room room, double now)
    {
        if (room.InSession)
        {
            Leave(room, now, interrupted: true);
        }
    }

    /// <summary>Dawn: every room empties (CYC-03).</summary>
    public void EndAll()
    {
        foreach (var room in _run.Rooms)
        {
            if (room.InSession)
            {
                _run.Board.Unblock(room.Door);
            }

            room.Clear();
        }
    }

    /// <summary>Who has been giving each guest floor company, and since when (CMP-03, CMP-04).</summary>
    private void TrackFloorCompany(double now)
    {
        var givers = new Dictionary<CreatureId, Unit>();
        foreach (var unit in _run.AvailableStaff)
        {
            if (unit.Session is { } session && session.Technique.Technique == Technique.Company)
            {
                foreach (var id in session.Targets)
                {
                    givers[id] = unit;
                }
            }
        }

        foreach (var creature in _run.Creatures.Where(c => c.Activity is not (CreatureActivity.GoingToRoom or CreatureActivity.InRoom)))
        {
            if (givers.TryGetValue(creature.Id, out var unit))
            {
                if (creature.CompanyWith != unit.Id)
                {
                    creature.CompanyWith = unit.Id;
                    creature.CompanySince = now;
                }
            }
            else
            {
                creature.CompanyWith = null;
            }
        }
    }

    /// <summary>
    /// A guest with at least 8 s of floor company that still craves Company, and has not had a room this cycle, goes to
    /// the first free room with its companion (CMP-04, D-071).
    /// </summary>
    private void StartRoomVisits(double now)
    {
        foreach (var guest in _run.Creatures.Where(c => c.CompanyWith is not null).OrderBy(c => c.Id.Value).ToList())
        {
            if (now - guest.CompanySince < Tuning.RoomAfterSeconds || guest.HadRoom || guest.IsTurned
                || !guest.Def.Appetites.Any(a => a.Appetite == Appetite.Company && a.Strength == AppetiteStrength.Craves))
            {
                continue;
            }

            if (_run.FindUnit(guest.CompanyWith!.Value) is not { IsAvailable: true } companion)
            {
                continue;
            }

            var room = _run.Rooms.FirstOrDefault(r => !r.IsTaken && !r.IsBroken);
            if (room is null)
            {
                return;
            }

            var companionPath = _run.Board.FindPath(companion.Position.NearestTile, room.Door);
            var guestPath = _run.Board.FindPath(guest.Position.NearestTile, room.Door);
            if (companionPath is null || guestPath is null)
            {
                continue;
            }

            room.Companion = companion.Id;
            room.Guest = guest.Id;
            room.EndsAt = null;
            companion.Session = null;
            companion.Activity = UnitActivity.GoingToRoom;
            companion.SetPath(companionPath);
            _behaviour.Release(guest);
            guest.Activity = CreatureActivity.GoingToRoom;
            guest.SetPath(guestPath);
            _sink.Log("company.to-room", "company.floor", guest.Id.ToRef(), companion.Id.ToRef());
        }
    }

    private void Update(Room room, double stepSeconds, double now)
    {
        var companion = room.Companion is { } u ? _run.FindUnit(u) : null;
        var guest = room.Guest is { } g ? _run.FindCreature(g) : null;

        if (!room.InSession)
        {
            var cause = companion is null || companion.IsFallen ? "companion.fell"
                : guest is null ? "guest.gone"
                : guest.IsTurned ? "guest.turned"
                : _sense.ViolenceNear(guest.Position, Tuning.InterruptRadius) || _sense.ViolenceNear(companion.Position, Tuning.InterruptRadius) ? "violence"
                : null;
            if (cause is not null)
            {
                Cancel(room, companion, guest, cause, now);
                return;
            }

            companion!.Advance(companion.Stats.Speed * stepSeconds);
            guest!.Advance(guest.Def.Speed * stepSeconds);
            Soothe(companion, guest, 1);
            if (companion.Path.Count == 0 && guest.Path.Count == 0)
            {
                Enter(room, companion, guest, now);
            }

            return;
        }

        if (companion is null || guest is null || now >= room.EndsAt)
        {
            Leave(room, now, interrupted: false);
            return;
        }

        Soothe(companion, guest, Tuning.RoomSootheMultiplier);
    }

    private void Soothe(Unit companion, Creature guest, double multiplier)
    {
        var company = companion.Techniques.First(t => t.Technique == Technique.Company);
        _service.Offer(guest, _service.SootheRate(companion, company, guest) * multiplier, $"{companion.Def.Id}:{company.Id}");
    }

    /// <summary>The door closes: the session happens off-screen (CMP-04, RAT-02).</summary>
    private void Enter(Room room, Unit companion, Creature guest, double now)
    {
        room.EndsAt = now + Tuning.RoomSessionSeconds;
        _run.Board.Block(room.Door);
        companion.Activity = UnitActivity.InRoom;
        guest.Activity = CreatureActivity.InRoom;
        companion.Position = TilePosition.Center(room.Door);
        guest.Position = TilePosition.Center(room.Door);
        _sink.Raise(new RoomSessionStarted(_sink.Time, room.Zone, companion.Id, guest.Id));
        _sink.Log("company.room", "company.floor", guest.Id.ToRef(), companion.Id.ToRef(), flavour: "company.room");
    }

    /// <summary>
    /// Both come out at the door. After a full session the guest is calm; the companion rests, out of play (CMP-04).
    /// </summary>
    private void Leave(Room room, double now, bool interrupted)
    {
        var companion = room.Companion is { } u ? _run.FindUnit(u) : null;
        var guest = room.Guest is { } g ? _run.FindCreature(g) : null;
        var door = TilePosition.Center(room.Door);
        _run.Board.Unblock(room.Door);

        if (guest is not null)
        {
            guest.Position = door;
            guest.PreviousPosition = door;
            guest.ClearPath();
            guest.Activity = CreatureActivity.Idle;
            guest.IdleUntil = now;
            guest.HadRoom = true;
            guest.CompanyWith = null;
            if (!interrupted)
            {
                guest.Agitation = Math.Min(guest.Agitation, _content.Balance.Agitation.RestlessAt - 1);
            }
        }

        if (companion is not null)
        {
            companion.Position = door;
            companion.PreviousPosition = door;
            companion.ClearPath();
            companion.Activity = UnitActivity.Resting;
            companion.RestUntil = now + Tuning.RoomRestSeconds;
        }

        room.Clear();
        _sink.Raise(new RoomSessionEnded(_sink.Time, room.Zone, interrupted));
        _sink.Log(interrupted ? "company.interrupted" : "company.left", interrupted ? "door.attacked" : "company.ended",
            guest?.Id.ToRef(), companion?.Id.ToRef());
    }

    /// <summary>The walk to the room is called off (CMP-05): the companion goes back to its post.</summary>
    private void Cancel(Room room, Unit? companion, Creature? guest, string cause, double now)
    {
        room.Clear();
        if (companion is { IsFallen: false })
        {
            companion.Activity = UnitActivity.Returning;
            companion.ClearPath();
        }

        if (guest is { IsTurned: false })
        {
            guest.ClearPath();
            guest.Activity = CreatureActivity.Idle;
            guest.IdleUntil = now;
        }

        _sink.Log("company.interrupted", cause, guest?.Id.ToRef(), companion?.Id.ToRef());
    }
}
