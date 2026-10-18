# Deferred work

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-strict-move-parsing-part-of-fr2.md`
  summary: `ArchitectureTests` checks project references only; a `PackageReference` added to `Rubik.Domain` would not fail a test.
  evidence: `tests/Rubik.Api.Tests/Architecture/ProjectGraph.cs` reads `ProjectReference` edges only. Pre-existing from Story 1.1; 1.3 does not touch any csproj. Candidate for Story 4.2 (quality pass).

- source_spec: `_bmad-output/implementation-artifacts/spec-2-2-read-a-cube-session-and-report-errors-fr5-nfr4.md`
  summary: Requests that match no route (unknown path, wrong method such as 405, unsupported media type 415) return the framework's empty 4xx, not `ErrorDetails`, and are not logged by the Api.
  evidence: The mapper and the exception handler only see requests that reach a controller. A `UseStatusCodePages` hook routing empty 4xx responses through `ErrorReporter` would close it; take it at the quality pass (Story 4.2), with tests for 404, 405 and 415; the new POST routes make 405 and 415 real.

- source_spec: `_bmad-output/implementation-artifacts/spec-2-3-apply-moves-with-a-revision-check-fr6.md`
  summary: Optional, decide at the quality pass (Story 4.2): switch `CubesController` from one constructor parameter per use case to `[FromServices]` parameters on each action, as a separate small change with no behaviour change.
  evidence: The constructor takes the use cases plus `ResultMapper` and grows with every operation (Story 2.4 adds two). With `[FromServices]` the constructor stays at `ResultMapper` and each action declares only the use case it calls. A missing registration still fails loudly (500) and the integration tests do not change. Not needed for any story, so it waits unless the constructor becomes a nuisance.

- source_spec: `_bmad-output/implementation-artifacts/spec-2-4-undo-and-reset-fr7.md`
  summary: Adopt CSharpier at the quality pass (Story 4.2): add it as a local tool, format the whole C# code base once, and run `dotnet csharpier check .` as a check (and in CI, Story 5.3).
  evidence: `.editorconfig` now sets `max_line_length = 120`, but nothing enforces it and about 86 lines in `src` and `tests` are longer (19 over 140), for example the `CubesController` constructor. `dotnet build` does not warn on line length, and `dotnet format` does not wrap lines. CSharpier puts each parameter on its own line with the closing parenthesis on a new line, which is the style written in `CLAUDE.md`. Do the formatting as its own change so the diff is mechanical.

## Deferred from: code review of spec-3-1-frontend-scaffold (2026-10-18)

- No check that the committed `web/src/api/generated/schema.d.ts` matches the API. Regenerate with `npm run api:generate` (Development API on port 5080) and commit it with every contract change; deploy backend and frontend together. A CI step that runs the script and fails on a non-empty `git diff` closes it (AD-11, optional); take it with CI (Story 5.3). The README should state the `api:generate` prerequisite (Story 3.5, "Run the UI").

- source_spec: `_bmad-output/implementation-artifacts/spec-3-1-frontend-scaffold-with-generated-api-types-and-a-typed-clien.md`
  summary: Make `Program.cs` thinner at the quality pass (Story 4.2): move the service registrations and the Swagger setup into extension methods, for example `AddCubeServices`, `AddOpenApiDocument` and `UseDevelopmentTools`, in the Api project.
  evidence: `Program.cs` now holds MVC and error-mapping setup, the OpenAPI filters (3.1 added three lines for them), nine use case and adapter registrations, the exception handler and the Development-only Swagger. Each new operation or filter adds lines to it. Extension methods keep it a short composition root and let each group be read on its own. Behaviour must not change; the integration tests and `ProgramTests` already cover the wiring. Optional, like the `[FromServices]` entry above; do both in the same pass if taken.

- source_spec: `_bmad-output/implementation-artifacts/spec-3-3-apply-moves-by-buttons-and-keyboard-fr9.md`
  summary: Check the UI against WCAG 2.1 Level AA at the quality pass (Story 4.2) and fix what fails.
  evidence: Stories 3.2 and 3.3 only cover the basics (real buttons, colour names on tiles, errors in `role="alert"`, queries by role). Not yet checked: colour contrast of text and tiles (1.4.3, 1.4.11), colours not being the only signal (1.4.1), visible focus (2.4.7), keyboard shortcuts that do not clash with assistive technology (2.1.4: single-character keys need a way to turn off or remap; today `F R U B L D` work on the whole page), status messages announced to screen readers (4.1.3), reflow and zoom to 200% (1.4.4, 1.4.10), target size, and an automated pass (axe or Lighthouse) plus a manual keyboard and screen reader run. `aria-keyshortcuts` on the move buttons was also suggested in the 3.3 review. Story 3.4 adds the history list and the Undo and Reset buttons: a successful move, undo or reset changes the history silently, so check 4.1.3 for those too (for example an aria-live region), and the focus move from Undo to Reset when the history empties.
