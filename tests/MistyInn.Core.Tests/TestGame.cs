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

    /// <summary>
    /// The real content, with an innkeeper and an inn that cannot be brought down (step 5 added combat). Tests of the
    /// cycle, arrivals and service run on it so that a turned creature cannot end their run early; combat tests use
    /// the real values (<c>sturdy: false</c>). Like every <see cref="ContentWith"/> content it has no omens and locks
    /// nothing (D-093); the roguelike tests add them back.
    /// </summary>
    public static GameContent Sturdy { get; } = Toughen(Plain(GameContent.Default.Set));

    public static GameSession NewSession(ulong seed = 1, GameContent? content = null, GameSessionLimits? limits = null) =>
        new(new SeededRandom(seed), limits, content ?? Sturdy);

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

    /// <summary>
    /// The real content (the built-in copy of <c>data/</c>) with no omens and nothing locked (D-093), then other
    /// starting staff, tuning values or content, made sturdy unless <paramref name="sturdy"/> is false.
    /// </summary>
    public static GameContent ContentWith(
        IReadOnlyList<string>? startingStaff = null,
        Func<BalanceDef, BalanceDef>? balance = null,
        bool sturdy = true,
        Func<ContentSet, ContentSet>? set = null)
    {
        var s = Plain(GameContent.Default.Set);
        if (set is not null)
        {
            s = set(s);
        }

        var b = balance is null ? s.Balance : balance(s.Balance);
        if (startingStaff is not null)
        {
            b = b with { StartingStaff = startingStaff };
        }

        s = s with { Balance = b };
        return sturdy ? Toughen(s) : GameContent.Create(s);
    }

    /// <summary>The content without omens or unlocks, so a cycle plays out as it did before step 7 (D-093).</summary>
    public static ContentSet Plain(ContentSet set) => set with { Omens = [], Unlocks = [] };

    /// <summary>A creature with changed values, for content tests.</summary>
    public static ContentSet WithCreature(ContentSet set, string id, Func<CreatureDef, CreatureDef> change) =>
        set with { Creatures = set.Creatures.Select(c => c.Id == id ? change(c) : c).ToList() };

    private static GameContent Toughen(ContentSet set)
    {
        var keeper = set.Staff.Innkeeper;
        var staff = set.Staff with { Innkeeper = keeper with { Stats = keeper.Stats with { Health = 1_000_000 } } };
        return GameContent.Create(set with { Staff = staff, Balance = set.Balance with { Integrity = 1_000_000 } });
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
