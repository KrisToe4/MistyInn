namespace MistyInn.Core.Run;

/// <summary>
/// The characters whose hidden ancestry has been revealed by observation (STF-16). Like the bestiary (D-066) it lives
/// outside the session and is kept across runs: pass the same instance to each new session; step 8 saves it with the
/// profile (MET-01, D-082).
/// </summary>
public sealed class AncestryRecord
{
    private readonly HashSet<string> _revealed;

    public AncestryRecord(IEnumerable<string>? revealed = null)
    {
        _revealed = new HashSet<string>(revealed ?? Array.Empty<string>());
    }

    /// <summary>The character IDs with a revealed ancestry, sorted (for saves and the state hash).</summary>
    public IReadOnlyList<string> Revealed => _revealed.Order(StringComparer.Ordinal).ToList();

    public bool IsRevealed(string characterId) => _revealed.Contains(characterId);

    /// <summary>Marks a character's ancestry as revealed; false if it already was.</summary>
    internal bool Reveal(string characterId) => _revealed.Add(characterId);
}
