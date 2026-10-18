---
title: 'Story 3.2: Show the cube and keep the session in the URL (FR8)'
type: 'feature'
created: '2026-10-18'
status: 'done'
route: 'dispatch'
baseline_commit: '4e973ee7f5121f444b9e27083f0104d3621b4528'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent â€” do not modify unless human renegotiates">

## Intent

**Problem:** The app shows only a title. A reviewer cannot see the cube, compare it with the PDF, or reopen the same cube from a link.

**Approach:** `/` creates a cube (`POST /api/cubes`) and moves to `/cubes/<cubeId>`; that URL loads the session (`GET /api/cubes/<cubeId>`). The page renders `faces` as 3x3 tiles in the exploded layout and `explodedView` as text, unchanged (AD-3). State lives in one `useCubeSession` hook (`useReducer` plus the 3.1 client, AD-9). An error boundary replaces a crashed tree with a fallback.

## Boundaries & Constraints

**Always:** Use `createCube` and `getCube` from `client.ts`; no new fetch. Every tile carries its colour name for assistive technology. Face keys: `up left front right back down`. Named constants, no `any`, tests beside the file, queries by role.

**Never:** Change `web/src/api/generated/`; compute layout or colours in TypeScript; move buttons and keys (3.3); history, undo, reset (3.4); TTC button, e2e, README "Run the UI" (3.5); Docker, CI.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| New session | URL `/` | POST, URL becomes `/cubes/<id>`, Solved State shown | Failure: message and "Start a new cube" |
| Reload / shared link | URL `/cubes/<id>` | GET, same session shown | N/A |
| Unknown or malformed id | GET answers 404 or 400 | "Cube not found" and "Start a new cube" | Button creates a session |
| Network or server failure | GET/POST fails otherwise | API message and "Start a new cube" | N/A |
| Render error | Component throws | Error boundary fallback | N/A |

</frozen-after-approval>

## Code Map

- `web/src/features/cube/sessionUrl.ts` -- `cubeIdFromPath`, `cubePath`; the whole "router" (two routes)
- `web/src/features/cube/useCubeSession.ts` -- reducer states `loading | ready | error`, loads or creates on mount, `startNewCube`
- `web/src/features/cube/CubeView.tsx` (+ `.css`) -- tiles in exploded layout, text view
- `web/src/features/cube/CubePage.tsx` -- hook plus view plus error message
- `web/src/features/cube/solvedCubeFixture.ts` -- test fixture of the Solved State
- `web/src/features/cube/apiResponseFixture.ts` -- test helpers for fetch responses and status constants
- `web/src/app/ErrorBoundary.tsx`, `App.tsx` -- boundary around the page
- `web/src/spaFallback.test.ts` -- starts the Vite dev server, requests `/cubes/<id>`

## Tasks & Acceptance

**Execution:**
- [x] sessionUrl, useCubeSession, CubeView, CubePage, ErrorBoundary, App wiring, with tests
- [x] SPA fallback test against the Vite dev server

**Acceptance Criteria:**
- Given `/`, when the page loads, then POST /api/cubes runs, the URL is `/cubes/<cubeId>` and the Solved State shows
- Given a displayed cube, then six faces show as 3x3 tiles with colour names and the text lines match `explodedView`
- Given `/cubes/<id>`, when reloaded, then GET /api/cubes/<id> shows the same session
- Given an unknown id, then "Cube not found" and a working "Start a new cube" button show
- Given a rendering error, then the boundary fallback shows
- Given the dev server, when `/cubes/<id>` is requested, then `index.html` is served

## Implementation Notes

- Deferred entries: none fall inside 3.2 (drift check, `Program.cs`, CSharpier, `[FromServices]`, `UseStatusCodePages` are out of scope), so none were taken.
- Routing: no router library. Two routes, read once from `window.location.pathname`; `history.replaceState` writes the new URL, so the app adds no history entries and needs no `popstate` handling (KISS). Revisit if 3.4 needs navigation.
- 400 on load (malformed id such as `/cubes/abc`) is shown as "cube not found" like 404: the id in the URL is unusable either way.
- Under React StrictMode the mount effect runs twice in development, so `/` can create one unused cube; a cancel flag keeps only the last result.

## Spec Change Log

## Review Triage Log

Pass 1 (bmad-code-review: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor; no layer failed).

| Finding | Verdict | Disposition |
|---|---|---|
| Broken percent-encoding in the path throws URIError and shows the boundary, not "cube not found" | medium | Patched: id passed on undecoded, plus test |
| ErrorBoundary wiring in App untested | medium | Patched: App test with a crashing page |
| Cancel flag in useCubeSession untested | medium | Patched: unmount-before-response test |
| Rejected fetch / network failure row untested | low | Patched: hook test (the client already converts it to ApiResult; no catch needed) |
| BOM in CubePage.test.tsx; bare literals in fixtures; duplicate STATUS_OK; POST/GET literals in tests | low | Patched |
| Code Map omits apiResponseFixture.ts | low | Patched |
| .catch missing on request.then | false | client.request never throws (3.1) |
| cubeId change leaves stale state | false | cubeId is read once and never changes |
| startNewCube race, double click, dispatch after unmount | low | Rejected: button exists only in the error state and unmounts on click; React ignores dispatch after unmount |
| Trailing slash, extra segments, /cubes without slash, query and hash dropped | low | Rejected: speculative guards |
| Silent `?? []` for a missing face; payload shape not validated | low | Rejected: the contract guarantees six faces (3.1 types) |
| No retry, 54 role=img tiles, no forced-colors styling, fixtures outside TestHelpers | low | Rejected: not required by the story; frontend tests sit beside files |
| Production SPA fallback (nginx/Docker) | n/a | Out of scope: AD-8 deployment, Docker story |

## Verification

**Commands:**
- `npm run lint`, `npm run test`, `npm run build` in `web/` -- expected: green
