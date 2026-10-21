using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Run;
using static MistyInn.Core.Tests.Content.ContentTestKit;

namespace MistyInn.Core.Tests.Simulation;

/// <summary>Basic service at tables, the Alcohol appetite, and staff homes and roles (SDD v1.10, step 6b).</summary>
public class BasicServiceTests
{
    // Creature 07 craves Quiet and likes Order: nothing in these inns provides either, so it sits at a table. The
    // table at (12,8) seats guests at (12,7) and (11,8). The innkeeper (soothe 0.5) stands at (11,13); Staff 02
    // (Companion/Server, soothe 1.0) knows Food at ★1 and no Company, so it is a runner.
    private static readonly Tile Table = new(12, 8);

    private static Unit Keeper(Scenario s) => s.Session.UnitForTests(TestGame.Innkeeper(s.Session).Id);

    [Test]
    [Property("Requirement", "CRE-04")]
    [Property("Requirement", "AGI-11")]
    public void AGuestWithNothingForItsAppetitesSitsAtATable()
    {
        var s = new Scenario();
        s.Place("table", Table);
        s.Begin();
        var id = s.Spawn("creature-07", new Tile(12, 10));

        s.RunUntil(() => s.Creature(id).Activity == CreatureActivity.Waiting, 15);

        Assert.That(s.Creature(id).Spot, Is.EqualTo(new Tile(12, 7)).Or.EqualTo(new Tile(11, 8)));
    }

    [Test]
    [Property("Requirement", "AGI-11")]
    [Property("Requirement", "ESC-06")]
    [Property("Requirement", "STF-04")]
    public void TheInnkeeperWalksToATableAndGivesBasicService()
    {
        var s = new Scenario();
        s.Place("table", Table);
        s.Begin();
        var id = s.Spawn("creature-07", new Tile(12, 10));
        var drink = s.Session.Supplies[Supply.Drink];

        s.RunUntil(() => s.Creature(id).ServedBy == "innkeeper:food", 30);

        Assert.That(s.Session.ServiceRates[id].Rate, Is.EqualTo(0.5 * 0.3 * 2).Within(1e-9), "soothe 0.5 × basic 0.3 × scale 2");
        Assert.That(Keeper(s).Position, Is.Not.EqualTo(TilePosition.Center(new Tile(11, 13))), "it walked to the table");
        Assert.That(s.Session.Supplies[Supply.Drink], Is.EqualTo(drink), "basic service uses no supplies");
        Assert.That(s.Creature(id).WasServed, Is.True, "it counts as served (tributes, bestiary)");
    }

    [Test]
    [Property("Requirement", "AGI-11")]
    [Property("Requirement", "STF-02")]
    public void TheFoodTechniqueGivesBetterBasicService()
    {
        var s = new Scenario(startingStaff: ["staff-02"]);
        s.Place("table", Table);
        s.PlaceStaff("staff-02", new Tile(14, 10));
        s.Begin();
        var guest = s.Creature(s.Spawn("creature-07", new Tile(12, 10)));
        var food = s.Content.Technique("food");
        var service = s.Session.ServiceForTests;

        Assert.That(service.SootheRate(s.Unit("staff-02"), food, guest), Is.EqualTo(1.0 * 0.6 * 2).Within(1e-9));
        Assert.That(service.SootheRate(Keeper(s), food, guest), Is.EqualTo(0.5 * 0.3 * 2).Within(1e-9));
        Assert.That(service.Strength(guest.Def, food), Is.EqualTo(1), "everyone takes basic service as if they craved it");
    }

    [Test]
    [Property("Requirement", "AGI-11")]
    [Property("Requirement", "STF-04")]
    public void BasicServiceIsChosenOnlyWhenNothingBetterIsInReach()
    {
        // Staff 01 at the bar: Creature 01 craves Alcohol (value 1.25) and sits at the bar; Creature 07 sits at a table
        // in reach (basic service, value 0.3). Drink comes first.
        var s = new Scenario();
        s.Place("bar-counter", new Tile(11, 4));
        s.Place("table", new Tile(14, 6));
        s.PlaceStaff("staff-01", new Tile(13, 5));
        s.Begin();
        var thirsty = s.Spawn("creature-01", new Tile(11, 5));
        s.Spawn("creature-07", new Tile(15, 8));

        s.RunUntil(() => s.Creature(thirsty).ServedBy is not null, 15);

        Assert.That(s.Creature(thirsty).ServedBy, Is.EqualTo("staff-01:drink"));
    }

    [Test]
    [Property("Requirement", "STF-04")]
    public void ARunnerGoesHomeWhenThereIsNothingToServe()
    {
        var s = new Scenario();
        s.Place("table", Table);
        s.Begin();
        var id = s.Spawn("creature-07", new Tile(12, 10));
        s.RunUntil(() => s.Creature(id).ServedBy == "innkeeper:food", 30);

        s.Session.RunForTests.Creatures.Remove(s.Creature(id));
        var home = TilePosition.Center(new Tile(11, 13));
        s.RunUntil(() => Keeper(s).Activity == UnitActivity.AtPost && Keeper(s).Position == home, 20);

        Assert.That(Keeper(s).ServeTarget, Is.Null);
    }

    [Test]
    [Property("Requirement", "STF-04")]
    public void TwoRunnersNeverGoForTheSameGuest()
    {
        var s = new Scenario(startingStaff: ["staff-02"]);
        s.Place("table", Table);
        s.PlaceStaff("staff-02", new Tile(16, 9));
        s.Begin();
        var id = s.Spawn("creature-07", new Tile(12, 10));

        for (var i = 0; i < 20 * GameSession.StepsPerSecond; i++)
        {
            s.Step();
            var onIt = s.Session.RunForTests.Units.Count(u => u.ServeTarget == id || u.Session?.Targets.Contains(id) == true);
            Assert.That(onIt, Is.LessThanOrEqualTo(1), $"step {i}");
        }
    }

    [Test]
    [Property("Requirement", "STF-04")]
    public void BouncersAndCompanionsStayHome()
    {
        // Staff 09 is a Fighter (a bouncer); Staff 03 knows Company (a companion). A guest waits at a table out of reach.
        var s = new Scenario(startingStaff: ["staff-09", "staff-03"]);
        s.Place("table", Table);
        s.PlaceStaff("staff-09", new Tile(12, 12));
        s.PlaceStaff("staff-03", new Tile(16, 9));
        s.Begin();
        s.Spawn("creature-07", new Tile(12, 10));

        s.Run(10);

        Assert.That(s.Unit("staff-09").Position, Is.EqualTo(TilePosition.Center(new Tile(12, 12))));
        Assert.That(s.Unit("staff-03").Position, Is.EqualTo(TilePosition.Center(new Tile(16, 9))));
    }

    [Test]
    [Property("Requirement", "STF-04")]
    public void TheMainClassDecidesTheRole()
    {
        var content = GameContent.Default;
        var ids = new IdAllocator();
        var fighterWithCompany = RunState.HireUnit(content, ids, "staff-09", 4); // preset r4-company
        var barkeep = RunState.HireUnit(content, ids, "staff-01", 1);
        var companion = RunState.HireUnit(content, ids, "staff-01", 3); // preset r3-company
        var gambler = RunState.HireUnit(content, ids, "staff-05", 1);

        Assert.That(fighterWithCompany.Techniques.Select(t => t.Id), Does.Contain("company"));
        Assert.That(fighterWithCompany.IsRunner, Is.False, "main class Fighter: a bouncer");
        Assert.That(barkeep.IsRunner, Is.True);
        Assert.That(companion.IsRunner, Is.False, "knows Company: a companion");
        Assert.That(gambler.IsRunner, Is.True);
    }

    [Test]
    [Property("Requirement", "AGI-05")]
    [Property("Requirement", "FIX-06")]
    [Property("Requirement", "STF-02")]
    public void AlcoholReplacesTheDrinkAndFoodAppetites()
    {
        var content = GameContent.Default;

        Assert.That(content.Technique("drink").Answers, Is.EqualTo(new[] { Appetite.Alcohol }));
        Assert.That(content.Technique("food").Answers, Is.Empty);
        Assert.That(content.Technique("food").Supply, Is.Null);
        Assert.That(content.Balance.Supplies.Keys, Is.EquivalentTo(new[] { Supply.Drink }));
        Assert.That(content.Creature("creature-01").Appetites[0].Appetite, Is.EqualTo(Appetite.Alcohol));
        Assert.That(content.Balance.BasicServiceSoothe, Is.EqualTo(0.3));
    }

    [Test]
    [Property("Requirement", "AGI-11")]
    [Property("Requirement", "DAT-04")]
    public void FoodMustStayBasicService()
    {
        AssertRejected(WithTechnique("food", t => t with { Answers = [Appetite.Alcohol] }), "Food is basic service");
        AssertRejected(Set with { Balance = Set.Balance with { BasicServiceSoothe = 0 } }, "out of range");
    }
}
