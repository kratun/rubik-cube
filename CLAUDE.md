# Rubik's Cube Simulator: project rules

Read before writing or changing any code. Source of truth: `_bmad-output/planning-artifacts/prds/prd-rubik-cube-2026-10-14/prd.md` (what) and the architecture spine in `_bmad-output/planning-artifacts/architecture/` (how). Where this file and the spine disagree, the spine wins.

The author keeps more detailed .NET and React best-practices references in `docs/local/`. They are not in git; if the folder is missing, follow the spine.

## Principles (apply to every project and every file)

- **SOLID**: one reason to change per class (SRP); extend by adding types, not editing working ones (OCP); implementations honour their interface contracts (LSP); small, role-specific interfaces (ISP); depend on abstractions, with ports owned by the inner layer (DIP).
- **DRY**: one place per piece of knowledge (one rotation function for all faces, one Result-to-HTTP mapping, one fetch wrapper). Do not merge code that is only similar by coincidence.
- **KISS**: the simplest design that meets the requirement; no speculative abstractions, no patterns without a present need.
- **No magic values**: no bare numbers, strings or other literals in logic or tests. Give each a named `const`, `static readonly` field, enum or well-named variable (for example `CubieCount`, not `26`). Applies to production code and tests, backend and frontend. Test data that defines the expected value itself (a literal vector or colour in a `[MemberData]` table) is fine.
- **Exceptions are allowed**, but never silent. Leave a one-line comment at the spot saying which principle is bent and why, and mention it in the PR or commit message.

## Tests (every project, every file with behaviour)

- Each `src` project has a `*.Tests` project; the frontend has tests next to the file (`Foo.test.tsx`).
- Every class or module with logic gets tests in the same change. DTOs, records, DI registration and `Program.cs` are covered by the integration tests instead.
- No change is done while its tests are missing or failing. Build with zero warnings.

## Architecture guardrails

- Backend dependency direction: Api to Application and Infrastructure; Infrastructure to Application; Application to Domain; Domain depends on nothing. A test enforces it.
- Frontend talks to the backend only through relative `/api/...` paths and the generated OpenAPI types.
- No demo code, template leftovers or unused files.
