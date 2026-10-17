---
title: 'Story 2.4: Undo and reset (FR7)'
type: 'feature'
created: '2026-10-17'
status: 'in-review'
baseline_commit: 'ae3be97966aaf477f0c7e58f7efe3ee38dd5c069'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/CLAUDE.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** A reviewer can turn a cube but cannot step back or start over without a new session (FR7), and the README does not show how to try the API.

**Approach:** `POST /api/cubes/{id}/undo` and `/reset` take `{ expectedRevision }`. Two use cases (`UndoUseCase`, `ResetUseCase`) build the new move list in Application and share one revision-checked write with `ApplyMovesUseCase`, which goes through `ICubeRepository.TryUpdateAsync` (AD-5). A README section "Try the API" shows the curl commands for the TTC sequence.

## Boundaries & Constraints

**Always:** `expectedRevision` is compared with the snapshot first (`RevisionConflict` before any list is built) and passed to `TryUpdateAsync` unchanged. Undo and reset on an empty session with the empty-list revision succeed as a no-op (same revision). Missing or empty `expectedRevision` is 400 through `ResultMapper.ToBadRequest` (`[Required]` on the record parameter). One revision-check-and-write implementation, one error mapping, one log place (`ErrorReporter`). No new port method. No bare literals. Build with 0 warnings; every class with logic has tests in the same change.

**Never:** frontend, Swagger or type generation (3.1), `[FromServices]` refactor, `UseStatusCodePages` (4.2), value equality on `Cube`/`Cubie`/`Sticker`, a new error type or status mapping.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Undo | session with moves, current revision | 200, last move removed, `revision` of the shortened list | N/A |
| Reset | session with moves, current revision | 200, empty `moves`, empty-list `revision`, Solved State | N/A |
| Empty session | no moves, empty-list revision, undo or reset | 200, Solved State, revision unchanged | N/A |
| Stale revision | `expectedRevision` differs from current | 409 `ErrorDetails`, session unchanged, one Warning | `RevisionConflict` before any list is built |
| Race | two parallel requests, same revision | exactly one 200, one 409 | `TryUpdateAsync` returns `RevisionConflict` |
| Bad input | `expectedRevision` missing/empty/null, body `{}` | 400 generic `ErrorDetails`, session unchanged | model validation to `ToBadRequest` |
| Unknown or malformed id | no such session / `abc` | 404 / 400 | `CubeNotFound` / model binding |

</frozen-after-approval>

## Code Map

- `src/Rubik.Application/Cubes/ApplyMovesUseCase.cs` -- holds today the get, compare, build, `TryUpdateAsync`, outcome switch; move that into a shared `RevisionCheckedUpdate` and keep its constructor `(ICubeRepository)` and behaviour (tokens parsed first).
- `src/Rubik.Application/Cubes/{UndoUseCase,ResetUseCase,RevisionCheckedUpdate}.cs` (new). Undo builds `snapshot.Moves.SkipLast(1)`, reset builds an empty list.
- `src/Rubik.Api/Cubes/{ExpectedRevisionRequest,CubesController}.cs` -- one request record for both actions; two thin actions (`Undo`, `Reset`, operations `Cubes.Undo`, `Cubes.Reset`); `Program.cs` registers both use cases. `ResultMapper` needs no change.
- `README.md` -- add "Try the API" (port 5080, `dotnet run --project src/Rubik.Api`, curl for create, moves `F R' U B' L D'`, undo, reset); point at the expected view already in the README instead of a third copy.
- `tests/Rubik.Api.Tests/Rubik.Api.Tests.csproj` -- include `..\..\README.md`; new `ReadmeTests` check the README holds the fixture's exploded view and sequence tokens (takes the README-versus-fixture deferred entry).
- Tests: `tests/Rubik.Application.Tests/Cubes/{UndoUseCase,ResetUseCase,RevisionCheckedUpdate}Tests.cs`; `CubesControllerTests` (undo/reset, 409 with one Warning, parallel 200/409 for undo, 400 inputs, 404, malformed id, 500); existing `ApplyMovesUseCaseTests` must stay green. `CubeRepositoryTestHelper` and `ThrowingCubeRepositoryTestHelper` are reusable.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- delete the value-equality entry and the README-versus-fixture entry; `sprint-status.yaml`.

## Tasks & Acceptance

**Execution:**
- [x] Application: `RevisionCheckedUpdate`, `UndoUseCase`, `ResetUseCase`, `ApplyMovesUseCase` reuses the shared write + tests -- AD-5, AD-6
- [x] Api: `ExpectedRevisionRequest`, controller actions, DI + integration tests for the whole matrix -- AD-6, AD-7, AD-12
- [x] README "Try the API" + `ReadmeTests` + csproj include
- [x] `deferred-work.md` entries removed; `sprint-status.yaml` in-progress then review

**Acceptance Criteria:**
- All acceptance criteria of Story 2.4 in `_bmad-output/planning-artifacts/epics.md` hold.
- `dotnet build` has 0 warnings and `dotnet test` is green.

## Implementation Notes

Deferred entries checked: (1) README versus fixture: taken (deleted from `deferred-work.md`). `ReadmeTests` check that the README holds the fixture's exploded view as one block and the fixture's tokens in order in the moves command; the README is copied to the test output through `Rubik.Api.Tests.csproj`. The README keeps its one copy of the expected view; "Try the API" points at it instead of adding a third. (2) Value equality on `Cube`/`Cubie`/`Sticker`: not needed and deleted. Undo and reset work on the move list and its revision, and `RevisionCheckedUpdate` compares revisions, never cube states, so no production code compares cubes. (3) `UseStatusCodePages` and `[FromServices]`: left for 4.2 as instructed; the controller constructor now takes five use cases plus the mapper, which the `[FromServices]` entry already anticipated.

`ApplyMovesUseCase` lost its inline get/compare/write and now delegates to the new `RevisionCheckedUpdate` (DRY: three operations, one protocol); its public behaviour and tests are unchanged. The three use cases take `RevisionCheckedUpdate` through their constructor (registered scoped in `Program.cs`) and no longer receive the repository, which only `RevisionCheckedUpdate` uses; it is a concrete class like the other Application collaborators, with no interface, because it has one implementation and lives in the same layer as its callers. Undo and reset go through `TryUpdateAsync` like apply; an empty-session no-op still performs the (equal-content) write, which keeps one code path.

## Spec Change Log

## Review Triage Log

| Finding (layer) | Verdict | Route | Evidence |
|---|---|---|---|
| README token check matches `"R` inside `R'`, shell-quoted apostrophes hard to read and paste (Blind, Edge) | low | patch | README uses a quoted heredoc with plain JSON; test asserts the exact `"moves":[...]` array from the fixture |
| Missing/`null` body on undo and reset untested (Blind, Edge) | low | patch | Cases `null` and empty body added to the 400 theory |
| Lazy `buildMoves` sequence kept by `CubeSession` (Edge) | false | rejected | `CubeSession` copies with `ToArray()` in its constructor |
| Empty-session undo/reset still writes; parallel no-ops both return 200 (Blind, Edge, Gap) | low | rejected | AC requires a successful no-op; one code path; a write of equal content is harmless |
| Duplicated test constants and `CreateSessionAsync` across use case tests (Blind) | low | rejected | Same shape as `ApplyMovesUseCaseTests`; a helper adds more than it saves |
| Bare JSON bodies in `InlineData` (Blind) | low | rejected | Input data that defines the case is allowed; same as the 2.3 theory |
| Controller constructor grows; `[FromServices]` now (Blind) | low | rejected | Explicitly kept for 4.2 by the story brief |
| Inject `RevisionCheckedUpdate` through DI (Blind) | low | rejected | KISS: no second implementation, use cases stay constructible from a repository |
| Ordinal compare spelled out, 415 and wrong content type (Edge, Blind) | low | rejected | `string !=` is ordinal; 405/415 stay in the existing deferred entry |
| README does not describe 400/404, whitespace revision, extra tests (Blind) | low | rejected | Not in the AC; whitespace revision gives 400 or 409 already |
| Task checkboxes unchecked, value-equality note wording (Blind) | low | patch | Checkboxes ticked; the note says "not needed and deleted" |

Second pass, bmad-code-review (Blind, Edge, Gap, Auditor):

| Finding (layer) | Verdict | Route | Evidence |
|---|---|---|---|
| `ApplyMovesUseCase.ExecuteAsync` became non-async, so a throw in the parse loop no longer faults the Task (Edge) | low | patch | Method is `async` again |
| Ternary on the outcome inside a test assertion (Blind) | low | patch | Split into two facts |
| DRY bend in test fixtures has no one-line comment (Blind, Auditor) | low | patch | Comment added in the three test classes |
| README: bash-only, placeholders, what to do after 409 (Blind) | low | patch | One sentence each added |
| Empty-session undo/reset writes; parallel no-ops both 200 (Edge, Blind, Gap, Auditor) | low | rejected | The AC defines a successful no-op; same grounds as the first pass |
| Parallel test may not interleave (Auditor, Blind) | low | rejected | Same result either way for a non-empty session; `ForcedUpdateOutcome` tests cover the branches |
| README tests: whitespace brittleness, File.Exists message, empty fixture, port and curl not verified (Blind, Edge, Auditor) | low | rejected | Failure prints the diff; the fixture is non-empty by construction; port and curl syntax are not checkable by the AC |
| Duplicated test helpers, bare `InlineData` bodies, `-1` cast, `Range(0, n)`, operation names repeated in tests (Blind, Auditor) | low | rejected | Named const for the cast; case-defining data; the operation name comes from controller and action, no production constant exists |
| No `:guid` constraint, constructor size, repeated `ProducesResponseType`, DI for `RevisionCheckedUpdate`, retry idempotency, whitespace revision, 415 (Blind) | low | rejected | Design from 2.2/2.3, or 4.2 (`[FromServices]`, 415 entry); not in the 2.4 AC |
| sprint-status still `in-progress` (Blind, Auditor) | false | rejected | Set to `review` once the review is handled |

## Design Notes

Undo and reset work on the move list and the revision, never on cube states, so `Cube` needs no value equality (the deferred entry is dropped, not taken). `RevisionCheckedUpdate.ExecuteAsync(cubeId, expectedRevision, Func<IReadOnlyList<Move>, IEnumerable<Move>> buildMoves, ct)` returns `Result<CubeState>`; the delegate runs only after the revision matched the snapshot.

## Verification

**Commands:**
- `dotnet build` -- expected: 0 warnings, 0 errors
- `dotnet test` -- expected: all green
