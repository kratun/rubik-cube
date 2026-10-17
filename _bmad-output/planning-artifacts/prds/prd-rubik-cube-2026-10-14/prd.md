---
title: Rubik's Cube Simulator
status: final
created: 2026-10-14
updated: 2026-10-15
---

# PRD: Rubik's Cube Simulator
*Working title — confirm.*

## 0. Document Purpose
Scope and acceptance criteria for the TTC Software Developer "Rubik's Cube Challenge" submission. Written for the builder (me) and for downstream BMad workflows (architecture, epics, build). The task source is `docs/TTC-Rubiks-Challenge.md`; technical decisions from brainstorming live in `addendum.md`.
## 1. Vision
A programmatic 3×3×3 Rubik's cube that rotates any face correctly and shows all six faces in the exploded-view layout required by the task. It is a job-interview submission: reviewers (TTC Lead Developers) judge whether the output is exactly right and whether the code is close to production quality: readable, maintainable, testable, with frontend and backend clearly separated.

The cube is always derived from its ordered move list; only the list is stored. A .NET 10 backend owns the cube logic and exposes it through an HTTP API that keeps each cube in memory behind a repository interface; a React frontend shows the cube and lets a person apply moves. The task allows console-only output, but the builder chose an API + UI to show full-stack skill; correctness is proven by tests and a one-click preset of the test sequence.

## 2. Target User

### 2.1 Jobs To Be Done
- **Reviewer:** confirm in minutes that the cube turns correctly and that the code is clean, from a clean clone and the README alone.
- **Reviewer:** see the exact expected exploded view after `F R' U B' L D'` without hand-checking each tile.
- **Builder (me):** deliver a correct, tested, well-structured submission within two days.

### 2.2 Non-Users (v1)
Cubers who want a solver, or anyone needing multi-user or persisted cubes.

### 2.3 Key User Journeys
- **UJ-1. Reviewer verifies the test sequence.** A TTC Lead Developer clones the repo, follows the README, starts backend and frontend, opens the UI, clicks "Apply TTC test sequence" and compares the exploded view with the PDF. The faces match tile for tile.
- **UJ-2. Reviewer plays with the cube.** The reviewer applies moves by button and keyboard (`F`, then `f` to undo it), sees the history, resets to solved, and rotates the view to check hidden faces.
- **UJ-3. Reviewer checks quality.** The reviewer runs the backend and frontend tests (green), reads the README architecture note, and sees engine, API, and UI as separate, small, readable units.

## 3. Glossary
- **Cube** — a 3×3×3 Rubik's cube: six Faces of nine Tiles each.
- **Face** — one of Front, Back, Up, Down, Left, Right.
- **Tile** — a single coloured square on a Face.
- **Move** — a 90° turn of one Face, clockwise or counter-clockwise as seen looking at that Face. Notation: `F` clockwise, `F'` counter-clockwise (likewise R, U, B, L, D).
- **Move Sequence** — an ordered list of Moves applied to a solved Cube.
- **Solved State** — each Face a single colour: Green Front, Red Right, White Up, Blue Back, Orange Left, Yellow Down.
- **Exploded View** — the 2D net of all six Faces defined by the task (PDF page 2): Up above Front, Down below Front, and Left, Front, Right, Back in one row.
- **Cube Session** — a server-held Cube identified by a `cubeId`, stored as its ordered Move list; the Cube is derived from it.
- **Revision** — a text token derived from a Cube Session's Move list alone (equal lists give equal Revisions, any other list a different one). It is not the number of Moves: undo and new Moves can bring the count back to an earlier number with a different history. Every mutating request states the Revision it expects; a mismatch is a conflict.
- **TTC Test Sequence** — `F R' U B' L D'`.

## 4. Features

### 4.1 Cube engine
**Description:** The domain core: holds the Cube, applies Moves, and has no knowledge of printing, HTTP, or UI. Realizes UJ-1, UJ-2.

#### FR-1: Solved initial state
A new Cube is in the Solved State with the orientation defined in the Glossary.

**Consequences (testable):**
- Every Face has nine Tiles of its own colour.

#### FR-2: Rotate any Face
The engine can turn any of the six Faces 90° clockwise or counter-clockwise, correctly updating the adjacent Face Tiles.

**Consequences (testable):**
- Four identical Moves return the Cube to its previous state.
- A Move followed by its inverse returns the Cube to its previous state.
- After the TTC Test Sequence the Faces equal the expected Exploded View in §4.2.

**Out of Scope:** solving; double or wide moves; cubes other than 3×3×3.

### 4.2 Exploded View
**Description:** A separate projection from Cube state to the 2D layout; the engine stays print-agnostic. Realizes UJ-1.

#### FR-3: Exploded View output
The system renders the Cube in the Exploded View layout.

**Consequences (testable):**
- The solved Cube renders each Face as its single colour in the required layout.
- After the TTC Test Sequence the output equals exactly (W white, Y yellow, G green, B blue, R red, O orange):

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

### 4.3 HTTP API
**Description:** The API keeps Cube Sessions in memory behind a repository interface (own file) so a database can replace it later without touching controllers. Only the Move list is stored; the Cube and Exploded View are derived on each read. Realizes UJ-1, UJ-2.

#### FR-4: Create a Cube Session
The API creates a Cube Session in the Solved State and returns its `cubeId`.

**Consequences (testable):**
- The response contains a `cubeId`, the Solved State faces and the Revision of an empty Move list.
- Two sessions do not affect each other.

#### FR-5: Read a Cube Session
The API returns the Faces, the Exploded View, the Move list and the Revision of a Cube Session.

**Consequences (testable):**
- An unknown `cubeId` returns HTTP 404 with a ErrorDetails body.

#### FR-6: Apply Moves
The API appends the Moves to the Cube Session, given the Revision the caller expects, and returns the new state.

**Consequences (testable):**
- A request whose expected Revision differs from the session's Revision returns HTTP 409 with a ErrorDetails body, and the session is unchanged.
- A successful request changes the Revision to the one of the new Move list.
- Two concurrent requests with the same expected Revision: exactly one succeeds, the other gets 409.
- The TTC Test Sequence on a new session returns Faces equal to FR-3's expected output.
- An unknown move token returns HTTP 400 with a ErrorDetails body naming the token, and the session is unchanged.

#### FR-7: Undo and reset
The API removes the last Move (undo) or returns the session to the Solved State (reset).

**Consequences (testable):**
- Undo yields the state before the last Move; undo on an empty session leaves the Solved State.
- Reset yields an empty Move list and the Solved State.
- Undo and reset follow the same expected-Revision rule as FR-6: a mismatch returns HTTP 409 and changes nothing.

### 4.4 Web UI
**Description:** A React single-page app, separate from the backend, that holds only the `cubeId` and renders what the API returns. Realizes UJ-1, UJ-2.

#### FR-8: Show the Cube
The UI shows all six Faces as coloured Tiles in the Exploded View layout.

**Consequences (testable):**
- Fresh page load shows the Solved State.
- Tile colours match the API response.
- The UI also shows the Exploded View as plain text, equal to the `explodedView` lines in the API response, so it can be compared with the PDF and copied.
- The `cubeId` is kept in the URL; reload or sharing the link shows the same Cube Session while the server runs.
- An unknown `cubeId` in the URL shows a clear message and a control to start a new Cube Session.

#### FR-9: Apply Moves
The UI applies Moves by on-screen buttons for all twelve Moves and by keyboard.

**Consequences (testable):**
- Keys `F R U B L D` apply the clockwise Move; keys `f r u b l d` apply the counter-clockwise Move.
- Buttons are real buttons, reachable and operable by keyboard.
- An API error is shown to the user without breaking the page.
- On a 409 conflict (undo and reset included) the UI reloads the Cube Session from the API and tells the user "the cube was changed by someone else"; the user's action is not applied.

#### FR-10: TTC Test Sequence preset
A single control returns the Cube Session to the Solved State and then applies the TTC Test Sequence.

**Consequences (testable):**
- Used on a Cube Session that already has Moves, the result is the same as on a new one.
- After use, the displayed Faces equal FR-3's expected output.

#### FR-11: Move history, undo, reset
The UI lists the Move list from the API and calls undo and reset.

**Consequences (testable):**
- Reset yields an empty history and the Solved State.
- Undo yields the state before the last Move.

#### FR-12: Orbit the cube *(Could)*
Cut first if time runs out; nothing else depends on it. A 3D view of the Cube the user can rotate to see hidden Faces.

## 5. Non-Goals (Explicit)
- Not a solver and not a scrambler.
- No live sync between people sharing a Cube Session (no websockets or polling): another person's change is noticed on conflict or reload.
- No accounts and no database in v1: Cube Sessions live in memory behind a repository interface and are lost on server restart.
- No animation polish, no mobile-native app.
- Not a general N×N×N engine.

## 6. MVP Scope

### 6.1 In Scope (delivery order; each layer is a valid stop point)
1. Engine + unit tests (FR-1 to FR-3).
2. HTTP API + repository + integration tests (FR-4 to FR-7).
3. React UI + tests (FR-8 to FR-11).
4. README: prerequisites, build/run steps, expected output, short architecture note.
5. *Could:* 3D orbit view (FR-12); Docker image and compose file so it can be hosted on a Hetzner server.

### 6.2 Out of Scope for MVP
- Console host (dropped by decision; the TTC task accepts console output but does not require it).
- Solver, double moves (`F2`), wide or slice moves, other cube sizes.

## 7. Success Metrics
**Primary**
- **SM-1**: After the TTC Test Sequence the Exploded View matches the PDF exactly, in an automated test at engine, API, and UI levels. Validates FR-2, FR-3, FR-6, FR-10.
- **SM-2**: A reviewer goes from a clean clone to a running app in under 10 minutes using only the README (verified by following it in a fresh clone). Validates the Delivery NFRs.

**Secondary**
- **SM-3**: All backend and frontend tests pass from the documented commands; build has zero warnings.

**Counter-metrics (do not optimize)**
- **SM-C1**: Number of UI features. Counterbalances SM-1: extras must never take time from engine correctness, tests, or README.

## 8. Cross-Cutting NFRs
- **Platform:** builds and runs on Windows with free tools only; backend .NET 10, frontend React (latest stable).
- **Separation:** backend and frontend in separate projects; the engine has no dependency on API or UI.
- **Quality:** readable, maintainable, testable; nullable reference types on and warnings treated as errors; unit tests for the engine and renderer, integration tests for the API, component tests for the UI, and one end-to-end test that applies the TTC Test Sequence through the UI and checks the displayed output.
- **Observability:** every API error response (400, 404, 409, 500) is logged with level, status, `cubeId` when known and a trace id, and the trace id is returned in the error body; unexpected exceptions are logged with the exception and never exposed to the client.
- **Cleanliness:** no demo code, template leftovers, or unnecessary files.
- **Delivery:** the solution is published in a GitHub repository the TTC Lead Developers can open and clone.
- **Documentation:** README with exact prerequisites (.NET 10 SDK, Node version), numbered build/run/test commands, the expected output, and an architecture note (diagram + rationale).

## 9. Open Questions
None. The API contract is recorded in `addendum.md`.
