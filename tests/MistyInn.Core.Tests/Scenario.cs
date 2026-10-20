using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Run;

namespace MistyInn.Core.Tests;

/// <summary>
/// A set-up inn for agitation and service tests: no random arrivals, staff and fixtures where the test puts them, and
/// creatures dropped on chosen tiles (TST-01).
/// </summary>
internal sealed class Scenario
{
    public Scenario(
        IReadOnlyList<string>? startingStaff = null,
        Func<BalanceDef, BalanceDef>? balance = null,
        ulong seed = 1,
        bool sturdy = true,
        Func<ContentSet, ContentSet>? set = null)
    {
        Content = TestGame.ContentWith(startingStaff, b =>
        {
            var changed = balance is null ? b : balance(b);
            return changed with { BaseArrivalIntervalSeconds = 1_000_000 };
        }, sturdy, set);
        Session = TestGame.NewSession(seed, Content);
    }

    public GameContent Content { get; }

    public GameSession Session { get; }

    public UnitSnapshot Staff(string staffId) => Session.Units.Single(u => u.StaffId == staffId);

    public void PlaceStaff(string staffId, Tile tile) =>
        Assert.That(Session.Execute(new PlaceUnit(Staff(staffId).Id, tile)).Failure, Is.EqualTo(CommandFailure.None), staffId);

    /// <summary>Places a fixture of this type: one from storage if there is one, otherwise a newly built one.</summary>
    public FixtureId Place(string fixtureDefId, Tile origin, int rotation = 0)
    {
        var stored = Session.Fixtures.FirstOrDefault(f => f.FixtureDefId == fixtureDefId && f.IsInStorage);
        if (stored is null)
        {
            Assert.That(Session.Execute(new BuildFixture(fixtureDefId)).Failure, Is.EqualTo(CommandFailure.None), $"build {fixtureDefId}");
            stored = Session.Fixtures.Last(f => f.FixtureDefId == fixtureDefId);
        }

        Assert.That(Session.Execute(new PlaceFixture(stored.Id, origin, rotation)).Failure, Is.EqualTo(CommandFailure.None),
            $"place {fixtureDefId} at {origin}");
        return stored.Id;
    }

    public void Begin()
    {
        Assert.That(Session.Execute(new PlaceUnit(TestGame.Innkeeper(Session).Id, new Tile(11, 13))).Succeeded, Is.True);
        Assert.That(Session.Execute(new BeginMist()).Succeeded, Is.True);
    }

    public CreatureId Spawn(string creatureDefId, Tile at) => Session.SpawnCreatureForTests(creatureDefId, at);

    public Creature Creature(CreatureId id) => Session.CreatureForTests(id);

    public Unit Unit(string staffId) => Session.UnitForTests(Staff(staffId).Id);

    public bool IsGone(CreatureId id) => Session.Creatures.All(c => c.Id != id);

    /// <summary>Spawns a creature and turns it at once (AGI-03), as if its agitation had just reached 100.</summary>
    public CreatureId SpawnTurned(string creatureDefId, Tile at)
    {
        var id = Spawn(creatureDefId, at);
        Creature(id).Agitation = 99.99;
        Step();
        return id;
    }

    /// <summary>Events drained by <see cref="Collect"/>, oldest first.</summary>
    public List<DomainEvent> Events { get; } = new();

    public void Collect() => Events.AddRange(Session.DrainEvents());

    /// <summary>Like <see cref="RunUntil"/>, keeping every event raised on the way in <see cref="Events"/>.</summary>
    public void RunUntilCollecting(Func<bool> condition, double maxSeconds = 60) =>
        RunUntil(() =>
        {
            Collect();
            return condition();
        }, maxSeconds);

    /// <summary>Runs exactly one fixed step.</summary>
    public void Step() => Session.Tick(TimeSpan.FromTicks(TimeSpan.TicksPerSecond / GameSession.StepsPerSecond + 1));

    /// <summary>Runs the mist for a number of whole game seconds (at speed 1).</summary>
    public void Run(double seconds)
    {
        for (var i = 0; i < (int)Math.Round(seconds); i++)
        {
            Session.Tick(TimeSpan.FromSeconds(1));
        }
    }

    /// <summary>Runs single steps until the condition holds, or fails after <paramref name="maxSeconds"/>.</summary>
    public void RunUntil(Func<bool> condition, double maxSeconds = 60)
    {
        var step = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / GameSession.StepsPerSecond + 1);
        for (var i = 0; i < maxSeconds * GameSession.StepsPerSecond; i++)
        {
            if (condition())
            {
                return;
            }

            Session.Tick(step);
        }

        Assert.Fail($"The condition did not hold within {maxSeconds} s.");
    }
}
