# Epic 2 Context: Cube sessions over the HTTP API

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

A reviewer can create a cube, read it, turn it, undo and reset it through Swagger or curl. Concurrent edits are safe (no silent overwrite) and every failure is reported in one consistent shape and logged with a trace id.

## Stories

- Story 2.1: Create a Cube Session (FR4)
- Story 2.2: Read a Cube Session and report errors consistently (FR5, NFR4)
- Story 2.3: Apply moves with a revision check (FR6)
- Story 2.4: Undo and reset (FR7)

## Requirements & Constraints

- Wire body for every success: `{ cubeId, revision, moves, faces, explodedView }`; create returns 201 with `Location: /api/cubes/<cubeId>`, everything else 200.
- Every non-2xx has an `ErrorDetails` body (`type`, `title`, `status`, `detail`, `traceId`): 400 invalid move, 404 unknown cube, 409 revision conflict, 500 unexpected. No stack traces to clients.
- `revision` is a text hash of the move list, never a count.
- Undo/reset on an empty session is a successful no-op; an apply request with empty `moves` is 400.
- Build has zero warnings; every class with logic has tests in the same change.

## Technical Decisions

- Hexagonal layering: Api -> Application, Infrastructure; Infrastructure -> Application; Application -> Domain; an architecture test enforces it. Api and Infrastructure cannot reference Domain, so the Application read model `CubeState` carries no Domain types and the controller returns it directly.
- A session stores `{ cubeId, moves[] }` only; the cube is rebuilt by replay on each read.
- `ICubeRepository` owns atomicity: `CreateAsync`, `GetAsync`, and (2.3) compare-and-set `TryUpdateAsync(expectedRevision, replacement)`. Repository is a singleton. `CubeSession` stays a class (reference comparison).
- Failures are values: `Result<T>` with typed errors in Application (`InvalidMove`, `CubeNotFound`, `RevisionConflict`). One Result-to-HTTP mapper and one global exception handler in Api.
- Logging (AD-12): only Api logs. Expected failures at Warning without exception; unexpected at Error with exception. Each entry carries `operation`, `cubeId` when known, `status`, `traceId`; the same traceId is in the body. Tests use `FakeLogger`.
- Swagger is served in Development only. JSON camelCase; face keys `up left front right back down`; colours as names.

## Cross-Story Dependencies

- 2.2 introduces `Result<T>`, the mapper and the handler that 2.3 and 2.4 reuse (2.3 adds `InvalidMove` and `RevisionConflict`).
- 2.3 adds `TryUpdateAsync`; 2.4 builds undo/reset on it.
- Epic 3 generates frontend types from the OpenAPI document, so contract changes need regenerated types.
