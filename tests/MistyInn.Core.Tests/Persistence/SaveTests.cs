using System.Text.Json.Nodes;
using MistyInn.Core.Bestiary;
using MistyInn.Core.Board;
using MistyInn.Core.Commands;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Meta;
using MistyInn.Core.Persistence;
using MistyInn.Core.Randomness;
using MistyInn.Core.Run;
using MistyInn.Core.Settings;
using MistyInn.Core.Tests.Content;
using static MistyInn.Core.Tests.TestGame;

namespace MistyInn.Core.Tests.Persistence;

/// <summary>The run save, Continue and the profile (SAV-01 to SAV-04, TST-08, step 8).</summary>
public class SaveTests
{
    private const ulong Seed = 20261026;
    private static readonly GameContent Real = GameContent.Default;

    /// <summary>The sturdy content with an omen every cycle, so the omen and its random stream are saved too.</summary>
    private static readonly GameContent WithOmens = ContentWith(
        balance: b => b with { Omens = new OmenBalanceDef(FromCycle: 1, Chance: 1, ConvergenceChance: 1) },
        set: s => s with { Omens = Real.Omens });

    private static readonly Tile RoomThree = new(22, 9);

    /// <summary>
    /// A run played to the start of cycle 3's Dusk: fixtures placed, a staff item carried, an inn item that opens the
    /// third back room with a staff member in it, two mists played (so the bestiary has entries).
    /// </summary>
    private static GameSession PlayedToDusk3()
    {
        var session = new GameSession(new SeededRandom(Seed), content: WithOmens, bestiary: new CreatureBestiary(),
            ancestry: new AncestryRecord(), profile: new MetaProfile());
        FixtureIdOf(session, "hearth", out var hearth);
        FixtureIdOf(session, "lantern", out var lantern);
        Assert.That(session.Execute(new PlaceFixture(hearth, new Tile(12, 7), 1)).Succeeded, Is.True);
        Assert.That(session.Execute(new PlaceFixture(lantern, new Tile(12, 9), 0)).Succeeded, Is.True);

        session.SetShopForTests(session.RunForTests.Shop.Staff.ToList(), [new ItemOffer("spare-keys", 0), new ItemOffer("lute", 0)]);
        Assert.That(session.Execute(new BuyOffer(ShopSection.Items, 0)).Succeeded, Is.True);
        Assert.That(session.Execute(new BuyOffer(ShopSection.Items, 1)).Succeeded, Is.True);
        var staff = Staff(session);
        var lute = session.Items.Single(i => i.ItemDefId == "lute").Id;
        Assert.That(session.Execute(new EquipItem(lute, staff[0].Id)).Succeeded, Is.True);
        Assert.That(session.Execute(new PlaceUnit(staff[0].Id, new Tile(14, 9))).Succeeded, Is.True);
        Assert.That(session.Execute(new PlaceUnit(staff[1].Id, RoomThree)).Succeeded, Is.True, "Spare Keys open the third room");

        PlayCycle(session);
        PlayCycle(session);
        Assert.That(session.Cycle, Is.EqualTo(3));
        return session;
    }

    private static void FixtureIdOf(GameSession session, string defId, out FixtureId id) =>
        id = session.Fixtures.First(f => f.FixtureDefId == defId && f.IsInStorage).Id;

    /// <summary>The profile through its file format and back, as Continue sees it after a restart.</summary>
    private static LoadedProfile Reloaded(GameSession session) =>
        ProfileSaves.Restore(SaveCodec.ReadProfile(SaveCodec.Write(ProfileSaves.Capture(session.Bestiary, session.Ancestry, session.Profile, true))));

    private static GameSession Continue(GameSession session, string runText)
    {
        var profile = Reloaded(session);
        return GameSession.Restore(SaveCodec.ReadRun(runText), session.Content, profile.Bestiary, profile.Ancestry, profile.Meta);
    }

    [Test]
    [Property("Requirement", "SAV-01")]
    [Property("Requirement", "SAV-03")]
    [Property("Requirement", "INV-02")]
    [Property("Requirement", "TST-08")]
    public void ARestoredRunCarriesOnExactlyLikeTheSavedOne()
    {
        var session = PlayedToDusk3();
        var text = SaveCodec.Write(session.CreateSave(Seed));
        var restored = Continue(session, text);

        Assert.That(SaveCodec.Write(restored.CreateSave(Seed)), Is.EqualTo(text), "the round trip loses nothing");
        Assert.That(restored.Phase, Is.EqualTo(Phase.Dusk));
        Assert.That(restored.Cycle, Is.EqualTo(3));
        Assert.That(restored.Silver, Is.EqualTo(session.Silver));
        Assert.That((restored.Omen?.Def, restored.Omen?.SurgePlane), Is.EqualTo((session.Omen?.Def, session.Omen?.SurgePlane)));
        Assert.That(restored.ConvergingPlanes, Is.EqualTo(session.ConvergingPlanes));
        Assert.That(restored.LockedRooms, Is.EquivalentTo(session.LockedRooms));
        Assert.That(restored.Rooms.Select(r => r.Zone), Is.EqualTo(session.Rooms.Select(r => r.Zone)), "the inn item's room is open again");
        Assert.That(restored.Shop.RerollCost, Is.EqualTo(session.Shop.RerollCost));
        Assert.That(restored.Synergies.Select(s => (s.SynergyId, s.Count)), Is.EqualTo(session.Synergies.Select(s => (s.SynergyId, s.Count))));
        foreach (var (a, b) in session.Units.Zip(restored.Units))
        {
            Assert.That((b.Id, b.StaffId, b.Tile, b.Rank, b.Health, b.MaxHealth, b.Attack, b.Armour, b.Shaken),
                Is.EqualTo((a.Id, a.StaffId, a.Tile, a.Rank, a.Health, a.MaxHealth, a.Attack, a.Armour, a.Shaken)));
            Assert.That(b.Techniques, Is.EqualTo(a.Techniques));
            Assert.That(b.Items, Is.EqualTo(a.Items));
        }

        // Continue resumes at the start of the saved Dusk: the same commands give the same mists (INV-02).
        for (var cycle = 3; cycle <= 4; cycle++)
        {
            foreach (var s in new[] { session, restored })
            {
                StartMist(s);
                s.RunMistToDawn();
            }

            Assert.That(restored.ComputeStateHash(), Is.EqualTo(session.ComputeStateHash()), $"cycle {cycle}'s mist");
            Assert.That(restored.Execute(new ContinueFromDawn()).Succeeded, Is.EqualTo(session.Execute(new ContinueFromDawn()).Succeeded));
            Assert.That(restored.ComputeStateHash(), Is.EqualTo(session.ComputeStateHash()), $"the Dusk after cycle {cycle}");
        }
    }

    [Test]
    [Property("Requirement", "SAV-01")]
    public void TheSaveHoldsWhatSav01Lists()
    {
        var session = PlayedToDusk3();

        // Its first carrier may have died in the mist, sending it back to storage (ESC-09); give it to someone alive.
        var lute = session.Items.Single(i => i.ItemDefId == "lute").Id;
        var carrier = Staff(session).First().Id;
        Assert.That(session.Execute(new EquipItem(lute, carrier)).Succeeded, Is.True);
        var save = session.CreateSave(Seed);
        var json = JsonNode.Parse(SaveCodec.Write(save))!.AsObject();

        Assert.That(json.Select(p => p.Key), Is.SupersetOf(new[]
        {
            "schemaVersion", "contentVersion", "seed", "cycle", "random", "shopRandom", "omenRandom", "ids", "silver", "integrity",
            "supplies", "unlockedPlanes", "omen", "creaturesOccupied", "units", "fixtures", "items", "shop",
        }));
        Assert.That(save.ContentVersion, Is.EqualTo(WithOmens.Balance.ContentVersion));
        Assert.That(save.Seed, Is.EqualTo(Seed));
        Assert.That(save.Omen, Is.Not.Null, "an omen every cycle");
        Assert.That(save.Fixtures.Count(f => f.Origin is not null), Is.EqualTo(2));
        Assert.That(save.Items.Single(i => i.DefId == "lute").Carrier, Is.EqualTo(carrier.Value));
        Assert.That(save.Items.Select(i => (i.Id, i.DefId, i.Carrier)),
            Is.EqualTo(session.Items.Select(i => (i.Id.Value, i.ItemDefId, i.Carrier?.Value))), "every item and its carrier");
        Assert.That(save.Units.Single(u => u.IsInnkeeper).Tile, Is.EqualTo(InnkeeperTile));
        Assert.That(json["shop"]!["staff"]!.AsArray(), Has.Count.EqualTo(WithOmens.Balance.Shop.StaffOffers));
    }

    [Test]
    [Property("Requirement", "SAV-03")]
    public void TheRunIsSavedOnlyBetweenCycles()
    {
        var session = NewSession();
        Assert.That(() => session.CreateSave(1), Throws.Nothing, "at Dusk");

        StartMist(session);
        Assert.That(() => session.CreateSave(1), Throws.InvalidOperationException, "during the mist");
        session.RunMistToDawn();
        Assert.That(() => session.CreateSave(1), Throws.InvalidOperationException, "at Dawn: the save is taken when the player leaves it");
    }

    [Test]
    [Property("Requirement", "SAV-01")]
    [Property("Requirement", "MET-03")]
    public void AContinuedRunKeepsTheUnlocksItStartedWith()
    {
        var content = ContentWith(set: s => s with { Unlocks = Real.Unlocks });
        var profile = new MetaProfile(essence: 100);
        var session = new GameSession(new SeededRandom(Seed), content: content, profile: profile);
        PlayCycle(session);
        var save = session.CreateSave(Seed);

        Assert.That(profile.Buy(content, "unlock-ledger-desk"), Is.EqualTo(UnlockFailure.None), "bought on the title screen before Continue");
        var restored = GameSession.Restore(save, content, profile: profile);

        Assert.That(restored.BuildMenu.Select(f => f.Id), Does.Not.Contain("ledger-desk"), "an unlock takes effect from the next run");
        Assert.That(new GameSession(new SeededRandom(Seed), content: content, profile: profile).BuildMenu.Select(f => f.Id), Does.Contain("ledger-desk"));
    }

    [Test]
    [Property("Requirement", "SAV-02")]
    [Property("Requirement", "MET-01")]
    [Property("Requirement", "BST-04")]
    [Property("Requirement", "TST-08")]
    public void TheProfileRoundTripsWithEverythingItHolds()
    {
        var session = PlayedToDusk3();
        var meta = new MetaProfile(essence: 12, unlocked: ["unlock-staff-04"], statistics: new RunStatistics(3, 1, 10, 41, 50));
        var ancestry = new AncestryRecord(["staff-02", "staff-07"]);
        var captured = ProfileSaves.Capture(session.Bestiary, ancestry, meta, contentNoteAcknowledged: true);
        var text = SaveCodec.Write(captured);

        var loaded = ProfileSaves.Restore(SaveCodec.ReadProfile(text));

        Assert.That(captured.Bestiary, Is.Not.Empty, "two mists filled the bestiary");
        Assert.That(SaveCodec.Write(ProfileSaves.Capture(loaded.Bestiary, loaded.Ancestry, loaded.Meta, loaded.ContentNoteAcknowledged)),
            Is.EqualTo(text));
        Assert.That(loaded.Meta.Essence, Is.EqualTo(12));
        Assert.That(loaded.Meta.Owns("unlock-staff-04"), Is.True);
        Assert.That(loaded.Meta.Statistics, Is.EqualTo(meta.Statistics));
        Assert.That(loaded.Ancestry.IsRevealed("staff-07"), Is.True);
        Assert.That(loaded.ContentNoteAcknowledged, Is.True);
        foreach (var creature in session.Content.Creatures)
        {
            Assert.That(loaded.Bestiary.LevelOf(creature, session.Content.Balance.Bestiary),
                Is.EqualTo(session.Bestiary.LevelOf(creature, session.Content.Balance.Bestiary)), creature.Id);
        }
    }

    [Test]
    [Property("Requirement", "SAV-02")]
    public void AbandoningASavedRunCountsAsALossWithNoEssence()
    {
        var meta = new MetaProfile(essence: 5, statistics: new RunStatistics(1, 1, 10, 30, 40));
        meta.RecordAbandoned(cyclesSurvived: 4, creaturesOccupied: 9);

        Assert.That(meta.Essence, Is.EqualTo(5));
        Assert.That(meta.Statistics, Is.EqualTo(new RunStatistics(2, 1, 10, 39, 40)));
    }

    [Test]
    [Property("Requirement", "SAV-01")]
    [Property("Requirement", "DAT-04")]
    public void TheContentVersionIsValidated()
    {
        Assert.That(Real.Balance.ContentVersion, Is.GreaterThanOrEqualTo(1));
        ContentTestKit.AssertRejected(Real.Set with { Balance = Real.Balance with { ContentVersion = 0 } }, "out of range");
    }

    [Test]
    [Property("Requirement", "SAV-06")]
    [Property("Requirement", "INV-07")]
    public void ADamagedSaveIsCorruptAndOneWithUnknownContentIsIncompatible()
    {
        var save = PlayedToDusk3().CreateSave(Seed);

        Assert.That(RunSaves.Check(save, WithOmens), Is.Null);
        Assert.That(RunSaves.Check(save with { Cycle = 0 }, WithOmens)!.Problem, Is.EqualTo(SaveProblem.Corrupt));
        Assert.That(RunSaves.Check(save with { Random = default }, WithOmens)!.Problem, Is.EqualTo(SaveProblem.Corrupt));
        Assert.That(RunSaves.Check(save with { Units = save.Units.Where(u => !u.IsInnkeeper).ToList() }, WithOmens)!.Problem,
            Is.EqualTo(SaveProblem.Corrupt), "no innkeeper");
        Assert.That(RunSaves.Check(save with { Items = null! }, WithOmens)!.Problem, Is.EqualTo(SaveProblem.Corrupt), "a missing list");

        var renamed = save with { Units = save.Units.Select(u => u.IsInnkeeper ? u : u with { DefId = "staff-99" }).ToList() };
        var problem = RunSaves.Check(renamed, WithOmens)!;
        Assert.That(problem.Problem, Is.EqualTo(SaveProblem.Incompatible));
        Assert.That(problem.Message, Does.Contain("staff-99"));
        var onAWall = save with { Units = save.Units.Select(u => u.IsInnkeeper ? u with { Tile = new Tile(5, 3) } : u).ToList() };
        Assert.That(RunSaves.Check(onAWall, WithOmens)!.Problem, Is.EqualTo(SaveProblem.Incompatible));

        Assert.That(() => GameSession.Restore(renamed, WithOmens),
            Throws.TypeOf<SaveException>().With.Property(nameof(SaveException.Problem)).EqualTo(SaveProblem.Incompatible));
    }

    [Test]
    [Property("Requirement", "SAV-04")]
    [Property("Requirement", "SAV-06")]
    public void TextThatIsNotASaveIsCorrupt()
    {
        var valid = SaveCodec.Write(NewSession().CreateSave(1));
        var unknownProperty = JsonNode.Parse(valid)!.AsObject();
        unknownProperty["gold"] = 3;

        foreach (var text in new[] { "", "{ not json", "[]", "{}", "{ \"schemaVersion\": \"one\" }", unknownProperty.ToJsonString() })
        {
            Assert.That(() => SaveCodec.ReadRun(text),
                Throws.TypeOf<SaveException>().With.Property(nameof(SaveException.Problem)).EqualTo(SaveProblem.Corrupt), text);
        }
    }

    [Test]
    [Property("Requirement", "SAV-04")]
    [Property("Requirement", "TST-08")]
    public void ANewerSaveIsRefusedAndAnOlderOneIsMigratedStepByStep()
    {
        var newer = JsonNode.Parse(SaveCodec.Write(NewSession().CreateSave(1)))!.AsObject();
        newer["schemaVersion"] = RunSaves.SchemaVersion + 1;
        Assert.That(() => SaveCodec.ReadRun(newer.ToJsonString()),
            Throws.TypeOf<SaveException>().With.Property(nameof(SaveException.Problem)).EqualTo(SaveProblem.TooNew));

        // A made-up history: version 1 called Essence "mistEssence", version 2 had no content note flag.
        var profile = JsonNode.Parse(SaveCodec.Write(ProfileSaves.Capture(new CreatureBestiary(), new AncestryRecord(), new MetaProfile(7), true)))!.AsObject();
        profile["schemaVersion"] = 1;
        profile["mistEssence"] = profile["essence"]!.GetValue<int>();
        profile.Remove("essence");
        profile.Remove("contentNoteAcknowledged");
        var order = new List<int>();
        var migrator = new SaveMigrator(3, new Dictionary<int, Func<JsonObject, JsonObject>>
        {
            [1] = doc =>
            {
                order.Add(1);
                doc["essence"] = doc["mistEssence"]!.GetValue<int>();
                doc.Remove("mistEssence");
                return doc;
            },
            [2] = doc =>
            {
                order.Add(2);
                doc["contentNoteAcknowledged"] = false;
                return doc;
            },
        });

        var migrated = SaveCodec.ReadProfile(profile.ToJsonString(), migrator);

        Assert.That(order, Is.EqualTo(new[] { 1, 2 }), "one version at a time, in order");
        Assert.That(migrated.SchemaVersion, Is.EqualTo(3));
        Assert.That(migrated.Essence, Is.EqualTo(7));
        Assert.That(migrated.ContentNoteAcknowledged, Is.False);
        Assert.That(() => SaveCodec.ReadProfile(profile.ToJsonString(), new SaveMigrator(2)),
            Throws.TypeOf<SaveException>().With.Property(nameof(SaveException.Problem)).EqualTo(SaveProblem.Corrupt), "a missing step");
    }

    [Test]
    [Property("Requirement", "SET-02")]
    [Property("Requirement", "SET-03")]
    [Property("Requirement", "SET-04")]
    [Property("Requirement", "SET-05")]
    [Property("Requirement", "SET-06")]
    public void SettingsAreAlwaysInRange()
    {
        var wild = new GameSettings(MasterVolume: 3, MusicVolume: -1, SfxVolume: double.NaN, CameraPanSpeed: 99, ZoomSpeed: 0,
            WindowMode: (WindowMode)42, ResolutionScale: 0.1, MistSpeed: 3, ReducedBlood: true, NoSuggestive: true);

        var settings = wild.Normalised();

        Assert.That((settings.MasterVolume, settings.MusicVolume, settings.SfxVolume), Is.EqualTo((1.0, 0.0, GameSettings.Default.SfxVolume)));
        Assert.That((settings.CameraPanSpeed, settings.ZoomSpeed), Is.EqualTo((GameSettings.MaxMultiplier, GameSettings.MinMultiplier)));
        Assert.That(settings.WindowMode, Is.EqualTo(WindowMode.Windowed));
        Assert.That(settings.ResolutionScale, Is.EqualTo(GameSettings.MinResolutionScale));
        Assert.That(settings.MistSpeed, Is.EqualTo(1), "1, 2 or 4; never paused");
        Assert.That(settings.Filters, Is.EqualTo(new ContentFilters(ReducedBlood: true, NoSuggestive: true)));
        Assert.That(GameSettings.Default.Normalised(), Is.EqualTo(GameSettings.Default));
    }
}
