---
status: final
stepsCompleted: ['step-01-validate-prerequisites', 'step-02-design-epics', 'step-03-create-stories', 'step-04-final-validation']
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-rubik-cube-2026-10-14/prd.md
  - _bmad-output/planning-artifacts/prds/prd-rubik-cube-2026-10-14/addendum.md
  - _bmad-output/planning-artifacts/architecture/architecture-rubik-cube-2026-10-15/ARCHITECTURE-SPINE.md
  - docs/TTC-Rubiks-Challenge.md
---

# rubik-cube - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for rubik-cube, decomposing the requirements from the PRD and Architecture spine into implementable stories. There is no UX design document.

## Requirements Inventory

### Functional Requirements

FR1: A new Cube is in the Solved State (Green front, Red right, White up, Blue back, Orange left, Yellow down); every Face has nine Tiles of its own colour.
FR2: The engine turns any of the six Faces 90° clockwise or counter-clockwise; four identical Moves, or a Move and its inverse, return the previous state; after `F R' U B' L D'` the Faces equal the expected Exploded View.
FR3: The system renders the Cube in the Exploded View layout (solved cube: one colour per Face; after the TTC Test Sequence: exactly the expected 9-line output).
FR4: The API creates a Cube Session in the Solved State and returns its `cubeId`, the Solved State faces and Version 0; sessions are independent.
FR5: The API returns the Faces, Exploded View, Move list and Version of a Cube Session; an unknown `cubeId` returns 404 with `ErrorDetails`.
FR6: The API appends Moves to a Cube Session given `expectedVersion`; mismatch returns 409 and changes nothing; success increases Version by the number of Moves; of two concurrent requests with the same expected Version exactly one succeeds; an unknown token returns 400 naming the token and leaves the session unchanged.
FR7: The API undoes the last Move or resets the session to the Solved State, with the same expected-Version rule as FR6; undo on an empty session leaves the Solved State.
FR8: The UI shows all six Faces as coloured Tiles in the Exploded View layout, also as plain text equal to the API's `explodedView`; the fresh page shows the Solved State; the `cubeId` is kept in the URL so reload or sharing shows the same session; an unknown `cubeId` shows a message and a control to start a new session.
FR9: The UI applies Moves by on-screen buttons for all twelve Moves and by keyboard (`F R U B L D` clockwise, `f r u b l d` counter-clockwise); buttons are real buttons operable by keyboard; API errors are shown without breaking the page; on 409 the UI reloads the session and says "the cube was changed by someone else".
FR10: A single control returns the session to the Solved State and applies the TTC Test Sequence; the displayed Faces equal the expected output, also on an already-used session.
FR11: The UI lists the Move list from the API and calls undo and reset; reset gives an empty history and the Solved State; undo gives the state before the last Move.
FR12 (Could): A 3D view of the Cube the user can rotate to see hidden Faces; cut first if time runs out.

### NonFunctional Requirements

NFR1 (Platform): builds and runs on Windows with free tools only; backend .NET 10, frontend React (latest stable).
NFR2 (Separation): backend and frontend are separate projects; the engine has no dependency on API or UI.
NFR3 (Quality): readable, maintainable, testable; nullable reference types on, warnings as errors; unit tests for engine and renderer, integration tests for the API, component tests for the UI, one end-to-end test of the TTC Test Sequence through the UI.
NFR4 (Observability): every API error response (400, 404, 409, 500) is logged with level, status, `cubeId` when known and a trace id, returned in the error body; unexpected exceptions are logged with the exception and never exposed.
NFR5 (Cleanliness): no demo code, template leftovers or unnecessary files.
NFR6 (Delivery): published in a GitHub repository the TTC Lead Developers can open and clone.
NFR7 (Documentation): README with exact prerequisites (.NET 10 SDK, Node version), numbered build/run/test commands, the expected output and an architecture note (diagram plus rationale), verified in a fresh clone; reviewer reaches a running app in under 10 minutes.

### Additional Requirements

- **Starter template:** none specified; greenfield. Epic 1 Story 1 scaffolds the solution (four `src` projects, four `*.Tests` projects, `web/`), `global.json` pinning SDK 10.0.x, `Directory.Build.props` (Nullable, TreatWarningsAsErrors), `Directory.Packages.props` (AD-11).
- AD-1: dependency direction Api to Application, Infrastructure; Infrastructure to Application; Application to Domain; Domain references nothing; enforced by a reflection-based assembly-reference test in `Rubik.Api.Tests`.
- AD-2: coordinate engine, 26 cubies with sticker normals, one rotation function for all faces, no permutation tables.
- AD-3: `ExplodedViewRenderer` in Domain produces both `faces` grids (net orientation, row-major) and `explodedView` text lines; frontend never recomputes either; TTC expected output is a fixture at Domain, API and UI levels.
- AD-4: a session stores only `{ cubeId, moves[] }`; every read replays moves; `version` equals move count.
- AD-5: `ICubeRepository` with `CreateAsync`, `GetAsync`, `TryUpdateAsync(cubeId, expectedVersion, newMoves)` returning `Updated | VersionConflict | NotFound`; caller's `expectedVersion` passed unchanged; in-memory adapter atomic per cube; singleton lifetime; `cubeId` is a Guid generated in Application.
- AD-6: `Move.TryParse` in Domain; `Result<T>` with `InvalidMove`, `CubeNotFound`, `VersionConflict` in Application; one Api mapper to 200/400/404/409 with `ErrorDetails` (`type`, `title`, `status`, `detail`, `traceId`); global handler for 500; undo/reset on empty session is a 200 no-op; apply with empty `moves` is 400; thin controllers.
- AD-7: plain success bodies (200; `POST /api/cubes` returns 201 Created with `Location: /api/cubes/<cubeId>`) `{ cubeId, version, moves, faces, explodedView }`; apply `{ expectedVersion, moves }`, undo/reset `{ expectedVersion }`; OpenAPI via Swashbuckle; `openapi-typescript` types committed in `web/src/api/generated/`, regenerated by one npm script; hand-written `ApiResult<T>` and fetch wrapper.
- AD-8: frontend calls only relative `/api/...`; Vite proxy in dev (API on 5080, web on 5173); nginx in Docker (8080); no CORS; non-`/api` paths fall back to `index.html`.
- AD-9: `useCubeSession` hook (`useReducer` + fetch wrapper), server is the source of truth, reload on 409; keyboard lowercase keys send `F'` etc.
- AD-10: SOLID, DRY, KISS; exceptions only with a one-line comment; every class with behaviour has tests in the same change; frontend tests beside the file.
- AD-11: reproducible build and run; README verified in a fresh clone; `GET /health`.
- AD-12: errors logged only in Api (mapper + global handler); 4xx Warning, 500 Error with exception; fields `operation`, `cubeId`, `status`, `traceId`; `ILogger` console; tests with `FakeLogger`.
- Stack (pinned in spine): .NET SDK 10.0.302, xunit.v3 4.0.2, Mvc.Testing 10.0.13, Swashbuckle 10.3.0, Diagnostics.Testing 10.11.0, Node 24, React 19.3.1, Vite 8.3.4, TypeScript 6.0.3 (not 7.x), Vitest 5.0.5, Testing Library, Playwright 1.64.1, ESLint 10.12.0, Prettier 3.9.10, openapi-typescript 7.13.0.
- Deferred / optional: CI (GitHub Actions), Docker + nginx for Hetzner (nice to have), React Compiler, 3D orbit.
- Delivery order from the PRD: engine + tests, API + repository + integration tests, React UI + tests, README; each layer is a valid stop point; two days of development.

### UX Design Requirements

None; no UX design document exists. UI behaviour is defined by FR8 to FR12.

### FR Coverage Map

FR1: Epic 1 - Solved initial state
FR2: Epic 1 - Rotate any face correctly
FR3: Epic 1 - Exploded view output
FR4: Epic 2 - Create a Cube Session
FR5: Epic 2 - Read a Cube Session
FR6: Epic 2 - Apply Moves with version check
FR7: Epic 2 - Undo and reset
FR8: Epic 3 - Show the Cube (tiles, text, URL session)
FR9: Epic 3 - Apply Moves by buttons and keyboard, conflict handling
FR10: Epic 3 - TTC Test Sequence preset
FR11: Epic 3 - Move history, undo, reset
FR12: Epic 5 - 3D orbit (optional)

NFR coverage: NFR1, NFR2, NFR5 in Epic 1; NFR3 in Epics 1 to 3 (tests in every story); NFR4 in Epic 2; NFR5, NFR6, NFR7 finalised in Epic 4.

## Epic List

### Epic 1: Verifiable cube engine with exploded view
A reviewer can run the tests and see that `F R' U B' L D'` yields exactly the required exploded view. Includes the solution scaffold.
**FRs covered:** FR1, FR2, FR3

### Epic 2: Cube sessions over the HTTP API
A cube can be created, turned, undone and reset through Swagger or curl; concurrent edits are safe; errors are logged.
**FRs covered:** FR4, FR5, FR6, FR7

### Epic 3: Interactive web interface
A reviewer sees the cube as coloured tiles and text, turns it by buttons and keys, reviews history, undoes, resets, and runs the TTC Test Sequence in one click.
**FRs covered:** FR8, FR9, FR10, FR11

### Epic 4: Reviewer-ready delivery
README with exact prerequisites and expected output, architecture note, fresh-clone verification, GitHub publishing and cleanup.
**FRs covered:** none (NFR5, NFR6, NFR7)

### Epic 5 (optional): Extras
3D orbit view, Docker with nginx for Hetzner, CI. Cut first if time runs out.
**FRs covered:** FR12

## Epic 1: Verifiable cube engine with exploded view

A reviewer can run the tests and see that `F R' U B' L D'` yields exactly the required exploded view. Includes the solution scaffold.

### Story 1.1: Solution scaffold with a clean build

As a builder,
I want a scaffolded solution with the agreed projects and rules,
So that every later story builds on a consistent, warning-free base.

**Acceptance Criteria:**

**Given** a fresh clone with the .NET 10 SDK
**When** I run `dotnet build` at the repo root
**Then** the solution builds with zero warnings
**And** it contains `Rubik.Domain`, `Rubik.Application`, `Rubik.Infrastructure`, `Rubik.Api` under `src/` and a `*.Tests` project for each under `tests/`.

**Given** `global.json`, `Directory.Build.props` and `Directory.Packages.props` exist
**When** a project is built
**Then** SDK 10.0.x is pinned, Nullable and TreatWarningsAsErrors are on, and package versions come from the central file only.

**Given** the project references
**When** `dotnet test` runs
**Then** an architecture test in `Rubik.Api.Tests` passes
**And** it fails if any reference breaks the direction Api to Application and Infrastructure, Infrastructure to Application, Application to Domain, Domain to nothing.

**Given** the repository
**When** I open `README.md`
**Then** it lists the prerequisites (.NET 10 SDK) and the `dotnet build` and `dotnet test` commands
**And** the repo contains no template leftovers (no `WeatherForecast`, no empty `Class1.cs`).

### Story 1.2: Solved cube with the correct orientation (FR1)

As a reviewer,
I want a new cube to be solved with Green front, Red right and White up,
So that I can trust the starting point of every sequence.

**Acceptance Criteria:**

**Given** a new `Cube`
**When** I read its six faces
**Then** Front is all Green, Right all Red, Up all White, Back all Blue, Left all Orange and Down all Yellow
**And** each face has nine tiles.

**Given** the cube model
**When** I inspect it in tests
**Then** it is built from 26 cubies with coordinates in {-1,0,1} and a normal vector per sticker (AD-2).

### Story 1.3: Strict move parsing (part of FR2)

As a reviewer,
I want moves written in standard notation to be parsed strictly,
So that invalid input is rejected instead of silently misread.

**Acceptance Criteria:**

**Given** the tokens `F R U B L D` and `F' R' U' B' L' D'`
**When** each is passed to `Move.TryParse`
**Then** it returns true with the matching face and direction (clockwise without `'`, counter-clockwise with `'`).

**Given** tokens such as `f`, `F2`, `X`, `F''`, an empty string or `null`
**When** passed to `Move.TryParse`
**Then** it returns false and does not throw.

**Given** the Domain project
**When** its references are checked
**Then** it still references no package and no other project.

### Story 1.4: Turn any face (FR2)

As a reviewer,
I want any face to turn 90 degrees in either direction,
So that the simulator behaves like a real cube.

**Acceptance Criteria:**

**Given** a solved cube
**When** I apply any of the twelve moves
**Then** the face centres never change
**And** only the tiles of that face and the 12 adjacent tiles change.

**Given** any cube state and any face
**When** I apply the same clockwise move four times, or a move followed by its inverse
**Then** the cube equals its previous state.

**Given** a solved cube
**When** I apply `F`
**Then** the Up bottom row is Orange, the Right left column is White, the Down top row is Red and the Left right column is Yellow
**And** the other tiles are unchanged.

**Given** a solved cube
**When** I apply `R`, then separately `U`
**Then** the full nets equal the reference nets in the tests, generated by an independent simulation: after `R` the Front right column is Yellow, the Up right column Green, the Back left column White and the Down right column Blue; after `U` the top rows of Front, Right, Back and Left read Red, Blue, Orange and Green.

**Given** the engine code
**When** I review it
**Then** all six faces use one shared rotation function parameterised by face and direction, with no per-face permutation tables.

### Story 1.5: Exploded view and the TTC Test Sequence (FR3)

As a reviewer,
I want the cube printed in the exploded view after `F R' U B' L D'`,
So that I can compare it with the PDF and see the correct output.

**Acceptance Criteria:**

**Given** a solved cube
**When** `ExplodedViewRenderer` renders it
**Then** each of the six faces shows its single colour in the layout Up on top, Left, Front, Right and Back in one row, Down below.

**Given** the TTC Test Sequence applied to a solved cube
**When** it is rendered
**Then** the nine text lines equal exactly:

```
      R O G
      B W W
      B B B
G Y Y O R R Y B O Y B W
O O G O G W R R W O B Y
B G O W W W O Y R Y Y W
      G G B
      R Y R
      R G G
```

**Given** the renderer output
**When** I read the `faces` result
**Then** it holds six 3x3 colour grids in the same net orientation (row-major) and agrees with the text tile for tile.

**Given** the README
**When** I open it
**Then** it has a "Run the tests" section that shows this expected output.

## Epic 2: Cube sessions over the HTTP API

A cube can be created, turned, undone and reset through Swagger or curl; concurrent edits are safe; errors are logged.

### Story 2.1: Create a Cube Session (FR4)

As a reviewer,
I want to create a cube through the API,
So that I get a solved cube with its own identity.

**Acceptance Criteria:**

**Given** the API is running
**When** I send `POST /api/cubes`
**Then** I get HTTP 201 Created with a `Location: /api/cubes/<cubeId>` header
**And** the body is `{ cubeId, version: 0, moves: [], faces, explodedView }` with a new Guid `cubeId` and the Solved State.

**Given** two calls to `POST /api/cubes`
**When** I compare the results
**Then** the `cubeId` values differ and the sessions are independent.

**Given** the code
**When** I review it
**Then** the use case lives in `Rubik.Application` behind `ICubeRepository` (`CreateAsync`, `GetAsync`), the controller is thin, and `InMemoryCubeRepository` in `Rubik.Infrastructure` is registered as a singleton.

**Given** the running API
**When** I open Swagger and call `GET /health`
**Then** the OpenAPI document is served and health returns 200
**And** unit tests cover the use case and the repository, and integration tests cover the endpoint.

### Story 2.2: Read a Cube Session and report errors consistently (FR5, NFR4)

As a reviewer,
I want to read a cube and get clear, logged errors,
So that I can see its state and trust how failures are reported.

**Acceptance Criteria:**

**Given** an existing session
**When** I send `GET /api/cubes/{id}`
**Then** I get HTTP 200 with `{ cubeId, version, moves, faces, explodedView }`.

**Given** an unknown `cubeId`
**When** I send `GET /api/cubes/{id}`
**Then** I get HTTP 404 with an `ErrorDetails` body (`type`, `title`, `status`, `detail`, `traceId`).

**Given** a `CubeNotFound` result
**When** it is mapped
**Then** it is logged at Warning, without an exception, with `operation`, `cubeId`, `status` and `traceId` (verified with `FakeLogger`)
**And** the `traceId` in the body equals the one in the log.

**Given** a repository that throws an unexpected exception
**When** I call any endpoint
**Then** I get HTTP 500 with `ErrorDetails` and no stack trace
**And** the exception is logged at Error with the same fields.

**Given** the code
**When** I review it
**Then** `Result<T>` with typed errors lives in `Rubik.Application`, one mapper and one global handler live in `Rubik.Api`, and `Rubik.Application` and `Rubik.Domain` do not log.

### Story 2.3: Apply moves with a version check (FR6)

As a reviewer,
I want to apply moves safely,
So that two people sharing a cube never silently overwrite each other.

**Acceptance Criteria:**

**Given** a new session
**When** I send `POST /api/cubes/{id}/moves` with `{ "expectedVersion": 0, "moves": ["F","R'","U","B'","L","D'"] }`
**Then** I get HTTP 200 with `version: 6` and an `explodedView` equal to the expected TTC output.

**Given** a session whose version is 3
**When** I send moves with `expectedVersion` 1
**Then** I get HTTP 409 with `ErrorDetails`, the session is unchanged, and the conflict is logged at Warning.

**Given** moves that include an invalid token such as `"X"` or `"f"`
**When** I send them
**Then** I get HTTP 400 whose `detail` names the token, and the session is unchanged.

**Given** an empty `moves` list
**When** I send it
**Then** I get HTTP 400.

**Given** two concurrent requests with the same `expectedVersion`
**When** both are processed
**Then** exactly one returns 200 and the other 409.

**Given** the code
**When** I review it
**Then** the update goes through `ICubeRepository.TryUpdateAsync(cubeId, expectedVersion, newMoves)` returning `Updated | VersionConflict | NotFound`, the caller's `expectedVersion` is passed unchanged, and the in-memory check-and-write is atomic per cube.

### Story 2.4: Undo and reset (FR7)

As a reviewer,
I want to undo the last move or reset the cube,
So that I can step back or start over without creating a new session.

**Acceptance Criteria:**

**Given** a session with moves
**When** I send `POST /api/cubes/{id}/undo` with `{ "expectedVersion": n }`
**Then** I get HTTP 200 with the last move removed and `version` reduced by one.

**Given** a session with moves
**When** I send `POST /api/cubes/{id}/reset` with `{ "expectedVersion": n }`
**Then** I get HTTP 200 with an empty `moves` list, version 0 and the Solved State.

**Given** an empty session
**When** I send undo or reset with `expectedVersion` 0
**Then** I get HTTP 200 with the Solved State and version 0.

**Given** a wrong `expectedVersion`
**When** I send undo or reset
**Then** I get HTTP 409 and nothing changes.

**Given** the README
**When** I open it
**Then** it has a "Try the API" section with the curl commands that reproduce the TTC expected output, the port (5080) and how to run `dotnet run`.

## Epic 3: Interactive web interface

A reviewer sees the cube as coloured tiles and text, turns it by buttons and keys, reviews history, undoes, resets, and runs the TTC Test Sequence in one click.

### Story 3.1: Frontend scaffold with generated API types and a typed client

As a builder,
I want a React project wired to the API contract,
So that UI stories build on typed, tested foundations.

**Acceptance Criteria:**

**Given** a fresh clone with Node 24
**When** I run `npm install` and `npm run dev` in `web/`
**Then** Vite serves the app on port 5173 and proxies `/api` to `http://localhost:5080` (AD-8).

**Given** the API is running
**When** I run `npm run api:generate`
**Then** TypeScript types are generated from its OpenAPI document into `web/src/api/generated/`
**And** the generated file is committed so a reviewer does not need to run it.

**Given** the fetch wrapper
**When** tests call it
**Then** a 2xx response returns `{ ok: true, data }`, a non-2xx response with an `ErrorDetails` body returns `{ ok: false, status, error }`, and a network failure returns `{ ok: false, status: 0, error }` with a generic message.

**Given** the project
**When** I run `npm run lint`, `npm run test` and `npm run build`
**Then** all pass with TypeScript strict, ESLint and Prettier configured and no `any`
**And** no Vite template leftovers remain (logo, counter, demo CSS).

### Story 3.2: Show the cube and keep the session in the URL (FR8)

As a reviewer,
I want to see the cube as coloured tiles and as text, and to share its link,
So that I can compare it with the PDF and reopen the same cube.

**Acceptance Criteria:**

**Given** I open `/`
**When** the page loads
**Then** the app calls `POST /api/cubes`, moves to `/cubes/<cubeId>` and shows the Solved State.

**Given** a session
**When** the cube is displayed
**Then** the six faces appear as 3x3 coloured tiles in the exploded layout, each tile exposes its colour name to assistive technology, and the text view shows the `explodedView` lines unchanged.

**Given** a session in the URL
**When** I reload the page or open the link elsewhere
**Then** the app calls `GET /api/cubes/<cubeId>` and shows the same session.

**Given** a `cubeId` the API does not know
**When** the page loads
**Then** I see a clear "cube not found" message and a "Start a new cube" button that creates a new session.

**Given** a rendering error in the UI
**When** it occurs
**Then** an error boundary shows a fallback instead of a blank page
**And** component tests (Testing Library, queries by role) cover these cases, and the SPA fallback for `/cubes/<id>` works in the Vite dev server.

### Story 3.3: Apply moves by buttons and keyboard (FR9)

As a reviewer,
I want to turn faces with buttons and keys,
So that I can play with the cube.

**Acceptance Criteria:**

**Given** a session
**When** I look at the controls
**Then** there are twelve real buttons labelled `F F' R R' U U' B B' L L' D D'`, reachable and operable by keyboard.

**Given** I click a move button
**When** the request is sent
**Then** it posts `{ expectedVersion: <current version>, moves: [<token>] }` and the view updates from the response.

**Given** the page has focus
**When** I press `F R U B L D`
**Then** the clockwise move is applied
**And** pressing `f r u b l d` applies the counter-clockwise move (`F'` and so on), while keys with Ctrl, Alt or Meta are ignored.

**Given** the API returns 400 or 500
**When** a move is applied
**Then** the error message is shown without breaking the page and the last good state stays visible.

**Given** the API returns 409
**When** a move is applied
**Then** the UI reloads the session from the API, shows "the cube was changed by someone else", and does not apply the action.

**Given** a network failure
**When** a move is applied
**Then** a clear error is shown and the page stays usable.

### Story 3.4: Move history, undo and reset (FR11)

As a reviewer,
I want to see the moves so far, step back and start over,
So that I can explore sequences safely.

**Acceptance Criteria:**

**Given** a session with moves
**When** I look at the history
**Then** the moves from the API are listed in order (for example `F R' U`), and an empty session shows "No moves yet".

**Given** a session with moves
**When** I press Undo
**Then** the UI calls the undo endpoint with the current `expectedVersion` and shows the state before the last move.

**Given** a session with moves
**When** I press Reset
**Then** the UI calls the reset endpoint and shows an empty history and the Solved State.

**Given** an empty session
**When** I look at Undo
**Then** it is disabled.

**Given** a 409 from undo or reset
**When** it happens
**Then** the UI reloads the session and shows the same "changed by someone else" message.

### Story 3.5: TTC Test Sequence in one click and the end-to-end check (FR10, NFR3)

As a reviewer,
I want one button that applies the TTC Test Sequence,
So that I can compare the result with the PDF immediately.

**Acceptance Criteria:**

**Given** a solved session
**When** I press "Apply TTC test sequence"
**Then** the text view equals the nine expected lines and the history reads `F R' U B' L D'`.

**Given** a session that already has moves
**When** I press the same button
**Then** the session is reset first and the result is the same as on a new session.

**Given** the repository
**When** I run `npm run e2e`
**Then** one Playwright flow starts the API and the web app, opens the app, presses the preset button and asserts the exact expected text.

**Given** the README
**When** I open it
**Then** it has a "Run the UI" section (`npm install`, `npm run dev`, ports) and the full two-terminal run with the expected output
**And** the unit, component and e2e commands are listed.

## Epic 4: Reviewer-ready delivery

README with exact prerequisites and expected output, architecture note, fresh-clone verification, GitHub publishing and cleanup.

### Story 4.1: README verified in a fresh clone (NFR7)

As a reviewer,
I want a README I can follow from a fresh clone,
So that I get a running app in under ten minutes.

**Acceptance Criteria:**

**Given** the README
**When** I read it
**Then** it states the exact prerequisites (.NET 10 SDK, Node 24), the repository layout, a ports table (API 5080, web dev 5173), the two-terminal run (`dotnet run` in `src/Rubik.Api`, `npm run dev` in `web/`), the commands for backend tests, frontend tests and e2e, and the expected `F R' U B' L D'` output.

**Given** a new empty folder and the GitHub repository
**When** I clone it and follow the README step by step without any other knowledge
**Then** the API and UI start, the TTC preset shows the expected output, and all tests pass
**And** the result of this run (date, SDK and Node versions, what was followed) is noted in the PR or commit message.

**Given** the README
**When** I look for the architecture
**Then** it contains or links the architecture note (`docs/ARCHITECTURE.md`) with the diagram and rationale
**And** it states that sessions live in memory and are lost on restart.

### Story 4.2: Quality pass and cleanup (NFR3, NFR5)

As a reviewer,
I want a clean, warning-free repository,
So that it looks like production-quality work.

**Acceptance Criteria:**

**Given** the repository
**When** I run `dotnet build`, `dotnet test`, `npm run lint`, `npm run test` and `npm run build`
**Then** everything passes with zero warnings.

**Given** the codebase
**When** I search it
**Then** there are no demo files, template leftovers, dead code or commented-out code
**And** every deliberate exception to SOLID, DRY or KISS carries its one-line comment (AD-10).

**Given** every class with behaviour
**When** I check the tests
**Then** each has tests, and DTOs, records, DI registration and `Program.cs` are covered by integration tests.

**Given** the planning material (`_bmad/`, `_bmad-output/`, `.claude/`, `CLAUDE.md`, `docs/` sources)
**When** I prepare the submission
**Then** the owner decides which to keep, and `.gitignore` and the final tree reflect that decision.

### Story 4.3: Publish to GitHub (NFR6)

As a TTC Lead Developer,
I want the solution in a GitHub repository,
So that I can open and clone it easily.

**Acceptance Criteria:**

**Given** the final tree
**When** it is pushed
**Then** the repository is reachable by the reviewers (visibility decided by the owner) with a clear name and description
**And** the default branch contains the README at the root and no secrets.

## Epic 5 (optional): Extras

3D orbit view, Docker with nginx for Hetzner, CI. Cut first if time runs out.

### Story 5.1: Orbit the cube in 3D (FR12)

As a reviewer,
I want to rotate a 3D view of the cube,
So that I can see hidden faces.

**Acceptance Criteria:**

**Given** a session
**When** I open the 3D view
**Then** the cube is drawn from the API `faces` data (no cube logic in the frontend, AD-9) and I can rotate it with the mouse or touch
**And** the 3D view is optional, does not replace the tiles and text, and has a component test for its data mapping.

### Story 5.2: Docker for hosting (Hetzner)

As an owner,
I want `docker compose up` to run the whole app,
So that I can host it on a Hetzner server.

**Acceptance Criteria:**

**Given** Docker is installed
**When** I run `docker compose up --build`
**Then** the app is available on port 8080, where nginx serves the built SPA, falls back to `index.html` for `/cubes/<id>`, and proxies `/api` to the `api` container (AD-8).

**Given** the README
**When** I read the Docker section
**Then** it states that the non-Docker path remains the primary way to run, and lists the minimal steps to deploy to a Hetzner server.

### Story 5.3: Continuous integration

As an owner,
I want every push built and tested automatically,
So that reviewers see a green build.

**Acceptance Criteria:**

**Given** a push or pull request
**When** the GitHub Actions workflow runs
**Then** it builds and tests the backend with .NET 10 and lints, tests and builds the frontend with Node 24
**And** a failing step fails the workflow.
