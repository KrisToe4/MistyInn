# PROGRESS

| Step | SDD version | Date | Status | Build | Tests | Smoke run | Notes |
|---|---|---|---|---|---|---|---|
| Skeleton | none | 2026-10-19 | Removed (D-034) | Passed (agent, before AGT-05 existed) | none | Passed (agent, before AGT-05 existed) | Pre-SDD third-person prototype; removed before step 0 |
| SDD | 0.1 | 2026-10-19 | Superseded | n/a | n/a | n/a | First draft (cute inn management); kept as `docs/SDD_v0.1.md` |
| SDD | 0.2 | 2026-10-20 | Superseded | n/a | n/a | n/a | Review points 1 and 2; kept as `docs/SDD_v0.2.md` |
| SDD | 1.0 | 2026-10-20 | Approved | n/a | n/a | n/a | Initial version: 248 requirements, decisions D-001 to D-034 |
| 0 | 1.0 | | Not started | | | | Repository inspection |
| 1 | 1.0 | | Not started | | | | Solution structure and Core skeleton |
| 2 | 1.0 | | Not started | | | | Board, cycle loop, isometric view |
| 3 | 1.0 | | Not started | | | | Content data and validation |
| 4 | 1.0 | | Not started | | | | Agitation, techniques and the bestiary |
| 5 | 1.0 | | Not started | | | | Escalation, combat, Company and content filters |
| 6 | 1.0 | | Not started | | | | Shop, ranks, items and synergies |
| 7 | 1.0 | | Not started | | | | Roguelike layer |
| 8 | 1.0 | | Not started | | | | Saves, profile and settings |
| 9 | 1.0 | | Not started | | | | UI pass |
| 10 | 1.0 | | Not started | | | | Art and audio pass |
| 11 | 1.0 | | Not started | | | | Audit, export and traceability |

## Toolchain

| Tool | Version | Source |
|---|---|---|
| Godot | 4.7.2 stable, .NET (`Godot_v4.7.2-stable_mono_win64`) | Downloaded by the human |
| Godot.NET.Sdk | 4.7.2 | `MistyInn.csproj` Sdk attribute; must equal the editor version (STR-01) |
| .NET SDK | 10.0.400, 10.0.402 installed | `dotnet --list-sdks` |
| Target framework | `net8.0` | Godot default |

## Packages

None yet. Test packages are added in step 1 with `dotnet add` (STR-09), and their resolved versions are recorded here.
