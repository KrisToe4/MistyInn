# PROGRESS

| Step | SDD version | Date | Status | Build | Tests | Smoke run | Notes |
|---|---|---|---|---|---|---|---|
| Skeleton | none | 2026-10-19 | Removed (D-034) | Passed (agent, before AGT-05 existed) | none | Passed (agent, before AGT-05 existed) | Pre-SDD third-person prototype; removed before step 0 |
| SDD | 0.1 | 2026-10-19 | Superseded | n/a | n/a | n/a | First draft (cute inn management); kept as `docs/SDD_v0.1.md` |
| SDD | 0.2 | 2026-10-20 | Superseded | n/a | n/a | n/a | Review points 1 and 2; kept as `docs/SDD_v0.2.md` |
| SDD | 1.0 | 2026-10-20 | Superseded | n/a | n/a | n/a | Initial version: 248 requirements, decisions D-001 to D-034; kept as `docs/SDD_v1.0.md` |
| SDD | 1.1 | 2026-10-20 | Superseded | n/a | n/a | n/a | Step 0 recommendations: `.sln` kept with configuration mapping (STR-01), Godot path in PROGRESS (TST-12), export preset created in step 1 (D-035, D-036); kept as `docs/SDD_v1.1.md` |
| SDD | 1.2 | 2026-10-20 | Superseded | n/a | n/a | n/a | Step 1 recommendation: smoke run starts at step 2 (TST-12); D-037 to D-041 accepted; kept as `docs/SDD_v1.2.md` |
| SDD | 1.3 | 2026-10-20 | Superseded | n/a | n/a | n/a | Step 2 recommendations: SIM-02 (Core's `Tick` runs the fixed steps), ARC-10 (`ContinueFromDawn`), BRD-03 (every room reachable, D-050); D-042 to D-049 accepted; kept as `docs/SDD_v1.3.md` |
| SDD | 1.4 | 2026-10-20 | Superseded | n/a | n/a | n/a | Step 3 questions: one more technique per class and masteries (STF-02, STF-06, STF-07, STF-17), six effect kinds (DAT-03) (D-051, D-052); kept as `docs/SDD_v1.4.md` |
| SDD | 1.5 | 2026-10-20 | Superseded | n/a | n/a | n/a | Step 3 recommendations: SIM-04 and TST-07 (no scene markers), STF-02 (`needsFixture`), DAT-04 (whole-set fallback), step 4 providers for Order and Darkness; D-054 to D-060 accepted; kept as `docs/SDD_v1.5.md` |
| SDD | 1.6 | 2026-10-20 | Superseded | n/a | n/a | n/a | Step 4 questions: Shadowed alcove and Ledger desk, Cards and Tales answer Order and Darkness, the door's Cold draught, the starting kit in storage, fixture shapes and seats (D-061 to D-064); kept as `docs/SDD_v1.6.md` |
| SDD | 1.7 | 2026-10-20 | Superseded | n/a | n/a | n/a | Step 4 report: INV-12 (the cycle log records causes and turning points) and STF-10 (★1 base technique until step 6); D-065 to D-068 Accepted; kept as `docs/SDD_v1.7.md` |
| SDD | 1.8 | 2026-10-20 | Superseded | n/a | n/a | n/a | Step 5 report: rooms as zones with RoomDoor tiles, integrity and repairs, Prey and Hunt targets, the innkeeper striking back, Company room rules, prefers and refuses, combat and company tuning, dev commands; D-071, D-073, D-075 and D-076 Accepted; kept as `docs/SDD_v1.8.md` |
| SDD | 1.9 | 2026-10-21 | Superseded | n/a | n/a | n/a | Step 6 report: shop and item commands, adjacentTo, room-3 and display spots, rank multiplier, pending pick, shop stream, rerolls and lock, synergy recipients, save contents; D-077 to D-083 Accepted |
| SDD | 1.10 | 2026-10-21 | Superseded | n/a | n/a | n/a | Basic service at tables (new AGI-11), `Alcohol` replaces the `Drink` and `Food` appetites, staff homes and roles (bouncers and companions stay home, runners and the innkeeper walk), left-drag on open ground pans the camera (CAM-10), selected creatures show bestiary-revealed appetites with a debug toggle for everything (UI-06), new step 6b; v1.9 kept as `docs/SDD_v1.9.md` |
| SDD | 1.11 | 2026-10-21 | Superseded | n/a | n/a | n/a | Satisfied guests pay and leave (new CRE-08), no edge panning (CAM-10), new step 6c; v1.10 kept as `docs/SDD_v1.10.md` |
| SDD | 1.12 | 2026-10-21 | Approved | n/a | n/a | n/a | Step 6c report: CRE-08 Company comes first (D-085); v1.11 kept as `docs/SDD_v1.11.md` |
| 0 | 1.0 | 2026-10-20 | Complete | NOT RUN (read-only step) | NOT RUN | NOT RUN | Read-only inspection: repository matches the v1.0 shell; gaps are step 1 work (STR-02..08, STR-11); risks: solution configuration mapping, `.sln` vs `.slnx`, Smart App Control may block test DLLs |
| 1 | 1.1 | 2026-10-20 | Complete | Passed: Core and tests (human, .NET SDK 10 container); Godot project (human, Godot 4.7.2 editor) | 49/49 passed (human, container, 72 ms) | n/a (no main scene until step 2; TST-12 as of v1.2) | Core library and NUnit project, shared build settings, `.sln` with build type mapping, `.gitignore` fixed; Windows Desktop export preset created by the human (path `builds/windows/MistyInn.exe`, includes `data/*.json`); decisions D-037 to D-041 accepted |
| 2 | 1.2 | 2026-10-20 | Complete (play-through passed, human) | Passed (human): `dotnet build` after fixing nullable errors in two tests, and the Godot editor build | 145/145 passed (human, container, 467 ms) after one test fix (NUnit 5 `Is.SubsetOf` counts duplicates) | Not reported separately | Board, A*, Dusk/Mist/Dawn loop, arrivals, wandering, dawn, determinism; isometric camera, board and unit views, drag-and-drop placement, speed, pause menu; decisions D-042 to D-049 |
| 3 | 1.4 | 2026-10-20 | Complete (demo passed, human) | Passed (Core and tests, human, container) | 197/197 passed (human, container, 710 ms; 59 new, the 7 step 2 content tests replaced) | Passed (human, `--headless --quit-after 600`) | `data/*.json` (nine files, placeholder roster and creatures), loader, validator, upgrade-tree rules, built-in copy in Core, Godot content source with debug stop and export fallback; decisions D-051 to D-059 |
| 4 | 1.6 | 2026-10-20 | Complete (play-through passed) | Passed (after a CS0234 fix in BuildTests) | Passed 238/238 (41 new) | Passed | Agitation, triggers, contagion, staff service sessions, creatures seeking service, fixtures (build menu, shapes, seats, effects), silver and supplies, tributes, bestiary and forecast; decisions D-061 to D-068 |
| 5 | 1.7 | 2026-10-20 | Complete (play-through passed) | Passed (Core and tests, container; Godot) | Passed 281/281 (43 new) | Passed | Escalation (Wreck, Prey, Hunt), combat, banishing, staff falling and the Dawn roll, the innkeeper ending the run, integrity and repairs, Company rooms, refusals, interruptions, hosting and both content filters, rooms reachable on their own, the debug dev panel; decisions D-069 to D-076 |
| 6 | 1.8 | 2026-10-20 | Build, tests and smoke run passed (human); demo play-through not reported yet | Passed (container) | First run 324/326: two older layout-data tests assumed two rooms (`InnFloorIsFourteenByTen`, `CombatCompanyAndRoomValuesAreChecked`); updated for the annex room-3 (D-078); rerun passed 326/326 (human, container, 4 s) | Passed (human) | Shop (3 + 3 offers, rank odds, pity and home-plane weights, reroll, lock, own random stream), hiring at a rank with the preset build, one-rank upgrades with the pick dialog, selling, staff items (2 slots, storage 8), inn items with display spots and effects, the annex room-3, adjacency (`adjacentTo`), synergies (`data/synergies.json`), ancestry reveal; decisions D-077 to D-083 (Proposed) |
| 6b | 1.10 | 2026-10-21 | Build, tests and smoke run passed (human); play-through not reported yet | Passed (human, 10.4 s) | Passed 336/336 (human, 9.5 s; 10 new) | Passed (human) | `Alcohol` appetite, basic service at tables (AGI-11), runners, bouncers and companions with homes (STF-04), the innkeeper serving at tables, left-drag camera pan (CAM-10), creature appetites in the selection line with a debug toggle (UI-06); decision D-084 (Accepted) |
| 6c | 1.11 | 2026-10-21 | Complete: build, tests, smoke run and demo passed (human) | Passed (human) | First run 337/341: a leaving guest kept its served-by mark (fixed), and Company guests were satisfied before their room (Company now comes first, D-085); rerun passed 341/341 (human, 7.2 s) | Passed (human) | Satisfied guests pay and walk out (CRE-08), no edge panning (CAM-10); one Dawn test keeps its guest until Dawn; decision D-085 (Accepted) |
| 7 | 1.12 | | Not started | | | | Roguelike layer |
| 8 | 1.12 | | Not started | | | | Saves, profile and settings |
| 9 | 1.12 | | Not started | | | | UI pass |
| 10 | 1.12 | | Not started | | | | Art and audio pass |
| 11 | 1.12 | | Not started | | | | Audit, export and traceability |

## Toolchain

| Tool | Version | Source |
|---|---|---|
| Godot | 4.7.2 stable, .NET, at `C:\Godot_v4.7.2` (`Godot_v4.7.2-stable_mono_win64.exe`, `Godot_v4.7.2-stable_mono_win64_console.exe`) | Installed by the human; folder confirmed in step 0 |
| Godot.NET.Sdk | 4.7.2 | `MistyInn.csproj` Sdk attribute; must equal the editor version (STR-01) |
| .NET SDK | 10.0.400, 10.0.402 | SDK folders, step 0 (confirm with `dotnet --list-sdks`) |
| .NET runtimes | 8.0.22, 8.0.25, 9.0.7, 10.0.11, 10.0.13 | Runtime folders, step 0 (confirm with `dotnet --list-runtimes`) |
| Python | On the PATH (Microsoft Store install) | Used by `tools/gen-requirement-index.py` |
| Target framework | `net8.0` (game, Core and tests) | Godot default; .NET 8 runtime installed |

## Packages

Added in step 1 with `dotnet add` (STR-09); versions as resolved by NuGet.

| Project | Package | Version | Step |
|---|---|---|---|
| MistyInn.Core.Tests | Microsoft.NET.Test.Sdk | 18.11.0 | 1 |
| MistyInn.Core.Tests | NUnit | 5.0.0 | 1 |
| MistyInn.Core.Tests | NUnit3TestAdapter | 6.3.1 | 1 |

## Running the tests in a container

When Smart App Control blocks the test DLLs on Windows, run the tests in the .NET SDK Linux container from the
repository root (PowerShell). The repository is mounted read-only and copied inside, without the Windows `bin/` and
`obj/` folders, so the two builds never mix:

```
docker run --rm -v "${PWD}:/src:ro" mcr.microsoft.com/dotnet/sdk:10.0 bash -c "cp -r /src /work && cd /work && find . -type d \( -name bin -o -name obj -o -name .git \) -prune -exec rm -rf {} + && dotnet test tests/MistyInn.Core.Tests/MistyInn.Core.Tests.csproj"
```

## Smoke-run command (TST-12)

```
C:\Godot_v4.7.2\Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . --quit-after 600
```

## Known machine risk

Windows Smart App Control blocked newly built test DLLs in the human's other project on this machine. If
`dotnet test` is blocked, run the tests in the .NET SDK Linux container instead.
