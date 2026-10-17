---
title: 'Story 1.5: Exploded view and the TTC Test Sequence (FR3)'
type: 'feature'
created: '2026-10-17'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context:
  - '{project-root}/CLAUDE.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The cube can turn, but nothing shows it as the exploded view, so the TTC Test Sequence `F R' U B' L D'` cannot be compared with the required output (FR3).

**Approach:** Add `ExplodedViewRenderer` to `Rubik.Domain` (AD-3). From a sticker's normal it finds the face; the cubie position gives row and column in the net orientation (Up on top; Left, Front, Right, Back in one row; Down below). One call returns both the six 3x3 `faces` grids and the nine `explodedView` text lines, so tiles and text always agree. `Cube` exposes no layout. The colour letter (W Y G B R O) lives in one place. The expected TTC output is a shared fixture file for later API and UI tests. README gets the "Run the tests" expected output. Domain references no package and no project.

</frozen-after-approval>

## Implementation Notes

`ExplodedViewRenderer.Render(Cube)` finds each sticker's face from its normal and places it by `Vector3.Dot(position, rowDirection/columnDirection)` with the net orientations written in the renderer (the test helper keeps its own copy as an independent oracle). It returns `ExplodedView(Faces, Lines)`; `Lines` is built from the `Faces` grids. `ColorExtensions.Letter` is the single colour-to-letter table (the test helper uses it too). Shared fixtures: `tests/Fixtures/ttc-sequence.txt` and `ttc-exploded-view.txt`, copied to the Domain test output and read by `TtcFixtureTestHelper`. README "Run the tests" shows the expected output. Tests: `ExplodedViewRendererTests`, `ColorExtensionsTests`. The existing local branch `feat/1-5-exploded-view` (same commit as main) was left alone; work is on `feat/1-5-exploded-view-and-ttc-sequence`.

## Review Triage Log

Layers: Blind Hunter (run twice), Edge Case Hunter, Verification Gap, Acceptance Auditor. No AC violation.

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| Bare literals in renderer (`1`, `' '`) and in test net-position table (Blind, Auditor) | low | patch | Named `TileWidth`, `IndentCharacter`, band/slot constants |
| `Faces` dictionary could be downcast and mutated (Edge, Blind) | low | patch | Wrapped in `ReadOnlyDictionary` |
| Duplicate row-count assert; README double blank line and mixed line endings (Blind, Auditor) | low | patch | Fixed |
| Net reader compared only on TTC; solved cube is symmetric (Blind) | low | patch | Theory over `""`, `U`, `R'`, `B L'`, TTC |
| Csproj comment claimed fixtures reach every test project (Blind) | low | patch | Comment reworded |
| README block duplicates the fixture, no tie test (Blind, Gap, Auditor) | low | defer | deferred-work.md |
| Fixtures wired only into Domain.Tests (Blind, Auditor) | low | defer | deferred-work.md |
| Unwritten cell, out-of-range index, unknown normal, null cube (Edge, Blind) | low | rejected | Cube is immutable with a private constructor, so a malformed cube is unreachable; guards add code for no path |
| `ExplodedView` public constructor / reference equality on records (Blind) | low | rejected | No caller compares or builds views; same reasoning as the 1.4 equality deferral |
| `FaceOf` allocates per sticker (Blind, Auditor) | low | rejected | 54 stickers per render, negligible |
| Orientation table duplicated in test helper (Blind) | false | rejected | Intentional independent oracle, stated in the helper comment |
| Fixture trailing-whitespace guard, `ColorExtensions` class doc, null message in throw (Blind) | low | rejected | Fixture checked by exact-match test; throw style matches `FaceExtensions` |
| Spec sections empty, status in-progress (Gap, Auditor) | false | rejected | Filled in this step |
