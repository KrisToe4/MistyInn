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
    private readonly Modifiers _modifiers;

    public ArrivalSystem(
        GameContent content, RunState run, IRandomSource random, IdAllocator ids, IEventSink sink,
        CreatureBehaviourSystem behaviour, BestiaryRecorder bestiary, Modifiers modifiers)
    {
        _modifiers = modifiers;
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

    /// <summary>
    /// The cycle's average arrival interval (section 6.4): ×0.92 per cycle, ×0.6 on the Convergence (RUN-02), divided by
    /// the <c>ArrivalRate</c> multipliers and by the planes' mean <c>PlaneWeight</c>, so a plane at weight 2 sends twice
    /// as many creatures while the others send as many as before (D-087).
    /// </summary>
    public double IntervalSeconds
    {
        get
        {
            var b = _content.Balance;
            var interval = b.BaseArrivalIntervalSeconds * Math.Pow(b.ArrivalIntervalPerCycle, _run.Cycle - 1);
            if (_run.Cycle == b.RunLength)
            {
                interval *= b.ConvergenceArrivalMultiplier;
            }

            var meanWeight = _run.ConvergingPlanes.Count == 0 ? 1 : _run.ConvergingPlanes.Average(p => _modifiers.PlaneWeight(p.Id));
            return interval / _modifiers.InnWideMultiplier(EffectKind.ArrivalRate) / meanWeight;
        }
    }

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

    /// <summary>
    /// The plane an arrival comes from: uniform, or by <c>PlaneWeight</c> when an effect (a Surge) weights one (D-087).
    /// </summary>
    internal PlaneDef PickPlane()
    {
        var planes = _run.ConvergingPlanes;
        var weights = planes.Select(p => _modifiers.PlaneWeight(p.Id)).ToList();
        if (weights.All(w => w == 1))
        {
            return planes[_random.NextInt(0, planes.Count)];
        }

        var roll = _random.NextDouble() * weights.Sum();
        for (var i = 0; i < planes.Count; i++)
        {
            roll -= weights[i];
            if (roll < 0)
            {
                return planes[i];
            }
        }

        return planes[^1];
    }

    private void TrySpawn(double now)
    {
        if (_run.Creatures.Count >= _content.Balance.MaxCreatures)
        {
            _sink.Log("arrival.skipped", "board.full");
            return;
        }

        var plane = PickPlane();
        var def = _content.Creature(PickWeighted(plane.Creatures).Creature);
        var creature = new Creature(_ids.NextCreature(), def, _content.GateFor(plane.Id));
        var arrival = _modifiers.InnWide(EffectKind.ArrivalAgitation, plane.Id);
        if (arrival != 0)
        {
            // Inn items such as the Drowned Bell (ITM-06); a creature never arrives already turned.
            creature.Agitation = Math.Clamp(creature.Agitation + arrival, 0, 99);
        }

        _run.Creatures.Add(creature);
        _sink.Raise(new CreatureArrived(_sink.Time, creature.Id, def.Id, plane.Id));
        _sink.Log("creature.arrived", $"gate.{plane.Id}", creature.Id.ToRef());
        _bestiary.Seen(def);
        _behaviour.Decide(creature, now, arriving: true);
    }
}
