# Epic 4 Context: Reviewer-ready delivery

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

A reviewer gets a clean, warning-free repository with a README that works from a fresh clone, an architecture note, an accessibility-checked UI and a published GitHub repository. Epic 4 runs last of the required epics; the suggested order is 4.2 (4.2a, 4.2b), 4.3, 4.1, because the fresh-clone check needs the published repository.

## Stories

- Story 4.1: README verified in a fresh clone
- Story 4.2a: Quality pass and cleanup
- Story 4.2b: WCAG 2.1 AA audit
- Story 4.3: Publish to GitHub

## Requirements & Constraints

- No demo code, template leftovers or unnecessary files; `dotnet build`, `dotnet test`, `npm run lint`, `npm run test`, `npm run build` and `npm run e2e` pass with zero warnings.
- Every deliberate exception to SOLID, DRY or KISS carries a one-line comment; no magic values in code, tests or CSS (colours and fonts are custom properties).
- The WCAG audit fixes only what fails and records the rest in `deferred-work.md`; the API format and the generated types do not change.
- The owner decides which planning material is submitted: `_bmad-output/`, `CLAUDE.md` and the `docs/` sources go in; `_bmad/`, `.claude/` and `docs/local/` stay ignored.

## Technical Decisions

- C# is formatted by the CSharpier local tool (`dotnet csharpier check .`), at most 120 characters per line.
- Frontend layout: `web/src/services`, `web/src/app`, and per feature `components/`, `hooks/`, `utils/`, `fixtures/`; ESLint enforces the import rules and a vitest case proves them.
- Same-origin topology: the UI calls relative `/api/...` paths; Vite proxies to the API on port 5080 (web dev on 5173).

## Cross-Story Dependencies

- 4.2b follows 4.2a (it reuses the CSS custom-property rule and the lint and test set-up).
- 4.1 clones from GitHub, so 4.3 runs before it.
