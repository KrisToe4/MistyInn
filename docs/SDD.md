# Software Design Document (SDD)
## Misty Inn

| | |
|---|---|
| **Version** | 1.5 (approved) |
| **Supersedes** | 1.4 |
| **Audience** | LLM-based coding agents and the human developer |
| **Genre** | Dark roguelike autobattler: prepare the inn, then survive the mist |
| **Engine** | Godot 4.7.2 (.NET build) |
| **Language** | C# (`net8.0`, `Godot.NET.Sdk/4.7.2`) |
| **Platform** | Windows desktop (mouse and keyboard) |
| **Scope** | Hobby prototype |
| **Content ceiling** | Mature: moderate blood, suggestive themes; see section 17 |
| **Tests** | NUnit, on the engine-free Core library |

---

## Contents

- [0. Document control](#0-document-control)
- [1. Vision, scope and non-goals](#1-vision-scope-and-non-goals)
- [2. Invariants](#2-invariants)
- [3. Repository structure and build hygiene](#3-repository-structure-and-build-hygiene)
- [4. Technology defaults](#4-technology-defaults)
- [5. Architecture](#5-architecture)
- [6. Core loop: runs and cycles](#6-core-loop-runs-and-cycles)
- [7. Planes and the mist](#7-planes-and-the-mist)
- [8. Creatures](#8-creatures)
- [9. Agitation](#9-agitation)
- [10. Escalation and combat](#10-escalation-and-combat)
- [11. Staff](#11-staff)
- [12. The inn board: layout and fixtures](#12-the-inn-board-layout-and-fixtures)
- [13. Shop, items and synergies](#13-shop-items-and-synergies)
- [14. Roguelike systems: omens, bestiary, meta](#14-roguelike-systems-omens-bestiary-meta)
- [15. Camera, input and user interface](#15-camera-input-and-user-interface)
- [16. Art direction and audio](#16-art-direction-and-audio)
- [17. Content rating and tone](#17-content-rating-and-tone)
- [18. Content data](#18-content-data)
- [19. Save, load and settings](#19-save-load-and-settings)
- [20. Testing](#20-testing)
- [21. Implementation steps](#21-implementation-steps)
- [22. Mandatory agent rules](#22-mandatory-agent-rules)
- [23. Definition of done and known limitations](#23-definition-of-done-and-known-limitations)
- [24. Backlog and extension seams](#24-backlog-and-extension-seams)
- [Appendix A: Step report template](#appendix-a-step-report-template)
- [Appendix B: PROGRESS and DECISIONS templates](#appendix-b-progress-and-decisions-templates)
- [Appendix C: Traceability matrix](#appendix-c-traceability-matrix)
- [Appendix D: Requirement index](#appendix-d-requirement-index)
- [Appendix E: Glossary](#appendix-e-glossary)

---

# 0. Document control

## 0.1 How this document is used

This SDD is applied **one step at a time** (section 21). After each step the human builds the code, runs the tests and
the game, reads the agent's step report (Appendix A), and decides whether the SDD needs to change **before** the next
step starts.

- Every requirement has an ID (for example `AGI-04`). Prompts, tests, step reports, decisions and change-log entries
  refer to requirements by ID, never by paraphrase. Appendix D lists every ID.
- Tests are tagged with the requirements they verify (`TST-02`). The audit step produces a traceability matrix in
  `docs/TRACEABILITY.md` (Appendix C).
- Every change to the SDD bumps its version. Each prompt states the SDD version it was written against. If an agent
  finds the SDD ambiguous, contradictory or silent on a material decision, it **stops and asks** (`AGT-03`).
- Wording: **shall** = mandatory; **should** = a default that may be changed with a recorded decision; **may** =
  optional.
- Balance numbers (section 6.4 and the data files) are **tuning values**, not requirements. They may change without an
  SDD change, but each change is recorded in `docs/DECISIONS.md`.
- Removed requirement IDs are never reused. Version 0.2 replaced the v0.1 game design; the v0.1 IDs that no longer
  exist are listed in the change log.

## 0.2 Change log

| Version | Change | Reason |
|---|---|---|
| 1.5 | SIM-04 and TST-07: the board view is built from `inn.json`, so there are no scene markers to compare (until hand-built inn art arrives). STF-02: a technique says whether it needs a fixture, and fixtures' `Enables` effects say which. DAT-04: an export build falls back to the whole built-in set. Step 4: add providers for the `Order` and `Darkness` appetites | Step 3 report (D-054, D-058, D-059, D-060) |
| 1.4 | STF-02: one more technique per class (Comfort, Bones, Grapple, Tales). STF-06, STF-07 and new STF-17: an upgrade adds a technique or masters a known one; every tree always offers an available option. DAT-03: effect kinds `RerollCost`, `RoomSlots`, `Provides`, `Enables`, `Emits` and `CountsAs` | Step 3: two classes could not fill five ranks, and sections 12.2, 13.2 and SHP-05 used effects DAT-03 did not list (D-051, D-052) |
| 1.3 | SIM-02: `GameSession.Tick` takes real frame time and runs the due fixed steps itself (exact integer accounting, at most 120 steps per call, `InterpolationAlpha` for views); GameRoot passes frame time. ARC-10: `ContinueFromDawn` added to the commands. BRD-03: every room must stay reachable on its own, not just the Rooms zone; checked from step 5 | Step 2 report (D-042, D-047, D-050) |
| 1.2 | TST-12: the headless smoke run starts at step 2, the first step with a main scene; before that, opening and building the project in the Godot editor stands in for it | Step 1 report |
| 1.1 | STR-01: the solution stays a classic `.sln` (Godot expects it; never `dotnet new sln`, which makes `.slnx` on .NET 10), and Core and the tests map the solution's ExportDebug and ExportRelease configurations to their Debug and Release. TST-12: the Godot path on this machine is recorded in `docs/PROGRESS.md`. Step 1: the human creates the Windows export preset in the editor (`export_presets.cfg`, committed), with the agent giving the steps | Step 0 report (D-035, D-036) |
| 1.0 | Approved. Final review changes: 3 staff offers per Dusk (`SHP-02`); a home-plane draw weight (`SHP-04`); the 16-character roster, 4 per main class, weighted towards the Mortal Realm (`STF-14`); hidden ancestry, revealed by observation and kept across runs (`STF-15`, `STF-16`, `SYN-01`, `MET-01`, `SAV-02`, `UI-06`); the Company section renumbered 11.5. Steps 0 to 2 updated because the pre-SDD prototype code was removed. Every Proposed decision is Accepted | The human approved the SDD as the initial version (D-032 to D-034) |
| 0.2 | Review point 2: staff are unique named characters with **classes** (Companion/Server, Gambler, Fighter, Entertainer) and **techniques**; five **ranks**, with an upgrade tree of at most two classes and one technique chosen per upgrade; upgrades come from buying the same character again in a random **shop** of ranked staff and items (in-run silver only, never real money); **staff items** and **inn items**, which replace relics; a fixed **build menu** for fixtures and supplies; Company on the floor or in a **room**; new appetites `Spectacle` and `Brawl`. Removed: `REL-*`, the Guard, Warden and Fixer roles, three-copy merging, the Dawn relic choice, lounge booths | Review point 2: staff classes and upgrades (D-025 to D-031) |
| 0.2 | The game becomes a dark roguelike autobattler. New sections 6 to 17: cycles of Dusk (setup), Mist (automatic) and Dawn; hostile creatures with agitation; escalation (wreck, prey, hunt); staff units with a shop, merging and synergies; the inn board with fixtures and adjacency; omens, relics and the bestiary; an isometric camera; dark stylised art; moderate blood. Content filters replace Cozy mode. Removed v0.1 IDs: `DAY-*`, `GST-*`, `SVC-*`, `ECO-*`, `UPG-*`, `REP-*`, `EVT-*`, `BON-*`, `PLR-*`, `CAM-01..03`, the v0.1 `UI-*` and `ART-*` and `AUD-*` (replaced by renumbered IDs), `SCP-11`. Kept with changes: `INV-*`, `STR-*`, `ARC-*`, `SIM-*`, `RUN-*`, `PLN-*`, `CMP-*`, `MET-*`, `RAT-*`, `DAT-*`, `SAV-*`, `SET-*`, `TST-*`, `AGT-*` | Review point 1: a darker, more mature tone; the planes are hostile and each cycle is about surviving by keeping the guests occupied; an autobattler (D-017 to D-024) |
| 0.1 | First draft: vision, invariants, architecture, game systems, content rating, data, saves, tests, steps 0 to 11, agent rules, indexes | The human asked for a full design document before development continues (D-001 to D-016) |

## 0.3 Document index

| File | Purpose | Owner |
|---|---|---|
| `docs/SDD.md` | This document: the design and the rules | Human (agents propose changes, `AGT-08`) |
| `docs/SDD_v<version>.md` | Earlier SDD versions, kept on every change | Human |
| `docs/DECISIONS.md` | Recorded decisions, `D-001` onward, with an index table | Agents add entries; the human approves |
| `docs/PROGRESS.md` | Step status, build and test results, package versions | Agents update every step |
| `docs/TRACEABILITY.md` | Requirement-to-test matrix (Appendix C) | Created in the audit step |
| `docs/ASSETS.md` | Every third-party asset with its source and licence (`INV-10`) | Agents update when they add an asset |
| `CLAUDE.md` | Short orientation and working rules for coding agents | Human |
| `README.md` | Concept, how to run, controls | Agents keep it current |
| `tools/gen-requirement-index.py` | Regenerates Appendix D from the requirement IDs | Run by the human |

---

# 1. Vision, scope and non-goals

## 1.1 Pitch

Misty Inn stands at a **nexus where planes of reality converge**. When the mist rolls in, the planes open, and what
walks out of them is not looking for a bed. Drowned things, ash-born brutes, hungry pale courtiers and rusted
machines step into the common room, and each one is a fuse.

You are the innkeeper. You cannot fight them. You can only **prepare**: hire the right staff from whoever drifts in
(companions and servers, gamblers, fighters, entertainers), upgrade them as they come back to you, set the tables,
light or douse the lanterns, place the wards and barricades, and decide who works where.
Then the mist comes, and you **watch**. If the guests stay occupied until dawn, they fade back into their planes and
leave a tribute. If they don't, they wreck the inn, turn on each other and your staff, and finally come for you.

Each run, you learn a little more about what the mist brings.

## 1.2 Design pillars

1. **Dread under the mist.** A dark, moody inn: warm candlelight inside, a cold, hostile mist outside.
2. **Prepare, then watch.** Every decision is made during setup. The mist phase plays out on its own, and you can
   always see why things went the way they did.
3. **Knowledge is survival.** Creatures do not explain themselves. Their wants and triggers are learned by watching,
   and the bestiary keeps that knowledge across runs.
4. **Every cycle is different.** Planes, creatures, omens, shop offers and the way each character grows change from
   cycle to cycle and run to run.
5. **Short sessions.** A cycle takes about 5 minutes (3 for setup, 2 for the mist) and a run about 50.

## 1.3 Required capabilities

- **SCP-01** An inn board viewed from an isometric camera, on which the player places staff and fixtures during setup.
- **SCP-02** A cycle of Dusk (setup), Mist (automatic simulation) and Dawn (aftermath) phases (section 6).
- **SCP-03** A roguelike run: random planes, creatures, omens and shop offers; it ends in a win or a loss; a seed can
  reproduce it.
- **SCP-04** Hostile creatures from several planes, each with hidden appetites, triggers and an agitation level
  (sections 8 and 9).
- **SCP-05** An automatic escalation and combat simulation when a creature turns (section 10).
- **SCP-06** Unique staff characters with classes and techniques, five ranks and an upgrade tree, hired and upgraded
  through a random shop, with class and origin synergies (sections 11 and 13).
- **SCP-07** Fixtures on the inn's grid, bought from a fixed build menu, whose effects depend on their position and
  neighbours (section 12).
- **SCP-08** A bestiary that reveals creature knowledge as it is observed and keeps it across runs (section 14.2).
- **SCP-09** Dark stylised 3D presentation, with the mist as a menace (section 16).
- **SCP-10** Saving and loading a run between cycles, and a profile that keeps meta-progression and the bestiary.
- **SCP-11** *(Removed in v0.2: Cozy mode. Content filters are in `SET-04`.)*
- **SCP-12** Content kept as data files that can be edited without code changes.
- **SCP-13** Pause and speed controls (pause, 1×, 2×, 4×) during the mist phase; no other player input then.
- **SCP-14** Staff items carried by staff, and inn items that change rules across the whole run and are shown in the
  inn (section 13.2).

## 1.4 Prototype decisions

- Single player, offline, Windows only.
- Placeholder primitives are acceptable until the art step (step 10).
- Saves happen only between cycles (at Dawn). Quitting during a cycle loses that cycle.
- The UI is in English only, but its text goes through Godot's translation functions so it can be localised later
  (`UI-09`).
- The inn has one floor in the prototype; the rooms are back rooms on the ground floor. A second floor is in the
  backlog.

## 1.5 Non-goals

- **SCP-20** Multiplayer, online features, leaderboards or accounts. (No asynchronous PvP, even though many
  autobattlers have it.)
- **SCP-21** Consoles, mobile or controller-first design.
- **SCP-22** Explicit sexual content in any form (section 17).
- **SCP-23** Procedurally generated inn layouts. The inn's shape is hand-built; only what the player places on it and
  the content of each cycle change.
- **SCP-24** Voice acting, cutscenes and branching story campaigns.
- **SCP-25** Direct control of any unit, during setup or the mist phase.
- **SCP-26** Gore beyond the ceiling in section 17 (no dismemberment).
- **SCP-27** Real money of any kind: no microtransactions, paid currencies or paid random draws. The shop's random
  draws (section 13) use only silver earned in the current run.

---

# 2. Invariants

These rules apply to every step and every file. Other sections refer to them by ID.

- **INV-01** `MistyInn.Core` shall not reference Godot (`GodotSharp`, `Godot.NET.Sdk`) or any engine type. All game
  rules, including movement, targeting and combat, live in Core; Godot nodes only present Core's state and send it
  commands.
- **INV-02** The simulation is deterministic: the same seed, content and command sequence produce the same state,
  and the same mist phase plays out identically every time. All randomness goes through an injected `IRandomSource`
  seeded per run. `System.Random`, `Guid.NewGuid()`, `DateTime.Now` and Godot's random functions are forbidden in Core.
- **INV-03** Game time advances only through `GameSession.Tick(TimeSpan)`, with a fixed step (`SIM-02`). Core never
  reads the wall clock. Pausing stops the ticks; speed controls change how many fixed steps run per frame, never the
  step size.
- **INV-04** Content (planes, creatures, staff and their upgrade trees, classes, techniques, items, fixtures, omens,
  flavour text, balance) is data in
  `data/*.json` (section 18), not literals in code. Code may only hold fallback defaults that tests use.
- **INV-05** No unbounded growth: creatures and units on the board, queues, logs and histories have fixed caps,
  enforced in Core.
- **INV-06** Every character is an adult. Content never goes past the ceiling in section 17. Sexual or suggestive
  content and violence are never combined. This invariant cannot be changed by a recorded decision, only by the human
  editing section 17.
- **INV-07** A missing, corrupt or older save never crashes the game and is never silently overwritten (section 19).
- **INV-08** The solution compiles and every test passes at the end of each step. No step weakens an earlier step's
  tests.
- **INV-09** Godot node scripts are `partial` classes named after their file. C# event handlers that nodes attach to
  Core or to other nodes are removed in `_ExitTree`.
- **INV-10** Every third-party asset (model, texture, font, sound, music, shader) has a known licence that allows use
  in a game, and is listed in `docs/ASSETS.md` with its source and licence. Unlicensed assets, or assets of unknown
  origin, are never added.
- **INV-11** No secrets, personal data, telemetry or network calls in the game.
- **INV-12** Every outcome of the mist phase is explainable: Core records each agitation change, escalation, attack
  and death with its cause in the cycle log (`UI-07`).

---

# 3. Repository structure and build hygiene

```text
MistyInn/                            (repository root, also the Godot project root)
├── project.godot
├── MistyInn.sln                     (Godot project, Core library and tests)
├── MistyInn.csproj                  (the Godot game: presentation only)
├── Directory.Build.props
├── .editorconfig
├── .gitignore / .gitattributes
├── icon.svg
├── README.md
├── CLAUDE.md
├── export_presets.cfg               (committed; no secrets)
├── data/                            (content JSON, section 18)
├── scenes/
│   ├── Main.tscn                    (root scene: owns the GameSession)
│   ├── inn/                         (Inn.tscn: the board, zones, mist gates, tile markers)
│   ├── units/                       (Staff.tscn, Creature.tscn, Innkeeper.tscn)
│   ├── fixtures/                    (one scene per fixture kind)
│   └── ui/                          (setup UI, mist HUD, Dawn summary, menus)
├── scripts/                         (C# node scripts, mirroring scenes/)
├── assets/
│   ├── models/  textures/  materials/  shaders/  vfx/  audio/  fonts/  ui/
├── src/
│   ├── .gdignore
│   └── MistyInn.Core/               (engine-free game logic, INV-01)
├── tests/
│   ├── .gdignore
│   └── MistyInn.Core.Tests/         (NUnit)
├── tools/                           (gen-requirement-index.py)
└── docs/
```

- **STR-01** The Godot project lives at the repository root. `MistyInn.csproj` uses
  `Godot.NET.Sdk/<editor version>`, and that version shall equal the Godot editor version in use (currently 4.7.2).
  Upgrading Godot is a recorded decision that changes both together. The solution is a classic `MistyInn.sln`, which
  Godot expects; it is never replaced by `.slnx` (the .NET 10 default for `dotnet new sln`, which is not used).
  Projects without Godot's configurations (Core and the tests) map the solution's `ExportDebug` to `Debug` and
  `ExportRelease` to `Release`, written into the `.sln` explicitly.
- **STR-02** `src/MistyInn.Core` is a plain class library (`Microsoft.NET.Sdk`, `net8.0`) with no Godot reference
  (`INV-01`). The game project references it with a `ProjectReference`.
- **STR-03** `tests/MistyInn.Core.Tests` is an NUnit project that references only Core. It does not reference the Godot
  project and does not need the engine to run.
- **STR-04** The Godot SDK compiles every `*.cs` file under the project folder by default, so `MistyInn.csproj` shall
  exclude them: `<Compile Remove="src/**" />` and `<Compile Remove="tests/**" />`. `src/` and `tests/` each contain an
  empty `.gdignore` so the Godot editor does not import them.
- **STR-05** References point one way: Game → Core, Tests → Core. Core references nothing in the solution.
- **STR-06** `Directory.Build.props` sets `Nullable` to `enable`, `LangVersion` to `latest`, and `ImplicitUsings` to
  `enable` for Core and the tests. Core and the tests set `TreatWarningsAsErrors`. The Godot project does not, because
  the Godot source generators can raise warnings outside our control.
- **STR-07** The test project sets `<RollForward>Major</RollForward>` so it runs on whichever .NET runtime (8 or later)
  is installed.
- **STR-08** Committed: `project.godot`, `*.tscn`, `*.tres`, `*.import`, `*.uid`, `export_presets.cfg`, `data/`,
  `assets/`. Ignored: `.godot/`, `bin/`, `obj/`, `.vs/`, `*.user`, `builds/`.
- **STR-09** Package versions are never typed by hand. Agents give `dotnet add <project> package <id>` commands, and
  the version NuGet resolves is recorded in `docs/PROGRESS.md`.
- **STR-10** Folder and namespace mirror each other: `MistyInn.Core.Creatures` is `src/MistyInn.Core/Creatures/`, and a
  node script in `scripts/ui/` uses the namespace `MistyInn.UI`.
- **STR-11** Windows builds export to `builds/windows/`, which is ignored by git.

---

# 4. Technology defaults

| Area | Default |
|---|---|
| Engine | Godot 4.7.2 .NET, Forward+ renderer |
| Language | C# 12 or later, `net8.0`, nullable enabled |
| Simulation | Fixed step of 1/30 s in Core, independent of Godot physics (`SIM-02`) |
| Pathfinding | Grid A* in Core on the inn's tile grid (no Godot navigation, `INV-01`) |
| Serialisation | `System.Text.Json` in Core (source-generated contexts where practical) |
| Tests | NUnit, NUnit3TestAdapter, Microsoft.NET.Test.Sdk (versions from NuGet, `STR-09`) |
| Input | Mouse first; input actions registered in code with defaults (`InputActions`) |
| UI | Godot `Control` nodes with a shared `Theme` resource (`assets/ui/InnTheme.tres`) |
| Text | Godot `Tr()` keys for UI text; content names come from data |
| Audio | Godot audio buses: Master, Music, SFX, Ambience |
| Save location | `user://` (on Windows, `%APPDATA%\Godot\app_userdata\Misty Inn\`) |

---

# 5. Architecture

## 5.1 Layers

```text
┌──────────────── Godot game (MistyInn.csproj) ────────────────┐
│ Scenes and node scripts: board view, unit and creature views,│
│ fixture views, isometric camera, setup UI, mist HUD, VFX.    │
│ GameRoot node: owns GameSession, ticks it, drains its        │
│ events, sends setup commands.                                │
│ Adapters: GodotContentSource, GodotSaveStore, settings.      │
└───────────────────────▲──────────────────┬───────────────────┘
                        │ events (read)    │ commands (setup only)
┌───────────────────────┴──────────────────▼───────────────────┐
│ MistyInn.Core (no engine)                                    │
│ GameSession → RunState → CycleState                          │
│ Board: grid, zones, placement, pathfinding.                  │
│ Systems: Phases, Arrivals, Agitation, Service, Escalation,   │
│ Combat, Staff, Shop, Items, Synergies, Fixtures, Omens,      │
│ Bestiary, Meta. Cycle log.                                   │
│ Content: definitions, loader, validator.                     │
│ Persistence: save DTOs, versioned serialiser, migrations.    │
│ Abstractions: IRandomSource, IContentSource, ISaveStore.     │
└──────────────────────────────────────────────────────────────┘
```

- **ARC-01** `GameSession` is the single entry point into Core. It holds the current `RunState` and exposes
  `Tick(TimeSpan)`, `Execute(GameCommand)`, `DrainEvents()` and read-only state queries.
- **ARC-02** One `GameRoot` node (the script on `Main.tscn`) owns the only `GameSession`. Other nodes get it by
  `[Export]` reference or from `GameRoot`; there are no static singletons that hold game state (Godot autoloads may
  only hold settings, audio and scene switching).
- **ARC-03** Core state is changed only by `Tick` and `Execute`. Nodes never change Core objects directly; queries
  return read-only views.
- **ARC-04** Each Core system is a class with explicit dependencies passed to its constructor and is unit-testable on
  its own. There is no service locator and no hidden global state.
- **ARC-05** A whole mist phase can be simulated in Core without the engine, faster than real time
  (`GameSession.RunMistToDawn()` for tests and tools).

## 5.2 Commands and domain events

- **ARC-10** Commands are immutable records, for example `BuyOffer(offerIndex)`, `ChooseUpgrade(unitId, nodeId)`,
  `Reroll`, `LockShop`, `PlaceUnit(unitId, tile)`, `BenchUnit(unitId)`, `SellUnit(unitId)`,
  `EquipItem(itemId, unitId)`, `UnequipItem(itemId)`, `BuildFixture(fixtureId)`,
  `PlaceFixture(fixtureId, tile, rotation)`, `BuySupplies(kind, amount)`, `Repair(target)`, `BeginMist`,
  `ContinueFromDawn`, `SetSpeed(multiplier)`, `StartRun(seed)`.
- **ARC-11** `Execute` returns a `CommandResult` (success, or a reason code such as `WrongPhase`, `NotEnoughSilver`,
  `TileBlocked`, `BoardFull`). A command that is not valid changes nothing. During the mist phase only `SetSpeed` is
  accepted (`SCP-13`).
- **ARC-12** Core raises domain events (`PhaseChanged`, `CreatureArrived`, `ServiceStarted`, `AgitationChanged`,
  `ThresholdCrossed`, `CreatureTurned`, `AttackHit`, `UnitFell`, `FixtureDamaged`, `FixtureDestroyed`,
  `CreatureBanished`, `TributeLeft`, `BestiaryRevealed`, `OmenStarted`, `UnitUpgraded`, `RoomEntered`,
  `RoomLeft`, `RunEnded`, and so on) into a
  bounded buffer (`INV-05`). `GameRoot` drains it once per frame and passes each event to the views that care about
  it.
- **ARC-13** Every entity has a stable integer ID issued by Core (`UnitId`, `CreatureId`, `FixtureId`). Views map IDs
  to nodes and free a node when its entity leaves.

## 5.3 Time, space and the simulation tick

- **SIM-01** Core measures time as `TimeSpan` game time since the start of the mist phase. Durations are in game
  seconds.
- **SIM-02** Core runs fixed steps of 1/30 s. `GameRoot` passes each frame's real time to `GameSession.Tick`, which
  multiplies it by the speed setting (1×, 2× or 4×; 0 when paused) and runs as many fixed steps as are due. The owed
  time is kept exactly, so the outcome never depends on the frame rate. One `Tick` runs at most 120 steps; a longer
  frame drops the backlog rather than freezing to catch up. `GameSession.InterpolationAlpha` tells views how far the
  next step is (SIM-03). Dusk and Dawn are not timed.
- **SIM-03** Core owns positions. Units and creatures have a position in tile space (a tile plus an offset) and move
  along A* paths at their speed. Views interpolate between Core positions; they never move units themselves.
- **SIM-04** The inn's grid, zones and mist gates are defined in data (`data/inn.json`). The inn view builds its tiles
  and gates from that data at run time, so the two cannot disagree; a test checks that the board comes from `inn.json`
  (`TST-07`). If hand-built inn art adds markers later (step 10), a test that the markers and the data agree comes with
  it.

## 5.4 Scene tree (target)

```text
Main (Node3D, GameRoot.cs)
├── WorldEnvironment            fog, sky, tonemap, glow
├── Moon (DirectionalLight3D)
├── Clearing                    ground, mist gates, mist VFX, the edge of the world
├── Inn (Inn.tscn)              walls, zones, tile grid, placement overlay
├── Fixtures (Node3D)           fixture views
├── Units (Node3D)              staff and innkeeper views
├── Creatures (Node3D)          creature views
├── CameraRig                   isometric camera
├── Audio                       music, ambience players
└── UI (CanvasLayer)            setup UI, mist HUD, cycle log, Dawn summary, menus
```

- **ARC-20** Everything that appears more than once (units, creatures, fixtures) is its own `.tscn`, instanced from a
  `PackedScene` export. Views do not build meshes in code; placeholder primitives live in each view's own scene.
- **ARC-21** Node references use `[Export]` fields set in the editor, or `%UniqueName` lookups inside one scene. Long
  `GetNode("../../..")` paths are not allowed.

---

# 6. Core loop: runs and cycles

## 6.1 Run

- **RUN-01** A run starts from a seed (random unless the player enters one) and the profile's unlocks. A new run
  starts at cycle 1 with the starting silver, the innkeeper, two starting staff at ★1, the base fixtures, and no
  items.
- **RUN-02** A run is **won** by surviving the last cycle (`RunLength`, default 10). The last cycle is the
  **Convergence**: every unlocked plane opens at once and arrivals are faster.
- **RUN-03** A run is **lost** at once when the innkeeper falls (`ESC-06`), or at the end of a mist phase if the inn's
  integrity is 0 (`BRD-06`).
- **RUN-04** When a run ends, the player earns **Mist Essence** (`MET-02`), keeps everything the bestiary learned
  (`BST-04`), and returns to the title screen.

## 6.2 Cycle phases

```text
Dusk (setup, untimed) ──BeginMist──▶ Mist (automatic, timed) ──timer ends──▶ Dawn (aftermath, untimed) ──next──▶ Dusk
```

- **CYC-01** **Dusk** has no timer. The forecast shows which planes will converge (`PLN-03`), the omen (`OMN-01`) and
  what the bestiary knows about the creatures that may come (`BST-03`). The player buys from the shop (`SHP-*`),
  builds from the build menu (`FIX-05`), places staff and fixtures, equips items and buys supplies, then starts the
  mist with `BeginMist`.
- **CYC-02** **Mist** lasts `MistDuration` (default 120 s of game time). Creatures arrive (`CRE-01`), and the
  simulation runs with no player input except speed (`SCP-13`). The HUD shows the time to dawn.
- **CYC-03** Arrivals stop 20 s before dawn. When the timer ends, **dawn breaks**: every creature still on the board,
  calm or turned, dissolves back into the mist.
- **CYC-04** **Dawn** shows the cycle summary: creatures that stayed occupied and their tributes, creatures that
  turned, damage to the inn, staff who fell or died, and new bestiary entries. The player may pay for repairs
  (`BRD-07`), and wounded staff recover (`STF-12`). Moving on saves the run (`SAV-01`) and starts the next Dusk, with
  a fresh shop and the reroll cost reset (`SHP-05`).
- **CYC-05** The cycle log of the mist phase (`INV-12`) can be replayed at Dawn as a timeline (`UI-07`).

## 6.3 Pausing

- **CYC-10** Esc opens the pause menu in every phase and stops ticking (`INV-03`): Resume, Settings, and Quit to
  title (with a warning that the current cycle is lost, `SAV-03`). The pause button on the mist HUD only stops the
  simulation, so the player can look around.

## 6.4 Tuning defaults (in `data/balance.json`)

| Value | Default |
|---|---|
| `RunLength` | 10 cycles |
| `MistDuration` | 120 s |
| `StartingSilver` | 10 |
| Silver income per cycle | 5 base + tributes |
| Base arrival interval | 9 s, ×0.92 per cycle, ×0.6 on the Convergence |
| Planes per cycle | 1 on cycles 1-2, 2 on cycles 3-5, 3 on cycles 6-9, all on cycle 10 |
| Max creatures on the board | 12 (`INV-05`) |
| Max staff on the board | 3 + 1 per two cycles survived, up to 9 (excluding the innkeeper) |
| Bench size | 6 |
| Inn integrity | 100 |
| Shop offers | 3 staff + 3 items per Dusk |
| Draw weights | pity ×1.5; home plane ×2 (`SHP-04`) |
| Ancestry soothe bonus | ×1.25 (`STF-16`) |
| Reroll cost | 2, then +1 per reroll in the same cycle; resets each Dusk |
| Staff price | base cost (1-3) + 2 per rank above ★1 |
| Rank odds | see `SHP-03` |
| Staff item slots | 2 per staff member |
| Rooms | 2 at the start |

---

# 7. Planes and the mist

- **PLN-01** A plane is a data entry (`data/planes.json`) with an ID, a name, a mist colour, an ambience cue, its
  creatures and their weights, and an optional plane rule (for example "creatures of the Drowned Deep grow agitated in
  dry, warm rooms").
- **PLN-02** The prototype ships five planes. Three are unlocked at the start; the rest are unlocked with Mist Essence
  (`MET-03`):

  | Plane | Character |
  |---|---|
  | The Drowned Deep | Slow, cold, thirsty things from a sunken world |
  | The Ashen Waste | Brutes from a burnt world; they crave heat and hate silence |
  | The Hollow Wood | Rot and roots; they fear fire and are soothed by song |
  | The Gearbound Ruin | Machines of a dead empire; they want order and quiet |
  | The Pale Court | Hungry, beautiful, sapient nobles; they want company and flattery, and hate light |

- **PLN-03** At Dusk, Core draws the cycle's converging planes from the unlocked set with the seeded RNG, following
  the planes-per-cycle table (section 6.4). It does not draw the same set two cycles in a row when more than one set is
  possible.
- **PLN-04** Each converging plane opens one **mist gate** at the edge of the clearing, tinted in its colour.
  Creatures of that plane walk out of that gate to the inn's front door.
- **PLN-05** The mist is the main visual motif: dense fog around the clearing that hides the edge of the world, pours
  out of each open gate in its plane's colour, and creeps under the door during the mist phase (`ART-13`).

---

# 8. Creatures

- **CRE-01** During the mist phase, creatures arrive at a seeded random interval around the cycle's arrival interval
  (×0.6 to ×1.4). The plane is chosen evenly from the converging planes unless the omen changes the weights; the
  creature by the plane's weights.
- **CRE-02** A creature does not arrive if the board is full (`INV-05`); the next arrival is tried at the next
  interval.
- **CRE-03** A creature (`data/creatures.json`) has an ID, a plane, a name, a size (1 or 2 tiles), whether it is
  sapient, its appetites (section 9.2) and preferred techniques, triggers (section 9.3), agitation values, combat
  stats when turned (health,
  attack, attack interval, range, speed, armour), an escalation profile (`ESC-02`) and a tribute value.
- **CRE-04** On arrival, a creature walks in through the front door and looks for something that matches one of its
  appetites: a free seat at the bar, a hearth, a table, a card table, the stage, a staff member offering company. It
  walks to the best match it can reach
  (`AGI-05`), or wanders the common room if there is none.
- **CRE-05** At dawn, a creature that has not turned leaves a **tribute** (silver, by its tribute value) and
  dissolves. A turned creature dissolves with no tribute.
- **CRE-06** Creatures act only through the simulation's rules; the player never commands them (`SCP-25`).
- **CRE-07** Every creature that can take part in the Company service is sapient and adult (`RAT-01`); the data has
  `"sapient": true` and `"adult": true`, and the validator checks both (`DAT-04`).

---

# 9. Agitation

## 9.1 The agitation meter

- **AGI-01** Each creature has agitation from 0 to 100, starting at its arrival value (usually 10-30).
- **AGI-02** Agitation rises at the creature's base rate every second. Fixtures, omens, items and plane rules change
  the rate (`DAT-03`).
- **AGI-03** Thresholds:

  | Agitation | State | Behaviour |
  |---|---|---|
  | 0-49 | Calm | Seeks its appetites; rises at the base rate |
  | 50-74 | Restless | Rises 1.5× faster; wanders between rooms; knocks things over (small fixture damage) |
  | 75-99 | Menacing | Breaks one adjacent fixture every 8 s; frightens other creatures nearby (`AGI-08`) |
  | 100 | **Turned** | Becomes hostile and escalates (section 10); agitation no longer changes |

- **AGI-04** A turned creature stays turned until it is **banished** (reduced to 0 health) or dawn breaks. Nothing
  calms it in the prototype. (A technique or item that brings a turned creature back to Menacing is in the backlog.)

## 9.2 Appetites: keeping them occupied

- **AGI-05** An appetite is something a creature wants: `Drink`, `Food`, `Company`, `Gambling`, `Music`,
  `Spectacle`, `Brawl`, `Warmth`, `Quiet`, `Darkness`, `Order` (closed set, `DAT-03`). Each creature has one to three
  appetites, each with a strength: **craves** (×1.0), **likes** (×0.5). An appetite may also name a **preferred
  technique** (for example `Gambling` preferring Dice); served with that technique, it soothes ×1.25.
- **AGI-06** A creature is **occupied** while it is being served one of its appetites, by a staff member's technique
  (`STF-03`) or by a fixture (a Hearth giving `Warmth`, a quiet corner giving `Quiet`). While occupied, its agitation
  falls at the provider's **soothe** rate × the appetite strength, instead of rising.
- **AGI-07** A provider can serve a limited number of creatures at once: each technique states its capacity (a
  Drink technique: 1; Song: everyone in the zone; Cards: the seats at a card table; a Hearth: the 4 nearest tiles).
  Techniques that need supplies (`Drink`, `Food`) use one supply per serving; with no supplies left they cannot be
  used (`FIX-06`).

## 9.3 Triggers and contagion

- **AGI-08** A trigger is a condition that raises a creature's agitation at once or speeds it up while the condition
  holds: `Light` (in a lantern's radius), `Noise` (near the stage), `Heat`, `Cold`, `Crowding` (more than N creatures
  in the zone), `RivalPlane` (a creature of a named plane in the same zone), `Violence` (an attack within 4 tiles),
  `Blood` (a fallen unit within 4 tiles). Closed set, `DAT-03`.
- **AGI-09** Contagion: when a creature turns, every creature within 4 tiles that sees it gains +15 agitation (plus its
  own `Violence` trigger if it has one). This is how one mistake becomes a chain reaction.
- **AGI-10** Triggers and appetites are hidden until the bestiary reveals them (`BST-02`), but their effects are always
  visible: the agitation bar moves, and the cycle log records the cause (`INV-12`).

---

# 10. Escalation and combat

## 10.1 Escalation

- **ESC-01** A turned creature escalates through three stages, in this order:

  | Stage | Target | Moves to the next stage when |
  |---|---|---|
  | 1. **Wreck** | The nearest fixture | It has destroyed one fixture, or 10 s have passed |
  | 2. **Prey** | The nearest creature or staff member | It has made one unit fall, or 15 s have passed |
  | 3. **Hunt** | The innkeeper | (final stage) |

- **ESC-02** A creature's escalation profile may change the timings or skip a stage (for example Pale Court creatures
  skip Wreck), from data.
- **ESC-03** In every stage, a turned creature that is attacked by a fighter turns on that fighter until the fighter
  falls or moves out of reach, then returns to its stage target.
- **ESC-04** Calm creatures attacked by a turned creature lose health like anyone else; if they fall, they dissolve
  (no tribute) and count as `Blood` for the creatures around them (`AGI-08`).

## 10.2 Combat

- **ESC-05** Combat is fully automatic and deterministic. Each fighter has health, attack, attack interval, range,
  armour and speed. A hit does `max(1, attack − armour)` damage. There are no random critical hits or misses; variety
  comes from positions, timing and abilities.
- **ESC-06** The innkeeper is a unit with low attack and moderate health. The player places it during Dusk like any
  other unit (it may not be sold or benched). **If the innkeeper falls, the run ends at once** (`RUN-03`).
- **ESC-07** Staff with a Fighter technique (Fists or Weapons, `STF-02`) engage any turned creature in their zone or
  within their guard radius, in order of threat (Hunt before Prey before Wreck, then nearest). Engaging a turned
  creature always takes priority over serving appetites (`STF-04`).
- **ESC-08** A turned creature reduced to 0 health is **banished**: it dissolves into mist, leaves no tribute, and
  gives bestiary progress (`BST-02`).
- **ESC-09** A staff member reduced to 0 health **falls**. At Dawn, a fallen staff member is wounded (70 %) or dead
  (30 %), on a roll from the run's RNG (`INV-02`). A dead staff member is removed from the inn with their items
  returned to storage; the character can still be offered and hired again from the shop (`SHP-07`).
- **ESC-10** Fixtures have health. A destroyed fixture stops working until it is repaired at Dawn (`BRD-07`). Nothing
  repairs fixtures during the mist phase.

---

# 11. Staff

## 11.1 Characters, classes and techniques

- **STF-01** Staff are **unique named characters** (`data/staff.json`), each with an ID, a name, an origin (a plane,
  or the Mortal Realm), an optional hidden **ancestry** (`STF-15`), a base cost (1 to 3), a **main class**, base stats
  (health, attack, attack interval, range,
  armour, speed, soothe), a ★1 base technique, an upgrade tree (`STF-07`) and a preset build for each rank
  (`STF-10`). At most one copy of a character is ever in the inn.
- **STF-02** Classes and their techniques (closed sets, `DAT-03`):

  | Class | Technique | Answers | Notes |
  |---|---|---|---|
  | **Companion/Server** | Company | `Company` | On the floor or in a room (section 11.5) |
  | | Food | `Food` | Needs a table; uses a Food supply |
  | | Drink | `Drink` | Needs a bar counter; uses a Drink supply |
  | | Comfort | `Quiet` | A soft voice and full attention; no supplies; one creature at a time |
  | **Gambler** | Cards | `Gambling` (skill) | Needs a card table |
  | | Dice | `Gambling` (chance) | Needs a card table or any table |
  | | Bones | `Gambling` (chance) | Played on the floor; no table needed |
  | **Fighter** | Fists | Turned creatures; `Brawl` | Fast, light hits; sparring soothes calm creatures that crave `Brawl` |
  | | Weapons | Turned creatures | Slower, heavy hits; never used on calm creatures |
  | | Grapple | Turned creatures; `Brawl` | Holds a turned creature, slowing its attacks, instead of hurting it |
  | **Entertainer** | Song | `Music` | Reaches the whole zone; more from the stage |
  | | Dance | `Spectacle` | Needs open floor or the stage |
  | | Tales | `Spectacle` | A small audience close by; no stage needed |

  Each technique's data gives its soothe multiplier, capacity (`AGI-07`), range, whether it needs a fixture (the
  fixtures it works at are those whose `Enables` effects name it, `DAT-03`), supplies used,
  its mastery values (`STF-17`) and, for Fighter techniques, its attack modifiers.
- **STF-03** A character **has a class** if it knows at least one technique of that class. Items may add further
  class **tags**, which count only for synergies (`ITM-03`).
- **STF-04** **What staff do in the mist phase.** Each time a staff member is free, it chooses in this order:
  1. If it knows a Fighter technique and a turned creature is within its guard radius: engage it (`ESC-07`).
  2. Otherwise, the technique and creature pair in reach with the highest value: appetite strength × preferred
     technique bonus × the technique's soothe, respecting capacity, required fixtures, supplies and refusals
     (`CMP-02`). Ties go to the lowest creature ID (`INV-02`).
  3. Otherwise, wait at its post.

  Staff stay at their placed tile, except fighters who engage and companions who go to a room (`CMP-04`).
- **STF-05** Staff on the bench do nothing during the mist phase and cannot be hurt.

## 11.2 Ranks and upgrade trees

- **STF-06** Characters have **five ranks**, ★1 to ★5. Each rank above ★1 gives one upgrade pick, either a new
  technique or the mastery of one the character already knows (`STF-17`), and multiplies the base stats by that
  rank's multiplier from data. A ★5 character has its base technique and four picks.
- **STF-07** Each character has an **upgrade tree**: for each of ranks ★2 to ★5, two or three options, each a new
  technique or a mastery. Options come from the main class or from **one second class**; a tree spans **at most two
  classes**. An option may require an earlier choice, so the tree can branch.
- **STF-08** **Upgrading**: buying a shop offer of a character the player already owns raises the owned character by
  **exactly one rank**, whatever rank the offer shows. The player then picks one option for the new rank
  (`ChooseUpgrade`). The price paid is the offer's price, so upgrading from a high-rank offer costs more for the same
  single rank (`SHP-08`). Offers of an owned ★5 character cannot be bought.
- **STF-09** Selling a character returns half the silver spent on it, rounded down. Its items go to storage
  (`ITM-02`).
- **STF-10** **Hiring** a character the player does not own gives that character at the offer's rank with its
  **preset build** for that rank (techniques predetermined in data). Later upgrades continue up the tree from that
  build.
- **STF-17** **Mastery** makes a known technique stronger by the mastery values in its data (for example ×1.5 soothe
  and +1 capacity, or +3 attack for a Fighter technique). Each technique can be mastered once. An option is
  **available** when its requirement (if any) was chosen, and its technique is not known yet (a new technique) or is
  known and not yet mastered (a mastery). Every tree offers at least one available option at every rank, whatever was
  picked before; the validator checks every path (`DAT-04`).

## 11.3 Wounds and death

- **STF-11** A fallen staff member is wounded or dead at Dawn (`ESC-09`). A dead character can appear in the shop
  again at any rank (`SHP-07`); hiring them again is a new hire (`STF-10`).
- **STF-12** At Dawn, wounded staff recover to full health; staff who fell and survived the roll come back with a
  `Shaken` status for the next cycle (−20 % soothe).
- **STF-13** Every staff member is an adult (`RAT-01`); the data has `"adult": true` (`DAT-04`).

## 11.4 Roster, origins and ancestry

- **STF-14** The prototype roster is **16 characters, 4 per main class**. Origins are weighted towards the Mortal
  Realm: about 10 Mortal and 6 plane-born, the plane-born spread across the five planes (a content guideline, checked
  by a data test, not by the validator).
- **STF-15** **Ancestry** is a hidden link between a character and one plane, which may be a locked plane. About half
  of the Mortal characters have one. Plane-born characters are linked to their origin plane and have no separate
  ancestry. Ancestry counts for the home-plane shop weight (`SHP-04`) from the start, even while hidden.
- **STF-16** Ancestry is **revealed by observation**: the first time the character soothes or fights a creature of
  its ancestral plane during the mist phase, the link is revealed, shown on the character's panel and recorded in the
  cycle log. The reveal is kept in the profile across runs (`MET-01`). A revealed ancestry gives a ×1.25 soothe bonus
  with creatures of that plane and counts as that plane's origin for synergies (`SYN-01`). Before it is revealed it
  has no effect except the shop weight.

## 11.5 Company (the brothel)

- **CMP-01** The Company technique belongs to the Companion/Server class. Characters who know it are adult
  professionals with a personality, **preferences** (creatures they like to entertain) and **refusals** (creatures
  they will not entertain), from data.
- **CMP-02** Company is given only to sapient, adult creatures (`CRE-07`) that crave or like `Company` and are not on
  the character's refusal list. A preferred creature is soothed ×1.5. The simulation always honours refusals, and the
  player never chooses the pairing (`RAT-07`).
- **CMP-03** **On the floor**: flirting and conversation, shown in frame at a table or in the lounge, within the
  limits of `RAT-02`. It soothes at the technique's base rate, for one creature at a time.
- **CMP-04** **In a room**: when a creature has had floor company for at least 8 s, still craves `Company`, and a
  room is free, the pair goes to the room. The session lasts 25 s and happens **off-screen**: the door closes, and one
  line of flavour text may appear in the cycle log. During the session the creature counts as occupied at three times
  the floor rate, and it comes out Calm. The companion is **out of play** for the session plus a 10 s rest: it serves
  no one and cannot fight.
- **CMP-05** **Interruptions**: floor company ends at once when violence happens within 4 tiles. A turned creature in
  its Wreck stage may target an occupied room's door like a fixture; when the door is attacked, the session ends at
  once and both come out before any violence is shown (`INV-06`). Suggestive content and violence are never shown
  together.
- **CMP-06** With the suggestive content filter on (`SET-04`), Company becomes **Hosting**: conversation, cards and
  poetry on the floor, and a private audience behind the closed door. The mechanics are the same, and only flavour
  text marked `filterSafe` is used.

---

# 12. The inn board: layout and fixtures

## 12.1 The board

- **BRD-01** The inn is a grid of 1 m tiles (prototype: 14 × 10) divided into **zones**: Entrance, Common Room, Bar,
  Kitchen, Lounge, Stage, **Rooms** (two back rooms at the start, each with a door) and Cellar door. The layout is
  defined in `data/inn.json` (`SIM-04`). Inn items may add rooms (`ITM-06`), from a fixed set of extra room slots in
  the layout.
- **BRD-02** During Dusk, the player places units and fixtures on free floor tiles by drag and drop. Some fixtures
  belong to a zone (the bar counter in the Bar, the stage in the Stage zone); some techniques need a fixture to work
  (Drink needs a bar counter, Food a table, Cards a card table, `STF-02`).
- **BRD-03** Placements must leave a path from the front door to every zone, and to **every room on its own**: a
  placement that cuts off any single room is refused, even when another room is still reachable. A placement that
  would block either is refused (`TileBlocked`).
- **BRD-04** Placements stay from cycle to cycle until the player moves them; destroyed fixtures stay in place,
  broken, until they are repaired.
- **BRD-05** The front door is where every creature enters. Fixtures may not be placed on the door tile or the two
  tiles in front of it.
- **BRD-06** **Integrity** is the inn's overall health (100 at the start of a run). Damage to fixtures and walls lowers
  it. The run is lost if integrity is 0 at the end of the mist phase (`RUN-03`).
- **BRD-07** At Dawn, the player may pay silver to repair fixtures and integrity. Unrepaired damage carries over.

## 12.2 Fixtures

- **FIX-01** A fixture (`data/fixtures.json`) has an ID, a name, a footprint (tiles), the zone it may go in, health, a
  cost, and effects with a **radius** or an **adjacency** rule (`DAT-03`).
- **FIX-02** Prototype fixtures:

  | Fixture | Effect |
  |---|---|
  | Bar counter | Lets the Drink technique serve; 3 seats |
  | Table | Lets the Food and Dice techniques serve; 2 seats |
  | Card table | Lets the Cards and Dice techniques serve 4 seats |
  | Stage | Song and Dance from the stage reach the whole zone and one more; `Noise` trigger within 4 tiles |
  | Hearth | Gives `Warmth` to the 4 nearest tiles; `Heat` trigger within 3 tiles |
  | Quiet corner | Gives `Quiet` to 2 seats; any `Noise` within 3 tiles cancels it |
  | Lantern | `Light` within 3 tiles: calms some creatures, triggers others |
  | Ward circle | Agitation rises 25 % slower within 2 tiles |
  | Barricade | Blocks a tile; absorbs damage; turned creatures attack it to pass |
  | Salt line | Turned creatures of one plane (chosen when placed) cannot cross it |

- **FIX-03** **Adjacency** effects apply to fixtures and units next to each other (8 neighbours), for example a Ward
  circle next to a room door protects the session inside from interruption by the first attack, or a Lantern next
  to the Stage doubles the stage's reach.
- **FIX-04** Fixture effects are applied by Core systems reading the board's modifiers, never by code that checks for
  a specific fixture ID.
- **FIX-05** **The build menu** is a fixed catalogue: every unlocked fixture, always available at a fixed price,
  separate from the random shop. Built fixtures go to storage until placed. Fixtures sell back for half.
- **FIX-06** **Supplies** (`Drink`, `Food`) are bought in bundles from the build menu at Dusk and used by the Drink and
  Food techniques (`AGI-07`). Unused supplies carry over.

---

# 13. Shop, items and synergies

## 13.1 Silver and the shop

- **SHP-01** **Silver** is the run's only currency. It is earned from the base income each cycle and from tributes
  (`CRE-05`), and spent in the shop, the build menu (`FIX-05`) and on Dawn repairs. Silver never goes below zero; a
  purchase that would do so is refused. Silver exists only inside a run and can never be bought (`SCP-27`).
- **SHP-02** Each Dusk the shop shows **3 staff offers** and **3 item offers**, drawn with the run's RNG. A staff
  offer is a character at a rank (`STF-10`); an item offer is a staff item or an inn item (section 13.2).
- **SHP-03** **Rank odds** grow over the run (tuning values in `data/balance.json`):

  | Cycles | ★1 | ★2 | ★3 | ★4 | ★5 |
  |---|---|---|---|---|---|
  | 1-2 | 85 % | 15 % | | | |
  | 3-4 | 65 % | 28 % | 7 % | | |
  | 5-6 | 45 % | 35 % | 16 % | 4 % | |
  | 7-8 | 30 % | 35 % | 24 % | 9 % | 2 % |
  | 9-10 | 20 % | 30 % | 28 % | 16 % | 6 % |

- **SHP-04** **Draw weights**: each character's chance to be drawn is multiplied by:
  - **Pity**: ×1.5 if the player owns the character, so upgrades are not pure luck.
  - **Home plane**: ×2 if the character's plane (its plane-born origin, or its ancestry, revealed or not, `STF-15`) is
    converging that cycle (`PLN-03`).

  The multipliers stack. The same weights apply to rerolls. Owned ★5 characters are not drawn. Characters already
  showing in the current offers are not drawn twice.
- **SHP-05** **Reroll** redraws every offer not yet bought. The first reroll in a cycle costs 2, and each further
  reroll in the same cycle costs 1 more. The cost resets at each Dusk. `RerollCost` effects from items and inn items
  lower it, down to 0 (`DAT-03`).
- **SHP-06** **Lock** keeps the current offers for the next Dusk instead of a new draw.
- **SHP-07** Any character can be offered: one never hired, one who died, or one the player owns (an upgrade offer,
  `STF-08`).
- **SHP-08** **Prices**: a staff offer costs the character's base cost + 2 per rank above ★1. Item prices come from
  their rarity and data. The same price applies whether the offer is a hire or an upgrade.
- **SHP-09** Unspent silver carries over between cycles, but never between runs.

## 13.2 Items

- **ITM-01** Items (`data/items.json`) have an ID, a name, a kind (**staff item** or **inn item**), a rarity (Common,
  Rare, Cursed), a price, effects (`DAT-03`), optional class tags, and for inn items a display model and a display
  spot.
- **ITM-02** **Staff items** are carried by a staff member, with **two slots** each. During Dusk they can be moved
  freely between staff and **storage** (up to 8 items). Their effects apply to the carrier.
- **ITM-03** **Class tags** on a staff item count the carrier as having that class **for synergy calculations only**
  (`SYN-01`). A tag never gives a technique and never changes what the carrier can do.
- **ITM-04** **Inn items** apply across the whole run from the moment they are bought: to staff, creatures, the shop
  (for example cheaper rerolls), the economy or the board. They replace the relics of other roguelikes. Each one is
  **shown in the inn** at its display spot (a cursed bell over the door, a ledger on the bar), up to 12 (`INV-05`).
- **ITM-05** **Cursed** items have a strong upside and a real downside.
- **ITM-06** Prototype items, for example:

  | Item | Kind | Effect |
  |---|---|---|
  | Brass Knuckles | Staff | Fists +3 attack; Fighter tag |
  | Lucky Dice | Staff | Dice soothe ×1.3; Gambler tag |
  | Silk Scarf | Staff | Company soothe ×1.2 |
  | Lute | Staff | Song capacity +2; Entertainer tag |
  | Ward Charm | Staff | Agitation rises 10 % slower within 2 tiles of the carrier |
  | Drowned Bell | Inn | Drowned Deep creatures arrive 10 agitation calmer |
  | Innkeeper's Ledger | Inn | +1 silver per tribute |
  | Loaded Coin | Inn | Rerolls cost 1 less |
  | Ember in a Jar | Inn | Hearths also count as lanterns |
  | Spare Keys | Inn | +1 room |
  | Cursed Crown | Inn (Cursed) | +1 board slot; the innkeeper has half health |

- **ITM-07** Items can be sold back for half their price, rounded down.

## 13.3 Synergies

- **SYN-01** A synergy is a bonus for having enough **distinct characters on the board** (the bench does not count)
  with one class, or of one origin. A character counts for a class through its techniques (`STF-03`) or a class tag on
  an item it carries (`ITM-03`), and for an origin through its plane-born origin, the Mortal Realm, or a revealed
  ancestry (`STF-16`). Class thresholds are 2, 4 and 6; origin thresholds are 2 and 4.
- **SYN-02** Prototype synergies, for example: **Companion/Server** (2/4/6): +10/+25/+50 % soothe for Company, Food
  and Drink; **Gambler** (2/4): tributes +1/+3 silver; **Fighter** (2/4/6): +2/+4/+7 armour; **Entertainer** (2/4):
  Song and Dance reach one/two more zones; **Drowned** (2): Drowned staff soothe Drowned creatures ×1.5; **Mortal**
  (2/4): +10/+25 % health.
- **SYN-03** Synergy effects use the same effect kinds as items and fixtures (`DAT-03`). The setup UI shows active and
  nearly-active synergies (`UI-03`).

---

# 14. Roguelike systems: omens, bestiary, meta

## 14.1 Omens

- **OMN-01** Each Dusk from cycle 2, Core may draw one omen (`data/omens.json`), by weight, with a 60 % chance (100 %
  on the Convergence). The forecast shows it.
- **OMN-02** Prototype omens: **Blood Moon** (all creatures arrive at +20 agitation), **Thick Mist** (arrivals −30 %,
  but creatures are hidden until they reach the door), **Surge** (one plane sends double), **Drought** (no supplies in
  the build menu this cycle), **Feast Night** (tributes +50 %, arrivals +30 %), **Wanderers** (the shop shows one
  extra staff offer at +1 rank).
- **OMN-03** An omen's effect uses the same effect kinds (`DAT-03`) and lasts one cycle.

## 14.2 The bestiary

- **BST-01** The bestiary (in the profile) has an entry for every creature, with four reveal levels: **Unknown**
  (silhouette only), **Seen** (name, plane, size), **Studied** (appetites, their strengths and preferred techniques),
  **Understood** (triggers, combat stats and escalation profile).
- **BST-02** Reveal progress comes from observation during the mist phase: seeing a creature reveals **Seen**; keeping
  it occupied until dawn adds study points towards **Studied**; seeing each of its triggers fire, or banishing it, adds
  points towards **Understood**. The thresholds are in data.
- **BST-03** The Dusk forecast shows, for each creature the converging planes may send, what the bestiary knows. The
  rest shows as "?".
- **BST-04** Bestiary knowledge is kept in the profile across runs, win or lose (`MET-01`).

## 14.3 Meta-progression

- **MET-01** The profile (`SAV-02`) keeps Mist Essence, unlocks, the bestiary, revealed ancestries and run statistics
  across runs.
- **MET-02** Mist Essence at the end of a run = cycles survived × 2 + creatures kept occupied until dawn ÷ 5 (rounded
  down) + 10 if won.
- **MET-03** Essence buys unlocks on the title screen: new planes, new characters in the shop pool, new items in the
  shop pool, new fixtures in the build menu, and a choice of starting staff. Unlocks never make the base game
  unwinnable without them.

---

# 15. Camera, input and user interface

## 15.1 Camera and input

- **CAM-10** An isometric-style camera (orthographic or a narrow-FOV perspective, about 45° down) looking at the inn.
  Pan with WASD, the arrow keys, the middle mouse button or the screen edges; zoom with the wheel; rotate in 90° steps
  with Q and E. The camera stays inside the clearing.
- **CAM-11** The roof and the wall nearest the camera fade out so the board is always visible.
- **CAM-12** During the mist phase, clicking a unit or creature selects it and shows its panel; F follows the
  selected one. Selection never changes the simulation (`SCP-25`).
- **CAM-13** Dragging from the shop, the bench or the board, and dropping on a tile, places a unit or fixture during
  Dusk. Right-click on a placed item opens Sell, Bench and Rotate. Invalid tiles show red.

## 15.2 Screens

- **UI-01** Title screen: New Run (with an optional seed field), Continue (when a run save exists), Bestiary, Unlocks,
  Settings and Quit.
- **UI-02** Dusk HUD: cycle number, silver, integrity, the forecast (planes, omen, known creatures), and the Begin the
  Mist button.
- **UI-03** Dusk shop panel: the staff offers (each showing the character, rank, classes and techniques, and an
  **Upgrade** badge when the player owns that character) and the item offers, Reroll with its current cost, Lock, the
  bench, item storage, and the synergy panel showing active and nearly-active synergies. A separate **build menu** tab
  holds the fixed fixture catalogue and supplies (`FIX-05`, `FIX-06`).
- **UI-04** Mist HUD: time to dawn, speed controls (pause, 1×, 2×, 4×), integrity, the innkeeper's health, and a
  short feed of important events (a creature turns, a unit falls).
- **UI-05** Over each creature: an agitation bar coloured by state (Calm, Restless, Menacing, Turned) and an icon for
  what it is doing. Over each unit: a health bar and an icon for what it is doing.
- **UI-06** Unit and creature panels: for staff, rank, classes, origin, ancestry (shown as "?" until revealed), known
  techniques, the upgrade tree (chosen, available and locked options), items and stats; for creatures, appetites, preferred techniques and triggers (only what the
  bestiary has revealed); for both, the current activity.
- **UI-07** Dawn summary: tributes, turned creatures, banished creatures, fallen and dead staff, damage, new bestiary
  entries, and the **cycle timeline**: the cycle log (`INV-12`) as a scrubbable timeline that explains each turn and
  death.
- **UI-08** Upgrade choice: when an upgrade offer is bought, a dialog shows the character's tree and the 2 or 3
  options for the new rank; the player picks one (`STF-08`). Inn items show where they will appear in the inn.
- **UI-09** UI text uses `Tr()` keys. Names and flavour text come from the data files.
- **UI-10** Run end screen: win or loss and its cause, cycles survived, Mist Essence earned, bestiary progress.
- **UI-11** One theme (`InnTheme.tres`): dark wood and parchment, candlelit accents, a readable serif or
  semi-serif font (licensed, `INV-10`). The minimum text size is 18 px at 1080p.

---

# 16. Art direction and audio

## 16.1 Art

- **ART-10** **Dark stylised**: normal proportions with slightly exaggerated silhouettes, simple forms, painterly or
  toon shading with soft, dark bands. Readable from the isometric distance.
- **ART-11** **Palette**: muted, desaturated colours. Inside the inn, warm candle and hearth light; outside, cold
  blue-grey mist. Each plane has one saturated signature colour, used sparingly: its gate, its creatures' glow, its UI
  accents.
- **ART-12** **Creatures** read as dangerous through silhouette, movement and glow rather than gore. The agitation
  state shows in the model too (posture, glow intensity, colour shift).
- **ART-13** **The mist** (`PLN-05`): volumetric fog or layered fog cards around the clearing and at the gates, drifting
  slowly; it creeps into the inn during the mist phase and pulls back at dawn.
- **ART-14** **Violence** follows section 17: hit flashes, knockdowns, blood splashes and decals, fallen bodies that
  dissolve or are carried off at dawn. No dismemberment.
- **ART-15** **Characters who know Company** use adult styling: attractive, confident, dressed in alluring but
  non-revealing outfits (`RAT-03`). Each character's look reflects its classes, and a branch into a second class shows
  on the model (for example a blade at the hip of a dancer who learned Weapons).
- **ART-17** **Inn items** have a visible model placed at their display spot in the inn (`ITM-04`).
- **ART-16** Placeholders: until step 10, units are capsules and creatures are coloured shapes with a name label;
  fixtures are boxes. They use the same scenes and data as the final art, so replacing them changes no code.

## 16.2 Audio

- **AUD-10** Audio buses: Master, Music, SFX and Ambience, each with a volume setting (`SET-02`).
- **AUD-11** Music: a quiet, uneasy theme at Dusk; a tense, building theme in the mist that rises with the number of
  turned creatures; a sombre theme at Dawn.
- **AUD-12** Ambience: wind and distant sounds from each open plane outside; a creaking, crackling inn inside.
- **AUD-13** SFX: footsteps, pouring, cooking, music from the stage, agitation cues (growls rising), turning stings,
  hits, falls, breaking furniture, dawn's arrival, UI sounds.
- **AUD-14** Every audio file is licensed and listed in `docs/ASSETS.md` (`INV-10`).

---

# 17. Content rating and tone

The human chose a **Mature** ceiling: a dark tone with **moderate blood** (D-019) and **suggestive** themes (D-004).
This section is the only place that sets what content is allowed. Agents shall not go past it, and shall stop and ask
(`AGT-03`) when unsure.

- **RAT-01** **Adults only.** Every character, including the innkeeper, staff, companions and every sapient creature,
  is an adult. Data enforces this (`STF-13`, `CRE-07`, `DAT-04`). No character is described, written or styled as a
  child.
- **RAT-02** **Suggestive content allowed**: flirting, innuendo, romance, teasing dialogue, alluring outfits, light
  touches (a hand on an arm, leaning close) shown in frame on the floor (`CMP-03`), and implying that something
  happens behind a closed room door (`CMP-04`).
- **RAT-03** **Suggestive content not allowed**: explicit sexual acts, depicted or described; nudity; sexual focus on
  body parts. Company involves only sapient, adult creatures (`CRE-07`).
- **RAT-04** **Never allowed**, by any setting, mod hook or decision: sexual violence or coercion; anything
  non-consensual; sexual content combined with violence (`INV-06`, `CMP-05`); content involving minors in any way;
  torture shown for its own sake.
- **RAT-05** **Violence allowed (moderate blood)**: attacks, injuries shown through blood splashes and decals, staff
  and creatures falling and dying, bodies on the floor until dawn. Deaths have weight: a fallen staff member is named
  in the Dawn summary.
- **RAT-06** **Violence not allowed**: dismemberment, decapitation, exposed organs, lingering close-ups of injuries,
  cruelty to the helpless played as a joke.
- **RAT-07** **Companions** are respected adult professionals who choose their work. The game always honours their
  refusals (`CMP-03`); the player cannot force a pairing. They are never the butt of a joke, and harm to them is never
  played for titillation.
- **RAT-08** **Flavour text** for Company is one line, dark-romantic and cheeky rather than crude, for example *"The
  curtain falls. For once, the Pale Duchess forgets she is hungry."*
- **RAT-09** **Content filters** (`SET-04`): **Blood** (full or reduced: no blood decals, hits flash instead) and
  **Suggestive content** (on or off: companions become hosts, `CMP-06`). Both are honoured everywhere, including the
  cycle log.
- **RAT-10** When the game is published, the store page states the content honestly: "Moderate blood and violence,
  suggestive themes; both can be reduced in Settings." For a hobby release, a short content note on first launch is
  enough.

---

# 18. Content data

- **DAT-01** Content files live in `data/` as JSON: `balance.json`, `inn.json`, `planes.json`, `creatures.json`,
  `staff.json` (characters, upgrade trees, preset builds), `techniques.json`, `items.json`, `fixtures.json`,
  `synergies.json`, `omens.json`, `unlocks.json`, `flavour.json`.
- **DAT-02** Core defines the schema as C# records and loads the files through `IContentSource`, which returns file
  text by name. The Godot adapter reads `res://data/` with `FileAccess`; the tests read from a folder or from strings.
  `export_presets.cfg` shall include `data/*.json` in the exported files.
- **DAT-03** Effects, appetites, triggers and roles are closed sets of kinds with parameters, for example
  `{ "kind": "AgitationRate", "radius": 2, "multiplier": 0.75 }`. Effect kinds: `AgitationRate`, `ArrivalAgitation`,
  `Soothe`, `ArrivalRate`, `PlaneWeight`, `Stat` (stat + amount or multiplier), `Radius`, `BoardSlots`, `Tribute`,
  `Income`, `ShopOdds`, `Supplies`, `Blocks`, `PlaneBarrier`, `Aura`, `RerollCost` (shop rerolls cost more or less),
  `RoomSlots` (more or fewer rooms), `Provides` (a fixture gives an appetite to a number of seats or tiles), `Enables`
  (a technique needs this fixture to work, with its seats), `Emits` (a fixture gives off `Light`, `Noise`, `Heat` or
  `Cold` within a radius) and `CountsAs` (one fixture also counts as another, for example a hearth as a lantern). A new
  kind in any of these sets is an SDD change.
- **DAT-04** The content validator runs at load time and in the tests. It rejects: duplicate IDs, references to IDs
  that do not exist (including an ancestry plane), unknown kinds, values out of range, staff or companions without `"adult": true`, creatures with a
  `Company` appetite without `"sapient": true` and `"adult": true`, flavour text without its `filterSafe` and
  `bloodSafe` flags, and an `inn.json` with no path from the door to every zone. In a debug build an invalid file stops
  the game with a clear message; in an export build the game falls back to the whole content set built into it (the
  rules cross files, so one file is never swapped on its own).
- **DAT-05** IDs are lowercase kebab-case (`drowned-deep`, `pale-duchess`) and never change once a save may contain
  them. Display names may change freely.
- **DAT-06** Flavour text has a `filterSafe` flag and a `bloodSafe` flag. With a content filter on, only lines with
  the matching flag set to `true` are used (`RAT-09`).

---

# 19. Save, load and settings

- **SAV-01** The run is saved automatically when the player leaves Dawn (`CYC-04`), to `user://saves/run.json`. The
  save holds the seed, the RNG state, the cycle, silver, integrity, supplies, the board (units, fixtures, their damage),
  the bench, each character's rank, techniques, tree choices, items and status, item storage, inn items, the
  locked shop, unlocked planes for the run, and the content version.
- **SAV-02** The profile (`user://saves/profile.json`) holds Mist Essence, unlocks, the bestiary, revealed ancestries,
  statistics and the content note acknowledgement. It is saved at every Dawn (the bestiary and ancestries) and when a
  run ends or an unlock is bought.
- **SAV-03** Saving is only between cycles. Continue resumes at the start of the saved Dusk. Quitting during a cycle
  loses that cycle, and the pause menu says so. (Bestiary progress from the lost cycle is also lost.)
- **SAV-04** Saves have a `schemaVersion`. Core migrates older versions forward step by step. A save from a newer
  version is not loaded, and the player is told why.
- **SAV-05** Writes are atomic: write `*.tmp`, then replace. The previous file is kept as `*.bak`.
- **SAV-06** A corrupt save is moved to `*.corrupt-<n>.json`, the player is told, and the game goes on without it
  (`INV-07`). A run save is deleted when the run ends.
- **SET-01** Settings are in `user://settings.cfg` (Godot `ConfigFile`), outside the saves.
- **SET-02** Audio: volume per bus (`AUD-10`).
- **SET-03** Controls: camera pan speed, edge panning on or off, zoom speed.
- **SET-04** Content filters: Blood (full or reduced) and Suggestive content (on or off) (`RAT-09`).
- **SET-05** Display: window mode (windowed, borderless, fullscreen), V-Sync, resolution scale.
- **SET-06** Gameplay: default mist speed, and whether the game pauses automatically when the innkeeper is attacked
  (a viewing aid; it does not change the outcome).

---

# 20. Testing

- **TST-01** Core tests are deterministic: a fixed seed through a test `IRandomSource`, time only through `Tick`, no
  sleeps, no file system except temporary folders for save tests.
- **TST-02** NUnit. Each test method carries `[Property("Requirement", "<ID>")]` for every requirement it verifies
  (more than one is allowed).
- **TST-03** Every behaviour change in Core comes with tests in the same step (`AGT-04`); no test only re-asserts a
  mock or a constant.
- **TST-04** Cycle and simulation: phase transitions, commands refused in the wrong phase, arrival intervals and caps,
  dawn rules, run win and loss, determinism (two sessions with the same seed and commands produce the same state hash
  after a full mist phase, `ARC-05`).
- **TST-05** Agitation and service: rise rates, thresholds and their behaviours, appetite matching and soothe rates,
  provider capacity, supplies, each trigger kind, contagion, the cycle log recording causes.
- **TST-06** Escalation and combat: stage order and timers, profile overrides, guard targeting order, damage formula,
  banishing, staff falling and the wound-or-die roll, the innkeeper ending the run, fixture destruction, integrity.
- **TST-07** Board and content: placement rules, path checks, adjacency, the board coming from `inn.json` (`SIM-04`),
  the real `data/` folder loading and validating, each validator rule rejecting a bad
  example, content filters.
- **TST-08** Saves: round trip, atomic replace, `.bak` kept, corrupt file quarantined, older version migrated, newer
  version refused.
- **TST-09** Shop, staff and roguelike: rank odds per cycle, pity and home-plane weights (including hidden ancestry),
  ancestry reveal by observation and its effects, the roster guideline (`STF-14`), no duplicate offers, reroll cost
  escalation and reset, reroll discounts, lock, hiring at a rank with the preset build, upgrading exactly one rank at
  the offer's price, owned ★5 offers refused, tree options and the two-class limit, selling, technique choice in the
  mist (`STF-04`), item slots and storage, class tags counting only for synergies, inn item effects, synergy
  thresholds, plane and omen draws, effects through modifiers, bestiary reveal thresholds, the Mist Essence formula,
  unlocks.
- **TST-10** Company: refusals always honoured, preferences, sapient-only Company, floor company, room sessions
  (conditions, duration, the companion out of play), interruptions by violence and by an attacked door, hosting with
  the filter on.
- **TST-11** Presentation (Godot nodes) has no automated tests in the prototype. Each step's demo script (section 21) is
  the manual check, and the human runs it in the editor.
- **TST-12** A headless smoke run is part of every step's commands for the human:
  `<Godot folder>\Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . --quit-after 600`. It shall print no
  errors. The Godot folder on the development machine is recorded in `docs/PROGRESS.md` (toolchain table). The smoke
  run starts at step 2, the first step with a main scene; in steps 0 and 1 the human opens and builds the project in
  the Godot editor instead.

---

# 21. Implementation steps

Steps are done **one at a time**. Each step gives the sections to read, the prompt, the requirements it covers,
acceptance criteria and a demo. After each step the human builds, tests, plays the demo and reads the report.

**Common preamble** (put before every step prompt):

> You are implementing Misty Inn from SDD v1.5. Work **only** on the step below; do not start later steps. Read
> sections 0, 2, 17 and 22 and the sections the step lists first. Apply the invariants (section 2) and the agent
> rules (section 22). If anything is ambiguous, contradictory or missing, **stop and ask** before writing code. Do not
> run builds, tests, Godot or git: list the exact commands for the human and mark results NOT RUN. Finish with the
> step report in Appendix A, including deviations from the SDD and proposed SDD changes.

## Step 0: Repository inspection (read-only)

**Read:** 1, 3, 5.
**Prompt:** Inspect the repository and the environment. The repository holds the Godot project shell
(`project.godot`, `MistyInn.csproj`, `MistyInn.sln`, `icon.svg`), the documents and `tools/`; there is no game code yet
(the pre-SDD prototype was removed, D-034). Report the files, the SDKs (`dotnet --list-sdks`) and Godot version the
human reports, how the repository differs from section 3, and the smallest safe plan for step 1. List any decision
you need from the human. Change nothing.
**Covers:** AGT-01.
**Acceptance:** Report only.
**Demo:** None.

## Step 1: Solution structure and Core skeleton

**Read:** 3, 4, 5.1, 5.2, 20.
**Prompt:** Create `src/MistyInn.Core` and `tests/MistyInn.Core.Tests` with `.gdignore` files, `Directory.Build.props`,
`.editorconfig`, the `Compile Remove` lines and the `ProjectReference` (`STR-01`..`STR-07`), and add both projects to
`MistyInn.sln` with the configuration mapping (`STR-01`). Stop ignoring `export_presets.cfg` and ignore `builds/`
(`STR-08`, `STR-11`), and give the human the editor steps to create the Windows export preset: Project → Export →
Add → Windows Desktop, export path `builds/windows/MistyInn.exe`, and `data/*.json` in the non-resource files filter
(`DAT-02`). In Core, add `GameSession`,
`IRandomSource` with a seeded implementation, `GameCommand`, `CommandResult`, the domain event base type and its
bounded buffer, the cycle log, and entity IDs. Add tests for the RNG's determinism and the buffers' bounds. No scenes
yet.
**Covers:** STR-*, ARC-01, ARC-03, ARC-04, ARC-10..13, INV-01, INV-02, INV-05, TST-01..03.
**Acceptance:** The solution builds; the tests pass; the Godot editor opens the project without errors;
`export_presets.cfg` exists and is tracked by git.
**Demo:** `dotnet test`; open the project in Godot.

## Step 2: Board, cycle loop, isometric view

**Read:** 5.3, 5.4, 6, 7, 12.1, 15.1.
**Prompt:** In Core: the tile grid and zones from a hard-coded test layout, placement and path checks, A* movement,
the fixed-step tick, the Dusk/Mist/Dawn phases, arrivals with the seeded plane draw, creatures walking in and
wandering, dawn dissolving them, and `RunMistToDawn`. In Godot: create `scenes/Main.tscn` and set it as the main scene
in `project.godot`, the input actions registered in code (D-015), the isometric camera rig, `GameRoot` ticking the
session, placeholder views for units, creatures and the board, drag-and-drop placement of the innkeeper and starting
staff, and the speed controls.
**Covers:** SIM-01..04, CYC-01..05, CYC-10, CRE-01, CRE-02, CRE-04, CRE-06, PLN-03, PLN-04, BRD-01..05, CAM-10..13,
ARC-02, ARC-05, ARC-20, ARC-21, SCP-13, TST-04.
**Acceptance:** Tests for phases, placement, paths and determinism; one cycle can be played with placeholders.
**Demo:** Place the innkeeper and two staff, begin the mist, watch creatures walk in and dissolve at dawn.

## Step 3: Content data and validation

**Read:** 7, 8, 11, 12.2, 13.2, 17, 18.
**Prompt:** Add the data records, `IContentSource`, the Godot adapter, the loader and the validator, and the
`data/*.json` files for balance, inn, planes, creatures, staff (with upgrade trees and preset builds), techniques,
items and fixtures. Replace the hard-coded layout and content.
**Covers:** DAT-01..06, PLN-01, PLN-02, CRE-03, CRE-07, STF-01, STF-02, STF-07, STF-13, STF-14, ITM-01, FIX-01, RAT-01, SIM-04,
TST-07.
**Acceptance:** The real data loads and validates in the tests; every validator rule has a failing example, including
a tree with three classes.
**Demo:** Change a creature's name in `creatures.json` and see it in the game.

## Step 4: Agitation, techniques and the bestiary

**Read:** 9, 11.1, 12.2, 14.2.
**Prompt:** Add agitation (rates, thresholds, behaviours), appetites with preferred techniques, the non-fighting
techniques (Company on the floor, Food, Drink, Cards, Dice, Song, Dance, and Fists for `Brawl`), how staff choose
what to do (`STF-04`), fixtures with radius effects and the build menu with supplies, triggers, contagion, tributes at
dawn, the bestiary reveal rules and the forecast, and the cycle log entries for each cause. Add providers for the
`Order` and `Darkness` appetites, which nothing answers yet (D-060): propose them (techniques or fixtures) at the
start of the step and wait for the human's answer before writing them.
**Covers:** AGI-01..10, CRE-05, STF-02..05, FIX-02, FIX-04..06, CMP-03, BST-01..04, INV-12, TST-05.
**Acceptance:** Tests for every agitation rule and for the technique choice; a cycle shows creatures calming and
turning for visible reasons.
**Demo:** Put a Hollow Wood creature near a hearth and watch it turn; keep a Drowned creature at the bar until dawn.

## Step 5: Escalation, combat, Company and content filters

**Read:** 10, 11, 17.
**Prompt:** Add turning and the three escalation stages, combat with the Fists and Weapons techniques, banishing,
staff falling and the wound-or-die roll, the innkeeper's fall ending the run, fixture damage and integrity, Dawn
repairs, Company preferences and refusals, room sessions, interruptions (violence and attacked doors), hosting, and
both content filters, and extend the path check so that every room stays reachable on its own (`BRD-03`). Blood and
flavour text follow section 17.
**Covers:** ESC-01..10, STF-11, STF-12, CMP-01..06, BRD-03, BRD-06, BRD-07, RUN-03, RAT-02..09, SET-04 (logic), DAT-06,
TST-06, TST-10.
**Acceptance:** Tests for every escalation and combat rule, for refusals being honoured and for room sessions; a chain
reaction can happen and be read in the log.
**Demo:** Let one creature turn in a crowded room with no fighters; then defend with two fighters and a ward circle;
watch a companion take a guest to a room.

## Step 6: Shop, ranks, items and synergies

**Read:** 11.2, 12.2, 13.
**Prompt:** Add the shop (3 staff offers and 3 item offers), rank odds, pity and home-plane weights, reroll with escalating cost
and its reset, lock, hiring at a rank with the preset build, upgrading by buying an owned character with the upgrade
choice, selling, the bench, board slots, staff items with slots and storage, inn items and their display spots,
class tags, adjacency effects and synergies, and the shop, upgrade and synergy UI.
**Covers:** SHP-01..09, STF-06..10, STF-15, STF-16, ITM-01..07, FIX-03, SYN-01..03, UI-03, UI-08, ART-17, TST-09 (shop, staff and
item part).
**Acceptance:** Tests for odds, pity, reroll costs, the one-rank upgrade rule, the two-class tree limit, items and
synergy thresholds.
**Demo:** Hire a character at ★2, find them again in the shop, upgrade them to ★3 and branch into a second class; buy
an inn item and see it in the inn.

## Step 7: Roguelike layer

**Read:** 6.1, 14.
**Prompt:** Add run length and the Convergence, omens, Mist Essence, unlocks, and the title screen's Unlocks and
Bestiary pages.
**Covers:** RUN-01, RUN-02, RUN-04, OMN-01..03, MET-01..03, TST-09 (roguelike part).
**Acceptance:** Tests for draws, effects and the Essence formula; a seeded run replays identically.
**Demo:** Play to cycle 3 with a fixed seed twice and compare the forecasts, the shops and the first mist phase.

## Step 8: Saves, profile and settings

**Read:** 19.
**Prompt:** Add the run save when leaving Dawn, Continue, the profile with the bestiary, migrations, atomic writes,
quarantine of corrupt files, and the settings file.
**Covers:** SAV-01..06, SET-01..06, TST-08.
**Acceptance:** Tests for every `SAV` rule.
**Demo:** Play two cycles, quit, Continue; corrupt the save by hand and start the game.

## Step 9: UI pass

**Read:** 15.
**Prompt:** Build the title screen, the Dusk HUD and forecast, the mist HUD with bars and the event feed, unit and
creature panels, the Dawn summary with the cycle timeline, the run end screen, the pause menu and the shared theme.
**Covers:** UI-01..11, CAM-11, CAM-12.
**Acceptance:** Every screen works with the mouse and the keyboard; all text goes through `Tr()`; the timeline explains
each turn and death.
**Demo:** A full run from the title screen to the run end screen.

## Step 10: Art and audio pass

**Read:** 16, 17.
**Prompt:** Add the shading, the inn interior and lighting, creature and staff models, agitation visuals, blood VFX
and decals (with the reduced option), the mist, music, ambience and SFX. Every asset is licensed and listed in
`docs/ASSETS.md`. Placeholders are replaced without code changes.
**Covers:** ART-10..16, AUD-10..14, PLN-05, INV-10.
**Acceptance:** No unlicensed assets; the scenes keep their data bindings; content follows section 17.
**Demo:** Screenshots of Dusk, a mist phase with a turned creature, and Dawn.

## Step 11: Audit, export and traceability (read-only except fixes)

**Read:** the whole SDD.
**Prompt:** Audit the repository against the SDD. Produce `docs/TRACEABILITY.md` from the `Requirement` tags, list
uncovered requirements, check the invariants and section 17, and set up the Windows export preset. Fix only clear,
in-scope defects.
**Covers:** All.
**Acceptance:** Matrix produced; definition of done checked.
**Demo:** Run the exported `builds/windows/MistyInn.exe`.

---

# 22. Mandatory agent rules

- **AGT-01** Inspect the repository before changing it.
- **AGT-02** Work only on the requested step. Keep to the architecture (section 5) and the folder layout (section 3).
- **AGT-03** If the SDD is ambiguous, contradictory or silent on a material decision, **stop and ask**. Do not invent
  requirements. Use documented defaults for small things and record them in `docs/DECISIONS.md`.
- **AGT-04** Add or update NUnit tests for every behaviour change in Core, tagged with requirement IDs.
- **AGT-05** Do not run builds, tests, the Godot editor, headless Godot or git commands. The human runs them. List the
  exact commands and mark every result **NOT RUN**. Never claim that something builds, passes or runs unless the
  human reported it.
- **AGT-06** Never leave the solution uncompilable at the end of a step.
- **AGT-07** Edit `.tscn` and `.tres` files by hand only for small, clear changes (adding a node, setting a property).
  For larger scene work, describe the editor steps for the human, or build the node tree from a script that the human
  runs once in the editor. Never change or delete a resource's `uid`.
- **AGT-08** Record every deviation from the SDD, and every SDD change you recommend, in the step report. Edit
  `docs/SDD.md` only when the human explicitly asks. When you do, bump the version, update the change log, first save
  the previous version as `docs/SDD_v<old>.md`, and regenerate Appendix D.
- **AGT-09** Update `docs/PROGRESS.md` and `docs/DECISIONS.md` in every step.
- **AGT-10** Never type package versions by hand (`STR-09`).
- **AGT-11** Never add content that goes past section 17. When unsure, stop and ask.
- **AGT-12** Never add a third-party asset without its licence in `docs/ASSETS.md` (`INV-10`). Do not download assets
  yourself; give the human the link and the licence to check.
- **AGT-13** Keep `README.md` current when controls, run instructions or the folder layout change.
- **AGT-14** Balance changes go in `data/`, with a decision entry (section 0.1). Never change balance to make a test
  pass; tests use their own fixed content.

---

# 23. Definition of done and known limitations

## 23.1 Definition of done (prototype)

- [ ] The solution builds; every Core test passes; the headless smoke run prints no errors.
- [ ] Every requirement has a tagged test, or a Manual or Exempt status with a reason in `docs/TRACEABILITY.md`.
- [ ] A full run can be played from the title screen to a win or a loss.
- [ ] A seeded run replays identically, including every mist phase (`INV-02`).
- [ ] Every turn, fall and death in a mist phase can be explained from the cycle timeline (`INV-12`).
- [ ] All content comes from `data/` and validates.
- [ ] Saves survive quitting, corruption and an older version; the bestiary survives every run.
- [ ] Both content filters work everywhere.
- [ ] Every asset is listed with its licence.
- [ ] A Windows export runs on its own.

## 23.2 Known prototype limitations

- Saves only between cycles.
- One floor; no upstairs rooms.
- No gamepad support or key rebinding UI.
- English only (with the hooks for translation).
- Node scripts have no automated tests (`TST-11`).
- The balance is untested beyond the developer's play sessions.

---

# 24. Backlog and extension seams

Ideas that are **not** in scope for the prototype. Adding one is an SDD change.

- A second floor with guest rooms (`Rest` appetite) and stairs as a choke point.
- A technique or item that brings a turned creature back to Menacing.
- More classes (for example a ward-keeping class, or a class that repairs fixtures during the mist).
- Named regular creatures with their own story across runs.
- Staff relationships (synergies between specific pairs).
- Gamepad support and key rebinding.
- Steam release: achievements, cloud saves, store page with the content note (`RAT-10`).
- Seams: `IContentSource` (mod folders), `ISaveStore` (cloud saves), the closed kind sets (`DAT-03`), new fixtures and
  staff through data, new planes through data and a gate colour.

---

# Appendix A: Step report template

The agent ends every step with:

```markdown
## Step <n> report (SDD v<version>)
**Requirements addressed:** <IDs>
**Files added/changed:** <list>
**Commands run and results:** <command: result, or "NOT RUN">
**Commands for the human to run:** <exact commands>
**Tests added (with Requirement tags):** <list>
**Package versions added (source: NuGet):** <list, or "none">
**Scenes and resources changed:** <list, with editor steps the human must do>
**Assets added (licence in docs/ASSETS.md):** <list, or "none">
**Deviations from the SDD:** <none, or a list with reasons>
**Decisions made (also in docs/DECISIONS.md):** <list>
**Ambiguities / questions for the human:** <list>
**Recommended SDD changes:** <list, by ID>
**Known limitations after this step:** <list>
**Demo script:** <what to run and what should be seen>
```

# Appendix B: PROGRESS and DECISIONS templates

```markdown
# PROGRESS
| Step | SDD version | Date | Status | Build | Tests | Smoke run | Notes |
|---|---|---|---|---|---|---|---|

# DECISIONS
| ID | Title | Status | Date | Requirements |
|---|---|---|---|---|

## D-001 <title>
- **Status:** Proposed | Accepted | Superseded by D-xxx
- **Date / step:**
- **Context:**
- **Decision:**
- **Alternatives considered:**
- **Requirement IDs affected:**
```

# Appendix C: Traceability matrix

The audit step writes `docs/TRACEABILITY.md` from the IDs in Appendix D and the `Requirement` properties on the tests.
Every requirement gets one status: **Covered** (at least one tagged test), **Manual** (checked by a demo script, for
example a scene or art requirement), **Exempt** (no sensible check; the reason is given), **Removed** (removed in an
earlier SDD version) or **Not covered** (reported as a finding).

```markdown
| Requirement | Short description | Tests / check | Status |
|---|---|---|---|
| AGI-09 | Contagion: turning raises nearby agitation | AgitationTests.ContagionWithinFourTiles | Covered |
```

# Appendix D: Requirement index

Every requirement ID in this document, in order. Generated from the text by `tools/gen-requirement-index.py`;
regenerate it whenever the SDD changes.

<!-- REQUIREMENT-INDEX:START -->
249 requirements.

| ID | Summary | Section |
|---|---|---|
| SCP-01 | An inn board viewed from an isometric camera, on which the player places staff and... | 1.3 |
| SCP-02 | A cycle of Dusk (setup), Mist (automatic simulation) and Dawn (aftermath) phases... | 1.3 |
| SCP-03 | A roguelike run: random planes, creatures, omens and shop offers; it ends in a win or... | 1.3 |
| SCP-04 | Hostile creatures from several planes, each with hidden appetites, triggers and an... | 1.3 |
| SCP-05 | An automatic escalation and combat simulation when a creature turns (section 10) | 1.3 |
| SCP-06 | Unique staff characters with classes and techniques, five ranks and an upgrade tree,... | 1.3 |
| SCP-07 | Fixtures on the inn's grid, bought from a fixed build menu, whose effects depend on... | 1.3 |
| SCP-08 | A bestiary that reveals creature knowledge as it is observed and keeps it across runs... | 1.3 |
| SCP-09 | Dark stylised 3D presentation, with the mist as a menace (section 16) | 1.3 |
| SCP-10 | Saving and loading a run between cycles, and a profile that keeps meta-progression and... | 1.3 |
| SCP-11 | *(Removed in v0.2: Cozy mode | 1.3 |
| SCP-12 | Content kept as data files that can be edited without code changes | 1.3 |
| SCP-13 | Pause and speed controls (pause, 1×, 2×, 4×) during the mist phase; no other player... | 1.3 |
| SCP-14 | Staff items carried by staff, and inn items that change rules across the whole run and... | 1.3 |
| SCP-20 | Multiplayer, online features, leaderboards or accounts | 1.5 |
| SCP-21 | Consoles, mobile or controller-first design | 1.5 |
| SCP-22 | Explicit sexual content in any form (section 17) | 1.5 |
| SCP-23 | Procedurally generated inn layouts | 1.5 |
| SCP-24 | Voice acting, cutscenes and branching story campaigns | 1.5 |
| SCP-25 | Direct control of any unit, during setup or the mist phase | 1.5 |
| SCP-26 | Gore beyond the ceiling in section 17 (no dismemberment) | 1.5 |
| SCP-27 | Real money of any kind: no microtransactions, paid currencies or paid random draws | 1.5 |
| INV-01 | `MistyInn.Core` shall not reference Godot (`GodotSharp`, `Godot.NET.Sdk`) or any... | 2 |
| INV-02 | The simulation is deterministic: the same seed, content and command sequence produce... | 2 |
| INV-03 | Game time advances only through `GameSession.Tick(TimeSpan)`, with a fixed step (`SIM-02`) | 2 |
| INV-04 | Content (planes, creatures, staff and their upgrade trees, classes, techniques, items,... | 2 |
| INV-05 | No unbounded growth: creatures and units on the board, queues, logs and histories have... | 2 |
| INV-06 | Every character is an adult | 2 |
| INV-07 | A missing, corrupt or older save never crashes the game and is never silently... | 2 |
| INV-08 | The solution compiles and every test passes at the end of each step | 2 |
| INV-09 | Godot node scripts are `partial` classes named after their file | 2 |
| INV-10 | Every third-party asset (model, texture, font, sound, music, shader) has a known... | 2 |
| INV-11 | No secrets, personal data, telemetry or network calls in the game | 2 |
| INV-12 | Every outcome of the mist phase is explainable: Core records each agitation change,... | 2 |
| STR-01 | The Godot project lives at the repository root | 3 |
| STR-02 | `src/MistyInn.Core` is a plain class library (`Microsoft.NET.Sdk`, `net8.0`) with no... | 3 |
| STR-03 | `tests/MistyInn.Core.Tests` is an NUnit project that references only Core | 3 |
| STR-04 | The Godot SDK compiles every `*.cs` file under the project folder by default, so... | 3 |
| STR-05 | References point one way: Game → Core, Tests → Core | 3 |
| STR-06 | `Directory.Build.props` sets `Nullable` to `enable`, `LangVersion` to `latest`, and... | 3 |
| STR-07 | The test project sets `<RollForward>Major</RollForward>` so it runs on whichever .NET... | 3 |
| STR-08 | Committed: `project.godot`, `*.tscn`, `*.tres`, `*.import`, `*.uid`,... | 3 |
| STR-09 | Package versions are never typed by hand | 3 |
| STR-10 | Folder and namespace mirror each other: `MistyInn.Core.Creatures` is... | 3 |
| STR-11 | Windows builds export to `builds/windows/`, which is ignored by git | 3 |
| ARC-01 | `GameSession` is the single entry point into Core | 5.1 |
| ARC-02 | One `GameRoot` node (the script on `Main.tscn`) owns the only `GameSession` | 5.1 |
| ARC-03 | Core state is changed only by `Tick` and `Execute` | 5.1 |
| ARC-04 | Each Core system is a class with explicit dependencies passed to its constructor and... | 5.1 |
| ARC-05 | A whole mist phase can be simulated in Core without the engine, faster than real time... | 5.1 |
| ARC-10 | Commands are immutable records, for example `BuyOffer(offerIndex)`,... | 5.2 |
| ARC-11 | `Execute` returns a `CommandResult` (success, or a reason code such as `WrongPhase`,... | 5.2 |
| ARC-12 | Core raises domain events (`PhaseChanged`, `CreatureArrived`, `ServiceStarted`,... | 5.2 |
| ARC-13 | Every entity has a stable integer ID issued by Core (`UnitId`, `CreatureId`, `FixtureId`) | 5.2 |
| SIM-01 | Core measures time as `TimeSpan` game time since the start of the mist phase | 5.3 |
| SIM-02 | Core runs fixed steps of 1/30 s | 5.3 |
| SIM-03 | Core owns positions | 5.3 |
| SIM-04 | The inn's grid, zones and mist gates are defined in data (`data/inn.json`) | 5.3 |
| ARC-20 | Everything that appears more than once (units, creatures, fixtures) is its own... | 5.4 |
| ARC-21 | Node references use `[Export]` fields set in the editor, or `%UniqueName` lookups... | 5.4 |
| RUN-01 | A run starts from a seed (random unless the player enters one) and the profile's unlocks | 6.1 |
| RUN-02 | A run is won by surviving the last cycle (`RunLength`, default 10) | 6.1 |
| RUN-03 | A run is lost at once when the innkeeper falls (`ESC-06`), or at the end of a mist... | 6.1 |
| RUN-04 | When a run ends, the player earns Mist Essence (`MET-02`), keeps everything the... | 6.1 |
| CYC-01 | Dusk has no timer | 6.2 |
| CYC-02 | Mist lasts `MistDuration` (default 120 s of game time) | 6.2 |
| CYC-03 | Arrivals stop 20 s before dawn | 6.2 |
| CYC-04 | Dawn shows the cycle summary: creatures that stayed occupied and their tributes,... | 6.2 |
| CYC-05 | The cycle log of the mist phase (`INV-12`) can be replayed at Dawn as a timeline (`UI-07`) | 6.2 |
| CYC-10 | Esc opens the pause menu in every phase and stops ticking (`INV-03`): Resume,... | 6.3 |
| PLN-01 | A plane is a data entry (`data/planes.json`) with an ID, a name, a mist colour, an... | 7 |
| PLN-02 | The prototype ships five planes | 7 |
| PLN-03 | At Dusk, Core draws the cycle's converging planes from the unlocked set with the... | 7 |
| PLN-04 | Each converging plane opens one mist gate at the edge of the clearing, tinted in its... | 7 |
| PLN-05 | The mist is the main visual motif: dense fog around the clearing that hides the edge... | 7 |
| CRE-01 | During the mist phase, creatures arrive at a seeded random interval around the cycle's... | 8 |
| CRE-02 | A creature does not arrive if the board is full (`INV-05`); the next arrival is tried... | 8 |
| CRE-03 | A creature (`data/creatures.json`) has an ID, a plane, a name, a size (1 or 2 tiles),... | 8 |
| CRE-04 | On arrival, a creature walks in through the front door and looks for something that... | 8 |
| CRE-05 | At dawn, a creature that has not turned leaves a tribute (silver, by its tribute... | 8 |
| CRE-06 | Creatures act only through the simulation's rules; the player never commands them... | 8 |
| CRE-07 | Every creature that can take part in the Company service is sapient and adult... | 8 |
| AGI-01 | Each creature has agitation from 0 to 100, starting at its arrival value (usually 10-30) | 9.1 |
| AGI-02 | Agitation rises at the creature's base rate every second | 9.1 |
| AGI-03 | Thresholds: | 9.1 |
| AGI-04 | A turned creature stays turned until it is banished (reduced to 0 health) or dawn breaks | 9.1 |
| AGI-05 | An appetite is something a creature wants: `Drink`, `Food`, `Company`, `Gambling`,... | 9.2 |
| AGI-06 | A creature is occupied while it is being served one of its appetites, by a staff... | 9.2 |
| AGI-07 | A provider can serve a limited number of creatures at once: each technique states its... | 9.2 |
| AGI-08 | A trigger is a condition that raises a creature's agitation at once or speeds it up... | 9.3 |
| AGI-09 | Contagion: when a creature turns, every creature within 4 tiles that sees it gains +15... | 9.3 |
| AGI-10 | Triggers and appetites are hidden until the bestiary reveals them (`BST-02`), but... | 9.3 |
| ESC-01 | A turned creature escalates through three stages, in this order: | 10.1 |
| ESC-02 | A creature's escalation profile may change the timings or skip a stage (for example... | 10.1 |
| ESC-03 | In every stage, a turned creature that is attacked by a fighter turns on that fighter... | 10.1 |
| ESC-04 | Calm creatures attacked by a turned creature lose health like anyone else; if they... | 10.1 |
| ESC-05 | Combat is fully automatic and deterministic | 10.2 |
| ESC-06 | The innkeeper is a unit with low attack and moderate health | 10.2 |
| ESC-07 | Staff with a Fighter technique (Fists or Weapons, `STF-02`) engage any turned creature... | 10.2 |
| ESC-08 | A turned creature reduced to 0 health is banished: it dissolves into mist, leaves no... | 10.2 |
| ESC-09 | A staff member reduced to 0 health falls | 10.2 |
| ESC-10 | Fixtures have health | 10.2 |
| STF-01 | Staff are unique named characters (`data/staff.json`), each with an ID, a name, an... | 11.1 |
| STF-02 | Classes and their techniques (closed sets, `DAT-03`): | 11.1 |
| STF-03 | A character has a class if it knows at least one technique of that class | 11.1 |
| STF-04 | What staff do in the mist phase. Each time a staff member is free, it chooses in this... | 11.1 |
| STF-05 | Staff on the bench do nothing during the mist phase and cannot be hurt | 11.1 |
| STF-06 | Characters have five ranks, ★1 to ★5 | 11.2 |
| STF-07 | Each character has an upgrade tree: for each of ranks ★2 to ★5, two or three options,... | 11.2 |
| STF-08 | Upgrading: buying a shop offer of a character the player already owns raises the owned... | 11.2 |
| STF-09 | Selling a character returns half the silver spent on it, rounded down | 11.2 |
| STF-10 | Hiring a character the player does not own gives that character at the offer's rank... | 11.2 |
| STF-17 | Mastery makes a known technique stronger by the mastery values in its data (for... | 11.2 |
| STF-11 | A fallen staff member is wounded or dead at Dawn (`ESC-09`) | 11.3 |
| STF-12 | At Dawn, wounded staff recover to full health; staff who fell and survived the roll... | 11.3 |
| STF-13 | Every staff member is an adult (`RAT-01`); the data has `"adult": true` (`DAT-04`) | 11.3 |
| STF-14 | The prototype roster is 16 characters, 4 per main class | 11.4 |
| STF-15 | Ancestry is a hidden link between a character and one plane, which may be a locked plane | 11.4 |
| STF-16 | Ancestry is revealed by observation: the first time the character soothes or fights a... | 11.4 |
| CMP-01 | The Company technique belongs to the Companion/Server class | 11.5 |
| CMP-02 | Company is given only to sapient, adult creatures (`CRE-07`) that crave or like... | 11.5 |
| CMP-03 | On the floor: flirting and conversation, shown in frame at a table or in the lounge,... | 11.5 |
| CMP-04 | In a room: when a creature has had floor company for at least 8 s, still craves... | 11.5 |
| CMP-05 | Interruptions: floor company ends at once when violence happens within 4 tiles | 11.5 |
| CMP-06 | With the suggestive content filter on (`SET-04`), Company becomes Hosting:... | 11.5 |
| BRD-01 | The inn is a grid of 1 m tiles (prototype: 14 × 10) divided into zones: Entrance,... | 12.1 |
| BRD-02 | During Dusk, the player places units and fixtures on free floor tiles by drag and drop | 12.1 |
| BRD-03 | Placements must leave a path from the front door to every zone, and to every room on... | 12.1 |
| BRD-04 | Placements stay from cycle to cycle until the player moves them; destroyed fixtures... | 12.1 |
| BRD-05 | The front door is where every creature enters | 12.1 |
| BRD-06 | Integrity is the inn's overall health (100 at the start of a run) | 12.1 |
| BRD-07 | At Dawn, the player may pay silver to repair fixtures and integrity | 12.1 |
| FIX-01 | A fixture (`data/fixtures.json`) has an ID, a name, a footprint (tiles), the zone it... | 12.2 |
| FIX-02 | Prototype fixtures: | 12.2 |
| FIX-03 | Adjacency effects apply to fixtures and units next to each other (8 neighbours), for... | 12.2 |
| FIX-04 | Fixture effects are applied by Core systems reading the board's modifiers, never by... | 12.2 |
| FIX-05 | The build menu is a fixed catalogue: every unlocked fixture, always available at a... | 12.2 |
| FIX-06 | Supplies (`Drink`, `Food`) are bought in bundles from the build menu at Dusk and used... | 12.2 |
| SHP-01 | Silver is the run's only currency | 13.1 |
| SHP-02 | Each Dusk the shop shows 3 staff offers and 3 item offers, drawn with the run's RNG | 13.1 |
| SHP-03 | Rank odds grow over the run (tuning values in `data/balance.json`): | 13.1 |
| SHP-04 | Draw weights: each character's chance to be drawn is multiplied by: - Pity: ×1.5 if... | 13.1 |
| SHP-05 | Reroll redraws every offer not yet bought | 13.1 |
| SHP-06 | Lock keeps the current offers for the next Dusk instead of a new draw | 13.1 |
| SHP-07 | Any character can be offered: one never hired, one who died, or one the player owns... | 13.1 |
| SHP-08 | Prices: a staff offer costs the character's base cost + 2 per rank above ★1 | 13.1 |
| SHP-09 | Unspent silver carries over between cycles, but never between runs | 13.1 |
| ITM-01 | Items (`data/items.json`) have an ID, a name, a kind (staff item or inn item), a... | 13.2 |
| ITM-02 | Staff items are carried by a staff member, with two slots each | 13.2 |
| ITM-03 | Class tags on a staff item count the carrier as having that class for synergy... | 13.2 |
| ITM-04 | Inn items apply across the whole run from the moment they are bought: to staff,... | 13.2 |
| ITM-05 | Cursed items have a strong upside and a real downside | 13.2 |
| ITM-06 | Prototype items, for example: | 13.2 |
| ITM-07 | Items can be sold back for half their price, rounded down | 13.2 |
| SYN-01 | A synergy is a bonus for having enough distinct characters on the board (the bench... | 13.3 |
| SYN-02 | Prototype synergies, for example: Companion/Server (2/4/6): +10/+25/+50 % soothe for... | 13.3 |
| SYN-03 | Synergy effects use the same effect kinds as items and fixtures (`DAT-03`) | 13.3 |
| OMN-01 | Each Dusk from cycle 2, Core may draw one omen (`data/omens.json`), by weight, with a... | 14.1 |
| OMN-02 | Prototype omens: Blood Moon (all creatures arrive at +20 agitation), Thick Mist... | 14.1 |
| OMN-03 | An omen's effect uses the same effect kinds (`DAT-03`) and lasts one cycle | 14.1 |
| BST-01 | The bestiary (in the profile) has an entry for every creature, with four reveal... | 14.2 |
| BST-02 | Reveal progress comes from observation during the mist phase: seeing a creature... | 14.2 |
| BST-03 | The Dusk forecast shows, for each creature the converging planes may send, what the... | 14.2 |
| BST-04 | Bestiary knowledge is kept in the profile across runs, win or lose (`MET-01`) | 14.2 |
| MET-01 | The profile (`SAV-02`) keeps Mist Essence, unlocks, the bestiary, revealed ancestries... | 14.3 |
| MET-02 | Mist Essence at the end of a run = cycles survived × 2 + creatures kept occupied until... | 14.3 |
| MET-03 | Essence buys unlocks on the title screen: new planes, new characters in the shop pool,... | 14.3 |
| CAM-10 | An isometric-style camera (orthographic or a narrow-FOV perspective, about 45° down)... | 15.1 |
| CAM-11 | The roof and the wall nearest the camera fade out so the board is always visible | 15.1 |
| CAM-12 | During the mist phase, clicking a unit or creature selects it and shows its panel; F... | 15.1 |
| CAM-13 | Dragging from the shop, the bench or the board, and dropping on a tile, places a unit... | 15.1 |
| UI-01 | Title screen: New Run (with an optional seed field), Continue (when a run save... | 15.2 |
| UI-02 | Dusk HUD: cycle number, silver, integrity, the forecast (planes, omen, known... | 15.2 |
| UI-03 | Dusk shop panel: the staff offers (each showing the character, rank, classes and... | 15.2 |
| UI-04 | Mist HUD: time to dawn, speed controls (pause, 1×, 2×, 4×), integrity, the innkeeper's... | 15.2 |
| UI-05 | Over each creature: an agitation bar coloured by state (Calm, Restless, Menacing,... | 15.2 |
| UI-06 | Unit and creature panels: for staff, rank, classes, origin, ancestry (shown as "?"... | 15.2 |
| UI-07 | Dawn summary: tributes, turned creatures, banished creatures, fallen and dead staff,... | 15.2 |
| UI-08 | Upgrade choice: when an upgrade offer is bought, a dialog shows the character's tree... | 15.2 |
| UI-09 | UI text uses `Tr()` keys | 15.2 |
| UI-10 | Run end screen: win or loss and its cause, cycles survived, Mist Essence earned,... | 15.2 |
| UI-11 | One theme (`InnTheme.tres`): dark wood and parchment, candlelit accents, a readable... | 15.2 |
| ART-10 | Dark stylised: normal proportions with slightly exaggerated silhouettes, simple forms,... | 16.1 |
| ART-11 | Palette: muted, desaturated colours | 16.1 |
| ART-12 | Creatures read as dangerous through silhouette, movement and glow rather than gore | 16.1 |
| ART-13 | The mist (`PLN-05`): volumetric fog or layered fog cards around the clearing and at... | 16.1 |
| ART-14 | Violence follows section 17: hit flashes, knockdowns, blood splashes and decals,... | 16.1 |
| ART-15 | Characters who know Company use adult styling: attractive, confident, dressed in... | 16.1 |
| ART-17 | Inn items have a visible model placed at their display spot in the inn (`ITM-04`) | 16.1 |
| ART-16 | Placeholders: until step 10, units are capsules and creatures are coloured shapes with... | 16.1 |
| AUD-10 | Audio buses: Master, Music, SFX and Ambience, each with a volume setting (`SET-02`) | 16.2 |
| AUD-11 | Music: a quiet, uneasy theme at Dusk; a tense, building theme in the mist that rises... | 16.2 |
| AUD-12 | Ambience: wind and distant sounds from each open plane outside; a creaking, crackling... | 16.2 |
| AUD-13 | SFX: footsteps, pouring, cooking, music from the stage, agitation cues (growls... | 16.2 |
| AUD-14 | Every audio file is licensed and listed in `docs/ASSETS.md` (`INV-10`) | 16.2 |
| RAT-01 | Adults only. Every character, including the innkeeper, staff, companions and every... | 17 |
| RAT-02 | Suggestive content allowed: flirting, innuendo, romance, teasing dialogue, alluring... | 17 |
| RAT-03 | Suggestive content not allowed: explicit sexual acts, depicted or described; nudity;... | 17 |
| RAT-04 | Never allowed, by any setting, mod hook or decision: sexual violence or coercion;... | 17 |
| RAT-05 | Violence allowed (moderate blood): attacks, injuries shown through blood splashes and... | 17 |
| RAT-06 | Violence not allowed: dismemberment, decapitation, exposed organs, lingering close-ups... | 17 |
| RAT-07 | Companions are respected adult professionals who choose their work | 17 |
| RAT-08 | Flavour text for Company is one line, dark-romantic and cheeky rather than crude, for... | 17 |
| RAT-09 | Content filters (`SET-04`): Blood (full or reduced: no blood decals, hits flash... | 17 |
| RAT-10 | When the game is published, the store page states the content honestly: "Moderate... | 17 |
| DAT-01 | Content files live in `data/` as JSON: `balance.json`, `inn.json`, `planes.json`,... | 18 |
| DAT-02 | Core defines the schema as C# records and loads the files through `IContentSource`,... | 18 |
| DAT-03 | Effects, appetites, triggers and roles are closed sets of kinds with parameters, for... | 18 |
| DAT-04 | The content validator runs at load time and in the tests | 18 |
| DAT-05 | IDs are lowercase kebab-case (`drowned-deep`, `pale-duchess`) and never change once a... | 18 |
| DAT-06 | Flavour text has a `filterSafe` flag and a `bloodSafe` flag | 18 |
| SAV-01 | The run is saved automatically when the player leaves Dawn (`CYC-04`), to... | 19 |
| SAV-02 | The profile (`user://saves/profile.json`) holds Mist Essence, unlocks, the bestiary,... | 19 |
| SAV-03 | Saving is only between cycles | 19 |
| SAV-04 | Saves have a `schemaVersion` | 19 |
| SAV-05 | Writes are atomic: write `*.tmp`, then replace | 19 |
| SAV-06 | A corrupt save is moved to `*.corrupt-<n>.json`, the player is told, and the game goes... | 19 |
| SET-01 | Settings are in `user://settings.cfg` (Godot `ConfigFile`), outside the saves | 19 |
| SET-02 | Audio: volume per bus (`AUD-10`) | 19 |
| SET-03 | Controls: camera pan speed, edge panning on or off, zoom speed | 19 |
| SET-04 | Content filters: Blood (full or reduced) and Suggestive content (on or off) (`RAT-09`) | 19 |
| SET-05 | Display: window mode (windowed, borderless, fullscreen), V-Sync, resolution scale | 19 |
| SET-06 | Gameplay: default mist speed, and whether the game pauses automatically when the... | 19 |
| TST-01 | Core tests are deterministic: a fixed seed through a test `IRandomSource`, time only... | 20 |
| TST-02 | NUnit | 20 |
| TST-03 | Every behaviour change in Core comes with tests in the same step (`AGT-04`); no test... | 20 |
| TST-04 | Cycle and simulation: phase transitions, commands refused in the wrong phase, arrival... | 20 |
| TST-05 | Agitation and service: rise rates, thresholds and their behaviours, appetite matching... | 20 |
| TST-06 | Escalation and combat: stage order and timers, profile overrides, guard targeting... | 20 |
| TST-07 | Board and content: placement rules, path checks, adjacency, the board coming from... | 20 |
| TST-08 | Saves: round trip, atomic replace, `.bak` kept, corrupt file quarantined, older... | 20 |
| TST-09 | Shop, staff and roguelike: rank odds per cycle, pity and home-plane weights (including... | 20 |
| TST-10 | Company: refusals always honoured, preferences, sapient-only Company, floor company,... | 20 |
| TST-11 | Presentation (Godot nodes) has no automated tests in the prototype | 20 |
| TST-12 | A headless smoke run is part of every step's commands for the human: `<Godot... | 20 |
| AGT-01 | Inspect the repository before changing it | 22 |
| AGT-02 | Work only on the requested step | 22 |
| AGT-03 | If the SDD is ambiguous, contradictory or silent on a material decision, stop and ask | 22 |
| AGT-04 | Add or update NUnit tests for every behaviour change in Core, tagged with requirement IDs | 22 |
| AGT-05 | Do not run builds, tests, the Godot editor, headless Godot or git commands | 22 |
| AGT-06 | Never leave the solution uncompilable at the end of a step | 22 |
| AGT-07 | Edit `.tscn` and `.tres` files by hand only for small, clear changes (adding a node,... | 22 |
| AGT-08 | Record every deviation from the SDD, and every SDD change you recommend, in the step... | 22 |
| AGT-09 | Update `docs/PROGRESS.md` and `docs/DECISIONS.md` in every step | 22 |
| AGT-10 | Never type package versions by hand (`STR-09`) | 22 |
| AGT-11 | Never add content that goes past section 17 | 22 |
| AGT-12 | Never add a third-party asset without its licence in `docs/ASSETS.md` (`INV-10`) | 22 |
| AGT-13 | Keep `README.md` current when controls, run instructions or the folder layout change | 22 |
| AGT-14 | Balance changes go in `data/`, with a decision entry (section 0.1) | 22 |

**By prefix:** `SCP` (22), `INV` (12), `STR` (11), `ARC` (11), `SIM` (4), `RUN` (4), `CYC` (6), `PLN` (5), `CRE` (7), `AGI` (10), `ESC` (10), `STF` (17), `CMP` (6), `BRD` (7), `FIX` (6), `SHP` (9), `ITM` (7), `SYN` (3), `OMN` (3), `BST` (4), `MET` (3), `CAM` (4), `UI` (11), `ART` (8), `AUD` (5), `RAT` (10), `DAT` (6), `SAV` (6), `SET` (6), `TST` (12), `AGT` (14)
<!-- REQUIREMENT-INDEX:END -->

# Appendix E: Glossary

| Term | Meaning |
|---|---|
| **Agitation** | A creature's 0-100 meter; at 100 it turns |
| **Ancestry** | A character's hidden link to a plane; weights the shop at once, and helps once revealed |
| **Appetite** | Something a creature wants (`Drink`, `Music`, `Company`...); serving it keeps the creature occupied |
| **Banished** | A turned creature reduced to 0 health; it dissolves with no tribute |
| **Bench** | Where bought staff wait; they do nothing in the mist phase |
| **Build menu** | The fixed catalogue of fixtures and supplies, separate from the random shop |
| **Bestiary** | Knowledge about each creature, revealed by observation and kept across runs |
| **Class** | Companion/Server, Gambler, Fighter or Entertainer; a character has a class when it knows one of its techniques |
| **Companion** | An adult character who knows the Company technique and chooses whom to entertain |
| **Contagion** | Agitation spreading to creatures who see another turn |
| **Convergence** | The last cycle of a run: every plane opens at once |
| **Core** | `MistyInn.Core`, the engine-free library with all game rules |
| **Cycle** | Dusk (setup), Mist (automatic) and Dawn (aftermath) |
| **Cycle log** | The record of every cause and effect in a mist phase, shown as the Dawn timeline |
| **Escalation** | A turned creature's stages: Wreck, Prey, Hunt |
| **Fixture** | Furniture, a station or a ward placed on the board |
| **GameRoot** | The node on `Main.tscn` that owns the `GameSession` |
| **Home plane** | A character's plane-born origin or ancestry; it weights the shop when that plane converges |
| **Host** | A companion with the suggestive content filter on |
| **Inn item** | A run-long modifier bought in the shop and shown in the inn; replaces relics |
| **Integrity** | The inn's overall health; the run is lost at 0 |
| **Mist Essence** | The currency kept across runs, spent on unlocks |
| **Mist gate** | Where one plane's creatures walk out of the mist |
| **Omen** | A one-cycle modifier drawn at Dusk |
| **Pity weighting** | Owned characters are more likely to be offered, so upgrades are not pure luck |
| **Plane** | A slice of reality whose mist reaches the inn |
| **Rank** | ★1 to ★5; each rank above ★1 adds one technique |
| **Room session** | Company taken to a back room: off-screen, more soothing, and the companion is out of play |
| **Silver** | The run's currency |
| **Staff item** | An item a staff member carries, in one of two slots |
| **Synergy** | A bonus for having enough characters of one class or origin on the board |
| **Technique** | A way of dealing with a guest (Song, Dice, Fists...), belonging to a class |
| **Tribute** | What a creature that stayed calm leaves at dawn |
| **Turned** | A creature at 100 agitation: hostile until banished or dawn |
| **Upgrade tree** | A character's options for ranks ★2 to ★5, spanning at most two classes |
