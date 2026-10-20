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
    TileBlocked,
    BoardFull,
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
