---
title: 'Story 1.4: Turn any face (FR2)'
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

**Problem:** The cube has a solved state and a parsed `Move`, but nothing turns a face, so no sequence can be applied or verified (FR2).

**Approach:** Add `Cube.Apply(Move)` to `Rubik.Domain`, returning a new `Cube` (immutable). One shared rotation function, parameterised by `Face` and `Direction`, rotates position and sticker normals of the 9 cubies in the layer by 90 degrees (AD-2). Axes: x = Right(+)/Left(-), y = Up(+)/Down(-), z = Front(+)/Back(-). No permutation tables, no per-face logic, no layout or renderer on `Cube` (AD-3, story 1.5). Domain references no package and no project.

</frozen-after-approval>

## Implementation Notes

`Cube.Apply(Move)` selects the 9 cubies with `position . faceNormal == 1` and rotates position and sticker normals with `Vector3.RotateClockwiseAbout(axis)` (`v x axis + axis (axis . v)`, exact in integers); counter-clockwise uses the negated normal as axis. A private `Cube` constructor wraps the new cubie list; the model stays immutable. `Vector3` gained `+`, `*`, `Cross` and the rotation. Tests: `CubeTests` (reference nets for all 12 moves, centres and 12 adjacent tiles, four-turn and inverse identities, `R U R' U'` x6, immutability), `TestHelpers/CubeNetTestHelper` (test-only net reader, AD-3), `Vector3Tests`. State comparison uses a test-side snapshot (no production equality).

## Review Triage Log

Layers: Blind Hunter (also run once before the formal review), Edge Case Hunter, Verification Gap, Acceptance Auditor. The Acceptance Auditor found no AC violation.

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| Unknown `Direction` silently turned counter-clockwise (Blind, Edge) | medium | patch | `switch` with throwing default, like `Face.Normal`; test `Apply_UndefinedDirection_Throws` |
| Reference nets only for clockwise F, R, U; Left/Back/Down and counter-clockwise unpinned (Blind, Gap, Auditor) | medium | patch | Added hand-derived nets for F', R', L, B, D; all pass, so the implementation matches them |
| `CubeNet` helper has logic but no tests (Blind, Gap) | low | rejected (after review) | Dropped by the author: the F, R, U reference nets already exercise the helper, and its own tests were noise |
| `Orientations` used bare `Vector3(1, 0, 0)` literals (Blind, Auditor) | low | patch | Now `Vector3.UnitX` etc. |
| `Opposite(Face)` duplicated face knowledge in a test (Blind) | low | patch | Replaced by a normal comparison |
| `ReferenceNets` comment did not match the data (Blind) | low | patch | Comment rewritten |
| No known-sequence composition test (Blind, Gap) | low | patch | `R U R' U'` x6 returns to solved |
| No value equality on `Cube`/`Cubie` (Blind, Gap) | low | defer | Recorded in deferred-work.md |
| `RotateClockwiseAbout` public with an unenforced unit-axis precondition (Blind, Edge) | low | rejected | Documented; a guard adds code for a path no caller reaches |
| Repeated `.ToList().AsReadOnly()` (Blind, Gap) | low | rejected | Three short call sites; a helper adds more than it saves |
| Private constructor skips invariant checks (Gap) | false | rejected | Only `Apply` calls it, and tests assert 26 cubies / 54 stickers after each move |
| Test splitting, scramble reliance, `Single` message, bare `0` in `Range`, Vector3Tests input literals (Blind, Auditor) | low | rejected | Cosmetic; inputs are expected-value data |
| Spec sections empty, sprint `last_updated` churn (Gap, Blind) | false | rejected | Filled in this step; the workflow requires the sync |
