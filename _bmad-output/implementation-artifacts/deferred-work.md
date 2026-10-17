# Deferred work

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-strict-move-parsing-part-of-fr2.md`
  summary: Sequence-level parsing (split a string such as `F R' U`, report which token failed and why) and formatting a `Move` back to notation (round trip) are not part of 1.3.
  evidence: Story 1.3 specifies only single-token `Move.TryParse`; error reporting per token belongs to Story 2.3 (apply moves) and the TTC sequence rendering to 1.5. Add a notation formatter next to `FacesByLetter` there so the letter table stays in one place (DRY).

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-strict-move-parsing-part-of-fr2.md`
  summary: `ArchitectureTests` checks project references only; a `PackageReference` added to `Rubik.Domain` would not fail a test.
  evidence: `tests/Rubik.Api.Tests/Architecture/ProjectGraph.cs` reads `ProjectReference` edges only. Pre-existing from Story 1.1; 1.3 does not touch any csproj. Candidate for Story 4.2 (quality pass).

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-turn-any-face-fr2.md`
  summary: `Cube`, `Cubie` and `Sticker` have no value equality (records compare their lists by reference); 1.4 tests compare cubes through a test-side snapshot.
  evidence: Needed only if a later story must compare cube states in production code (for example Story 2.4 undo/reset, or an `IsSolved` check). Add it there with its own tests rather than as speculative public surface now.
