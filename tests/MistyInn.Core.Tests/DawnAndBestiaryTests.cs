using MistyInn.Core.Bestiary;
using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Content;
using MistyInn.Core.Events;
using MistyInn.Core.Randomness;
using MistyInn.Core.Run;

namespace MistyInn.Core.Tests;

/// <summary>Tributes at dawn and the bestiary (CRE-05, BST-01 to BST-04).</summary>
public class DawnAndBestiaryTests
{
    private static readonly BestiaryBalanceDef Thresholds = new(StudiedPoints: 3);

    [Test]
    [Property("Requirement", "CRE-05")]
    [Property("Requirement", "SHP-01")]
    [Property("Requirement", "INV-12")]
    public void CalmCreaturesLeaveATributeAtDawnAndTurnedOnesDoNot()
    {
        // Served at the bar, Creature 01 stays calm all night; unserved it would turn at about 114 s. Creature 02 turns
        // in the last seconds, far from the bar, so it has no time to reach anyone (step 5 added combat).
        var s = new Scenario(balance: b => b with { StartingSupplies = new Dictionary<Supply, int> { [Supply.Drink] = 100 } });
        s.Place("bar-counter", new Tile(11, 4));
        s.PlaceStaff("staff-01", new Tile(12, 5));
        s.Begin();
        s.Spawn("creature-01", new Tile(11, 5));
        s.RunUntil(() => s.Session.TimeToDawn <= TimeSpan.FromSeconds(3), maxSeconds: 130);
        var turned = s.Spawn("creature-02", new Tile(18, 13));
        s.Creature(turned).Agitation = 99.99;
        s.Session.DrainEvents();

        s.Session.RunMistToDawn();

        var tributes = s.Session.DrainEvents().OfType<TributeLeft>().ToList();
        Assert.That(tributes.Select(t => t.Silver), Is.EqualTo(new[] { 3 }), "only the calm one, tribute 3");
        Assert.That(s.Session.Silver, Is.EqualTo(10 + 3 + 5), "starting silver, the tribute and the income");
        Assert.That(s.Session.CycleLogEntries.Count(e => e.Kind == "tribute.left"), Is.EqualTo(1));
    }

    [Test]
    [Property("Requirement", "BST-01")]
    [Property("Requirement", "BST-02")]
    public void ACreatureIsSeenWhenItArrives()
    {
        var s = new Scenario();
        var drowned = s.Content.Creature("creature-01");
        Assert.That(s.Session.Bestiary.LevelOf(drowned, s.Content.Balance.Bestiary), Is.EqualTo(BestiaryLevel.Unknown));
        s.Begin();
        s.Session.DrainEvents();

        s.Spawn("creature-01", new Tile(12, 9));

        Assert.That(s.Session.Bestiary.LevelOf(drowned, s.Content.Balance.Bestiary), Is.EqualTo(BestiaryLevel.Seen));
        Assert.That(s.Session.DrainEvents().OfType<BestiaryRevealed>().Single().Level, Is.EqualTo(BestiaryLevel.Seen));
    }

    [Test]
    [Property("Requirement", "BST-01")]
    public void LevelsFollowTheThresholds()
    {
        var bestiary = new CreatureBestiary();
        var drowned = GameContent.Default.Creature("creature-01");

        bestiary.MarkSeen(drowned.Id);
        bestiary.AddStudyPoint(drowned.Id);
        bestiary.AddStudyPoint(drowned.Id);
        bestiary.SeeTrigger(drowned.Id, TriggerKind.Heat);
        Assert.That(bestiary.LevelOf(drowned, Thresholds), Is.EqualTo(BestiaryLevel.Seen), "two study points of three");

        bestiary.AddStudyPoint(drowned.Id);
        Assert.That(bestiary.LevelOf(drowned, Thresholds), Is.EqualTo(BestiaryLevel.Understood),
            "studied, and its only trigger has been seen");

        var ash = GameContent.Default.Creature("creature-05");
        bestiary.MarkSeen(ash.Id);
        for (var i = 0; i < 3; i++)
        {
            bestiary.AddStudyPoint(ash.Id);
        }

        bestiary.SeeTrigger(ash.Id, TriggerKind.Heat);
        Assert.That(bestiary.LevelOf(ash, Thresholds), Is.EqualTo(BestiaryLevel.Studied), "one of two triggers seen");
        bestiary.AddBanishPoint(ash.Id);
        Assert.That(bestiary.LevelOf(ash, Thresholds), Is.EqualTo(BestiaryLevel.Understood), "a banishing counts too");
    }

    [Test]
    [Property("Requirement", "BST-02")]
    [Property("Requirement", "BST-04")]
    public void ServedCreaturesThatStayCalmAreStudiedAndTheKnowledgeOutlivesTheRun()
    {
        var s = new Scenario(balance: b => b with { StartingSupplies = new Dictionary<Supply, int> { [Supply.Drink] = 100 } });
        s.Place("bar-counter", new Tile(11, 4));
        s.PlaceStaff("staff-01", new Tile(12, 5));
        var drowned = s.Content.Creature("creature-01");

        for (var cycle = 0; cycle < 3; cycle++)
        {
            s.Begin();
            s.Spawn("creature-01", new Tile(11, 5));
            s.Session.RunMistToDawn();
            Assert.That(s.Session.Execute(new ContinueFromDawn()).Succeeded, Is.True);
        }

        Assert.That(s.Session.Bestiary.Entry(drowned, s.Content.Balance.Bestiary).StudyPoints, Is.EqualTo(3));
        Assert.That(s.Session.Bestiary.LevelOf(drowned, s.Content.Balance.Bestiary), Is.EqualTo(BestiaryLevel.Studied));

        var nextRun = new GameSession(new SeededRandom(99), content: s.Content, bestiary: s.Session.Bestiary);
        Assert.That(nextRun.Bestiary.LevelOf(drowned, s.Content.Balance.Bestiary), Is.EqualTo(BestiaryLevel.Studied));
    }

    [Test]
    [Property("Requirement", "BST-03")]
    public void TheForecastShowsOnlyWhatIsKnown()
    {
        var bestiary = new CreatureBestiary();
        var session = new GameSession(new SeededRandom(4), bestiary: bestiary);
        var plane = session.ConvergingPlanes.Single();
        var known = plane.Creatures[0].Creature;
        bestiary.MarkSeen(known);
        for (var i = 0; i < 3; i++)
        {
            bestiary.AddStudyPoint(known);
        }

        var forecast = session.Forecast.Single();

        Assert.That(forecast.Plane, Is.EqualTo(plane));
        Assert.That(forecast.Creatures.Sum(c => c.Share), Is.EqualTo(1).Within(1e-9));
        var studied = forecast.Creatures.Single(c => c.CreatureDefId == known);
        Assert.That(studied.Level, Is.EqualTo(BestiaryLevel.Studied));
        Assert.That(studied.Name, Is.Not.Null);
        Assert.That(studied.Appetites, Is.Not.Null);
        Assert.That(studied.Triggers, Is.Null, "triggers need Understood");

        var unknown = forecast.Creatures.Single(c => c.CreatureDefId != known);
        Assert.That(unknown.Level, Is.EqualTo(BestiaryLevel.Unknown));
        Assert.That(unknown.Name, Is.Null);
        Assert.That(unknown.Appetites, Is.Null);
    }

    [Test]
    [Property("Requirement", "INV-02")]
    [Property("Requirement", "TST-04")]
    public void AServedInnReplaysIdentically()
    {
        static ulong Play()
        {
            var s = new Scenario(balance: b => b with { BaseArrivalIntervalSeconds = 4 }, seed: 17);
            s.Place("bar-counter", new Tile(11, 4));
            s.Place("hearth", new Tile(12, 7));
            s.PlaceStaff("staff-01", new Tile(12, 5));
            s.PlaceStaff("staff-09", new Tile(16, 9));
            s.Begin();
            s.Spawn("creature-04", new Tile(14, 10));
            s.Session.RunMistToDawn();
            return s.Session.ComputeStateHash();
        }

        Assert.That(Play(), Is.EqualTo(Play()));
    }
}
