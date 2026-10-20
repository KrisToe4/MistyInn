using MistyInn.Core.Board;
using MistyInn.Core.Content;
using static MistyInn.Core.Tests.Content.ContentTestKit;

namespace MistyInn.Core.Tests.Content;

/// <summary>One broken example per validator rule (DAT-04, TST-07).</summary>
public class ContentValidatorTests
{
    [Test]
    [Property("Requirement", "DAT-04")]
    public void DuplicateIdsAreRejected()
    {
        AssertRejected(Set with { Planes = Set.Planes.Append(Set.Planes[0]).ToList() }, "duplicate id 'drowned-deep'");
    }

    [Test]
    [Property("Requirement", "DAT-05")]
    public void IdsMustBeKebabCase()
    {
        AssertRejected(WithFixture("lantern", f => f with { Id = "Lantern_1" }), "must be lowercase kebab-case");
    }

    [Test]
    [Property("Requirement", "DAT-04")]
    [Property("Requirement", "PLN-01")]
    public void APlaneMayOnlyListItsOwnKnownCreatures()
    {
        AssertRejected(WithPlane("drowned-deep", p => p with { Creatures = [new CreatureWeightDef("nobody", 1)] }),
            "unknown creature 'nobody'");
        AssertRejected(WithPlane("drowned-deep", p => p with { Creatures = [new CreatureWeightDef("creature-03", 1)] }),
            "belongs to another plane");
        AssertRejected(WithPlane("drowned-deep", p => p with { Creatures = [new CreatureWeightDef("creature-01", 0)] }),
            "needs a weight above 0");
    }

    [Test]
    [Property("Requirement", "PLN-01")]
    public void MistColoursAreHtmlColours()
    {
        AssertRejected(WithPlane("pale-court", p => p with { MistColor = "lavender" }), "mistColor must be #rrggbb");
    }

    [Test]
    [Property("Requirement", "CRE-03")]
    public void ACreatureNeedsAKnownPlane()
    {
        AssertRejected(WithCreature("creature-01", c => c with { PlaneId = "nowhere" }), "unknown plane 'nowhere'");
    }

    [Test]
    [Property("Requirement", "AGI-05")]
    public void ACreatureHasOneToThreeAppetites()
    {
        AssertRejected(WithCreature("creature-01", c => c with { Appetites = [] }), "one to three appetites");
    }

    [Test]
    [Property("Requirement", "CRE-03")]
    public void APreferredTechniqueMustAnswerTheAppetite()
    {
        AssertRejected(WithCreature("creature-01", c => c with
        {
            Appetites = [new AppetiteDef(Appetite.Drink, AppetiteStrength.Craves, "song")],
        }), "does not answer Drink");
    }

    [Test]
    [Property("Requirement", "CRE-07")]
    [Property("Requirement", "RAT-01")]
    public void OnlySapientAdultCreaturesWantCompany()
    {
        AssertRejected(WithCreature("creature-09", c => c with { Adult = false }), "must be sapient and adult");
        AssertRejected(WithCreature("creature-09", c => c with { Sapient = false }), "must be sapient and adult");
    }

    [Test]
    [Property("Requirement", "STF-13")]
    [Property("Requirement", "RAT-01")]
    public void EveryStaffMemberIsAnAdult()
    {
        AssertRejected(WithCharacter("staff-05", s => s with { Adult = false }), "'staff-05' must be an adult");
        AssertRejected(Set with { Staff = Set.Staff with { Innkeeper = Set.Staff.Innkeeper with { Adult = false } } },
            "'innkeeper' must be an adult");
    }

    [Test]
    [Property("Requirement", "STF-15")]
    public void AncestryNamesAKnownPlaneAndOnlyMortalsHaveOne()
    {
        AssertRejected(WithCharacter("staff-01", s => s with { Ancestry = "atlantis" }), "unknown ancestry plane 'atlantis'");
        AssertRejected(WithCharacter("staff-03", s => s with { Ancestry = "pale-court" }), "only Mortal characters have a separate ancestry");
    }

    [Test]
    [Property("Requirement", "STF-01")]
    public void OriginIsAPlaneOrTheMortalRealm()
    {
        AssertRejected(WithCharacter("staff-02", s => s with { Origin = "the-moon" }), "unknown origin 'the-moon'");
    }

    [Test]
    [Property("Requirement", "STF-02")]
    public void TheBaseTechniqueBelongsToTheMainClass()
    {
        AssertRejected(WithCharacter("staff-01", s => s with { BaseTechnique = "cards" }), "the base technique must belong to the main class");
    }

    [Test]
    [Property("Requirement", "STF-07")]
    public void ATreeSpansAtMostTwoClasses()
    {
        // staff-01 is Companion/Server with Entertainer; Cards is Gambler, a third class.
        AssertRejected(WithCharacter("staff-01", s => WithRank(s, 2,
                new TreeOptionDef("r2-food", "food"), new TreeOptionDef("r2-cards", "cards"))),
            "outside the character's two classes");
    }

    [Test]
    [Property("Requirement", "STF-07")]
    public void EveryRankOffersTwoOrThreeOptions()
    {
        AssertRejected(WithCharacter("staff-01", s => WithRank(s, 2, new TreeOptionDef("r2-food", "food"))),
            "needs two or three options");
    }

    [Test]
    [Property("Requirement", "STF-07")]
    public void RequirementsPointToEarlierRanks()
    {
        AssertRejected(WithCharacter("staff-01", s => WithRank(s, 2,
                new TreeOptionDef("r2-food", "food", Requires: "r5-tales"), new TreeOptionDef("r2-song", "song"))),
            "requires 'r5-tales'");
    }

    [Test]
    [Property("Requirement", "STF-17")]
    public void AMasteryNeedsATechniqueThatCanBeKnownByThen()
    {
        AssertRejected(WithCharacter("staff-01", s => WithRank(s, 2,
                new TreeOptionDef("r2-food", "food"), new TreeOptionDef("r2-master-song", "song", Mastery: true))),
            "cannot be known by this rank");
    }

    [Test]
    [Property("Requirement", "STF-17")]
    public void EveryPathAlwaysOffersAnOption()
    {
        // Rank 2: master the base technique or learn Food. Rank 3 only offers masteries of Drink and Food, so after
        // mastering Drink at rank 2 nothing at rank 3 is available.
        AssertRejected(WithCharacter("staff-01", s =>
            WithRank(WithRank(s, 2,
                    new TreeOptionDef("r2-food", "food"), new TreeOptionDef("r2-master-drink", "drink", Mastery: true)),
                3,
                new TreeOptionDef("r3-master-drink", "drink", Mastery: true), new TreeOptionDef("r3-master-food", "food", Mastery: true))),
            "no option is available at rank 3 after [r2-master-drink]");
    }

    [Test]
    [Property("Requirement", "STF-10")]
    public void PresetBuildsMustBeValidPaths()
    {
        AssertRejected(WithCharacter("staff-01", s => s with
        {
            Presets = s.Presets.Select(p => p.Rank == 2 ? p with { Choices = ["r3-dance"] } : p).ToList(),
        }), "the rank 2 preset is invalid");
        AssertRejected(WithCharacter("staff-01", s => s with { Presets = s.Presets.Where(p => p.Rank != 4).ToList() }),
            "one preset build for each rank");
    }

    [Test]
    [Property("Requirement", "STF-02")]
    public void EachTechniqueIsListedOnceInItsOwnClass()
    {
        AssertRejected(WithTechnique("cards", t => t with { Class = StaffClass.Fighter }), "Cards belongs to Gambler");
        AssertRejected(Set with { Techniques = Set.Techniques.Where(t => t.Id != "tales").ToList() },
            "technique Tales must appear exactly once");
    }

    [Test]
    [Property("Requirement", "BRD-02")]
    public void ATechniqueThatNeedsAFixtureHasOne()
    {
        AssertRejected(WithFixture("bar-counter", f => f with { Effects = [] }), "'drink': needs a fixture");
    }

    [Test]
    [Property("Requirement", "STF-17")]
    public void AMasteryNeverWeakensATechnique()
    {
        AssertRejected(WithTechnique("song", t => t with { Mastery = new MasteryDef(SootheMultiplier: 0.5) }),
            "a mastery must not make the technique weaker");
    }

    [Test]
    [Property("Requirement", "DAT-03")]
    public void EffectsHaveTheParametersTheirKindNeeds()
    {
        AssertRejected(WithFixture("hearth", f => f with { Effects = [new EffectDef(EffectKind.Provides, Appetite: Appetite.Warmth)] }),
            "a Provides effect is missing a parameter");
    }

    [Test]
    [Property("Requirement", "DAT-03")]
    public void EffectsPointToKnownThings()
    {
        AssertRejected(WithItem("ember-in-a-jar", i => i with
        {
            Effects = [new EffectDef(EffectKind.CountsAs, Fixture: "hearth", CountsAs: "bonfire")],
        }), "unknown fixture 'bonfire'");
    }

    [Test]
    [Property("Requirement", "DAT-03")]
    public void FixturesOnlyGiveOffLightNoiseHeatOrCold()
    {
        AssertRejected(WithFixture("lantern", f => f with
        {
            Effects = [new EffectDef(EffectKind.Emits, Trigger: TriggerKind.Violence, Radius: 3)],
        }), "can only give off Light, Noise, Heat or Cold");
    }

    [Test]
    [Property("Requirement", "FIX-01")]
    public void AFixtureZoneMustExist()
    {
        AssertRejected(WithFixture("stage", f => f with { Zone = "ballroom" }), "unknown zone 'ballroom'");
    }

    [Test]
    [Property("Requirement", "ITM-01")]
    [Property("Requirement", "ITM-04")]
    public void InnItemsHaveADisplaySpot()
    {
        AssertRejected(WithItem("spare-keys", i => i with { DisplaySpot = null }), "needs a display model and a display spot");
    }

    [Test]
    [Property("Requirement", "DAT-06")]
    public void FlavourLinesSayWhetherTheyAreFilterAndBloodSafe()
    {
        AssertRejected(Set with { Flavour = Set.Flavour.Select(f => f with { BloodSafe = null }).ToList() },
            "filterSafe and bloodSafe are both required");
    }

    [Test]
    [Property("Requirement", "BRD-03")]
    [Property("Requirement", "SIM-04")]
    public void EveryZoneOfTheInnMapIsReachableFromTheDoor()
    {
        var rows = Set.Inn.Rows.ToList();
        rows[0] = "###" + rows[0][3..];
        rows[1] = "#Z#" + rows[1][3..];
        rows[2] = "###" + rows[2][3..];
        var legend = new Dictionary<string, LegendDef>(Set.Inn.Legend) { ["Z"] = new(TileKind.Floor, "secret-room") };

        AssertRejected(Set with { Inn = new InnDef(rows, legend) }, "no path from the front door to secret-room");
    }

    [Test]
    [Property("Requirement", "SIM-04")]
    public void TheInnMapMustParse()
    {
        AssertRejected(Set with { Inn = Set.Inn with { Rows = Set.Inn.Rows.Select(r => r.Replace('D', '#')).ToList() } },
            "exactly one front door");

        var legend = new Dictionary<string, LegendDef>(Set.Inn.Legend) { ["ZZ"] = new(TileKind.Floor, "two-letters") };
        AssertRejected(Set with { Inn = Set.Inn with { Legend = legend } }, "must be a single character");
    }

    [Test]
    [Property("Requirement", "PLN-04")]
    public void EveryPlaneNeedsAMistGate()
    {
        var extraCreature = Set.Creatures[0] with { Id = "creature-99", PlaneId = "sixth-plane" };
        var extraPlane = Set.Planes[0] with { Id = "sixth-plane", Creatures = [new CreatureWeightDef("creature-99", 1)] };

        AssertRejected(Set with
        {
            Planes = Set.Planes.Append(extraPlane).ToList(),
            Creatures = Set.Creatures.Append(extraCreature).ToList(),
        }, "6 planes need 6 mist gates");
    }

    [Test]
    [Property("Requirement", "SHP-03")]
    public void RankOddsSumToOneHundred()
    {
        var odds = Set.Balance.Shop.RankOdds.Select((o, i) => i == 0 ? o with { Odds = [80, 15, 0, 0, 0] } : o).ToList();

        AssertRejected(Set with { Balance = Set.Balance with { Shop = Set.Balance.Shop with { RankOdds = odds } } },
            "need five values summing to 100");
    }

    [Test]
    [Property("Requirement", "RUN-01")]
    public void StartingStaffAreKnownCharacters()
    {
        AssertRejected(Set with { Balance = Set.Balance with { StartingStaff = ["staff-99"] } }, "startingStaff must list distinct known characters");
    }

    [Test]
    [Property("Requirement", "CRE-04")]
    public void WanderZonesExist()
    {
        AssertRejected(Set with { Balance = Set.Balance with { WanderZones = ["attic"] } }, "wanderZones must be a non-empty list");
    }

    [Test]
    [Property("Requirement", "DAT-04")]
    public void EveryProblemIsReportedTogether()
    {
        var slowCreature = WithCreature("creature-01", c => c with { Speed = 0 });
        var broken = slowCreature with
        {
            Planes = slowCreature.Planes.Select(p => p.Id == "pale-court" ? p with { MistColor = "x" } : p).ToList(),
        };

        var problems = Problems(broken);

        Assert.That(problems, Has.Some.Contains("speed must be above 0"));
        Assert.That(problems, Has.Some.Contains("mistColor must be #rrggbb"));
        var e = Assert.Throws<ContentException>(() => GameContent.Create(broken));
        Assert.That(e!.Errors, Has.Count.EqualTo(problems.Count));
    }
}
