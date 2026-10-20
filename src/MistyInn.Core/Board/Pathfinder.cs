namespace MistyInn.Core.Board;

/// <summary>
/// Grid A* in Core (section 4: no Godot navigation, INV-01). Eight directions; a diagonal step is allowed only when
/// both tiles beside it are walkable, so nothing cuts through a wall corner. Ties are broken by insertion order, so the
/// same board always gives the same path (INV-02).
/// </summary>
public static class Pathfinder
{
    private const int Straight = 10;
    private const int Diagonal = 14;

    private static readonly (int Dx, int Dy, int Cost)[] Directions =
    [
        (0, -1, Straight), (1, 0, Straight), (0, 1, Straight), (-1, 0, Straight),
        (1, -1, Diagonal), (1, 1, Diagonal), (-1, 1, Diagonal), (-1, -1, Diagonal),
    ];

    /// <summary>
    /// The cheapest path from <paramref name="start"/> to <paramref name="goal"/>, both included, or null when there is
    /// none.
    /// </summary>
    public static IReadOnlyList<Tile>? FindPath(int width, int height, Func<Tile, bool> isWalkable, Tile start, Tile goal)
    {
        ArgumentNullException.ThrowIfNull(isWalkable);

        bool InBounds(Tile t) => t.X >= 0 && t.Y >= 0 && t.X < width && t.Y < height;

        if (!InBounds(start) || !InBounds(goal) || !isWalkable(start) || !isWalkable(goal))
        {
            return null;
        }

        if (start == goal)
        {
            return new[] { start };
        }

        var size = width * height;
        var cost = new int[size];
        var cameFrom = new int[size];
        var closed = new bool[size];
        Array.Fill(cost, int.MaxValue);
        Array.Fill(cameFrom, -1);

        var startIndex = start.Y * width + start.X;
        var goalIndex = goal.Y * width + goal.X;
        var open = new PriorityQueue<int, (int F, int H, long Order)>();
        long order = 0;

        cost[startIndex] = 0;
        var startH = Heuristic(start, goal);
        open.Enqueue(startIndex, (startH, startH, order++));

        while (open.TryDequeue(out var current, out _))
        {
            if (closed[current])
            {
                continue;
            }

            if (current == goalIndex)
            {
                return Reconstruct(cameFrom, goalIndex, width);
            }

            closed[current] = true;
            var cx = current % width;
            var cy = current / width;

            foreach (var (dx, dy, stepCost) in Directions)
            {
                var next = new Tile(cx + dx, cy + dy);
                if (!InBounds(next) || !isWalkable(next))
                {
                    continue;
                }

                if (dx != 0 && dy != 0 && (!isWalkable(new Tile(cx + dx, cy)) || !isWalkable(new Tile(cx, cy + dy))))
                {
                    continue;
                }

                var nextIndex = next.Y * width + next.X;
                if (closed[nextIndex])
                {
                    continue;
                }

                var tentative = cost[current] + stepCost;
                if (tentative >= cost[nextIndex])
                {
                    continue;
                }

                cost[nextIndex] = tentative;
                cameFrom[nextIndex] = current;
                var h = Heuristic(next, goal);
                open.Enqueue(nextIndex, (tentative + h, h, order++));
            }
        }

        return null;
    }

    private static int Heuristic(Tile a, Tile b)
    {
        var dx = Math.Abs(a.X - b.X);
        var dy = Math.Abs(a.Y - b.Y);
        return Straight * (dx + dy) + (Diagonal - 2 * Straight) * Math.Min(dx, dy);
    }

    private static IReadOnlyList<Tile> Reconstruct(int[] cameFrom, int goalIndex, int width)
    {
        var path = new List<Tile>();
        for (var i = goalIndex; i != -1; i = cameFrom[i])
        {
            path.Add(new Tile(i % width, i / width));
        }

        path.Reverse();
        return path.AsReadOnly();
    }
}
