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

    /// <summary>
    /// The effects of working (placed, unbroken) fixtures, of one kind, in fixture order: their own, and those of the
    /// fixtures they count as through inn items (<c>CountsAs</c>, ITM-06). An effect with an adjacency rule works only
    /// while that rule holds (FIX-03, D-079).
    /// </summary>
    public IEnumerable<(Fixture Fixture, EffectDef Effect)> Working(EffectKind kind) =>
        _run.PlacedFixtures
            .Where(f => !f.IsBroken)
            .SelectMany(f => EffectsOf(f).Where(e => e.Kind == kind && (e.AdjacentTo is null || IsAdjacentTo(f, e.AdjacentTo))).Select(e => (f, e)));

    /// <summary>
    /// The sum of the amounts of working <paramref name="kind"/> effects whose fixtures stand next to
    /// <paramref name="target"/> and name its type (FIX-03): for example a Lantern adding a zone to the Stage's reach.
    /// </summary>
    public int AdjacentAmount(Fixture target, EffectKind kind) =>
        (int)Working(kind)
            .Where(w => w.Fixture != target && w.Effect.AdjacentTo == target.Def.Id && AreAdjacent(w.Fixture.Tiles, target.Tiles))
            .Sum(w => w.Effect.Amount ?? 0);

    /// <summary>The door attacks a room's session is shielded from this mist: Ward circles next to its door (FIX-03, D-079).</summary>
    public int ShieldsFor(Room room) =>
        (int)Working(EffectKind.Blocks)
            .Where(w => w.Effect.AdjacentTo == EffectDef.AdjacentToRoomDoor && AreAdjacent(w.Fixture.Tiles, new[] { room.Door }))
            .Sum(w => w.Effect.Amount ?? 1);

    /// <summary>Whether two sets of tiles touch, counting diagonals (the 8 neighbours of FIX-03).</summary>
    public static bool AreAdjacent(IReadOnlyList<Tile> a, IReadOnlyList<Tile> b) =>
        a.Any(t => b.Any(u => t != u && Math.Abs(t.X - u.X) <= 1 && Math.Abs(t.Y - u.Y) <= 1));

    private IEnumerable<EffectDef> EffectsOf(Fixture fixture)
    {
        var effects = fixture.Def.Effects.AsEnumerable();
        foreach (var countsAs in _run.InnItems.SelectMany(i => i.Def.Effects)
                     .Where(e => e.Kind == EffectKind.CountsAs && e.Fixture == fixture.Def.Id))
        {
            effects = effects.Concat(_content.Fixture(countsAs.CountsAs!).Effects);
        }

        return effects;
    }

    private bool IsAdjacentTo(Fixture fixture, string adjacentTo) =>
        adjacentTo == EffectDef.AdjacentToRoomDoor
            ? _run.Rooms.Any(r => AreAdjacent(fixture.Tiles, new[] { r.Door }))
            : _run.PlacedFixtures.Any(f => f != fixture && !f.IsBroken && f.Def.Id == adjacentTo && AreAdjacent(fixture.Tiles, f.Tiles));

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
