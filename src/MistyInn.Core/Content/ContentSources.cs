using System.Reflection;

namespace MistyInn.Core.Content;

/// <summary>Returns the text of a content file by name, for example <c>planes.json</c> (DAT-02).</summary>
public interface IContentSource
{
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    string ReadText(string fileName);
}

/// <summary>
/// The copy of <c>data/*.json</c> built into Core. The tests read it, and an export build falls back to it when the
/// files it shipped with cannot be loaded (DAT-04). Both come from the same committed files.
/// </summary>
public sealed class EmbeddedContentSource : IContentSource
{
    private const string Prefix = "MistyInn.Data.";
    private static readonly Assembly Assembly = typeof(EmbeddedContentSource).Assembly;

    public string ReadText(string fileName)
    {
        using var stream = Assembly.GetManifestResourceStream(Prefix + fileName)
            ?? throw new FileNotFoundException($"The built-in content has no '{fileName}'.", fileName);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
