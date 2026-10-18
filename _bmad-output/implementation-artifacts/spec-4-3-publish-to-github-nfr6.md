---
title: 'Story 4.3: Publish to GitHub'
type: 'chore'
created: '2026-10-19'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'a1c01f97a075cfe53b78fe0876b02597a00421f1'
context:
  - '{project-root}/CLAUDE.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-4-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The repository `kratun/rubik-cube` must be reachable by the reviewers with a clear name and description, hold the README at the root of the default branch, and contain no secrets or personal data. A fresh clone also fails `dotnet csharpier check .` until `dotnet tool restore` has run, and neither the README nor CLAUDE.md says so.

**Approach:** Scan the working tree and the whole history for secrets and personal data (read only), tighten `.gitignore`, add `dotnet tool restore` to the README and the CSharpier line of CLAUDE.md, and verify the published state after the merge. Decisions (owner, 2026-10-19): visibility stays public; description stays as it is on GitHub (the README's second line); no topics; no website; the other remote branches stay; the history is not rewritten and nothing is force-pushed; "Drago" in `_bmad-output/` and `docs/TTC Rubiks Challenge - 2026_.pdf` stay in the repository.

</frozen-after-approval>

## Implementation Notes

Deferred entry taken (deleted from `deferred-work.md`): "Add `dotnet tool restore` to the CLAUDE.md line about CSharpier and to the README prerequisites". The other entries stay.

Scan (2026-10-19, read only; 205 tracked files, 47 commits, all branches): no secrets in the tree or the history (searched for passwords, keys, tokens, connection strings, `ghp_`, `github_pat_`, `AKIA`, private keys); no absolute user paths; no `.bak`, `.env`, `bin/`, `obj/` or `node_modules/` tracked or ever added; `_bmad/`, `.claude/` and `docs/local/` never committed and ignored.

One personal-data finding: 46 of 47 commits carry the author and committer email of a former employer (`dragomir.mitev@mentormate.com`), set in the global git config. Owner decisions: the global `user.email` is changed to `mitevdv@gmail.com` (done 2026-10-19, new commits only); the history is not rewritten and nothing is force-pushed, because the repository is already public with PR refs (`refs/pull/*`) that a rewrite would not remove.

Exception (CLAUDE.md "never silent"): the Problem asks for no personal data, and the old employer email stays in the history of the 46 commits by owner decision. To be named in the commit and PR text.

Changes: `README.md` (prerequisite line for `dotnet tool restore`), `CLAUDE.md` (same on the CSharpier line), `.gitignore` (`*.bak`), `web/.gitignore` (`.vitest/`; `playwright-report/` was already there), `deferred-work.md` (entry deleted), `sprint-status.yaml` (`in-progress`). No code, API or generated type changes.

Repository state on 2026-10-19 (owner, read in the GitHub UI): public, default branch `main`, description set (same as the README's second line), website empty, topics none (owner decision). The remote still holds about 40 old feature and chore branches; they stay (owner decision).

## Review Triage Log

Layers: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor (all ran).

| Finding | Verdict | Evidence / action |
|---|---|---|
| `playwright-report/` duplicated in root `.gitignore` and `web/.gitignore` (Edge) | low, patched | Confirmed in `web/.gitignore`; removed from root, and `.vitest/` moved to `web/.gitignore` next to the other web artefacts |
| Old employer email stays in history, Problem statement unmet (Acceptance, Blind, Edge) | medium, patched | Owner decision; now recorded as a named exception in Implementation Notes (PR text to repeat it) |
| `.mailmap`, repo-local `user.email` (Blind, Edge) | low, rejected | `.mailmap` fixes only local `git log`, not GitHub; the global config is changed; an extra file for no reviewer-visible gain |
| Post-merge verification not in the diff (Blind, Acceptance, Edge) | false | It is the last step of the story and runs after the merge; the result goes into Implementation Notes then |
| Scan has no scanner (gitleaks) and does not cover `refs/pull/*` (Blind, Edge) | low, rejected | Pattern search over all branches found nothing; PR refs hold the same commits as the branches |
| README tool line orphaned, not in Build; DRY with CLAUDE.md (Blind, Edge) | low, rejected | The deferred entry asked for the README prerequisites and the CLAUDE.md line; the README never runs `csharpier check`, so a Build step would add scope |
| `*.bak` unanchored; `web/.vitest/` not produced by config (Blind, Edge, Acceptance) | false | The unanchored pattern is intended; `.vitest/` appears when Vitest runs without a cache directory (owner note) |
| Empty heading left in `deferred-work.md` (Edge) | false | The other 4.2a entry remains under it |
