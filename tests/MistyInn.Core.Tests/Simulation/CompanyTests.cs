using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Content;
using MistyInn.Core.Events;
using MistyInn.Core.Run;
using MistyInn.Core.Tests.Content;

namespace MistyInn.Core.Tests.Simulation;

/// <summary>Company on the floor and in the back rooms, refusals, interruptions and hosting (section 11.5, TST-10).</summary>
public class CompanyTests
{
    // Staff 03 knows Company (soothe 1.1, Company 1.2, capacity 1, range 1.5), prefers Creature 09 and refuses
    // Creature 10. Room 1's door is at (9,9); the companion stands two tiles from it.
    private static readonly Tile Post = new(11, 9);
    private static readonly Tile Beside = new(12, 9);

    private static Scenario CompanyScenario(Func<ContentSet, ContentSet>? set = null, ContentFilters? filters = null)
    {
        var s = new Scenario(startingStaff: ["staff-03"], set: set);
        if (filters is not null)
        {
            Assert.That(s.Session.Execute(new SetContentFilters(filters)).Succeeded, Is.True);
        }

        s.PlaceStaff("staff-03", Post);
        s.Begin();
        return s;
    }

    private static void ToTheRoom(Scenario s) => s.RunUntilCollecting(() => s.Session.Rooms[0].InSession, 20);

    [Test]
    [Property("Requirement", "CMP-02")]
    [Property("Requirement", "RAT-07")]
    [Property("Requirement", "TST-10")]
    public void ARefusalIsAlwaysHonoured()
    {
        var s = CompanyScenario();
        var refused = s.Spawn("creature-10", Beside);

        for (var i = 0; i < 20 * GameSession.StepsPerSecond; i++)
        {
            s.Step();
            Assert.That(s.Unit("staff-03").Session?.Targets ?? [], Does.Not.Contain(refused));
            Assert.That(s.Creature(refused).ServedBy, Is.Not.EqualTo("staff-03:company"));
        }
    }

    [Test]
    [Property("Requirement", "CMP-02")]
    public void APreferredGuestIsSoothedOneAndAHalfTimesAsMuch()
    {
        static double Rate(Func<ContentSet, ContentSet>? set)
        {
            var s = CompanyScenario(set);
            var guest = s.Creature(s.Spawn("creature-09", Beside));
            return s.Session.ServiceForTests.SootheRate(s.Unit("staff-03"), s.Content.Technique("company"), guest);
        }

        var preferred = Rate(null);
        var plain = Rate(c => c with
        {
            Staff = c.Staff with { Characters = c.Staff.Characters.Select(x => x.Id == "staff-03" ? x with { Prefers = [] } : x).ToList() },
        });

        Assert.That(preferred, Is.EqualTo(plain * 1.5).Within(1e-9));
    }

    [Test]
    [Property("Requirement", "CMP-03")]
    [Property("Requirement", "CMP-02")]
    public void FloorCompanyIsForOneSapientGuestAtATime()
    {
        var s = CompanyScenario();
        s.Spawn("creature-09", Beside);
        s.Spawn("creature-09", new Tile(11, 10));
        s.Spawn("creature-01", new Tile(10, 9));

        s.RunUntil(() => s.Unit("staff-03").Session is not null, 5);

        var session = s.Unit("staff-03").Session!;
        Assert.That(session.Technique.Id, Is.EqualTo("company"));
        Assert.That(session.Targets, Has.Count.EqualTo(1));
        Assert.That(s.Session.Creatures.Single(c => c.Id == session.Targets[0]).CreatureDefId, Is.EqualTo("creature-09"));
    }

    [Test]
    [Property("Requirement", "CMP-04")]
    [Property("Requirement", "RAT-02")]
    [Property("Requirement", "INV-12")]
    [Property("Requirement", "TST-10")]
    public void AfterEightSecondsTheyGoToARoomAndTheGuestComesOutCalm()
    {
        var s = CompanyScenario();
        var guest = s.Spawn("creature-09", Beside);
        s.RunUntil(() => s.Unit("staff-03").Session is not null, 5);
        var floorStart = s.Session.Time;

        s.RunUntilCollecting(() => s.Session.Rooms[0].Companion is not null, 15);
        Assert.That((s.Session.Time - floorStart).TotalSeconds, Is.GreaterThanOrEqualTo(8 - 0.05));

        ToTheRoom(s);
        var entered = s.Session.Time;
        var companion = s.Unit("staff-03");
        var started = s.Events.OfType<RoomSessionStarted>().Single();
        Assert.That(started.Room, Is.EqualTo("room-1"));
        Assert.That(started.Guest, Is.EqualTo(guest));
        Assert.That(companion.Activity, Is.EqualTo(UnitActivity.InRoom));
        Assert.That(companion.IsAvailable, Is.False, "out of play");
        Assert.That(s.Session.Creatures.Single(c => c.Id == guest).IsVisible, Is.False, "off-screen");
        Assert.That(s.Session.RunForTests.Board.IsWalkable(new Tile(9, 9)), Is.False, "the door is closed");
        Assert.That(s.Session.CycleLogEntries.Single(e => e.Kind == "company.room").Flavour, Is.EqualTo("company.room"));

        s.RunUntilCollecting(() => !s.Session.Rooms[0].InSession, 30);
        Assert.That((s.Session.Time - entered).TotalSeconds, Is.EqualTo(25).Within(0.1));
        Assert.That(s.Events.OfType<RoomSessionEnded>().Single().Interrupted, Is.False);
        Assert.That(s.Creature(guest).State, Is.EqualTo(CreatureState.Calm));
        Assert.That(s.Creature(guest).HadRoom, Is.True);
        Assert.That(companion.Activity, Is.EqualTo(UnitActivity.Resting));

        s.Run(9);
        Assert.That(companion.Activity, Is.EqualTo(UnitActivity.Resting), "a 10 s rest");
        s.RunUntil(() => companion.Activity is UnitActivity.AtPost or UnitActivity.Serving, 10);
    }

    [Test]
    [Property("Requirement", "CMP-05")]
    public void ViolenceCloseByEndsFloorCompany()
    {
        var s = CompanyScenario();
        var guest = s.Spawn("creature-09", Beside);
        s.RunUntil(() => s.Unit("staff-03").Session is not null, 5);

        s.Session.RunForTests.Attacks.Add((new TilePosition(14, 9), s.Session.Time.TotalSeconds));
        s.Step();

        Assert.That(s.Unit("staff-03").Session, Is.Null);
        var entry = s.Session.CycleLogEntries.Single(e => e.Kind == "company.interrupted");
        Assert.That(entry.Cause, Is.EqualTo("violence"));
        Assert.That(entry.Subject, Is.EqualTo(guest.ToRef()));
    }

    [Test]
    [Property("Requirement", "CMP-05")]
    [Property("Requirement", "INV-06")]
    [Property("Requirement", "RAT-04")]
    [Property("Requirement", "TST-10")]
    public void AnAttackOnTheDoorEndsTheSessionBeforeTheBlowLands()
    {
        var s = CompanyScenario();
        var guest = s.Spawn("creature-09", Beside);
        ToTheRoom(s);
        s.Events.Clear();

        s.SpawnTurned("creature-01", new Tile(10, 10));
        s.Collect();

        var ended = s.Events.FindIndex(e => e is RoomSessionEnded { Interrupted: true });
        var blow = s.Events.FindIndex(e => e is AttackLanded { Target: null });
        Assert.That(ended, Is.GreaterThanOrEqualTo(0), "the session ended");
        Assert.That(blow, Is.GreaterThan(ended), "before the door was hit");

        var log = s.Session.CycleLogEntries.ToList();
        var interrupted = log.FindIndex(e => e.Kind == "company.interrupted" && e.Cause == "door.attacked");
        Assert.That(interrupted, Is.GreaterThanOrEqualTo(0));
        Assert.That(log.FindIndex(e => e.Kind == "combat.attack"), Is.GreaterThan(interrupted));

        Assert.That(s.Creature(guest).IsInRoom, Is.False, "both came out");
        Assert.That(s.Unit("staff-03").Activity, Is.EqualTo(UnitActivity.Resting));
        Assert.That(s.Session.Rooms[0].DoorHealth, Is.EqualTo(40 - 5));
    }

    [Test]
    [Property("Requirement", "CMP-06")]
    [Property("Requirement", "SET-04")]
    [Property("Requirement", "RAT-09")]
    [Property("Requirement", "DAT-06")]
    public void WithTheSuggestiveFilterOnCompanyBecomesHostingAndPlaysTheSame()
    {
        var hosting = CompanyScenario(filters: new ContentFilters(NoSuggestive: true));
        var company = CompanyScenario();
        Assert.That(hosting.Session.TechniqueName("company"), Is.EqualTo("Hosting"));
        Assert.That(company.Session.TechniqueName("company"), Is.EqualTo("Company"));

        foreach (var s in new[] { hosting, company })
        {
            s.Spawn("creature-09", Beside);
            ToTheRoom(s);
        }

        Assert.That(hosting.Session.ComputeStateHash(), Is.EqualTo(company.Session.ComputeStateHash()), "same mechanics");

        var line = hosting.Session.FlavourText(hosting.Session.CycleLogEntries.Single(e => e.Kind == "company.room"));
        Assert.That(line, Is.EqualTo("The door closes on a long, unhurried conversation."), "only filterSafe lines");
        Assert.That(company.Session.FlavourText(company.Session.CycleLogEntries.Single(e => e.Kind == "company.room")), Is.Not.Null);
    }

    [Test]
    [Property("Requirement", "SET-04")]
    public void TheFiltersCanChangeInAnyPhase()
    {
        var s = new Scenario();
        var on = new ContentFilters(ReducedBlood: true, NoSuggestive: true);
        Assert.That(s.Session.Execute(new SetContentFilters(on)).Succeeded, Is.True);
        s.Begin();
        Assert.That(s.Session.Execute(new SetContentFilters(ContentFilters.None)).Succeeded, Is.True, "during the mist");
        Assert.That(s.Session.Filters, Is.EqualTo(ContentFilters.None));
        Assert.That(s.Session.DrainEvents().OfType<ContentFiltersChanged>().Count(), Is.EqualTo(2));
    }

    [Test]
    [Property("Requirement", "RAT-09")]
    [Property("Requirement", "DAT-06")]
    public void WithReducedBloodOnlyBloodSafeLinesAreUsed()
    {
        var lines = GameContent.Default.Flavour;
        var reduced = Enumerable.Range(0, 8)
            .Select(seed => FlavourPicker.Pick(lines, "unit.fell", new ContentFilters(ReducedBlood: true), seed)!)
            .ToList();
        var full = Enumerable.Range(0, 8)
            .Select(seed => FlavourPicker.Pick(lines, "unit.fell", ContentFilters.None, seed)!.Id)
            .Distinct()
            .ToList();

        Assert.That(reduced.All(l => l.BloodSafe == true), Is.True);
        Assert.That(full, Has.Count.EqualTo(2), "both lines without the filter");
        Assert.That(FlavourPicker.Pick(lines, "no-such-context", ContentFilters.None, 0), Is.Null);
    }

    [Test]
    [Property("Requirement", "CMP-01")]
    [Property("Requirement", "DAT-04")]
    public void PreferencesAndRefusalsMustNameKnownCreaturesOnce()
    {
        ContentTestKit.AssertRejected(ContentTestKit.WithCharacter("staff-03", c => c with { Prefers = ["nobody"] }),
            "unknown creature 'nobody'");
        ContentTestKit.AssertRejected(ContentTestKit.WithCharacter("staff-03", c => c with { Prefers = ["creature-10"] }),
            "both preferred and refused");
    }

    [Test]
    [Property("Requirement", "DAT-04")]
    [Property("Requirement", "BRD-01")]
    public void CombatCompanyAndRoomValuesAreChecked()
    {
        var set = ContentTestKit.Set;
        ContentTestKit.AssertRejected(set with { Balance = set.Balance with { Rooms = 4 } }, "back rooms");
        ContentTestKit.AssertRejected(set with { Balance = set.Balance with { Combat = set.Balance.Combat with { WoundChance = 2 } } },
            "combat value");
        ContentTestKit.AssertRejected(set with { Balance = set.Balance with { Company = set.Balance.Company with { DoorHealth = 0 } } },
            "company value");
    }
}
