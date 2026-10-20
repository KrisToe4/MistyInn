using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Content;
using MistyInn.Core.Randomness;
using MistyInn.Core.Run;

namespace MistyInn.Core.Tests;

/// <summary>Shared helpers for session-level tests (fixed seeds and fixed content, TST-01).</summary>
internal static class TestGame
{
    /// <summary>An entrance tile, inside the inn and off the door approach.</summary>
    public static readonly Tile InnkeeperTile = new(11, 12);

    public static GameSession NewSession(ulong seed = 1, GameContent? content = null, GameSessionLimits? limits = null) =>
        new(new SeededRandom(seed), limits, content);

    public static UnitSnapshot Innkeeper(GameSession session) => session.Units.Single(u => u.IsInnkeeper);

    public static IReadOnlyList<UnitSnapshot> Staff(GameSession session) =>
        session.Units.Where(u => !u.IsInnkeeper).ToList();

    /// <summary>Places the innkeeper (if needed) and begins the mist.</summary>
    public static void StartMist(GameSession session)
    {
        Assert.That(session.Execute(new PlaceUnit(Innkeeper(session).Id, InnkeeperTile)).Succeeded, Is.True);
        Assert.That(session.Execute(new BeginMist()).Succeeded, Is.True);
    }

    /// <summary>Plays the current Dusk's cycle to the next Dusk.</summary>
    public static void PlayCycle(GameSession session)
    {
        StartMist(session);
        session.RunMistToDawn();
        Assert.That(session.Execute(new ContinueFromDawn()).Succeeded, Is.True);
    }

    /// <summary>The default content with other staff or tuning values.</summary>
    public static GameContent ContentWith(
        IReadOnlyList<StaffDef>? staff = null, Func<BalanceDef, BalanceDef>? balance = null)
    {
        var d = DefaultContent.Create();
        return new GameContent(
            d.Layout,
            d.Planes,
            d.Creatures,
            staff ?? d.StartingStaff,
            d.UnlockedPlanes,
            d.WanderZones,
            balance is null ? d.Balance : balance(d.Balance));
    }

    public static IReadOnlyList<StaffDef> InnkeeperAndStaff(int staffCount) =>
        new[] { new StaffDef("innkeeper", "The Innkeeper", IsInnkeeper: true) }
            .Concat(Enumerable.Range(1, staffCount).Select(i => new StaffDef($"staff-{i}", $"Staff {i}", false)))
            .ToList();

    /// <summary>Steps the mist one fixed step at a time, calling <paramref name="afterStep"/> after each.</summary>
    public static void StepThroughMist(GameSession session, Action afterStep)
    {
        var oneStep = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / GameSession.StepsPerSecond + 1);
        while (session.Phase == Phase.Mist)
        {
            session.Tick(oneStep);
            afterStep();
        }
    }
}
