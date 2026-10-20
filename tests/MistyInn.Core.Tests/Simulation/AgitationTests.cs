using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Events;
using MistyInn.Core.Run;

namespace MistyInn.Core.Tests.Simulation;

/// <summary>Agitation, thresholds, triggers and contagion (section 9, TST-05).</summary>
public class AgitationTests
{
    // Creature 01 (Drowned Deep): arrival 15, 0.6 per second, Drink craves, Quiet likes, Heat trigger (+10, ×1.5).
    private const string Drowned = "creature-01";

    private static readonly Tile Common = new(12, 9);

    [Test]
    [Property("Requirement", "AGI-01")]
    [Property("Requirement", "AGI-02")]
    public void AgitationStartsAtTheArrivalValueAndRisesAtTheBaseRate()
    {
        var s = new Scenario();
        s.Begin();
        var id = s.Spawn(Drowned, Common);

        Assert.That(s.Creature(id).Agitation, Is.EqualTo(15));
        s.Run(10);

        Assert.That(s.Creature(id).Agitation, Is.EqualTo(21).Within(1e-6));
    }

    [Test]
    [Property("Requirement", "AGI-03")]
    public void RestlessCreaturesGetAgitatedFaster()
    {
        var s = new Scenario();
        s.Begin();
        var id = s.Spawn(Drowned, Common);
        s.Creature(id).Agitation = 60;
        s.Run(1);
        var before = s.Creature(id).Agitation;

        s.Run(10);

        Assert.That(s.Creature(id).State, Is.EqualTo(CreatureState.Restless));
        Assert.That(s.Creature(id).Agitation - before, Is.EqualTo(0.6 * 1.5 * 10).Within(1e-6));
    }

    [Test]
    [Property("Requirement", "AGI-03")]
    [Property("Requirement", "INV-12")]
    public void CrossingAThresholdIsAnEventAndALogEntry()
    {
        var s = new Scenario();
        s.Begin();
        var id = s.Spawn(Drowned, Common);
        s.Session.DrainEvents();
        s.Creature(id).Agitation = 74.99;

        s.Run(1);

        Assert.That(s.Creature(id).State, Is.EqualTo(CreatureState.Menacing));
        Assert.That(s.Session.DrainEvents().OfType<ThresholdCrossed>().Single(),
            Has.Property(nameof(ThresholdCrossed.To)).EqualTo(CreatureState.Menacing));
        Assert.That(s.Session.CycleLogEntries.Any(e => e.Kind == "agitation.state" && e.Cause == "state.menacing"), Is.True);
    }

    [Test]
    [Property("Requirement", "AGI-03")]
    [Property("Requirement", "AGI-04")]
    public void AtOneHundredACreatureTurnsAndStaysTurned()
    {
        var s = new Scenario();
        s.Begin();
        var id = s.Spawn(Drowned, Common);
        s.Session.DrainEvents();
        s.Creature(id).Agitation = 99.99;

        s.Run(1);

        Assert.That(s.Creature(id).State, Is.EqualTo(CreatureState.Turned));
        Assert.That(s.Session.DrainEvents().OfType<CreatureTurned>().Count(), Is.EqualTo(1));
        s.Run(20);
        Assert.That(s.Creature(id).State, Is.EqualTo(CreatureState.Turned));
        Assert.That(s.Creature(id).Agitation, Is.EqualTo(100));
    }

    [Test]
    [Property("Requirement", "AGI-09")]
    [Property("Requirement", "INV-12")]
    public void ATurningCreatureAgitatesEveryoneCloseBy()
    {
        var s = new Scenario();
        s.Begin();
        var turning = s.Spawn(Drowned, new Tile(12, 9));
        var near = s.Spawn(Drowned, new Tile(13, 9));
        var far = s.Spawn(Drowned, new Tile(18, 13));
        s.Creature(turning).Agitation = 99.99;

        s.Session.Tick(TimeSpan.FromSeconds(1.0 / 30) + TimeSpan.FromTicks(1));

        Assert.That(s.Creature(near).Agitation, Is.GreaterThan(15 + 14.9));
        Assert.That(s.Creature(far).Agitation, Is.LessThan(16));
        Assert.That(s.Session.CycleLogEntries.Count(e => e.Kind == "agitation.contagion"), Is.EqualTo(1));
        Assert.That(s.Session.CycleLogEntries.Single(e => e.Kind == "agitation.contagion").Source, Is.EqualTo(turning.ToRef()));
    }

    [Test]
    [Property("Requirement", "AGI-09")]
    public void ContagionCanSetOffAChainReaction()
    {
        var s = new Scenario();
        s.Begin();
        var first = s.Spawn(Drowned, new Tile(12, 9));
        var second = s.Spawn(Drowned, new Tile(14, 9));
        var third = s.Spawn(Drowned, new Tile(17, 9));
        s.Creature(first).Agitation = 99.99;
        s.Creature(second).Agitation = 90;
        s.Creature(third).Agitation = 90;

        s.Session.Tick(TimeSpan.FromSeconds(1.0 / 30) + TimeSpan.FromTicks(1));

        Assert.That(s.Creature(second).State, Is.EqualTo(CreatureState.Turned), "within 4 tiles of the first");
        Assert.That(s.Creature(third).State, Is.EqualTo(CreatureState.Turned), "only within 4 tiles of the second");
    }

    [Test]
    [Property("Requirement", "AGI-08")]
    [Property("Requirement", "BST-02")]
    public void ATriggerSpikesOnceWhenItStartsAndIsSeen()
    {
        // Creature 05 (Hollow Wood): Heat +20 (×2 while it holds). The hearth gives off Heat within 3 tiles.
        var s = new Scenario();
        s.Place("hearth", new Tile(12, 7));
        s.Begin();
        var id = s.Spawn("creature-05", new Tile(12, 9));

        s.Session.Tick(TimeSpan.FromSeconds(1.0 / 30) + TimeSpan.FromTicks(1));
        var afterSpike = s.Creature(id).Agitation;
        s.Session.Tick(TimeSpan.FromSeconds(1.0 / 30) + TimeSpan.FromTicks(1));

        Assert.That(afterSpike, Is.GreaterThan(35).And.LessThan(35.2));
        Assert.That(s.Creature(id).Agitation - afterSpike, Is.LessThan(0.1), "no second spike while it holds");
        Assert.That(s.Session.CycleLogEntries.Any(e => e.Kind == "trigger.fired" && e.Cause == "trigger.heat"), Is.True);
        Assert.That(s.Session.Bestiary.Entry(s.Content.Creature("creature-05"), s.Content.Balance.Bestiary).TriggersSeen,
            Does.Contain(TriggerKind.Heat));
    }

    [Test]
    [Property("Requirement", "AGI-08")]
    public void TheDoorIsColdWhileTheMistIsIn()
    {
        // Creature 03 (Ashen Waste): Cold +10.
        var s = new Scenario();
        s.Begin();
        var id = s.Spawn("creature-03", new Tile(13, 12));

        s.Session.Tick(TimeSpan.FromSeconds(1.0 / 30) + TimeSpan.FromTicks(1));

        Assert.That(s.Creature(id).ActiveTriggers, Does.Contain(TriggerKind.Cold));
        Assert.That(s.Session.CycleLogEntries.Any(e => e.Cause == "trigger.cold"), Is.True);
    }

    [Test]
    [Property("Requirement", "AGI-08")]
    public void CrowdingAndRivalPlanesAreTriggers()
    {
        var s = new Scenario();
        s.Begin();
        var gear = s.Spawn("creature-07", new Tile(12, 9));
        for (var i = 0; i < 4; i++)
        {
            s.Spawn(Drowned, new Tile(10 + i, 10));
        }

        var pale = s.Spawn("creature-10", new Tile(15, 9));
        s.Spawn("creature-05", new Tile(14, 9));

        s.Session.Tick(TimeSpan.FromSeconds(1.0 / 30) + TimeSpan.FromTicks(1));

        Assert.That(s.Creature(gear).ActiveTriggers, Does.Contain(TriggerKind.Crowding), "seven in the common room, more than 4");
        Assert.That(s.Creature(pale).ActiveTriggers, Does.Contain(TriggerKind.RivalPlane), "a Hollow Wood creature in the same zone");
    }

    [Test]
    [Property("Requirement", "AGI-02")]
    [Property("Requirement", "FIX-02")]
    [Property("Requirement", "FIX-04")]
    public void AWardCircleSlowsTheRise()
    {
        static double RiseOverThreeSeconds(bool ward)
        {
            // No Drink supplies: the creature waits at the bar, unserved, so only its own rate counts.
            var s = new Scenario(balance: b => b with { StartingSupplies = new Dictionary<Supply, int> { [Supply.Drink] = 0 } });
            s.Place("bar-counter", new Tile(11, 4));
            if (ward)
            {
                s.Place("ward-circle", new Tile(10, 5));
            }

            s.PlaceStaff("staff-01", new Tile(12, 5));
            s.Begin();
            var id = s.Spawn(Drowned, new Tile(11, 5));
            s.RunUntil(() => s.Creature(id).Activity == CreatureActivity.Waiting);
            var before = s.Creature(id).Agitation;
            s.Run(3);
            return s.Creature(id).Agitation - before;
        }

        Assert.That(RiseOverThreeSeconds(ward: false), Is.EqualTo(0.6 * 3).Within(1e-6));
        Assert.That(RiseOverThreeSeconds(ward: true), Is.EqualTo(0.6 * 0.75 * 3).Within(1e-6));
    }

    [Test]
    [Property("Requirement", "AGI-03")]
    [Property("Requirement", "INV-12")]
    public void MenacingCreaturesBreakAFixtureBesideThem()
    {
        var s = new Scenario(balance: b => b with { StartingSupplies = new Dictionary<Supply, int> { [Supply.Drink] = 0 } });
        var bar = s.Place("bar-counter", new Tile(11, 4));
        s.PlaceStaff("staff-01", new Tile(12, 5));
        s.Begin();
        var id = s.Spawn(Drowned, new Tile(11, 5));
        s.RunUntil(() => s.Creature(id).Activity == CreatureActivity.Waiting);
        s.Creature(id).Agitation = 80;

        s.Run(10);

        Assert.That(s.Session.FixtureForTests(bar).IsBroken, Is.True);
        Assert.That(s.Session.CycleLogEntries.Single(e => e.Kind == "fixture.broken").Cause, Is.EqualTo("agitation.menacing"));
        Assert.That(s.Session.DrainEvents().OfType<FixtureBroken>().Single().Fixture, Is.EqualTo(bar));
    }

    [Test]
    [Property("Requirement", "AGI-03")]
    public void RestlessCreaturesKnockFixturesAbout()
    {
        var s = new Scenario(balance: b => b with { StartingSupplies = new Dictionary<Supply, int> { [Supply.Drink] = 0 } });
        var bar = s.Place("bar-counter", new Tile(11, 4));
        s.PlaceStaff("staff-01", new Tile(12, 5));
        s.Begin();
        var id = s.Spawn(Drowned, new Tile(11, 5));
        s.RunUntil(() => s.Creature(id).Activity == CreatureActivity.Waiting);
        s.Creature(id).Agitation = 55;

        s.Run(4);

        Assert.That(s.Session.FixtureForTests(bar).Health, Is.EqualTo(60 - 0.5 * 4).Within(0.05));
    }

    [Test]
    [Property("Requirement", "AGI-03")]
    [Property("Requirement", "AGI-08")]
    public void AMenacingCreatureFrightensOthersNearby()
    {
        var s = new Scenario(balance: b => b with { StartingSupplies = new Dictionary<Supply, int> { [Supply.Drink] = 0 } });
        s.Place("bar-counter", new Tile(11, 4));
        s.PlaceStaff("staff-01", new Tile(12, 5));
        s.Begin();
        var menacing = s.Spawn(Drowned, new Tile(11, 5));
        var other = s.Spawn(Drowned, new Tile(10, 4));
        s.Creature(menacing).Agitation = 80;

        s.Run(1);

        Assert.That(s.Creature(other).Frightened, Is.True);
        Assert.That(s.Session.CycleLogEntries.Any(e => e.Kind == "agitation.frightened"), Is.True);
    }
}
