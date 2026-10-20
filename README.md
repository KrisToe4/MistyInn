# Misty Inn

A dark roguelike autobattler about an inn that stands where hostile planes of reality meet.

## Concept

The inn sits at a **nexus of converging planes**. When the mist rolls in, the planes open, and what walks out of them
is dangerous: drowned things, ash-born brutes, hungry pale courtiers, rusted machines. Each one is a fuse.

You are the innkeeper, and you cannot fight them. At **Dusk** you prepare the inn: hire staff from a random shop of
drifters (companions and servers, gamblers, fighters, entertainers), upgrade them when they turn up again, buy items
for them and for the inn, and place the bar, the hearth, the stage, lanterns, wards and barricades. Then the **Mist**
comes and the night plays out on its own while you watch. Creatures that stay occupied until **Dawn** fade back into
their planes and leave a tribute. Creatures that don't turn violent: they wreck the inn, prey on the other guests and
your staff, and finally hunt you.

Each run is ten cycles, ending in the Convergence. What you learn about each creature (what calms it, what sets it
off) is kept in the bestiary from run to run, and so is what you discover about your staff's hidden ancestry.

Content: Mature. Moderate blood and suggestive themes; both can be reduced in Settings. See SDD section 17.

## Status

The design is complete (SDD v1.0). There is no game code yet: development starts at step 0 of the SDD (section 21),
one step at a time.

## Design documents

- [docs/SDD.md](docs/SDD.md): the software design document (vision, rules, systems, content rating, implementation
  steps, requirement index, glossary). It is the source of truth.
- [docs/DECISIONS.md](docs/DECISIONS.md): recorded design decisions
- [docs/PROGRESS.md](docs/PROGRESS.md): step status and results
- [CLAUDE.md](CLAUDE.md): working rules for coding agents

## Tech

- **Engine:** Godot 4.7.2 (.NET build)
- **Language:** C# (`net8.0`, `Godot.NET.Sdk/4.7.2`)
- **IDE:** Visual Studio (`MistyInn.sln`) or the Godot editor
- **Tests:** NUnit, on the engine-free Core library (from step 1)

## Opening the project

1. Extract `Godot_v4.7.2-stable_mono_win64.zip` somewhere permanent, for example `C:\Tools\Godot`.
2. Start `Godot_v4.7.2-stable_mono_win64.exe`, choose **Import**, and select this folder's `project.godot`.

There is no main scene until step 2. To build and test the C#:

```bash
dotnet build MistyInn.sln
```

```bash
dotnet test MistyInn.sln
```

## Layout

```
project.godot          Godot project settings
MistyInn.csproj/.sln   Godot C# project and solution (open the .sln in Visual Studio)
Directory.Build.props  Shared build settings for Core and the tests
.editorconfig          Code style
icon.svg               Project icon
src/MistyInn.Core/     Engine-free game rules (no Godot reference)
tests/MistyInn.Core.Tests/  NUnit tests for Core
docs/                  SDD (and earlier versions), decisions, progress
tools/                 gen-requirement-index.py (regenerates SDD Appendix D)
```

The target layout (Core library, tests, scenes, data, assets) is in SDD section 3.
