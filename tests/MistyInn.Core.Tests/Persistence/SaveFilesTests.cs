using MistyInn.Core.Commands;
using MistyInn.Core.Persistence;
using MistyInn.Core.Run;
using static MistyInn.Core.Tests.TestGame;

namespace MistyInn.Core.Tests.Persistence;

/// <summary>The save files on disk: atomic writes, backups and quarantine (SAV-04 to SAV-06, TST-08), in a temporary folder (TST-01).</summary>
public class SaveFilesTests
{
    private DirectoryInfo _folder = null!;
    private FolderSaveStore _store = null!;
    private SaveFiles _files = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = Directory.CreateTempSubdirectory("mistyinn-saves-");
        _store = new FolderSaveStore(_folder.FullName);
        _files = new SaveFiles(_store);
    }

    [TearDown]
    public void TearDown() => _folder.Delete(recursive: true);

    private string[] FileNames() => _folder.GetFiles().Select(f => f.Name).Order(StringComparer.Ordinal).ToArray();

    private static RunSave SaveAtCycle(int cycles)
    {
        var session = NewSession();
        for (var i = 1; i < cycles; i++)
        {
            PlayCycle(session);
        }

        return session.CreateSave(1);
    }

    [Test]
    [Property("Requirement", "SAV-05")]
    [Property("Requirement", "TST-08")]
    public void AWriteReplacesTheSaveAndKeepsThePreviousOneAsBak()
    {
        _files.SaveRun(SaveAtCycle(1));
        Assert.That(FileNames(), Is.EqualTo(new[] { "run.json" }), "no .tmp is left behind");

        _files.SaveRun(SaveAtCycle(2));

        Assert.That(FileNames(), Is.EqualTo(new[] { "run.bak", "run.json" }));
        Assert.That(_files.LoadRun().Value!.Cycle, Is.EqualTo(2));
        Assert.That(SaveCodec.ReadRun(_store.ReadText("run.bak")).Cycle, Is.EqualTo(1), "the previous file");
    }

    [Test]
    [Property("Requirement", "SAV-05")]
    [Property("Requirement", "INV-07")]
    public void AWriteThatFailsBeforeTheReplaceLeavesTheOldSaveWhole()
    {
        _files.SaveRun(SaveAtCycle(1));
        var failing = new SaveFiles(new FailingReplaceStore(_store));

        Assert.That(() => failing.SaveRun(SaveAtCycle(2)), Throws.TypeOf<IOException>());

        Assert.That(_files.LoadRun().Value!.Cycle, Is.EqualTo(1), "run.json is untouched");
        _files.SaveRun(SaveAtCycle(2));
        Assert.That(FileNames(), Is.EqualTo(new[] { "run.bak", "run.json" }), "the next write tidies the stray .tmp away");
    }

    [Test]
    [Property("Requirement", "SAV-06")]
    [Property("Requirement", "INV-07")]
    [Property("Requirement", "TST-08")]
    public void ACorruptSaveIsQuarantinedAndTheGameGoesOnWithoutIt()
    {
        _store.WriteText("run.json", "{ \"schemaVersion\": 1, \"cycle\": ");

        var first = _files.LoadRun();

        Assert.That(first.Status, Is.EqualTo(LoadStatus.Corrupt));
        Assert.That(first.QuarantinedAs, Is.EqualTo("run.corrupt-1.json"));
        Assert.That(first.Message, Does.Contain("run.corrupt-1.json"), "the player is told");
        Assert.That(_files.HasRun, Is.False, "Continue is gone");
        Assert.That(_files.LoadRun().Status, Is.EqualTo(LoadStatus.Missing));

        _store.WriteText("profile.json", "not a profile");
        _store.WriteText("run.json", "[]");
        Assert.That(_files.LoadProfile().Status, Is.EqualTo(LoadStatus.Corrupt));
        Assert.That(_files.LoadRun().QuarantinedAs, Is.EqualTo("run.corrupt-2.json"), "an earlier quarantined file is never overwritten");
        Assert.That(FileNames(), Is.EqualTo(new[] { "profile.corrupt-1.json", "run.corrupt-1.json", "run.corrupt-2.json" }));
    }

    [Test]
    [Property("Requirement", "SAV-06")]
    [Property("Requirement", "SAV-02")]
    public void AProfileWithImpossibleValuesIsQuarantinedLikeACorruptOne()
    {
        var damaged = ProfileSaves.Capture(new(), new(), new(), contentNoteAcknowledged: true) with { Essence = -5 };
        _files.SaveProfile(damaged);

        var loaded = _files.LoadProfile();
        Assert.That(loaded.Status, Is.EqualTo(LoadStatus.Loaded), "it is valid JSON");
        Assert.That(() => ProfileSaves.Restore(loaded.Value!),
            Throws.TypeOf<SaveException>().With.Property(nameof(SaveException.Problem)).EqualTo(SaveProblem.Corrupt));

        Assert.That(_files.QuarantineProfile(), Is.EqualTo("profile.corrupt-1.json"));
        Assert.That(_files.LoadProfile().Status, Is.EqualTo(LoadStatus.Missing), "the game goes on with a new profile");
    }

    [Test]
    [Property("Requirement", "SAV-04")]
    [Property("Requirement", "INV-07")]
    public void ANewerSaveIsNotLoadedAndIsLeftWhereItIs()
    {
        var text = SaveCodec.Write(SaveAtCycle(1)).Replace("\"schemaVersion\": 1", "\"schemaVersion\": 99", StringComparison.Ordinal);
        _store.WriteText("run.json", text);

        var result = _files.LoadRun();

        Assert.That(result.Status, Is.EqualTo(LoadStatus.TooNew));
        Assert.That(result.Message, Does.Contain("newer version"));
        Assert.That(FileNames(), Is.EqualTo(new[] { "run.json" }));
        Assert.That(_store.ReadText("run.json"), Is.EqualTo(text));
    }

    [Test]
    [Property("Requirement", "SAV-06")]
    [Property("Requirement", "SAV-02")]
    public void TheRunSaveIsDeletedWhenTheRunEndsAndTheProfileStays()
    {
        _files.SaveRun(SaveAtCycle(1));
        _files.SaveRun(SaveAtCycle(1));
        _files.SaveProfile(ProfileSaves.Capture(new(), new(), new(), contentNoteAcknowledged: false));

        _files.DeleteRun();

        Assert.That(_files.HasRun, Is.False);
        Assert.That(FileNames(), Is.EqualTo(new[] { "profile.json" }));
        Assert.That(_files.LoadProfile().Status, Is.EqualTo(LoadStatus.Loaded));
    }

    [Test]
    [Property("Requirement", "SAV-03")]
    public void ContinueAfterARestartResumesTheSavedDusk()
    {
        var session = NewSession(seed: 9);
        PlayCycle(session);
        _files.SaveRun(session.CreateSave(9));

        var loaded = _files.LoadRun();
        var restored = GameSession.Restore(loaded.Value!, session.Content);

        Assert.That(loaded.Status, Is.EqualTo(LoadStatus.Loaded));
        Assert.That((restored.Cycle, restored.Phase, restored.Silver), Is.EqualTo((2, Phase.Dusk, session.Silver)));
        Assert.That(restored.Execute(new BeginMist()).Succeeded, Is.True, "the innkeeper is still in place");
    }

    [Test]
    [Property("Requirement", "SAV-05")]
    public void TheStoreOnlyTakesPlainFileNames()
    {
        Assert.That(() => _store.WriteText("../run.json", "{}"), Throws.ArgumentException);
        Assert.That(() => _store.Exists("saves\\run.json"), Throws.ArgumentException);
    }

    /// <summary>A store whose replace step fails, as if the game stopped between writing <c>*.tmp</c> and replacing.</summary>
    private sealed class FailingReplaceStore(ISaveStore inner) : ISaveStore
    {
        public bool Exists(string name) => inner.Exists(name);

        public string ReadText(string name) => inner.ReadText(name);

        public void WriteText(string name, string text) => inner.WriteText(name, text);

        public void Replace(string source, string destination, string backup) => throw new IOException("The disk went away.");

        public void Move(string from, string to) => inner.Move(from, to);

        public void Delete(string name) => inner.Delete(name);
    }
}
