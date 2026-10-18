---
title: 'Story 4.2b: WCAG 2.1 AA audit'
type: 'chore'
created: '2026-10-18'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'fb1f17caba15b1ec22c51e2627dbe3b9a4f273f8'
context:
  - '{project-root}/CLAUDE.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-4-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The UI was only checked for the basics. Not yet audited: contrast (also while busy and for the cube tiles), single-key shortcuts (2.1.4), screen-reader status messages (4.1.3), the Exploded view dialog, and reflow at 320 px (1.4.10).

**Approach:** Add an automated axe pass (WCAG 2.0/2.1 A and AA tags) to the e2e suite for each UI state, check the rest by measurement and by manual steps, fix only what fails, and record whatever is left in `deferred-work.md`.

## Boundaries & Constraints

**Always:** Single-letter shortcuts (2.1.4) get a "Keyboard shortcuts" switch in the Session group, on by default and not stored between visits (owner decision, 2026-10-18). Keep FR9 (letters turn the cube by default) and the API format; colours stay custom properties (`--colour-*`); every fix has a test (component test, e2e or axe); the audit result (what passed, what was fixed, what was deferred) goes into Implementation Notes.

**Never:** WCAG 2.2-only criteria (target size 2.5.8), new features such as the textarea (Story 3.7), CI, README or publishing. No commit until the owner says so.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Automated pass | cube page: new, with moves, busy, Exploded view open, unknown cube (error) | axe reports no violation for tags wcag2a, wcag2aa, wcag21a, wcag21aa | any violation fails the e2e test |
| Shortcuts switched off | "Keyboard shortcuts" off, key `F` pressed | no move is sent | N/A |
| Shortcuts on (default) | key `F` pressed | clockwise move as today | N/A |
| Change starts and ends | Undo, Reset, move or TTC pressed | a polite live region announces "Working…", then the history change | N/A |
| Reflow | viewport 320 px wide, and 640 px at 200% zoom | no horizontal page scroll, nothing cut off | N/A |
| Dialog | Exploded view opened with the keyboard | focus on Close, Tab stays inside, Escape closes, focus returns to the opener | N/A |

</frozen-after-approval>

## Code Map

- `web/e2e/ttc.spec.ts`, `web/playwright.config.ts` -- existing e2e flow and servers; the new `e2e/a11y.spec.ts` reuses them.
- `web/package.json` -- add devDependency `@axe-core/playwright` 4.13.0 (exact pin, as for the other packages).
- `web/src/features/cube/hooks/useMoveKeys.ts`, `CubePage.tsx` -- key handling; today active on the whole page.
- `web/src/features/cube/components/SessionControls.tsx` -- `busy-cue` span ("Working…") is plain; make it a live region.
- `web/src/features/cube/components/MoveHistory.tsx` -- silent history changes; add a visually hidden live status ("N moves, last X" / "No moves yet").
- `web/src/features/cube/components/ExplodedView.tsx` -- dialog with focus handling already; verify, fix only on failure.
- `web/src/app/App.css` -- `--colour-border-strong` (#8b93a3 gives 2.81:1 on the page, needs 3:1 for control borders; use #7d8596, 3.37:1) and `[aria-busy="true"] { opacity: 0.7 }` (muted text 3.46:1 and focus ring 2.81:1 while busy; use 0.85, 4.93:1 and 3.60:1).
- `web/src/features/cube/components/CubeView.css` -- tiles red 2.72, green 4.27, blue 1.83 against the board (needs 3:1 for graphics, 1.4.11); give every tile the light ring that White and Yellow already have.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- delete the merged WCAG entry when taken; add what stays open.
- `_bmad-output/planning-artifacts/epics.md`, `sprint-status.yaml` -- heading "Story 4.2b" says (NFR5), but NFR5 is cleanliness; drop the tag.

## Tasks & Acceptance

**Execution:**
- [x] `web/package.json`, `web/package-lock.json` -- add `@axe-core/playwright` 4.13.0 -- automated WCAG pass
- [x] `web/e2e/a11y.spec.ts` -- axe per state of the matrix, a 320 px reflow check, and the keyboard run of the dialog -- evidence for the audit
- [x] `web/src/app/App.css`, `CubeView.css` -- border, busy opacity and tile ring contrast fixes -- 1.4.3, 1.4.11
- [x] `SessionControls.tsx`, `MoveHistory.tsx` and tests -- live regions -- 4.1.3
- [x] `useMoveKeys.ts`, `CubePage.tsx` and tests -- shortcut mechanism chosen in the Open Question -- 2.1.4
- [x] Anything else axe or the manual run finds -- fix with a test, or record in `deferred-work.md`
- [x] `deferred-work.md`, `epics.md`, `sprint-status.yaml` -- take the WCAG entry, drop the NFR5 tag, update status

**Acceptance Criteria:**
- Given the e2e suite, when `npm run e2e` runs, then the axe tests pass for every state in the matrix.
- Given `npm run lint`, `npm run test`, `npm run build`, `dotnet build` and `dotnet test`, then all pass with zero warnings.
- Given the deferred list, then it holds only WCAG items that were checked and consciously left.

## Implementation Notes

**Audit result (WCAG 2.1 A and AA)**

- Automated: `e2e/a11y.spec.ts` runs axe (tags wcag2a, wcag2aa, wcag21a, wcag21aa, package `@axe-core/playwright` 4.13.0) on a new cube, a cube with moves, the busy state, the open Exploded view and the unknown-cube error. First run: no violation in the static states; the busy test only failed because the "Working…" cue was no `status` region (4.1.3). All pass now.
- Reflow (1.4.10): no horizontal scroll at 320 px (e2e). Exploded view: focus on Close, Tab stays, Escape closes, focus returns to the opener (e2e with the keyboard); no fix needed.
- Contrast, measured with the WCAG formula (axe cannot see these): control border `#8b93a3` was 2.81:1 on the page, now `#7d8596` = 3.37:1 (1.4.11); busy opacity 0.7 gave muted text 3.46:1 and a focus ring 2.81:1, now 0.85 = 4.93:1 and 3.60:1 (1.4.3, 1.4.11); tiles red 2.72, green 4.27 and blue 1.83 against the board, so every tile now has the light ring (5.02:1 against the board) that White and Yellow already had (1.4.11). Text/page 15.0, muted/page 7.1, danger/soft 5.75, on-board/board 11.1, focus/page 4.57: pass.
- 2.1.4: the "Keyboard shortcuts" switch (`ShortcutsToggle`, on by default, owner decision) turns the letters off; `useMoveKeys(onMove, enabled)`. A checkbox with focus no longer counts as a text field, otherwise the letters would stay dead after switching the shortcuts on with the mouse.
- 4.1.3: the busy cue is `role="status"`; `MoveHistory` has a visually hidden status ("Last move: X, N in total" / "History is empty").
- Not done and recorded in `deferred-work.md`: a manual screen-reader run, a manual 200% browser zoom, storing the switch between visits. WCAG 2.2-only rules (target size 2.5.8) were out of scope.
- The `(NFR5)` tag was dropped from the Story 4.2b heading: NFR5 is cleanliness, not accessibility.
- The merged WCAG entry was taken and deleted from `deferred-work.md`.
- Criteria from the old deferred list, with their result: 1.4.1 passes (every tile has its colour name as accessible name and the text view repeats the colours; no meaning rests on colour alone); 2.4.7 passes (3 px `--colour-focus` outline, 4.57:1, also on the checkbox; axe finds nothing); the Undo to Reset focus move was built in Story 3.4 and has a test; `aria-keyshortcuts` on the move buttons is advisory and was not added; 1.4.4 and 1.4.10 are covered by the 320 px and 640 px reflow tests, the manual 200% browser zoom stays in `deferred-work.md`.
- Review fixes: the "Working…" status now sits outside the `aria-busy` group (assistive technology may hold back live updates inside a busy subtree); the busy e2e test holds the request until the scan has finished; the reflow test runs at 320 and 640 px after one move; `useMoveKeys` takes `enabled` without a default so a new caller cannot forget the 2.1.4 switch.
- Deviation, for the owner to confirm: the frozen text says the switch is "in the Session group". It is a separate control (`ShortcutsToggle`) between the move pad and the history, not inside `SessionControls`, to keep the six props of `SessionControls` and its eleven tests unchanged. Moving it in is a small follow-up if the owner wants it.
- Not audited (recorded in `deferred-work.md`): 1.4.12 text spacing, forced-colors mode, 1.4.13 hover content (the UI has none), and a manual screen reader run.

- Owner decisions after the review: the switch stays a separate control (not inside the Session group); contrast gets tests. `e2e/contrast.spec.ts` reads the computed styles and checks the WCAG ratios: tile colours painted and ring against the board (3:1), control border against the page (3:1), muted text (4.5:1) and focus ring (3:1) at the busy opacity. Setting the opacity back to 0.7 or the border back to `#8b93a3` makes it fail (tried).
- Left in `deferred-work.md`: a screen-reader run, a manual 200% zoom, storing the switch, 1.4.12 text spacing and forced colors.

## Spec Change Log

## Review Triage Log

| Code review (bmad-code-review), finding | Verdict | Evidence |
|---------|---------|----------|
| Eight `.bak` files staged (4 reviewers) | high, patched | A `perl -pi` call left them; removed from the index and the disk. |
| "Working…" status inside the `aria-busy` group | medium, patched | The status now sits outside the group. |
| Reflow only at 320 px on a new cube | medium, patched | Runs at 320 and 640 px after one move. |
| Busy e2e test uses a fixed 3 s delay, no release | medium, patched | The request is held on a promise and released after the scan. |
| Deleted deferred criteria (1.4.1, 2.4.7, Undo focus, aria-keyshortcuts, 1.4.4) without verdicts | medium, patched | Verdicts added to Implementation Notes. |
| `enabled = true` default in `useMoveKeys` | low, patched | The parameter is required. |
| No test for an unchecked switch or for the default state | low, patched | Two tests added. |
| Literal "Moves" repeated in the e2e file | low, patched | Named constant. |
| Contrast fixes (border, 0.85 opacity, tile ring) have no test | medium, patched at the owner's request | `e2e/contrast.spec.ts` measures them from the computed styles; the test fails when the old values are restored. |
| Other criteria not named (1.4.12, forced colours, 1.4.13) | low, deferred | Added to the deferred entry. |
| Switch not "in the Session group" | low, kept | Owner decided to leave it as a separate control. |
| Identical live text after two different changes | low, rejected | The text changes whenever the last move or the total changes; a fix needs a counter in the announcement. |
| Other input types (range, color) treated as text | low, rejected | The page has none. |
| Keys active in the error and loading state | false | `applyMove` does nothing before the cube is loaded (tested in `useCubeSession.test.ts`). |
| Hint text repeats the letters; CSS literals; long comments | low, rejected | Hint is user text; CSS values are exempt by the CLAUDE.md clause; the 120 limit is for C#. |
| `lastMoveStatus` export breaks a lint rule | false | `npm run lint` reports no warning. |
| Spec status versus sprint-status | low, patched | Sprint status set to `review` after the review. |

## Verification

**Commands:**
- `npm run lint`, `npm run test`, `npm run build` (in `web/`) -- expected: green
- `npm run e2e` (in `web/`) -- expected: green, including the axe tests

**Manual checks:**
- Keyboard only: Tab through the page, open and close the Exploded view, switch the shortcuts off and on; check the focus is always visible.
- Screen reader (NVDA or Narrator): "Working…" and the history change are announced once per change.
