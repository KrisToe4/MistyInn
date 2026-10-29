namespace MistyInn.Core.Persistence;

/// <summary>What happened when a save was read (SAV-04, SAV-06, INV-07).</summary>
public enum LoadStatus
{
    /// <summary>There is no such save.</summary>
    Missing,

    Loaded,

    /// <summary>The file was not a valid save. It was moved to <see cref="LoadResult{T}.QuarantinedAs"/>.</summary>
    Corrupt,

    /// <summary>Written by a newer version of the game: not loaded, and left where it is.</summary>
    TooNew,
}

/// <summary>A save read from the store, or why not; <see cref="Message"/> is for the player.</summary>
public sealed record LoadResult<T>(LoadStatus Status, T? Value, string? Message, string? QuarantinedAs = null)
    where T : class
{
    public static LoadResult<T> Missing { get; } = new(LoadStatus.Missing, null, null);
}

/// <summary>
/// The save files (section 19) and how they are written and read: <c>run.json</c> and <c>profile.json</c>, each written
/// to <c>*.tmp</c> and then put in place in one step, keeping the previous file as <c>*.bak</c> (SAV-05). A file that
/// is not a valid save is moved to <c>*.corrupt-&lt;n&gt;.json</c> and the game goes on without it (SAV-06, INV-07); a
/// newer one is left alone (SAV-04). Never throws for a bad file; only the store's own failures (a full disk) escape.
/// </summary>
public sealed class SaveFiles
{
    public const string Run = "run";
    public const string Profile = "profile";

    private readonly ISaveStore _store;

    public SaveFiles(ISaveStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public static string FileName(string slot) => slot + ".json";

    public static string TempName(string slot) => slot + ".tmp";

    public static string BackupName(string slot) => slot + ".bak";

    public static string QuarantineName(string slot, int n) => $"{slot}.corrupt-{n}.json";

    /// <summary>Whether a run save exists (UI-01: Continue), valid or not.</summary>
    public bool HasRun => _store.Exists(FileName(Run));

    public void SaveRun(RunSave save) => Write(Run, SaveCodec.Write(save));

    public void SaveProfile(ProfileSave save) => Write(Profile, SaveCodec.Write(save));

    public LoadResult<RunSave> LoadRun() => Load(Run, text => SaveCodec.ReadRun(text));

    public LoadResult<ProfileSave> LoadProfile() => Load(Profile, text => SaveCodec.ReadProfile(text));

    /// <summary>The run is over (SAV-06): its save and backup go.</summary>
    public void DeleteRun()
    {
        _store.Delete(FileName(Run));
        _store.Delete(BackupName(Run));
        _store.Delete(TempName(Run));
    }

    /// <summary>Quarantines a run save that read as JSON but failed <see cref="RunSaves.Check"/> as corrupt (SAV-06).</summary>
    public string QuarantineRun() => Quarantine(Run);

    /// <summary>Quarantines a profile that read as JSON but failed <see cref="ProfileSaves.Restore"/> as damaged (SAV-06).</summary>
    public string QuarantineProfile() => Quarantine(Profile);

    private void Write(string slot, string text)
    {
        _store.WriteText(TempName(slot), text);
        _store.Replace(TempName(slot), FileName(slot), BackupName(slot));
    }

    private LoadResult<T> Load<T>(string slot, Func<string, T> read)
        where T : class
    {
        if (!_store.Exists(FileName(slot)))
        {
            return LoadResult<T>.Missing;
        }

        string text;
        try
        {
            text = _store.ReadText(FileName(slot));
        }
        catch (FileNotFoundException)
        {
            return LoadResult<T>.Missing;
        }

        try
        {
            return new LoadResult<T>(LoadStatus.Loaded, read(text), null);
        }
        catch (SaveException e) when (e.Problem == SaveProblem.TooNew)
        {
            return new LoadResult<T>(LoadStatus.TooNew, null, e.Message);
        }
        catch (SaveException e)
        {
            var moved = Quarantine(slot);
            return new LoadResult<T>(LoadStatus.Corrupt, null, $"{e.Message} It was moved to {moved}.", moved);
        }
    }

    private string Quarantine(string slot)
    {
        var n = 1;
        while (_store.Exists(QuarantineName(slot, n)))
        {
            n++;
        }

        _store.Move(FileName(slot), QuarantineName(slot, n));
        return QuarantineName(slot, n);
    }
}
