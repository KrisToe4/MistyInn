using MistyInn.Core.Board;

namespace MistyInn.Core.Run;

/// <summary>Walking along a path of tiles, shared by creatures and units (SIM-03).</summary>
internal static class Walking
{
    /// <summary>Replaces the path; the first tile (where the walker stands) is skipped.</summary>
    public static void SetPath(Queue<Tile> queue, IReadOnlyList<Tile> path)
    {
        queue.Clear();
        foreach (var tile in path.Skip(1))
        {
            queue.Enqueue(tile);
        }
    }

    /// <summary>Walks up to <paramref name="distance"/> tiles along the path and returns the new position.</summary>
    public static TilePosition Advance(TilePosition position, Queue<Tile> path, double distance)
    {
        while (distance > 0 && path.Count > 0)
        {
            var next = TilePosition.Center(path.Peek());
            var gap = position.DistanceTo(next);
            if (gap <= distance)
            {
                position = next;
                distance -= gap;
                path.Dequeue();
            }
            else
            {
                position = position.MoveTowards(next, distance);
                distance = 0;
            }
        }

        return position;
    }
}
