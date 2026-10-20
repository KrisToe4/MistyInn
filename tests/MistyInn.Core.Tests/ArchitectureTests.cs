using System.Reflection;

namespace MistyInn.Core.Tests;

public class ArchitectureTests
{
    private static readonly Assembly Core = typeof(GameSession).Assembly;

    [Test]
    [Property("Requirement", "INV-01")]
    [Property("Requirement", "STR-02")]
    [Property("Requirement", "STR-05")]
    public void CoreReferencesNoGodotOrGameAssembly()
    {
        var references = Core.GetReferencedAssemblies().Select(a => a.Name ?? "").ToList();

        Assert.That(references, Has.None.StartsWith("Godot"));
        Assert.That(references, Has.None.EqualTo("GodotSharp"));
        Assert.That(references, Has.None.EqualTo("MistyInn"));
    }

    [Test]
    [Property("Requirement", "INV-01")]
    public void NoCoreTypeComesFromTheGodotNamespace()
    {
        var godotTypes = Core.GetTypes()
            .SelectMany(t => t.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(f => f.FieldType))
            .Where(t => t.Namespace?.StartsWith("Godot", StringComparison.Ordinal) == true)
            .ToList();

        Assert.That(godotTypes, Is.Empty);
    }

    [Test]
    [Property("Requirement", "INV-02")]
    public void CoreHoldsNoSystemRandom()
    {
        var randomFields = Core.GetTypes()
            .SelectMany(t => t.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(f => f.FieldType == typeof(System.Random))
            .Select(f => $"{f.DeclaringType?.Name}.{f.Name}")
            .ToList();

        Assert.That(randomFields, Is.Empty, "randomness goes through IRandomSource only");
    }

    [Test]
    [Property("Requirement", "STR-10")]
    public void NamespacesMirrorTheCoreRoot()
    {
        var outside = Core.GetTypes()
            .Where(t => t.Namespace is not null && !t.Namespace.StartsWith("MistyInn.Core", StringComparison.Ordinal))
            .Where(t => !t.Namespace!.StartsWith("Microsoft", StringComparison.Ordinal)
                && !t.Namespace.StartsWith("System", StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToList();

        Assert.That(outside, Is.Empty);
    }
}
