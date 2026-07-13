# Sprint 66 — Content Manifest + Playtest Corpus + Checklist v2 (E10)

**Dates:** After S65 (~2026-06/07; est. 8–10 days)
**Lead:** E10 Release operations
**Goal:** Package Baltic v2 content manifest (10 policies + 9 goldens from S57–S64), build playtest corpus index, author release-checklist-v2.md (superseding v1), closeout with full verification. Per release-train-scope-boundary-2026-06-24.md and execute-plan.
**Capacity:** ~8–10 days total, 20% buffer. Cloud for manifest/checklist; local for playtest index + closeout.
**Model:** Per roadmap-062426 §0 + execute-plan §4/5. Parallel after baseline. Isolated worktrees. GitNexus impact preflights mandatory. verification-before on all claims. Cite boundary + roadmap §0/5/7/10 + execute §6 gates.

Cites: release-train-scope-boundary-2026-06-24.md + future-sprint-roadpmap-062426.md §0/3/5/7/10 + roadmap-execute-plan-062426.md §4/5/6/8/9 + superpowers (dispatching-parallel-agents + verification-before-completion + using-git-worktrees)

## Tasks / Tracks (from execute-plan §4)

### Must Have
- S66-01/02: Baltic v2 content manifest (cloud, producer) — package 10 policies + 9 goldens using now-hardened UnifiedReleaseTrainManifest / DiffReport / CatalogReleaseDiffCommand (post-S65). Record baltic-v2 unified release. GitNexus impact pre on CatalogWriteGate (CRITICAL) + Manifest. TDD/roundtrip verification.
- S66-03: Playtest corpus index (local, qa-tester) — index S57–S64 playtests (human + auto) under production/qa/evidence/ or playtests/. See `production/qa/evidence/baltic-v2-playtest-index.md` (delivered: sessions table, domain findings, 10+9 traceability, difficulty/C2/delegation; cites boundary + S65 closeout + this plan + release-checklist-v2.md). verification-before + local capture + GitNexus pre.
- S66-04: release-checklist-v2.md (cloud, qa-lead) — skeleton from v1 + S66 scope (manifest, playtest index, gates, verification-before, GitNexus). Supersede v1 for Baltic v2.
- S66-05: Closeout (local, devops-engineer): gt restack, re-run gates (build/test/replay/C2), smoke closeout doc, update sprint-status + checklist, GitNexus re-index note.

### Should Have
- /qa-plan sprint 66 (if not prior)
- Evidence bundle packaging polish

## Baseline @ Start (post-S65 verification-before)
- Tests: ≥1229/0f (monotonic from S65)
- ReplayGolden 6/6
- C2 18/18
- Build 0e/0w
- Hash 17144800277401907079 preserved
- ZERO DelegationBridge
- GitNexus: impacts §5 exact (Catalog CRITICAL 176/178, etc.); detect low expected (doc/manifest)
- S65 manifest + gate matrix + re-index COMPLETE

## Inputs (Content Manifest)
- data/scenarios/baltic-v2-*.policy.json (10 files)
- tests/regression/replay-golden-baltic-v2-*.txt (9 files)
- production/playtests/ + production/qa/s57-s64-program-closeout-2026-06-22.md + baltic-v2-scenario-manifest.yaml
- S65 UnifiedReleaseTrain artifacts

## Risks & Mitigations
- Manifest drift: Use CatalogReleaseDiffCommand + S65 TDD roundtrips; verification-before.
- Playtest index scope: Limit to S57–S64 per boundary (no E9 new).
- Parallel conflicts: Single owner Catalog for manifest track; local-only for index.
- Checklist v2 stale: Draft from v1 skeleton first; populate post-manifest.

## Definition of Done
- All must-have complete
- Gates PASS (build 0e, test >=1229 0f, replay 6/6, C2 18/18, hash, ZERO)
- GitNexus preflights + detect clean (low for docs/manifest); re-index post
- Artifacts: content manifest entry (via Unified tools), playtest index, release-checklist-v2.md (S66-04 COMPLETE per boundary full S66 section; see production/release/release-checklist-v2.md with 10+9 lists, gates, sign-off, cites), sprint plan, closeout smoke, status updates
- No scope creep (E10 only; cite boundary on every artifact/story)
- verification-before on all tracks + closeout

## Minimal Stub Note for Content Manifest (using hardened UnifiedReleaseTrain tools)
To record a baltic-v2 unified release from the 10+9 items (post-S65 hardening):

1. GitNexus impact() preflight on CatalogWriteGate + UnifiedReleaseTrainManifest (summaryOnly; report CRITICAL/HIGH).
2. Invoke CatalogReleaseDiffCommand (or equivalent Record via UnifiedReleaseTrainManifest) against the exact set:
   - 10x baltic-v2-*.policy.json (data/scenarios/)
   - 9x replay-golden-baltic-v2-*.txt (tests/regression/)
3. Capture UnifiedReleaseTrainDiffReport for stable hash/order-indep/roundtrip (extend TDD if gaps).
4. Update manifest entry (additive only); extend-only on CatalogWriteGate.
5. verification-before: full RUN+READ of build/test/replay subsets + hash grep + detect_changes() (expect low).
6. Commit only after detect_changes() + closeout coord.

Example flow (per S65 manifest + execute-plan §4 S66): reference S65 Unified* + CatalogReleaseDiffCommand tests. Cite this sprint doc + boundary + roadmap §10. Do not edit CRITICAL symbols without owner/impact ack. (Scaffold only; populate in S66-01 track.)

## Next (per execute-plan §5)
- After S65 close: dispatch parallel for S66 tracks via dispatching-parallel-agents + worktrees (content-manifest || playtest-index || checklist-v2) then closeout.
- Integrate on closeout track (gt restack, re-verif, evidence).
- Update sprint-status.yaml + production/release/release-checklist-v2.md (S66-04 populated with concrete from S65 + boundary; supersedes v1) + production/qa/
- Prep S67 dispatch only after S66 PASS.

Cites: release-train-scope-boundary-2026-06-24.md + future-sprint-roadpmap-062426.md §0/3/5/7/10 + roadmap-execute-plan-062426.md §4/5/6/8/9 + superpowers:dispatching-parallel-agents + verification-before. S65 artifacts referenced. All artifacts cite boundary. *Generated per sprint-65-*-pattern + execute plan. Dispatch via dispatching-parallel-agents after S65.*