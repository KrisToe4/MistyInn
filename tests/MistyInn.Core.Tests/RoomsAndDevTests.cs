using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Events;
using MistyInn.Core.Run;
using static MistyInn.Core.Tests.TestGame;

namespace MistyInn.Core.Tests;

/// <summary>Every room reachable on its own (BRD-03), and the development commands (D-074).</summary>
public class RoomsAndDevTests
{
    [TestCase(10, 9, TestName = "In front of room 1's door")]
    [TestCase(10, 12, TestName = "In front of room 2's door")]
    [TestCase(9, 9, TestName = "On a room door")]
    [Property("Requirement", "BRD-03")]
    [Property("Requirement", "TST-07")]
    public void APlacementThatCutsOffOneRoomIsRefused(int x, int y)
    {
        var session = NewSession();
        var lantern = session.Fixtures.First(f => f.FixtureDefId == "lantern").Id;

        Assert.That(session.CheckPlaceFixture(lantern, new Tile(x, y), 0), Is.EqualTo(CommandFailure.TileBlocked),
            "the other room would still be reachable, but this one would not");
        Assert.That(session.CheckPlaceFixture(lantern, new Tile(12, 9), 0), Is.EqualTo(CommandFailure.None));
    }

    [Test]
    [Property("Requirement", "BRD-01")]
    public void TheRunUsesTheRoomsInBalance()
    {
        var session = NewSession();

        Assert.That(session.Rooms.Select(r => r.Zone), Is.EqualTo(new[] { "room-1", "room-2" }));
        Assert.That(session.Rooms.All(r => !r.InSession && r.DoorHealth == r.MaxDoorHealth), Is.True);
    }

    [Test]
    [Property("Requirement", "ARC-11")]
    public void TheDevPanelCanPutAnyCharacterOnTheBench()
    {
        var session = NewSession();
        session.DrainEvents();

        Assert.That(session.Execute(new DevAddStaff("staff-03")).Succeeded, Is.True);
        var added = session.Units.Single(u => u.StaffId == "staff-03");
        Assert.That(added.IsOnBench, Is.True);
        Assert.That(added.Techniques, Is.EqualTo(new[] { "company" }), "at ★1, with its base technique");
        Assert.That(session.DrainEvents().OfType<UnitJoined>().Single().Unit, Is.EqualTo(added.Id));

        Assert.That(session.Execute(new DevAddStaff("staff-03")).Failure, Is.EqualTo(CommandFailure.AlreadyOwned));
        Assert.That(session.Execute(new DevAddStaff("nobody")).Failure, Is.EqualTo(CommandFailure.InvalidValue));

        // Two starting staff and Staff 03 are on the bench; three more fill its six places.
        foreach (var id in new[] { "staff-04", "staff-05", "staff-06" })
        {
            Assert.That(session.Execute(new DevAddStaff(id)).Succeeded, Is.True, id);
        }

        Assert.That(session.Execute(new DevAddStaff("staff-07")).Failure, Is.EqualTo(CommandFailure.BenchFull));

        StartMist(session);
        Assert.That(session.Execute(new DevAddStaff("staff-08")).Failure, Is.EqualTo(CommandFailure.WrongPhase));
    }

    [Test]
    [Property("Requirement", "ARC-11")]
    [Property("Requirement", "PLN-03")]
    public void TheDevPanelCanChooseTheConvergingPlanes()
    {
        var session = NewSession();

        Assert.That(session.Execute(new DevSetConvergingPlanes(["pale-court"])).Succeeded, Is.True);
        Assert.That(session.ConvergingPlanes.Select(p => p.Id), Is.EqualTo(new[] { "pale-court" }));
        Assert.That(session.Forecast.Single().Plane.Id, Is.EqualTo("pale-court"));
        Assert.That(session.DrainEvents().OfType<ConvergingPlanesDrawn>().Last().PlaneIds, Is.EqualTo(new[] { "pale-court" }));

        Assert.That(session.Execute(new DevSetConvergingPlanes([])).Failure, Is.EqualTo(CommandFailure.InvalidValue));
        Assert.That(session.Execute(new DevSetConvergingPlanes(["atlantis"])).Failure, Is.EqualTo(CommandFailure.InvalidValue));
        Assert.That(session.Execute(new DevSetConvergingPlanes(["pale-court", "pale-court"])).Failure, Is.EqualTo(CommandFailure.InvalidValue));
    }

    [Test]
    [Property("Requirement", "RUN-03")]
    public void NothingButTheFiltersAndSpeedIsAcceptedOnceTheRunIsOver()
    {
        var session = NewSession(content: ContentWith(sturdy: false));
        StartMist(session);
        session.RunForTests.Integrity = 0;
        session.RunMistToDawn();

        Assert.That(session.Phase, Is.EqualTo(Phase.RunOver));
        Assert.That(session.Execute(new ContinueFromDawn()).Failure, Is.EqualTo(CommandFailure.WrongPhase));
        Assert.That(session.Execute(new BuildFixture("lantern")).Failure, Is.EqualTo(CommandFailure.WrongPhase));
        Assert.That(session.Execute(new SetContentFilters(new ContentFilters(ReducedBlood: true))).Succeeded, Is.True);
    }
}
