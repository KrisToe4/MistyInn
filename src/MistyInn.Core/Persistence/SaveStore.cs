namespace MistyInn.Core.Persistence;

/// <summary>
/// Where saves are kept: named text files in one folder (section 5.1). The rules about how they are written (SAV-05)
/// and quarantined (SAV-06) are in <see cref="SaveFiles"/>, so another store (a cloud store, section 24) only has to
/// move bytes.
/// </summary>
public interface ISaveStore
{
    bool Exists(string name);

    /// <exception cref="FileNotFoundException">There is no such file.</exception>
    string ReadText(string name);

    /// <summary>Creates or overwrites <paramref name="name"/>.</summary>
    void WriteText(string name, string text);

    /// <summary>
    /// Replaces <paramref name="destination"/> with <paramref name="source"/> in one step, keeping the old destination as
    /// <paramref name="backup"/> (overwriting any older backup). With no destination yet, <paramref name="source"/> is
    /// just renamed.
    /// </summary>
    void Replace(string source, string destination, string backup);

    /// <summary>Renames a file; <paramref name="to"/> must not exist.</summary>
    void Move(string from, string to);

    /// <summary>Deletes a file if it exists.</summary>
    void Delete(string name);
}

/// <summary>
/// A store in a folder on disk. The game uses it on <c>user://saves</c> (D-095); the tests use it on a temporary folder
/// (TST-01).
/// </summary>
public sealed class FolderSaveStore : ISaveStore
{
    private readonly string _folder;

    public FolderSaveStore(string folder)
    {
        ArgumentException.ThrowIfNullOrEmpty(folder);
        _folder = folder;
    }

    public bool Exists(string name) => File.Exists(PathOf(name));

    public string ReadText(string name) => File.ReadAllText(PathOf(name));

    public void WriteText(string name, string text)
    {
        Directory.CreateDirectory(_folder);
        using var stream = new FileStream(PathOf(name), FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream);
        writer.Write(text);
        writer.Flush();

        // On disk before the replace, so a crash never leaves a renamed but empty file.
        stream.Flush(flushToDisk: true);
    }

    public void Replace(string source, string destination, string backup)
    {
        if (File.Exists(PathOf(destination)))
        {
            File.Replace(PathOf(source), PathOf(destination), PathOf(backup));
        }
        else
        {
            File.Move(PathOf(source), PathOf(destination));
        }
    }

    public void Move(string from, string to) => File.Move(PathOf(from), PathOf(to));

    public void Delete(string name) => File.Delete(PathOf(name));

    private string PathOf(string name)
    {
        if (name.IndexOfAny(['/', '\\']) >= 0 || name is "." or "..")
        {
            throw new ArgumentException($"'{name}' is not a plain file name.", nameof(name));
        }

        return Path.Combine(_folder, name);
    }
}
