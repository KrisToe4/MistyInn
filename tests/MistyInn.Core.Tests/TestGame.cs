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

    /// <summary>The real content (the built-in copy of <c>data/</c>) with other starting staff or tuning values.</summary>
    public static GameContent ContentWith(
        IReadOnlyList<string>? startingStaff = null, Func<BalanceDef, BalanceDef>? balance = null)
    {
        var set = GameContent.Default.Set;
        var b = balance is null ? set.Balance : balance(set.Balance);
        if (startingStaff is not null)
        {
            b = b with { StartingStaff = startingStaff };
        }

        return GameContent.Create(set with { Balance = b });
    }

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
