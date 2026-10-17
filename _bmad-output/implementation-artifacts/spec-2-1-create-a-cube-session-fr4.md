---
title: 'Story 2.1: Create a Cube Session (FR4)'
type: 'feature'
created: '2026-10-17'
status: 'done'
route: 'dispatch'
baseline_commit: '29d4a0f37a174aabd77031c07aa910bec86b03a3'
review_loop_iteration: 0
context:
  - '{project-root}/CLAUDE.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** There is no API yet. A reviewer cannot create a cube and get a solved state with its own identity (FR4).

**Approach:** `POST /api/cubes` creates a session and returns 201 Created with `Location: /api/cubes/<cubeId>` and `{ cubeId, revision, moves: [], faces, explodedView }`. A use case in `Rubik.Application` generates the `cubeId` and stores `{ cubeId, moves[] }` through `ICubeRepository` (`CreateAsync`, `GetAsync`). The cube and the exploded view are rebuilt from the move list on every response (AD-4). `InMemoryCubeRepository` (Infrastructure) is registered as a singleton in Api only (AD-1). Swagger/OpenAPI and `GET /health` are served.

## Boundaries & Constraints

**Always:** Domain references no package and no project; the dependency-direction test stays green. Controller stays thin (call one use case, map to the DTO). No bare literals in logic or tests. Build with 0 warnings; every class with logic has tests in the same change.

**Never:** GET of a session, `ErrorDetails`, `Result<T>`, `TryUpdateAsync`, applying moves, revisions beyond the empty list, undo, reset (2.2 to 2.4); frontend; the TTC fixture wiring (nothing in 2.1 needs it).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Create | `POST /api/cubes` | 201, `Location: /api/cubes/<cubeId>`, body with new Guid, `revision` of an empty move list, `moves` [], solved `faces` and `explodedView` | N/A |
| Two creates | two `POST /api/cubes` | different `cubeId`; each stored session is separate | N/A |
| Health / docs | `GET /health`, `GET /swagger/v1/swagger.json` | 200 | N/A |

</frozen-after-approval>

## Code Map

- `src/Rubik.Domain/Cubes/Move.cs` -- `FacesByLetter` table; add `ToNotation()` here (letter table stays in one place).
- `src/Rubik.Domain/Cubes/{Cube,ExplodedViewRenderer}.cs` -- `Cube.Apply(Move)` returns a new cube; `Render(Cube)` returns `ExplodedView(Faces, Lines)`. Reuse; do not change.
- `src/Rubik.Application/` -- empty project (references Domain); new `Cubes/` folder.
- `src/Rubik.Infrastructure/` -- empty project (references Application).
- `src/Rubik.Api/Program.cs` -- empty; composition root. Needs `public partial class Program` for `WebApplicationFactory`.
- `tests/Rubik.{Application,Infrastructure}.Tests/*.csproj` -- carry a "remove with the first test" `--ignore-exit-code 8` KISS exception; remove it.
- `tests/Rubik.Api.Tests/Architecture/` -- must stay green.
- `Directory.Packages.props` -- add Swashbuckle 10.3.0 and Mvc.Testing 10.0.13 (spine versions).

## Tasks & Acceptance

**Execution:**
- [x] `src/Rubik.Domain/Cubes/Move.cs`, `MoveTests.cs` -- add `ToNotation()` (round trip with `TryParse` for all 12 moves) -- moves must go on the wire; taken from the 1.3 deferred entry
- [x] `src/Rubik.Application/Cubes/` `CubeSession`, `CubeState` (read model), `ICubeRepository`, `CreateCubeUseCase` -- session = id + moves; the revision is derived from the moves (`CubeRevision`); state replays moves on a solved cube and renders; use case generates the Guid and calls `CreateAsync` -- AD-4, AD-5
- [x] `src/Rubik.Infrastructure/Cubes/InMemoryCubeRepository.cs` -- `ConcurrentDictionary`, stores a copy of the moves -- AD-5
- [x] `src/Rubik.Api/` `Cubes/CubesController.cs`, `Program.cs`, `Properties/launchSettings.json` -- thin POST returning the Application `CubeState` (camelCase JSON, face keys `up left front right back down`, colour names), singleton repository, Swagger, health, port 5080 -- AD-1, AD-7
- [x] tests in all four test projects, mirroring production folders -- unit tests for use case, session, state (wire shape), repository; integration tests for endpoint, health, swagger, singleton lifetime
- [x] `deferred-work.md`, `sprint-status.yaml` -- trim taken entry, status updates

**Acceptance Criteria:**
- Given the API, when `POST /api/cubes`, then 201 with the Location header and the solved body.
- Given two creates, then the ids differ and each is stored independently.
- Given the code, then the use case is in Application behind `ICubeRepository`, the controller is thin, the repository is a singleton, and `dotnet build` has 0 warnings and `dotnet test` is green.

## Implementation Notes

Revision: `CubeRevision.Of(moves)` is the SHA-256 (lower-case hex) of the notation tokens joined by a space; the empty list gives the SHA-256 of the empty text. `CubeSession` has no version field. The wire field is `revision` (text), replacing `version`; AD-4/5/6/7/9 and the Epic 2 stories in `epics.md` were updated. PRD and addendum were updated too (Version became Revision).

Design: `Rubik.Api` and `Rubik.Infrastructure` must not reference `Rubik.Domain` (AD-1; the architecture test caught it when `Api` used `Face`/`Move`). So `CubeState` in Application is the read model with no Domain types (notation strings, lower-case face keys, colour names) and the controller returns it as is; there is no separate Api DTO. `CubeSession` copies its moves in its constructor, so a stored session cannot be changed through the caller's list.

Deferred entries taken: the notation formatter half of the Story 1.3 entry (needed so `moves` can be serialised; sequence-level parsing stays deferred for 2.3, the entry is shortened accordingly). Left alone: `PackageReference` check (4.2), `Cube` equality (2.4), README-vs-fixture (4.2), fixture wiring for API tests (the solved state needs no TTC fixture; wire it in 2.3 when the TTC sequence is posted).

## Spec Change Log

- After review, Drago asked to replace the integer `version` (= move count) by a text `revision` derived from the move list: a count can return to an earlier number after undo and new moves, so a stale client could write silently (see Epic 2 AD-4/AD-5 edits). The frozen Intent, the I/O row and the Never list were reworded to match. KEEP: `CubeRevision.Of` is the only place that computes it; `CubeSession` stores moves only.

## Review Triage Log

| Finding (layer) | Verdict | Route | Evidence |
|---|---|---|---|
| Bare `2` in `CreateCubeUseCaseTests` (Blind, Edge, Auditor) | low | patch | Named `CreatedSessionCount` |
| Tasks still named `CubeResponse` (Edge, Auditor) | low | patch | Task text updated; design note already in Implementation Notes |
| `ToNotation` test not a round trip (Blind, Auditor) | low | patch | Added `TryParse_ThenToNotation_ReturnsTheSameToken` |
| `CubeSession` record compares `Moves` by reference (Blind, Edge, Auditor) | low | patch | Now a `sealed class` (reference equality, honest) |
| `ToNotation` with undefined Face/Direction (Blind, Edge) | low | rejected | `Move` values come from `TryParse` or the enums; no path builds an undefined one |
| Swagger served in every environment (Blind, Edge) | low | patch | Initially rejected; after review by Drago, Swagger and its UI are now Development-only, with a test for 200 in Development and 404 in Production |
| Location points at a GET that does not exist yet (Blind) | false | rejected | Story 2.2 adds `GET /api/cubes/{id}`; Location format is fixed by the AC |
| Duplicate-id exception becomes an unmapped 500 (Blind, Edge) | low | rejected | Guid collision unreachable; the global handler is Story 2.2 |
| Unbounded sessions, no cancellation check, no concurrency test (Blind, Edge) | low | rejected | Unbounded store is a recorded v1 deferral in the spine; the in-memory calls are synchronous |
| Repeated string literals in tests, shared factory state, controller not sealed, helper vs real adapter (Blind, Edge) | low | rejected | Expected-value data is allowed by CLAUDE.md; no count-based assertions; no behavioural harm |
| Wire format not pinned by schema (Blind, Auditor) | low | rejected | Integration test pins names and values over HTTP |
| Gap layer | - | - | No gaps found |
