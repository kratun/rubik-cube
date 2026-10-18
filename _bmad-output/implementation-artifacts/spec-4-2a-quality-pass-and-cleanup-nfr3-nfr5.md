---
title: 'Story 4.2a: Quality pass and cleanup (NFR3, NFR5)'
type: 'chore'
created: '2026-10-18'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '448a3c3854b945533a304356fefa11b5073080ea'
context:
  - '{project-root}/CLAUDE.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The code base has deferred quality work: lines over 120 characters, a fat `Program.cs` and controller constructor, empty 4xx responses outside `ErrorDetails`, an architecture test blind to `PackageReference`, unproven ESLint layer rules, two oversized test files, and a lint run that warns on CRLF checkouts.

**Approach:** One quality pass, in three steps: (1) a feature-neutral change set, (2) CSharpier as its own mechanical commit, (3) the submission decision. The WCAG audit is Story 4.2b. The textarea idea is Story 3.7, not taken here.

## Boundaries & Constraints

**Always:** No change to the API format or to `web/src/services/types/generated/`. Zero warnings. Every moved or new class with behaviour has tests in its `FooTests`. Named constants, no magic values. Spine wins over CLAUDE.md. Deliberate exceptions to SOLID/DRY/KISS get a one-line comment.

**Never:** WCAG fixes, CI, Docker, GitHub publishing, README fresh-clone check, drift check on generated types. No commit before the owner says so.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Unknown route | `GET /api/nope` | 404 `ErrorDetails`, logged at Warning | N/A |
| Wrong method | `PUT /api/cubes` | 405 `ErrorDetails`, logged | N/A |
| Wrong media type | `POST` moves, `text/plain` | 415 `ErrorDetails`, logged | N/A |
| Domain package | `PackageReference` in `Rubik.Domain.csproj` | architecture test fails | names the package |
| Layer violation | component imports `services/client` | ESLint reports `no-restricted-imports` | N/A |

</frozen-after-approval>

## Code Map

- `src/Rubik.Api/Program.cs` -- composition root; move registrations and Swagger setup into extension methods in `src/Rubik.Api` (e.g. `AddCubeServices`, `AddOpenApiDocument`, `UseDevelopmentTools`), keep behaviour; add `UseStatusCodePages`.
- `src/Rubik.Api/Cubes/CubesController.cs` -- constructor takes five use cases plus `ResultMapper`; switch to `[FromServices]` per action, constructor keeps `ResultMapper`.
- `src/Rubik.Api/Errors/ErrorReporter.cs` -- single place that builds `ErrorDetails` and logs; the status-code hook must go through it, not duplicate it.
- `tests/Rubik.Api.Tests/Architecture/ProjectGraph.cs`, `ArchitectureRules.cs`, `ArchitectureTests.cs` -- reads `ProjectReference` only; add `PackageReference` check (Domain and Application must declare none).
- `tests/Rubik.Api.Tests/Cubes/CubesControllerTests.cs` (619 lines) -- move client, log and JSON helpers (`CreateLoggingClient`, `PostMovesAsync`, `ReadStateAsync`, `ReadBodyAsync`, `SingleFailure`, …) to `TestHelpers/*TestHelper.cs`; no second test class.
- `web/eslint.config.js` -- `no-restricted-imports` layer rules; add a vitest case that lints virtual files via the ESLint API.
- `web/.prettierrc.json` -- add `endOfLine: "auto"` so `npm run lint` passes on CRLF.
- `web/src/features/cube/CubePage.test.tsx` (595), `hooks/useCubeSession.test.ts` (567) -- repeated `readySession` helpers; move to a shared test helper file in the same folder.
- `.editorconfig`, `.config/dotnet-tools.json` (new) -- CSharpier local tool and config.
- `CLAUDE.md` -- state that "no magic values" covers CSS: custom properties for colours and fonts, media-query breakpoints exempt.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- delete the taken entries (CSharpier, `Program.cs`, `[FromServices]`, `UseStatusCodePages`, PackageReference, ESLint rule test); the textarea entry stays, noted as Story 3.7; both WCAG entries merge into one entry for 4.2b; drift check stays for 5.3.
- `.gitignore` -- keep `_bmad/`, `.claude/`, `docs/local/` ignored; `_bmad-output/`, `CLAUDE.md` and `docs/` sources are submitted.

## Tasks & Acceptance

**Execution:**
- [x] `src/Rubik.Api/Program.cs` and new extension files -- thin the root -- optional entries taken together
- [x] `src/Rubik.Api/Cubes/CubesController.cs` -- `[FromServices]` -- constructor stops growing
- [x] `src/Rubik.Api` status-code handling plus tests -- 404, 405, 415 as `ErrorDetails`
- [x] Architecture tests -- `PackageReference` check with its own tests in `ArchitectureTests`
- [x] `tests/Rubik.Api.Tests/TestHelpers/` -- extract helpers from `CubesControllerTests`
- [x] `web/` -- `endOfLine`, ESLint rule test, shared test helpers, `npm run lint` green
- [x] Repo sweep -- find demo files, dead or commented-out code, magic values, missing tests; fix
- [x] `CLAUDE.md`, `deferred-work.md`, `epics.md`, `sprint-status.yaml` -- apply the decisions above
- [x] CSharpier -- last: install tool, `dotnet csharpier format .`, `dotnet csharpier check .` green, kept as its own clean diff

**Acceptance Criteria:**
- Given the repo, when `dotnet build`, `dotnet test`, `npm run lint`, `npm run test`, `npm run build` and `npm run e2e` run, then all pass with zero warnings.
- Given `dotnet csharpier check .`, then it passes and no C# line exceeds 120 characters.
- Given the 405 and 415 requests above, then the body is `ErrorDetails` with the same `traceId` as the log entry.
- Given the final tree, then no demo files, dead code or commented-out code remain, and `deferred-work.md` holds only entries not taken here.

## Implementation Notes

- Deferred entries taken (deleted from `deferred-work.md`): CSharpier, `Program.cs` extension methods, `[FromServices]`, `UseStatusCodePages`, `PackageReference` architecture test, ESLint layer-rule test. The two WCAG entries are merged into one for Story 4.2b; the textarea entry is relabelled for Story 3.7; the drift check stays for Story 5.3.
- Epic 2 and 3 retro items done here: CSharpier, `CubesControllerTests` helpers moved to `TestHelpers/` (`CubesApiTestHelper`, `FailureLogTestHelper`), `endOfLine: "auto"`, CSS clause in `CLAUDE.md`, shared `renderReadySession` for `useCubeSession.test.ts`. Item 10 (WCAG) is split off to 4.2b.
- Deviation from the Code Map: the `PackageReference` rule covers `Rubik.Domain` only, as the spine says ("No packages" is stated for the Domain only). Application, Infrastructure and Api may use packages.
- `CubePage.test.tsx` stays one file: its helpers differ per `describe` and are small, so its size comes from the cases, not from duplication (retro item 13).
- `.editorconfig` needed no change: CSharpier reads `max_line_length = 120` from it.
- `SuppressMapClientErrors = true` is needed: without it MVC answers 415 with its own `ProblemDetails`, which the status code pages never see (the failing 415 case proved it).
- Tile colours in `CubeView.css` moved to `--colour-tile-*` custom properties to follow the new CSS clause; the repo sweep found no other hex values, demo files, dead or commented-out code.
- Doc comments over 120 characters were wrapped by hand (CSharpier does not wrap comments). The CSharpier reformat is part of the same working tree, so the history will show it in the commit the owner chooses.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence |
|---------|---------|----------|
| Package check covers Domain only, not Application (3 reviewers) | false | The spine states "No packages" for the Domain only; the spec Code Map line was wider than the spine, and the spine wins (see Implementation Notes). |
| `ProgramTests` constants named after values, duplicated `CubesPath`, redundant using | low, patched | Replaced by `StatusCodes.Status4xx` and the shared `CubesPath`. |
| ESLint rule test could pass vacuously on a parse failure | low, patched | Test now asserts there is no fatal message. |
| Empty `title` for a status without a reason phrase | false | Every 4xx/5xx the framework produces without a body has a phrase; 499 returns before the title is read. |
| `UnhandledDetail` is generic for controller-returned empty 4xx | low, rejected | Fix would add per-status text; the traceId and status carry the information. |
| `LoadPackages` ignores props files and `Update=` items | low, rejected | No package is declared there today; fix adds complexity. |
| Writer resolved from `app.Services` in the extension | false | `Program.cs` area is the composition root; the writer is a singleton. |
| `WriteAsJsonAsync` options differ from MVC | false | Both use web defaults (camelCase); the integration test reads `status` and `traceId` by name. |
| `CubePage.test.tsx` not changed | false | Decision recorded in Implementation Notes (retro item 13). |
| Line length not met in the reviewed diff | false | The reviewed diff is the pre-format content; `dotnet csharpier check .` and the 120-character scan pass on the tree. |
| CLAUDE.md does not mention `dotnet tool restore` | defer | Fix edits CLAUDE.md; recorded in `deferred-work.md`. |
| Race between abort check and write in the writer | low, rejected | Needs a try/catch for a harmless aborted write; `GlobalExceptionHandler` has the same shape. |


| Code review (bmad-code-review), finding | Verdict | Evidence |
|---------|---------|----------|
| Implementation Notes empty, tasks unchecked | medium, patched | A `git checkout` during the first review round had reverted the unstaged spec edits; the notes, checkboxes and status were written again. |
| `LoadPackages` never proven to see a package; Domain key could be missing | medium, patched | `LoadPackages_RealProjects_SeesAPackageAndTheDomainProject` asserts the Domain key and `Swashbuckle.AspNetCore` in `Rubik.Api`. |
| `LoadPackages` duplicates `Load` | low, patched | Both use one private `LoadItems` helper. |
| ESLint test passes vacuously for an ignored file | low, patched | Test now fails on a fatal message or a "File ignored" warning. |
| First ESLint case can exceed the 5 s vitest timeout | medium, patched | It did fail at 5.6 s under a full parallel run; both tables now use a 30 s timeout. |
| `.config/dotnet-tools.json` without final newline | low, patched | Newline added. |
| Package check excludes Application (4 reviewers) | false | See Implementation Notes: the spine names the Domain only. |
| `CubePage.test.tsx` unchanged (3 reviewers) | false | See Implementation Notes. |
| `--colour-tile-*` defined on `.cube-net`, tiles could sit elsewhere | false | `.tile` is rendered only inside `.cube-net` (`CubeView.tsx`). |
| Fonts not moved to `--font-*` | false | Fonts already use `var(--font-sans)` and `var(--font-mono)`. |
| Path logged at Warning for unknown routes (flooding, log forging) | low, rejected | The spec asks for these requests to be logged; structured logging keeps the value as one field; a fix needs sanitising rules nobody asked for. |
| Empty title, abort race, generic detail text, service locator, `ProgramTests` thin | low or false | Same as the first round (see above). |
| Extensions/StatusCodeErrorWriter not in the architecture spine | defer | The spine is a planning artifact outside this story; recorded in `deferred-work.md`. |
| No test renders the tile colours | defer | Static CSS; recorded in `deferred-work.md`. |
| Sprint key `4-2-...` versus 4.2a | low, rejected | Kept on purpose so the build workflow resolves it; `4-2b-...` cannot collide. |
| `readySessionFixture` is a helper in `fixtures/` | low, rejected | The ESLint rules allow it and CLAUDE.md puts test-only code there. |

## Verification

**Commands:**
- `dotnet build` -- expected: 0 warnings, 0 errors
- `dotnet test` -- expected: all pass
- `dotnet csharpier check .` -- expected: no unformatted files
- `npm run lint`, `npm run test`, `npm run build`, `npm run e2e` (in `web/`) -- expected: all green
