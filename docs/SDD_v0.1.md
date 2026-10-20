# Software Design Document (SDD)
## Misty Inn

| | |
|---|---|
| **Version** | 0.1 (draft, awaiting approval) |
| **Supersedes** | none |
| **Audience** | LLM-based coding agents and the human developer |
| **Genre** | Cute third-person roguelike management game |
| **Engine** | Godot 4.7.2 (.NET build) |
| **Language** | C# (`net8.0`, `Godot.NET.Sdk/4.7.2`) |
| **Platform** | Windows desktop (keyboard and mouse) |
| **Scope** | Hobby prototype |
| **Content ceiling** | Suggestive (Mature); see section 17 |
| **Tests** | NUnit, on the engine-free Core library |

---

## Contents

- [0. Document control](#0-document-control)
- [1. Vision, scope and non-goals](#1-vision-scope-and-non-goals)
- [2. Invariants](#2-invariants)
- [3. Repository structure and build hygiene](#3-repository-structure-and-build-hygiene)
- [4. Technology defaults](#4-technology-defaults)
- [5. Architecture](#5-architecture)
- [6. Core loop: runs and days](#6-core-loop-runs-and-days)
- [7. Planes and the mist](#7-planes-and-the-mist)
- [8. Guests](#8-guests)
- [9. Services, stations and companions](#9-services-stations-and-companions)
- [10. Economy, supplies and upgrades](#10-economy-supplies-and-upgrades)
- [11. Satisfaction and reputation](#11-satisfaction-and-reputation)
- [12. Roguelike systems](#12-roguelike-systems)
- [13. Player, camera and interaction](#13-player-camera-and-interaction)
- [14. User interface](#14-user-interface)
- [15. Art direction](#15-art-direction)
- [16. Audio](#16-audio)
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

This SDD is applied **one step at a time** (section 21). After each step the human builds the code, runs the tests and the
game, reads the agent's step report (Appendix A), and decides whether the SDD needs to change **before** the next step
starts.

- Every requirement has an ID (for example `GST-04`). Prompts, tests, step reports, decisions and change-log entries
  refer to requirements by ID, never by paraphrase. Appendix D lists every ID.
- Tests are tagged with the requirements they verify (`TST-03`). The audit step produces a traceability matrix in
  `docs/TRACEABILITY.md` (Appendix C).
- Every change to the SDD bumps its version. Each prompt states the SDD version it was written against. If an agent
  finds the SDD ambiguous, contradictory or silent on a material decision, it **stops and asks** (`AGT-03`).
- Wording: **shall** = mandatory; **should** = a default that may be changed with a recorded decision; **may** =
  optional.
- Balance numbers (section 6.4 and the data files) are **tuning values**, not requirements. They may change without an
  SDD change, but each change is recorded in `docs/DECISIONS.md`.

## 0.2 Change log

| Version | Change | Reason |
|---|---|---|
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

---

# 1. Vision, scope and non-goals

## 1.1 Pitch

Misty Inn stands at a **nexus where planes of reality converge**. The planes show up as coloured mist. Each morning
the mist shifts, and travellers from other slices of reality walk out of it: clockwork couriers, moss-covered
wanderers, ember-born smiths, sailors from a starlit sea. They come for what the inn offers: a drink, a meal, a bed
and good company.

You are the innkeeper. Over one **run** you keep the inn going one **day** at a time. You serve guests, keep the
supplies stocked, look after the resident companions, and pick a boon each night, until the inn either outlasts the
Convergence or fades into the mist.

## 1.2 Design pillars

1. **Cozy first.** Soft colours, gentle music, kind characters. Failing is a light setback, never a punishment.
2. **A little cheeky.** The inn is also a house of company. Flirting, innuendo and romance are part of its charm.
   Explicit content is not (section 17).
3. **Every day is different.** The planes, guests, events and boons change from day to day and run to run.
4. **Short sessions.** A day takes about 5 minutes and a run about 45.

## 1.3 Required capabilities

- **SCP-01** A third-person innkeeper who moves around the inn and its misty clearing and serves guests in person.
- **SCP-02** A daily loop of Morning, Open, Closing and Night phases (section 6).
- **SCP-03** A roguelike run: random planes, events and boons; it ends in a win or a loss; a seed can reproduce it.
- **SCP-04** Guests from several planes, each with their own wants, patience and tips (section 8).
- **SCP-05** Four service families: Tavern (drinks), Kitchen (meals), Rooms (beds) and Company (the lounge).
- **SCP-06** Coins, supplies and upgrades within a run, and a small meta-progression across runs.
- **SCP-07** Reputation that guests raise or lower; the run is lost when it reaches zero.
- **SCP-08** Chibi toon-shaded 3D presentation with mist as the main visual motif.
- **SCP-09** Saving and loading a run between days, and a profile that keeps meta-progression.
- **SCP-10** Content kept as data files that can be edited without code changes.
- **SCP-11** A Cozy mode setting that removes the Company service and its content.

## 1.4 Prototype decisions

- Single player, offline, Windows only.
- Placeholder primitives are acceptable until the art step (step 10).
- Saves happen only between days (at Night). Quitting during a day loses that day.
- The UI is in English only, but its text goes through Godot's translation functions so it can be localised later
  (`UI-09`).

## 1.5 Non-goals

- **SCP-20** Multiplayer, online features, leaderboards or accounts.
- **SCP-21** Consoles, mobile, Steam Deck tuning or controller-first design (basic gamepad support may come later; see
  section 24).
- **SCP-22** Explicit sexual content in any form (section 17).
- **SCP-23** Procedurally generated inn layouts. The inn is hand-built; only the content of each day is random.
- **SCP-24** Voice acting, cutscenes and branching story campaigns.
- **SCP-25** Building or decorating the inn freely. Upgrades are fixed slots (section 10.3).

---

# 2. Invariants

These rules apply to every step and every file. Other sections refer to them by ID.

- **INV-01** `MistyInn.Core` shall not reference Godot (`GodotSharp`, `Godot.NET.Sdk`) or any engine type. All game
  rules live in Core; Godot nodes only present Core's state and send it commands.
- **INV-02** The simulation is deterministic: the same seed, content and command sequence produce the same state. All
  randomness goes through an injected `IRandomSource` seeded per run. `System.Random`, `Guid.NewGuid()`,
  `DateTime.Now` and Godot's random functions are forbidden in Core.
- **INV-03** Game time advances only through `GameSession.Tick(TimeSpan)`, called from `_PhysicsProcess` with the
  fixed physics step (`SIM-02`). Core never reads the wall clock. Pausing stops the ticks.
- **INV-04** Content (planes, guest archetypes, services, supplies, upgrades, boons, events, companions) is data in
  `data/*.json` (section 18), not literals in code. Code may only hold fallback defaults that tests use.
- **INV-05** No unbounded growth: guests in the world, queues, logs and histories have fixed caps, enforced in Core.
- **INV-06** Every character is an adult. Content never goes past the ceiling in section 17. This invariant cannot be
  changed by a recorded decision, only by the human editing section 17.
- **INV-07** A missing, corrupt or older save never crashes the game and is never silently overwritten (section 19).
- **INV-08** The solution compiles and every test passes at the end of each step. No step weakens an earlier step's
  tests.
- **INV-09** Godot node scripts are `partial` classes named after their file. C# event handlers that nodes attach to
  Core or to other nodes are removed in `_ExitTree`.
- **INV-10** Every third-party asset (model, texture, font, sound, music, shader) has a known licence that allows use
  in a game, and is listed in `docs/ASSETS.md` with its source and licence. Unlicensed assets, or assets of unknown
  origin, are never added.
- **INV-11** No secrets, personal data, telemetry or network calls in the game.

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
│   ├── inn/                         (Inn.tscn, stations, rooms, slot markers)
│   ├── characters/                  (Player.tscn, Guest.tscn, Companion.tscn)
│   └── ui/                          (Hud.tscn, menus, Night summary, boon cards)
├── scripts/                         (C# node scripts, mirroring scenes/)
├── assets/
│   ├── models/  textures/  materials/  shaders/  audio/  fonts/
├── src/
│   ├── .gdignore
│   └── MistyInn.Core/               (engine-free game logic, INV-01)
├── tests/
│   ├── .gdignore
│   └── MistyInn.Core.Tests/         (NUnit)
└── docs/
```

- **STR-01** The Godot project lives at the repository root. `MistyInn.csproj` uses
  `Godot.NET.Sdk/<editor version>`, and that version shall equal the Godot editor version in use (currently 4.7.2).
  Upgrading Godot is a recorded decision that changes both together.
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
- **STR-10** Folder and namespace mirror each other: `MistyInn.Core.Guests` is `src/MistyInn.Core/Guests/`, and a node
  script in `scripts/ui/` uses the namespace `MistyInn.UI`.
- **STR-11** Windows builds export to `builds/windows/`, which is ignored by git.

---

# 4. Technology defaults

| Area | Default |
|---|---|
| Engine | Godot 4.7.2 .NET, Forward+ renderer |
| Language | C# 12 or later, `net8.0`, nullable enabled |
| Physics | Godot built-in 3D physics; fixed physics rate of 60 Hz |
| Serialisation | `System.Text.Json` in Core (source-generated contexts where practical) |
| Tests | NUnit, NUnit3TestAdapter, Microsoft.NET.Test.Sdk (versions from NuGet, `STR-09`) |
| Input | Input actions, registered in code with defaults (`InputActions`); rebinding in settings later |
| UI | Godot `Control` nodes with a shared `Theme` resource (`assets/ui/MistyTheme.tres`) |
| Text | Godot `Tr()` keys for UI text; content names come from data |
| Audio | Godot audio buses: Master, Music, SFX, Ambience |
| Save location | `user://` (on Windows, `%APPDATA%\Godot\app_userdata\Misty Inn\`) |

---

# 5. Architecture

## 5.1 Layers

```text
┌──────────────── Godot game (MistyInn.csproj) ────────────────┐
│ Scenes and node scripts: Player, GuestView, StationView,     │
│ Hud, menus, camera, audio, VFX.                              │
│ GameRoot node: owns GameSession, ticks it, drains its        │
│ events, sends commands.                                      │
│ Adapters: GodotContentSource, GodotSaveStore, settings.      │
└───────────────────────▲──────────────────┬───────────────────┘
                        │ events (read)    │ commands
┌───────────────────────┴──────────────────▼───────────────────┐
│ MistyInn.Core (no engine)                                    │
│ GameSession → RunState → DayState                            │
│ Systems: Phases, Arrivals, Guests, Services, Companions,     │
│ Economy, Reputation, Events, Boons, Meta.                    │
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

## 5.2 Commands and domain events

- **ARC-10** Commands are immutable records, for example `OpenInn`, `TakeOrder(guestId)`, `StartPrep(stationId,
  orderId)`, `Deliver(orderId, guestId)`, `AssignRoom(guestId, roomId)`, `PairCompanion(guestId, companionId)`,
  `Buy(itemId)`, `ChooseBoon(boonId)`, `Sleep`, `StartRun(seed)`.
- **ARC-11** `Execute` returns a `CommandResult` (success, or a reason code such as `NotInRange`, `WrongPhase`,
  `OutOfSupplies`). A command that is not valid changes nothing. Range checks are made by the presentation (it knows
  the 3D positions) and repeated in Core where Core knows the rule (phase, state, stock).
- **ARC-12** Core raises domain events (`PhaseChanged`, `GuestArrived`, `GuestQueued`, `OrderPlaced`,
  `PrepStarted`, `PrepFinished`, `OrderDelivered`, `GuestLeft`, `CoinsChanged`, `ReputationChanged`,
  `DailyEventStarted`, `BoonOffered`, `RunEnded`, and so on) into a bounded buffer (`INV-05`). `GameRoot` drains it
  once per frame and passes each event to the views that care about it.
- **ARC-13** Every entity has a stable integer ID issued by Core (`GuestId`, `OrderId`). Views map IDs to nodes and
  free a node when its entity leaves.

## 5.3 Time and the simulation tick

- **SIM-01** Core measures time as `TimeSpan` game time since the start of the day. Durations (patience, prep time,
  walking time) are in game seconds.
- **SIM-02** `GameRoot._PhysicsProcess(delta)` calls `Tick` with the fixed physics step multiplied by the time scale
  (1.0 normally; 0 when paused; up to 4.0 in debug builds for testing).
- **SIM-03** Core owns timing. When a guest walks from the mist to a slot, Core gives the travel time, and the view
  animates the walk so that it ends at that time. Views never report arrival back to Core.
- **SIM-04** Core knows locations only as logical slots (`MistGate[n]`, `QueueSlot[n]`, `TableSeat[n]`,
  `Room[n]`, `LoungeSeat[n]`, `Station[id]`). The inn scene places a `Marker3D` for each slot, and views look them up
  by name.

## 5.4 Scene tree (target)

```text
Main (Node3D, GameRoot.cs)
├── WorldEnvironment            mist fog, sky colour, tonemap
├── Sun (DirectionalLight3D)
├── Clearing                    ground, mist gates (Marker3D per gate), mist VFX
├── Inn (Inn.tscn)              walls, stations, queue/table/room/lounge markers
├── Guests (Node3D)             GuestView instances
├── Companions (Node3D)         CompanionView instances
├── Player (Player.tscn)        CharacterBody3D, camera rig, interaction area
├── Audio                       music, ambience players
└── UI (CanvasLayer)            Hud, prompts, menus, Night summary, boon picker
```

- **ARC-20** Scenes that appear more than once (guests, companions, stations) are their own `.tscn`, instanced from a
  `PackedScene` export. Building meshes in code is allowed only in the prototype skeleton and is removed in step 5.
- **ARC-21** Node references use `[Export]` fields set in the editor, or `%UniqueName` lookups inside one scene. Long
  `GetNode("../../..")` paths are not allowed.

---

# 6. Core loop: runs and days

## 6.1 Run

- **RUN-01** A run starts from a seed (random unless the player enters one) and the profile's unlocks. A new run
  starts with day 1, the starting coins, the starting supplies, starting reputation, and no boons.
- **RUN-02** A run is **won** by surviving the last day (`RunLength`, default 10). The last day is the
  **Convergence**: every unlocked plane is open and arrivals are faster.
- **RUN-03** A run is **lost** when reputation reaches 0 at the end of any day. The day is finished first; the loss
  is shown at Night.
- **RUN-04** When a run ends, the player earns **Mist Essence** based on days survived, guests served and whether the
  run was won (`MET-02`), then returns to the title screen.

## 6.2 Day phases

```text
Morning ──(player opens the doors)──▶ Open ──(timer runs out)──▶ Closing ──(last guest leaves)──▶ Night ──(Sleep)──▶ next Morning
```

- **DAY-01** **Morning** has no timer. The mist forecast shows which planes converge today (`PLN-03`) and today's
  daily event (`EVT-01`). The player may buy supplies and upgrades (section 10), then starts the day with the `OpenInn`
  command (interacting with the front door).
- **DAY-02** **Open** lasts `OpenDuration` (default 150 s). Guests arrive (`GST-01`). The HUD shows the time left.
- **DAY-03** **Closing** starts when the timer runs out. No new guests arrive. Guests already inside can still be
  served; queued guests who have not been taken in leave without counting as lost (`REP-04`). Closing ends when the inn
  is empty.
- **DAY-04** **Night** shows the day summary: guests served and lost, coins earned, tips, reputation change, and Mist
  Essence progress. If the run continues, the player picks one of three boons (`BON-01`) and then sleeps, which saves
  (`SAV-01`) and starts the next Morning.
- **DAY-05** Overnight, Rooms guests check out and pay, companions recover energy (`CMP-04`), and spoiled supplies are
  removed (`ECO-05`).

## 6.3 Pausing

- **DAY-10** Esc opens the pause menu and stops ticking (`INV-03`) in every phase. Resume, Settings, and Quit to title
  (with a warning that the current day is lost, `SAV-03`).

## 6.4 Tuning defaults (in `data/balance.json`)

| Value | Default |
|---|---|
| `RunLength` | 10 days |
| `OpenDuration` | 150 s |
| `StartingCoins` | 20 |
| `StartingReputation` / `MaxReputation` | 5 / 10 hearts |
| Base arrival interval | 7 s, ×0.9 per day, ×0.6 on the Convergence |
| Guest patience | from the archetype, 25-45 s |
| Planes per day | 2 on days 1-2, 3 on days 3-6, 4 on days 7-9, all on day 10 |
| Max guests in the world | 16 (`INV-05`) |
| Queue slots / table seats / rooms / lounge seats | 5 / 6 / 2 / 2 at the start |

---

# 7. Planes and the mist

- **PLN-01** A plane is a data entry (`data/planes.json`) with an ID, a display name, a mist colour, an ambience cue,
  guest archetypes, service preferences, and an optional modifier (for example "Ember guests tip double but have
  little patience").
- **PLN-02** The prototype ships five planes: the Clockwork Reach, Verdant Hollow, the Ember Wastes, the Starlit Sea
  and the Sugar Moon. Three are unlocked at the start; the rest are unlocked with Mist Essence (`MET-03`).
- **PLN-03** Each morning, Core draws today's converging planes from the unlocked set with the seeded RNG, according
  to the planes-per-day table (section 6.4). It does not draw the same set two days in a row when more than one set is
  possible.
- **PLN-04** Each converging plane opens one **mist gate** around the clearing, tinted in its colour. Guests from that
  plane walk out of that gate.
- **PLN-05** The mist is the main visual motif: a volumetric or layered fog around the clearing (never inside the inn),
  with each gate's mist tinted in its plane's colour (`ART-05`).

---

# 8. Guests

## 8.1 Arrivals and movement

- **GST-01** During Open, guests arrive at a seeded random interval around the day's arrival interval (×0.6 to ×1.4).
  The plane is chosen evenly from today's planes, unless a daily event changes the weights. The archetype is chosen by
  the plane's weights.
- **GST-02** A guest does not arrive if the world is full (`INV-05`) or every queue slot is taken. The next arrival
  is tried at the next interval.
- **GST-03** A guest walks from its plane's mist gate to the first free queue slot by the front door, and waits there
  to be greeted.

## 8.2 Guest life cycle

```text
Arriving → Queued → (Greet) → Seated / RoomAssigned / LoungePaired → Ordering → Waiting → Served → Leaving → Gone
                          └────────────── patience runs out at any waiting stage ───────────────▶ Leaving (lost)
```

- **GST-04** Greeting a queued guest (interact) shows their want: a specific drink, meal, room or companion. Tavern
  and Kitchen guests take a free table seat; Rooms guests need a free room; Company guests need a free lounge seat and
  an available companion.
- **GST-05** Patience counts down while the guest is Queued and while waiting for an order. Greeting adds a one-time
  bonus (+25 % of maximum patience). Patience is shown as a mood icon above the guest (`UI-04`).
- **GST-06** A guest whose patience runs out leaves **lost** (`REP-02`). A guest who is served leaves **happy** after
  a short stay (eat or drink 6-10 s; Rooms guests stay overnight, `DAY-05`; Company guests stay the length of the
  session, `CMP-03`).
- **GST-07** A guest has at most two wants per visit (for example a drink, then a meal). The second want appears only
  after the first is served, and only if the archetype allows it.

## 8.3 Archetypes

- **GST-10** An archetype (`data/guests.json`) has an ID, a plane, a list of names, a colour palette and model
  variant, a patience range, weights over wants, a tip multiplier and optional traits.
- **GST-11** Traits change rules in small, testable ways. Prototype set: `Impatient` (×0.7 patience), `Generous`
  (×1.5 tips), `Picky` (Quality must be Good or better for a tip), `Night Owl` (always wants a Room), `Romantic`
  (prefers Company).
- **GST-12** Every archetype is an adult (`INV-06`). The data has a required field `"adult": true`, and the content
  validator rejects any entry without it (`DAT-04`).

---

# 9. Services, stations and companions

## 9.1 Service families

| Family | Station | Flow | Supplies |
|---|---|---|---|
| Tavern | Bar | Take order → pour at the Bar (2 s) → deliver | Ale, Cider, Moonwine |
| Kitchen | Hearth | Take order → cook at the Hearth (6-10 s, runs on its own) → plate → deliver | Bread, Stew, Pie |
| Rooms | Front desk | Assign a free room; the guest goes upstairs and pays on checkout | Linen (one per stay) |
| Company | Lounge | Pair the guest with an available companion; the pair goes to a lounge booth | none (uses companion energy) |

- **SVC-01** A service (`data/services.json`) has an ID, a family, a price, a prep time, the supplies it uses, and its
  station.
- **SVC-02** The player carries **at most one** prepared item at a time (two with the Serving Tray upgrade). Carried
  items are shown in the player's hands and on the HUD.
- **SVC-03** **Quality**: Good if delivered within 50 % of the guest's patience, Fine within 80 %, Late after that.
  Quality changes the tip (`ECO-02`) and satisfaction (`REP-01`).
- **SVC-04** Stations run on their own once started. The Hearth can hold one dish (two with an upgrade). A finished
  dish waits on the station; it spoils into a lost item after 30 s.
- **SVC-05** Delivering the wrong item is refused (`CommandResult.WrongItem`) and costs nothing.

## 9.2 Companions (the Company family)

- **CMP-01** Companions are adult resident characters (`data/companions.json`), each with a name, a home plane, a
  personality, an energy value (default 3 sessions per day) and plane affinities. They are not guests and never leave
  the inn.
- **CMP-02** Pairing is the Company mini-game: pairing a guest with a companion they have affinity with gives
  Quality Good; neutral gives Fine; a disliked pairing gives Late.
- **CMP-03** A Company session lasts 20 s of game time. It takes place **off-screen**: the pair walks to a lounge
  booth, the booth curtain closes, and a short line of suggestive flavour text appears (section 17). The guest then
  pays and leaves happy.
- **CMP-04** Each session uses one energy. A companion with no energy rests until Night. Energy refills overnight.
- **CMP-05** The run starts with two companions; more are hired with upgrades (`UPG-*`) or unlocked with Mist Essence.
- **CMP-06** In Cozy mode (`SET-04`), the Company family, its guests' Company wants, companions and their flavour text
  are not shown or generated. Archetypes that would want Company pick another want. Cozy mode is not a difficulty
  change.

---

# 10. Economy, supplies and upgrades

## 10.1 Coins

- **ECO-01** Coins are earned from service prices and tips, and spent in the Morning shop. Coins never go below zero;
  a purchase that would do so is refused.
- **ECO-02** Tip = price × archetype tip multiplier × quality factor (Good 0.5, Fine 0.2, Late 0), rounded down.

## 10.2 Supplies

- **ECO-03** Supplies are counted per item. Preparing a service uses its supplies when preparation starts. When a
  supply runs out, the services that need it are greyed out and greeting a guest who wants it shows an "out of stock"
  bubble; the guest then picks another want if it has one, or leaves without counting as lost (one time per day,
  otherwise lost).
- **ECO-04** In the Morning, supplies are bought in bundles at prices from `data/supplies.json`.
- **ECO-05** Some supplies spoil: anything marked `perishable` that is still in stock at Night is lost.

## 10.3 Upgrades

- **UPG-01** Upgrades (`data/upgrades.json`) are bought in the Morning shop and last for the rest of the run. Each has
  an ID, a price, an optional prerequisite, and an effect from a fixed list of effect kinds (`DAT-03`).
- **UPG-02** Prototype upgrades: Serving Tray (carry 2), Second Hearth Pot, Extra Room, Lounge Booth, Hire Companion,
  Warm Lanterns (+10 % patience), Mist Bell (one extra queue slot).
- **UPG-03** Effects are applied by Core systems reading the run's modifiers, never by code that checks for a specific
  upgrade ID.

---

# 11. Satisfaction and reputation

- **REP-01** Each served guest gives satisfaction from quality: Good +2, Fine +1, Late 0. At the end of each day every
  6 satisfaction points (rounded down) add 1 reputation heart, up to `MaxReputation`.
- **REP-02** Each lost guest takes away 1 heart at once.
- **REP-03** A day with no lost guests adds 1 heart (Perfect Day).
- **REP-04** Guests who leave because of Closing, or the first "out of stock" each day (`ECO-03`), are not lost.
- **REP-05** Reputation is checked at the end of the day (`RUN-03`), so a bad day is always finished.

---

# 12. Roguelike systems

## 12.1 Daily events

- **EVT-01** Each morning from day 2, Core may draw one daily event (`data/events.json`), by weight, with a 60 %
  chance (100 % on the Convergence). The forecast shows it.
- **EVT-02** Prototype events: Plane Surge (one plane sends double guests), Thick Mist (+20 % patience, gates harder to
  see), Festival Night (+50 % tips, faster arrivals), Supply Cart (a free bundle), Grumpy Inspector (one VIP guest: a
  big tip if served Good, -2 hearts if lost).
- **EVT-03** An event's effect uses the same effect kinds as upgrades and boons (`DAT-03`), lasting one day.

## 12.2 Boons

- **BON-01** Each Night (except after the last day), Core offers three different boons from the unlocked pool by
  weight and rarity. The player must choose one. Boons last for the rest of the run.
- **BON-02** A boon has an ID, a name, a rarity (Common, Rare, Mythic), an optional plane tag, and one or more effects
  (`DAT-03`). Examples: Ember Hearth (meals cook 25 % faster), Starlit Charm (Starlit Sea guests tip double),
  Clockwork Legs (+15 % move speed), Sweet Talk (+1 companion energy).
- **BON-03** A boon offered that night is not offered again in the same run if it was not chosen, unless the pool would
  run out.

## 12.3 Meta-progression

- **MET-01** The profile (`SAV-02`) keeps Mist Essence, unlocks and run statistics across runs.
- **MET-02** Mist Essence at the end of a run = days survived × 2 + guests served ÷ 10 (rounded down) + 10 if won.
- **MET-03** Essence buys unlocks on the title screen: new planes, new boons in the pool, a starting companion,
  cosmetic decor. Unlocks never make the base game unwinnable without them.

---

# 13. Player, camera and interaction

- **PLR-01** The player is a `CharacterBody3D`: camera-relative movement with WASD or the arrow keys, Shift to walk
  slowly, Space to jump (it serves no purpose; it is just fun). Base speed is 5 m/s.
- **PLR-02** The character model turns smoothly to face the direction of movement.
- **PLR-03** The player has an interaction area (`Area3D`, radius 1.6 m). The nearest interactable in front of the
  player is highlighted, and a prompt shows the key and action ("E: Greet Mossbell", "E: Pour Moonwine").
- **PLR-04** Interact (E) sends the matching command to Core: greet, take order, start prep, pick up, deliver, assign
  room, pair companion, open the inn, shop. Core validates it (`ARC-11`), and a refusal shows a short reason bubble.
- **CAM-01** A third-person orbit camera on a `SpringArm3D` that does not collide with the player. Hold the right
  mouse button to orbit; the scroll wheel zooms between 3 and 9 m. Pitch is limited to between -70° and +10°.
- **CAM-02** Inside the inn the camera pulls in closer and the roof fades out, so the player can always see
  themselves.
- **CAM-03** Mouse sensitivity and camera inversion are settings (`SET-03`).

---

# 14. User interface

- **UI-01** The title screen has New Run (with an optional seed field), Continue (when a run save exists), Unlocks,
  Settings and Quit.
- **UI-02** The HUD shows the day, the phase and the time left, coins, reputation hearts, the carried items, and the
  Morning forecast.
- **UI-03** The Morning shop is a panel at the shop counter, with Supplies and Upgrades tabs. It shows prices, what the
  player owns and what they can afford.
- **UI-04** Each guest has a speech bubble showing their want as an icon and a mood icon for patience (happy, neutral,
  worried, cross). Bubbles stay readable through the mist.
- **UI-05** The Night summary lists served and lost guests, coins and tips, the reputation change, and then three boon
  cards to pick from.
- **UI-06** The run end screen shows win or loss, days, guests served and Mist Essence earned.
- **UI-07** The pause menu: Resume, Settings, Quit to title (`DAY-10`).
- **UI-08** One theme (`MistyTheme.tres`) with soft pastel colours, rounded panels and a friendly rounded font
  (licensed, `INV-10`). The minimum text size is 18 px at 1080p.
- **UI-09** UI text uses `Tr()` keys. Names and flavour text come from the data files.
- **UI-10** Every interactive control works with the mouse and the keyboard.

---

# 15. Art direction

- **ART-01** **Chibi proportions**: big heads (about 1:2 head to body), small bodies, big expressive eyes, simple
  shapes. The art should feel friendly and readable at a distance.
- **ART-02** **Toon shading**: a cel shader with 2 or 3 light bands, soft rim light and an inverted-hull outline,
  shared by every character and prop (`assets/shaders/toon.gdshader`).
- **ART-03** **Palette**: soft pastels. Each plane has a signature colour that appears in its guests' clothes, its
  mist gate and its UI accents.
- **ART-04** **The inn**: warm wood, lanterns and candlelight inside; a cosy contrast with the cool mist outside.
- **ART-05** **The mist**: layered fog or volumetric fog over the clearing, tinted near each gate, drifting slowly.
  It frames the world and hides the edge of the map.
- **ART-06** **Characters**: guests are made from shared base bodies (2 or 3) with plane-specific heads, outfits and
  colours, so new archetypes do not need new models.
- **ART-07** **Suggestive styling** (section 17) comes from outfits, poses, expressions and dialogue; it never relies
  on the chibi body itself. Companions use the tallest base body and adult styling (`RAT-03`).
- **ART-08** Placeholders: until step 10, characters are capsules with a colour and a name label, and the inn is
  boxes. Placeholders use the same scenes and slots as the final art, so replacing them changes no code.

---

# 16. Audio

- **AUD-01** Audio buses: Master, Music, SFX and Ambience, each with a volume setting (`SET-02`).
- **AUD-02** Music: a gentle loop for Morning and Night, a livelier loop for Open; a crossfade on phase change.
- **AUD-03** Ambience: mist wind outside, tavern murmur inside; each converging plane adds a quiet layer (for example
  ticking for the Clockwork Reach).
- **AUD-04** SFX: footsteps, door, pour, sizzle, coins, bell for a new guest, a happy and a cross sound when a guest
  leaves, UI clicks.
- **AUD-05** Every audio file is licensed and listed in `docs/ASSETS.md` (`INV-10`).

---

# 17. Content rating and tone

The human chose a **Suggestive (Mature)** ceiling (D-004). This section is the only place that sets what content is
allowed. Agents shall not go past it, and shall stop and ask (`AGT-03`) when unsure.

- **RAT-01** **Adults only.** Every character, including guests, companions and the innkeeper, is an adult. Data
  enforces this (`GST-12`, `DAT-04`). No character is described, written or styled as a child, and no suggestive
  content involves anyone who reads as young.
- **RAT-02** **Allowed**: flirting, innuendo, romance, double entendres, teasing dialogue, suggestive outfits
  (low necklines, bare shoulders, fitted clothes), suggestive poses and expressions, and implying that something
  happens behind a closed curtain.
- **RAT-03** **Because of the chibi style**, suggestive content is carried by dialogue, outfits and situations, not
  by the body. There is no nudity, no focus on body parts, and no sexualised close-ups of chibi models.
- **RAT-04** **Not allowed**: explicit sexual acts, depicted or described; nudity of genitals or breasts; sexual
  violence or coercion; anything non-consensual; content involving minors in any way. These cannot be enabled by
  a setting, mod hook or decision.
- **RAT-05** **Company sessions** happen off-screen (`CMP-03`). The flavour text is one or two lines, warm and cheeky,
  for example *"The curtain falls. Somewhere behind it, Pearl is laughing at a very bad sea pun."*
- **RAT-06** **Tone toward companions**: they are respected professionals and part of the inn's family. They choose
  their work, they are treated kindly, and the game never makes them the butt of a joke. The innkeeper never forces a
  pairing that a companion has refused (`CMP-02` dislikes are about fit, not refusal).
- **RAT-07** **Cozy mode** (`SET-04`) removes the Company family and every piece of suggestive flavour text, which
  makes the game suitable for a general audience. It is off by default and can be changed at any time in Settings.
- **RAT-08** When the game is published, the store page states the content honestly: "Suggestive themes, mild
  innuendo; Cozy mode removes them." For a hobby release, a short content note on first launch is enough.

---

# 18. Content data

- **DAT-01** Content files live in `data/` as JSON: `balance.json`, `planes.json`, `guests.json`, `services.json`,
  `supplies.json`, `upgrades.json`, `boons.json`, `events.json`, `companions.json`, `flavour.json`.
- **DAT-02** Core defines the schema as C# records and loads the files through `IContentSource`, which returns file
  text by name. The Godot adapter reads `res://data/` with `FileAccess`; the tests read from a folder or from strings.
  `export_presets.cfg` shall include `data/*.json` in the exported files.
- **DAT-03** Effects are a closed set of kinds with parameters, for example `{ "kind": "PrepSpeed", "family":
  "Kitchen", "multiplier": 0.75 }`. Supported kinds: `PrepSpeed`, `Patience`, `Tips`, `ArrivalRate`, `CarryCapacity`,
  `Capacity` (slot kind + count), `MoveSpeed`, `CompanionEnergy`, `PlaneWeight`, `GrantSupplies`, `ReputationOnLoss`.
  A new kind is an SDD change.
- **DAT-04** The content validator runs at load time and in the tests. It rejects: duplicate IDs, references to IDs
  that do not exist, unknown effect kinds, values out of range, characters without `"adult": true`, and flavour text
  without a `cozySafe` flag. In a debug build an invalid file stops the game with a clear message; in an export build
  the file falls back to the version built into the game.
- **DAT-05** IDs are lowercase kebab-case (`starlit-sea`, `moonwine`) and never change once a save may contain them.
  Display names may change freely.
- **DAT-06** Flavour text has a `cozySafe` flag. When Cozy mode is on, only lines with `cozySafe: true` are used
  (`RAT-07`).

---

# 19. Save, load and settings

- **SAV-01** The run is saved automatically when the player sleeps (`DAY-04`), to `user://saves/run.json`. The save
  holds the seed, the RNG state, the day, coins, reputation, supplies, upgrades, boons, companions, unlocked planes for
  the run, rooms occupied overnight, and the content version.
- **SAV-02** The profile (`user://saves/profile.json`) holds Mist Essence, unlocks, statistics and the content note
  acknowledgement. It is saved when a run ends and when an unlock is bought.
- **SAV-03** Saving is only between days. Continue resumes at the start of the saved Morning. Quitting during a day
  loses that day, and the pause menu says so.
- **SAV-04** Saves have a `schemaVersion`. Core migrates older versions forward step by step. A save from a newer
  version is not loaded, and the player is told why.
- **SAV-05** Writes are atomic: write `*.tmp`, then replace. The previous file is kept as `*.bak`.
- **SAV-06** A corrupt save is moved to `*.corrupt-<n>.json`, the player is told, and the game goes on without it
  (`INV-07`). A run save is deleted when the run ends.
- **SET-01** Settings are in `user://settings.cfg` (Godot `ConfigFile`), outside the saves.
- **SET-02** Audio: volume per bus (`AUD-01`).
- **SET-03** Controls: mouse sensitivity, invert camera Y.
- **SET-04** Content: Cozy mode (`RAT-07`).
- **SET-05** Display: window mode (windowed, borderless, fullscreen), V-Sync, resolution scale.

---

# 20. Testing

- **TST-01** Core tests are deterministic: a fixed seed through a test `IRandomSource`, time only through `Tick`, no
  sleeps, no file system except temporary folders for save tests.
- **TST-02** NUnit. Each test method carries `[Property("Requirement", "<ID>")]` for every requirement it verifies
  (more than one is allowed).
- **TST-03** Every behaviour change in Core comes with tests in the same step (`AGT-04`); no test only re-asserts a
  mock or a constant.
- **TST-04** Simulation: phase transitions, Closing rules, arrival intervals and caps, patience and loss, quality
  boundaries, tips, satisfaction to reputation, Perfect Day, run win and loss, determinism (two sessions with the same
  seed and commands produce the same state hash).
- **TST-05** Services: carrying limits, prep timing, spoilage, wrong item refused, supplies used, out-of-stock rules,
  companion energy and pairing quality.
- **TST-06** Roguelike: plane draws (no repeat), event draws, boon offers (three distinct, no repeat), effects applied
  through modifiers, Mist Essence formula, unlocks.
- **TST-07** Content: the real `data/` folder loads and validates; each validator rule rejects a bad example;
  Cozy mode filters Company content.
- **TST-08** Saves: round trip, atomic replace, `.bak` kept, corrupt file quarantined, older version migrated, newer
  version refused.
- **TST-09** Presentation (Godot nodes) has no automated tests in the prototype. Each step's demo script (section 21)
  is the manual check, and the human runs it in the editor.
- **TST-10** A headless smoke run is part of every step's commands for the human:
  `Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . --quit-after 600`. It shall print no errors.

---

# 21. Implementation steps

Steps are done **one at a time**. Each step gives the sections to read, the prompt, the requirements it covers,
acceptance criteria and a demo. After each step the human builds, tests, plays the demo and reads the report.

**Common preamble** (put before every step prompt):

> You are implementing Misty Inn from SDD v0.1. Work **only** on the step below; do not start later steps. Read
> sections 0, 2, 17 and 22 and the sections the step lists first. Apply the invariants (section 2) and the agent
> rules (section 22). If anything is ambiguous, contradictory or missing, **stop and ask** before writing code. Do not
> run builds, tests, Godot or git: list the exact commands for the human and mark results NOT RUN. Finish with the
> step report in Appendix A, including deviations from the SDD and proposed SDD changes.

## Step 0: Repository inspection (read-only)

**Read:** 1, 3, 5.
**Prompt:** Inspect the repository (the skeleton from before the SDD: `scenes/Main.tscn`, `scripts/*.cs`). Report the
files, how they differ from section 3 and section 5, and the smallest safe plan for step 1. Change nothing.
**Covers:** AGT-01.
**Acceptance:** Report only.
**Demo:** None.

## Step 1: Solution structure and Core skeleton

**Read:** 3, 4, 5.1, 5.2, 20.
**Prompt:** Create `src/MistyInn.Core` and `tests/MistyInn.Core.Tests` with `.gdignore` files, `Directory.Build.props`,
`.editorconfig`, the `Compile Remove` lines and the `ProjectReference` (`STR-01`..`STR-07`), and add both projects to
`MistyInn.sln`. Move `export_presets.cfg` from ignored to committed (`STR-08`). In Core, add `GameSession`,
`IRandomSource` with a seeded implementation, `GameCommand`, `CommandResult`, the domain event base type and its
bounded buffer, and entity IDs. Add tests for the RNG's determinism and the event buffer's bound. The game itself does
not change yet.
**Covers:** STR-*, ARC-01, ARC-03, ARC-10..13, INV-01, INV-02, INV-05, TST-01..03.
**Acceptance:** The solution builds; the tests pass; the game still runs as before.
**Demo:** `dotnet test`; run the game in Godot.

## Step 2: Core day loop and GameRoot

**Read:** 5.3, 6, 7 (PLN-03), 8.1, 11.
**Prompt:** Move the skeleton's day loop out of `scripts/Main.cs` into Core: phases, arrivals, patience, the simple
"serve" command, coins, reputation, run loss, and plane draws with the seeded RNG. `Main.cs` becomes `GameRoot`, which
ticks the session from `_PhysicsProcess`, drains events and drives the existing guest visuals. The behaviour should
match the skeleton, with Morning now waiting for `OpenInn` (`DAY-01`). Plane and service data may still be in code
until step 3.
**Covers:** SIM-01..04, DAY-01..05, DAY-10, GST-01..03, GST-05, GST-06, REP-01..05, RUN-03, PLN-03, ARC-02.
**Acceptance:** The tests cover `TST-04` for these rules, including determinism.
**Demo:** Play one day: Morning, open the doors, serve, Closing, Night, Sleep.

## Step 3: Content data and validation

**Read:** 7, 8.3, 17, 18.
**Prompt:** Add the data records, `IContentSource`, the Godot adapter, the loader and the validator, and the
`data/*.json` files for planes, archetypes, services, supplies and balance. Remove the content that is still in code.
**Covers:** DAT-01..06, PLN-01, PLN-02, GST-10..12, RAT-01, TST-07.
**Acceptance:** The real data loads and validates in the tests; every validator rule has a failing example.
**Demo:** Change a guest name in `guests.json` and see it in the game.

## Step 4: Service flow and stations

**Read:** 9.1, 10.2, 13.
**Prompt:** Add the greet, order, prepare, carry and deliver flow for Tavern and Kitchen, table seats, Quality, tips,
supplies, out-of-stock handling and spoilage. Add the Bar and Hearth stations, the interaction area and prompts.
**Covers:** GST-04, GST-07, SVC-01..05, ECO-01..05, PLR-01..04, CAM-01, TST-05.
**Acceptance:** Tests for every rule; a full day can be played with drinks and meals.
**Demo:** Serve a drink and a meal; let a dish spoil; run out of Ale.

## Step 5: Rooms, companions and scene instancing

**Read:** 9.2, 17, 5.4.
**Prompt:** Add Rooms (assign, overnight stay, checkout), companions (energy, affinity pairing, off-screen sessions,
flavour text) and Cozy mode filtering. Replace the code-built guest meshes with `Guest.tscn` and `Companion.tscn`
(`ARC-20`), and add the inn's slot markers.
**Covers:** CMP-01..06, SET-04 (logic only), RAT-02..07, ARC-20, ARC-21, SIM-04.
**Acceptance:** Tests for pairing quality, energy and Cozy mode filtering; flavour text follows section 17.
**Demo:** Pair a Romantic guest with a companion who likes their plane; turn Cozy mode on and see Company disappear.

## Step 6: Roguelike layer

**Read:** 6.1, 12.
**Prompt:** Add run length and the Convergence, daily events, boons and the Night boon choice, the effect system
(`DAT-03`) and modifiers, run win, Mist Essence and unlocks in Core.
**Covers:** RUN-01..04, EVT-01..03, BON-01..03, MET-01..03, UPG-03, TST-06.
**Acceptance:** Tests for draws, effects and the Essence formula; a seeded run replays identically.
**Demo:** Play to day 3 with a fixed seed twice and compare the forecasts.

## Step 7: Morning shop and upgrades

**Read:** 10.
**Prompt:** Add the shop counter, the shop panel, buying supplies and upgrades, and the prototype upgrades.
**Covers:** ECO-04, UPG-01..03, UI-03.
**Acceptance:** Tests for purchases and prerequisites; upgrades change play.
**Demo:** Buy the Serving Tray and carry two drinks.

## Step 8: Saves, profile and settings

**Read:** 19.
**Prompt:** Add the run save at Sleep, Continue, the profile, migrations, atomic writes, quarantine of corrupt files,
and the settings file with audio, controls, content and display options.
**Covers:** SAV-01..06, SET-01..05, MET-01, TST-08.
**Acceptance:** Tests for every `SAV` rule.
**Demo:** Play two days, quit, Continue; corrupt the save by hand and start the game.

## Step 9: UI pass

**Read:** 14.
**Prompt:** Build the title screen, HUD, guest bubbles, Night summary with boon cards, run end screen, pause menu and
the shared theme.
**Covers:** UI-01..10, CAM-02, CAM-03.
**Acceptance:** Every screen works with mouse and keyboard; all text goes through `Tr()`.
**Demo:** A full run from the title screen to the run end screen.

## Step 10: Art and audio pass

**Read:** 15, 16, 17.
**Prompt:** Add the toon shader and outline, chibi base bodies and plane variants, the inn interior, the mist gates and
fog, music, ambience and SFX. Every asset is licensed and listed in `docs/ASSETS.md`. Placeholders are replaced
without code changes.
**Covers:** ART-01..08, AUD-01..05, PLN-04, PLN-05, INV-10.
**Acceptance:** No unlicensed assets; the scenes keep their slots and scripts.
**Demo:** Screenshots of Morning, Open and Night.

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
  `docs/SDD.md` only when the human explicitly asks. When you do, bump the version, update the change log, and first
  save the previous version as `docs/SDD_v<old>.md`.
- **AGT-09** Update `docs/PROGRESS.md` and `docs/DECISIONS.md` in every step.
- **AGT-10** Never type package versions by hand (`STR-09`).
- **AGT-11** Never add content that goes past section 17. When unsure, stop and ask.
- **AGT-12** Never add a third-party asset without its licence in `docs/ASSETS.md` (`INV-10`). Do not download assets
  yourself; give the human the link and the licence to check.
- **AGT-13** Keep `README.md` current when controls, run instructions or the folder layout change.

---

# 23. Definition of done and known limitations

## 23.1 Definition of done (prototype)

- [ ] The solution builds; every Core test passes; the headless smoke run prints no errors.
- [ ] Every requirement has a tagged test, or an exemption with a reason in `docs/TRACEABILITY.md`.
- [ ] A full run can be played from the title screen to a win or a loss.
- [ ] A seeded run replays identically (`INV-02`).
- [ ] All content comes from `data/` and validates.
- [ ] Saves survive quitting, corruption and an older version.
- [ ] Cozy mode removes every Company element and suggestive line.
- [ ] Every asset is listed with its licence.
- [ ] A Windows export runs on its own.

## 23.2 Known prototype limitations

- Saves only between days.
- No gamepad support or key rebinding UI.
- English only (with the hooks for translation).
- Node scripts have no automated tests (`TST-09`).
- The balance is untested beyond the developer's play sessions.

---

# 24. Backlog and extension seams

Ideas that are **not** in scope for the prototype. Adding one is an SDD change.

- Staff: hire a barkeep or cook who runs a station on their own.
- Plane quests: a regular guest's short story over several runs.
- Decor that changes the mood or patience.
- Gamepad support and key rebinding.
- Steam release: achievements, cloud saves, store page with the content note (`RAT-08`).
- Seams: `IContentSource` (mod folders), `ISaveStore` (cloud saves), the effect kinds (`DAT-03`), new service
  families through `data/services.json` and a new station scene.

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
example a scene or art requirement), **Exempt** (no sensible check; the reason is given) or **Not covered** (reported
as a finding).

```markdown
| Requirement | Short description | Tests / check | Status |
|---|---|---|---|
| GST-06 | Patience runs out: guest leaves lost | GuestTests.PatienceExpiry | Covered |
```

# Appendix D: Requirement index

Every requirement ID in this document, in order. Generated from the text; regenerate it whenever the SDD changes.

<!-- REQUIREMENT-INDEX:START -->
189 requirements.

| ID | Summary | Section |
|---|---|---|
| SCP-01 | A third-person innkeeper who moves around the inn and its misty clearing and serves... | 1.3 |
| SCP-02 | A daily loop of Morning, Open, Closing and Night phases (section 6) | 1.3 |
| SCP-03 | A roguelike run: random planes, events and boons; it ends in a win or a loss; a seed... | 1.3 |
| SCP-04 | Guests from several planes, each with their own wants, patience and tips (section 8) | 1.3 |
| SCP-05 | Four service families: Tavern (drinks), Kitchen (meals), Rooms (beds) and Company (the... | 1.3 |
| SCP-06 | Coins, supplies and upgrades within a run, and a small meta-progression across runs | 1.3 |
| SCP-07 | Reputation that guests raise or lower; the run is lost when it reaches zero | 1.3 |
| SCP-08 | Chibi toon-shaded 3D presentation with mist as the main visual motif | 1.3 |
| SCP-09 | Saving and loading a run between days, and a profile that keeps meta-progression | 1.3 |
| SCP-10 | Content kept as data files that can be edited without code changes | 1.3 |
| SCP-11 | A Cozy mode setting that removes the Company service and its content | 1.3 |
| SCP-20 | Multiplayer, online features, leaderboards or accounts | 1.5 |
| SCP-21 | Consoles, mobile, Steam Deck tuning or controller-first design (basic gamepad support... | 1.5 |
| SCP-22 | Explicit sexual content in any form (section 17) | 1.5 |
| SCP-23 | Procedurally generated inn layouts | 1.5 |
| SCP-24 | Voice acting, cutscenes and branching story campaigns | 1.5 |
| SCP-25 | Building or decorating the inn freely | 1.5 |
| INV-01 | `MistyInn.Core` shall not reference Godot (`GodotSharp`, `Godot.NET.Sdk`) or any... | 2 |
| INV-02 | The simulation is deterministic: the same seed, content and command sequence produce... | 2 |
| INV-03 | Game time advances only through `GameSession.Tick(TimeSpan)`, called from... | 2 |
| INV-04 | Content (planes, guest archetypes, services, supplies, upgrades, boons, events,... | 2 |
| INV-05 | No unbounded growth: guests in the world, queues, logs and histories have fixed caps,... | 2 |
| INV-06 | Every character is an adult | 2 |
| INV-07 | A missing, corrupt or older save never crashes the game and is never silently... | 2 |
| INV-08 | The solution compiles and every test passes at the end of each step | 2 |
| INV-09 | Godot node scripts are `partial` classes named after their file | 2 |
| INV-10 | Every third-party asset (model, texture, font, sound, music, shader) has a known... | 2 |
| INV-11 | No secrets, personal data, telemetry or network calls in the game | 2 |
| STR-01 | The Godot project lives at the repository root | 3 |
| STR-02 | `src/MistyInn.Core` is a plain class library (`Microsoft.NET.Sdk`, `net8.0`) with no... | 3 |
| STR-03 | `tests/MistyInn.Core.Tests` is an NUnit project that references only Core | 3 |
| STR-04 | The Godot SDK compiles every `*.cs` file under the project folder by default, so... | 3 |
| STR-05 | References point one way: Game → Core, Tests → Core | 3 |
| STR-06 | `Directory.Build.props` sets `Nullable` to `enable`, `LangVersion` to `latest`, and... | 3 |
| STR-07 | The test project sets `<RollForward>Major</RollForward>` so it runs on whichever .NET... | 3 |
| STR-08 | Committed: `project.godot`, `*.tscn`, `*.tres`, `*.import`, `*.uid`,... | 3 |
| STR-09 | Package versions are never typed by hand | 3 |
| STR-10 | Folder and namespace mirror each other: `MistyInn.Core.Guests` is... | 3 |
| STR-11 | Windows builds export to `builds/windows/`, which is ignored by git | 3 |
| ARC-01 | `GameSession` is the single entry point into Core | 5.1 |
| ARC-02 | One `GameRoot` node (the script on `Main.tscn`) owns the only `GameSession` | 5.1 |
| ARC-03 | Core state is changed only by `Tick` and `Execute` | 5.1 |
| ARC-04 | Each Core system is a class with explicit dependencies passed to its constructor and... | 5.1 |
| ARC-10 | Commands are immutable records, for example `OpenInn`, `TakeOrder(guestId)`,... | 5.2 |
| ARC-11 | `Execute` returns a `CommandResult` (success, or a reason code such as `NotInRange`,... | 5.2 |
| ARC-12 | Core raises domain events (`PhaseChanged`, `GuestArrived`, `GuestQueued`,... | 5.2 |
| ARC-13 | Every entity has a stable integer ID issued by Core (`GuestId`, `OrderId`) | 5.2 |
| SIM-01 | Core measures time as `TimeSpan` game time since the start of the day | 5.3 |
| SIM-02 | `GameRoot._PhysicsProcess(delta)` calls `Tick` with the fixed physics step multiplied... | 5.3 |
| SIM-03 | Core owns timing | 5.3 |
| SIM-04 | Core knows locations only as logical slots (`MistGate[n]`, `QueueSlot[n]`,... | 5.3 |
| ARC-20 | Scenes that appear more than once (guests, companions, stations) are their own... | 5.4 |
| ARC-21 | Node references use `[Export]` fields set in the editor, or `%UniqueName` lookups... | 5.4 |
| RUN-01 | A run starts from a seed (random unless the player enters one) and the profile's unlocks | 6.1 |
| RUN-02 | A run is won by surviving the last day (`RunLength`, default 10) | 6.1 |
| RUN-03 | A run is lost when reputation reaches 0 at the end of any day | 6.1 |
| RUN-04 | When a run ends, the player earns Mist Essence based on days survived, guests served... | 6.1 |
| DAY-01 | Morning has no timer | 6.2 |
| DAY-02 | Open lasts `OpenDuration` (default 150 s) | 6.2 |
| DAY-03 | Closing starts when the timer runs out | 6.2 |
| DAY-04 | Night shows the day summary: guests served and lost, coins earned, tips, reputation... | 6.2 |
| DAY-05 | Overnight, Rooms guests check out and pay, companions recover energy (`CMP-04`), and... | 6.2 |
| DAY-10 | Esc opens the pause menu and stops ticking (`INV-03`) in every phase | 6.3 |
| PLN-01 | A plane is a data entry (`data/planes.json`) with an ID, a display name, a mist... | 7 |
| PLN-02 | The prototype ships five planes: the Clockwork Reach, Verdant Hollow, the Ember... | 7 |
| PLN-03 | Each morning, Core draws today's converging planes from the unlocked set with the... | 7 |
| PLN-04 | Each converging plane opens one mist gate around the clearing, tinted in its colour | 7 |
| PLN-05 | The mist is the main visual motif: a volumetric or layered fog around the clearing... | 7 |
| GST-01 | During Open, guests arrive at a seeded random interval around the day's arrival... | 8.1 |
| GST-02 | A guest does not arrive if the world is full (`INV-05`) or every queue slot is taken | 8.1 |
| GST-03 | A guest walks from its plane's mist gate to the first free queue slot by the front... | 8.1 |
| GST-04 | Greeting a queued guest (interact) shows their want: a specific drink, meal, room or... | 8.2 |
| GST-05 | Patience counts down while the guest is Queued and while waiting for an order | 8.2 |
| GST-06 | A guest whose patience runs out leaves lost (`REP-02`) | 8.2 |
| GST-07 | A guest has at most two wants per visit (for example a drink, then a meal) | 8.2 |
| GST-10 | An archetype (`data/guests.json`) has an ID, a plane, a list of names, a colour... | 8.3 |
| GST-11 | Traits change rules in small, testable ways | 8.3 |
| GST-12 | Every archetype is an adult (`INV-06`) | 8.3 |
| SVC-01 | A service (`data/services.json`) has an ID, a family, a price, a prep time, the... | 9.1 |
| SVC-02 | The player carries at most one prepared item at a time (two with the Serving Tray upgrade) | 9.1 |
| SVC-03 | Quality: Good if delivered within 50 % of the guest's patience, Fine within 80 %, Late... | 9.1 |
| SVC-04 | Stations run on their own once started | 9.1 |
| SVC-05 | Delivering the wrong item is refused (`CommandResult.WrongItem`) and costs nothing | 9.1 |
| CMP-01 | Companions are adult resident characters (`data/companions.json`), each with a name, a... | 9.2 |
| CMP-02 | Pairing is the Company mini-game: pairing a guest with a companion they have affinity... | 9.2 |
| CMP-03 | A Company session lasts 20 s of game time | 9.2 |
| CMP-04 | Each session uses one energy | 9.2 |
| CMP-05 | The run starts with two companions; more are hired with upgrades (`UPG-*`) or unlocked... | 9.2 |
| CMP-06 | In Cozy mode (`SET-04`), the Company family, its guests' Company wants, companions and... | 9.2 |
| ECO-01 | Coins are earned from service prices and tips, and spent in the Morning shop | 10.1 |
| ECO-02 | Tip = price × archetype tip multiplier × quality factor (Good 0.5, Fine 0.2, Late 0),... | 10.1 |
| ECO-03 | Supplies are counted per item | 10.2 |
| ECO-04 | In the Morning, supplies are bought in bundles at prices from `data/supplies.json` | 10.2 |
| ECO-05 | Some supplies spoil: anything marked `perishable` that is still in stock at Night is lost | 10.2 |
| UPG-01 | Upgrades (`data/upgrades.json`) are bought in the Morning shop and last for the rest... | 10.3 |
| UPG-02 | Prototype upgrades: Serving Tray (carry 2), Second Hearth Pot, Extra Room, Lounge... | 10.3 |
| UPG-03 | Effects are applied by Core systems reading the run's modifiers, never by code that... | 10.3 |
| REP-01 | Each served guest gives satisfaction from quality: Good +2, Fine +1, Late 0 | 11 |
| REP-02 | Each lost guest takes away 1 heart at once | 11 |
| REP-03 | A day with no lost guests adds 1 heart (Perfect Day) | 11 |
| REP-04 | Guests who leave because of Closing, or the first "out of stock" each day (`ECO-03`),... | 11 |
| REP-05 | Reputation is checked at the end of the day (`RUN-03`), so a bad day is always finished | 11 |
| EVT-01 | Each morning from day 2, Core may draw one daily event (`data/events.json`), by... | 12.1 |
| EVT-02 | Prototype events: Plane Surge (one plane sends double guests), Thick Mist (+20 %... | 12.1 |
| EVT-03 | An event's effect uses the same effect kinds as upgrades and boons (`DAT-03`), lasting... | 12.1 |
| BON-01 | Each Night (except after the last day), Core offers three different boons from the... | 12.2 |
| BON-02 | A boon has an ID, a name, a rarity (Common, Rare, Mythic), an optional plane tag, and... | 12.2 |
| BON-03 | A boon offered that night is not offered again in the same run if it was not chosen,... | 12.2 |
| MET-01 | The profile (`SAV-02`) keeps Mist Essence, unlocks and run statistics across runs | 12.3 |
| MET-02 | Mist Essence at the end of a run = days survived × 2 + guests served ÷ 10 (rounded... | 12.3 |
| MET-03 | Essence buys unlocks on the title screen: new planes, new boons in the pool, a... | 12.3 |
| PLR-01 | The player is a `CharacterBody3D`: camera-relative movement with WASD or the arrow... | 13 |
| PLR-02 | The character model turns smoothly to face the direction of movement | 13 |
| PLR-03 | The player has an interaction area (`Area3D`, radius 1.6 m) | 13 |
| PLR-04 | Interact (E) sends the matching command to Core: greet, take order, start prep, pick... | 13 |
| CAM-01 | A third-person orbit camera on a `SpringArm3D` that does not collide with the player | 13 |
| CAM-02 | Inside the inn the camera pulls in closer and the roof fades out, so the player can... | 13 |
| CAM-03 | Mouse sensitivity and camera inversion are settings (`SET-03`) | 13 |
| UI-01 | The title screen has New Run (with an optional seed field), Continue (when a run save... | 14 |
| UI-02 | The HUD shows the day, the phase and the time left, coins, reputation hearts, the... | 14 |
| UI-03 | The Morning shop is a panel at the shop counter, with Supplies and Upgrades tabs | 14 |
| UI-04 | Each guest has a speech bubble showing their want as an icon and a mood icon for... | 14 |
| UI-05 | The Night summary lists served and lost guests, coins and tips, the reputation change,... | 14 |
| UI-06 | The run end screen shows win or loss, days, guests served and Mist Essence earned | 14 |
| UI-07 | The pause menu: Resume, Settings, Quit to title (`DAY-10`) | 14 |
| UI-08 | One theme (`MistyTheme.tres`) with soft pastel colours, rounded panels and a friendly... | 14 |
| UI-09 | UI text uses `Tr()` keys | 14 |
| UI-10 | Every interactive control works with the mouse and the keyboard | 14 |
| ART-01 | Chibi proportions: big heads (about 1:2 head to body), small bodies, big expressive... | 15 |
| ART-02 | Toon shading: a cel shader with 2 or 3 light bands, soft rim light and an... | 15 |
| ART-03 | Palette: soft pastels | 15 |
| ART-04 | The inn: warm wood, lanterns and candlelight inside; a cosy contrast with the cool... | 15 |
| ART-05 | The mist: layered fog or volumetric fog over the clearing, tinted near each gate,... | 15 |
| ART-06 | Characters: guests are made from shared base bodies (2 or 3) with plane-specific... | 15 |
| ART-07 | Suggestive styling (section 17) comes from outfits, poses, expressions and dialogue;... | 15 |
| ART-08 | Placeholders: until step 10, characters are capsules with a colour and a name label,... | 15 |
| AUD-01 | Audio buses: Master, Music, SFX and Ambience, each with a volume setting (`SET-02`) | 16 |
| AUD-02 | Music: a gentle loop for Morning and Night, a livelier loop for Open; a crossfade on... | 16 |
| AUD-03 | Ambience: mist wind outside, tavern murmur inside; each converging plane adds a quiet... | 16 |
| AUD-04 | SFX: footsteps, door, pour, sizzle, coins, bell for a new guest, a happy and a cross... | 16 |
| AUD-05 | Every audio file is licensed and listed in `docs/ASSETS.md` (`INV-10`) | 16 |
| RAT-01 | Adults only. Every character, including guests, companions and the innkeeper, is an adult | 17 |
| RAT-02 | Allowed: flirting, innuendo, romance, double entendres, teasing dialogue, suggestive... | 17 |
| RAT-03 | Because of the chibi style, suggestive content is carried by dialogue, outfits and... | 17 |
| RAT-04 | Not allowed: explicit sexual acts, depicted or described; nudity of genitals or... | 17 |
| RAT-05 | Company sessions happen off-screen (`CMP-03`) | 17 |
| RAT-06 | Tone toward companions: they are respected professionals and part of the inn's family | 17 |
| RAT-07 | Cozy mode (`SET-04`) removes the Company family and every piece of suggestive flavour... | 17 |
| RAT-08 | When the game is published, the store page states the content honestly: "Suggestive... | 17 |
| DAT-01 | Content files live in `data/` as JSON: `balance.json`, `planes.json`, `guests.json`,... | 18 |
| DAT-02 | Core defines the schema as C# records and loads the files through `IContentSource`,... | 18 |
| DAT-03 | Effects are a closed set of kinds with parameters, for example `{ "kind": "PrepSpeed",... | 18 |
| DAT-04 | The content validator runs at load time and in the tests | 18 |
| DAT-05 | IDs are lowercase kebab-case (`starlit-sea`, `moonwine`) and never change once a save... | 18 |
| DAT-06 | Flavour text has a `cozySafe` flag | 18 |
| SAV-01 | The run is saved automatically when the player sleeps (`DAY-04`), to... | 19 |
| SAV-02 | The profile (`user://saves/profile.json`) holds Mist Essence, unlocks, statistics and... | 19 |
| SAV-03 | Saving is only between days | 19 |
| SAV-04 | Saves have a `schemaVersion` | 19 |
| SAV-05 | Writes are atomic: write `*.tmp`, then replace | 19 |
| SAV-06 | A corrupt save is moved to `*.corrupt-<n>.json`, the player is told, and the game goes... | 19 |
| SET-01 | Settings are in `user://settings.cfg` (Godot `ConfigFile`), outside the saves | 19 |
| SET-02 | Audio: volume per bus (`AUD-01`) | 19 |
| SET-03 | Controls: mouse sensitivity, invert camera Y | 19 |
| SET-04 | Content: Cozy mode (`RAT-07`) | 19 |
| SET-05 | Display: window mode (windowed, borderless, fullscreen), V-Sync, resolution scale | 19 |
| TST-01 | Core tests are deterministic: a fixed seed through a test `IRandomSource`, time only... | 20 |
| TST-02 | NUnit | 20 |
| TST-03 | Every behaviour change in Core comes with tests in the same step (`AGT-04`); no test... | 20 |
| TST-04 | Simulation: phase transitions, Closing rules, arrival intervals and caps, patience and... | 20 |
| TST-05 | Services: carrying limits, prep timing, spoilage, wrong item refused, supplies used,... | 20 |
| TST-06 | Roguelike: plane draws (no repeat), event draws, boon offers (three distinct, no... | 20 |
| TST-07 | Content: the real `data/` folder loads and validates; each validator rule rejects a... | 20 |
| TST-08 | Saves: round trip, atomic replace, `.bak` kept, corrupt file quarantined, older... | 20 |
| TST-09 | Presentation (Godot nodes) has no automated tests in the prototype | 20 |
| TST-10 | A headless smoke run is part of every step's commands for the human:... | 20 |
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

**By prefix:** `SCP` (17), `INV` (11), `STR` (11), `ARC` (10), `SIM` (4), `RUN` (4), `DAY` (6), `PLN` (5), `GST` (10), `SVC` (5), `CMP` (6), `ECO` (5), `UPG` (3), `REP` (5), `EVT` (3), `BON` (3), `MET` (3), `PLR` (4), `CAM` (3), `UI` (10), `ART` (8), `AUD` (5), `RAT` (8), `DAT` (6), `SAV` (6), `SET` (5), `TST` (10), `AGT` (13)
<!-- REQUIREMENT-INDEX:END -->

# Appendix E: Glossary

| Term | Meaning |
|---|---|
| **Archetype** | A kind of guest from one plane (data), from which individual guests are made |
| **Boon** | A run-long bonus chosen each Night |
| **Companion** | An adult resident of the inn who provides the Company service |
| **Convergence** | The last day of a run: every plane converges at once |
| **Core** | `MistyInn.Core`, the engine-free library with all game rules |
| **Cozy mode** | A setting that removes the Company service and suggestive content |
| **Daily event** | A one-day modifier drawn each Morning |
| **Effect kind** | One of the fixed modifier types in `DAT-03` |
| **GameRoot** | The node on `Main.tscn` that owns the `GameSession` |
| **Mist Essence** | The currency kept across runs, spent on unlocks |
| **Mist gate** | The point in the clearing where one plane's guests appear |
| **Plane** | A slice of reality whose mist reaches the inn |
| **Quality** | Good, Fine or Late, from how fast a guest was served |
| **Run** | One playthrough from day 1 to a win or a loss |
| **Slot** | A logical location (queue, seat, room, lounge, station) placed in the scene with a `Marker3D` |
| **Station** | The Bar, the Hearth, the front desk or the Lounge |
