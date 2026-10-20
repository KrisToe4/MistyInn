namespace MistyInn.Core.Content;

/// <summary>Content could not be read or is invalid (DAT-04). <see cref="Errors"/> lists every problem found.</summary>
public sealed class ContentException : Exception
{
    public ContentException(IReadOnlyList<string> errors)
        : base(Describe(errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }

    private static string Describe(IReadOnlyList<string> errors) =>
        errors.Count == 1
            ? $"Invalid content: {errors[0]}"
            : $"Invalid content ({errors.Count} problems):{Environment.NewLine}- " +
              string.Join(Environment.NewLine + "- ", errors);
}
