using MistyInn.Core.Content;
using MistyInn.Core.Run;

namespace MistyInn.Core.Simulation;

/// <summary>A synergy as the setup UI shows it (SYN-01, SYN-03, UI-03).</summary>
/// <param name="ActiveThreshold">The highest threshold reached, or 0.</param>
/// <param name="NextThreshold">The next threshold not reached yet, or null.</param>
public sealed record SynergyStatus(
    string SynergyId,
    string Name,
    int Count,
    IReadOnlyList<int> Thresholds,
    int ActiveThreshold,
    int? NextThreshold)
{
    public bool IsActive => ActiveThreshold > 0;

    /// <summary>One more character would reach the next threshold.</summary>
    public bool IsNearlyActive => NextThreshold == Count + 1;
}

/// <summary>
/// Gathers the effects that items, inn items, synergies and the cycle's omen put on units and on the inn (ITM-02 to
/// ITM-04, SYN-01 to SYN-03, OMN-03), by effect kind (FIX-04). Personal kinds (<c>Stat</c>, <c>Soothe</c>, <c>Aura</c>, <c>Radius</c>) go on
/// units; every other kind is inn-wide (D-077).
/// </summary>
internal sealed class Modifiers
{
    private static readonly EffectKind[] PersonalKinds = [EffectKind.Stat, EffectKind.Soothe, EffectKind.Aura, EffectKind.Radius];

    private readonly GameContent _content;
    private readonly RunState _run;
    private readonly AncestryObserver _ancestry;

    public Modifiers(GameContent content, RunState run, AncestryObserver ancestry)
    {
        _content = content;
        _run = run;
        _ancestry = ancestry;
    }

    /// <summary>Every synergy with its count, as of the last <see cref="Refresh"/>.</summary>
    public IReadOnlyList<SynergyStatus> Synergies { get; private set; } = Array.Empty<SynergyStatus>();

    public static bool IsPersonal(EffectKind kind) => PersonalKinds.Contains(kind);

    /// <summary>
    /// Recounts the synergies from the staff on the board and sets each unit's effects. At Dusk units are also healed
    /// to their (possibly new) full health. Called after every Dusk change and when the mist begins, so synergies hold
    /// for the whole mist (D-077).
    /// </summary>
    public void Refresh(bool heal)
    {
        Synergies = Count();
        var synergyEffects = ActiveSynergyEffects().Where(e => IsPersonal(e.Kind)).ToList();
        var innEffects = _run.InnItems.SelectMany(i => i.Def.Effects).Where(e => IsPersonal(e.Kind)).ToList();

        foreach (var unit in _run.Units)
        {
            var effects = new List<EffectDef>();
            if (unit.Def.IsInnkeeper)
            {
                effects.AddRange(innEffects.Where(e => e.Target == EffectTarget.Innkeeper));
            }
            else
            {
                effects.AddRange(_run.CarriedBy(unit.Id).SelectMany(i => i.Def.Effects)
                    .Where(e => IsPersonal(e.Kind) && e.Target is null or EffectTarget.Carrier));
                if (unit.Tile is not null)
                {
                    effects.AddRange(synergyEffects);
                }

                effects.AddRange(innEffects.Where(e => e.Target is null or EffectTarget.Staff));
            }

            unit.Effects = effects;
            if (heal)
            {
                unit.Health = unit.MaxHealth;
            }
        }
    }

    /// <summary>
    /// The sum of the amounts of an inn-wide effect kind (for example <c>RerollCost</c>, <c>Tribute</c>); effects that
    /// name a plane count only for that plane.
    /// </summary>
    public int InnWide(EffectKind kind, string? plane = null) =>
        (int)Math.Round(InnWideEffects(kind).Where(e => e.Plane is null || e.Plane == plane).Sum(e => e.Amount ?? 0));

    /// <summary>
    /// The product of the multipliers of an inn-wide effect kind (for example <c>Tribute</c>, <c>ArrivalRate</c>), 1 if
    /// there are none; effects that name a plane count only for that plane (D-087).
    /// </summary>
    public double InnWideMultiplier(EffectKind kind, string? plane = null) =>
        InnWideEffects(kind).Where(e => e.Plane is null || e.Plane == plane).Aggregate(1.0, (m, e) => m * (e.Multiplier ?? 1));

    /// <summary>A converging plane's arrival weight: the product of the <c>PlaneWeight</c> effects that name it (D-087).</summary>
    public double PlaneWeight(string plane) =>
        InnWideEffects(EffectKind.PlaneWeight).Where(e => e.Plane == plane).Aggregate(1.0, (m, e) => m * (e.Multiplier ?? 1));

    /// <summary>Inn-wide effects from inn items, carried staff items, active synergies and the cycle's omen.</summary>
    public IEnumerable<EffectDef> InnWideEffects(EffectKind kind) =>
        _run.InnItems.SelectMany(i => i.Def.Effects)
            .Concat(_run.Items.Where(i => i.Carrier is not null).SelectMany(i => i.Def.Effects))
            .Concat(ActiveSynergyEffects())
            .Concat(_run.OmenEffects)
            .Where(e => e.Kind == kind && !IsPersonal(kind));

    /// <summary>The classes a character counts for in synergies: its techniques' and its items' tags (STF-03, ITM-03).</summary>
    public IReadOnlySet<StaffClass> SynergyClasses(Unit unit)
    {
        var classes = new HashSet<StaffClass>(unit.Classes);
        foreach (var item in _run.CarriedBy(unit.Id))
        {
            classes.UnionWith(item.Def.ClassTags ?? Array.Empty<StaffClass>());
        }

        return classes;
    }

    /// <summary>The origins a character counts for: its origin (a plane or the Mortal Realm) and a revealed ancestry (SYN-01).</summary>
    public IReadOnlySet<string> SynergyOrigins(Unit unit)
    {
        var origins = new HashSet<string> { _content.Character(unit.Def.Id).Origin };
        if (_ancestry.RevealedAncestry(unit) is { } plane)
        {
            origins.Add(plane);
        }

        return origins;
    }

    private IEnumerable<EffectDef> ActiveSynergyEffects() =>
        Synergies.Where(s => s.IsActive)
            .SelectMany(s => _content.Synergies.First(d => d.Id == s.SynergyId).Tiers.First(t => t.Count == s.ActiveThreshold).Effects);

    /// <summary>Distinct characters on the board (the bench does not count) per synergy (SYN-01).</summary>
    private IReadOnlyList<SynergyStatus> Count()
    {
        var onBoard = _run.StaffOnTheFloor.ToList();
        return _content.Synergies.Select(def =>
        {
            var count = onBoard
                .Where(u => def.Class is { } c ? SynergyClasses(u).Contains(c) : SynergyOrigins(u).Contains(def.Origin!))
                .Select(u => u.Def.Id)
                .Distinct()
                .Count();
            var thresholds = def.Tiers.Select(t => t.Count).ToList();
            var active = thresholds.Where(t => t <= count).DefaultIfEmpty(0).Max();
            var next = thresholds.Where(t => t > count).Select(t => (int?)t).FirstOrDefault();
            return new SynergyStatus(def.Id, def.Name, count, thresholds, active, next);
        }).ToList();
    }
}
