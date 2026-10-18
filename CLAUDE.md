# Rubik's Cube Simulator: project rules

Read before writing or changing any code. Source of truth: `_bmad-output/planning-artifacts/prds/prd-rubik-cube-2026-10-14/prd.md` (what) and the architecture spine in `_bmad-output/planning-artifacts/architecture/` (how). Where this file and the spine disagree, the spine wins.

The author keeps more detailed .NET and React best-practices references in `docs/local/`. They are not in git; if the folder is missing, follow the spine.

## Principles (apply to every project and every file)

- **SOLID**: one reason to change per class (SRP); extend by adding types, not editing working ones (OCP); implementations honour their interface contracts (LSP); small, role-specific interfaces (ISP); depend on abstractions, with ports owned by the inner layer (DIP).
- **DRY**: one place per piece of knowledge (one rotation function for all faces, one Result-to-HTTP mapping, one fetch wrapper). Do not merge code that is only similar by coincidence.
- **KISS**: the simplest design that meets the requirement; no speculative abstractions, no patterns without a present need.
- **No magic values**: no bare numbers, strings or other literals in logic or tests. Give each a named `const`, `static readonly` field, enum or well-named variable (for example `CubieCount`, not `26`). Applies to production code and tests, backend and frontend. Test data that defines the expected value itself (a literal vector or colour in a `[MemberData]` table) is fine.
- **Line length**: C# lines are at most 120 characters (`.editorconfig`). When a parameter list or argument list does not fit, put every parameter on its own line, indented, with the closing parenthesis on its own line (the CSharpier style). Existing longer lines are reformatted once at the quality pass (Story 4.2), not piecemeal.
- **Exceptions are allowed**, but never silent. Leave a one-line comment at the spot saying which principle is bent and why, and mention it in the PR or commit message.

## Tests (every project, every file with behaviour)

- Each `src` project has a `*.Tests` project; the frontend has tests next to the file (`Foo.test.tsx`).
- Every class or module with logic gets tests in the same change. DTOs, records, DI registration and `Program.cs` are covered by the integration tests instead.
- Test classes mirror production classes: the tests of `Foo` live in `FooTests` (same folder structure and namespace as `Foo`), one test class per production class. A new method on an existing class gets its tests in that class's existing `FooTests`, not in a new `FooBarTests`. Split only when a file becomes unwieldy, and then by moving data or helpers to `TestHelpers/`, not by adding a second test class for the same type.
- Test-only helpers (code that exists just to support tests, not a test itself) live in a `TestHelpers/` folder of the test project, namespace `{TestProject}.TestHelpers`, and the class name ends in `TestHelper` (for example `CubeNetTestHelper`). Test helpers do not get their own tests (they add noise; the tests that use them already exercise them). Production code never references a helper.
- No change is done while its tests are missing or failing. Build with zero warnings.

## Before starting a story

- Read `_bmad-output/implementation-artifacts/deferred-work.md`. Take into the story only the entries that fall inside its scope; leave the rest.
- When a story takes an entry, delete it from `deferred-work.md` in the same change and name the entry in the story spec's Implementation Notes (git history keeps the old text). Do not mark entries as done: a file that only grows stops being read.

## Architecture guardrails

- Backend dependency direction: Api to Application and Infrastructure; Infrastructure to Application; Application to Domain; Domain depends on nothing. A test enforces it.
- A class receives its collaborators through its constructor. Do not create a collaborator with `new` inside another class (a hidden dependency, for example a helper built from a repository the class only passes on); register it in `Program.cs` and inject it. Data types, records and plain values are fine to create with `new`.
- Frontend talks to the backend only through relative `/api/...` paths and the generated OpenAPI types.
- Frontend layout: `web/src/services/` (client and `types/`), `web/src/app/`, and per feature `components/`, `hooks/`, `utils/`, `fixtures/` next to the feature's page. Put a new file where its role says; `utils/` has no React, `hooks/` do not import `components/`, `components/` reach the server only through `hooks/`, `fixtures/` are for tests only. ESLint (`no-restricted-imports`) enforces it; the full rules are in the architecture spine ("Frontend layout rules").
- No demo code, template leftovers or unused files.
