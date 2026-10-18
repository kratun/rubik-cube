---
title: 'Story 3.1: Frontend scaffold with generated API types and a typed client'
type: 'feature'
created: '2026-10-18'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The UI stories (3.2 to 3.5) have no React project, no typed access to the API contract and no tested fetch layer.

**Approach:** Scaffold `web/` (Vite, React, strict TypeScript, ESLint, Prettier, Vitest), generate the types from the running Development API's OpenAPI document, and add a hand-written `ApiResult<T>` fetch wrapper with typed cube operations. Decision (user, 2026-10-18): the OpenAPI document marked every property nullable and optional, so the backend is fixed to publish `required` properties and required request bodies instead of working around it in the client.

## Boundaries & Constraints

**Always:** Relative `/api/...` paths only (AD-8); Vite on 5173 proxies `/api` to `http://localhost:5080`; generated output committed; named constants, no `any`, no template leftovers; tests beside the file.

**Never:** Cube view, buttons, history, TTC button (3.2 to 3.5); Docker, CI, `[FromServices]`, `UseStatusCodePages` (4.2); client-side workarounds for the OpenAPI document.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Success | 2xx JSON | `{ ok: true, data }` | N/A |
| API error | non-2xx with `ErrorDetails` | `{ ok: false, status, error }` | N/A |
| Network failure | fetch rejects | `{ ok: false, status: 0, error }` generic message | N/A |
| Unreadable body | non-2xx without `ErrorDetails`, or 2xx not JSON | `{ ok: false, status, error }` generic message | N/A |

</frozen-after-approval>

## Code Map

- `src/Rubik.Api/Program.cs` -- registers non-nullable support and the two OpenAPI filters
- `src/Rubik.Api/OpenApi/` -- `RequiredPropertiesSchemaFilter`, `RequiredRequestBodyOperationFilter`
- `tests/Rubik.Api.Tests/OpenApi/`, `TestHelpers/OpenApiDocumentTestHelper.cs` -- tests on the published document
- `web/` -- Vite project: `vite.config.ts` (proxy, Vitest), `eslint.config.js`, `.prettierrc.json`, `tsconfig.json`
- `web/src/api/generated/schema.d.ts` -- output of `npm run api:generate` (committed)
- `web/src/api/{types,apiResult,client}.ts` -- contract aliases, `ApiResult<T>`, wrapper and cube operations
- `web/src/app/App.tsx`, `web/src/main.tsx` -- minimal shell for 3.2 to build on

## Tasks & Acceptance

**Execution:**
- [x] Backend OpenAPI: required properties and request bodies, with tests
- [x] `web/` scaffold with lint, test, build, dev and `api:generate` scripts
- [x] Fetch wrapper, typed operations and tests for success, API error, network failure and unreadable body

**Acceptance Criteria:**
- Given Node 24, when `npm run dev` runs, then Vite serves on 5173 and proxies `/api` to 5080
- Given the API running, when `npm run api:generate` runs, then types are written to `web/src/api/generated/` and committed
- Given the project, when `npm run lint`, `test` and `build` run, then all pass with no `any` and no template leftovers

## Implementation Notes

- Deferred entry taken and deleted: the Story 2.1 entry about `npm run generate-api`; the script is named `api:generate` as the acceptance criterion says.
- `overrides` in `web/package.json` lets openapi-typescript 7.13.0 (peer `typescript@^5`) use the spine's TypeScript 6.0.3 without `--legacy-peer-deps`; generation verified.
- `eslint.config.js` is linted without type information and is not in `tsconfig.json`.
- README "Run the UI" is Story 3.5's scope and was not touched.

## Spec Change Log

## Review Triage Log

Pass 1 (bmad-code-review: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor; no layer failed).

| Finding | Verdict | Disposition |
|---|---|---|
| Request types exported but bodies untyped (client not typed against the contract) | medium | Patched: bodies typed as ApplyMovesRequest and ExpectedRevisionRequest |
| Bare "GET"/"POST" and fixture strings in client and tests | low | Patched: HTTP_GET, HTTP_POST and named fixture constants |
| Schema filter could publish an empty required array; nullable branch untested | medium | Patched: guard plus two unit tests |
| No drift check between schema.d.ts and the API; regenerate rule lost with the deleted entry | medium | Deferred (CI, Story 5.3), new entry in deferred-work.md |
| Generated file may predate the filters | false | schema.d.ts was generated after the filters and shows required fields |
| overrides with $typescript may not resolve | false | npm install succeeded and generation ran on TypeScript 6.0.3 |
| error.status can differ from HTTP status; null or 204 2xx body; empty cubeId; no AbortSignal or timeout | low | Rejected: the API never returns these; speculative guards |
| $ref or oneOf nullable properties treated as required | low | Rejected: no such property in the contract |
| api:generate URL duplicates the Vite proxy origin; no .nvmrc; main.tsx untested; undisposed test client; hand-listed test paths | low | Rejected: cosmetic, same pattern as existing tests |

## Verification

**Commands:**
- `npm run lint`, `npm run test`, `npm run build` in `web/` -- expected: green
- `dotnet build` -- expected: 0 warnings; `dotnet test` -- expected: all pass
