# Deferred work

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-strict-move-parsing-part-of-fr2.md`
  summary: `ArchitectureTests` checks project references only; a `PackageReference` added to `Rubik.Domain` would not fail a test.
  evidence: `tests/Rubik.Api.Tests/Architecture/ProjectGraph.cs` reads `ProjectReference` edges only. Pre-existing from Story 1.1; 1.3 does not touch any csproj. Candidate for Story 4.2 (quality pass).

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-turn-any-face-fr2.md`
  summary: `Cube`, `Cubie` and `Sticker` have no value equality (records compare their lists by reference); 1.4 tests compare cubes through a test-side snapshot.
  evidence: Needed only if a later story must compare cube states in production code (for example Story 2.4 undo/reset, or an `IsSolved` check). Add it there with its own tests rather than as speculative public surface now.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-exploded-view-and-the-ttc-test-sequence-fr3.md`
  summary: The TTC expected output exists twice: in `tests/Fixtures/ttc-exploded-view.txt` (test source of truth) and as a block in `README.md`; no test ties them together.
  evidence: The README block must be readable without running anything, so it cannot just point at the fixture. Add a README-versus-fixture check when the quality pass (Story 4.2) or the README rewrite (Epic 4) touches it.

- source_spec: `_bmad-output/implementation-artifacts/spec-2-1-create-a-cube-session-fr4.md`
  summary: Story 3.1 generates the frontend API types from a running Development API: `npm run generate-api` fetches `http://localhost:5080/swagger/v1/swagger.json` (openapi-typescript, AD-7) and the output in `web/src/api/generated/` is committed.
  evidence: Swagger is served in Development only (2.1), so Production never needs it. A contract change (including a hotfix) must regenerate and commit the types together with the API change, and deploy backend and frontend together. A CI check that regeneration leaves `git diff` empty is optional (AD-11); `swagger tofile` from `Swashbuckle.AspNetCore.Cli` is the fallback if a running server becomes inconvenient.

- source_spec: `_bmad-output/implementation-artifacts/spec-2-2-read-a-cube-session-and-report-errors-fr5-nfr4.md`
  summary: Requests that match no route (unknown path, wrong method such as 405, unsupported media type 415) return the framework's empty 4xx, not `ErrorDetails`, and are not logged by the Api.
  evidence: The mapper and the exception handler only see requests that reach a controller. A `UseStatusCodePages` hook routing empty 4xx responses through `ErrorReporter` would close it; take it after 2.3 adds its routes, with tests for 404, 405 and 415.
