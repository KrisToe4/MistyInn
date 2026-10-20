# DECISIONS

Decisions for Misty Inn, numbered from D-001. **Accepted** decisions were made or approved by the human; the agent's
proposed details were approved with SDD v1.0 (2026-10-20). **Proposed** marks a new decision awaiting the human.
**Superseded** decisions are kept for history. Continue the numbering; never reuse a number.

| ID | Title | Status | Date | Requirements |
|---|---|---|---|---|
| D-001 | Engine: Godot 4.7.2 .NET with C# | Accepted | 2026-10-19 | STR-01, section 4 |
| D-002 | Genre: cute third-person inn management | Superseded by D-017 | 2026-10-19 | |
| D-003 | Art direction: chibi toon-shaded 3D | Superseded by D-018 | 2026-10-19 | |
| D-004 | Content ceiling: suggestive themes | Accepted (extended by D-019) | 2026-10-19 | INV-06, RAT-01..04, RAT-07..10 |
| D-005 | Scope: hobby prototype, Windows desktop only | Accepted | 2026-10-19 | section 1.4, SCP-20..26 |
| D-006 | Agent rules: strict; the human runs builds, tests, Godot and git | Accepted | 2026-10-19 | AGT-05 |
| D-007 | Project location and version control | Accepted | 2026-10-19 | STR-08 |
| D-008 | Engine-free Core library with all game rules | Accepted | 2026-10-19 | INV-01, ARC-01..05, STR-02..05 |
| D-009 | Deterministic seeded simulation on a fixed tick | Accepted | 2026-10-19 | INV-02, INV-03, SIM-01..04 |
| D-010 | Content as JSON data with a validator | Accepted | 2026-10-19 | INV-04, DAT-01..06 |
| D-011 | Companions provide Company off-screen, paired by the player | Superseded by D-023 | 2026-10-19 | |
| D-012 | Cozy mode setting | Superseded by D-023 | 2026-10-19 | |
| D-013 | Saves only between cycles | Accepted | 2026-10-19 | SAV-01..06 |
| D-014 | Run of 10 cycles ending in the Convergence; Mist Essence | Accepted | 2026-10-19 | RUN-01..04, MET-01..03 |
| D-015 | Input actions registered in code | Accepted | 2026-10-19 | section 4 |
| D-016 | Only Core has automated tests | Accepted | 2026-10-19 | TST-11, TST-12 |
| D-017 | Genre: dark roguelike autobattler | Accepted | 2026-10-20 | SCP-01..13, sections 6 to 14 |
| D-018 | Art direction: dark stylised 3D | Accepted | 2026-10-20 | ART-10..16 |
| D-019 | Violence: moderate blood | Accepted | 2026-10-20 | RAT-05, RAT-06, RAT-09, ART-14 |
| D-020 | Escalation: wreck, then prey, then hunt | Accepted | 2026-10-20 | ESC-01..04 |
| D-021 | Isometric camera; the player only watches the mist phase | Accepted | 2026-10-20 | CAM-10..13, SCP-13, SCP-25 |
| D-022 | Build model: staff units and the inn layout | Accepted (merging superseded by D-026) | 2026-10-20 | sections 11 to 13 |
| D-023 | Companions are willing staff who choose their work; content filters | Accepted | 2026-10-20 | CMP-01..06, RAT-07, RAT-09, SET-04 |
| D-024 | Agitation, bestiary, combat and staff death rules | Accepted | 2026-10-20 | AGI-*, BST-*, ESC-05..10, INV-12 |
| D-025 | Staff classes and techniques | Accepted | 2026-10-20 | STF-01..05, AGI-05..07, ESC-07 |
| D-026 | Five ranks and upgrade trees of at most two classes | Accepted | 2026-10-20 | STF-06..10 |
| D-027 | A random shop paid with in-run silver only | Accepted | 2026-10-20 | SHP-01..09, SCP-27, STF-11 |
| D-028 | Staff items and inn items; inn items replace relics | Accepted | 2026-10-20 | ITM-01..07, SYN-01, SCP-14, ART-17 |
| D-029 | Fixtures in a fixed build menu | Accepted | 2026-10-20 | FIX-05, FIX-06 |
| D-030 | Company on the floor and in rooms | Accepted | 2026-10-20 | CMP-01..06, BRD-01, RAT-02 |
| D-031 | No choice at Dawn | Accepted | 2026-10-20 | CYC-04 |
| D-032 | Three staff offers and a home-plane draw weight | Accepted | 2026-10-20 | SHP-02, SHP-04 |
| D-033 | Sixteen characters, Mortal-weighted, with hidden ancestry | Accepted | 2026-10-20 | STF-14..16, SYN-01, MET-01, SAV-02 |
| D-034 | SDD v1.0 approved; pre-SDD prototype code removed | Accepted | 2026-10-20 | AGT-01, step 0 |
| D-035 | Keep a classic `.sln` with explicit configuration mapping | Accepted | 2026-10-20 | STR-01 |
| D-036 | Create the Windows export preset in step 1 | Accepted | 2026-10-20 | STR-08, DAT-02, step 1 |

---

## D-001 Engine: Godot 4.7.2 .NET with C#
- **Status:** Accepted
- **Date / step:** 2026-10-19, before the SDD
- **Context:** The human asked for Godot with C#. Godot 4.7.2 mono was already downloaded, and `Godot.NET.Sdk 4.7.2`
  was in the NuGet cache.
- **Decision:** Godot 4.7.2 .NET, `Godot.NET.Sdk/4.7.2`, `net8.0`. The SDK version always equals the editor version.
- **Alternatives considered:** MonoGame, Unity, a web stack.
- **Requirement IDs affected:** STR-01, section 4.

## D-002 Genre: cute third-person inn management
- **Status:** Superseded by D-017 (SDD v0.2)
- **Date / step:** 2026-10-19, before the SDD
- **Context:** The human's first concept: a cute third-person game about an inn at a nexus of converging planes, with
  services at the inn, tavern and brothel, as a roguelike with a daily loop.
- **Decision:** Third-person inn management with Morning, Open, Closing and Night phases.
- **Alternatives considered:** A mystery game, a text adventure, a combat roguelike.
- **Requirement IDs affected:** the v0.1 `DAY-*`, `GST-*`, `SVC-*`, `REP-*` (removed in v0.2).

## D-003 Art direction: chibi toon-shaded 3D
- **Status:** Superseded by D-018 (SDD v0.2)
- **Date / step:** 2026-10-19, SDD v0.1
- **Decision:** Chibi proportions, cel shading, pastels.
- **Requirement IDs affected:** the v0.1 `ART-*` (removed in v0.2).

## D-004 Content ceiling: suggestive themes
- **Status:** Accepted; extended by D-019 (violence)
- **Date / step:** 2026-10-19, SDD v0.1
- **Context:** The inn is also a brothel. The human chose Suggestive over cozy-and-implied or explicit.
- **Decision:** Flirting, innuendo, romance and alluring outfits are allowed. Company sessions are off-screen. There is
  no nudity and no explicit content, and every character is an adult, enforced in the data. Section 17 is the single
  source for these rules.
- **Alternatives considered:** Cozy and implied (Teen); explicit adult content.
- **Requirement IDs affected:** INV-06, RAT-01..04, RAT-07..10, CRE-07, STF-08, DAT-04.

## D-005 Scope: hobby prototype, Windows desktop only
- **Status:** Accepted
- **Date / step:** 2026-10-19, SDD v0.1
- **Context:** The human chose a hobby/prototype scope over a vertical slice or a full 1.0 plan.
- **Decision:** The SDD guides development and has no release plan. Windows, mouse and keyboard, single player.
  Release work is in the backlog (section 24).
- **Alternatives considered:** A vertical slice followed by 1.0; a full 1.0 specification.
- **Requirement IDs affected:** section 1.4, SCP-20..26.

## D-006 Agent rules: strict
- **Status:** Accepted
- **Date / step:** 2026-10-19, SDD v0.1
- **Context:** The human chose the same rules as their mellenger project.
- **Decision:** Agents write code and documents only. The human runs builds, tests, the Godot editor, headless Godot
  and git, and agents mark results NOT RUN. (The skeleton was built and smoke-run once by the agent before this rule
  existed; see `PROGRESS.md`.)
- **Alternatives considered:** Agents may build and test but not use git; agents may also commit on branches.
- **Requirement IDs affected:** AGT-05.

## D-007 Project location and version control
- **Status:** Accepted
- **Date / step:** 2026-10-19, before the SDD
- **Context:** The human wants to push from GitKraken.
- **Decision:** The repository is at `C:\Users\Kitten\Documents\source\MistyInn`, on branch `main`. The human makes
  every commit and push.
- **Alternatives considered:** None.
- **Requirement IDs affected:** STR-08.

## D-008 Engine-free Core library
- **Status:** Accepted (approved with SDD v1.0)
- **Date / step:** 2026-10-19, SDD v0.1; reaffirmed in v0.2
- **Context:** Godot node scripts cannot be unit-tested without the engine. An autobattler's whole outcome (movement,
  agitation, targeting, combat) is rules, which is where most bugs will be.
- **Decision:** All rules live in `MistyInn.Core` (no Godot reference) and are driven through `GameSession`. Nodes only
  present state and send setup commands. A whole mist phase can be simulated without the engine (`ARC-05`).
- **Alternatives considered:** Rules in node scripts, tested with GdUnit4 inside the engine.
- **Requirement IDs affected:** INV-01, ARC-01..05, STR-02..05.

## D-009 Deterministic seeded simulation
- **Status:** Accepted (approved with SDD v1.0)
- **Date / step:** 2026-10-19, SDD v0.1; changed in v0.2
- **Context:** Runs should be reproducible by seed, tests need exact results, and the Dawn timeline must match what
  the player saw.
- **Decision:** One seeded `IRandomSource` per run. Core runs fixed 1/30 s steps, independent of Godot physics; speed
  controls run more steps per frame. Core owns positions and A* paths; views interpolate.
- **Alternatives considered:** Godot navigation and physics for movement (not deterministic, not testable in Core).
- **Requirement IDs affected:** INV-02, INV-03, SIM-01..04.

## D-010 Content as JSON data
- **Status:** Accepted (approved with SDD v1.0)
- **Date / step:** 2026-10-19, SDD v0.1
- **Context:** Planes, creatures, staff, fixtures and relics will grow the most and should be easy to edit and
  balance.
- **Decision:** JSON files in `data/`, loaded through `IContentSource`, checked by a validator in the tests and at
  load time. Effects, appetites, triggers and roles are closed sets of kinds.
- **Alternatives considered:** Godot `.tres` custom resources (editor-friendly, but they tie content to the engine and
  cannot be loaded in Core tests).
- **Requirement IDs affected:** INV-04, DAT-01..06.

## D-011 Companions paired by the player
- **Status:** Superseded by D-023 (SDD v0.2)
- **Date / step:** 2026-10-19, SDD v0.1
- **Decision:** The player paired guests with companions in a matchmaking mini-game.

## D-012 Cozy mode setting
- **Status:** Superseded by D-023 (SDD v0.2)
- **Date / step:** 2026-10-19, SDD v0.1
- **Decision:** A setting that removed the Company service. Replaced by the content filters, which fit the darker
  tone.

## D-013 Saves only between cycles
- **Status:** Accepted (approved with SDD v1.0)
- **Date / step:** 2026-10-19, SDD v0.1; changed from "days" to "cycles" in v0.2
- **Context:** Saving in the middle of a mist phase would mean serialising every creature, path and timer.
- **Decision:** An autosave when the player leaves Dawn; Continue resumes at the saved Dusk.
- **Alternatives considered:** Saving at any time.
- **Requirement IDs affected:** SAV-01..06.

## D-014 Run structure and meta-progression
- **Status:** Accepted (approved with SDD v1.0)
- **Date / step:** 2026-10-19, SDD v0.1; changed in v0.2
- **Context:** The skeleton's run had no win condition.
- **Decision:** A run is 10 cycles, and cycle 10 is the Convergence (all planes at once). Mist Essence, earned per run,
  unlocks planes, staff, fixtures and relics. The bestiary is also kept across runs (D-024).
- **Alternatives considered:** Endless runs with a high score; no meta-progression.
- **Requirement IDs affected:** RUN-01..04, MET-01..03.

## D-015 Input actions registered in code
- **Status:** Accepted (approved with SDD v1.0)
- **Date / step:** 2026-10-19, SDD v0.1; still applies in v0.2 (now for camera and UI keys)
- **Context:** The skeleton registers its key bindings in `InputActions.cs` instead of `project.godot`, which avoids
  hand-writing serialised input events.
- **Decision:** Keep code registration with defaults. Actions already defined in Project Settings are left alone.
- **Alternatives considered:** The Input Map in `project.godot`.
- **Requirement IDs affected:** section 4, CAM-10.

## D-016 Only Core has automated tests
- **Status:** Accepted (approved with SDD v1.0)
- **Date / step:** 2026-10-19, SDD v0.1
- **Context:** The game is a hobby prototype; in-engine tests (GdUnit4) add a toolchain and run slowly.
- **Decision:** NUnit tests on Core. Scenes are checked by each step's demo script and the headless smoke run.
- **Alternatives considered:** GdUnit4 scene tests.
- **Requirement IDs affected:** TST-11, TST-12.

## D-017 Genre: dark roguelike autobattler
- **Status:** Accepted
- **Date / step:** 2026-10-20, SDD v0.2 (review point 1)
- **Context:** The human wants a darker, more mature tone. The planes are full of hostile creatures, and each cycle is
  an attempt to survive by keeping the guests occupied until it ends. The human wants an autobattler: the player sets
  up, and the action then happens without player interaction.
- **Decision:** Cycles of Dusk (untimed setup), Mist (automatic, timed) and Dawn (aftermath). Creatures have agitation
  and turn hostile when it peaks. Reputation, tips and the cute interactions are removed.
- **Alternatives considered:** Keeping the v0.1 management game; a survival game with direct control.
- **Requirement IDs affected:** SCP-01..13, sections 6 to 14.

## D-018 Art direction: dark stylised 3D
- **Status:** Accepted
- **Date / step:** 2026-10-20, SDD v0.2
- **Context:** The human chose this over keeping chibi as a contrast, or a grounded, grim style.
- **Decision:** Normal proportions, simple forms, painterly or toon shading in muted colours; warm light inside, cold
  mist outside; one signature colour per plane.
- **Alternatives considered:** Chibi as contrast; grounded and grim.
- **Requirement IDs affected:** ART-10..16.

## D-019 Violence: moderate blood
- **Status:** Accepted
- **Date / step:** 2026-10-20, SDD v0.2
- **Context:** The darker tone needs a violence ceiling. The human chose moderate blood over stylised or gore.
- **Decision:** Blood splashes and decals, injuries and deaths with weight; no dismemberment or lingering injuries. A
  Blood filter reduces it. Violence is never combined with suggestive content.
- **Alternatives considered:** Stylised with little blood; gore.
- **Requirement IDs affected:** RAT-04..06, RAT-09, ART-14, INV-06, SCP-26.

## D-020 Escalation: wreck, then prey, then hunt
- **Status:** Accepted (all details approved with SDD v1.0)
- **Date / step:** 2026-10-20, SDD v0.2
- **Context:** The human chose "all of the above" for what a turned creature does.
- **Decision:** A turned creature first wrecks the nearest fixture, then attacks the nearest creature or staff member,
  then hunts the innkeeper. It moves on after a success or a timeout. Creatures may skip or retime stages in data.
  Contagion makes nearby creatures more agitated when one turns.
- **Alternatives considered:** A single behaviour (only hunting the innkeeper, only wrecking the inn, or only preying on
  others).
- **Requirement IDs affected:** ESC-01..04, AGI-09.

## D-021 Isometric camera; the player only watches the mist phase
- **Status:** Accepted
- **Date / step:** 2026-10-20, SDD v0.2
- **Context:** The human chose to drop third person, and to allow only watching (pause and speed) during the mist
  phase.
- **Decision:** An isometric-style camera in every phase, with drag-and-drop placement at Dusk. In the mist phase the
  player can pause, change speed, select and follow; nothing else.
- **Alternatives considered:** Walking the inn in third person to set up; a board view for setup with third person for
  watching; one emergency action per cycle.
- **Requirement IDs affected:** CAM-10..13, SCP-13, SCP-25, ARC-11.

## D-022 Build model: staff units and the inn layout
- **Status:** Accepted
- **Date / step:** 2026-10-20, SDD v0.2
- **Context:** The human chose "both" over a staff shop alone or a layout puzzle alone.
- **Decision:** A staff shop with tiers, rerolls, a shared pool, merging (three to level up) and origin and role
  synergies; plus fixtures on the inn's grid with radius and adjacency effects. Staff need fixtures to work, which
  ties the two together.
- **Alternatives considered:** A staff shop and synergies only; an inn layout puzzle only.
- **Requirement IDs affected:** STF-*, SHP-*, SYN-*, BRD-*, FIX-*.

## D-023 Companions choose their work; content filters
- **Status:** Accepted (all details approved with SDD v1.0)
- **Date / step:** 2026-10-20, SDD v0.2
- **Context:** The human chose "willing staff, chosen work" for the brothel. With hostile creatures, consent needs
  explicit rules.
- **Decision:** Companions are staff placed in lounge booths. They choose whom to entertain, and the simulation always
  honours their refusals. Company is only with sapient, adult creatures, happens off-screen, and ends at once when
  violence is near. Two content filters (Blood, Suggestive) replace Cozy mode; with the Suggestive filter on,
  companions become hosts with the same mechanics.
- **Alternatives considered:** The player pairs creatures with companions (v0.1); lore only; removing the brothel.
- **Requirement IDs affected:** CMP-01..06, CRE-07, RAT-03, RAT-04, RAT-07, RAT-09, SET-04, DAT-04, DAT-06.

## D-024 Agitation, bestiary, combat and staff death rules
- **Status:** Accepted (approved with SDD v1.0)
- **Date / step:** 2026-10-20, SDD v0.2
- **Context:** The human's concept is that each cycle is about figuring out how to keep the guests occupied. The
  autobattler needs rules for that, and for what happens when it fails.
- **Decision:**
  - Agitation from 0 to 100 with Calm, Restless, Menacing and Turned states; hidden appetites (craves or likes) and
    triggers from closed sets; contagion when a creature turns.
  - The bestiary reveals creatures in four levels by observation and keeps that knowledge across runs; this is the
    "figuring out".
  - Combat is deterministic, with no random hits or misses; the innkeeper is a placed unit whose fall ends the run.
  - A fallen staff member is wounded (70 %) or dead (30 %) at Dawn, rolled with the run's RNG.
  - Every cause is recorded in the cycle log, shown as the Dawn timeline.
- **Alternatives considered:** Visible appetites from the start (no discovery); random combat rolls; permanent death
  on every fall; staff who never die.
- **Requirement IDs affected:** AGI-01..10, BST-01..04, ESC-05..10, STF-07, INV-12, UI-07.

## D-025 Staff classes and techniques
- **Status:** Accepted (all details approved with SDD v1.0)
- **Date / step:** 2026-10-20, SDD v0.2 (review point 2)
- **Context:** The human wants staff to have a class, and possibly several, that decides how they deal with guests.
  Starting classes: Companion/Server (sex, food and drink), Gambler (cards, dice), Fighter (fists, weapons) and
  Entertainer (dance, song).
- **Decision:** Four classes, each with techniques: Companion/Server is **one** class (Company, Food, Drink); Gambler
  (Cards, Dice); Fighter (Fists, Weapons); Entertainer (Dance, Song). A character has a class when it knows one of its
  techniques. The v0.2 Guard, Warden and Fixer roles are dropped. *Agent's detail, approved with v1.0:* new appetites `Spectacle` (Dance) and
  `Brawl` (Fists sparring with calm creatures); preferred techniques on appetites (×1.25); the order in which staff
  choose what to do (`STF-04`), with fighting first.
- **Alternatives considered:** Companion and Server as two classes; keeping Warden and Fixer.
- **Requirement IDs affected:** STF-01..05, AGI-05..07, ESC-03, ESC-07, ESC-10.

## D-026 Five ranks and upgrade trees
- **Status:** Accepted (all details approved with SDD v1.0)
- **Date / step:** 2026-10-20, SDD v0.2
- **Context:** The human wants upgrades through a gacha-like shop: buying a character you already own upgrades them,
  and each upgrade adds a technique from the main class or another class. Five tiers; at most two classes in a tree;
  items can add more class tags.
- **Decision:** Ranks ★1 to ★5; each rank adds one technique. Buying an owned character's offer raises them **exactly
  one rank**, whatever the offer's rank, at the offer's price, and the player picks the technique. Trees span at most
  two classes. Hiring a character you don't own gives them at the offer's rank with a predetermined build. This
  replaces three-copy merging. *Agent's detail, approved with v1.0:* 2 or 3 options per rank; options may depend on earlier choices; owned ★5
  offers cannot be bought; selling returns half.
- **Alternatives considered:** Three copies to merge (v0.2 point 1); training a second class at Dawn; dual-class
  characters fixed in data; up to three classes.
- **Requirement IDs affected:** STF-06..10, UI-06, UI-08.

## D-027 A random shop paid with in-run silver only
- **Status:** Accepted (all details approved with SDD v1.0)
- **Date / step:** 2026-10-20, SDD v0.2
- **Context:** The human: no real money, only in-game currency for each run. All staff, dead or not, can appear at
  different ranks with predetermined techniques; higher ranks are rare early and more common later. Rerolls escalate
  in cost, reset each cycle, and can be made cheaper by items and inn upgrades.
- **Decision:** As above, plus SCP-27 (no real money in any form). *Agent's detail, approved with v1.0:* 5 staff offers and 3 item offers per
  Dusk; the rank odds table (`SHP-03`); pity weighting ×1.5 for owned characters; reroll 2, +1 per reroll in a cycle;
  Lock; price = base cost + 2 per rank above ★1.
- **Alternatives considered:** A shared pool with limited copies (v0.2 point 1).
- **Requirement IDs affected:** SHP-01..09, SCP-27, STF-11.

## D-028 Staff items and inn items
- **Status:** Accepted (all details approved with SDD v1.0)
- **Date / step:** 2026-10-20, SDD v0.2
- **Context:** The human: items are part of the shop, for staff to carry or for the inn. Inn items are like relics in
  other games, with a visual component, and replace any other relic system. Class tags on items only count for
  synergies.
- **Decision:** As above. Relics and their Dawn choice are removed. *Agent's detail, approved with v1.0:* two item slots per staff member; item
  storage of 8; up to 12 inn items on display; Common, Rare and Cursed rarities; selling for half; the example items
  in `ITM-06`.
- **Alternatives considered:** Relics chosen at Dawn alongside items.
- **Requirement IDs affected:** ITM-01..07, SYN-01, SCP-14, ART-17, CYC-04.

## D-029 Fixtures in a fixed build menu
- **Status:** Accepted
- **Date / step:** 2026-10-20, SDD v0.2
- **Context:** The human: fixtures are a separate menu that is always the same and acts as a different money sink.
- **Decision:** The build menu holds every unlocked fixture at a fixed price, and the supplies (*agent's detail, approved with v1.0*: supplies
  live here too).
- **Alternatives considered:** Fixtures as random shop items.
- **Requirement IDs affected:** FIX-05, FIX-06, UI-03.

## D-030 Company on the floor and in rooms
- **Status:** Accepted (all details approved with SDD v1.0)
- **Date / step:** 2026-10-20, SDD v0.2
- **Context:** The human: Company can be limited, flirty and less overt in frame; if a room is free the pair sometimes
  goes into a room, which satisfies the guest more but takes the staff member out of circulation for a while.
- **Decision:** Floor company in frame within `RAT-02`; room sessions off-screen behind a closed door, stronger, with
  the companion out of play. Refusals and the sapient-adult rule still apply. *Agent's detail, approved with v1.0:* two back rooms at the start
  (more from inn items); 8 s of floor company before a room; 25 s sessions at three times the floor rate; a 10 s rest;
  turned creatures can attack an occupied room's door, which ends the session before any violence is shown.
- **Alternatives considered:** Lounge booths with a curtain (v0.2 point 1).
- **Requirement IDs affected:** CMP-01..06, BRD-01, RAT-02, ART-15, TST-10.

## D-031 No choice at Dawn
- **Status:** Accepted
- **Date / step:** 2026-10-20, SDD v0.2
- **Context:** Inn items replace relics (D-028), so the Dawn relic pick is gone.
- **Decision:** Dawn is the summary, the timeline and repairs; all choices happen at Dusk.
- **Requirement IDs affected:** CYC-04, UI-07, UI-08.

## D-032 Three staff offers and a home-plane draw weight
- **Status:** Accepted
- **Date / step:** 2026-10-20, SDD v1.0
- **Context:** The human wants 3 staff in the shop at a time, and a bias towards characters whose home plane is one
  the inn will encounter that night.
- **Decision:** 3 staff offers and 3 item offers per Dusk. A ×2 draw weight for characters whose plane (plane-born
  origin or ancestry, hidden or not) converges that cycle, stacked with the ×1.5 pity weight, also on rerolls.
- **Alternatives considered:** 5 staff offers (v0.2).
- **Requirement IDs affected:** SHP-02, SHP-04.

## D-033 Sixteen characters, Mortal-weighted, with hidden ancestry
- **Status:** Accepted
- **Date / step:** 2026-10-20, SDD v1.0
- **Context:** The human wants 16 characters, 4 per core class, weighted towards the Mortal Realm, with a hidden
  ancestry property linking a character to a plane (even a locked one), revealed by observation.
- **Decision:** About 10 Mortal and 6 plane-born characters; about half of the Mortal ones have an ancestry. Ancestry
  weights the shop from the start. It is revealed the first time the character soothes or fights a creature of that
  plane, kept in the profile across runs, and then gives ×1.25 soothe with that plane's creatures and counts for that
  plane's origin synergy.
- **Alternatives considered:** An even spread of origins; revealing ancestry by rank; never revealing it.
- **Requirement IDs affected:** STF-01, STF-14..16, SHP-04, SYN-01, MET-01, SAV-02, UI-06, DAT-04.

## D-034 SDD v1.0 approved; pre-SDD prototype code removed
- **Status:** Accepted
- **Date / step:** 2026-10-20, before step 0
- **Context:** The human approved the SDD as the initial version and asked for the code generated before the SDD to
  be removed, so the repository is ready for an initial commit and step 0.
- **Decision:** SDD v1.0 is approved, and every Proposed decision so far is Accepted. Removed: `scenes/Main.tscn`,
  `scripts/*.cs` (Main, Player, Guest, Realms, InputActions) and their `.uid` files, the `.godot/` cache and build
  output, and the `run/main_scene` setting in `project.godot` (step 2 sets it again). Kept: the Godot project shell
  (`project.godot`, `MistyInn.csproj`, `MistyInn.sln`, `icon.svg`, `icon.svg.import`), `.gitignore`,
  `.gitattributes`, the documents and `tools/`. From now on the agent rules apply in full (AGT-05).
- **Requirement IDs affected:** AGT-01, AGT-05, steps 0 to 2.

## D-035 Keep a classic `.sln` with explicit configuration mapping
- **Status:** Accepted
- **Date / step:** 2026-10-20, step 0 report (SDD v1.1)
- **Context:** Godot expects `MistyInn.sln` with its Debug, ExportDebug and ExportRelease configurations. The .NET 10 SDK
  creates `.slnx` by default, and plain projects (Core, tests) have only Debug and Release.
- **Decision:** Keep the classic `.sln` and never run `dotnet new sln`. Core and the tests map ExportDebug to Debug and
  ExportRelease to Release, written into the `.sln` explicitly rather than left to `dotnet sln add`.
- **Alternatives considered:** Switching to `.slnx` (briefly adopted, then reverted by the human the same day:
  Godot does not support `.slnx`).
- **Requirement IDs affected:** STR-01.

## D-036 Create the Windows export preset in step 1
- **Status:** Accepted
- **Date / step:** 2026-10-20, step 0 report (SDD v1.1)
- **Context:** `export_presets.cfg` is ignored by git today, while STR-08 says to commit it. It is normally created in
  the Godot editor.
- **Decision:** In step 1 the agent stops ignoring it (and ignores `builds/`), and gives the human the editor steps to
  create the Windows Desktop preset (export path `builds/windows/MistyInn.exe`, `data/*.json` in the non-resource
  files filter). The human creates it, and it is committed.
- **Alternatives considered:** Waiting until step 11; writing the file by hand.
- **Requirement IDs affected:** STR-08, STR-11, DAT-02.
