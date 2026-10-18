# Deferred work

## Deferred from: code review of spec-3-1-frontend-scaffold (2026-10-18)

- No check that the committed `web/src/services/types/generated/schema.d.ts` matches the API. Regenerate with `npm run api:generate` (Development API on port 5080) and commit it with every contract change; deploy backend and frontend together. A CI step that runs the script and fails on a non-empty `git diff` closes it (AD-11, optional); take it with CI (Story 5.3).

## For Story 4.2b (WCAG 2.1 AA audit)

- source_spec: `_bmad-output/implementation-artifacts/spec-3-3-apply-moves-by-buttons-and-keyboard-fr9.md`
  summary: Check the UI against WCAG 2.1 Level AA in Story 4.2b and fix what fails (merged from the Story 3.3 and Story 3.6 review entries).
  evidence: Stories 3.2 and 3.3 only cover the basics (real buttons, colour names on tiles, errors in `role="alert"`, queries by role). Not yet checked: colour contrast of text and tiles (1.4.3, 1.4.11), including while `aria-busy` dims the group to opacity 0.7 (text and focus ring); colours not being the only signal (1.4.1); visible focus (2.4.7); keyboard shortcuts that do not clash with assistive technology (2.1.4: single-character keys need a way to turn off or remap; today `F R U B L D` work on the whole page; `aria-keyshortcuts` on the move buttons was suggested in the 3.3 review); status messages announced to screen readers (4.1.3): the busy cue "Working…" is a plain span, and a successful move, undo or reset changes the history silently (for example an aria-live region); the focus move from Undo to Reset when the history empties; the Exploded view dialog (focus trap, Escape, focus return); reflow and zoom to 200% (1.4.4, 1.4.10); target size; an automated pass (axe or Lighthouse) plus a manual keyboard and screen reader run.

## For Story 3.7 (not yet written)

- source_spec: none
  summary: Idea from the owner (2026-10-18), decided to become Story 3.7: add a textarea with an Apply button for typing a move sequence, and let the "Apply TTC test sequence" button put the TTC text into that textarea for testing.
  evidence: Open choices for the story: whether Apply appends to the current cube or resets first; whether the TTC button only fills the text or also applies it. Taking it changes the Story 3.5 acceptance criterion ("one click") and the e2e flow (an extra click). The API already validates tokens (400), so the frontend would only split the text on whitespace.

## For the quality pass follow-up

- source_spec: `_bmad-output/implementation-artifacts/spec-4-2a-quality-pass-and-cleanup-nfr3-nfr5.md`
  summary: Add `dotnet tool restore` to the CLAUDE.md line about CSharpier and to the README prerequisites (Story 4.1).
  evidence: CSharpier is a local tool; on a fresh clone `dotnet csharpier check .` fails until `dotnet tool restore` has run. The fix edits CLAUDE.md, so the review deferred it.

- source_spec: `_bmad-output/implementation-artifacts/spec-4-2a-quality-pass-and-cleanup-nfr3-nfr5.md`
  summary: Mirror the 4.2a structure in the architecture spine: the `Extensions/` extension methods, `StatusCodeErrorWriter` for empty 4xx and 5xx responses, and `SuppressMapClientErrors`.
  evidence: The code review of 4.2a noted that the spine does not mention them; the spine is a planning artifact outside the story's scope.

- source_spec: `_bmad-output/implementation-artifacts/spec-4-2a-quality-pass-and-cleanup-nfr3-nfr5.md`
  summary: No test checks that the tile colours are painted (a misspelt `--colour-tile-*` name would give transparent tiles).
  evidence: Component tests run in jsdom without the stylesheet. One Playwright computed-style check on a tile would close it; the WCAG contrast work in 4.2b can include it.
