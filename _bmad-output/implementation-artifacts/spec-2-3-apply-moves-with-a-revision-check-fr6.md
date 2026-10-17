---
title: 'Story 2.3: Apply moves with a revision check (FR6)'
type: 'feature'
created: '2026-10-17'
status: 'in-review'
baseline_commit: '6d26f3ae3740f67296b35ce1e78dd9115c0420e4'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/CLAUDE.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** A cube can be created and read but not turned, and two people sharing a cube could silently overwrite each other (FR6).

**Approach:** `POST /api/cubes/{id}/moves` with `{ expectedRevision, moves }` goes through an `ApplyMovesUseCase` that parses the tokens, compares `expectedRevision` with the revision it just read, builds the new list only from that snapshot, and writes it through the atomic `ICubeRepository.TryUpdateAsync` (AD-5, AD-6). New errors `InvalidMove(token)` and `RevisionConflict` are mapped by the one `ResultMapper`.

## Boundaries & Constraints

**Always:** `expectedRevision` is passed to `TryUpdateAsync` unchanged; the in-memory compare-and-set is atomic per cube and compares `CubeSession` by reference (it stays a class). Missing or empty `expectedRevision`, missing or empty `moves` are 400 through `ResultMapper.ToBadRequest`. Only Api logs (Warning, no exception, for 400/404/409). Api and Infrastructure keep no Domain reference. No bare literals. Build with 0 warnings; every class with logic has tests in the same change.

**Never:** undo/reset (2.4); frontend; README "Try the API" (2.4); `UseStatusCodePages` (stays deferred); a second place that picks status codes or logs errors; echoing raw model-state errors.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Apply | current revision, valid tokens | 200, `CubeState` with the new `revision`, `moves`, `explodedView` | N/A |
| TTC | new session, `F R' U B' L D'` | 200, `explodedView` equals `tests/Fixtures/ttc-exploded-view.txt`, `revision` is the hash of the sequence | N/A |
| Stale revision | `expectedRevision` differs from current | 409 `ErrorDetails`, session unchanged, Warning log | `RevisionConflict` before any list is built |
| Same count, other history | client saw A B C D, session is A B X Y | 409, session unchanged | revision is a content hash |
| Race | two parallel requests, same revision | exactly one 200, one 409 | `TryUpdateAsync` returns `RevisionConflict` |
| Bad token | `"X"`, `"f"`, `"F2"`, null | 400, `detail` names the token, session unchanged | `InvalidMove(token)` |
| Empty or missing input | `moves` empty/missing, `expectedRevision` missing/empty | 400 generic `ErrorDetails`, session unchanged | model validation to `ToBadRequest` |
| Unknown or malformed id | no such session / `abc` | 404 / 400 | `CubeNotFound` / model binding |

</frozen-after-approval>

## Code Map

- `src/Rubik.Application/Cubes/ICubeRepository.cs` -- add `TryUpdateAsync` and `UpdateOutcome` (`Updated | RevisionConflict | NotFound`).
- `src/Rubik.Application/Cubes/{CubeSession,CubeState}.cs` -- session exposes `Revision` (derived once); `CubeState.From` reuses it.
- `src/Rubik.Application/Cubes/{InvalidMove,RevisionConflict,ApplyMovesUseCase}.cs` (new); `Results/Result.cs` -- null guard in `Failure`.
- `src/Rubik.Infrastructure/Cubes/InMemoryCubeRepository.cs` -- `TryUpdateAsync` via `ConcurrentDictionary.TryUpdate` (reference comparison), retry when the reference changed.
- `src/Rubik.Api/Errors/ResultMapper.cs` -- 400 for `InvalidMove`, 409 for `RevisionConflict`; `src/Rubik.Api/Cubes/{ApplyMovesRequest,CubesController}.cs`; `Program.cs` registers the use case.
- Tests: `tests/Rubik.Application.Tests/{Cubes,Results,TestHelpers}`, `tests/Rubik.Infrastructure.Tests/Cubes`, `tests/Rubik.Api.Tests/{Cubes,Errors,TestHelpers}`; `Rubik.Api.Tests.csproj` includes `..\Fixtures\*.txt`.

## Tasks & Acceptance

**Execution:**
- [ ] Application: port, errors, use case, `Result` guard + tests -- AD-5, AD-6
- [ ] Infrastructure: atomic `TryUpdateAsync` + parallel test -- AD-5
- [ ] Api: request DTO, action, mapper cases, DI + tests incl. TTC fixture, parallel 409/200, A B C D vs A B X Y -- AD-6, AD-12
- [ ] `deferred-work.md`, `sprint-status.yaml` -- remove taken entries; status in-progress then review

**Acceptance Criteria:**
- All acceptance criteria of Story 2.3 in `_bmad-output/planning-artifacts/epics.md` hold.
- `dotnet build` has 0 warnings and `dotnet test` is green.

## Implementation Notes

Deferred entries taken (deleted from `deferred-work.md`): (1) sequence-level parsing, token by token with the failing token named: done in `ApplyMovesUseCase` over the wire's token array (a string split is not needed, the wire carries an array); (2) TTC fixtures for the API tests: `Rubik.Api.Tests.csproj` includes `..\Fixtures\*.txt`, read by `TtcFixtureTestHelper`; (3) `Result<T>.Failure(null)` guard, with a test. Not taken: `UseStatusCodePages` for unmatched routes, 405 and 415 (2.3's design does not need it); the entry stays.

Deviation from AD-5 wording: the port is `TryUpdateAsync(expectedRevision, CubeSession replacement, ct)`, not `(cubeId, expectedRevision, newMoves)`. A signature naming `Move` makes the Infrastructure assembly reference Domain, which the architecture test forbids (found by that test). The id and the new moves travel in the replacement session; `expectedRevision` is still the caller's value, unchanged.

`CubeSession` now exposes `Revision`, derived once in the constructor, so Infrastructure compares revisions without touching `CubeRevision` or `Move`. Tokens are parsed before the session is read, so a bad token on an unknown cube is 400, not 404.

## Spec Change Log

## Review Triage Log

| Finding (layer) | Verdict | Route | Evidence |
|---|---|---|---|
| Tokens parsed before cube lookup and revision check (Blind, Edge) | low | rejected | The AC forbids building the new list before the revision check; parsing builds no list on the session. Order recorded in Implementation Notes |
| No cap on moves or token length; token echoed in `detail` (Blind, Edge) | low | rejected | Spine "Deferred: no cap on moves per request in v1"; `detail` is JSON-encoded and the log does not carry it |
| `[MinLength(1)]` on `IReadOnlyList` may not validate (Edge) | false | rejected | `PostMoves_MissingOrEmptyInput` with `moves: []` returns 400 |
| Concurrency tests may not interleave (Blind) | low | rejected | Both start behind a gate; `ForcedUpdateOutcome` and the 32-writer test cover the branches |
| Retry loop ignores the cancellation token, null `replacement` (Edge) | false | rejected | The loop never awaits and ends after at most one pass per competing writer; the use case never passes null |
| Revision re-hashed per request, redundant pre-check (Blind) | low | rejected | Pre-check is required by the AC; cost is linear in a small list |
| 409 gives no current revision (Blind) | low | rejected | Not in the AC or `ErrorDetails`; client re-reads |
| Test hashes revision itself (Blind) | false | rejected | An independent oracle is the point |
| Bare JSON property names in tests; unnamed outcome message (Blind) | low | patch | Named constants |
| POST response unchecked in `AfterOk` test; null element and malformed id on POST untested; fixture split on space only (Blind, Edge, Gap) | low | patch | Assertion and tests added; split on any whitespace |
| Implementation Notes empty, deferred entries not named (Edge) | medium | patch | Written above |
| bmad-code-review: null token gives detail `"" is not a move` (Blind, Edge, Gap, Auditor) | low | patch | Distinct null detail and a mapper test |
| bmad-code-review: precedence of bad token over stale revision / unknown cube unpinned (Blind, Edge, Gap) | low | patch | Two use case tests pin it |
| bmad-code-review: bare `1` in the parallel test; `TtcFixtureTestHelper` comment says Api tests cannot use Domain types, but they do (Auditor) | low | patch | Named constant; comment reworded |
| bmad-code-review: port differs from AD-5 wording, spine not updated (Auditor) | medium | decision | Left to Drago: amend AD-5 or change the port |
| bmad-code-review: no cap on moves or token echo, `Success(null)` unguarded, cancellation in retry loop, retry branch not deterministic, 409 weak assertions, 409 without current revision, 415/405 (Blind, Edge, Auditor) | low | rejected | Same grounds as the first pass; spine defers request limits; the loop never blocks; the entry for 405/415 stays deferred |
| 405 and 415 now real, entry not re-deferred (Blind) | low | rejected | Entry still says take it with tests for 404, 405 and 415; left in `deferred-work.md` |

## Verification

**Commands:**
- `dotnet build` -- expected: 0 warnings, 0 errors
- `dotnet test` -- expected: all green
