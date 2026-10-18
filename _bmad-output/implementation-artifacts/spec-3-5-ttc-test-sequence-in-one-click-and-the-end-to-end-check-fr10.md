---
title: 'Story 3.5: TTC Test Sequence in one click and the end-to-end check (FR10, NFR3)'
type: 'feature'
created: '2026-10-18'
status: 'done'
baseline_commit: 'd29e016deb6b1246097ba278606508b3834edd55'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/CLAUDE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** A reviewer cannot compare the cube with the PDF without typing six moves, and nothing proves that API and web app work together in a browser.

**Approach:** An "Apply TTC test sequence" button runs reset-then-apply through the shared `mutate` path of `useCubeSession`. One Playwright flow (`npm run e2e`) starts both servers and checks the exact text. The README gets "Run the UI".

## Boundaries & Constraints

**Always:** Relative `/api` paths through `resetCube` and `applyMoves`; extend `useCubeSession`, no second hook; history and text come from the API; no bare literals; the expected lines and moves come from `tests/Fixtures/` (story 1.5), not re-invented.

**Never:** Touch `web/src/services/types/generated/`; Docker, CI, drift check, WCAG, retrospective; a new API shape (stop and ask).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Solved session | No moves | One `moves` request with the six tokens; text equals the nine lines; history `F R' U B' L D'` | N/A |
| Session with moves | Any moves | `reset` first, then `moves` with the revision from the reset response; same result as above | N/A |
| Reset ok, apply fails (400/500/network) | Server is at the Solved State | The Solved State from the reset response stays, with the failure message (the last state the server confirmed) | Message in the notice |
| 409 on either request | Stale revision | Session reloaded with `getCube`, "The cube was changed by someone else" | Reload fails: its message, last cube stays |
| Reset fails | Any error but 409 | Old cube stays, message shown | As above |
| Second click in flight | Request pending | Ignored (one change at a time) | N/A |

</frozen-after-approval>

## Code Map

- `web/src/features/cube/hooks/useCubeSession.ts` -- `mutate(send)`; `send` now takes the cube and returns an outcome that can carry the reset cube when only the second step failed
- `web/src/features/cube/components/SessionControls.tsx` -- third button next to Undo and Reset
- `web/src/features/cube/CubePage.tsx` -- passes `applyTtcSequence`
- `web/src/features/cube/utils/ttcSequence.ts` -- new: the six tokens (no React)
- `web/src/features/cube/fixtures/ttcCubeFixture.ts` -- new: expected lines read from `tests/Fixtures/ttc-exploded-view.txt` (`?raw`)
- `web/e2e/ttc.spec.ts`, `web/playwright.config.ts` -- new: one flow, `webServer` starts API (5080, `/health`) and Vite (5173)
- `web/package.json`, `web/vite.config.ts`, `web/tsconfig.json`, `web/eslint.config.js`, `web/.gitignore` -- `e2e` script, Playwright 1.64.1 and `@types/node`, vitest excludes `e2e/`
- `README.md` -- "Run the UI", two-terminal run, test commands, `api:generate` prerequisite
- `tests/Fixtures/ttc-sequence.txt`, `ttc-exploded-view.txt` -- source of truth, unchanged
- `_bmad-output/implementation-artifacts/deferred-work.md` -- delete the `api:generate` README entry (taken)

## Tasks & Acceptance

**Execution:**
- [x] `ttcSequence.ts` + test -- tokens match `ttc-sequence.txt`
- [x] `useCubeSession.ts` + tests -- `applyTtcSequence`, partial-failure outcome
- [x] `SessionControls.tsx`, `CubePage.tsx` + tests -- button; solved, with moves, 400/500/network, 409, double click
- [x] e2e files and config -- `npm run e2e` starts and stops both servers
- [x] `README.md`, `deferred-work.md`, `sprint-status.yaml`

**Acceptance Criteria:**
- Given a solved session, when the button is pressed, then the text view equals the nine lines and the history reads `F R' U B' L D'`
- Given a session with moves, when pressed, then reset runs first and the result equals the solved-session result
- Given `npm run e2e`, then one browser flow asserts the exact text and no process stays running
- Given the README, then it has "Run the UI", the two-terminal run with expected output, and the unit, component and e2e commands

## Implementation Notes

- Deferred entries taken: the `api:generate` README prerequisite (from the 3.1 review). Its README half is deleted from `deferred-work.md`; the drift check and CI half stays for 5.3. Out of scope and left: `Program.cs`, CSharpier, `[FromServices]`, `UseStatusCodePages`, WCAG 2.1 AA, ESLint rule test.
- `mutate(send)` now passes the whole cube to `send` and gets `{ result, confirmed }` back. `confirmed` is the reset cube when the reset worked and the apply step failed (400, 500, network): the server is already solved, so that state is shown with the failure message instead of the old cube. A 409 on either request reloads the session with `getCube` (same path as undo and reset). A failed reset keeps the old cube.
- A session without moves skips the reset request (it would be a no-op); the check reads `cube.moves` from the API, no state is computed.
- Shared fixtures: `ttcSequence.test.ts` and `fixtures/ttcCubeFixture.ts` import `tests/Fixtures/*.txt` with `?raw`; `vite.config.ts` adds `../tests/Fixtures` to `server.fs.allow`. The e2e test reads the same files with `node:fs`. `e2e/` is excluded from Vitest.
- `npm run e2e` (`playwright.config.ts`): two `webServer` entries (API with `/health` on 5080, Vite on 5173), `reuseExistingServer: false`. The exact text is compared with `textContent` because `toHaveText` collapses whitespace. Needs `npx playwright install chromium` once (in the README). `@types/node` was added for the config and the fixture read.
- Manual check through the Vite proxy: new cube plus the six moves gives the nine lines; a cube with an extra move, reset, then the six moves gives the same. Both servers stopped, ports 5080 and 5173 free.

## Spec Change Log

## Review Triage Log

Pass 1 (Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor; no layer failed).

| Finding | Verdict | Disposition |
|---|---|---|
| 409 on the sequence after a successful reset, then the reload fails: old cube stays though the server is solved | medium | Patched: the confirmed reset cube is shown with the reload error; hook test added |
| TTC_HISTORY is a second copy of the sequence; e2e fixture file names are bare strings | low | Patched: derived from TTC_TEST_SEQUENCE; named constants |
| Fixture read breaks on CRLF | false | .gitattributes keeps tests/Fixtures/*.txt LF; tests pass on this Windows checkout |
| Skipping the reset when moves is empty may hit a non-solved cube | false | AD-4: the cube is derived from the move list, so no moves means the Solved State |
| Orphan dotnet process after e2e; no process-left check | false | Checked after two runs: ports 5080 and 5173 free, no Rubik process left |
| Chromium not installed gives a late error | low | Rejected: README documents the install; a pre-script would download on every run |
| Click before the page loaded | false | getByRole click waits for the button to exist |
| send() rejects and leaves the in-flight guard set | false | The client never throws (AD-7 wrapper) |
| Partial-failure tests only use 500 or network; no 400 case; e2e covers only the solved path | low | Rejected: same code path as 500; the reset-first path has component and hook tests |
| Hardcoded tokens in ttcSequence.ts | false | Production source; ttcSequence.test.ts checks it against tests/Fixtures |
| "Failed to fetch" repeated in tests | low | Rejected: same pattern accepted in the 3.2 to 3.4 triage |
| No busy state while the sequence runs; second click ignored | low | Rejected: same decision as 3.3 and 3.4 |
| node types in the shared tsconfig | low | Rejected: needs a second tsconfig for little gain |
| server.fs.allow unnecessary | false | Removing it gave "Denied ID" in Vitest |
| README may go stale (version sample, duplicate npm install) | low | Rejected: sample output is labelled as differing |
| confirmed detected by object identity | low | Rejected: commented, covered by tests |
| Bookkeeping: unchecked tasks, empty log | low | Patched |

## Design Notes

Reset and apply are two requests, so the pair is not atomic. If apply fails after reset succeeded, the server is already solved; showing the old cube would be wrong (the next click would give a false 409). So the reset response is shown with the failure notice. A solved session (no moves) skips the reset request: it would be a no-op.

## Verification

**Commands:**
- `npx vitest run`, `npx eslint .`, `npx prettier --check --end-of-line auto .`, `npm run build`, `npm run e2e` in `web/` -- expected: green
