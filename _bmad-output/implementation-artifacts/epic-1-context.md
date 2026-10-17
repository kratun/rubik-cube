# Epic 1 Context: Verifiable cube engine with exploded view

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

A reviewer can run the tests and see that the TTC Test Sequence `F R' U B' L D'` on a solved cube yields exactly the required nine-line exploded view. The epic also scaffolds the .NET solution that every later epic builds on.

## Stories

- Story 1.1: Solution scaffold with a clean build
- Story 1.2: Solved cube with the correct orientation
- Story 1.3: Strict move parsing
- Story 1.4: Turn any face
- Story 1.5: Exploded view and the TTC Test Sequence

## Requirements & Constraints

- A new cube is solved: Green front, Red right, White up, Blue back, Orange left, Yellow down; nine tiles per face.
- Any face turns 90 degrees either way; four identical turns, or a turn plus its inverse, restore the previous state.
- The solved cube and the TTC sequence render as an exploded view (Up on top; Left, Front, Right, Back in one row; Down below); the TTC output is fixed and must match exactly.
- Builds on Windows with free tools only, .NET 10; the engine depends on nothing else (no API, UI, packages).
- Nullable reference types on, warnings as errors, zero warnings; no demo code or template leftovers.
- Every class or module with logic has tests in the same change.

## Technical Decisions

- Hexagonal layout: Domain (no references) <- Application <- Infrastructure and Api; Api -> Application, Infrastructure; Infrastructure -> Application. A reflection-based test in `Rubik.Api.Tests` fails on any other reference edge. Only Api registers concrete adapters.
- Four `src` projects (`Rubik.Domain`, `Rubik.Application`, `Rubik.Infrastructure`, `Rubik.Api`), each with a `*.Tests` project under `tests/`. Namespaces mirror projects (`Rubik.Domain.Cubes`); one public type per file; tests named `{Class}Tests`, methods `Method_Condition_Result`.
- Build base: `global.json` pins SDK 10.0.x; `Directory.Build.props` sets Nullable and TreatWarningsAsErrors; `Directory.Packages.props` holds all package versions centrally. Pinned versions: SDK 10.0.302, xunit.v3 4.0.2, Mvc.Testing 10.0.13, Swashbuckle 10.3.0, Diagnostics.Testing 10.11.0.
- Engine: 26 cubies at coordinates in {-1,0,1}, each sticker with a normal vector; one shared rotation function parameterised by face and direction; no permutation tables.
- `ExplodedViewRenderer` in Domain produces both the `faces` grids (row-major, net orientation) and the `explodedView` text lines; the cube exposes no layout.
- Moves are parsed strictly by `Move.TryParse` (tokens `F R U B L D`, optional `'`, case-sensitive; invalid input returns false, never throws).
- SOLID, DRY, KISS; a justified exception carries a one-line comment naming the principle.

## Cross-Story Dependencies

- 1.1 is the base for every later story; 1.2 to 1.5 build the Domain in order (cube, parsing, rotation, rendering).
- The TTC expected output is a shared fixture reused in later API and UI tests.
- The README grows: 1.1 creates it (prerequisites, build and test commands); 1.5 adds the "Run the tests" section with the expected output.
