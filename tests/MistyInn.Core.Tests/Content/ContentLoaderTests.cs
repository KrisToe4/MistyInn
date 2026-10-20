using MistyInn.Core.Content;
using static MistyInn.Core.Tests.Content.ContentTestKit;

namespace MistyInn.Core.Tests.Content;

public class ContentLoaderTests
{
    private static ContentException LoadFailure(Dictionary<string, string?> overrides)
    {
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(new EditedSource(overrides)));
        return e!;
    }

    [Test]
    [Property("Requirement", "DAT-01")]
    [Property("Requirement", "DAT-02")]
    [Property("Requirement", "TST-07")]
    public void TheRealDataLoadsAndValidates()
    {
        var set = ContentLoader.Load(new EmbeddedContentSource());

        Assert.That(ContentValidator.Validate(set), Is.Empty);
        Assert.That(() => GameContent.Create(set), Throws.Nothing);
        Assert.That(set.Planes, Has.Count.EqualTo(5));
        Assert.That(set.Creatures, Has.Count.EqualTo(10));
        Assert.That(set.Techniques, Has.Count.EqualTo(13));
        Assert.That(set.Staff.Characters, Has.Count.EqualTo(16));
        Assert.That(set.Items, Has.Count.EqualTo(11));
        Assert.That(set.Fixtures, Has.Count.EqualTo(12));
    }

    [Test]
    [Property("Requirement", "DAT-01")]
    public void EveryContentFileIsBuiltIn()
    {
        foreach (var file in ContentLoader.FileNames)
        {
            Assert.That(() => new EmbeddedContentSource().ReadText(file), Throws.Nothing, file);
        }
    }

    [Test]
    [Property("Requirement", "DAT-02")]
    public void AMissingFileIsReported()
    {
        var e = LoadFailure(new() { [ContentLoader.Planes] = null });

        Assert.That(e.Errors, Is.EqualTo(new[] { "planes.json: the file is missing." }));
    }

    [Test]
    [Property("Requirement", "DAT-04")]
    public void BrokenJsonIsReported()
    {
        var e = LoadFailure(new() { [ContentLoader.Planes] = "[ { \"id\": " });

        Assert.That(e.Errors.Single(), Does.StartWith("planes.json:"));
    }

    [Test]
    [Property("Requirement", "DAT-04")]
    public void AnUnknownPropertyIsRejected()
    {
        var text = EditedSource.Embedded(ContentLoader.Creatures).Replace("\"tribute\": 3", "\"tribute\": 3, \"colour\": \"red\"");

        var e = LoadFailure(new() { [ContentLoader.Creatures] = text });

        Assert.That(e.Errors.Single(), Does.StartWith("creatures.json:").And.Contains("colour"));
    }

    [Test]
    [Property("Requirement", "DAT-03")]
    [Property("Requirement", "DAT-04")]
    public void AnUnknownEffectKindIsRejected()
    {
        var text = EditedSource.Embedded(ContentLoader.Fixtures).Replace("\"kind\": \"Blocks\"", "\"kind\": \"Teleport\"");

        var e = LoadFailure(new() { [ContentLoader.Fixtures] = text });

        Assert.That(e.Errors.Single(), Does.StartWith("fixtures.json:"));
    }

    [Test]
    [Property("Requirement", "DAT-03")]
    [Property("Requirement", "DAT-04")]
    public void KindsMustBeWrittenAsNamesNotNumbers()
    {
        var text = EditedSource.Embedded(ContentLoader.Fixtures).Replace("\"kind\": \"Blocks\"", "\"kind\": 12");

        var e = LoadFailure(new() { [ContentLoader.Fixtures] = text });

        Assert.That(e.Errors.Single(), Does.StartWith("fixtures.json:"));
    }

    [Test]
    [Property("Requirement", "DAT-04")]
    public void EveryBrokenFileIsReportedAtOnce()
    {
        var e = LoadFailure(new() { [ContentLoader.Planes] = "{", [ContentLoader.Items] = null });

        Assert.That(e.Errors, Has.Count.EqualTo(2));
        Assert.That(e.Message, Does.Contain("planes.json").And.Contain("items.json"));
    }
}
