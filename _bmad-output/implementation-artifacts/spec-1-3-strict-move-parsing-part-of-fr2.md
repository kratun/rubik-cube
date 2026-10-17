---
title: 'Story 1.3: Strict move parsing (part of FR2)'
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

**Problem:** Moves arrive as text. Without a strict parser, invalid input could be silently misread, and the rotation (1.4) and the API (Epic 2) have no typed move to work with.

**Approach:** Add `Move` (face plus direction) to `Rubik.Domain` with `Move.TryParse(string? token, out Move move)`. Valid tokens are `F R U B L D` (clockwise) and the same letters followed by `'` (counter-clockwise); case-sensitive. Anything else (`f`, `F2`, `X`, `F''`, empty, `null`) returns false and never throws. `Face` from story 1.2 is reused. No rotation or renderer (1.4, 1.5); Domain still references no package and no project.

</frozen-after-approval>

## Implementation Notes

Added `Direction` (Clockwise, CounterClockwise) and `Move` (`readonly record struct Move(Face Face, Direction Direction)`) with `Move.TryParse`; `Face` from 1.2 is reused. The letter table `FacesByLetter` is the one place that maps notation to faces. On failure `move` is `default`, as with the framework `TryParse` pattern. Tests: `MoveTests` (12 valid tokens, invalid and null cases, coverage of all face and direction pairs).

## Review Triage Log

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| Coverage test only counts theory rows, would pass with duplicated rows (Blind) | low | patch | Now asserts parsed moves equal every face x direction pair |
| Invalid-token set misses whitespace, control characters, trailing chars, other lowercase letters (Blind, Edge) | low | patch | Added `" "`, `"F\n"`, `"F'\n"`, `"F\t"`, `"R'x"`, `"u"`, `"d'"`; curly apostrophe written as `\u2019` |
| Index/length constants derived from each other, hard to read (Blind) | low | patch | `PlainTokenLength` and `PrimedTokenLength` are now plain named values |
| `default(Move)` on failure equals a valid move (U clockwise) (Blind, Edge) | low | rejected | Standard `TryParse` contract, documented in XML doc; changing enum zero values would edit 1.2's `Face` |
| Public constructor accepts undefined enum values (Edge) | low | rejected | Speculative; rotation (1.4) throws on unknown values |
| `FacesByLetter` is a mutable dictionary behind a read-only interface (Blind) | low | rejected | Private static, not reachable from outside |
| No sequence parsing, per-token error reporting or formatter (Blind) | low | defer | Out of 1.3 scope; recorded in deferred-work.md |
| Package references on Domain not covered by `ArchitectureTests` (Acceptance) | low | defer | Pre-existing from 1.1; recorded in deferred-work.md |
| Null test duplicates body of the invalid-token test (Blind, Acceptance) | false | rejected | `TheoryData<string>` cannot hold null under nullable checks; separate test is needed |
| Spec Implementation Notes empty, status in-progress (Acceptance, Blind) | false | rejected | Filled and set to done in this step |
| `sprint-status.yaml` edited with code (Blind) | false | rejected | Required by the workflow's status sync |
| Verification Gap reviewer | none | n/a | No gaps reported |
