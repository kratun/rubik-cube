# Deferred work

## Deferred from: code review of spec-3-1-frontend-scaffold (2026-10-18)

- No check that the committed `web/src/services/types/generated/schema.d.ts` matches the API. Regenerate with `npm run api:generate` (Development API on port 5080) and commit it with every contract change; deploy backend and frontend together. A CI step that runs the script and fails on a non-empty `git diff` closes it (AD-11, optional); take it with CI (Story 5.3).

## Left open by the WCAG audit (Story 4.2b)

- source_spec: `_bmad-output/implementation-artifacts/spec-4-2b-wcag-2-1-aa-audit.md`
  summary: A manual run with a real screen reader (NVDA or Narrator) and a manual 200% browser zoom check have not been done; the automated axe pass, the 320 px reflow test and the keyboard run of the dialog have.
  evidence: The announcements ("Working…", the history status) are live regions covered by component tests, but only a screen reader proves they are spoken once per change. Ask the owner to run it once before submission.

- source_spec: `_bmad-output/implementation-artifacts/spec-4-2b-wcag-2-1-aa-audit.md`
  summary: The "Keyboard shortcuts" choice is not remembered between visits.
  evidence: The switch is on after every reload (owner decision: no storage). A user who needs the shortcuts off must switch them off on every visit; `localStorage` would close it.

- source_spec: `_bmad-output/implementation-artifacts/spec-4-2b-wcag-2-1-aa-audit.md`
  summary: WCAG 2.1 criteria not audited in 4.2b: 1.4.12 text spacing and forced-colors (high contrast) mode.
  evidence: axe 4.13.0 passes the five UI states and `e2e/contrast.spec.ts` measures tile, border and busy contrast, but neither overrides text spacing nor emulates forced colors. A Playwright run with `forcedColors: "active"` and a style override for 1.4.12 would close it.

## For Story 3.7 (not yet written)

- source_spec: none
  summary: Idea from the owner (2026-10-18), decided to become Story 3.7: add a textarea with an Apply button for typing a move sequence, and let the "Apply TTC test sequence" button put the TTC text into that textarea for testing.
  evidence: Open choices for the story: whether Apply appends to the current cube or resets first; whether the TTC button only fills the text or also applies it. Taking it changes the Story 3.5 acceptance criterion ("one click") and the e2e flow (an extra click). The API already validates tokens (400), so the frontend would only split the text on whitespace. Typing into the textarea must not trigger the move shortcuts (the hook already ignores text fields).

## For the quality pass follow-up

- source_spec: `_bmad-output/implementation-artifacts/spec-4-2a-quality-pass-and-cleanup-nfr3-nfr5.md`
  summary: Mirror the 4.2a structure in the architecture spine: the `Extensions/` extension methods, `StatusCodeErrorWriter` for empty 4xx and 5xx responses, and `SuppressMapClientErrors`.
  evidence: The code review of 4.2a noted that the spine does not mention them; the spine is a planning artifact outside the story's scope.
