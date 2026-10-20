using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Content;
using MistyInn.Core.Events;
using MistyInn.Core.Run;

namespace MistyInn.Core.Tests.Simulation;

/// <summary>Escalation and combat (section 10, TST-06).</summary>
public class EscalationTests
{
    // Creature 01: health 40, attack 5 every 1.6 s, armour 1, range 1. Creature 09 (Pale Court) skips Wreck: health 40,
    // attack 9 every 1.3 s. Creature 10 skips Wreck: attack 11 every 1.4 s. The lantern has 10 health.
    private static readonly Tile Common = new(12, 9);

    [Test]
    [Property("Requirement", "ESC-01")]
    [Property("Requirement", "ESC-10")]
    [Property("Requirement", "BRD-06")]
    [Property("Requirement", "INV-12")]
    public void ATurnedCreatureWrecksTheNearestFixtureAndThenPreys()
    {
        var s = new Scenario();
        var lantern = s.Place("lantern", new Tile(12, 7));
        s.Begin();
        var id = s.SpawnTurned("creature-01", Common);
        Assert.That(s.Creature(id).Stage, Is.EqualTo(EscalationStage.Wreck));

        s.RunUntilCollecting(() => s.Session.FixtureForTests(lantern).IsBroken, 10);
        s.Step();

        Assert.That(s.Session.Integrity, Is.EqualTo(s.Session.MaxIntegrity - 10 * 0.25).Within(1e-6), "a quarter of what the fixture took");
        Assert.That(s.Events.OfType<FixtureBroken>().Single().By, Is.EqualTo(id));
        Assert.That(s.Creature(id).Stage, Is.EqualTo(EscalationStage.Prey), "one fixture destroyed ends Wreck");
        var log = s.Session.CycleLogEntries;
        Assert.That(log.Any(e => e.Kind == "fixture.broken" && e.Cause == "combat.attack"), Is.True);
        Assert.That(log.Count(e => e.Kind == "escalation.stage"), Is.EqualTo(2));
        Assert.That(log.Last(e => e.Kind == "escalation.stage").Cause, Is.EqualTo("stage.prey"));
    }

    [Test]
    [Property("Requirement", "ESC-01")]
    [Property("Requirement", "BRD-06")]
    public void WithNothingToWreckItHitsTheWallsUntilWreckTimesOut()
    {
        var s = new Scenario();
        s.Begin();
        var id = s.SpawnTurned("creature-01", Common);

        s.Run(5);
        Assert.That(s.Creature(id).Stage, Is.EqualTo(EscalationStage.Wreck));
        Assert.That(s.Session.Integrity, Is.LessThan(s.Session.MaxIntegrity), "walls take integrity directly");
        Assert.That(s.Session.CycleLogEntries.Any(e => e.Kind == "inn.damaged" && e.Cause == "wall.attacked"), Is.True);

        s.Run(6);
        Assert.That(s.Creature(id).Stage, Is.EqualTo(EscalationStage.Prey), "Wreck lasts at most 10 s");
    }

    [Test]
    [Property("Requirement", "ESC-02")]
    public void AnEscalationProfileCanSkipWreck()
    {
        var s = new Scenario();
        s.Begin();
        var id = s.SpawnTurned("creature-09", Common);

        Assert.That(s.Creature(id).Stage, Is.EqualTo(EscalationStage.Prey));
        Assert.That(s.Session.CycleLogEntries.Single(e => e.Kind == "escalation.stage").Cause, Is.EqualTo("stage.prey"));
    }

    [Test]
    [Property("Requirement", "ESC-04")]
    [Property("Requirement", "ESC-01")]
    [Property("Requirement", "INV-12")]
    public void ACalmCreatureStruckDownDissolvesAndThePreyMovesOnToTheHunt()
    {
        // Creature 05: health 35, armour 0: four hits of 9.
        var s = new Scenario();
        s.Begin();
        var victim = s.Spawn("creature-05", new Tile(13, 9));
        var killer = s.SpawnTurned("creature-09", Common);

        s.RunUntilCollecting(() => s.IsGone(victim), 20);
        s.Step();

        var fell = s.Events.OfType<CreatureFell>().Single();
        Assert.That(fell.Creature, Is.EqualTo(victim));
        Assert.That(fell.By, Is.EqualTo(killer));
        Assert.That(s.Events.OfType<CreatureDissolved>().Select(d => d.Creature), Does.Contain(victim));
        Assert.That(s.Session.RunForTests.BloodMarks, Has.Count.EqualTo(1), "a fallen creature counts as Blood");
        Assert.That(s.Session.CycleLogEntries.Any(e => e.Kind == "creature.fell" && e.Subject == victim.ToRef()), Is.True);
        Assert.That(s.Creature(killer).Stage, Is.EqualTo(EscalationStage.Hunt), "one fall ends Prey");

        s.Session.RunMistToDawn();
        Assert.That(s.Session.DrainEvents().OfType<TributeLeft>(), Is.Empty, "neither leaves a tribute");
    }

    [Test]
    [Property("Requirement", "ESC-01")]
    [Property("Requirement", "ESC-06")]
    [Property("Requirement", "RUN-03")]
    [Property("Requirement", "TST-06")]
    public void TheHuntForTheInnkeeperEndsTheRunWhenItFalls()
    {
        // The real innkeeper: 40 health, armour 1. Creature 10 has nobody to prey on, so after 15 s it hunts.
        var s = new Scenario(sturdy: false);
        s.Begin();
        var id = s.SpawnTurned("creature-10", new Tile(12, 11));

        s.Run(14);
        Assert.That(s.Creature(id).Stage, Is.EqualTo(EscalationStage.Prey));
        s.RunUntilCollecting(() => s.Session.Phase == Phase.RunOver, 30);

        Assert.That(s.Session.EndReason, Is.EqualTo(RunEndReason.InnkeeperFell));
        Assert.That(s.Events.OfType<UnitFell>().Single().Unit, Is.EqualTo(TestGame.Innkeeper(s.Session).Id));
        Assert.That(s.Events.OfType<RunEnded>().Single().Reason, Is.EqualTo(RunEndReason.InnkeeperFell));
        Assert.That(s.Session.CycleLogEntries[^1], Has.Property("Kind").EqualTo("run.lost"));

        var time = s.Session.Time;
        s.Run(5);
        Assert.That(s.Session.Time, Is.EqualTo(time), "nothing more happens");
        Assert.That(s.Session.Execute(new ContinueFromDawn()).Failure, Is.EqualTo(CommandFailure.WrongPhase));
    }

    [TestCase(2, 8, TestName = "Attack 10 against armour 2 does 8")]
    [TestCase(50, 1, TestName = "Never less than 1")]
    [Property("Requirement", "ESC-05")]
    public void AHitDoesAttackMinusArmour(int armour, int damage)
    {
        // Staff 10: attack 6 at ★1, Weapons +4.
        var s = new Scenario(
            startingStaff: ["staff-10"],
            set: c => TestGame.WithCreature(c, "creature-02", d => d with { Combat = d.Combat with { Armour = armour } }));
        s.PlaceStaff("staff-10", new Tile(12, 8));
        s.Begin();
        s.SpawnTurned("creature-02", new Tile(13, 9));
        var fighter = s.Staff("staff-10").Id.ToRef();

        s.RunUntilCollecting(() => s.Events.OfType<AttackLanded>().Any(a => a.Source == fighter), 10);

        Assert.That(s.Events.OfType<AttackLanded>().First(a => a.Source == fighter).Damage, Is.EqualTo(damage));
    }

    [Test]
    [Property("Requirement", "ESC-07")]
    [Property("Requirement", "STF-04")]
    public void FightersDropServiceToEngageTheMostThreateningCreature()
    {
        // Staff 09 (Fists) spars with Creature 04, which craves Brawl, until two creatures turn close by.
        var s = new Scenario(startingStaff: ["staff-09"]);
        s.PlaceStaff("staff-09", new Tile(12, 8));
        s.Begin();
        s.Spawn("creature-04", new Tile(12, 9));
        s.RunUntil(() => s.Unit("staff-09").Session is not null, 10);

        var near = s.Spawn("creature-01", new Tile(13, 8));
        var far = s.Spawn("creature-01", new Tile(15, 9));
        s.Creature(near).Agitation = 99.99;
        s.Creature(far).Agitation = 99.99;
        s.Step();

        var fighter = s.Unit("staff-09");
        Assert.That(fighter.Session, Is.Null, "engaging comes before serving");
        Assert.That(fighter.Activity, Is.EqualTo(UnitActivity.Fighting));
        Assert.That(fighter.Engaging, Is.EqualTo(near), "same stage: the nearest");

        s.Creature(far).Stage = EscalationStage.Hunt;
        fighter.Engaging = null;
        s.Step();
        Assert.That(fighter.Engaging, Is.EqualTo(far), "Hunt before Prey before Wreck");
    }

    [Test]
    [Property("Requirement", "ESC-03")]
    public void ATurnedCreatureTurnsOnTheFighterThatHitsIt()
    {
        var s = new Scenario(startingStaff: ["staff-09"]);
        s.Place("lantern", new Tile(17, 10));
        s.PlaceStaff("staff-09", new Tile(12, 8));
        s.Begin();
        var id = s.SpawnTurned("creature-01", new Tile(13, 9));
        var fighter = s.Staff("staff-09").Id;

        s.RunUntilCollecting(() => s.Creature(id).EngagedBy == fighter, 10);
        s.RunUntilCollecting(() => s.Events.OfType<AttackLanded>().Any(a => a.Source == id.ToRef() && a.Target == fighter.ToRef()), 10);

        Assert.That(s.Session.FixtureForTests(s.Session.Fixtures.Single(f => f.FixtureDefId == "lantern").Id).IsBroken, Is.False,
            "it left the lantern for the fighter");
    }

    [Test]
    [Property("Requirement", "ESC-08")]
    [Property("Requirement", "BST-02")]
    [Property("Requirement", "INV-12")]
    public void ABanishedCreatureDissolvesAndTeachesTheBestiary()
    {
        var s = new Scenario(startingStaff: ["staff-10"]);
        s.PlaceStaff("staff-10", new Tile(12, 8));
        s.Begin();
        var id = s.SpawnTurned("creature-01", new Tile(13, 9));
        s.Creature(id).Health = 1;

        s.RunUntilCollecting(() => s.IsGone(id), 10);

        Assert.That(s.Events.OfType<CreatureBanished>().Single().By, Is.EqualTo(s.Staff("staff-10").Id));
        Assert.That(s.Events.OfType<CreatureDissolved>().Single().Creature, Is.EqualTo(id));
        var entry = s.Session.CycleLogEntries.Single(e => e.Kind == "creature.banished");
        Assert.That(entry.Flavour, Is.EqualTo("creature.banished"));
        Assert.That(s.Session.Bestiary.Entry(s.Content.Creature("creature-01"), s.Content.Balance.Bestiary).BanishPoints, Is.EqualTo(1));
    }

    [TestCase(1.0, true, TestName = "Wounded")]
    [TestCase(0.0, false, TestName = "Dead")]
    [Property("Requirement", "ESC-09")]
    [Property("Requirement", "STF-11")]
    [Property("Requirement", "STF-12")]
    [Property("Requirement", "RAT-05")]
    public void AFallenStaffMemberIsWoundedOrDeadAtDawn(double woundChance, bool survives)
    {
        // Staff 01: 18 health, armour 0: two hits of 9 from Creature 09, which preys on the nearest staff member.
        var s = new Scenario(balance: b => b with { Combat = b.Combat with { WoundChance = woundChance } });
        s.PlaceStaff("staff-01", new Tile(12, 8));
        s.Begin();
        s.SpawnTurned("creature-09", new Tile(13, 8));

        s.RunUntilCollecting(() => s.Unit("staff-01").IsFallen, 10);
        Assert.That(s.Events.OfType<UnitFell>().Single().Unit, Is.EqualTo(s.Staff("staff-01").Id));
        Assert.That(s.Session.CycleLogEntries.Single(e => e.Kind == "unit.fell").Flavour, Is.EqualTo("unit.fell"));

        s.Session.RunMistToDawn();
        var events = s.Session.DrainEvents();

        if (survives)
        {
            var unit = s.Unit("staff-01");
            Assert.That(events.OfType<StaffWounded>().Single().Unit, Is.EqualTo(unit.Id));
            Assert.That(unit.Health, Is.EqualTo(unit.MaxHealth));
            Assert.That(unit.Shaken, Is.True);
            Assert.That(unit.Activity, Is.EqualTo(UnitActivity.AtPost));
        }
        else
        {
            var died = events.OfType<StaffDied>().Single();
            Assert.That(died.Name, Is.EqualTo("Staff 01"));
            Assert.That(s.Session.Units.Any(u => u.StaffId == "staff-01"), Is.False);
            Assert.That(s.Session.Execute(new ContinueFromDawn()).Succeeded, Is.True);
            Assert.That(s.Session.CheckPlaceUnit(TestGame.Innkeeper(s.Session).Id, new Tile(12, 8)),
                Is.EqualTo(CommandFailure.None), "the post is free");
        }
    }

    [Test]
    [Property("Requirement", "STF-12")]
    public void ShakenStaffSootheLessForACycle()
    {
        var s = new Scenario();
        s.PlaceStaff("staff-01", new Tile(12, 5));
        s.Begin();
        var creature = s.Creature(s.Spawn("creature-01", new Tile(12, 6)));
        var unit = s.Unit("staff-01");
        var drink = s.Content.Technique("drink");

        var steady = s.Session.ServiceForTests.SootheRate(unit, drink, creature);
        unit.Shaken = true;
        var shaken = s.Session.ServiceForTests.SootheRate(unit, drink, creature);

        Assert.That(shaken, Is.EqualTo(steady * 0.8).Within(1e-9));

        s.Session.RunMistToDawn();
        Assert.That(unit.Shaken, Is.False, "it did not fall again");
    }

    [Test]
    [Property("Requirement", "STF-05")]
    public void BenchedStaffCannotBeHurt()
    {
        var s = new Scenario();
        s.Begin();
        s.SpawnTurned("creature-09", Common);

        s.Run(10);

        Assert.That(s.Session.Units.Where(u => !u.IsInnkeeper).All(u => u.IsOnBench && u.Health == u.MaxHealth), Is.True);
    }

    [Test]
    [Property("Requirement", "STF-02")]
    public void GrappleHoldsTheCreatureAndSlowsItsAttacks()
    {
        var s = new Scenario(startingStaff: ["staff-11"]);
        s.PlaceStaff("staff-11", new Tile(12, 8));
        s.Begin();
        var id = s.SpawnTurned("creature-01", new Tile(13, 8));
        var grappler = s.Staff("staff-11").Id;

        s.RunUntilCollecting(() => s.Creature(id).GrappledBy == grappler, 10);
        s.RunUntilCollecting(() => s.Events.OfType<AttackLanded>().Count(a => a.Source == id.ToRef()) >= 2, 15);

        var times = s.Events.OfType<AttackLanded>().Where(a => a.Source == id.ToRef()).Select(a => a.At.TotalSeconds).ToList();
        Assert.That(times[^1] - times[^2], Is.EqualTo(1.6 * 1.5).Within(0.05), "1.6 s between attacks, ×1.5 while held");
    }

    [Test]
    [Property("Requirement", "ESC-10")]
    [Property("Requirement", "BRD-04")]
    [Property("Requirement", "BRD-07")]
    public void ABrokenFixtureStaysUntilItIsRepairedForFreeAtDawn()
    {
        var s = new Scenario();
        var lantern = s.Place("lantern", new Tile(12, 7));
        s.Begin();
        s.SpawnTurned("creature-01", Common);
        s.RunUntil(() => s.Session.FixtureForTests(lantern).IsBroken, 10);
        var silver = s.Session.Silver;

        s.Session.RunMistToDawn();

        var fixture = s.Session.Fixtures.Single(f => f.Id == lantern);
        Assert.That(fixture.IsBroken, Is.False);
        Assert.That(fixture.Health, Is.EqualTo(fixture.MaxHealth));
        Assert.That(fixture.Origin, Is.EqualTo(new Tile(12, 7)), "still in place");
        Assert.That(s.Session.DrainEvents().OfType<FixtureRepaired>().Select(r => r.Fixture), Does.Contain(lantern));
        Assert.That(s.Session.Silver, Is.EqualTo(silver + s.Content.Balance.IncomePerCycle), "fixture repairs are free");
    }

    [TestCase(0.0, true, TestName = "No integrity left: lost")]
    [TestCase(1.0, false, TestName = "Some integrity left: Dawn")]
    [Property("Requirement", "BRD-06")]
    [Property("Requirement", "RUN-03")]
    public void TheRunIsLostIfIntegrityIsZeroAtTheEndOfTheMist(double integrity, bool lost)
    {
        var s = new Scenario(sturdy: false);
        s.Begin();
        s.Session.RunForTests.Integrity = integrity;

        s.Session.RunMistToDawn();

        Assert.That(s.Session.Phase, Is.EqualTo(lost ? Phase.RunOver : Phase.Dawn));
        Assert.That(s.Session.EndReason, Is.EqualTo(lost ? RunEndReason.InnDestroyed : null));
        Assert.That(s.Session.CycleLogEntries.Any(e => e.Kind == "run.lost" && e.Cause == "integrity.zero"), Is.EqualTo(lost));
    }

    [Test]
    [Property("Requirement", "BRD-07")]
    public void IntegrityIsRepairedAtDawnForOneSilverPerFivePoints()
    {
        var s = new Scenario(sturdy: false);
        Assert.That(s.Session.Execute(new RepairIntegrity(1)).Failure, Is.EqualTo(CommandFailure.WrongPhase));
        s.Begin();
        s.Session.RunForTests.Integrity = 80;
        s.Session.RunMistToDawn();
        Assert.That(s.Session.Silver, Is.EqualTo(15));

        Assert.That(s.Session.Execute(new RepairIntegrity(2)).Succeeded, Is.True);
        Assert.That(s.Session.Integrity, Is.EqualTo(90));
        Assert.That(s.Session.Silver, Is.EqualTo(13));

        Assert.That(s.Session.Execute(new RepairIntegrity(10)).Succeeded, Is.True);
        Assert.That(s.Session.Integrity, Is.EqualTo(100));
        Assert.That(s.Session.Silver, Is.EqualTo(11), "only what was missing is charged");

        Assert.That(s.Session.Execute(new RepairIntegrity(1)).Failure, Is.EqualTo(CommandFailure.NothingToRepair));
        Assert.That(s.Session.Execute(new RepairIntegrity(0)).Failure, Is.EqualTo(CommandFailure.InvalidValue));

        s.Session.RunForTests.Integrity = 10;
        Assert.That(s.Session.Execute(new RepairIntegrity(50)).Failure, Is.EqualTo(CommandFailure.NotEnoughSilver),
            "18 silver needed, 11 held");
    }

    [Test]
    [Property("Requirement", "AGI-08")]
    public void ViolenceAndBloodNearbyAreTriggers()
    {
        var s = new Scenario(set: c => TestGame.WithCreature(c, "creature-01", d => d with
        {
            Triggers = [new TriggerDef(TriggerKind.Blood, Spike: 10)],
        }));
        s.Begin();
        var brawler = s.Spawn("creature-04", new Tile(12, 9));
        var drowned = s.Spawn("creature-01", new Tile(16, 12));
        var run = s.Session.RunForTests;

        run.Attacks.Add((new TilePosition(14, 9), s.Session.Time.TotalSeconds));
        run.BloodMarks.Add(s.Creature(drowned).Position);
        s.Step();

        Assert.That(s.Creature(brawler).ActiveTriggers, Does.Contain(TriggerKind.Violence), "an attack within 4 tiles");
        Assert.That(s.Creature(drowned).ActiveTriggers, Does.Contain(TriggerKind.Blood), "a fallen unit within 4 tiles");
        Assert.That(s.Session.CycleLogEntries.Count(e => e.Kind == "trigger.fired" && e.Cause is "trigger.violence" or "trigger.blood"),
            Is.EqualTo(2));
    }

    [Test]
    [Property("Requirement", "INV-02")]
    [Property("Requirement", "TST-04")]
    [Property("Requirement", "TST-06")]
    public void AMistWithFightingPlaysOutTheSameEveryTime()
    {
        static (ulong Hash, IReadOnlyList<string> Kinds) Play()
        {
            // Creatures four times as quick to anger, so that some turn and fight.
            var content = TestGame.ContentWith(set: c => c with
            {
                Creatures = c.Creatures.Select(d => d with { Agitation = d.Agitation with { RatePerSecond = d.Agitation.RatePerSecond * 4 } }).ToList(),
            });
            var session = TestGame.NewSession(21, content);
            Assert.That(session.Execute(new PlaceUnit(session.Units.Single(u => u.StaffId == "staff-09").Id, new Tile(12, 9))).Succeeded, Is.True);
            Assert.That(session.Execute(new PlaceUnit(session.Units.Single(u => u.StaffId == "staff-01").Id, new Tile(15, 10))).Succeeded, Is.True);
            TestGame.StartMist(session);
            session.RunMistToDawn();
            return (session.ComputeStateHash(), session.CycleLogEntries.Select(e => e.Kind).ToList());
        }

        var first = Play();
        var second = Play();

        Assert.That(second.Hash, Is.EqualTo(first.Hash));
        Assert.That(second.Kinds, Is.EqualTo(first.Kinds));
        Assert.That(first.Kinds, Does.Contain("creature.turned").And.Contain("combat.attack"), "the mist had fighting in it");
    }
}
