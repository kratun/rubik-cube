---
title: 'Story 3.3: Apply moves by buttons and keyboard (FR9)'
type: 'feature'
created: '2026-10-18'
status: 'in-review'
route: 'dispatch'
baseline_commit: '09a57783d77e84a0702d338c742b6229ab1e0f3b'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** A reviewer can see the cube but cannot turn it.

**Approach:** `useCubeSession` gains `applyMove(token)`; it posts `{ expectedRevision, moves: [token] }` with the last seen revision (`applyMoves` from `client.ts`) and shows the response. Twelve real buttons (`F F' R R' U U' B B' L L' D D'`) and a window key listener (`F R U B L D` clockwise, `f r u b l d` counter-clockwise) call it. The UI holds no cube logic (AD-3, AD-9).

## Boundaries & Constraints

**Always:** `applyMoves` from `client.ts`, relative `/api` paths only. Extend `useCubeSession`, no second hook. Named constants, no `any`, tests beside the file, queries by role.

**Never:** Change `web/src/api/generated/`; history, undo, reset (3.4); TTC button, e2e, README "Run the UI" (3.5); Docker, CI; compute layout or colours.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Button click | Ready cube | POST `{ expectedRevision: <current>, moves: [token] }`, view shows the response | N/A |
| Key `F`/`f` | Ready cube | Sends `F` / `F'` (same for R U B L D) | Ctrl, Alt or Meta held: ignored; other keys: ignored |
| 400 / 500 / network | Move fails | Message from the API (or the network message) shown, last good cube stays | Page stays usable |
| 409 | Revision is stale | Session reloaded with `getCube`, "The cube was changed by someone else" shown, action not applied | Reload fails: its message shown, last cube stays |

</frozen-after-approval>

## Code Map

- `web/src/features/cube/moves.ts` -- `MOVE_TOKENS` (12, derived from the six face letters), `moveForKey`
- `web/src/features/cube/useCubeSession.ts` -- `ready` state gains `notice`; `applyMove`; in-flight guard
- `web/src/features/cube/useMoveKeys.ts` -- `keydown` listener on `window` with cleanup
- `web/src/features/cube/MovePad.tsx` -- twelve buttons
- `web/src/features/cube/CubePage.tsx` -- wires hook, pad, keys, notice (`role="alert"`)

## Tasks & Acceptance

**Execution:**
- [x] moves, useMoveKeys, MovePad, useCubeSession and CubePage changes, with tests

**Acceptance Criteria:**
- Given a session, then twelve buttons labelled `F F' R R' U U' B B' L L' D D'` exist
- Given a click, then the body is `{ expectedRevision: <current>, moves: [<token>] }` and the view shows the response
- Given keys `F R U B L D` / `f r u b l d`, then the clockwise / counter-clockwise move is sent; Ctrl, Alt or Meta keys send nothing
- Given 400, 500 or a network failure, then the message shows and the last good cube stays
- Given 409, then the session is reloaded, "The cube was changed by someone else" shows and the move is not applied

## Implementation Notes

- Deferred entries: none fall inside 3.3 (drift check, `Program.cs`, CSharpier, `[FromServices]`, `UseStatusCodePages` are out of scope), so none were taken.
- Load error vs move error: `status: "error"` stays for loading (nothing to show; offers "Start a new cube"). A failed move is a `notice` on the `ready` state, so the last good cube stays visible.
- One move at a time: while a request is in flight further moves are ignored (a ref guard). A second move would carry the same stale revision and cause a false 409; a held key would flood the server. Buttons are not disabled, so keyboard focus is not lost.
- The guard opens in an effect after the new state has rendered, not when the response arrives, so the next move always reads the new revision.
- Keys: held-key repeats (`event.repeat`) are ignored. Letters pressed in a text field (`input`, `textarea`, `select`, content-editable) are not moves. On a focused button they are, since Space and Enter (the button's own keys) are not move letters.
- `App.test` counted six `group` roles; the move pad adds a seventh, so it now looks for the `up` face group.
- Caps Lock inverts the direction (the browser reports the upper-case key); accepted, the mapping follows the key as AD-9 states.

## Spec Change Log

## Review Triage Log

Pass 1 (Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor; no layer failed; Acceptance Auditor found no AC violation).

| Finding | Verdict | Disposition |
|---|---|---|
| Guard cleared in finally before React re-renders: a key in that gap sends the old revision (false 409) | medium | Patched: guard opens in an effect on state |
| Held key keeps sending a move per round trip | low | Patched: `event.repeat` ignored, plus test |
| bodyOf / bodyOfCall duplicated in two test files | low | Patched: `requestBodyOf` in apiResponseFixture |
| Spec bookkeeping (checkbox, log, App.test change, CRLF note) | low | Patched |
| Late move response after Start a new cube / unmount overwrites state | low | Rejected: the button exists only in the error state, where no move can be in flight; React ignores dispatch after unmount |
| No .catch on applyMoves / getCube | false | client.request never throws (3.1) |
| 409 then failed reload keeps the stale revision | low | Rejected: the next move gets 409 and reloads again; self-healing |
| applyMove changes identity per state, listener re-added | low | Rejected: correct, negligible cost |
| event.key undefined, role=textbox widgets, IME, Caps Lock | low | Rejected: speculative guards |
| Dropped move has no feedback, no aria-keyshortcuts, notice not dismissible | low | Rejected: not required by the story |
| Bare literals in tests, HTTP_CONFLICT duplicated in fixture | false | Same pattern as 3.2 (status literals in tests as expected data; production and fixture constants are separate) |
| Mojibake in the spec / LOADING_MESSAGE | false | Artifact of how the diff was written; files are UTF-8 |
| querySelector("pre") not by role | low | Rejected: same as 3.2 |
| moveFailed on non-ready state untested | low | Rejected: unreachable (applyMove starts only when ready) |

## Verification

**Commands:**
- `npm run lint`, `npm run test`, `npm run build` in `web/` -- expected: green (lint: `prettier --check` fails on CRLF files in the working tree, also on a clean main; check with `npx prettier --check --end-of-line auto .` and `npx eslint .`)
