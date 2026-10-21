using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Run;
using static MistyInn.Core.Tests.Content.ContentTestKit;

namespace MistyInn.Core.Tests.Simulation;

/// <summary>Satisfied guests pay and leave (CRE-05, CRE-08, SDD v1.11, step 6c).</summary>
public class SatisfiedGuestsTests
{
    // Creature 03 craves Warmth and arrives at 20 agitation; a hearth at (12,7) warms its seats at 2.0 per second, so it
    // reaches 0 in about 10 s. Its tribute is 3.
    private static (Scenario S, CreatureId Id) WarmGuest(Action<GameSession>? beforeBegin = null)
    {
        var s = new Scenario();
        s.Place("hearth", new Tile(12, 7));
        beforeBegin?.Invoke(s.Session);
        s.Begin();
        return (s, s.Spawn("creature-03", new Tile(12, 10)));
    }

    [Test]
    [Property("Requirement", "CRE-08")]
    [Property("Requirement", "INV-12")]
    public void AGuestAtZeroForFiveSecondsPaysAndWalksOut()
    {
        var (s, id) = WarmGuest();
        var silver = s.Session.Silver;

        s.RunUntilCollecting(() => s.Creature(id).IsLeaving, 60);

        Assert.That(s.Session.Silver, Is.EqualTo(silver + 3), "its tribute, at once");
        Assert.That(s.Events.OfType<CreatureSatisfied>().Single().Silver, Is.EqualTo(3));
        Assert.That(s.Session.CycleLogEntries.Any(e => e.Kind == "creature.satisfied" && e.Subject == id.ToRef()), Is.True);
        s.Step();
        Assert.That(s.Session.ServiceRates.ContainsKey(id), Is.False, "nobody serves a leaving guest");

        s.RunUntilCollecting(() => s.IsGone(id), 30);

        Assert.That(s.Events.OfType<CreatureDissolved>().Single().Creature, Is.EqualTo(id));
        Assert.That(s.Session.CycleLogEntries.Any(e => e.Kind == "creature.left"), Is.True);
    }

    [Test]
    [Property("Requirement", "CRE-08")]
    public void AnyRiseRestartsTheCount()
    {
        var (s, id) = WarmGuest();
        s.RunUntil(() => s.Creature(id).CalmSince is not null, 60);
        s.Run(3);

        s.Creature(id).Agitation = 10;
        s.Step();
        Assert.That(s.Creature(id).CalmSince, Is.Null);

        s.RunUntil(() => s.Creature(id).CalmSince is not null, 30);
        var calmAgain = s.Session.Time.TotalSeconds;
        s.RunUntil(() => s.Creature(id).IsLeaving, 30);

        Assert.That(s.Session.Time.TotalSeconds - calmAgain, Is.GreaterThanOrEqualTo(5 - 0.05), "a full 5 s after the rise");
    }

    [Test]
    [Property("Requirement", "CRE-08")]
    [Property("Requirement", "CRE-05")]
    [Property("Requirement", "BST-02")]
    public void ASatisfiedGuestCountsForTheBestiaryAndPaysNothingAtDawn()
    {
        var (s, id) = WarmGuest();
        var def = s.Content.Creature("creature-03");

        s.RunUntilCollecting(() => s.IsGone(id), 90);
        Assert.That(s.Session.Bestiary.Entry(def, s.Content.Balance.Bestiary).StudyPoints, Is.EqualTo(1), "served, then satisfied");

        s.Session.RunMistToDawn();
        s.Collect();

        Assert.That(s.Events.OfType<TributeLeft>().Count(t => t.Creature == id), Is.EqualTo(1), "paid once, when it left");
        Assert.That(s.Session.Bestiary.Entry(def, s.Content.Balance.Bestiary).StudyPoints, Is.EqualTo(1), "once per cycle");
    }

    [Test]
    [Property("Requirement", "CRE-08")]
    [Property("Requirement", "ITM-06")]
    public void ASatisfiedGuestPaysTheSameBonusesAsAtDawn()
    {
        var (s, id) = WarmGuest(session =>
        {
            session.SetShopForTests(session.RunForTests.Shop.Staff.ToList(), [new ItemOffer("innkeepers-ledger", 0)]);
            Assert.That(session.Execute(new BuyOffer(ShopSection.Items, 0)).Succeeded, Is.True);
        });

        s.RunUntilCollecting(() => s.Creature(id).IsLeaving, 60);

        Assert.That(s.Events.OfType<CreatureSatisfied>().Single().Silver, Is.EqualTo(3 + 1), "the Innkeeper's Ledger adds 1");
    }

    [Test]
    [Property("Requirement", "CRE-08")]
    public void TheSatisfactionTimeIsTuning()
    {
        Assert.That(GameContent.Default.Balance.SatisfiedAfterSeconds, Is.EqualTo(5));
        AssertRejected(Set with { Balance = Set.Balance with { SatisfiedAfterSeconds = 0 } }, "out of range");
    }
}
