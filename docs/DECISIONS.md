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
| D-037 | Shared build settings scoped to Core and tests | Accepted | 2026-10-20 | STR-06 |
| D-038 | Random source: xoshiro256** seeded by SplitMix64 | Accepted | 2026-10-20 | INV-02, SAV-01 |
| D-039 | Full event buffer drops the oldest event | Accepted | 2026-10-20 | ARC-12, INV-05 |
| D-040 | Bounded cycle log with a truncation marker and key strings | Accepted | 2026-10-20 | INV-12, INV-05 |
| D-041 | GameSession skeleton without a run state | Accepted | 2026-10-20 | ARC-01, ARC-11 |
| D-042 | Core owns the fixed-step accumulator | Accepted | 2026-10-20 | SIM-02, INV-03 |
| D-043 | Staff do not block movement; one unit per tile | Accepted | 2026-10-20 | BRD-02, BRD-03 |
| D-044 | Built-in step 2 content and layout | Accepted | 2026-10-20 | BRD-01, PLN-02, PLN-04, INV-04 |
| D-045 | A new run is a new session; the first Dusk raises no events | Accepted | 2026-10-20 | ARC-01, RUN-01 |
| D-046 | Game time runs only during the mist | Accepted | 2026-10-20 | SIM-01, CYC-01, INV-03 |
| D-047 | ContinueFromDawn and new refusal reasons | Accepted | 2026-10-20 | ARC-10, ARC-11, CYC-04, ESC-06 |
| D-048 | Placeholder HUD and input for step 2 | Accepted | 2026-10-20 | CAM-12, CAM-13, CYC-10, UI-09 |
| D-049 | Orthographic camera at 50° with a 45° start | Accepted | 2026-10-20 | CAM-10 |
| D-050 | Every room must stay reachable on its own | Accepted | 2026-10-20 | BRD-03 |
| D-051 | Masteries and one more technique per class | Accepted | 2026-10-20 | STF-02, STF-06, STF-07, STF-17 |
| D-052 | Six more effect kinds | Accepted | 2026-10-20 | DAT-03 |
| D-053 | Generic placeholder content | Accepted | 2026-10-20 | STF-14, CRE-03, DAT-01 |
| D-054 | The data is built into Core; an export falls back to it as a whole | Accepted | 2026-10-20 | DAT-02, DAT-04 |
| D-055 | Content file conventions | Accepted | 2026-10-20 | DAT-01..05 |
| D-056 | The innkeeper is separate from the roster; starting staff in balance.json | Accepted | 2026-10-20 | RUN-01, STF-14, ESC-06 |
| D-057 | Upgrade tree layout for the placeholder roster | Accepted | 2026-10-20 | STF-07, STF-10, STF-17 |
| D-058 | Techniques say whether they need a fixture; fixtures name the techniques they enable | Accepted | 2026-10-20 | STF-02, FIX-02, DAT-03 |
| D-059 | No scene markers: the board view is built from inn.json | Accepted | 2026-10-20 | SIM-04, TST-07 |
| D-060 | Step 4 adds providers for Order and Darkness | Accepted | 2026-10-20 | AGI-05, AGI-06 |
| D-061 | Order and Darkness: two fixtures, and Cards and Tales | Accepted | 2026-10-20 | FIX-02, STF-02, AGI-05 |
| D-062 | Cold is the draught at the front door | Accepted | 2026-10-20 | AGI-08 |
| D-063 | The starting kit is in storage | Accepted | 2026-10-20 | RUN-01, FIX-05, FIX-06 |
| D-064 | Service, seeking, fixtures and bestiary rules | Accepted | 2026-10-20 | STF-04, CRE-04, AGI-03, AGI-06, AGI-07, FIX-01, BST-02 |
| D-065 | Staff know only their base technique until step 6 | Accepted | 2026-10-20 | STF-06, STF-10 |
| D-066 | The bestiary lives outside the session | Accepted | 2026-10-20 | BST-04, SAV-02 |
| D-067 | What the cycle log records | Accepted | 2026-10-20 | INV-12, UI-07 |
| D-068 | Internal test hooks on GameSession | Accepted | 2026-10-20 | TST-01, ARC-03 |
| D-069 | Each back room is its own zone with a RoomDoor tile | Accepted | 2026-10-20 | BRD-01, BRD-03, CMP-04, CMP-05 |
| D-070 | Integrity takes a share of fixture damage, and walls | Accepted | 2026-10-20 | BRD-06, ESC-01 |
| D-071 | Company room rules and tuning | Accepted | 2026-10-20 | CMP-02, CMP-04, CMP-05 |
| D-072 | Fixtures repair for free at Dawn; integrity costs silver | Accepted | 2026-10-20 | BRD-07, ESC-10 |
| D-073 | Combat details | Accepted | 2026-10-20 | ESC-01, ESC-03, ESC-05, ESC-06, ESC-07, ESC-09, STF-02 |
| D-074 | A debug dev panel for staff and planes | Accepted | 2026-10-20 | ARC-11, PLN-03 |
| D-075 | Content filters in Core change only what is shown | Accepted | 2026-10-20 | RAT-09, SET-04, CMP-06, DAT-06, INV-02 |
| D-076 | Older tests run on sturdy content | Accepted | 2026-10-20 | TST-01, INV-08, AGT-14 |

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

## D-037 Shared build settings scoped to Core and tests
- **Status:** Accepted (approved by the human after step 1)
- **Date / step:** 2026-10-20, step 1
- **Context:** STR-06 puts nullable, latest C#, implicit usings and warnings as errors on Core and the tests, but not on
  the Godot project. `Directory.Build.props` at the root is picked up by every project, including `MistyInn.csproj`.
- **Decision:** The settings apply only when `$(MSBuildProjectName)` starts with `MistyInn.Core`. The Godot project keeps
  its own `Nullable` setting. When Godot exports, it builds Core through the `ProjectReference` with the configuration
  `ExportRelease`, which a plain project does not know; Core then turns on `Optimize`, like Release.
- **Alternatives considered:** A `Directory.Build.props` in `src/` and another in `tests/` (two files to keep in step).
- **Requirement IDs affected:** STR-06.

## D-038 Random source: xoshiro256** seeded by SplitMix64
- **Status:** Accepted (approved by the human after step 1)
- **Date / step:** 2026-10-20, step 1
- **Context:** INV-02 forbids `System.Random`, and SAV-01 saves the generator's state, so the algorithm must be fixed and
  its state small and serialisable.
- **Decision:** `SeededRandom` implements xoshiro256** with a 64-bit seed expanded by SplitMix64; its state is four
  `ulong` words (`RandomState`). `NextInt` rejects biased values so every result is equally likely. A test pins the
  output for seed 42 (values computed independently in Python), so an accidental change to the algorithm fails the
  tests. Changing the algorithm later breaks saves and needs a decision. The namespace is `MistyInn.Core.Randomness`, to
  avoid clashing with `System.Random`.
- **Alternatives considered:** PCG32 (smaller state, shorter period); `System.Random` with a seed (forbidden, and its
  algorithm is not guaranteed across .NET versions).
- **Requirement IDs affected:** INV-02, SAV-01, TST-01.

## D-039 Full event buffer drops the oldest event
- **Status:** Accepted (approved by the human after step 1)
- **Date / step:** 2026-10-20, step 1
- **Context:** ARC-12 needs a bounded event buffer (INV-05), but does not say what happens when it fills between
  drains.
- **Decision:** The buffer (default 4,096 events) drops the oldest event and counts it in `DroppedEventCount`. Events
  only drive presentation; the cycle log is the authoritative record (INV-12), so a dropped event never changes the
  game. The GameRoot node (step 2) should log a warning if the count rises.
- **Alternatives considered:** Throwing when full (would crash the game over a view lagging behind); refusing new
  events (would keep stale ones and lose the latest state).
- **Requirement IDs affected:** ARC-12, INV-05.

## D-040 Bounded cycle log with a truncation marker and key strings
- **Status:** Accepted (approved by the human after step 1)
- **Date / step:** 2026-10-20, step 1
- **Context:** INV-12 needs every cause recorded, and INV-05 needs a cap.
- **Decision:** The log holds up to 20,000 entries per cycle. When one slot is left, a `log.truncated` entry is written
  and later records are only counted, so the timeline can always show that it was cut. Each entry has a sequence
  number, the game time, a `Kind` and a `Cause` as machine-readable keys (for example `agitation.rose` and
  `trigger.light`, turned into text by the UI with `Tr()`), and optional subject and source entities.
- **Alternatives considered:** Dropping the oldest entries (would lose the start of a chain reaction); enums for kinds
  and causes (would need a Core change for every new content cause).
- **Requirement IDs affected:** INV-12, INV-05, UI-07.

## D-041 GameSession skeleton without a run state
- **Status:** Accepted (approved by the human after step 1)
- **Date / step:** 2026-10-20, step 1
- **Context:** ARC-01 says `GameSession` holds the current `RunState`, but runs, phases and the fixed-step tick belong to
  step 2.
- **Decision:** In step 1, `GameSession` has the random source, the ID allocator, the event buffer, the cycle log, a
  game clock advanced by `Tick`, `Execute` and `DrainEvents`. `Execute` refuses every command with `NotSupported` until
  a step adds its handler. `RunState`, concrete commands and concrete events are added by the steps that need them.
- **Alternatives considered:** An empty `RunState` placeholder now (dead code until step 2).
- **Requirement IDs affected:** ARC-01, ARC-03, ARC-11.

## D-042 Core owns the fixed-step accumulator
- **Status:** Accepted (approved by the human after step 2)
- **Date / step:** 2026-10-20, step 2
- **Context:** SIM-02 says GameRoot accumulates frame time and runs the due fixed steps; INV-03 says time advances only
  through `GameSession.Tick`, with a fixed step.
- **Decision:** `Tick(delta)` takes real frame time, multiplies it by the speed, and runs the due 1/30 s steps inside
  Core. The owed time is kept as an integer (TimeSpan ticks × 30), so whole steps are exact and the outcome never
  depends on the frame rate (tested). One `Tick` runs at most 120 steps; a longer frame drops the backlog instead of
  freezing to catch up. `InterpolationAlpha` tells views how far the next step is.
- **Alternatives considered:** GameRoot accumulating and calling `Tick(step)` repeatedly, as SIM-02 is worded (the same
  result, but the rule would live in an untested node).
- **Requirement IDs affected:** SIM-02, SIM-03, INV-03. SIM-02's wording should follow (recommended SDD change).

## D-043 Staff do not block movement; one unit per tile
- **Status:** Accepted (approved by the human after step 2)
- **Date / step:** 2026-10-20, step 2
- **Context:** The SDD places staff on tiles but does not say whether creatures can walk through them.
- **Decision:** One unit per tile; creatures walk past staff (staff never block a path). Staff may stand anywhere on
  the inn floor, including the two tiles in front of the door; only fixtures are kept off them (BRD-05). The path rule
  (BRD-03) therefore matters only for blocking fixtures, from step 4; `InnBoard.WouldBlockPath` is ready for them.
  Rooms are one zone until step 5 gives each room its own door rules.
- **Alternatives considered:** Staff blocking tiles (a staff member in the doorway would lock the inn).
- **Requirement IDs affected:** BRD-02, BRD-03, BRD-05.

## D-044 Built-in step 2 content and layout
- **Status:** Accepted (approved by the human after step 2)
- **Date / step:** 2026-10-20, step 2
- **Context:** Step 2 uses a hard-coded layout; content files arrive in step 3.
- **Decision:** `DefaultContent` holds a 26 × 20 map: the inn (14 × 10 floor inside walls) in the middle of the
  clearing, with the front door on the south wall and five mist gates at the edges. It has every BRD-01 zone (two back
  rooms with doorways), the five planes (three unlocked), one placeholder creature per plane, three starting units
  (the innkeeper and two staff) and the section 6.4 tuning. Planes take the gates in map order. `GameContent` checks
  only what the simulation relies on; step 3 replaces this with `data/*.json` and the full validator.
- **Alternatives considered:** Writing `data/inn.json` now (step 3 work).
- **Requirement IDs affected:** BRD-01, PLN-02, PLN-04, INV-04.

## D-045 A new run is a new session; the first Dusk raises no events
- **Status:** Accepted (approved by the human after step 2)
- **Date / step:** 2026-10-20, step 2
- **Context:** ARC-10 lists `StartRun(seed)`, but there is no title screen until step 9.
- **Decision:** A run is one `GameSession`, created with its seeded random source; GameRoot makes one at start (seed
  from its `Seed` export, or random). The session starts at Dusk of cycle 1 with the forecast drawn, and the views read
  that starting state through queries, so no events are raised for it. `StartRun` waits for the title screen (steps 7
  and 9).
- **Alternatives considered:** A `StartRun` command on an empty session now.
- **Requirement IDs affected:** ARC-01, ARC-10, RUN-01.

## D-046 Game time runs only during the mist
- **Status:** Accepted (approved by the human after step 2)
- **Date / step:** 2026-10-20, step 2
- **Context:** SIM-01 measures time from the start of the mist; Dusk and Dawn are untimed (SIM-02, CYC-01).
- **Decision:** `Tick` does nothing at Dusk, at Dawn or at speed 0, and `Time` restarts at 0 when the mist begins. Three
  step 1 tests that ticked at Dusk now begin the mist first, or compare the state hash instead of the time; what they
  check is unchanged.
- **Alternatives considered:** A clock that runs in every phase (nothing would use it).
- **Requirement IDs affected:** SIM-01, SIM-02, CYC-01, INV-03, INV-08.

## D-047 ContinueFromDawn and new refusal reasons
- **Status:** Accepted (approved by the human after step 2)
- **Date / step:** 2026-10-20, step 2
- **Context:** CYC-04 says moving on from Dawn starts the next Dusk, but ARC-10 has no command for it; the step's rules
  need more refusal reasons than ARC-11 lists.
- **Decision:** New command `ContinueFromDawn`. New `CommandFailure` values: `UnknownUnit`, `TileOccupied`,
  `CannotBench` (the innkeeper, ESC-06), `InnkeeperNotPlaced` (the mist cannot begin without the innkeeper on the board)
  and `InvalidValue` (a speed other than 0, 1, 2 or 4). Every refusal changes nothing (tested with the state hash).
- **Alternatives considered:** Starting the next Dusk automatically after Dawn (the player needs Dawn to read the
  summary).
- **Requirement IDs affected:** ARC-10, ARC-11, CYC-04, ESC-06.

## D-048 Placeholder HUD and input for step 2
- **Status:** Accepted (approved by the human after step 2)
- **Date / step:** 2026-10-20, step 2
- **Context:** Step 2 needs enough UI to play a cycle; the UI pass is step 9.
- **Decision:** A HUD built in code (`Hud.cs`): cycle, phase, time to dawn, speed, the forecast, staff slots, the seed,
  messages for refused commands, the bench (drag a name onto the floor), Begin the Mist, speed buttons, Continue, a
  selection line and a pause menu. Dragging a placed unit moves it; dropping it on the bench, or right-clicking it,
  benches it (Sell and Rotate come with the shop and fixtures). In the mist, clicking selects the nearest creature or
  unit, and F follows it (CAM-12); the full panel comes in step 9. Esc opens the pause menu, which stops ticking
  (CYC-10); its Quit closes the game until the title screen exists, and Settings comes in step 8. Texts go through
  `Tr()` with English keys.
- **Alternatives considered:** Designed `.tscn` screens now (would be redone in step 9).
- **Requirement IDs affected:** CAM-12, CAM-13, CYC-10, UI-09.

## D-049 Orthographic camera at 50° with a 45° start
- **Status:** Accepted (approved by the human after step 2)
- **Date / step:** 2026-10-20, step 2
- **Context:** CAM-10 allows orthographic or narrow-FOV perspective, about 45° down.
- **Decision:** Orthographic, pitched 50° down, starting at a 45° yaw for the classic isometric look; zoom changes the
  orthographic size (8 to 34 m). Edge panning is on by default (SET-03 will make it a setting). The roof fade (CAM-11)
  waits for walls tall enough to hide anything (step 10).
- **Alternatives considered:** A narrow-FOV perspective camera.
- **Requirement IDs affected:** CAM-10, CAM-11.

## D-050 Every room must stay reachable on its own
- **Status:** Accepted
- **Date / step:** 2026-10-20, step 2 report (SDD v1.3)
- **Context:** BRD-03 required a path from the front door to every zone. The Rooms zone holds several rooms, so
  blocking one room's doorway passed the check as long as another room was still reachable (D-043).
- **Decision:** The human chose the stricter rule: every room must stay reachable on its own, and a placement that cuts
  off any single room is refused (`TileBlocked`). The step 2 check (`InnBoard.WouldBlockPath`) is zone-based; step 5,
  which gives rooms their identity, extends it to each room.
- **Alternatives considered:** The Rooms zone as a whole being enough.
- **Requirement IDs affected:** BRD-03.

## D-051 Masteries and one more technique per class
- **Status:** Accepted (the human chose both, and kept the four technique names after step 3)
- **Date / step:** 2026-10-20, step 3 (SDD v1.4)
- **Context:** STF-06 gave a ★5 character five techniques, but two classes held at most five techniques between them,
  and only four when neither was Companion/Server, so a Gambler/Fighter could never fill rank 5.
- **Decision:** An upgrade pick is a new technique or the mastery of a known one (STF-17: stronger by the technique's
  mastery values, once per technique). Each class also gets one more technique: **Comfort** (Companion/Server, answers
  `Quiet`), **Bones** (Gambler, `Gambling` without a table), **Grapple** (Fighter, holds a turned creature; `Brawl`) and
  **Tales** (Entertainer, `Spectacle` for a small audience). The validator checks that every path through every tree
  always offers an available option.
- **Alternatives considered:** Masteries only; more techniques only.
- **Requirement IDs affected:** STF-02, STF-06, STF-07, STF-17, DAT-04.

## D-052 Six more effect kinds
- **Status:** Accepted
- **Date / step:** 2026-10-20, step 3 (SDD v1.4)
- **Context:** SHP-05 used `RerollCost`, and the fixtures and items of sections 12.2 and 13.2 needed effects DAT-03 did
  not list (more rooms, a fixture giving an appetite, a technique needing a fixture, light and noise, one fixture
  counting as another).
- **Decision:** DAT-03 adds `RerollCost`, `RoomSlots`, `Provides`, `Enables`, `Emits` and `CountsAs`. The validator
  checks each kind's required parameters.
- **Alternatives considered:** Rewording the fixtures and items to fit the old list.
- **Requirement IDs affected:** DAT-03, SHP-05, FIX-02, ITM-06.

## D-053 Generic placeholder content
- **Status:** Accepted
- **Date / step:** 2026-10-20, step 3
- **Context:** The human chose generic placeholders over agent-written names and personalities.
- **Decision:** Characters are "Staff 01" to "Staff 16" (`staff-01`...) and creatures "Creature 01" to "Creature 10",
  with plausible stats, appetites and triggers that follow each plane's character (PLN-02). Plane, fixture and item
  names are the SDD's own. Everything in `data/` is tuning and may be rewritten freely; IDs should stay once saves
  exist (DAT-05). Two appetites, `Order` and `Darkness`, have no provider yet (no technique or fixture answers them);
  step 4 should give them one or the creatures that want them will only ever be soothed by their other appetites.
- **Alternatives considered:** Agent-written roster; a roster supplied by the human first.
- **Requirement IDs affected:** STF-14, CRE-03, DAT-01.

## D-054 The data is built into Core; an export falls back to it as a whole
- **Status:** Accepted (approved by the human after step 3)
- **Date / step:** 2026-10-20, step 3
- **Context:** DAT-04 says an invalid file in an export build falls back to "the version built into the game", and the
  tests need the real data without touching the file system (TST-01).
- **Decision:** `MistyInn.Core.csproj` embeds `data/*.json` as resources (`MistyInn.Data.<file>`), read by
  `EmbeddedContentSource`; it is always the same as the committed files at build time. The game reads `res://data/`
  through `GodotContentSource`. A debug build that finds invalid content shows the errors and stops; an export build
  logs them and uses the built-in set. The fallback is the whole set, not one file, because the rules cross files (a
  plane names creatures, a tree names techniques).
- **Alternatives considered:** A per-file fallback (could mix files that no longer agree); no fallback.
- **Requirement IDs affected:** DAT-02, DAT-04, TST-01.

## D-055 Content file conventions
- **Status:** Accepted (approved by the human after step 3)
- **Date / step:** 2026-10-20, step 3
- **Context:** DAT-01 to DAT-05 fix the files and IDs, but not the JSON details.
- **Decision:** camelCase property names; enums written as their names (numbers are rejected); unknown properties are
  rejected, so a typo is an error rather than a silently ignored value; no comments, because Godot also reads `.json`
  files; one generic effect record with optional parameters, checked per kind. The loader reads every file and reports
  every broken one at once; the validator reports every rule broken at once.
- **Alternatives considered:** Comments in the data (System.Text.Json allows them, but Godot's JSON parser does not).
- **Requirement IDs affected:** DAT-01 to DAT-05.

## D-056 The innkeeper is separate from the roster; starting staff in balance.json
- **Status:** Accepted (approved by the human after step 3)
- **Date / step:** 2026-10-20, step 3
- **Context:** STF-14's roster is 16 characters, 4 per class; the innkeeper has no class or tree (ESC-06); RUN-01 starts
  with two staff.
- **Decision:** `staff.json` has an `innkeeper` entry and a `characters` list. `balance.json` names the starting staff
  (`staff-01`, a Companion/Server, and `staff-09`, a Fighter); MET-03 will let unlocks change them.
- **Alternatives considered:** The innkeeper as a seventeenth character with a flag.
- **Requirement IDs affected:** RUN-01, STF-14, ESC-06.

## D-057 Upgrade tree layout for the placeholder roster
- **Status:** Accepted (approved by the human after step 3)
- **Date / step:** 2026-10-20, step 3
- **Context:** STF-07 and STF-17 allow many tree shapes; the placeholder roster needs one that always works.
- **Decision:** Every character has a second class. Each rank from 2 to 5 has three options: at least one new technique
  (each new technique is offered only once, so it is always available), the spare new techniques at the earliest ranks,
  and masteries for the rest, starting with the base technique. Preset builds take the first available option at each
  rank. Option IDs are local to the character (`r2-food`, `r3-master-drink`). The data was generated once by a script
  that checks every path; the validator checks the same rules on load.
- **Alternatives considered:** Hand-made trees per character (better once the characters are real).
- **Requirement IDs affected:** STF-07, STF-10, STF-17.

## D-058 Techniques say whether they need a fixture
- **Status:** Accepted (approved by the human after step 3)
- **Date / step:** 2026-10-20, step 3
- **Context:** Dice works "at a card table or any table" (STF-02), so one required fixture per technique was not enough.
- **Decision:** A technique has `needsFixture`; the fixtures it works at are those with an `Enables` effect naming it
  (the bar counter enables Drink, tables enable Food and Dice, card tables Cards and Dice, the stage boosts Song and
  Dance). The validator checks that every technique that needs a fixture has one.
- **Alternatives considered:** A list of fixtures on each technique (the same fact in two places).
- **Requirement IDs affected:** STF-02, FIX-02, DAT-03.

## D-059 No scene markers: the board view is built from inn.json
- **Status:** Accepted (approved by the human after step 3)
- **Date / step:** 2026-10-20, step 3
- **Context:** SIM-04 and TST-07 ask for a test that the inn scene's markers agree with `inn.json`.
- **Decision:** The inn scene has no hand-placed markers: `InnView` builds every tile and gate from the loaded layout, so
  the two cannot disagree. The test checks instead that the board comes from `inn.json` and that every plane has a gate.
  If hand-built inn art arrives in step 10, markers and their test come with it.
- **Alternatives considered:** Hand-placing markers now, only to test them.
- **Requirement IDs affected:** SIM-04, TST-07.

## D-060 Step 4 adds providers for Order and Darkness
- **Status:** Accepted
- **Date / step:** 2026-10-20, step 3 report (SDD v1.5)
- **Context:** No technique or fixture answers the `Order` and `Darkness` appetites (D-053), so the creatures that want
  them could only be soothed by their other appetites.
- **Decision:** Step 4 adds providers for both. Which ones (a technique, a fixture, or both) is a design choice: the
  step 4 agent proposes them at the start of the step and waits for the human's answer before writing them (AGT-03).
- **Alternatives considered:** Removing the two appetites from the closed set.
- **Requirement IDs affected:** AGI-05, AGI-06, step 4.

## D-061 Order and Darkness: two fixtures, and Cards and Tales
- **Status:** Accepted
- **Date / step:** 2026-10-20, step 4 (SDD v1.6)
- **Context:** Nothing answered `Order` or `Darkness` (D-060).
- **Decision:** Both: a **Shadowed alcove** (gives `Darkness` to 2 seats; `Light` within 3 tiles cancels it) and a
  **Ledger desk** (gives `Order` to 2 seats; `Noise` within 3 tiles cancels it), and the Cards and Tales techniques also
  answer `Order` and `Darkness`.
- **Alternatives considered:** Fixtures only; techniques only.
- **Requirement IDs affected:** FIX-02, STF-02, AGI-05.

## D-062 Cold is the draught at the front door
- **Status:** Accepted
- **Date / step:** 2026-10-20, step 4 (SDD v1.6)
- **Context:** Nothing gave off `Cold`, so Creature 03's trigger could never fire.
- **Decision:** While the mist is in, the tiles within 3 of the front door count as `Cold` (`balance.json`,
  `agitation.coldRadius`).
- **Alternatives considered:** A cold-giving fixture; dropping the trigger.
- **Requirement IDs affected:** AGI-08.

## D-063 The starting kit is in storage
- **Status:** Accepted
- **Date / step:** 2026-10-20, step 4 (SDD v1.6)
- **Context:** RUN-01's "base fixtures" were not defined.
- **Decision:** A run starts with a bar counter, two tables, a hearth and a lantern in storage, and 6 Drink and 4 Food;
  the player places them at the first Dusk. The list is in `balance.json`.
- **Alternatives considered:** Pre-placed in a default layout; nothing.
- **Requirement IDs affected:** RUN-01, FIX-05, FIX-06.

## D-064 Service, seeking, fixtures and bestiary rules
- **Status:** Accepted (the human approved the proposed defaults)
- **Date / step:** 2026-10-20, step 4 (SDD v1.6)
- **Context:** The SDD gives the outline of service and agitation but not how they play out.
- **Decision:**
  - **Service (STF-04):** staff serve from where they are placed. A technique that needs a fixture serves creatures
    seated at a working fixture that enables it, within the technique's range of the staff member (+0.5 tile).
    Beside the stage, Song and Dance reach the stage's zone and the zones next to it; otherwise a technique reaches its
    range. Service comes in sessions of `serviceSessionSeconds` (6); a session serves up to the technique's capacity
    and uses one supply per creature when it starts; then the staff member chooses again. Only waiting or idle
    creatures are served, and each by one staff member at a time.
  - **Soothe (AGI-06):** staff soothe × the rank multiplier × technique soothe × appetite strength (craves 1, likes 0.5)
    × 1.25 for the preferred technique × `sootheScale` (2), in agitation points per second. A fixture that gives an
    appetite soothes its seated creatures at `fixtureSoothe` × strength × `sootheScale`. The strongest offer wins.
  - **Seeking (CRE-04):** a creature walks to the best reachable spot: strongest appetite (with the preferred bonus),
    then nearest, then map order. Spots are free seats of fixtures that give the appetite or enable a technique a
    placed staff member can give there, and free tiles within range of staff with a technique that needs no fixture.
    Spots are reserved. A creature that waits unserved for `reseekAfterSeconds` (8) looks again; with nothing to seek
    it wanders. Restless and turned creatures wander the whole inn.
  - **Agitation (AGI-02, AGI-03):** unserved, agitation rises at the base rate × 1.5 when Restless × the rate
    multipliers of holding triggers × ward circles in range × 1.2 when a Menacing creature is within 3 tiles. A trigger
    spikes once when its condition starts. Restless creatures damage the first fixture next to them at 0.5 health per
    second; Menacing ones break it every 8 s. Integrity and repairs stay in step 5.
  - **Fixtures (FIX-01):** a fixture has a shape (tile offsets rotated in 90° steps), every fixture blocks movement, and
    its seats are the free floor tiles next to it, in map order, up to its seat count.
  - **Bestiary (BST-02):** Seen on arrival; one study point per creature type per cycle in which a served one stayed
    calm until dawn, Studied at 3 (`bestiary.studiedPoints`); Understood once every trigger has been seen firing,
    banishings (step 5) counting for missing ones.
- **Alternatives considered:** Continuous service without sessions; creatures choosing at random.
- **Requirement IDs affected:** STF-04, CRE-04, AGI-02, AGI-03, AGI-06, AGI-07, FIX-01, BST-02.

## D-065 Staff know only their base technique until step 6
- **Status:** Accepted
- **Date / step:** 2026-10-20, step 4
- **Context:** Ranks and upgrades arrive in step 6, but service needs staff techniques now.
- **Decision:** A unit knows its character's ★1 base technique (the innkeeper knows none) and uses the ★1 stat
  multiplier. Step 6 adds ranks, preset builds and upgrades on top of the same `Techniques` list.
- **Requirement IDs affected:** STF-06, STF-10.

## D-066 The bestiary lives outside the session
- **Status:** Accepted
- **Date / step:** 2026-10-20, step 4
- **Context:** BST-04 keeps the bestiary across runs; a run is one session (D-045); saving is step 8.
- **Decision:** `CreatureBestiary` is passed into each `GameSession`; GameRoot keeps one in memory for the whole game
  session. Step 8 saves it in the profile (SAV-02).
- **Requirement IDs affected:** BST-04, SAV-02.

## D-067 What the cycle log records
- **Status:** Accepted
- **Date / step:** 2026-10-20, step 4
- **Context:** INV-12 wants every agitation change explained; agitation changes every step.
- **Decision:** The log records causes and turning points, not every step: arrivals, service starting (who, with
  what), triggers firing, being frightened, contagion (with its source), state changes, turning, broken fixtures,
  tributes, income and bestiary reveals. The rise and fall between them follows from those causes and the rates in
  `balance.json`.
- **Alternatives considered:** A log entry per step per creature (would fill the 20,000-entry log in minutes).
- **Requirement IDs affected:** INV-12, UI-07.

## D-068 Internal test hooks on GameSession
- **Status:** Accepted
- **Date / step:** 2026-10-20, step 4
- **Context:** Agitation and service tests need a known creature on a known tile, not a random arrival.
- **Decision:** `GameSession` has internal members, visible only to the test project: spawn a creature on a tile,
  reach a live creature or fixture, and read the service rates and rules. Nothing in the game calls them.
- **Requirement IDs affected:** TST-01, ARC-03.

## D-069 Each back room is its own zone with a RoomDoor tile
- **Status:** Accepted (the human's answer)
- **Date / step:** 2026-10-20, step 5
- **Context:** Both rooms shared one `rooms` zone and their doors were plain floor, so BRD-03 could not check each room
  on its own, and CMP-05 had no door to attack.
- **Decision:** In `inn.json` each room is its own zone (`room-1`, `room-2`), and its door is a new `RoomDoor` tile kind
  in that zone: walkable, never placeable, closed (blocked) during a session. A room door has health
  (`company.doorHealth`, 40); a broken door closes the room until Dawn. The layout parser rejects a room door with no
  zone, a room with two doors, and a room with no floor. Because each room is a zone, the existing "every zone
  reachable" check now covers every room on its own (BRD-03). `balance.json` `rooms` (2) says how many are used, and
  the validator checks the map has that many.
- **Alternatives considered:** A separate `rooms` list in `inn.json`.
- **Requirement IDs affected:** BRD-01, BRD-03, CMP-04, CMP-05.

## D-070 Integrity takes a share of fixture damage, and walls
- **Status:** Accepted (the human's answer); the tuning values are proposed
- **Date / step:** 2026-10-20, step 5
- **Context:** BRD-06 says damage to fixtures and walls lowers integrity, but not how much.
- **Decision:** Integrity loses 25 % (`combat.integrityShare`) of the damage fixtures and room doors take. A Wreck-stage
  creature with no working fixture or occupied room door in reach hits the nearest inner wall, which takes integrity
  directly (`combat.wallDamageMultiplier`, 1.0). Restless knocks and Menacing breaks count too. The other combat values
  are in the new `combat` section of `balance.json`: guard radius 4, violence within 4 tiles for 2 s, blood within 4
  tiles, wound chance 0.7, Shaken soothe 0.8, Grapple ×1.5 attack interval, repath every 0.5 s, 5 integrity per
  silver, and at most 64 recent attacks and blood marks kept.
- **Alternatives considered:** 50 % of fixture damage and no walls; a fixed 10 per broken fixture.
- **Requirement IDs affected:** BRD-06, ESC-01, AGI-08, ESC-09, STF-12.

## D-071 Company room rules and tuning
- **Status:** Accepted (approved after step 5)
- **Date / step:** 2026-10-20, step 5
- **Context:** CMP-04 and CMP-05 leave some details open.
- **Decision:**
  - "Still craves Company" means a `Craves`-strength Company appetite. A guest has at most one room session a cycle.
  - The pair walk to the first free, unbroken room in map order. The guest is soothed at the floor rate on the way;
    violence within 4 tiles, the companion falling or the guest turning calls the walk off.
  - Inside, both are off the board: no triggers, fright, contagion or attacks reach them. The guest is soothed at
    3 × the floor rate (`company.roomSootheMultiplier`), and comes out with agitation at most 49 (Calm).
  - A session ended by an attack on the door does not calm the guest. Either way the companion then rests 10 s,
    out of play, and walks back to its post.
  - Preferred guests ×1.5 (`company.preferredSoothe`) on the floor and in the room. Staff 01, 02 and 03 have
    placeholder preferences and refusals in `staff.json` (new optional `prefers` and `refuses` lists of creature IDs).
- **Requirement IDs affected:** CMP-01, CMP-02, CMP-04, CMP-05.

## D-072 Fixtures repair for free at Dawn; integrity costs silver
- **Status:** Accepted (the human's answer)
- **Date / step:** 2026-10-20, step 5
- **Context:** BRD-07 lets the player pay to repair fixtures and integrity; the price was not set.
- **Decision:** At Dawn every damaged or broken fixture, and every room door, is repaired for free and at once (a
  `FixtureRepaired` event and log entry each). Integrity costs 1 silver per 5 points (`combat.integrityPerSilver`),
  bought with `RepairIntegrity(silver)`; only what is missing is charged, and partial repairs are allowed. Unrepaired
  integrity carries over.
- **Alternatives considered:** 1 silver per 10 missing fixture health; half the build cost per fixture.
- **Requirement IDs affected:** BRD-07, ESC-10, BRD-04.

## D-073 Combat details
- **Status:** Accepted (approved after step 5)
- **Date / step:** 2026-10-20, step 5
- **Context:** Section 10 leaves some rules to the implementation.
- **Decision:**
  - **Targets (ESC-01):** Wreck goes for the nearest working fixture or occupied room door it can reach (then walls,
    D-070); Prey for the nearest creature that has not turned, or staff member on the floor (not the innkeeper, who is
    the Hunt's target); Hunt for the innkeeper. With no path to a Prey or Hunt target it attacks the nearest barricade.
    Targets are chosen again every 0.5 s.
  - **Turning on a fighter (ESC-03):** "out of reach" is beyond the guard radius.
  - **Fighters (ESC-07):** a fighter uses its first Fighter technique; reach is the larger of its range stat and the
    technique's range; a hit does `max(1, attack × rank multiplier + technique attack bonus − armour)` every attack
    interval × the technique's interval multiplier. It walks to the creature, and back to its post when there is
    nothing to fight. Grapple also slows the held creature's attacks ×1.5.
  - **Innkeeper (ESC-06):** stays at its post and strikes the nearest turned creature in reach. Its blows banish too.
  - **Falling (ESC-09):** a fallen unit stays on the floor until dawn (RAT-05) and leaves a Blood mark, as does a
    creature struck down (ESC-04). The Dawn roll is in unit order from the run's random source.
  - **Violence (AGI-08, CMP-05):** any landed attack counts for 2 s within 4 tiles of where it landed.
- **Requirement IDs affected:** ESC-01, ESC-03, ESC-04, ESC-05, ESC-06, ESC-07, ESC-09, STF-02, AGI-08.

## D-074 A debug dev panel for staff and planes
- **Status:** Accepted (the human's answer)
- **Date / step:** 2026-10-20, step 5
- **Context:** The step 5 demo needs two fighters, a companion and Pale Court guests, but the run starts with Staff 01
  and Staff 09, the shop comes in step 6, and the Pale Court is locked.
- **Decision:** Two development commands in Core, sent only from a dev panel shown in debug builds at Dusk:
  `DevAddStaff(characterId)` puts any character on the bench at ★1 (refused if owned, unknown, or the bench is full),
  and `DevSetConvergingPlanes(planeIds)` sets this Dusk's planes and unlocks them for the run (`RunState.UnlockedPlanes`
  now holds the run's planes). The panel also has the two content filter toggles until the settings screen (step 8).
- **Alternatives considered:** Changing the starting staff and unlocked planes in `balance.json`.
- **Requirement IDs affected:** ARC-11, PLN-03.

## D-075 Content filters in Core change only what is shown
- **Status:** Accepted (approved after step 5)
- **Date / step:** 2026-10-20, step 5
- **Context:** RAT-09 wants both filters honoured everywhere, including the cycle log; CMP-06 wants the same mechanics.
- **Decision:** `GameSession.Filters` holds `ContentFilters(ReducedBlood, NoSuggestive)`, set with `SetContentFilters` in
  any phase. They are not part of the state hash and no rule reads them. Log entries keep neutral keys
  (`company.room`) and a flavour context; `FlavourText(entry)` picks the line when it is read, with the filters of that
  moment, from the entry's sequence number (never the run's random source), so turning a filter on also changes what
  earlier entries show. `TechniqueName("company")` is "Hosting" with the suggestive filter on. Views flash hits white
  and hide blood marks with the Blood filter on. Settings saving the filters is step 8 (SET-01).
- **Requirement IDs affected:** RAT-09, SET-04, CMP-06, DAT-06, INV-02.

## D-076 Older tests run on sturdy content
- **Status:** Accepted (approved after step 5)
- **Date / step:** 2026-10-20, step 5
- **Context:** With combat, a creature that turns in an older cycle or service test can hunt the innkeeper or wear
  integrity to 0 and end the run, which those tests never meant to test.
- **Decision:** `TestGame.NewSession` and `Scenario` use `TestGame.Sturdy` by default: the real content with an
  innkeeper of 1,000,000 health and 1,000,000 integrity. Combat and run-loss tests ask for the real values
  (`sturdy: false`). No value in `data/` changed for this (AGT-14). One step 4 tribute test now turns its creature in
  the last 3 s, so it cannot reach anyone.
- **Requirement IDs affected:** TST-01, INV-08, AGT-14.
