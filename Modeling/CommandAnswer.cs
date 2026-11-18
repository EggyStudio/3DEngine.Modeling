namespace Modeling;

/// <summary>
/// What a model command answers, the text a script reads and why it was refused where it was.
/// </summary>
/// <param name="Text">
/// The answer, a model's counts after an operation, or why the line was refused.
/// </param>
/// <param name="Failure">Why the line was refused, or null where it was done.</param>
public sealed record CommandAnswer(string Text, CommandFailure? Failure = null)
{
    /// <summary>Whether the line was done.</summary>
    public bool Done => Failure is null;

    /// <summary>
    /// The failure as a code of capitals and underscores, as <c>NO_SUCH_MODEL</c>, which a console
    /// that names its failures so takes as it is, or null where the line was done.
    /// </summary>
    public string? Code => Failure switch
    {
        null => null,
        CommandFailure.BadArgument => "BAD_ARGUMENT",
        CommandFailure.NoSuchCommand => "NO_SUCH_COMMAND",
        CommandFailure.NoSuchModel => "NO_SUCH_MODEL",
        CommandFailure.NothingToUndo => "NOTHING_TO_UNDO",
        CommandFailure.NothingToRedo => "NOTHING_TO_REDO",
        CommandFailure.NoSuchFile => "NO_SUCH_FILE",
        CommandFailure.BadFile => "BAD_FILE",
        CommandFailure.NotWritten => "NOT_WRITTEN",
        _ => throw new ArgumentOutOfRangeException(nameof(Failure), Failure, "No such failure."),
    };
}

/// <summary>Why a model command refused its line.</summary>
public enum CommandFailure
{
    /// <summary>
    /// A word of the line is not what the command takes, or the operation refused it.
    /// </summary>
    BadArgument,

    /// <summary>The line names no command of <see cref="ModelCommands"/>.</summary>
    NoSuchCommand,

    /// <summary>No model has the name, which an operation that makes no model needs.</summary>
    NoSuchModel,

    /// <summary>The model has no operation to take back.</summary>
    NothingToUndo,

    /// <summary>The model has no operation taken back to do again.</summary>
    NothingToRedo,

    /// <summary>There is no file at the path a load names.</summary>
    NoSuchFile,

    /// <summary>The file a load names is not a model's recipe, or a line of it fails.</summary>
    BadFile,

    /// <summary>A file could not be written.</summary>
    NotWritten,
}
