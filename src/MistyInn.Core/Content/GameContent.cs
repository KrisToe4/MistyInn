using MistyInn.Core.Board;

namespace MistyInn.Core.Content;

/// <summary>A plane whose mist can reach the inn (PLN-01). <paramref name="MistColor"/> is an HTML colour (#rrggbb).</summary>
public sealed record PlaneDef(string Id, string Name, string MistColor);

/// <summary>A kind of creature (CRE-03). Step 2 needs only its plane and walking speed (tiles per second).</summary>
public sealed record CreatureDef(string Id, string Name, string PlaneId, double Speed);

/// <summary>A staff character (STF-01). Step 2 needs only the name and whether it is the innkeeper.</summary>
public sealed record StaffDef(string Id, string Name, bool IsInnkeeper);

/// <summary>Tuning values (section 6.4).</summary>
/// <param name="PlanesPerCycle">Converging planes for cycles 1, 2, 3...; the last entry repeats; 0 means all unlocked.</param>
public sealed record BalanceDef(
    double MistDurationSeconds,
    double ArrivalStopBeforeDawnSeconds,
    double BaseArrivalIntervalSeconds,
    double ArrivalIntervalPerCycle,
    double ArrivalJitterMin,
    double ArrivalJitterMax,
    int MaxCreatures,
    int BaseStaffSlots,
    int MaxStaffSlots,
    int BenchSize,
    IReadOnlyList<int> PlanesPerCycle,
    double IdleMinSeconds,
    double IdleMaxSeconds);

/// <summary>
/// Everything Core needs to know about the game's content. Step 2 builds it from <see cref="DefaultContent"/>; step 3
/// loads it from <c>data/*.json</c> and adds the full validator (DAT-04). This constructor checks only what the
/// simulation relies on.
/// </summary>
public sealed class GameContent
{
    public GameContent(
        BoardLayout layout,
        IReadOnlyList<PlaneDef> planes,
        IReadOnlyList<CreatureDef> creatures,
        IReadOnlyList<StaffDef> startingStaff,
        IReadOnlyList<string> unlockedPlanes,
        IReadOnlyList<string> wanderZones,
        BalanceDef balance)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Planes = planes ?? throw new ArgumentNullException(nameof(planes));
        Creatures = creatures ?? throw new ArgumentNullException(nameof(creatures));
        StartingStaff = startingStaff ?? throw new ArgumentNullException(nameof(startingStaff));
        UnlockedPlanes = unlockedPlanes ?? throw new ArgumentNullException(nameof(unlockedPlanes));
        WanderZones = wanderZones ?? throw new ArgumentNullException(nameof(wanderZones));
        Balance = balance ?? throw new ArgumentNullException(nameof(balance));

        if (planes.Count == 0 || planes.Count > layout.Gates.Count)
        {
            throw new ArgumentException($"Need 1 to {layout.Gates.Count} planes (one gate each); got {planes.Count}.");
        }

        foreach (var plane in planes.Where(p => creatures.All(c => c.PlaneId != p.Id)))
        {
            throw new ArgumentException($"Plane '{plane.Id}' has no creatures.");
        }

        if (unlockedPlanes.Count == 0 || unlockedPlanes.Any(id => planes.All(p => p.Id != id)))
        {
            throw new ArgumentException("Unlocked planes must be a non-empty set of known plane IDs.");
        }

        if (startingStaff.Count(s => s.IsInnkeeper) != 1)
        {
            throw new ArgumentException("The starting staff must include exactly one innkeeper.");
        }

        if (wanderZones.Count == 0 || wanderZones.Any(z => !layout.ZoneIds.Contains(z)))
        {
            throw new ArgumentException("Wander zones must be a non-empty set of zones in the layout.");
        }

        if (balance.PlanesPerCycle.Count == 0)
        {
            throw new ArgumentException("PlanesPerCycle needs at least one entry.");
        }
    }

    public BoardLayout Layout { get; }

    public IReadOnlyList<PlaneDef> Planes { get; }

    public IReadOnlyList<CreatureDef> Creatures { get; }

    public IReadOnlyList<StaffDef> StartingStaff { get; }

    public IReadOnlyList<string> UnlockedPlanes { get; }

    /// <summary>Where creatures wander when nothing calls to them (CRE-04).</summary>
    public IReadOnlyList<string> WanderZones { get; }

    public BalanceDef Balance { get; }

    public PlaneDef Plane(string id) =>
        Planes.FirstOrDefault(p => p.Id == id) ?? throw new KeyNotFoundException($"Unknown plane '{id}'.");

    /// <summary>The gate a plane's creatures walk out of: planes take the gates in order (PLN-04).</summary>
    public Tile GateFor(string planeId)
    {
        for (var i = 0; i < Planes.Count; i++)
        {
            if (Planes[i].Id == planeId)
            {
                return Layout.Gates[i];
            }
        }

        throw new KeyNotFoundException($"Unknown plane '{planeId}'.");
    }
}
