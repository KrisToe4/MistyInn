using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Run;

namespace MistyInn.Core.Simulation;

/// <summary>
/// What a creature or staff member can sense on the board: distances, working fixture effects, trigger sources and
/// zones (FIX-04: by effect kind, never by fixture ID).
/// </summary>
internal sealed class InnSense
{
    private readonly GameContent _content;
    private readonly RunState _run;
    private readonly Dictionary<string, HashSet<string>> _zoneNeighbours;

    public InnSense(GameContent content, RunState run)
    {
        _content = content;
        _run = run;
        _zoneNeighbours = BuildZoneNeighbours(content.Layout);
    }

    public static double Distance(TilePosition from, Tile to) => from.DistanceTo(TilePosition.Center(to));

    public static double DistanceToTiles(TilePosition from, IReadOnlyList<Tile> tiles) =>
        tiles.Count == 0 ? double.PositiveInfinity : tiles.Min(t => Distance(from, t));

    public static double DistanceBetween(IReadOnlyList<Tile> a, IReadOnlyList<Tile> b) =>
        a.Count == 0 || b.Count == 0 ? double.PositiveInfinity : a.Min(t => DistanceToTiles(TilePosition.Center(t), b));

    /// <summary>The effects of working (placed, unbroken) fixtures, of one kind, in fixture order.</summary>
    public IEnumerable<(Fixture Fixture, EffectDef Effect)> Working(EffectKind kind) =>
        _run.PlacedFixtures
            .Where(f => !f.IsBroken)
            .SelectMany(f => f.Def.Effects.Where(e => e.Kind == kind).Select(e => (f, e)));

    /// <summary>
    /// Whether a fixture-borne trigger (Light, Noise, Heat) reaches <paramref name="tiles"/> from within its radius
    /// plus <paramref name="extraRadius"/>, or the door's draught (Cold) does (AGI-08).
    /// </summary>
    public bool TriggerReaches(TriggerKind trigger, IReadOnlyList<Tile> tiles, double extraRadius = 0)
    {
        if (trigger == TriggerKind.Cold)
        {
            return _run.Phase == Phase.Mist
                   && DistanceBetween(tiles, new[] { _content.Layout.FrontDoor }) <= _content.Balance.Agitation.ColdRadius + extraRadius;
        }

        return Working(EffectKind.Emits).Any(w =>
            w.Effect.Trigger == trigger && DistanceBetween(tiles, w.Fixture.Tiles) <= (w.Effect.Radius ?? 0) + extraRadius);
    }

    public bool TriggerReaches(TriggerKind trigger, TilePosition position) =>
        TriggerReaches(trigger, new[] { position.NearestTile });

    public string ZoneOf(TilePosition position) => _content.Layout.ZoneAt(position.NearestTile);

    /// <summary>Whether an attack landed within <paramref name="radius"/> lately (AGI-08 <c>Violence</c>, CMP-05).</summary>
    public bool ViolenceNear(TilePosition position, double radius) =>
        _run.Attacks.Any(a => a.At.DistanceTo(position) <= radius);

    /// <summary>Whether a unit or creature fell within the blood radius this mist (AGI-08 <c>Blood</c>, ESC-04).</summary>
    public bool BloodNear(TilePosition position) =>
        _run.BloodMarks.Any(m => m.DistanceTo(position) <= _content.Balance.Combat.BloodRadius);

    /// <summary>A zone and the zones next to it, <paramref name="levels"/> steps out (the stage's reach, FIX-02).</summary>
    public IReadOnlySet<string> ZonesAround(string zone, int levels)
    {
        var result = new HashSet<string> { zone };
        var frontier = new List<string> { zone };
        for (var i = 0; i < levels; i++)
        {
            frontier = frontier.SelectMany(z => _zoneNeighbours.TryGetValue(z, out var n) ? n : new HashSet<string>())
                .Where(result.Add)
                .ToList();
        }

        return result;
    }

    private static Dictionary<string, HashSet<string>> BuildZoneNeighbours(BoardLayout layout)
    {
        var neighbours = new Dictionary<string, HashSet<string>>();
        foreach (var tile in layout.AllTiles())
        {
            if (!BoardLayout.IsWalkableKind(layout.KindAt(tile)))
            {
                continue;
            }

            var zone = layout.ZoneAt(tile);
            foreach (var next in new[] { new Tile(tile.X + 1, tile.Y), new Tile(tile.X, tile.Y + 1) })
            {
                var other = layout.ZoneAt(next);
                if (!BoardLayout.IsWalkableKind(layout.KindAt(next)) || other == zone || other.Length == 0)
                {
                    continue;
                }

                (neighbours.TryGetValue(zone, out var a) ? a : neighbours[zone] = new HashSet<string>()).Add(other);
                (neighbours.TryGetValue(other, out var b) ? b : neighbours[other] = new HashSet<string>()).Add(zone);
            }
        }

        return neighbours;
    }
}
