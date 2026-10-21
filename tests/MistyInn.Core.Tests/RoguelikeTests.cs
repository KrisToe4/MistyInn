using MistyInn.Core.Bestiary;
using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Content;
using MistyInn.Core.Events;
using MistyInn.Core.Meta;
using MistyInn.Core.Randomness;
using MistyInn.Core.Run;
using static MistyInn.Core.Tests.TestGame;

namespace MistyInn.Core.Tests;

/// <summary>Run length and the Convergence, omens, Mist Essence and unlocks (sections 6.1 and 14, TST-09, step 7).</summary>
public class RoguelikeTests
{
    private static readonly GameContent Real = GameContent.Default;

    /// <summary>The sturdy test content with the real omens and unlocks back in.</summary>
    private static GameContent Roguelike(Func<BalanceDef, BalanceDef>? balance = null, IReadOnlyList<OmenDef>? omens = null) =>
        ContentWith(balance: balance, set: s => s with { Omens = omens ?? Real.Omens, Unlocks = Real.Unlocks });

    /// <summary>Content whose every cycle has <paramref name="omen"/> (OMN-01 tuned to always).</summary>
    private static GameContent Always(string omen, Func<BalanceDef, BalanceDef>? balance = null) =>
        Roguelike(b => (balance?.Invoke(b) ?? b) with { Omens = new OmenBalanceDef(FromCycle: 1, Chance: 1, ConvergenceChance: 1) },
            [Real.Omen(omen)]);

    private static GameSession Session(GameContent content, ulong seed = 1, MetaProfile? profile = null, IReadOnlyList<string>? staff = null) =>
        new(new SeededRandom(seed), content: content, profile: profile, startingStaff: staff);

    [Test]
    [Property("Requirement", "RUN-02")]
    [Property("Requirement", "RUN-04")]
    [Property("Requirement", "INV-12")]
    public void SurvivingTheConvergenceWinsTheRun()
    {
        var profile = new MetaProfile();
        var session = Session(ContentWith(balance: b => b with { RunLength = 3 }), profile: profile);
        PlayCycle(session);
        PlayCycle(session);

        Assert.That(session.Cycle, Is.EqualTo(3));
        Assert.That(session.IsConvergence, Is.True);
        Assert.That(session.ConvergingPlanes.Select(p => p.Id), Is.EquivalentTo(Real.UnlockedPlanes), "every unlocked plane opens");

        StartMist(session);
        session.RunMistToDawn();

        Assert.That(session.Phase, Is.EqualTo(Phase.RunOver));
        Assert.That(session.EndReason, Is.EqualTo(RunEndReason.Survived));
        var summary = session.Summary!;
        Assert.That(summary.Won, Is.True);
        Assert.That(summary.CyclesSurvived, Is.EqualTo(3));
        Assert.That(summary.EssenceEarned, Is.EqualTo(3 * 2 + summary.CreaturesOccupied / 5 + 10));
        Assert.That(session.DrainEvents().OfType<RunEnded>().Single().Summary, Is.EqualTo(summary));
        Assert.That(session.CycleLogEntries.Any(e => e.Kind == "run.won" && e.Cause == "convergence.survived"), Is.True);
        Assert.That(session.CycleLogEntries.Any(e => e.Kind == "convergence"), Is.True);
        Assert.That(session.Execute(new ContinueFromDawn()).Failure, Is.EqualTo(CommandFailure.WrongPhase), "there is no cycle 4");

        Assert.That(profile.Essence, Is.EqualTo(summary.EssenceEarned), "the Essence goes to the profile");
        Assert.That(profile.Statistics, Is.EqualTo(new RunStatistics(1, 1, 3, summary.CreaturesOccupied, summary.EssenceEarned)));
    }

    [Test]
    [Property("Requirement", "RUN-02")]
    public void TheDefaultRunIsTenCyclesAndConvergenceArrivalsAreFaster()
    {
        Assert.That(Real.Balance.RunLength, Is.EqualTo(10));
        var session = Session(ContentWith(balance: b => b with { RunLength = 2 }));
        Assert.That(session.ArrivalsForTests.IntervalSeconds, Is.EqualTo(9).Within(1e-9));

        PlayCycle(session);

        Assert.That(session.IsConvergence, Is.True);
        Assert.That(session.ArrivalsForTests.IntervalSeconds, Is.EqualTo(9 * 0.92 * 0.6).Within(1e-9));
    }

    [Test]
    [Property("Requirement", "MET-02")]
    [Property("Requirement", "TST-09")]
    public void TheEssenceFormula()
    {
        var formula = Real.Balance.Essence;
        Assert.That(formula, Is.EqualTo(new EssenceDef(PerCycleSurvived: 2, OccupiedPerPoint: 5, WinBonus: 10)));

        Assert.That(MetaProfile.EssenceFor(formula, cyclesSurvived: 0, creaturesOccupied: 4, won: false), Is.EqualTo(0));
        Assert.That(MetaProfile.EssenceFor(formula, cyclesSurvived: 3, creaturesOccupied: 12, won: false), Is.EqualTo(6 + 2));
        Assert.That(MetaProfile.EssenceFor(formula, cyclesSurvived: 10, creaturesOccupied: 24, won: true), Is.EqualTo(20 + 4 + 10));
    }

    [Test]
    [Property("Requirement", "MET-02")]
    [Property("Requirement", "RUN-03")]
    [Property("Requirement", "RUN-04")]
    public void TheCycleTheInnFellInDoesNotCountAsSurvived()
    {
        var profile = new MetaProfile(essence: 4);
        var session = Session(ContentWith(), profile: profile);
        PlayCycle(session);
        StartMist(session);
        session.RunForTests.Integrity = 0;
        session.RunMistToDawn();

        Assert.That(session.EndReason, Is.EqualTo(RunEndReason.InnDestroyed));
        var summary = session.Summary!;
        Assert.That(summary.Won, Is.False);
        Assert.That(summary.CyclesSurvived, Is.EqualTo(1), "cycle 2 was lost");
        Assert.That(summary.EssenceEarned, Is.EqualTo(2 + summary.CreaturesOccupied / 5));
        Assert.That(profile.Essence, Is.EqualTo(4 + summary.EssenceEarned));
        Assert.That(profile.Statistics.RunsPlayed, Is.EqualTo(1));
        Assert.That(profile.Statistics.RunsWon, Is.EqualTo(0));
    }

    [Test]
    [Property("Requirement", "MET-02")]
    [Property("Requirement", "CRE-08")]
    public void SatisfiedGuestsAndGuestsCalmAtDawnAreKeptOccupied()
    {
        var s = new Scenario(balance: b => b with { RunLength = 1 });
        s.Place("hearth", new Tile(12, 7));
        s.Begin();
        var warm = s.Spawn("creature-03", new Tile(12, 10));
        s.RunUntil(() => s.IsGone(warm), 90);
        Assert.That(s.Session.RunForTests.CreaturesOccupied, Is.EqualTo(1), "satisfied and gone");

        s.RunUntil(() => s.Session.Time.TotalSeconds >= 112, 120);
        var late = s.Spawn("creature-01", new Tile(11, 5));
        s.Creature(late).Agitation = 0;
        s.Session.RunMistToDawn();

        var summary = s.Session.Summary!;
        Assert.That(summary.CreaturesOccupied, Is.EqualTo(2), "one satisfied, one calm at dawn, each counted once");
        Assert.That(summary.EssenceEarned, Is.EqualTo(1 * 2 + 2 / 5 + 10));
    }

    [Test]
    [Property("Requirement", "OMN-01")]
    [Property("Requirement", "TST-09")]
    public void OmensComeFromTheSecondCycleSixtyPercentOfTheTime()
    {
        var session = Session(Roguelike());
        Assert.That(session.Omen, Is.Null, "never on cycle 1");

        const int draws = 3000;
        var counts = new Dictionary<string, int>();
        var none = 0;
        for (var i = 0; i < draws; i++)
        {
            session.DrawOmenForTests(2);
            if (session.Omen is { } omen)
            {
                counts[omen.Def.Id] = counts.GetValueOrDefault(omen.Def.Id) + 1;
            }
            else
            {
                none++;
            }
        }

        Assert.That(1 - none / (double)draws, Is.EqualTo(0.6).Within(0.03));
        foreach (var id in new[] { "blood-moon", "surge", "feast-night" })
        {
            Assert.That(counts[id] / (double)(draws - none), Is.EqualTo(1 / 3.0).Within(0.04), id);
        }

        for (var i = 0; i < 50; i++)
        {
            session.DrawOmenForTests(1);
            Assert.That(session.Omen, Is.Null);
            session.DrawOmenForTests(session.RunLength);
            Assert.That(session.Omen, Is.Not.Null, "always on the Convergence");
        }
    }

    [Test]
    [Property("Requirement", "OMN-01")]
    [Property("Requirement", "INV-02")]
    public void OmensDrawFromTheirOwnStream()
    {
        var session = Session(Roguelike());
        var before = session.Random.GetState();

        for (var i = 0; i < 20; i++)
        {
            session.DrawOmenForTests(2);
        }

        Assert.That(session.Random.GetState(), Is.EqualTo(before), "the planes, arrivals and Dawn rolls are untouched (D-088)");
    }

    [Test]
    [Property("Requirement", "OMN-02")]
    [Property("Requirement", "OMN-03")]
    [Property("Requirement", "INV-12")]
    public void ABloodMoonBringsEveryCreatureInAtPlusTwenty()
    {
        double FirstArrivalAgitation(GameContent content)
        {
            var session = Session(content, seed: 5);
            StartMist(session);
            while (true)
            {
                session.Tick(TimeSpan.FromSeconds(0.1));
                if (session.DrainEvents().OfType<CreatureArrived>().FirstOrDefault() is { } arrived)
                {
                    return session.CreatureForTests(arrived.Creature).Agitation;
                }
            }
        }

        var calm = FirstArrivalAgitation(Roguelike(omens: []));
        var moon = Always("blood-moon");
        var red = FirstArrivalAgitation(moon);

        Assert.That(red - calm, Is.EqualTo(20).Within(0.5));

        var logged = Session(moon);
        StartMist(logged);
        Assert.That(logged.CycleLogEntries.Any(e => e.Kind == "omen" && e.Cause == "omen.blood-moon"), Is.True);
    }

    [Test]
    [Property("Requirement", "OMN-02")]
    [Property("Requirement", "OMN-03")]
    public void AFeastNightBringsMoreGuestsWhoPayHalfAgain()
    {
        var s = new Scenario(set: c => c with { Omens = [Real.Omen("feast-night")] },
            balance: b => b with { Omens = new OmenBalanceDef(FromCycle: 1, Chance: 1, ConvergenceChance: 1) });
        Assert.That(s.Session.Omen!.Def.Id, Is.EqualTo("feast-night"));
        s.Place("hearth", new Tile(12, 7));
        s.Begin();
        var guest = s.Spawn("creature-03", new Tile(12, 10));

        s.RunUntilCollecting(() => s.Creature(guest).IsLeaving, 60);

        Assert.That(s.Events.OfType<CreatureSatisfied>().Single().Silver, Is.EqualTo(5), "3 × 1.5, rounded");

        var plain = Session(ContentWith());
        var feast = Session(Always("feast-night"));
        Assert.That(feast.ArrivalsForTests.IntervalSeconds, Is.EqualTo(plain.ArrivalsForTests.IntervalSeconds / 1.3).Within(1e-9));
    }

    [Test]
    [Property("Requirement", "OMN-02")]
    [Property("Requirement", "OMN-03")]
    public void ASurgeDoublesOneConvergingPlane()
    {
        var session = Session(Always("surge", b => b with { PlanesPerCycle = [2] }), seed: 3);
        var omen = session.Omen!;
        var planes = session.ConvergingPlanes.Select(p => p.Id).ToList();

        Assert.That(planes, Has.Count.EqualTo(2));
        Assert.That(planes, Does.Contain(omen.SurgePlane));
        Assert.That(omen.Effects.Single(), Is.EqualTo(new EffectDef(EffectKind.PlaneWeight, Multiplier: 2, Plane: omen.SurgePlane)));
        Assert.That(session.ModifiersForTests.PlaneWeight(omen.SurgePlane!), Is.EqualTo(2));
        Assert.That(session.ModifiersForTests.PlaneWeight(planes.Single(p => p != omen.SurgePlane)), Is.EqualTo(1));

        // The other plane sends as many as before; the surging one twice as many.
        Assert.That(session.ArrivalsForTests.IntervalSeconds, Is.EqualTo(9 / 1.5).Within(1e-9));
        const int picks = 3000;
        var surging = Enumerable.Range(0, picks).Count(_ => session.ArrivalsForTests.PickPlane().Id == omen.SurgePlane);
        Assert.That(surging / (double)picks, Is.EqualTo(2 / 3.0).Within(0.03));
    }

    [Test]
    [Property("Requirement", "OMN-03")]
    public void AnOmenLastsOneCycle()
    {
        var session = Session(Always("feast-night"));
        Assert.That(session.ModifiersForTests.InnWideMultiplier(EffectKind.Tribute), Is.EqualTo(1.5));

        PlayCycle(session);

        Assert.That(session.Omen!.Effects, Has.Count.EqualTo(2), "drawn anew, not added to the last one");
        Assert.That(session.ModifiersForTests.InnWideMultiplier(EffectKind.Tribute), Is.EqualTo(1.5));

        var never = Session(Roguelike(b => b with { Omens = new OmenBalanceDef(FromCycle: 1, Chance: 0, ConvergenceChance: 0) }));
        PlayCycle(never);
        Assert.That(never.Omen, Is.Null);
        Assert.That(never.ModifiersForTests.InnWideMultiplier(EffectKind.Tribute), Is.EqualTo(1));
    }

    [Test]
    [Property("Requirement", "INV-02")]
    [Property("Requirement", "TST-09")]
    [Property("Requirement", "OMN-01")]
    public void ASeededRunReplaysIdentically()
    {
        static List<string> Play(ulong seed)
        {
            var trace = new List<string>();
            var session = Session(Roguelike(), seed);
            for (var cycle = 1; cycle <= 3; cycle++)
            {
                trace.Add($"{cycle}: planes {string.Join(",", session.ConvergingPlanes.Select(p => p.Id))}"
                          + $" omen {session.Omen?.Def.Id}/{session.Omen?.SurgePlane}"
                          + $" shop {string.Join(",", session.Shop.Staff.Select(o => $"{o?.Offer.CharacterId}*{o?.Offer.Rank}"))}"
                          + $" {string.Join(",", session.Shop.Items.Select(o => o?.Offer.ItemId))}"
                          + $" hash {session.ComputeStateHash()}");
                StartMist(session);
                session.RunMistToDawn();
                trace.Add($"{cycle} dawn: hash {session.ComputeStateHash()} log {session.CycleLogEntries.Count}");
                Assert.That(session.Execute(new ContinueFromDawn()).Succeeded, Is.True);
            }

            return trace;
        }

        Assert.That(Play(42), Is.EqualTo(Play(42)));
        Assert.That(Play(42), Is.Not.EqualTo(Play(43)));
    }

    [Test]
    [Property("Requirement", "MET-03")]
    [Property("Requirement", "PLN-02")]
    [Property("Requirement", "TST-09")]
    public void LockedContentStaysOutOfTheRun()
    {
        var content = Roguelike();
        var session = Session(content);
        session.RunForTests.Silver = 1000;
        var lockedStaff = new[] { "staff-04", "staff-08", "staff-12", "staff-15" };
        var lockedItems = new[] { "ward-charm", "ember-in-a-jar", "cursed-crown" };

        Assert.That(session.RunForTests.UnlockedPlanes, Is.EqualTo(new[] { "drowned-deep", "ashen-waste", "hollow-wood" }));
        Assert.That(session.BuildMenu.Select(f => f.Id), Is.EquivalentTo(content.Fixtures.Select(f => f.Id)
            .Except(["ledger-desk", "ward-circle", "salt-line"])));
        Assert.That(session.Execute(new BuildFixture("ledger-desk")).Failure, Is.EqualTo(CommandFailure.Locked));
        Assert.That(lockedStaff.Select(content.Character).Any(c => session.ShopForTests.CanBeDrawn(c, new HashSet<string>())), Is.False);
        for (var i = 0; i < 100; i++)
        {
            Assert.That(session.Shop.Staff.Select(o => o?.Offer.CharacterId).Intersect(lockedStaff), Is.Empty);
            Assert.That(session.Shop.Items.Select(o => o?.Offer.ItemId).Intersect(lockedItems), Is.Empty);
            session.RunForTests.Shop.RerollsThisCycle = 0;
            Assert.That(session.Execute(new Reroll()).Succeeded, Is.True);
        }
    }

    [Test]
    [Property("Requirement", "MET-03")]
    [Property("Requirement", "MET-01")]
    [Property("Requirement", "PLN-02")]
    public void EssenceBuysUnlocksForTheNextRun()
    {
        var content = Roguelike();
        var profile = new MetaProfile(essence: 20);

        Assert.That(profile.Buy(content, "nothing"), Is.EqualTo(UnlockFailure.UnknownUnlock));
        Assert.That(profile.Buy(content, "unlock-pale-court"), Is.EqualTo(UnlockFailure.None));
        Assert.That(profile.Essence, Is.EqualTo(5));
        Assert.That(profile.Buy(content, "unlock-pale-court"), Is.EqualTo(UnlockFailure.AlreadyOwned));
        Assert.That(profile.Buy(content, "unlock-staff-15"), Is.EqualTo(UnlockFailure.NotEnoughEssence));
        Assert.That(profile.Essence, Is.EqualTo(5), "a refused purchase changes nothing");
        Assert.That(profile.Unlocked, Is.EqualTo(new[] { "unlock-pale-court" }));

        var next = Session(content, profile: profile);
        Assert.That(next.Profile, Is.SameAs(profile));
        Assert.That(next.RunForTests.UnlockedPlanes, Is.EqualTo(new[] { "drowned-deep", "ashen-waste", "hollow-wood", "pale-court" }));

        profile = new MetaProfile(unlocked: ["unlock-staff-15", "unlock-salt-line", "unlock-cursed-crown"]);
        var unlocked = Session(content, profile: profile);
        Assert.That(unlocked.ShopForTests.CanBeDrawn(content.Character("staff-15"), new HashSet<string>()), Is.True);
        Assert.That(unlocked.BuildMenu.Select(f => f.Id), Does.Contain("salt-line"));
        Assert.That(unlocked.RunForTests.Start.LockedItems, Does.Not.Contain("cursed-crown"));
    }

    [Test]
    [Property("Requirement", "MET-03")]
    [Property("Requirement", "RUN-01")]
    public void TheStartingStaffCanBeChosenOnceUnlocked()
    {
        var content = Roguelike();
        var fresh = new MetaProfile();
        Assert.That(fresh.CanChooseStartingStaff(content), Is.False);
        Assert.That(fresh.CheckStartingStaff(content, content.Balance.StartingStaff), Is.Null, "the default is always allowed");
        Assert.That(() => Session(content, profile: fresh, staff: ["staff-02", "staff-05"]), Throws.ArgumentException);

        var chooser = new MetaProfile(unlocked: ["unlock-starting-staff"]);
        Assert.That(chooser.CanChooseStartingStaff(content), Is.True);
        Assert.That(chooser.CheckStartingStaff(content, ["staff-02"]), Is.Not.Null, "two, like the default");
        Assert.That(chooser.CheckStartingStaff(content, ["staff-02", "staff-02"]), Is.Not.Null);
        Assert.That(chooser.CheckStartingStaff(content, ["staff-02", "staff-15"]), Is.Not.Null, "staff-15 is locked");
        Assert.That(chooser.AvailableCharacters(content).Select(c => c.Id), Has.None.EqualTo("staff-15"));

        var session = Session(content, profile: chooser, staff: ["staff-02", "staff-05"]);
        var staff = Staff(session);
        Assert.That(staff.Select(u => u.StaffId), Is.EqualTo(new[] { "staff-02", "staff-05" }));
        Assert.That(staff.All(u => u.Rank == 1), Is.True);
    }

    [Test]
    [Property("Requirement", "BST-01")]
    [Property("Requirement", "BST-04")]
    [Property("Requirement", "UI-01")]
    public void TheBestiaryPageShowsOnlyWhatIsRevealed()
    {
        var bestiary = new CreatureBestiary();
        var creature = Real.Creature("creature-03");

        var unknown = BestiaryBook.Entries(Real, bestiary).Single(e => e.CreatureId == creature.Id);
        Assert.That(unknown, Is.EqualTo(new BestiaryPageEntry(creature.Id, BestiaryLevel.Unknown, null, null, null, null, null, null, null)));
        Assert.That(BestiaryBook.Entries(Real, bestiary), Has.Count.EqualTo(Real.Creatures.Count));

        bestiary.MarkSeen(creature.Id);
        var seen = BestiaryBook.Entries(Real, bestiary).Single(e => e.CreatureId == creature.Id);
        Assert.That(seen.Name, Is.EqualTo(creature.Name));
        Assert.That(seen.PlaneName, Is.EqualTo("The Ashen Waste"));
        Assert.That(seen.Size, Is.EqualTo(creature.Size));
        Assert.That(seen.Appetites, Is.Null);

        for (var i = 0; i < Real.Balance.Bestiary.StudiedPoints; i++)
        {
            bestiary.AddStudyPoint(creature.Id);
        }

        var studied = BestiaryBook.Entries(Real, bestiary).Single(e => e.CreatureId == creature.Id);
        Assert.That(studied.Appetites, Is.EqualTo(creature.Appetites));
        Assert.That(studied.Triggers, Is.Null);
        Assert.That(studied.Combat, Is.Null);

        foreach (var trigger in creature.Triggers)
        {
            bestiary.SeeTrigger(creature.Id, trigger.Trigger);
        }

        var understood = BestiaryBook.Entries(Real, bestiary).Single(e => e.CreatureId == creature.Id);
        Assert.That(understood.Level, Is.EqualTo(BestiaryLevel.Understood));
        Assert.That(understood.Triggers, Is.EqualTo(creature.Triggers));
        Assert.That(understood.Combat, Is.EqualTo(creature.Combat));
        Assert.That(understood.Escalation, Is.EqualTo(creature.Escalation));

        var progress = BestiaryBook.Progress(Real, bestiary);
        Assert.That(progress[BestiaryLevel.Understood], Is.EqualTo(1));
        Assert.That(progress[BestiaryLevel.Unknown], Is.EqualTo(Real.Creatures.Count - 1));
    }
}
