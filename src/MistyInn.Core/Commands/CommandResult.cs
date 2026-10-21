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

    /// <summary>Integrity is already full (BRD-07).</summary>
    NothingToRepair,

    /// <summary>That character is already in the inn (STF-01).</summary>
    AlreadyOwned,

    /// <summary>The bench has no room for another staff member (section 6.4).</summary>
    BenchFull,

    /// <summary>The shop slot is empty or does not exist (SHP-02).</summary>
    UnknownOffer,

    /// <summary>The owned character is already ★5 (STF-08).</summary>
    MaxRank,

    /// <summary>An upgrade pick must be made first (STF-08, D-081).</summary>
    UpgradePending,

    /// <summary>The option is not in the tree at the new rank, or not available after the earlier picks (STF-17).</summary>
    OptionNotAvailable,

    /// <summary>The command names an item the player does not own, or the wrong kind of item.</summary>
    UnknownItem,

    /// <summary>The staff member already carries as many items as it has slots (ITM-02).</summary>
    SlotsFull,

    /// <summary>Item storage is full (ITM-02).</summary>
    StorageFull,

    /// <summary>No more inn items can be shown in the inn (ITM-04).</summary>
    InnItemsFull,

    /// <summary>The innkeeper cannot be sold or carry staff items (ESC-06).</summary>
    NotStaff,
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
