# Intent: Rubik's Cube Simulator (TTC Challenge)

Source: brainstorming session 2026-10-14. Task spec: `docs/TTC-Rubiks-Challenge.md`.

## Goal
A Rubik's cube simulator that rotates any face correctly and prints the faces in the exploded view.
Submission for a job interview, judged on correct output and production-quality code
(readable, maintainable, testable), with frontend and backend clearly separated.

## Must-haves
- Starts solved: Green front, Red right, White up (Blue back, Orange left, Yellow down).
- After `F R' U B' L D'` the printed faces match the required exploded view and rubiks-cube-solver.com.
- Runs on Windows with free tools; README lets a reviewer build and run from a clean clone.
- Tests (unit + API integration), no demo code or unnecessary files.

## Stack
- Backend: .NET 10. Frontend: React (latest stable) with Vite + TypeScript.

## Key decisions
1. **Derived, never stored.** The cube is a pure function of the ordered move list; everything else
   (3D view, exploded print, history, undo, reset) derives from it.
2. **Coordinate engine.** 26 cubies at (x,y,z) in {-1,0,1}, each sticker with a normal vector.
   A face turn rotates coordinates and normals of the 9 cubies in the layer by 90 degrees; one rotation
   function serves all six faces (no hand-written permutation tables).
3. **Separate projection.** A distinct `ExplodedViewRenderer` maps 3D to the 2D layout
   (normal -> face, other two coordinates -> row/column). The engine knows nothing about printing.
4. **Stateless API.** React keeps only the move list; the API replays it from a solved cube and returns
   state/faces. No server sessions.
5. **Notation.** Standard: uppercase `F R U B L D` = clockwise, lowercase `f r u b l d` = counter-clockwise
   (keyboard input and move history display).
6. **Layered delivery**, each layer a valid submission:
   engine + tests -> console output + README -> API -> React UI -> Docker.
7. **Selective best practices** (the author's .NET and React best-practices references):
   - .NET: thin controllers, Result pattern for invalid moves, ProblemDetails, Swagger, nullable +
     warnings-as-errors, xUnit + WebApplicationFactory. Skip EF/Postgres/JWT/rate limiting/OpenTelemetry.
   - React: function components, derived data in render, React Compiler, ESLint + Prettier,
     Vitest + Testing Library, one Playwright flow, semantic buttons, error boundary.
     Skip Server Components, Zustand/Redux, virtualization.

## UI nice-to-haves (not required by the task)
Orbit the whole cube to see hidden faces, reset to solved, move history (replay/undo), keyboard moves.

## Reviewer risks to pre-empt
- "Architecture is not appropriate": include a short architecture note (diagram + rationale) in the README.
- "Cannot be started from the docs": verify the README by following it in a fresh clone; list exact
  prerequisites (.NET 10 SDK, Node version) and paste the expected output.

## Open items
- Exploded-view layout and initial-orientation images in the PDF still need to be confirmed.
- `uv` is not installed (needed later by `bmad-build`).
