namespace MistyInn.Core.Commands;

/// <summary>Why a command was refused (ARC-11). A refused command changes nothing.</summary>
public enum CommandFailure
{
    None = 0,

    /// <summary>No handler exists for this command (yet).</summary>
    NotSupported,

    /// <summary>The command is not allowed in the current phase (for example anything but SetSpeed in the mist).</summary>
    WrongPhase,

    NotEnoughSilver,

    /// <summary>The tile is not inn floor, or a placement there would block the way in (BRD-02, BRD-03).</summary>
    TileBlocked,

    /// <summary>No more staff may stand on the board this cycle (section 6.4).</summary>
    BoardFull,

    /// <summary>The command names a unit that does not exist.</summary>
    UnknownUnit,

    /// <summary>Another unit already stands on that tile.</summary>
    TileOccupied,

    /// <summary>The innkeeper cannot be benched (ESC-06).</summary>
    CannotBench,

    /// <summary>The mist cannot begin until the innkeeper is on the board (ESC-06).</summary>
    InnkeeperNotPlaced,

    /// <summary>A value outside what the command allows (for example a speed of 3).</summary>
    InvalidValue,

    /// <summary>The command names a fixture the player does not own, or a fixture type that does not exist.</summary>
    UnknownFixture,

    /// <summary>The fixture may only go in another zone (FIX-01).</summary>
    WrongZone,
}

/// <summary>The outcome of <see cref="GameSession.Execute"/>.</summary>
public readonly record struct CommandResult(CommandFailure Failure)
{
    public static CommandResult Ok { get; } = new(CommandFailure.None);

    public bool Succeeded => Failure == CommandFailure.None;

    public static CommandResult Fail(CommandFailure failure)
    {
        if (failure == CommandFailure.None)
        {
            throw new ArgumentException("A failure needs a reason.", nameof(failure));
        }

        return new CommandResult(failure);
    }
}
