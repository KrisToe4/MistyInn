using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Randomness;
using MistyInn.Core.Run;

namespace MistyInn.Core.Simulation;

/// <summary>
/// Moves creatures along their A* paths at their speed (SIM-03), and lets them idle and wander the common room when
/// they have nowhere to be (CRE-04). Appetites (step 4) will give them better places to go.
/// </summary>
internal sealed class CreatureMovementSystem
{
    private readonly GameContent _content;
    private readonly RunState _run;
    private readonly IRandomSource _random;
    private readonly IReadOnlyList<Tile> _wanderTiles;

    public CreatureMovementSystem(GameContent content, RunState run, IRandomSource random, IReadOnlyList<Tile> wanderTiles)
    {
        _content = content;
        _run = run;
        _random = random;
        _wanderTiles = wanderTiles;
    }

    public void Step(double stepSeconds, double now)
    {
        foreach (var creature in _run.Creatures)
        {
            creature.PreviousPosition = creature.Position;

            switch (creature.Activity)
            {
                case CreatureActivity.Arriving:
                case CreatureActivity.Wandering:
                    creature.Advance(creature.Def.Speed * stepSeconds);
                    if (creature.Path.Count == 0)
                    {
                        creature.Activity = CreatureActivity.Idle;
                        creature.IdleUntil = now + IdleSeconds();
                    }

                    break;

                case CreatureActivity.Idle when now >= creature.IdleUntil:
                    var target = _wanderTiles[_random.NextInt(0, _wanderTiles.Count)];
                    var path = _run.Board.FindPath(creature.Position.NearestTile, target);
                    if (path is { Count: > 1 })
                    {
                        creature.SetPath(path);
                        creature.Activity = CreatureActivity.Wandering;
                    }
                    else
                    {
                        creature.IdleUntil = now + _content.Balance.IdleMinSeconds;
                    }

                    break;
            }
        }
    }

    private double IdleSeconds()
    {
        var b = _content.Balance;
        return b.IdleMinSeconds + (b.IdleMaxSeconds - b.IdleMinSeconds) * _random.NextDouble();
    }
}
