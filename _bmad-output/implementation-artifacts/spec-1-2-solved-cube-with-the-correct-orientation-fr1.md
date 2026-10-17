---
title: 'Story 1.2: Solved cube with the correct orientation'
type: 'feature'
created: '2026-10-17'
status: 'done'
baseline_commit: '4d639c15e8769228c17d4f86d0ad453e28a34428'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/CLAUDE.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The Domain is empty. Every later story (parsing, rotation, rendering) needs a cube model that starts solved in the standard orientation, so a reviewer can trust the starting point of every sequence.

**Approach:** Add `Cube` to `Rubik.Domain` as 26 cubies at coordinates in {-1,0,1}, each sticker carrying a normal vector and a colour (AD-2). A new `Cube` is solved: Green front, Red right, White up, Blue back, Orange left, Yellow down.

## Boundaries & Constraints

**Always:** Domain references no package and no other project. Namespace `Rubik.Domain.Cubes`; one public type per file. Immutable model (the cube exposes no setters). Zero warnings; `dotnet test` green. The `--ignore-exit-code 8` line is removed from `Rubik.Domain.Tests.csproj` together with the first test.

**Never:** rotation, move parsing, `ExplodedViewRenderer` or any layout/net-orientation logic on `Cube` (AD-3; stories 1.3 to 1.5). No permutation tables. No Application, Api or frontend changes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Solved faces | `new Cube()`, stickers whose normal is the Front/Right/Up/Back/Left/Down unit vector | Nine stickers per face; all Green / Red / White / Blue / Orange / Yellow | N/A |
| Cubie set | `new Cube()` | 26 cubies, distinct positions, each coordinate in {-1,0,1}, none at (0,0,0) | N/A |
| Sticker normals | Any cubie | One sticker per non-zero coordinate; its normal is that axis unit vector, signed like the coordinate | N/A |
| Totals | `new Cube()` | 54 stickers: 8 corner cubies x3, 12 edge x2, 6 centre x1 | N/A |

</frozen-after-approval>

## Code Map

- `src/Rubik.Domain/Rubik.Domain.csproj` -- empty project; no change.
- `tests/Rubik.Domain.Tests/Rubik.Domain.Tests.csproj` -- remove the `PropertyGroup` with `--ignore-exit-code 8` and its comment.
- `tests/Rubik.Api.Tests/` `ArchitectureTests` -- already guards Domain isolation; must stay green.
- Axes: x = Right(+)/Left(-), y = Up(+)/Down(-), z = Front(+)/Back(-). Spine AD-2 fixes the model, not the axes; this choice is recorded here for 1.4.

## Tasks & Acceptance

**Execution:**
- [x] `src/Rubik.Domain/Cubes/Vector3.cs` -- `readonly record struct Vector3(int X, int Y, int Z)` -- shared by positions and normals (DRY)
- [x] `src/Rubik.Domain/Cubes/Color.cs` -- enum White, Yellow, Green, Blue, Red, Orange
- [x] `src/Rubik.Domain/Cubes/Face.cs` -- enum Up, Down, Left, Right, Front, Back, plus an extension giving its unit-vector normal; the one place that maps face to normal
- [x] `src/Rubik.Domain/Cubes/Sticker.cs` -- `record Sticker(Vector3 Normal, Color Color)`
- [x] `src/Rubik.Domain/Cubes/Cubie.cs` -- `record Cubie(Vector3 Position, IReadOnlyList<Sticker> Stickers)`
- [x] `src/Rubik.Domain/Cubes/Cube.cs` -- public constructor builds the solved cube; `IReadOnlyList<Cubie> Cubies`; solved colour per face lives here
- [x] `tests/Rubik.Domain.Tests/Cubes/CubeTests.cs` -- one test per matrix row; face colours asserted against hard-coded expectations, independent of production mapping
- [x] `tests/Rubik.Domain.Tests/Cubes/FaceTests.cs` -- each face maps to the expected unit normal, all six distinct
- [x] `tests/Rubik.Domain.Tests/Rubik.Domain.Tests.csproj` -- drop `--ignore-exit-code 8`

**Acceptance Criteria:**
- Given a new `Cube`, when I read its six faces, then Front is all Green, Right all Red, Up all White, Back all Blue, Left all Orange and Down all Yellow, and each face has nine tiles.
- Given the cube model, when I inspect it in tests, then it is built from 26 cubies with coordinates in {-1,0,1} and a normal vector per sticker.
- Given the Domain project, when its references are checked, then it still references no package and no other project.

## Implementation Notes

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| `Cubies` and `Stickers` are mutable `List<T>` behind `IReadOnlyList`, downcast can corrupt the cube (Blind, Edge, Gap) | medium | patch | Contradicts "immutable model". Fixed with `AsReadOnly()`; build 0 warnings, 27 tests green |
| `Cubie`/`Sticker` record equality compares lists by reference (Blind, Edge, Gap) | low | rejected | No current code compares cubies; equality semantics belong to 1.4 where rotation tests need them; a fix adds public surface |
| No validation in `Cubie` (range, unit normals, null) (Blind, Edge) | low | rejected | Speculative; only `Cube` builds cubies; invariants are asserted by tests |
| `SolvedColors` dictionary order and missing-face silence (Edge) | low | rejected | Order unused; the six-face theory fails if a mapped face loses stickers |
| Colour scheme not tied to `Face` enum, mapping private (Blind) | low | rejected | Tested via normals; API and UI never recompute it (AD-3, AD-9) |
| Brittle test code: `ToString()` sort, `byStickerCount[3]` KeyNotFound, untested default throw (Blind) | low | rejected | Fail loudly if wrong; fixes add complexity |
| No `IsSolved`, `Color` name clash, other test projects keep `--ignore-exit-code 8` (Blind) | false | rejected | Out of intent; each exception is removed with its own first test |
| Missing `using System.Linq` (Edge) | false | rejected | `ImplicitUsings` is enabled in `Directory.Build.props`; tests compile and run |

## Design Notes

`Cube` exposes only cubies; "reading a face" in tests means filtering stickers by normal. That is a query, not a layout, so AD-3 holds: row/column mapping stays in the renderer (1.5). `Face` is introduced now because the solved colours are defined per face and `Move` (1.3) will reuse it.

## Verification

**Commands:**
- `dotnet build` -- expected: Build succeeded, 0 Warning(s), 0 Error(s)
- `dotnet test` -- expected: all projects pass, exit code 0; Domain.Tests runs real tests
- `git status --short` -- expected: only intended files
