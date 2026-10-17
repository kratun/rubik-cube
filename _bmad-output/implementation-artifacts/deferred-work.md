# Deferred work

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-strict-move-parsing-part-of-fr2.md`
  summary: `ArchitectureTests` checks project references only; a `PackageReference` added to `Rubik.Domain` would not fail a test.
  evidence: `tests/Rubik.Api.Tests/Architecture/ProjectGraph.cs` reads `ProjectReference` edges only. Pre-existing from Story 1.1; 1.3 does not touch any csproj. Candidate for Story 4.2 (quality pass).

- source_spec: `_bmad-output/implementation-artifacts/spec-2-1-create-a-cube-session-fr4.md`
  summary: Story 3.1 generates the frontend API types from a running Development API: `npm run generate-api` fetches `http://localhost:5080/swagger/v1/swagger.json` (openapi-typescript, AD-7) and the output in `web/src/api/generated/` is committed.
  evidence: Swagger is served in Development only (2.1), so Production never needs it. A contract change (including a hotfix) must regenerate and commit the types together with the API change, and deploy backend and frontend together. A CI check that regeneration leaves `git diff` empty is optional (AD-11); `swagger tofile` from `Swashbuckle.AspNetCore.Cli` is the fallback if a running server becomes inconvenient.

- source_spec: `_bmad-output/implementation-artifacts/spec-2-2-read-a-cube-session-and-report-errors-fr5-nfr4.md`
  summary: Requests that match no route (unknown path, wrong method such as 405, unsupported media type 415) return the framework's empty 4xx, not `ErrorDetails`, and are not logged by the Api.
  evidence: The mapper and the exception handler only see requests that reach a controller. A `UseStatusCodePages` hook routing empty 4xx responses through `ErrorReporter` would close it; take it at the quality pass (Story 4.2), with tests for 404, 405 and 415; the new POST routes make 405 and 415 real.

- source_spec: `_bmad-output/implementation-artifacts/spec-2-3-apply-moves-with-a-revision-check-fr6.md`
  summary: Optional, decide at the quality pass (Story 4.2): switch `CubesController` from one constructor parameter per use case to `[FromServices]` parameters on each action, as a separate small change with no behaviour change.
  evidence: The constructor takes the use cases plus `ResultMapper` and grows with every operation (Story 2.4 adds two). With `[FromServices]` the constructor stays at `ResultMapper` and each action declares only the use case it calls. A missing registration still fails loudly (500) and the integration tests do not change. Not needed for any story, so it waits unless the constructor becomes a nuisance.
