using MistyInn.Core.Board;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Events;
using MistyInn.Core.Randomness;
using MistyInn.Core.Run;

namespace MistyInn.Core.Simulation;

/// <summary>
/// Brings creatures out of the mist (CRE-01, CRE-02, CYC-03). Each one walks from its plane's gate, through the front
/// door, to the best spot for its appetites, or into the common room (CRE-04).
/// </summary>
internal sealed class ArrivalSystem
{
    private readonly GameContent _content;
    private readonly RunState _run;
    private readonly IRandomSource _random;
    private readonly IdAllocator _ids;
    private readonly IEventSink _sink;
    private readonly CreatureBehaviourSystem _behaviour;
    private readonly BestiaryRecorder _bestiary;

    public ArrivalSystem(
        GameContent content, RunState run, IRandomSource random, IdAllocator ids, IEventSink sink,
        CreatureBehaviourSystem behaviour, BestiaryRecorder bestiary)
    {
        _content = content;
        _run = run;
        _random = random;
        _ids = ids;
        _sink = sink;
        _behaviour = behaviour;
        _bestiary = bestiary;
    }

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
        var creature = new Creature(_ids.NextCreature(), def, _content.GateFor(plane.Id));

        _run.Creatures.Add(creature);
        _sink.Raise(new CreatureArrived(_sink.Time, creature.Id, def.Id, plane.Id));
        _sink.Log("creature.arrived", $"gate.{plane.Id}", creature.Id.ToRef());
        _bestiary.Seen(def);
        _behaviour.Decide(creature, now, arriving: true);
    }
}
