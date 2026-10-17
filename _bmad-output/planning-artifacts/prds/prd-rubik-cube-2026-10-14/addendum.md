# Addendum: technical decisions carried from brainstorming

Source: `_bmad-output/brainstorming/brainstorm-rubiks-cube-project-2026-10-14/brainstorm-intent.md`. For the architecture step; not requirements.

- **Derived, never stored.** The Cube is a pure function of the ordered Move list; history, undo, reset, Exploded View all derive from it.
- **Coordinate engine.** 26 cubies at (x,y,z) in {-1,0,1}, each sticker with a normal vector. A Move rotates coordinates and normals of the 9 cubies in the layer by 90°; one rotation function serves all six Faces (no hand-written permutation tables).
- **Separate projection.** `ExplodedViewRenderer` maps sticker normal → Face and the other two coordinates → row/column. Net orientation: U seen from above with B at top, F/R/B/L from outside, D from below with F at top.
- **API state.** Cube Sessions held in memory by `ICubeRepository` (in-memory implementation in its own file, `ConcurrentDictionary`); only the Move list is stored, the Cube is derived. Supersedes the earlier stateless-API decision; rejected: a duplicate TypeScript engine (logic in two places).
- **Concurrency.** Optimistic concurrency on the Move count: state carries `version`; mutating requests send `expectedVersion`; mismatch → `409 Conflict` ErrorDetails. The repository applies check-and-append atomically (per-session lock). Rejected: last-writer-wins (undo/reset silently destroy another person's moves) and read-only share links with forks (more endpoints and logic).
- **API contract (agreed).**
  - Request: `POST /api/cubes/{id}/moves` with `{ "expectedVersion": 0, "moves": ["F", "R'", "U"] }`; move tokens use standard notation as strings; invalid token → `400`.
  - Success: HTTP 200 with the data directly (no envelope); creating a session (`POST /api/cubes`) returns 201 Created with a `Location` header: `{ cubeId, version, moves, faces, explodedView }`. `faces` has `up|left|front|right|back|down`, each a 3×3 array of colour names (`"White"`, ...); `explodedView` is an array of text lines (single letters W Y G B R O) equal to the PDF layout.
  - Errors: real HTTP status with an ErrorDetails body (fields `type`, `title`, `status`, `detail`, `traceId`, following the RFC 9457 shape) (`400` invalid move, `404` unknown cube, `409` version conflict).
  - Backend: services return `Result<T>` with typed errors; the base controller maps it to status + ErrorDetails in one place. The Result type is internal, not a wire envelope (rejected: `{data, statusCode, hasError, error}` body, which duplicates the HTTP status and adds a second error shape).
  - Frontend: a small fetch wrapper returns `ApiResult<T> = { ok: true; data: T } | { ok: false; status: number; error: ErrorDetails }`, with the same fields as the backend `ErrorDetails`.
- **Endpoints (proposed).** `POST /api/cubes`, `GET /api/cubes/{id}`, `POST /api/cubes/{id}/moves`, `POST /api/cubes/{id}/undo`, `POST /api/cubes/{id}/reset`.
- **Hosting.** Docker image + compose for a Hetzner server (nice to have); non-Docker path stays in the README.
- **Backend practices (selective, from the author's .NET best-practices reference).** Thin controllers, Result pattern for invalid moves, ErrorDetails + global handler, Swagger, nullable + warnings-as-errors, xUnit + WebApplicationFactory. Skipped: EF/Postgres, JWT, rate limiting, OpenTelemetry, Testcontainers.
- **Frontend practices (selective, from the author's React best-practices reference).** Vite + TypeScript, function components, derived data in render, React Compiler, ESLint (react-hooks) + Prettier, Vitest + Testing Library (query by role), one Playwright flow, semantic buttons, error boundary. Skipped: Server Components, Zustand/Redux, virtualization.
- **Reviewer risks to pre-empt.** "Architecture is not appropriate" → architecture note in README. "Cannot be started from the docs" → README verified in a fresh clone.
- **Verified expected output.** The PDF exploded view was reproduced by an independent 3D-sticker simulation of `F R' U B' L D'` and matched tile for tile.
