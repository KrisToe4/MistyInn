using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Run;
using static MistyInn.Core.Tests.TestGame;

namespace MistyInn.Core.Tests;

/// <summary>Arrivals, movement, dawn and determinism in the mist phase (TST-04).</summary>
public class MistSimulationTests
{
    private static List<T> EventsOf<T>(IEnumerable<DomainEvent> events) => events.OfType<T>().ToList();

    [Test]
    [Property("Requirement", "CRE-01")]
    [Property("Requirement", "CYC-02")]
    public void CreaturesArriveDuringTheMist()
    {
        var session = NewSession(seed: 7);
        StartMist(session);

        session.RunMistToDawn();

        var arrivals = EventsOf<CreatureArrived>(session.DrainEvents());

        // About 100 s of arrivals at about 9 s each in cycle 1.
        Assert.That(arrivals.Count, Is.InRange(6, 18));
        Assert.That(arrivals.Select(a => a.Creature), Is.Unique);
    }

    [Test]
    [Property("Requirement", "CRE-01")]
    [Property("Requirement", "PLN-03")]
    public void CreaturesComeOnlyFromTheConvergingPlanes()
    {
        var session = NewSession(seed: 3);
        PlayCycle(session);
        PlayCycle(session);
        var planes = session.ConvergingPlanes.Select(p => p.Id).ToList();
        StartMist(session);
        session.DrainEvents();

        session.RunMistToDawn();

        // Is.SubsetOf counts duplicates, so compare the distinct planes the creatures came from.
        var arrivedFrom = EventsOf<CreatureArrived>(session.DrainEvents()).Select(a => a.PlaneId).Distinct().ToList();
        Assert.That(arrivedFrom, Is.Not.Empty);
        Assert.That(arrivedFrom, Is.SubsetOf(planes));
    }

    [Test]
    [Property("Requirement", "CYC-03")]
    public void ArrivalsStopTwentySecondsBeforeDawn()
    {
        for (ulong seed = 1; seed <= 10; seed++)
        {
            var session = NewSession(seed);
            StartMist(session);
            session.RunMistToDawn();

            // An arrival due before 100 s happens on the first step at or after it, so at most at 100 s.
            Assert.That(EventsOf<CreatureArrived>(session.DrainEvents()).Select(a => a.At),
                Has.All.LessThanOrEqualTo(TimeSpan.FromSeconds(100)), $"seed {seed}");
        }
    }

    [Test]
    [Property("Requirement", "CRE-02")]
    [Property("Requirement", "INV-05")]
    [Property("Requirement", "INV-12")]
    public void TheBoardNeverHoldsMoreThanTwelveCreatures()
    {
        var crowded = ContentWith(balance: b => b with { BaseArrivalIntervalSeconds = 0.5 });
        var session = NewSession(seed: 2, content: crowded);
        StartMist(session);
        var most = 0;

        StepThroughMist(session, () => most = Math.Max(most, session.Creatures.Count));

        Assert.That(most, Is.EqualTo(12));
        Assert.That(session.CycleLogEntries.Any(e => e.Kind == "arrival.skipped" && e.Cause == "board.full"), Is.True);
    }

    [Test]
    [Property("Requirement", "PLN-04")]
    public void EachCreatureWalksOutOfItsPlanesGate()
    {
        var session = NewSession(seed: 4);
        StartMist(session);
        var checkedCount = 0;

        StepThroughMist(session, () =>
        {
            foreach (var arrived in EventsOf<CreatureArrived>(session.DrainEvents()))
            {
                var creature = session.Creatures.Single(c => c.Id == arrived.Creature);
                var gate = TilePosition.Center(session.Content.GateFor(arrived.PlaneId));
                Assert.That(creature.PreviousPosition, Is.EqualTo(gate));
                checkedCount++;
            }
        });

        Assert.That(checkedCount, Is.GreaterThan(0));
    }

    [Test]
    [Property("Requirement", "CRE-04")]
    [Property("Requirement", "BRD-05")]
    [Property("Requirement", "SIM-03")]
    public void CreaturesComeInOnlyThroughTheFrontDoorAndStayOnWalkableTiles()
    {
        var session = NewSession(seed: 9, content: ContentWith(balance: b => b with { BaseArrivalIntervalSeconds = 2 }));
        StartMist(session);
        var layout = session.Layout;
        var lastTile = new Dictionary<CreatureId, Tile>();
        var cameIn = 0;

        StepThroughMist(session, () =>
        {
            foreach (var creature in session.Creatures)
            {
                var tile = creature.Position.NearestTile;
                Assert.That(BoardLayout.IsWalkableKind(layout.KindAt(tile)), Is.True, $"{creature.Id} at {tile}");

                if (lastTile.TryGetValue(creature.Id, out var previous)
                    && layout.KindAt(previous) != TileKind.Floor && layout.KindAt(tile) == TileKind.Floor)
                {
                    Assert.That(previous, Is.EqualTo(layout.FrontDoor), $"{creature.Id} came in from {previous}");
                    cameIn++;
                }

                lastTile[creature.Id] = tile;
            }
        });

        Assert.That(cameIn, Is.GreaterThan(5));
    }

    [Test]
    [Property("Requirement", "CRE-04")]
    public void CreaturesInsideWanderTheCommonRoom()
    {
        var session = NewSession(seed: 5);
        StartMist(session);
        var idleZones = new HashSet<string>();

        StepThroughMist(session, () =>
        {
            foreach (var creature in session.Creatures.Where(c => c.Activity == CreatureActivity.Idle))
            {
                idleZones.Add(session.Layout.ZoneAt(creature.Position.NearestTile));
            }
        });

        Assert.That(idleZones, Is.Not.Empty);
        Assert.That(idleZones, Is.SubsetOf(session.Content.WanderZones));
    }

    [Test]
    [Property("Requirement", "SIM-03")]
    public void ViewsCanInterpolateBetweenSteps()
    {
        var session = NewSession(seed: 6);
        StartMist(session);
        for (var i = 0; i < 30; i++)
        {
            session.Tick(TimeSpan.FromSeconds(1));
        }

        Assert.That(session.Creatures, Is.Not.Empty);
        var halfStep = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / GameSession.StepsPerSecond / 2);

        session.Tick(halfStep);

        Assert.That(session.InterpolationAlpha, Is.InRange(0.4, 0.6));
        foreach (var creature in session.Creatures.Where(c => c.Activity != CreatureActivity.Idle))
        {
            var speed = session.Content.Creatures.Single(d => d.Id == creature.CreatureDefId).Speed;
            Assert.That(creature.PreviousPosition.DistanceTo(creature.Position),
                Is.LessThanOrEqualTo(speed / GameSession.StepsPerSecond + 1e-9), "one step apart");
        }
    }

    [Test]
    [Property("Requirement", "CYC-03")]
    [Property("Requirement", "INV-12")]
    [Property("Requirement", "CYC-05")]
    public void DawnDissolvesEveryCreatureAndTheLogSaysWhy()
    {
        var session = NewSession(seed: 8);
        StartMist(session);
        session.Tick(TimeSpan.FromSeconds(4));
        while (session.Phase == Phase.Mist && session.TimeToDawn > TimeSpan.FromSeconds(1))
        {
            session.Tick(TimeSpan.FromSeconds(1));
        }

        var present = session.Creatures.Select(c => c.Id).ToList();
        session.DrainEvents();
        session.RunMistToDawn();

        var dissolved = EventsOf<CreatureDissolved>(session.DrainEvents()).Select(d => d.Creature).ToList();
        Assert.That(present, Is.Not.Empty);
        Assert.That(dissolved, Is.EquivalentTo(present));
        Assert.That(session.Creatures, Is.Empty);

        var log = session.CycleLogEntries;
        Assert.That(log.Count(e => e.Kind == "creature.dissolved" && e.Cause == "dawn"), Is.EqualTo(present.Count));
        Assert.That(log.Count(e => e.Kind == "creature.arrived"), Is.GreaterThanOrEqualTo(present.Count));
        Assert.That(log[^1].Kind, Is.EqualTo("phase.dawn"));
        Assert.That(log.Select(e => e.Sequence), Is.Ordered.Ascending);
    }

    [Test]
    [Property("Requirement", "INV-02")]
    [Property("Requirement", "TST-04")]
    [Property("Requirement", "ARC-05")]
    public void SameSeedAndCommandsGiveTheSameRun()
    {
        static ulong Play(ulong seed)
        {
            var session = NewSession(seed);
            session.Execute(new PlaceUnit(Staff(session)[0].Id, new Tile(12, 6)));
            for (var i = 0; i < 3; i++)
            {
                PlayCycle(session);
            }

            StartMist(session);
            session.RunMistToDawn();
            return session.ComputeStateHash();
        }

        Assert.That(Play(42), Is.EqualTo(Play(42)));
        Assert.That(Play(42), Is.Not.EqualTo(Play(43)));
    }

    [Test]
    [Property("Requirement", "SIM-02")]
    [Property("Requirement", "INV-03")]
    [Property("Requirement", "ARC-05")]
    public void FrameRateNeverChangesTheOutcome()
    {
        var fast = NewSession(seed: 12);
        var slow = NewSession(seed: 12);
        var instant = NewSession(seed: 12);
        StartMist(fast);
        StartMist(slow);
        StartMist(instant);

        while (fast.Phase == Phase.Mist)
        {
            fast.Tick(TimeSpan.FromSeconds(1.0 / 144));
        }

        var frames = new[] { 0.016, 0.033, 0.1, 0.007, 0.25 };
        for (var i = 0; slow.Phase == Phase.Mist; i++)
        {
            slow.Tick(TimeSpan.FromSeconds(frames[i % frames.Length]));
        }

        instant.RunMistToDawn();

        Assert.That(slow.ComputeStateHash(), Is.EqualTo(fast.ComputeStateHash()));
        Assert.That(instant.ComputeStateHash(), Is.EqualTo(fast.ComputeStateHash()));
    }
}
