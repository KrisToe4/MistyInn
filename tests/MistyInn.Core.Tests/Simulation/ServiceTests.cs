using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Run;

namespace MistyInn.Core.Tests.Simulation;

/// <summary>Staff service, fixtures that give appetites, and where creatures go (STF-04, AGI-05 to AGI-07, CRE-04).</summary>
public class ServiceTests
{
    // The bar counter at (11,4) covers (11,4)-(13,4); its three seats are (10,4), (14,4) and (11,5).
    private static readonly Tile BarOrigin = new(11, 4);
    private static readonly Tile Barkeep = new(12, 5);

    private static Scenario BarScenario(Func<BalanceDef, BalanceDef>? balance = null)
    {
        var s = new Scenario(balance: balance);
        s.Place("bar-counter", BarOrigin);
        s.PlaceStaff("staff-01", Barkeep);
        return s;
    }

    [Test]
    [Property("Requirement", "CRE-04")]
    [Property("Requirement", "AGI-06")]
    [Property("Requirement", "STF-04")]
    [Property("Requirement", "INV-12")]
    public void ACreatureFindsTheBarAndTheBarkeepSoothesIt()
    {
        var s = BarScenario();
        s.Begin();
        var id = s.Spawn("creature-01", new Tile(12, 8));

        s.RunUntil(() => s.Creature(id).ServedBy is not null, 20);

        var creature = s.Creature(id);
        Assert.That(creature.Activity, Is.EqualTo(CreatureActivity.Waiting));
        Assert.That(new[] { new Tile(10, 4), new Tile(14, 4), new Tile(11, 5) }, Does.Contain(creature.Position.NearestTile));
        Assert.That(creature.ServedBy, Is.EqualTo("staff-01:drink"));
        Assert.That(s.Session.Supplies[Supply.Drink], Is.EqualTo(5), "one Drink per serving");

        var log = s.Session.CycleLogEntries.Single(e => e.Kind == "service.started");
        Assert.That(log.Cause, Is.EqualTo("technique.drink"));
        Assert.That(log.Subject, Is.EqualTo(id.ToRef()));
        Assert.That(log.Source, Is.EqualTo(s.Staff("staff-01").Id.ToRef()));

        var before = creature.Agitation;
        s.Run(2);
        Assert.That(creature.Agitation, Is.LessThan(before));
    }

    [Test]
    [Property("Requirement", "AGI-05")]
    [Property("Requirement", "AGI-06")]
    public void TheSootheRateFollowsTheFormula()
    {
        // Staff 01: soothe 1.0 at rank 1; Drink soothe 1.0; Creature 01 craves Drink (1.0), preferring it (×1.25);
        // the soothe scale is 2. Creature 09 only likes Drink (×0.5).
        var s = BarScenario();
        s.Begin();
        var craves = s.Spawn("creature-01", new Tile(11, 5));
        s.Run(1);

        Assert.That(s.Session.ServiceRates[craves].Rate, Is.EqualTo(2.5).Within(1e-9));

        var drink = s.Content.Technique("drink");
        var service = s.Session.ServiceForTests;
        Assert.That(service.Strength(s.Content.Creature("creature-01"), drink), Is.EqualTo(1.25));
        Assert.That(service.Strength(s.Content.Creature("creature-09"), drink), Is.EqualTo(0.5));
        Assert.That(service.Strength(s.Content.Creature("creature-01"), s.Content.Technique("song")), Is.Zero);
    }

    [Test]
    [Property("Requirement", "AGI-07")]
    [Property("Requirement", "FIX-06")]
    public void WithoutSuppliesTheDrinkTechniqueCannotBeUsed()
    {
        var s = BarScenario(b => b with { StartingSupplies = new Dictionary<Supply, int> { [Supply.Drink] = 0 } });
        s.Begin();
        var id = s.Spawn("creature-01", new Tile(11, 5));
        var start = s.Creature(id).Agitation;

        s.Run(20);

        Assert.That(s.Session.CycleLogEntries.Any(e => e.Kind == "service.started"), Is.False);
        Assert.That(s.Creature(id).Agitation, Is.GreaterThan(start));
    }

    [Test]
    [Property("Requirement", "AGI-07")]
    public void ATechniqueServesOnlyItsCapacityAtOnce()
    {
        var s = BarScenario();
        s.Begin();
        var a = s.Spawn("creature-01", new Tile(10, 4));
        var b = s.Spawn("creature-01", new Tile(11, 5));
        var most = 0;

        for (var i = 0; i < 20 * GameSession.StepsPerSecond; i++)
        {
            s.Session.Tick(TimeSpan.FromSeconds(1.0 / 30) + TimeSpan.FromTicks(1));
            most = Math.Max(most, new[] { a, b }.Count(id => s.Creature(id).ServedBy is not null));
        }

        Assert.That(most, Is.EqualTo(1), "Drink has a capacity of 1");
    }

    [Test]
    [Property("Requirement", "STF-04")]
    [Property("Requirement", "INV-02")]
    public void StaffServeTheBestMatchFirstAndBreakTiesByCreatureId()
    {
        var s = BarScenario();
        s.Begin();
        var likes = s.Spawn("creature-09", new Tile(10, 4));
        var craves = s.Spawn("creature-01", new Tile(11, 5));

        s.Session.Tick(TimeSpan.FromSeconds(1.0 / 30) + TimeSpan.FromTicks(1));

        Assert.That(s.Staff("staff-01").ServingCreatures, Is.EqualTo(new[] { craves }), "craves and preferred beats likes");
        Assert.That(likes.Value, Is.LessThan(craves.Value));

        var tie = BarScenario();
        tie.Begin();
        var first = tie.Spawn("creature-01", new Tile(10, 4));
        tie.Spawn("creature-01", new Tile(11, 5));
        tie.Session.Tick(TimeSpan.FromSeconds(1.0 / 30) + TimeSpan.FromTicks(1));

        Assert.That(tie.Staff("staff-01").ServingCreatures, Is.EqualTo(new[] { first }), "equal value: lowest creature ID");
    }

    [Test]
    [Property("Requirement", "STF-04")]
    [Property("Requirement", "STF-05")]
    public void StaffStayAtTheirPostAndBenchedStaffServeNoOne()
    {
        var s = new Scenario();
        s.Place("bar-counter", BarOrigin);
        s.Begin();
        var id = s.Spawn("creature-01", new Tile(11, 5));

        s.Run(15);

        Assert.That(s.Creature(id).WasServed, Is.False, "the barkeep is on the bench");

        var placed = BarScenario();
        placed.Begin();
        placed.Spawn("creature-01", new Tile(11, 5));
        placed.Run(15);
        Assert.That(placed.Staff("staff-01").Tile, Is.EqualTo(Barkeep));
    }

    [Test]
    [Property("Requirement", "AGI-06")]
    [Property("Requirement", "FIX-02")]
    public void AHearthWarmsTheCreaturesInItsSeats()
    {
        // Creature 03 craves Warmth. The hearth at (12,7) covers (12,7) and (13,7).
        var s = new Scenario();
        s.Place("hearth", new Tile(12, 7));
        s.Begin();
        var id = s.Spawn("creature-03", new Tile(12, 10));

        s.RunUntil(() => s.Creature(id).ServedBy is not null, 20);

        Assert.That(s.Creature(id).ServedBy, Is.EqualTo("fixture:hearth"));
        Assert.That(s.Session.ServiceRates[id].Rate, Is.EqualTo(2.0).Within(1e-9));
        Assert.That(s.Session.CycleLogEntries.Any(e => e.Kind == "service.started" && e.Cause == "fixture.hearth"), Is.True);
    }

    [Test]
    [Property("Requirement", "FIX-02")]
    public void NoiseNearAQuietCornerCancelsIt()
    {
        // Creature 07 craves Quiet. The stage gives off Noise within 4 tiles; the quiet corner is cancelled by Noise
        // within 3 of it.
        static bool Served(bool withStage)
        {
            var s = new Scenario();
            s.Place("quiet-corner", new Tile(14, 7));
            if (withStage)
            {
                s.Place("stage", new Tile(16, 6));
            }

            s.Begin();
            var id = s.Spawn("creature-07", new Tile(13, 9));
            for (var i = 0; i < 15 * GameSession.StepsPerSecond; i++)
            {
                s.Session.Tick(TimeSpan.FromSeconds(1.0 / 30) + TimeSpan.FromTicks(1));
                if (s.Creature(id).ServedBy is not null)
                {
                    return true;
                }
            }

            return false;
        }

        Assert.That(Served(withStage: false), Is.True);
        Assert.That(Served(withStage: true), Is.False);
    }

    [Test]
    [Property("Requirement", "AGI-05")]
    [Property("Requirement", "FIX-02")]
    public void OrderAndDarknessHaveProviders()
    {
        var s = new Scenario();
        s.Place("ledger-desk", new Tile(12, 7));
        s.Begin();
        var gear = s.Spawn("creature-08", new Tile(12, 10));

        s.RunUntil(() => s.Creature(gear).ServedBy is not null, 20);
        Assert.That(s.Creature(gear).ServedBy, Is.EqualTo("fixture:ledger-desk"));

        Assert.That(s.Content.Technique("cards").Answers, Does.Contain(Appetite.Order));
        Assert.That(s.Content.Technique("tales").Answers, Does.Contain(Appetite.Darkness));
        Assert.That(s.Content.Fixture("shadowed-alcove").Effects.Single().Appetite, Is.EqualTo(Appetite.Darkness));
    }

    [Test]
    [Property("Requirement", "CRE-04")]
    public void WithNothingToSeekACreatureWanders()
    {
        var s = new Scenario();
        s.Begin();
        var id = s.Spawn("creature-01", new Tile(12, 9));

        s.Run(1);

        Assert.That(s.Creature(id).Activity, Is.EqualTo(CreatureActivity.Wandering).Or.EqualTo(CreatureActivity.Idle));
        Assert.That(s.Creature(id).Spot, Is.Null);
    }

    [Test]
    [Property("Requirement", "STF-04")]
    public void ServiceSessionsAreEvents()
    {
        var s = BarScenario();
        s.Begin();
        var id = s.Spawn("creature-01", new Tile(11, 5));
        s.Session.DrainEvents();

        s.Run(1);

        var started = s.Session.DrainEvents().OfType<ServiceStarted>().Single();
        Assert.That(started.TechniqueId, Is.EqualTo("drink"));
        Assert.That(started.Creatures, Is.EqualTo(new[] { id }));
    }
}
