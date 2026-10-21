# CLAUDE.md

Misty Inn: a dark roguelike autobattler about an inn at the nexus of hostile, converging planes. The player sets the
inn up at Dusk; the Mist phase then plays out automatically until Dawn. Godot 4.7.2 .NET, C# (`net8.0`), Windows,
hobby prototype. It is built one step at a time from the design document.

## Read first
- `docs/SDD.md`: the design document (SDD v1.9, approved). Before any work, read sections 0, 2, 17 and 22 and
  Appendix A, plus the sections the requested step lists. Appendix D indexes every requirement ID; Appendix E is the
  glossary. Earlier versions are kept as `docs/SDD_v<version>.md`.
- `docs/PROGRESS.md`: step status, build, test and smoke-run results, toolchain and package versions.
- `docs/DECISIONS.md`: recorded decisions, with an index table at the top (D-001 to D-083 so far; continue numbering).
- `docs/ASSETS.md`: third-party assets and their licences (created when the first asset is added).

## Current state
- SDD v1.9 is approved. Steps 0 to 6 are complete (step 6 demo not reported yet).
- Step 1 built the solution structure and the Core skeleton: `src/MistyInn.Core` (GameSession, SeededRandom, event
  buffer, cycle log, entity IDs), `tests/MistyInn.Core.Tests` (49/49 pass in the .NET SDK container), shared build
  settings (D-037), the classic `.sln` with export build types mapped by hand (D-035), and the Windows export preset
  (`export_presets.cfg`, committed). The Godot editor builds the project. Decisions D-001 to D-041 are Accepted.
- Step 2 (board, cycle loop, isometric view) is complete: builds pass, 145/145 tests pass, and the human's
  play-through passed. Core board, A*, the Dusk/Mist/Dawn loop, arrivals and wandering with built-in content (D-044); `scenes/Main.tscn` is
  the main scene, with the isometric camera, board and unit views, drag-and-drop placement and a placeholder HUD
  (D-048). Decisions D-042 to D-049 are Accepted. The headless smoke run (TST-12) applies from this step.
- Step 3 (content data and validation) is complete: 197/197 tests, the smoke run and the demo passed.
  `data/*.json` (placeholder roster and creatures, D-053), `ContentLoader`, `ContentValidator`, `UpgradeTrees`, the
  built-in copy in Core (D-054) and `GodotContentSource`. Decisions D-001 to D-064 are Accepted.
- Step 4 (agitation, techniques and the bestiary) is complete: build, 238/238 tests, smoke run and
  play-through passed. It adds `AgitationSystem`, `ServiceSystem`, `CreatureBehaviourSystem`, `InnSense`, fixtures with shapes and
  seats, the build menu, silver and supplies, tributes, `CreatureBestiary` and the forecast. Decisions D-001 to
  D-068 are Accepted.
- Step 5 (escalation, combat, Company and content filters) is complete: build, 281/281 tests, smoke run and
  play-through passed. It adds `CombatSystem`, `CompanySystem`, `InnDamage`, back rooms as their own zones with `RoomDoor` tiles,
  integrity and Dawn repairs, the Dawn wound-or-die roll, `ContentFilters` and `FlavourPicker`, and the debug dev
  panel (`DevAddStaff`, `DevSetConvergingPlanes`). Older cycle tests use `TestGame.Sturdy` content (D-076). Decisions
  D-001 to D-076 are Accepted.
- Step 6 (shop, ranks, items and synergies) is complete: build, 326/326 tests and smoke run passed (the demo
  play-through is not reported yet); SDD v1.9 records it. The next step is step 7 (roguelike layer). It adds
  `ShopSystem` (its own random stream, D-080), `Modifiers` (item, inn item and synergy effects; synergies go to all staff
  on the board, D-077), `AncestryObserver` and `AncestryRecord` (kept across runs like the bestiary, D-082), ranks and
  the pending upgrade pick (every other command refused until it is made, D-081), owned items with `ItemId`,
  `data/synergies.json`, the locked annex `room-3` and `displaySpots` in `inn.json` (D-078), and `adjacentTo` effects
  (D-079). The HUD has Shop, Build menu and Staff and items tabs and an upgrade dialog. D-077 to D-083 are Accepted (2026-10-21).
- Godot is at `C:\Godot_v4.7.2`. If Smart App Control blocks `dotnet test`, the tests run in the .NET SDK Linux
  container.

## Working rules
- Do only the step requested. Start from the SDD section 21 common preamble, and finish with the Appendix A step
  report.
- If the SDD is ambiguous, contradictory or silent on a material decision, stop and ask (AGT-03).
- Do not run builds, tests, the Godot editor, headless Godot or git commands. The human runs them. List the exact
  commands and mark results NOT RUN (AGT-05).
- Never type package versions by hand. Provide `dotnet add <project> package <id>` commands (STR-09).
- Game rules go in `src/MistyInn.Core`, which never references Godot (INV-01). Node scripts only present and command.
- All randomness through the run's seeded `IRandomSource`, all time through `GameSession.Tick` (INV-02, INV-03).
- Content is data in `data/*.json`, never literals in code (INV-04). The files are strict JSON (no comments) and
  unknown properties are errors (D-055); run the tests after editing them.
- Content never goes past SDD section 17: every character is an adult; Company is off-screen in rooms, only with
  sapient adult creatures, and refusals are always honoured; no nudity or explicit content; moderate blood but no
  dismemberment; suggestive content and violence are never combined. When unsure, stop and ask (AGT-11).
- Every mist-phase outcome must be explainable from the cycle log (INV-12).
- No real money anywhere: the shop's random draws use only in-run silver (SCP-27).
- Never add an asset without its licence in `docs/ASSETS.md`, and never download assets yourself (AGT-12).
- Edit `.tscn`/`.tres` files by hand only for small changes, and never change a `uid` (AGT-07).
- Edit `docs/SDD.md` only when the human explicitly asks. Then bump the version, update the change log, save the
  previous version as `docs/SDD_v<old>.md` first, and regenerate Appendix D with `tools/gen-requirement-index.py`.
  Otherwise put recommended SDD changes in the step report (AGT-08).
- Update `docs/PROGRESS.md` and `docs/DECISIONS.md` as part of every step (AGT-09).
- Tag every test with `[Property("Requirement", "<ID>")]` (TST-02).

## Commands (for the human to run)
```
dotnet build MistyInn.sln
dotnet test MistyInn.sln
# or, when Smart App Control blocks the test DLLs, in the .NET SDK container (also in docs/PROGRESS.md):
docker run --rm -v "${PWD}:/src:ro" mcr.microsoft.com/dotnet/sdk:10.0 bash -c "cp -r /src /work && cd /work && find . -type d \( -name bin -o -name obj -o -name .git \) -prune -exec rm -rf {} + && dotnet test tests/MistyInn.Core.Tests/MistyInn.Core.Tests.csproj"
C:\Godot_v4.7.2\Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . --quit-after 600
python tools/gen-requirement-index.py docs/SDD.md
```
