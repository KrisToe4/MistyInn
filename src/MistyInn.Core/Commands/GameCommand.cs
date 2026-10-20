namespace MistyInn.Core.Commands;

/// <summary>
/// The base of every command the presentation sends to Core (ARC-10). Commands are immutable records; the concrete
/// commands are added by the steps that implement them.
/// </summary>
public abstract record GameCommand;
