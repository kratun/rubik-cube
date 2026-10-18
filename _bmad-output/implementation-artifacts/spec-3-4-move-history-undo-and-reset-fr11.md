---
title: 'Story 3.4: Move history, undo and reset (FR11)'
type: 'feature'
created: '2026-10-18'
status: 'in-review'
route: 'dispatch'
baseline_commit: '3e234ae'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent - do not modify unless human renegotiates">

## Intent

**Problem:** A reviewer can turn the cube but cannot see the moves so far, step back or start over.

**Approach:** `useCubeSession` gains `undo()` and `reset()` next to `applyMove`; all three go through one `mutate` path (revision check, in-flight guard, notice on failure, 409 reload). The page lists `cube.moves` from the API, offers Undo (disabled when the history is empty) and Reset. The UI holds no cube logic (AD-3, AD-9).

## Boundaries & Constraints

**Always:** `undoMove` and `resetCube` from `client.ts`, relative `/api` paths only. Extend `useCubeSession`, no second hook. Named constants, no `any`, tests beside the file, queries by role.

**Never:** Change `web/src/api/generated/`; compute history or state in TypeScript; TTC button, e2e, README "Run the UI" (3.5); Docker, CI.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| History | Session with `F R' U` | Moves listed in order | N/A |
| Empty session | `moves` is empty | "No moves yet"; Undo disabled | N/A |
| Undo | Session with moves | POST `{ expectedRevision: <current> }` to `undo`, view shows the response | 400 / 500 / network: message shown, last good state stays |
| Reset | Any session | POST `{ expectedRevision: <current> }` to `reset`, empty history and Solved State | Same as undo |
| 409 | Stale revision on undo or reset | Session reloaded with `getCube`, "The cube was changed by someone else" shown | Reload fails: its message shown, last cube stays |

</frozen-after-approval>

## Code Map

- `web/src/features/cube/useCubeSession.ts` -- `applyMove` body became `mutate(send)`; `applyMove`, `undo`, `reset` are one-line callers
- `web/src/features/cube/MoveHistory.tsx` -- ordered list of `cube.moves`, or "No moves yet"
- `web/src/features/cube/SessionControls.tsx` -- Undo (disabled without moves) and Reset buttons, focus handling
- `web/src/features/cube/CubePage.tsx` -- wires controls and history between the move pad and the cube view

## Tasks & Acceptance

**Execution:**
- [x] useCubeSession `mutate` / `undo` / `reset`, MoveHistory, SessionControls, CubePage wiring, with tests

**Acceptance Criteria:**
- Given a session with moves, then the history lists the API moves in order; an empty session shows "No moves yet"
- Given Undo, then the body is `{ expectedRevision: <current> }` and the view shows the state before the last move
- Given Reset, then the reset endpoint is called and the history is empty with the Solved State
- Given an empty session, then Undo is disabled
- Given 400, 500 or a network failure on undo or reset, then the message shows and the last good state stays
- Given 409 on undo or reset, then the session is reloaded and "The cube was changed by someone else" shows

## Implementation Notes

- Deferred entries: none fall inside 3.4 (drift check, `Program.cs`, CSharpier, `[FromServices]`, `UseStatusCodePages` and WCAG 2.1 AA are for 4.2), so none were taken or deleted.
- DRY: the 3.3 move path became `mutate(send)`, taking a function `(cubeId, revision) => Promise<ApiResult<CubeState>>`. One guard, one 409 reload, one notice path for move, undo and reset; 3.3 behaviour and tests are unchanged.
- Disabled Undo and focus: when the last Undo empties the history, the focused button becomes disabled and the browser drops focus. `SessionControls` moves focus to Reset (its neighbour, always enabled) on the true-to-false change of `canUndo`, only if Undo (or the page body) had focus. Not on first render, and not when another control has focus. Native `disabled` was kept over `aria-disabled` so the state is unambiguous to assistive technology and tests.
- Reset stays enabled on an empty session: it is harmless and gives the focus a place to land.
- History keys combine position and move (`F F` repeats); the list only changes at its end (append, undo) or empties, so there is no reorder problem.
- Manual check through the Vite proxy: create, move `F R'`, undo (revision changed, one move left), stale undo and stale reset both 409, reset returns the Solved State revision, `/cubes/<id>` serves the SPA. Both servers stopped.

## Spec Change Log

## Review Triage Log

Pass 1 (Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor; no layer failed; Acceptance Auditor found no AC violation).

| Finding | Verdict | Disposition |
|---|---|---|
| No test for a failed reload after a 409 on undo or reset (3 layers) | medium | Patched: hook test for undo and reset |
| Comment said the history only grows or empties; undo removes the last move | low | Patched in MoveHistory.tsx and the notes |
| explodedViewText and CUBE_URL duplicated across describe blocks in CubePage.test | low | Patched: one copy at file level |
| Undo and Reset results not announced to screen readers | low | Deferred: added to the 4.2 WCAG entry (status messages, 4.1.3) |
| Click on Undo or Reset dropped silently while a change is in flight | low | Rejected: same decision as 3.3 (buttons stay enabled so focus is kept; the window is one round trip) |
| Body-focus fallback may move focus to Reset when nothing in the controls held it | low | Rejected: needs Undo to become disabled while focus is on the body; Reset is only focused, never pressed; a tighter rule needs focus tracking |
| cube.moves undefined on a malformed response | false | The generated type has moves as required; the 3.1 client rejects non-JSON |
| Very long history pushes the cube down | low | Rejected: speculative, the fix is styling beyond the story |
| Reset has no confirmation; Reset on an empty session sends a request | low | Rejected: not in the story; noted as harmless |
| No Undo or Reset key bindings; move keys while a control is focused | low | Rejected: not required; Space and Enter are not move letters (3.3 note) |
| mutate depends on [state], callbacks change identity | low | Rejected: same as 3.3, negligible cost |
| Bare literals in tests (Failed to fetch, ready), one-off BAD_REQUEST_DETAIL | false | Same pattern as the 3.2 and 3.3 triage (expected data in tests) |
| Undo label does not name the move it removes | low | Rejected: not required by the story |
| Spec status vs sprint status, empty triage log | low | Patched: bookkeeping |

## Verification

**Commands:**
- `npm run test`, `npm run build`, `npx eslint .`, `npx prettier --check --end-of-line auto .` in `web/` -- expected: green (`npm run lint` fails on CRLF in the working tree, also on a clean main)
