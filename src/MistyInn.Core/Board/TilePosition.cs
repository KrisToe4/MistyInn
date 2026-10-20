namespace MistyInn.Core.Board;

/// <summary>
/// A position in tile space (SIM-03): the centre of tile (x, y) is (x, y), so a unit between two tiles has a fractional
/// coordinate. Views turn this into world space and interpolate between steps; they never move units themselves.
/// </summary>
public readonly record struct TilePosition(double X, double Y)
{
    public static TilePosition Center(Tile tile) => new(tile.X, tile.Y);

    /// <summary>The tile this position is over.</summary>
    public Tile NearestTile => new((int)Math.Floor(X + 0.5), (int)Math.Floor(Y + 0.5));

    public double DistanceTo(TilePosition other)
    {
        var dx = other.X - X;
        var dy = other.Y - Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Moves up to <paramref name="maxDistance"/> towards <paramref name="target"/>, never past it.</summary>
    public TilePosition MoveTowards(TilePosition target, double maxDistance)
    {
        var distance = DistanceTo(target);
        if (distance <= maxDistance || distance == 0)
        {
            return target;
        }

        var f = maxDistance / distance;
        return new TilePosition(X + (target.X - X) * f, Y + (target.Y - Y) * f);
    }

    /// <summary>A point on the straight line from this position to <paramref name="to"/>, for interpolation.</summary>
    public TilePosition Lerp(TilePosition to, double t) => new(X + (to.X - X) * t, Y + (to.Y - Y) * t);
}
