using MistyInn.Core.Content;
using MistyInn.Core.Randomness;
using MistyInn.Core.Run;

namespace MistyInn.Core.Simulation;

/// <summary>
/// The Dusk shop (section 13.1): staff offers at a rank drawn from the cycle's odds (SHP-03) with pity and home-plane
/// weights (SHP-04), item offers weighted by rarity (D-080), rerolls (SHP-05) and the lock (SHP-06). It draws from its
/// own random stream, derived from the run's seed, so shopping never changes how a mist plays out (INV-02, D-080).
/// </summary>
internal sealed class ShopSystem
{
    private readonly GameContent _content;
    private readonly RunState _run;
    private readonly IRandomSource _random;
    private readonly Modifiers _modifiers;

    public ShopSystem(GameContent content, RunState run, IRandomSource random, Modifiers modifiers)
    {
        _content = content;
        _run = run;
        _random = random;
        _modifiers = modifiers;
    }

    private ShopDef Tuning => _content.Balance.Shop;

    /// <summary>The random stream the shop draws from; saved with the run in step 8.</summary>
    public IRandomSource Random => _random;

    /// <summary>The next reroll's price: 2, then 1 more per reroll this cycle, less <c>RerollCost</c> effects, never below 0.</summary>
    public int RerollCost =>
        Math.Max(0, Tuning.RerollBase + Tuning.RerollStep * _run.Shop.RerollsThisCycle + _modifiers.InnWide(EffectKind.RerollCost));

    /// <summary>The rank odds (percent for ★1 to ★5) for a cycle (SHP-03).</summary>
    public IReadOnlyList<int> OddsFor(int cycle) => Tuning.RankOdds.Last(o => o.FromCycle <= cycle).Odds;

    /// <summary>A staff offer's price: base cost + 2 per rank above ★1 (SHP-08).</summary>
    public int PriceOf(StaffDef character, int rank) => character.BaseCost + Tuning.RankPriceStep * (rank - 1);

    /// <summary>A character's draw weight: ×1.5 if owned, ×2 if its plane (origin or ancestry, even hidden) converges (SHP-04).</summary>
    public double WeightOf(StaffDef character)
    {
        var weight = 1.0;
        if (_run.Units.Any(u => u.Def.Id == character.Id))
        {
            weight *= Tuning.PityWeight;
        }

        var converging = _run.ConvergingPlanes.Select(p => p.Id).ToHashSet();
        if (converging.Contains(character.Origin) || (character.Ancestry is { } a && converging.Contains(a)))
        {
            weight *= Tuning.HomePlaneWeight;
        }

        return weight;
    }

    /// <summary>Owned ★5 characters, characters already showing and locked characters are not drawn (SHP-04, MET-03).</summary>
    public bool CanBeDrawn(StaffDef character, IReadOnlySet<string> showing) =>
        !showing.Contains(character.Id) && !_run.Start.LockedCharacters.Contains(character.Id)
        && _run.Units.FirstOrDefault(u => u.Def.Id == character.Id) is not { Rank: >= UpgradeTrees.MaxRank };

    /// <summary>Each Dusk: a locked shop keeps its offers once, otherwise everything is drawn anew; the reroll cost resets.</summary>
    public void NewDusk()
    {
        _run.Shop.RerollsThisCycle = 0;
        if (_run.Shop.Locked)
        {
            _run.Shop.Locked = false;
            return;
        }

        DrawAll();
    }

    /// <summary>Fills every slot with a fresh offer (a new Dusk, or a reroll, D-080).</summary>
    public void DrawAll()
    {
        _run.Shop.Staff.Clear();
        var showing = new HashSet<string>();
        for (var i = 0; i < Tuning.StaffOffers; i++)
        {
            var offer = DrawStaff(showing);
            if (offer is not null)
            {
                showing.Add(offer.CharacterId);
            }

            _run.Shop.Staff.Add(offer);
        }

        _run.Shop.Items.Clear();
        var shownItems = new HashSet<string>();
        for (var i = 0; i < Tuning.ItemOffers; i++)
        {
            var offer = DrawItem(shownItems);
            if (offer is not null)
            {
                shownItems.Add(offer.ItemId);
            }

            _run.Shop.Items.Add(offer);
        }
    }

    /// <summary>A rank by the cycle's odds (SHP-03).</summary>
    public int DrawRank(int cycle)
    {
        var odds = OddsFor(cycle);
        var roll = _random.NextInt(0, odds.Sum());
        for (var rank = 1; rank <= odds.Count; rank++)
        {
            roll -= odds[rank - 1];
            if (roll < 0)
            {
                return rank;
            }
        }

        return 1;
    }

    private StaffOffer? DrawStaff(IReadOnlySet<string> showing)
    {
        var candidates = _content.Characters.Where(c => CanBeDrawn(c, showing)).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        var rank = DrawRank(_run.Cycle);
        var character = Pick(candidates, WeightOf);
        return new StaffOffer(character.Id, rank, PriceOf(character, rank));
    }

    /// <summary>Items weighted by rarity; an inn item already owned and a locked item are not offered (D-080, MET-03).</summary>
    private ItemOffer? DrawItem(IReadOnlySet<string> showing)
    {
        var owned = _run.InnItems.Select(i => i.Def.Id).ToHashSet();
        var candidates = _content.Items
            .Where(i => !showing.Contains(i.Id) && !owned.Contains(i.Id) && !_run.Start.LockedItems.Contains(i.Id) && Tuning.ItemWeights[i.Rarity] > 0)
            .ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        var item = Pick(candidates, i => Tuning.ItemWeights[i.Rarity]);
        return new ItemOffer(item.Id, item.Price);
    }

    private T Pick<T>(IReadOnlyList<T> candidates, Func<T, double> weight)
    {
        var roll = _random.NextDouble() * candidates.Sum(weight);
        foreach (var candidate in candidates)
        {
            roll -= weight(candidate);
            if (roll < 0)
            {
                return candidate;
            }
        }

        return candidates[^1];
    }
}
