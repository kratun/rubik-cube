---
title: 'Story 2.2: Read a Cube Session and report errors consistently (FR5, NFR4)'
type: 'feature'
created: '2026-10-17'
status: 'done'
baseline_commit: 'f4ef01da5a913dc2bcdedbcab60d72e8ac587b31'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/CLAUDE.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** A created cube cannot be read back (the `Location` of `POST /api/cubes` points nowhere), and failures have no common shape, no trace id and no log line (FR5, NFR4).

**Approach:** `GET /api/cubes/{id}` returns the `CubeState` body or a 404. A `GetCubeUseCase` returns `Result<CubeState>` with the typed error `CubeNotFound`. One `ResultMapper` and one `GlobalExceptionHandler` in Api build `ErrorDetails` (`type`, `title`, `status`, `detail`, `traceId`) and log through one `ErrorReporter` (Warning without exception for expected failures, Error with the exception for 500) with `operation`, `cubeId` when known, `status` and `traceId`; the body's `traceId` is the logged one (AD-6, AD-12).

## Boundaries & Constraints

**Always:** Only Api logs; Application and Domain do not. Api and Infrastructure keep no Domain reference. `Result<T>` and its errors live in Application. Face keys, JSON casing and body shape stay as in 2.1. No bare literals in logic or tests. Build with 0 warnings; every class with logic has tests in the same change.

**Never:** `InvalidMove`, `RevisionConflict`, `TryUpdateAsync`, `expectedRevision`, applying moves (2.3); undo/reset (2.4); frontend; stack traces or exception messages in a response; a second place that picks status codes or logs errors.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Read | `GET /api/cubes/{id}` of a created cube | 200, same body as the create response | N/A |
| Unknown id | well-formed Guid, no such session | 404 `ErrorDetails`; Warning log, no exception, with `operation`, `cubeId`, `status`, `traceId`; body `traceId` equals the log's | N/A |
| Malformed id | `GET /api/cubes/abc` | 400 `ErrorDetails` (same logging, no `cubeId`) | model binding failure routed through the mapper |
| Repository throws | any endpoint | 500 `ErrorDetails` with a generic `detail`; Error log with the exception and the same fields | request-aborted cancellation is not reported as 500 |

</frozen-after-approval>

## Code Map

- `src/Rubik.Application/Cubes/{ICubeRepository,CubeState,CreateCubeUseCase}.cs` -- port, read model and use-case style to follow; `GetAsync` already exists.
- `src/Rubik.Application/Results/` (new) `Result<T>`, `ApplicationError`; `Cubes/CubeNotFound.cs`, `Cubes/GetCubeUseCase.cs` (new).
- `src/Rubik.Api/Errors/` (new) `ErrorDetails`, `ErrorReporter`, `ResultMapper`, `GlobalExceptionHandler`.
- `src/Rubik.Api/Cubes/CubesController.cs` -- add thin `Get`; `src/Rubik.Api/Program.cs` -- register use case, reporter, mapper, handler, `UseExceptionHandler`, invalid-model-state factory.
- `tests/Rubik.Application.Tests/{Results,Cubes}/`, `tests/Rubik.Api.Tests/{Errors,Cubes,TestHelpers}/` -- new tests; extend `CubesControllerTests` and `CubeRepositoryTestHelper` use.
- `Directory.Packages.props` -- add `Microsoft.Extensions.Diagnostics.Testing` 10.11.0; reference it from `Rubik.Api.Tests` only.
- `src/Rubik.Infrastructure/Cubes/InMemoryCubeRepository.cs` -- throws `InvalidOperationException` on a duplicate id; unchanged, covered by the 500 path.

## Tasks & Acceptance

**Execution:**
- [x] `src/Rubik.Application/Results/`, `Cubes/CubeNotFound.cs`, `Cubes/GetCubeUseCase.cs` + tests -- `Result<T>` (success/failure, guarded access), `CubeNotFound(Guid)`, use case returns the replayed state or `CubeNotFound` -- AD-6
- [x] `src/Rubik.Api/Errors/` + tests -- `ErrorReporter` builds `ErrorDetails` from `HttpContext.TraceIdentifier`, derives `operation` (controller.action, else method + path) and `cubeId` (route value `id`), logs by source-generated messages; `ResultMapper` maps `CubeNotFound` to 404 (unmapped error throws, so it surfaces as 500) and model-state failures to 400; `GlobalExceptionHandler` writes 500 -- AD-12
- [x] `src/Rubik.Api/Cubes/CubesController.cs`, `Program.cs` -- `GET {id}` action taking a `Guid id` (no route constraint, so a bad id reaches the mapper) with `ProducesResponseType` 200/400/404/500, DI wiring, `AddExceptionHandler`, `UseExceptionHandler`, `AddProblemDetails` as the middleware's required fallback
- [x] `tests/Rubik.Api.Tests/` -- integration: GET 200 and 404, malformed id 400, throwing repository gives 500 without a stack trace, `traceId` in body equals the `FakeLogger` record, Location from POST resolves
- [x] `deferred-work.md`, `sprint-status.yaml` -- no deferred entry falls in scope; status in-progress then review

**Acceptance Criteria:**
- Given a created cube, when `GET /api/cubes/{id}`, then 200 and the same body as creation; its `Location` works.
- Given an unknown id, when read, then 404 `ErrorDetails` and one Warning record (no exception) carrying `operation`, `cubeId`, `status`, `traceId`, with the body's `traceId` equal to the record's.
- Given a throwing repository, when any endpoint is called, then 500 `ErrorDetails` without stack trace and one Error record with the exception and the same fields.
- Given the code, then Application and Domain do not log, there is one mapper and one handler, `dotnet build` has 0 warnings and `dotnet test` is green.

## Implementation Notes

Deferred entries taken: none; nothing in `deferred-work.md` falls inside 2.2 (sequence parsing and fixture wiring stay for 2.3, `Cube` equality for 2.4, the rest for 4.2). One entry added: requests that match no route return the framework's empty 4xx (see `deferred-work.md`).

Decisions beyond the matrix, agreed with Drago: a malformed id returns 400 `ErrorDetails` through the mapper (`InvalidModelStateResponseFactory`, detail is generic so raw input is never echoed); a request aborted by the client is answered with status 499, no body and no log entry (nobody reads it, and it is not a server fault).

Log property names are PascalCase (`Operation`, `CubeId`, `Status`, `TraceId`), the names of the message-template placeholders. Inside the exception handler middleware the endpoint and route values are cleared, so `ErrorReporter` reads them from `IExceptionHandlerFeature`.

## Spec Change Log

## Review Triage Log

| Finding (layer) | Verdict | Route | Evidence |
|---|---|---|---|
| `AssertState` used `StartsWith` for the operation and `?? ""` for the cube id (Blind, Edge, Gap) | low | patch | Exact match and `null` now asserted |
| 500 integration test never checked `CubeId` (Gap) | medium | patch | GET asserts the requested id, POST asserts null |
| Bare `0`, `"status"`, `"traceId"` and `"GET "` in tests; bare messages in `Result` (Blind, Auditor) | low | patch | Named constants |
| Unmatched route, 405, 415 give an empty 4xx (Blind, Edge, Auditor) | medium | defer | Real; outside 2.2 by agreement, entry added to `deferred-work.md` |
| Non-abort `OperationCanceledException` branch untested (Blind) | low | patch | Test added, still 500 with Error log |
| Design Note promised a check of the framework's own log entries (Auditor) | low | patch | Verified by experiment: one Warning-or-worse record in total. Tests now assert exactly one, from `ErrorReporter` |
| 499 has no body or `ErrorDetails` (Blind, Auditor) | low | rejected | Deliberate, nobody reads it; recorded in Implementation Notes |
| `TaskCanceledException` slips past the abort check (Blind) | false | rejected | It derives from `OperationCanceledException`, so `is` covers it |
| Missing 400 on `Create` (Blind) | false | rejected | `Create` has no input |
| `HasStarted` guard missing (Edge) | false | rejected | The middleware rethrows before it calls any handler when the response has started |
| Empty `TraceIdentifier` (Edge) | false | rejected | Kestrel always sets it |
| Test factories never disposed (Edge) | false | rejected | Derived factories are disposed with their parent |
| Log keys versus template placeholders can drift (Blind, Auditor) | false | rejected | Tests read `state[Key]`, so drift fails them |
| Package version 10.11.0 may not restore (Gap) | false | rejected | It restored and builds |
| `Result.Failure(null)` or `Success(null)` (Edge) | low | rejected | Nullable annotations guard it; no caller passes null |
| Path in `Operation` unbounded or forgeable, write to aborted client throws (Edge) | low | rejected | Only the no-endpoint fallback; fix is added guards for an unlikely case |
| Guid.Empty and non-canonical Guid forms; external `type` URI; no OpenAPI snapshot; mapper throws for unmapped errors; `Location` not via `CreatedAtAction`; operation names in tests (Blind, Edge) | low | rejected | Valid ids are valid; the URI is only an identifier; throwing is the intended loud failure; Location format is fixed by the AC |

## Design Notes

`ErrorReporter` is the single place that builds `ErrorDetails` and writes the log entry; the mapper (expected failures) and the handler (exceptions) only choose status and detail. The framework's exception middleware adds no log entry of its own for a handled exception; the integration tests assert that exactly one Warning-or-worse record exists and that it comes from `ErrorReporter`.

## Verification

**Commands:**
- `dotnet build` -- expected: 0 warnings, 0 errors
- `dotnet test` -- expected: all green
