---
title: 'Story 1.1: Solution scaffold with a clean build'
type: 'chore'
created: '2026-10-17'
status: 'done'
baseline_commit: '1282d7d9f85589adb1a9728f19ad4219f4599824'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/CLAUDE.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The repo has no code. Every later story needs a consistent, warning-free .NET base whose layer rules (AD-1) are enforced by the compiler and a test.

**Approach:** Scaffold one solution with the four `src` projects, four xunit.v3 `*.Tests` projects, central build files and a README, wired by the AD-1 references, with an architecture test in `Rubik.Api.Tests`. No behaviour yet.

## Boundaries & Constraints

**Always:** SDK 10.0.302 pinned (`rollForward: latestPatch`); `Nullable` and `TreatWarningsAsErrors` in `Directory.Build.props`; every package version only in `Directory.Packages.props` (central package management); versions from the spine Stack table. References exactly per AD-1: Api to Application and Infrastructure; Infrastructure to Application; Application to Domain; Domain to nothing. Each `*.Tests` project references its own `src` project. Zero warnings on `dotnet build`; `dotnet test` green.

**Never:** create `web/`, `docker-compose.yml`, CI or any cube/domain code (Epics 1.2+, 2, 3, 5); leave template leftovers (`WeatherForecast`, `Class1.cs`, `UnitTest1.cs`, demo endpoints); put a version in a `.csproj`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Rules respected | Real Rubik assemblies | Architecture test passes | N/A |
| Forbidden edge | Reference map with e.g. Domain to Application, Application to Infrastructure, Infrastructure to Api, or Api-only edge missing from allowed set | Rule check returns the violations; test asserts they are reported | Test fails naming the offending edge |
| Domain isolation | Domain assembly | References no other `Rubik.*` assembly | Test fails naming the assembly |

</frozen-after-approval>

## Code Map

- `.gitignore` -- already ignores `_bmad/`, `.claude/`, `docs/local/`; add `bin/`, `obj/` and IDE folders.
- `README.md` -- currently a 2-line stub; extend, keep its description line.
- `docs/ARCHITECTURE.md` -- existing; do not change.
- `CLAUDE.md` -- project rules; do not change.
- Spine Structural Seed -- target layout (`src/`, `tests/`, root build files).

## Tasks & Acceptance

**Execution:**
- [x] `global.json` -- SDK 10.0.302, `rollForward: latestPatch`, `test.runner: Microsoft.Testing.Platform` -- verified: without it `dotnet test` fails on .NET 10 with xunit.v3
- [x] `Directory.Build.props` -- `net10.0`, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`, `LangVersion` default -- single place for rules
- [x] `Directory.Packages.props` -- `ManagePackageVersionsCentrally`; xunit.v3 4.0.2 (Mvc.Testing, Swashbuckle, Diagnostics.Testing are added by the stories that use them)
- [x] `Rubik.slnx` -- solution at the repo root so `dotnet build` / `dotnet test` need no path
- [x] `src/Rubik.Domain`, `src/Rubik.Application`, `src/Rubik.Infrastructure` -- empty class libraries with AD-1 references, no files besides the csproj
- [x] `src/Rubik.Api` -- Web SDK project with a minimal `Program.cs` (build and run the app, nothing else); references Application and Infrastructure
- [x] `tests/Rubik.{Domain,Application,Infrastructure}.Tests` -- xunit.v3 projects with no tests yet; each sets `--ignore-exit-code 8` (verified: zero tests exits 8) with a comment to remove it when the first test lands
- [x] `tests/Rubik.Api.Tests` -- references Api; `ArchitectureRules` (allowed-edge table plus `Violations(...)`) and `ArchitectureTests` (real assemblies via `GetReferencedAssemblies`, plus synthetic forbidden maps proving it fails)
- [x] `README.md` -- prerequisites (.NET 10 SDK), `dotnet build`, `dotnet test`
- [x] `.gitignore` -- add build output and IDE entries

**Acceptance Criteria:**
- Given a fresh clone with the .NET 10 SDK, when I run `dotnet build`, then it succeeds with 0 warnings and contains the four `src` and four `tests` projects.
- Given the build files, when a project builds, then SDK 10.0.x is pinned, Nullable and TreatWarningsAsErrors are on and no `.csproj` holds a package version.
- Given the references, when `dotnet test` runs, then the architecture test passes and fails for any edge outside AD-1.
- Given the repo, when I open `README.md`, then it lists the prerequisites and both commands, and no template leftovers exist.

## Implementation Notes

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| Real-assembly architecture test is vacuous: compiler drops unused project references (Blind, Edge, Gap) | high | patch | Reproduced by the gap layer. Fixed with `ProjectGraph` reading csproj `ProjectReference` items; verified failing with a temporary Api to Domain reference |
| `latestPatch` rejects SDK 10.0.4xx; README wording inaccurate (Blind, Edge) | medium | patch | Band 3xx only; set `latestFeature`, reworded README |
| Hard-coded assembly list never discovers new projects (Edge) | medium | patch | Covered by the csproj discovery assertion in the same fix |
| `TestResults/` not ignored (Edge) | low | patch | Added to `.gitignore` |
| `Assembly.Load` opaque failure (Blind, Edge) | low | rejected | Fails loudly with FileNotFoundException; a guard adds complexity |
| `--ignore-exit-code 8` repeated in 3 csproj, may be forgotten (Blind, Edge) | low | rejected | Similar by coincidence; each is removed with its own first test; comment marks it |
| xunit.v3 4.0.2 and runner property unverified (Blind, Gap) | false | rejected | Restored, 11 tests ran green on SDK 10.0.302 |
| Missing analyzers, .editorconfig, .gitattributes, README layout/run sections, Program.cs stub, Mvc.Testing, IsPackable, tests-props Condition, allowPrerelease (Blind, Edge) | low | rejected | Speculative or owned by later stories; not required by 1.1 |

## Design Notes

Compilers drop unused assembly references from metadata, so reflection on empty projects cannot see a reference nobody uses. The real-assembly check is therefore a guard that gains strength as code lands; the rule logic is proven separately with synthetic maps. Use `.slnx` (default solution format of SDK 10).

## Verification

**Commands:**
- `dotnet build` -- expected: Build succeeded, 0 Warning(s), 0 Error(s)
- `dotnet test` -- expected: all projects pass, exit code 0
- `git status --short` -- expected: only intended files (no `bin/`, `obj/`)
