using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Randomness;
using MistyInn.Core.Run;

namespace MistyInn.Core.Simulation;

/// <summary>
/// Where creatures go (CRE-04, SIM-03): each walks to the best reachable spot where one of its appetites can be
/// served, waits there, and looks again if nobody serves it; with nothing to seek it wanders. Restless and turned
/// creatures wander the whole inn (AGI-03).
/// </summary>
internal sealed class CreatureBehaviourSystem
{
    private readonly GameContent _content;
    private readonly RunState _run;
    private readonly IRandomSource _random;
    private readonly IReadOnlyList<Tile> _wanderTiles;
    private readonly IReadOnlyList<Tile> _innTiles;
    private readonly ServiceSystem _service;
    private readonly InnSense _sense;

    public CreatureBehaviourSystem(
        GameContent content, RunState run, IRandomSource random, IReadOnlyList<Tile> wanderTiles, ServiceSystem service, InnSense sense)
    {
        _content = content;
        _run = run;
        _random = random;
        _wanderTiles = wanderTiles;
        var rooms = content.Layout.Rooms.Select(r => r.Zone).ToHashSet();
        _innTiles = content.Layout.AllTiles()
            .Where(t => content.Layout.KindAt(t) == TileKind.Floor && !rooms.Contains(content.Layout.ZoneAt(t)))
            .ToList();
        _service = service;
        _sense = sense;
    }

    public void Step(double stepSeconds, double now)
    {
        foreach (var creature in _run.Creatures)
        {
            creature.PreviousPosition = creature.Position;

            // Turned creatures fight (CombatSystem); pairs going to a room walk with the companion (CompanySystem).
            if (creature.IsTurned || creature.Activity is CreatureActivity.GoingToRoom or CreatureActivity.InRoom)
            {
                continue;
            }

            switch (creature.Activity)
            {
                case CreatureActivity.Arriving:
                case CreatureActivity.Seeking:
                case CreatureActivity.Wandering:
                    creature.Advance(creature.Def.Speed * stepSeconds);
                    if (creature.Path.Count == 0)
                    {
                        Arrive(creature, now);
                    }

                    break;

                case CreatureActivity.Waiting:
                    if (creature.ServedBy is not null)
                    {
                        creature.UnservedSince = now;
                    }
                    else if (now - creature.UnservedSince >= _content.Balance.Agitation.ReseekAfterSeconds)
                    {
                        Decide(creature, now, arriving: false);
                    }

                    break;

                case CreatureActivity.Idle when now >= creature.IdleUntil:
                    Decide(creature, now, arriving: false);
                    break;

                case CreatureActivity.Leaving:
                    creature.Advance(creature.Def.Speed * stepSeconds);
                    creature.HasLeft = creature.Path.Count == 0;
                    break;
            }
        }
    }

    /// <summary>Chooses where a creature goes next: a service spot, or a wander target (CRE-04).</summary>
    public void Decide(Creature creature, double now, bool arriving)
    {
        Release(creature);
        var from = creature.Position.NearestTile;

        if (!creature.IsTurned)
        {
            foreach (var spot in SpotsFor(creature))
            {
                var path = _run.Board.FindPath(from, spot);
                if (path is null)
                {
                    continue;
                }

                creature.Spot = spot;
                _run.Reservations[spot] = creature.Id;
                creature.SetPath(path);
                creature.Activity = arriving ? CreatureActivity.Arriving : CreatureActivity.Seeking;
                if (path.Count <= 1)
                {
                    Arrive(creature, now);
                }

                return;
            }
        }

        var pool = creature.State >= CreatureState.Restless ? _innTiles : _wanderTiles;
        var target = pool[_random.NextInt(0, pool.Count)];
        var wander = _run.Board.FindPath(from, target);
        if (wander is { Count: > 1 })
        {
            creature.SetPath(wander);
            creature.Activity = arriving ? CreatureActivity.Arriving : CreatureActivity.Wandering;
        }
        else
        {
            creature.Activity = CreatureActivity.Idle;
            creature.IdleUntil = now + _content.Balance.IdleMinSeconds;
        }
    }

    /// <summary>A satisfied guest gives up its spot and walks out of the front door (CRE-08).</summary>
    public void SendOut(Creature creature)
    {
        Release(creature);
        creature.CompanyWith = null;
        creature.ServedBy = null;
        creature.Activity = CreatureActivity.Leaving;
        var path = _run.Board.FindPath(creature.Position.NearestTile, _content.Layout.FrontDoor);
        creature.SetPath(path ?? []);
        creature.HasLeft = path is null || path.Count <= 1;
    }

    public void Release(Creature creature)
    {
        if (creature.Spot is { } spot && _run.Reservations.TryGetValue(spot, out var holder) && holder == creature.Id)
        {
            _run.Reservations.Remove(spot);
        }

        creature.Spot = null;
    }

    private void Arrive(Creature creature, double now)
    {
        if (creature.Spot is { } spot && creature.Position.NearestTile == spot)
        {
            creature.Activity = CreatureActivity.Waiting;
            creature.UnservedSince = now;
            return;
        }

        Release(creature);
        creature.Activity = CreatureActivity.Idle;
        var b = _content.Balance;
        creature.IdleUntil = now + b.IdleMinSeconds + (b.IdleMaxSeconds - b.IdleMinSeconds) * _random.NextDouble();
    }

    /// <summary>
    /// The spots where something could serve this creature, best first: a stronger appetite first (with the preferred
    /// technique bonus), then the nearest, then map order (D-064).
    /// </summary>
    private IEnumerable<Tile> SpotsFor(Creature creature)
    {
        var candidates = new List<(Tile Tile, double Score)>();
        var staff = _run.AvailableStaff.ToList();

        foreach (var appetite in creature.Def.Appetites)
        {
            var strength = _service.StrengthOf(appetite);

            foreach (var (fixture, _) in _sense.Working(EffectKind.Provides).Where(w => w.Effect.Appetite == appetite.Appetite))
            {
                candidates.AddRange(fixture.Seats.Where(s => IsFree(s, creature)).Select(s => (s, strength)));
            }

            foreach (var (fixture, effect) in _sense.Working(EffectKind.Enables))
            {
                var technique = _content.Technique(effect.Technique!);
                if (!technique.NeedsFixture || !technique.Answers.Contains(appetite.Appetite)
                    || !staff.Any(u => u.Techniques.Contains(technique)
                        && (u.IsRunner || InnSense.DistanceToTiles(TilePosition.Center(u.Tile!.Value), fixture.Tiles) <= technique.Range + 0.5)))
                {
                    continue;
                }

                var score = strength * (appetite.PreferredTechnique == technique.Id ? _content.Balance.Agitation.PreferredBonus : 1);
                candidates.AddRange(fixture.Seats.Where(s => IsFree(s, creature)).Select(s => (s, score)));
            }

            foreach (var unit in staff)
            {
                foreach (var technique in unit.Techniques.Where(t => !t.NeedsFixture && t.Answers.Contains(appetite.Appetite)))
                {
                    if (technique.Technique == Technique.Company
                        && (!(creature.Def.Sapient && creature.Def.Adult) || ServiceSystem.Refuses(unit, creature.Def)))
                    {
                        continue;
                    }

                    var score = strength * (appetite.PreferredTechnique == technique.Id ? _content.Balance.Agitation.PreferredBonus : 1);
                    candidates.AddRange(TilesInRange(unit.Tile!.Value, technique.Range).Where(s => IsFree(s, creature)).Select(s => (s, score)));
                }
            }
        }

        // Nothing for its appetites: a free seat at a table, where anyone can bring basic service (CRE-04, AGI-11).
        if (candidates.Count == 0)
        {
            foreach (var (fixture, effect) in _sense.Working(EffectKind.Enables)
                         .Where(w => _content.Technique(w.Effect.Technique!).Technique == Technique.Food))
            {
                candidates.AddRange(fixture.Seats.Where(s => IsFree(s, creature)).Select(s => (s, 0.0)));
            }
        }

        return candidates
            .OrderByDescending(c => c.Score)
            .ThenBy(c => InnSense.Distance(creature.Position, c.Tile))
            .ThenBy(c => c.Tile.Y)
            .ThenBy(c => c.Tile.X)
            .Select(c => c.Tile)
            .Distinct()
            .Take(12);
    }

    private IEnumerable<Tile> TilesInRange(Tile centre, double range)
    {
        var r = (int)Math.Ceiling(range);
        for (var y = centre.Y - r; y <= centre.Y + r; y++)
        {
            for (var x = centre.X - r; x <= centre.X + r; x++)
            {
                var tile = new Tile(x, y);
                if (tile != centre && InnSense.Distance(TilePosition.Center(centre), tile) <= range)
                {
                    yield return tile;
                }
            }
        }
    }

    private bool IsFree(Tile tile, Creature creature) =>
        _run.Board.IsPlaceableFloor(tile)
        && _run.Board.UnitAt(tile) is null
        && (!_run.Reservations.TryGetValue(tile, out var holder) || holder == creature.Id);
}
