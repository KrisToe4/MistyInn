using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Run;

namespace MistyInn.Core.Simulation;

/// <summary>
/// Damage to the inn (BRD-06, ESC-10): fixtures, room doors and walls. Integrity loses a share of what fixtures and
/// doors take, and what walls take directly (D-070). Every break is an event and a log entry (INV-12).
/// </summary>
internal sealed class InnDamage
{
    private readonly GameContent _content;
    private readonly RunState _run;
    private readonly IEventSink _sink;

    public InnDamage(GameContent content, RunState run, IEventSink sink)
    {
        _content = content;
        _run = run;
        _sink = sink;
    }

    private CombatBalanceDef Tuning => _content.Balance.Combat;

    /// <summary>Damages a working fixture; returns true if this broke it.</summary>
    public bool Fixture(Fixture fixture, double amount, Creature by, string cause)
    {
        if (fixture.IsBroken || amount <= 0)
        {
            return false;
        }

        var applied = Math.Min(amount, fixture.Health);
        fixture.Health -= applied;
        LoseIntegrity(applied * Tuning.IntegrityShare);
        if (!fixture.IsBroken)
        {
            return false;
        }

        fixture.Health = 0;
        by.BrokeSomething = true;
        _sink.Raise(new FixtureBroken(_sink.Time, fixture.Id, by.Id));
        _sink.Log("fixture.broken", cause, fixture.Id.ToRef(), by.Id.ToRef());
        return true;
    }

    /// <summary>Damages a back room's door (CMP-05); a broken door closes the room until Dawn (D-069).</summary>
    public void Door(Room room, double amount, Creature by)
    {
        if (room.IsBroken)
        {
            return;
        }

        var applied = Math.Min(amount, room.DoorHealth);
        room.DoorHealth -= applied;
        LoseIntegrity(applied * Tuning.IntegrityShare);
        if (room.IsBroken)
        {
            room.DoorHealth = 0;
            by.BrokeSomething = true;
            _sink.Log("door.broken", "combat.attack", source: by.Id.ToRef());
        }
    }

    /// <summary>A wall hit by a turned creature with no fixture in reach (BRD-06, D-070).</summary>
    public void Wall(Tile wall, double amount, Creature by)
    {
        LoseIntegrity(amount * Tuning.WallDamageMultiplier);
        _sink.Log("inn.damaged", "wall.attacked", source: by.Id.ToRef());
    }

    private void LoseIntegrity(double amount) => _run.Integrity = Math.Max(0, _run.Integrity - amount);
}
