---
title: 'Story 3.6: UI polish (FR8 to FR11)'
type: 'feature'
created: '2026-10-18'
status: 'done'
baseline_commit: '6ccd88c4c285e3c898f083cc1022b795ca187aa7'
route: 'dispatch'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The cube page works but looks unfinished: unstyled controls, a tall numbered history, no busy cue while a request runs, plain error text, light tiles with no visible edge.

**Approach:** Plain CSS and small markup changes in `web/src/features/cube` (acceptance criteria of Story 3.6 in `epics.md`). The hook exposes a `busy` flag; no API, cube logic or generated-types change.

## Boundaries & Constraints

**Always:** Components reach the server only through hooks; `utils/` has no React; fixtures only in tests; queries by role; no bare literals in code or tests; C# untouched; the first click is the only one sent and buttons stay enabled while busy (3.3 decision), so focus is not lost.

**Never:** WCAG 2.1 AA audit, contrast ratios, status announcements, axe/Lighthouse, shortcut remapping (Story 4.2); textarea for move sequences; Docker/CI; a UI library; edits to `web/src/services/types/generated/`; a second session hook.

</frozen-after-approval>

## Code Map

- `web/src/features/cube/hooks/useCubeSession.ts` -- add `busy` to the ready state; `moveInFlight` ref stays as the synchronous guard and is released when `busy` is false again.
- `web/src/features/cube/CubePage.tsx` (+ `CubePage.css`) -- page grid: net as main element, side panel with controls and history, text view below; alert banner.
- `web/src/features/cube/components/AlertBanner.tsx` -- new, one `role="alert"` banner for errors and the conflict notice (DRY).
- `MovePad.tsx`, `SessionControls.tsx`, `MoveHistory.tsx`, `CubeView.tsx/.css` -- markup and styles per the criteria.
- `web/src/app/App.tsx` + `App.css` -- page title, fonts, shared button hover/focus, responsive width.
- `web/e2e/ttc.spec.ts` -- reads `listitem` in the History region; must stay green.

## Tasks & Acceptance

**Execution:**
- [x] `useCubeSession.ts` + test -- `busy` true from the start of a change until its answer (success, failure, 409 reload) -- busy cue
- [x] `AlertBanner.tsx` + test, `CubePage.tsx/.css` -- banner, layout, pass `busy` down
- [x] `MovePad`, `SessionControls`, `MoveHistory`, `CubeView`, `App` (+ CSS, tests) -- visible group labels, face pairs, chips, primary/destructive styles, face labels, light-tile edge, larger monospace

**Acceptance Criteria:**
- Given the criteria of Story 3.6, when `lint` (`npx prettier --check --end-of-line auto .` and `npx eslint .`), `vitest`, `build` and `e2e` run, then all pass.

## Implementation Notes

- Deferred entries taken: none (the WCAG, textarea and tooling entries are out of scope).
- `useCubeSession` returns `busy` (a `useState`); `SessionState` and the `moveInFlight` ref are unchanged, so existing hook tests keep their shape. `MOVE_TOKENS_BY_FACE` in `utils/moves.ts` feeds the face pairs; `MOVE_TOKENS` derives from it.
- Layout: `CubePage.css` places the fragment children of `CubeView` (net, text) and the side panel with grid areas; side by side from 60rem, stacked below. The shared look and the single button hover and focus style live in `app/App.css`; button colours go through custom properties, so the primary and danger variants need no specificity tricks.
- After the owner's look at the page: the exploded view opens in a modal dialog (`ExplodedView.tsx`, a custom `role="dialog"` because jsdom has no `showModal`; Escape, Close or a click outside closes it, focus returns to the opener, `useMoveKeys` ignores keys inside it). The history chips copy the size of the move buttons but are flat; buttons have a raised edge. Beside the cube the pair is centred as a whole and the history fills the height down to the bottom of the cube and scrolls inside (11rem cap when stacked).
- Last round: Session is above Moves; "Show exploded view" is a Session button (Undo, Reset and the opener on row 1, TTC across row 2). `ExplodedView` is now a controlled dialog (`CubePage` holds `open`); `CubeView` draws only the net. `epics.md` (Story 3.6, 3.2 and 3.5 criteria) and the README run section were updated to match.
- `ErrorBoundary` now uses `AlertBanner` (app may import features; ESLint agrees).
- Checked in Chromium at 1366, 375 and 320 px: no horizontal overflow; the TTC run shows the expected text and history. Servers stopped.

## Spec Change Log

## Review Triage Log

Pass 1 (Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor; no layer failed).

| Finding | Verdict | Disposition |
|---|---|---|
| Net wider than a 320px screen (tile floor 1.35rem) | medium | Patched: floor 1rem, 6vw; measured 0px overflow at 320, 375 and 1366 |
| Busy cue appears and shifts the layout | low | Patched: cue row always rendered with a fixed height |
| `toBeVisible` on captions means nothing in jsdom | low | Patched: `toBeInTheDocument` |
| `setBusy(false)` skipped if `send` rejects or throws | false | The client never throws (AD-7 wrapper), same as the 3.5 triage |
| Stale answer sets busy after a new cube or unmount | false | Moves exist only in the ready state; Start a new cube is only offered in the error state; `cubeId` is fixed per page |
| Two sources of truth (ref and busy) | low | Rejected: commented; the ref must be synchronous, the state drives the display |
| Busy cue only in the Session group, none in the pad | low | Rejected: the pad is dimmed and `aria-busy`; the cue sits directly below it |
| No live region for the busy cue; dimming at opacity 0.7 may lower contrast | low | Deferred to the WCAG pass (4.2), as the story says |
| `ErrorBoundary` imports a feature component | false | `eslint .` passes; the rules only forbid the inner layers |
| Controls below the net under 60rem | low | Rejected: the AC says parts stack on phones; the net stays first |
| Unknown colour name is transparent | low | Rejected: closed colour set from the API; a guard adds complexity |
| Grid areas depend on CubeView being a fragment | low | Rejected: documented in the CSS comment |
| Empty history style, alert style in App.css, pair tuple type, CSS literals | low | Rejected: cosmetic or no named harm |
| No test for a rejected fetch | false | Cannot happen (see the first `false` row) |
| Spec bookkeeping | low | Patched |
