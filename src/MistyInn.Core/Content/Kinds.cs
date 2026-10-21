namespace MistyInn.Core.Content;

// The closed sets of DAT-03 and STF-02. A new value in any of them is an SDD change. In the JSON files they are
// written as their names (for example "CompanionServer", "Drink").

/// <summary>What a creature wants (AGI-05).</summary>
public enum Appetite
{
    /// <summary>Strong drink at the bar (the Drink technique). Food and ordinary drink are basic service (AGI-11).</summary>
    Alcohol,
    Company,
    Gambling,
    Music,
    Spectacle,
    Brawl,
    Warmth,
    Quiet,
    Darkness,
    Order,
}

/// <summary>How strongly a creature wants an appetite (AGI-05).</summary>
public enum AppetiteStrength
{
    /// <summary>×1.0.</summary>
    Craves,

    /// <summary>×0.5.</summary>
    Likes,
}

/// <summary>What sets a creature off (AGI-08).</summary>
public enum TriggerKind
{
    Light,
    Noise,
    Heat,
    Cold,
    Crowding,
    RivalPlane,
    Violence,
    Blood,
}

/// <summary>The four staff classes (STF-02).</summary>
public enum StaffClass
{
    CompanionServer,
    Gambler,
    Fighter,
    Entertainer,
}

/// <summary>Every technique (STF-02). <see cref="TechniqueCatalog"/> says which class each belongs to.</summary>
public enum Technique
{
    Company,
    Food,
    Drink,
    Comfort,
    Cards,
    Dice,
    Bones,
    Fists,
    Weapons,
    Grapple,
    Song,
    Dance,
    Tales,
}

/// <summary>The supplies that some techniques use (FIX-06): Drink, for the Drink technique.</summary>
public enum Supply
{
    Drink,
}

/// <summary>The stages of a turned creature (ESC-01).</summary>
public enum EscalationStage
{
    Wreck,
    Prey,
    Hunt,
}

/// <summary>The effect kinds of DAT-03.</summary>
public enum EffectKind
{
    AgitationRate,
    ArrivalAgitation,
    Soothe,
    ArrivalRate,
    PlaneWeight,
    Stat,
    Radius,
    BoardSlots,
    Tribute,
    Income,
    ShopOdds,
    Supplies,
    Blocks,
    PlaneBarrier,
    Aura,
    RerollCost,
    RoomSlots,
    Provides,
    Enables,
    Emits,
    CountsAs,
}

/// <summary>The stats a <see cref="EffectKind.Stat"/> effect can change.</summary>
public enum StatKind
{
    Health,
    Attack,
    AttackInterval,
    Range,
    Armour,
    Speed,
    Soothe,
    Capacity,
}

/// <summary>Who an effect applies to, when it is not obvious from where it sits.</summary>
public enum EffectTarget
{
    Carrier,
    Staff,
    Innkeeper,
    Creatures,
}

public enum ItemKind
{
    /// <summary>Carried by a staff member (ITM-02).</summary>
    Staff,

    /// <summary>Run-wide, shown in the inn (ITM-04).</summary>
    Inn,
}

public enum Rarity
{
    Common,
    Rare,
    Cursed,
}

/// <summary>The closed technique-to-class mapping of STF-02.</summary>
public static class TechniqueCatalog
{
    public static StaffClass ClassOf(Technique technique) => technique switch
    {
        Technique.Company or Technique.Food or Technique.Drink or Technique.Comfort => StaffClass.CompanionServer,
        Technique.Cards or Technique.Dice or Technique.Bones => StaffClass.Gambler,
        Technique.Fists or Technique.Weapons or Technique.Grapple => StaffClass.Fighter,
        Technique.Song or Technique.Dance or Technique.Tales => StaffClass.Entertainer,
        _ => throw new ArgumentOutOfRangeException(nameof(technique), technique, null),
    };
}
