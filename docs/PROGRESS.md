# PROGRESS

| Step | SDD version | Date | Status | Build | Tests | Smoke run | Notes |
|---|---|---|---|---|---|---|---|
| Skeleton | none | 2026-10-19 | Removed (D-034) | Passed (agent, before AGT-05 existed) | none | Passed (agent, before AGT-05 existed) | Pre-SDD third-person prototype; removed before step 0 |
| SDD | 0.1 | 2026-10-19 | Superseded | n/a | n/a | n/a | First draft (cute inn management); kept as `docs/SDD_v0.1.md` |
| SDD | 0.2 | 2026-10-20 | Superseded | n/a | n/a | n/a | Review points 1 and 2; kept as `docs/SDD_v0.2.md` |
| SDD | 1.0 | 2026-10-20 | Superseded | n/a | n/a | n/a | Initial version: 248 requirements, decisions D-001 to D-034; kept as `docs/SDD_v1.0.md` |
| SDD | 1.1 | 2026-10-20 | Superseded | n/a | n/a | n/a | Step 0 recommendations: `.sln` kept with configuration mapping (STR-01), Godot path in PROGRESS (TST-12), export preset created in step 1 (D-035, D-036); kept as `docs/SDD_v1.1.md` |
| SDD | 1.2 | 2026-10-20 | Superseded | n/a | n/a | n/a | Step 1 recommendation: smoke run starts at step 2 (TST-12); D-037 to D-041 accepted; kept as `docs/SDD_v1.2.md` |
| SDD | 1.3 | 2026-10-20 | Approved | n/a | n/a | n/a | Step 2 recommendations: SIM-02 (Core's `Tick` runs the fixed steps), ARC-10 (`ContinueFromDawn`), BRD-03 (every room reachable, D-050); D-042 to D-049 accepted |
| 0 | 1.0 | 2026-10-20 | Complete | NOT RUN (read-only step) | NOT RUN | NOT RUN | Read-only inspection: repository matches the v1.0 shell; gaps are step 1 work (STR-02..08, STR-11); risks: solution configuration mapping, `.sln` vs `.slnx`, Smart App Control may block test DLLs |
| 1 | 1.1 | 2026-10-20 | Complete | Passed: Core and tests (human, .NET SDK 10 container); Godot project (human, Godot 4.7.2 editor) | 49/49 passed (human, container, 72 ms) | n/a (no main scene until step 2; TST-12 as of v1.2) | Core library and NUnit project, shared build settings, `.sln` with build type mapping, `.gitignore` fixed; Windows Desktop export preset created by the human (path `builds/windows/MistyInn.exe`, includes `data/*.json`); decisions D-037 to D-041 accepted |
| 2 | 1.2 | 2026-10-20 | Complete (play-through passed, human) | Passed (human): `dotnet build` after fixing nullable errors in two tests, and the Godot editor build | 145/145 passed (human, container, 467 ms) after one test fix (NUnit 5 `Is.SubsetOf` counts duplicates) | Not reported separately | Board, A*, Dusk/Mist/Dawn loop, arrivals, wandering, dawn, determinism; isometric camera, board and unit views, drag-and-drop placement, speed, pause menu; decisions D-042 to D-049 |
| 3 | 1.3 | | Not started | | | | Content data and validation |
| 4 | 1.3 | | Not started | | | | Agitation, techniques and the bestiary |
| 5 | 1.3 | | Not started | | | | Escalation, combat, Company and content filters |
| 6 | 1.3 | | Not started | | | | Shop, ranks, items and synergies |
| 7 | 1.3 | | Not started | | | | Roguelike layer |
| 8 | 1.3 | | Not started | | | | Saves, profile and settings |
| 9 | 1.3 | | Not started | | | | UI pass |
| 10 | 1.3 | | Not started | | | | Art and audio pass |
| 11 | 1.3 | | Not started | | | | Audit, export and traceability |

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
