using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Randomness;
using MistyInn.Core.Run;

namespace MistyInn.Core.Simulation;

/// <summary>
/// Brings creatures out of the mist (CRE-01, CRE-02, CYC-03). Each one walks from its plane's gate, through the front
/// door, to a spot in the common room (CRE-04).
/// </summary>
internal sealed class ArrivalSystem
{
    private readonly GameContent _content;
    private readonly RunState _run;
    private readonly IRandomSource _random;
    private readonly IdAllocator _ids;
    private readonly IEventSink _sink;

    public ArrivalSystem(GameContent content, RunState run, IRandomSource random, IdAllocator ids, IEventSink sink)
    {
        _content = content;
        _run = run;
        _random = random;
        _ids = ids;
        _sink = sink;
        WanderTiles = run.Board.WalkableTilesIn(content.WanderZones);
        if (WanderTiles.Count == 0)
        {
            throw new ArgumentException("The wander zones have no walkable tiles.", nameof(content));
        }
    }

    /// <summary>Where creatures go when nothing calls to them, in map order.</summary>
    public IReadOnlyList<Tile> WanderTiles { get; }

    /// <summary>Game seconds of the next arrival attempt.</summary>
    public double NextArrivalSeconds { get; private set; }

    /// <summary>The cycle's average arrival interval (section 6.4).</summary>
    public double IntervalSeconds =>
        _content.Balance.BaseArrivalIntervalSeconds * Math.Pow(_content.Balance.ArrivalIntervalPerCycle, _run.Cycle - 1);

    public void StartMist() => NextArrivalSeconds = SampleInterval();

    public void Update(double now)
    {
        var lastArrival = _content.Balance.MistDurationSeconds - _content.Balance.ArrivalStopBeforeDawnSeconds;
        while (NextArrivalSeconds <= now && NextArrivalSeconds < lastArrival)
        {
            TrySpawn(now);
            NextArrivalSeconds += SampleInterval();
        }
    }

    private double SampleInterval()
    {
        var b = _content.Balance;
        var jitter = b.ArrivalJitterMin + (b.ArrivalJitterMax - b.ArrivalJitterMin) * _random.NextDouble();
        return IntervalSeconds * jitter;
    }

    /// <summary>A creature by the plane's weights (CRE-01).</summary>
    private CreatureWeightDef PickWeighted(IReadOnlyList<CreatureWeightDef> choices)
    {
        var roll = _random.NextDouble() * choices.Sum(c => c.Weight);
        foreach (var choice in choices)
        {
            roll -= choice.Weight;
            if (roll < 0)
            {
                return choice;
            }
        }

        return choices[^1];
    }

    private void TrySpawn(double now)
    {
        if (_run.Creatures.Count >= _content.Balance.MaxCreatures)
        {
            _sink.Log("arrival.skipped", "board.full");
            return;
        }

        var plane = _run.ConvergingPlanes[_random.NextInt(0, _run.ConvergingPlanes.Count)];
        var def = _content.Creature(PickWeighted(plane.Creatures).Creature);
        var gate = _content.GateFor(plane.Id);
        var target = WanderTiles[_random.NextInt(0, WanderTiles.Count)];

        var creature = new Creature(_ids.NextCreature(), def, gate);
        var path = _run.Board.FindPath(gate, target);
        if (path is null)
        {
            creature.Activity = CreatureActivity.Idle;
            creature.IdleUntil = now + _content.Balance.IdleMaxSeconds;
        }
        else
        {
            creature.SetPath(path);
        }

        _run.Creatures.Add(creature);
        _sink.Raise(new CreatureArrived(_sink.Time, creature.Id, def.Id, plane.Id));
        _sink.Log("creature.arrived", $"gate.{plane.Id}", creature.Id.ToRef());
    }
}
