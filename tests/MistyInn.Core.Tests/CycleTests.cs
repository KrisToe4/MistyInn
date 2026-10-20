using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Run;
using static MistyInn.Core.Tests.TestGame;

namespace MistyInn.Core.Tests;

/// <summary>Phases, placement and the commands of Dusk and Dawn (TST-04).</summary>
public class CycleTests
{
    [Test]
    [Property("Requirement", "CYC-01")]
    [Property("Requirement", "PLN-03")]
    public void RunStartsAtDuskOfCycleOneWithEveryoneOnTheBench()
    {
        var session = NewSession();

        Assert.That(session.Phase, Is.EqualTo(Phase.Dusk));
        Assert.That(session.Cycle, Is.EqualTo(1));
        Assert.That(session.ConvergingPlanes, Has.Count.EqualTo(1));
        Assert.That(session.Content.UnlockedPlanes, Does.Contain(session.ConvergingPlanes[0].Id));
        Assert.That(session.Units, Has.Count.EqualTo(3));
        Assert.That(session.Units.All(u => u.IsOnBench), Is.True, "everyone starts on the bench");
        Assert.That(session.TimeToDawn, Is.EqualTo(session.MistDuration));
        Assert.That(session.DrainEvents(), Is.Empty, "the starting state is read through queries");
    }

    [Test]
    [Property("Requirement", "CYC-01")]
    [Property("Requirement", "SIM-02")]
    public void DuskIsUntimed()
    {
        var session = NewSession();
        var before = session.ComputeStateHash();

        session.Tick(TimeSpan.FromMinutes(10));

        Assert.That(session.Time, Is.EqualTo(TimeSpan.Zero));
        Assert.That(session.ComputeStateHash(), Is.EqualTo(before));
    }

    [Test]
    [Property("Requirement", "BRD-02")]
    [Property("Requirement", "CAM-13")]
    public void PlacingAUnitPutsItOnTheTile()
    {
        var session = NewSession();
        var mara = Staff(session)[0];
        var tile = new Tile(12, 6);

        var result = session.Execute(new PlaceUnit(mara.Id, tile));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(session.Units.Single(u => u.Id == mara.Id).Tile, Is.EqualTo(tile));
        Assert.That(session.StaffOnBoard, Is.EqualTo(1));
        Assert.That(session.DrainEvents(), Is.EqualTo(new DomainEvent[] { new UnitPlaced(TimeSpan.Zero, mara.Id, tile) }));
    }

    [Test]
    [Property("Requirement", "BRD-02")]
    public void MovingAUnitFreesItsOldTile()
    {
        var session = NewSession();
        var mara = Staff(session)[0];
        var tobin = Staff(session)[1];
        session.Execute(new PlaceUnit(mara.Id, new Tile(12, 6)));

        session.Execute(new PlaceUnit(mara.Id, new Tile(14, 6)));

        Assert.That(session.Execute(new PlaceUnit(tobin.Id, new Tile(12, 6))).Succeeded, Is.True);
        Assert.That(session.StaffOnBoard, Is.EqualTo(2));
    }

    [Test]
    [Property("Requirement", "BRD-02")]
    public void PlacingOnTheSameTileAgainChangesNothing()
    {
        var session = NewSession();
        var mara = Staff(session)[0];
        session.Execute(new PlaceUnit(mara.Id, new Tile(12, 6)));
        session.DrainEvents();

        Assert.That(session.Execute(new PlaceUnit(mara.Id, new Tile(12, 6))).Succeeded, Is.True);
        Assert.That(session.DrainEvents(), Is.Empty);
    }

    [TestCase(1, 1, CommandFailure.TileBlocked, TestName = "Outside in the clearing")]
    [TestCase(5, 4, CommandFailure.TileBlocked, TestName = "On a wall")]
    [TestCase(13, 14, CommandFailure.TileBlocked, TestName = "In the doorway")]
    [TestCase(40, 40, CommandFailure.TileBlocked, TestName = "Off the map")]
    [Property("Requirement", "BRD-02")]
    [Property("Requirement", "ARC-11")]
    public void StaffCannotStandOffTheInnFloor(int x, int y, CommandFailure expected)
    {
        var session = NewSession();
        var before = session.ComputeStateHash();

        var result = session.Execute(new PlaceUnit(Staff(session)[0].Id, new Tile(x, y)));

        Assert.That(result.Failure, Is.EqualTo(expected));
        Assert.That(session.ComputeStateHash(), Is.EqualTo(before), "a refused command changes nothing");
    }

    [Test]
    [Property("Requirement", "BRD-02")]
    [Property("Requirement", "ARC-11")]
    public void OneUnitPerTile()
    {
        var session = NewSession();
        session.Execute(new PlaceUnit(Staff(session)[0].Id, new Tile(12, 6)));

        var result = session.Execute(new PlaceUnit(Staff(session)[1].Id, new Tile(12, 6)));

        Assert.That(result.Failure, Is.EqualTo(CommandFailure.TileOccupied));
        Assert.That(session.CheckPlaceUnit(Staff(session)[1].Id, new Tile(12, 6)), Is.EqualTo(CommandFailure.TileOccupied));
    }

    [Test]
    [Property("Requirement", "ARC-11")]
    public void UnknownUnitIsRefused()
    {
        var session = NewSession();

        Assert.That(session.Execute(new PlaceUnit(new UnitId(99), new Tile(12, 6))).Failure,
            Is.EqualTo(CommandFailure.UnknownUnit));
        Assert.That(session.Execute(new BenchUnit(new UnitId(99))).Failure, Is.EqualTo(CommandFailure.UnknownUnit));
    }

    [Test]
    [Property("Requirement", "BRD-02")]
    [Property("Requirement", "ARC-11")]
    public void OnlyThreeStaffFitOnTheBoardInTheFirstCycle()
    {
        var session = NewSession(content: ContentWith(startingStaff: ["staff-01", "staff-02", "staff-03", "staff-04"]));
        var staff = Staff(session);

        for (var i = 0; i < 3; i++)
        {
            Assert.That(session.Execute(new PlaceUnit(staff[i].Id, new Tile(11 + i, 6))).Succeeded, Is.True);
        }

        Assert.That(session.Execute(new PlaceUnit(staff[3].Id, new Tile(15, 6))).Failure,
            Is.EqualTo(CommandFailure.BoardFull));
        Assert.That(session.Execute(new PlaceUnit(Innkeeper(session).Id, InnkeeperTile)).Succeeded, Is.True,
            "the innkeeper does not take a staff slot");
        Assert.That(session.Execute(new PlaceUnit(staff[0].Id, new Tile(16, 9))).Succeeded, Is.True,
            "moving a placed staff member is not adding one");
    }

    [Test]
    [Property("Requirement", "BRD-02")]
    public void StaffSlotsGrowEveryTwoCycles()
    {
        var session = NewSession();
        var slots = new List<int>();

        for (var cycle = 1; cycle <= 6; cycle++)
        {
            slots.Add(session.MaxStaffOnBoard);
            PlayCycle(session);
        }

        Assert.That(slots, Is.EqualTo(new[] { 3, 3, 4, 4, 5, 5 }));
    }

    [Test]
    [Property("Requirement", "CAM-13")]
    public void BenchingTakesAStaffMemberOffTheBoard()
    {
        var session = NewSession();
        var mara = Staff(session)[0];
        session.Execute(new PlaceUnit(mara.Id, new Tile(12, 6)));
        session.DrainEvents();

        var result = session.Execute(new BenchUnit(mara.Id));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(session.Units.Single(u => u.Id == mara.Id).IsOnBench, Is.True);
        Assert.That(session.StaffOnBoard, Is.Zero);
        Assert.That(session.DrainEvents(), Is.EqualTo(new DomainEvent[] { new UnitBenched(TimeSpan.Zero, mara.Id) }));
        Assert.That(session.Execute(new PlaceUnit(Staff(session)[1].Id, new Tile(12, 6))).Succeeded, Is.True,
            "the tile is free again");
    }

    [Test]
    [Property("Requirement", "CAM-13")]
    public void TheInnkeeperCannotBeBenched()
    {
        var session = NewSession();
        var keeper = Innkeeper(session);
        session.Execute(new PlaceUnit(keeper.Id, InnkeeperTile));

        Assert.That(session.Execute(new BenchUnit(keeper.Id)).Failure, Is.EqualTo(CommandFailure.CannotBench));
        Assert.That(Innkeeper(session).Tile, Is.EqualTo(InnkeeperTile));
    }

    [Test]
    [Property("Requirement", "CYC-01")]
    public void TheMistNeedsTheInnkeeperOnTheBoard()
    {
        var session = NewSession();

        Assert.That(session.Execute(new BeginMist()).Failure, Is.EqualTo(CommandFailure.InnkeeperNotPlaced));
        Assert.That(session.Phase, Is.EqualTo(Phase.Dusk));
    }

    [Test]
    [Property("Requirement", "CYC-02")]
    [Property("Requirement", "INV-12")]
    public void BeginningTheMistStartsTheClockAndTheLog()
    {
        var session = NewSession();
        session.Execute(new PlaceUnit(Innkeeper(session).Id, InnkeeperTile));
        session.DrainEvents();

        var result = session.Execute(new BeginMist());

        Assert.That(result.Succeeded, Is.True);
        Assert.That(session.Phase, Is.EqualTo(Phase.Mist));
        Assert.That(session.Time, Is.EqualTo(TimeSpan.Zero));
        Assert.That(session.DrainEvents(), Is.EqualTo(new DomainEvent[] { new PhaseChanged(TimeSpan.Zero, 1, Phase.Mist) }));
        Assert.That(session.CycleLogEntries.Single().Kind, Is.EqualTo("phase.mist"));
    }

    [Test]
    [Property("Requirement", "ARC-11")]
    [Property("Requirement", "SCP-13")]
    public void OnlySpeedCanChangeDuringTheMist()
    {
        var session = NewSession();
        StartMist(session);
        var before = session.ComputeStateHash();

        Assert.That(session.Execute(new PlaceUnit(Staff(session)[0].Id, new Tile(12, 6))).Failure,
            Is.EqualTo(CommandFailure.WrongPhase));
        Assert.That(session.Execute(new BenchUnit(Staff(session)[0].Id)).Failure, Is.EqualTo(CommandFailure.WrongPhase));
        Assert.That(session.Execute(new BeginMist()).Failure, Is.EqualTo(CommandFailure.WrongPhase));
        Assert.That(session.Execute(new ContinueFromDawn()).Failure, Is.EqualTo(CommandFailure.WrongPhase));
        Assert.That(session.CheckPlaceUnit(Staff(session)[0].Id, new Tile(12, 6)), Is.EqualTo(CommandFailure.WrongPhase));
        Assert.That(session.ComputeStateHash(), Is.EqualTo(before));
        Assert.That(session.Execute(new SetSpeed(2)).Succeeded, Is.True);
    }

    [TestCase(3)]
    [TestCase(-1)]
    [TestCase(8)]
    [Property("Requirement", "SCP-13")]
    public void SpeedIsPauseOneTwoOrFour(int multiplier)
    {
        var session = NewSession();

        Assert.That(session.Execute(new SetSpeed(multiplier)).Failure, Is.EqualTo(CommandFailure.InvalidValue));
        Assert.That(session.Speed, Is.EqualTo(1));
    }

    [Test]
    [Property("Requirement", "SIM-02")]
    [Property("Requirement", "INV-03")]
    [Property("Requirement", "SCP-13")]
    public void SpeedRunsMoreStepsNotLongerOnes()
    {
        var session = NewSession();
        StartMist(session);

        session.Execute(new SetSpeed(2));
        session.Tick(TimeSpan.FromSeconds(1));
        Assert.That(session.Time, Is.EqualTo(TimeSpan.FromSeconds(2)));

        session.Execute(new SetSpeed(0));
        session.Tick(TimeSpan.FromSeconds(5));
        Assert.That(session.Time, Is.EqualTo(TimeSpan.FromSeconds(2)), "paused");

        session.Execute(new SetSpeed(4));
        session.Tick(TimeSpan.FromSeconds(0.5));
        Assert.That(session.Time, Is.EqualTo(TimeSpan.FromSeconds(4)));
    }

    [Test]
    [Property("Requirement", "SIM-02")]
    public void ALongFrameRunsAtMostOneTicksWorthOfSteps()
    {
        var session = NewSession();
        StartMist(session);
        session.Execute(new SetSpeed(4));

        session.Tick(TimeSpan.FromSeconds(10));

        Assert.That(session.Time, Is.EqualTo(TimeSpan.FromSeconds((double)GameSession.MaxStepsPerTick / GameSession.StepsPerSecond)));
        Assert.That(session.InterpolationAlpha, Is.Zero, "the backlog is dropped, not saved up");
    }

    [Test]
    [Property("Requirement", "CYC-03")]
    public void DawnBreaksWhenTheMistTimerEnds()
    {
        var session = NewSession();
        StartMist(session);

        session.RunMistToDawn();

        Assert.That(session.Phase, Is.EqualTo(Phase.Dawn));
        Assert.That(session.Time, Is.EqualTo(session.MistDuration));
        Assert.That(session.TimeToDawn, Is.EqualTo(TimeSpan.Zero));
        Assert.That(session.Creatures, Is.Empty);
    }

    [Test]
    [Property("Requirement", "CYC-04")]
    [Property("Requirement", "PLN-03")]
    public void ContinuingFromDawnStartsTheNextDusk()
    {
        var session = NewSession();
        StartMist(session);
        session.RunMistToDawn();
        session.DrainEvents();

        Assert.That(session.Execute(new ContinueFromDawn()).Succeeded, Is.True);

        Assert.That(session.Phase, Is.EqualTo(Phase.Dusk));
        Assert.That(session.Cycle, Is.EqualTo(2));
        Assert.That(session.Time, Is.EqualTo(TimeSpan.Zero));
        var events = session.DrainEvents();
        Assert.That(events[0], Is.EqualTo(new PhaseChanged(TimeSpan.Zero, 2, Phase.Dusk)));
        Assert.That(((ConvergingPlanesDrawn)events[1]).PlaneIds, Is.EqualTo(session.ConvergingPlanes.Select(p => p.Id)));
    }

    [Test]
    [Property("Requirement", "CYC-04")]
    public void ContinuingIsOnlyForDawn()
    {
        var session = NewSession();

        Assert.That(session.Execute(new ContinueFromDawn()).Failure, Is.EqualTo(CommandFailure.WrongPhase));
    }

    [Test]
    [Property("Requirement", "BRD-04")]
    public void PlacementsStayFromCycleToCycle()
    {
        var session = NewSession();
        var mara = Staff(session)[0];
        session.Execute(new PlaceUnit(mara.Id, new Tile(16, 9)));

        PlayCycle(session);

        Assert.That(session.Units.Single(u => u.Id == mara.Id).Tile, Is.EqualTo(new Tile(16, 9)));
        Assert.That(Innkeeper(session).Tile, Is.EqualTo(InnkeeperTile));
    }

    [Test]
    [Property("Requirement", "PLN-03")]
    public void TheSamePlaneNeverComesTwiceInARow()
    {
        var session = NewSession(seed: 21);
        var first = session.ConvergingPlanes.Single().Id;

        PlayCycle(session);
        Assert.That(session.ConvergingPlanes.Single().Id, Is.Not.EqualTo(first), "cycle 2 has one other plane");

        PlayCycle(session);
        Assert.That(session.ConvergingPlanes, Has.Count.EqualTo(2), "cycle 3 has two planes");
    }

    [Test]
    [Property("Requirement", "ARC-05")]
    public void RunMistToDawnNeedsTheMist()
    {
        var session = NewSession();

        Assert.That(() => session.RunMistToDawn(), Throws.InvalidOperationException);
    }
}
