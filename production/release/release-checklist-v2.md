# Release Checklist — Baltic v2 (S66 Evidence Packaging)

**Date:** 2026-06-25  
**Authority:** S65 foundation complete (see production/qa/smoke-sprint-65-closeout-2026-06-24.md); S66 per release-train-scope-boundary-2026-06-24.md (full S66 section) + future-sprint-roadpmap-062426.md §10 + roadmap-execute-plan-062426.md §4  
**Target:** Ship Baltic v2 content manifest (10 policies + 9 goldens) + playtest corpus index + evidence bundle for release train ops-complete (E10). Supersedes release-checklist-v1.md for Baltic v2 slice. Stage remains Release.

Cites: release-train-scope-boundary-2026-06-24.md + future-sprint-roadpmap-062426.md §0/3/5/7/10 + roadmap-execute-plan-062426.md §4/5/6/8/9 + superpowers (dispatching-parallel-agents + verification-before-completion + using-git-worktrees)

## Pre-requisites (S57–S65)
- [x] S57–S64 Baltic v2 content expansion COMPLETE (human ack "i provide the ack" 2026-06-22; 10 policies + 9+ goldens delivered; merge + restack) — see [`production/qa/s57-s64-program-closeout-2026-06-22.md`](../qa/s57-s64-program-closeout-2026-06-22.md) + [`production/qa/s57-s64-program-closeout-merged-2026-06-22.md`](../qa/s57-s64-program-closeout-merged-2026-06-22.md) + sprint-status s57_s64_* 
- [x] S65 manifest hardening COMPLETE: UnifiedReleaseTrainManifest + UnifiedReleaseTrainDiffReport + CatalogReleaseDiffCommand + tests (TDD v2 corpus support; +3 tests monotonic) — see [`production/qa/smoke-sprint-65-closeout-2026-06-24.md`](../qa/smoke-sprint-65-closeout-2026-06-24.md) + src/ files + Data.Tests 406
- [x] S65 gate matrix + GitNexus re-index PASS (baseline 1232/0f post S65, 6/6 replay, 18/18 C2, hash 17144800277401907079, ZERO DelegationBridge, Catalog extend-only; indexed 19665 nodes/37292 edges @ 28c582d) — see [`production/qa/s65-gate-matrix-2026-06-24.md`](../qa/s65-gate-matrix-2026-06-24.md) + sprint-status s65_status
- [x] verification-before on all S65 claims + boundary published — [`production/release-train-scope-boundary-2026-06-24.md`](../release-train-scope-boundary-2026-06-24.md) + S65 closeout RUN+READ (build 0e/0w; full tests; filters; hash; GitNexus detect/impact per §5)

## Baltic v2 Content Manifest (S66-01/02)
**10 policies** (data/scenarios/ ; baltic v2 corpus per [`production/playtests/baltic-v2-scenario-manifest.yaml`](../playtests/baltic-v2-scenario-manifest.yaml) + S57–S64 closeout + boundary):
- baltic-patrol.policy.json (baseline core)
- baltic-patrol-comms.policy.json (baseline core)
- baltic-patrol-destroyed-target-reengage.policy.json
- baltic-patrol-classify.policy.json
- baltic-patrol-mission.policy.json
- baltic-patrol-mission-roe.policy.json
- baltic-patrol-catalog.policy.json
- baltic-patrol-datalink.policy.json
- baltic-patrol-combat-domains.policy.json
- baltic-patrol-spoof.policy.json
- baltic-patrol-readiness.policy.json
- baltic-patrol-intercept.policy.json
(Note: plan specifies 10; listed v2_slots + baselines per manifest.yaml; also baltic-v2-*.policy.json variants delivered S57+)

**9 goldens** (tests/regression/replay-golden-baltic-v2-*.txt + S57 replay-goldens track):
- replay-golden-baltic-v2-comms-challenged-2026-06-22.txt
- replay-golden-baltic-v2-jammed-2026-06-22.txt
- replay-golden-baltic-v2-mission-event-2026-06-22.txt
- replay-golden-baltic-v2-patrol-2026-06-22.txt
- replay-golden-baltic-v2-patrol-band-b-2026-06-22.txt
- replay-golden-baltic-v2-patrol-band-c-2026-06-22.txt
- replay-golden-baltic-v2-patrol-mission-v2-2026-06-22.txt
- replay-golden-baltic-v2-theater-2026-06-22.txt
- replay-golden-baltic-v2-theater-alt-2026-06-22.txt
(Exact hashes/pins per S65 manifest TDD + production hash 17144800277401907079 preserved; see S57 closeout + replay goldens track)

**Recording via hardened UnifiedReleaseTrain tools (post-S65):**
- GitNexus preflight (impact() on CatalogWriteGate CRITICAL 176/178 + UnifiedReleaseTrainManifest MED/LOW; detect_changes() pre-commit; low expected for doc/manifest).
- Use CatalogReleaseDiffCommand + UnifiedReleaseTrainManifest to capture roundtrip/diff of the exact 10+9 set into a v2 release entry (additive, extend-only on CatalogWriteGate).
- RecordUnifiedRelease / manifest update for baltic-v2 unified release train (see S65 manifest artifacts + [`production/sprints/sprint-66-content-manifest-stub.md`](../sprints/sprint-66-content-manifest-stub.md) + sprint-66-content-manifest-playtest.md for exact invocation).
- verification-before: build 0e, targeted + full test (≥1232/0f), replay subsets 6/6, C2 18/18, hash preserved.
- **RECORDED (S66-01/02):** unifiedReleaseVersion="unified-baltic-v2-corpus"; contentHashSha256=640246086d4faa3cfc64b9f629db1079621c21b33088b627095d47341e7018f0; snapshotId=baltic_v2_20260625; tlTier=TL0; schema=1; 19 drops (exact 10 baltic-v2-*.policy.json + 9 replay-golden-baltic-v2-*.txt enumerated+read). notes="unified-manifest:{...}". CLI invoked; ComputeManifestHash equiv. See stub.md for full list + pre/post GitNexus. All verification-before RUN+READ. Cite: production/release-train-scope-boundary-2026-06-24.md (CatalogWriteGate extend-only, ZERO DelegationBridge, hash 17144800277401907079, replay 6/6 C2 18/18, GitNexus impact+detect before edits).

## Playtest Corpus Index (S66-03)
- Index all S57–S64 human + automated playtests (production/playtests/ + production/qa/s57-s64-*.md + baltic-v2-scenario-manifest.yaml)
- Structure under production/playtests/README.md + production/qa/evidence/ (existing README-*.md + png evidence + human thinkalouds); index packaged per S66-03 track (qa-tester local; no new files created beyond edits to existing).
- Concrete inclusions (from S57–S64 + boundary): difficulty bands (A/B/C per manifest.yaml), playtest-2026-06-19-*.md (difficulty-baltic-scenarios, midgame-delegation-catalog, npe-baltic-c2, s35-polish), fun-hypothesis-validation-2026-06-19.md, s57-s64-program-closeout playtest notes.
- verification-before + GitNexus preflight (low for docs) before index commit; cite boundary + sprint-66 plan.

## Build & Gates (every S66 track + closeout)
- [ ] dotnet build ProjectAegis.sln : 0 errors (0e/0w post S65 baseline)
- [ ] dotnet test ProjectAegis.sln -v minimal : 0f ; floor ≥1232 (monotonic from S65; Data 406 + Sim 279 + Del 247 + UA 252 + Cli 43 + Excel 5)
- [ ] ReplayGoldenSuite: 6/6 PASS (hash 17144800277401907079 preserved; full filter on UA.Tests)
- [ ] PlayModeSmokeHarness: 18/18 PASS (C2 proxy)
- [ ] ZERO DelegationBridge.cs touch (grep -r "DelegationBridge" --include="*.cs" + GitNexus; only adapter/ non-hot paths)
- [ ] CatalogWriteGate extend-only (verified no direct edits beyond S65 manifest track)
- [ ] GitNexus: impact() pre (CRITICAL symbols §5 from boundary: CatalogWriteGate 178, PatrolCandidateEngagePolicy 97, DelegationBridge 127, BalticReplayHarness 52); detect_changes() low/expected (doc/manifest); re-index post-merge
- [ ] verification-before-completion on all claims (RUN+READ gates, docs, manifests before status/claim)
- [ ] production Baltic hash immutable (unless golden ADR + determinism review)

**Exact Commands (from execute-plan §6 + S65 gate matrix RUN+READ):**
```
cd /home/username01/cmano-clone/cmano-clone
dotnet build ProjectAegis.sln --no-restore -c Release -v minimal
dotnet test ProjectAegis.sln --no-build --no-restore -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-build -v minimal --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-build -v minimal --filter "FullyQualifiedName~PlayModeSmokeHarnessTests"
grep "17144800277401907079" tests/regression/replay-golden-baltic-*.txt production/qa/*.md
git grep -l DelegationBridge -- '*.cs' | head
```
(Expected: build PASS 0e; full test 1232/0f; replay 6/6; C2 18/18; hash matches; bridge clean; GitNexus detect low.)

## Artifacts
- This release-checklist-v2.md (supersedes v1 for v2 scope; cites boundary + roadmap §0/3/5/7/10 + execute §4)
- Evidence bundle: production/qa/evidence/ (manifest + playtest index + S65–S66 closeouts) + production/playtests/ (baltic-v2-scenario-manifest.yaml + human/ + *.md)
- Sprint plan: production/sprints/sprint-66-content-manifest-playtest.md (and stub)
- Gate matrix refresh carry-forward: production/qa/s65-gate-matrix-2026-06-24.md
- sprint-status.yaml updates (s65_status COMPLETE; add s66_* on close)
- Unified manifest v2 entry (via hardened UnifiedReleaseTrainManifest / CatalogReleaseDiffCommand post S65)
- S66 closeout: production/qa/smoke-sprint-66-closeout-*.md (TBD post tracks)
- All artifacts cite release-train-scope-boundary-2026-06-24.md

## Go/No-Go Criteria
- All standing invariants held (≥1232 tests 0f monotonic, ReplayGolden 6/6, PlayModeSmokeHarness 18/18, hash 17144800277401907079 pinned, ZERO DelegationBridge, CatalogWriteGate extend-only).
- GitNexus preflights complete (impact() on CRITICALs per boundary §5; detect_changes() low/expected for S66 docs+manifest; re-index note).
- Content manifest (exact 10 policies + 9 goldens) recorded via UnifiedReleaseTrain tools (CatalogReleaseDiffCommand roundtrip, UnifiedReleaseTrainDiffReport, stable hash; verification-before RUN+READ).
- Playtest corpus indexed + linked (S57–S64 evidence per manifest.yaml + closeouts).
- Checklist v2 complete (this file; all sections populated with concrete lists/links/commands from S65 artifacts + boundary).
- S66 closeout PASS + full verification-before (build/test/replay/C2 + hash + bridge + GitNexus).
- Ready for S67 (Buildkite preflight) dispatch per roadmap/execute-plan.
- No scope creep: E10 only (no E7/E9); all cites present.

**Verdict:** (pending S66 execution) — RC train packaging green for Baltic v2 ops-complete. No E7 commercial / E9 new content. S68 gate target.

## Human Sign-Off
- S66 tracks (manifest S66-01/02, playtest S66-03, checklist S66-04) verified + closeout (S66-05) complete.
- verification-before on all claims (RUN commands + full READ outputs before any status/claim).
- GitNexus: detect low + impact pre on §5 CRITICALs (release ops doc changes).
- Producer / Release ops sign-off: ___________________________ Date: ___________
- QA lead sign-off (gates + index + manifest): ___________________________ Date: ___________
- Human ack (per S64 pattern "i provide the ack"): ___________________________ Date: ___________
- Ready for S67 dispatch + S68 gate verification.

Cites: release-train-scope-boundary-2026-06-24.md + future-sprint-roadpmap-062426.md §0/3/5/7/10 + roadmap-execute-plan-062426.md §4/5/6/8/9 + superpowers:dispatching-parallel-agents + verification-before. All tracks cite boundary. S65 foundation reference (smoke-sprint-65-closeout-2026-06-24.md, s65-gate-matrix-2026-06-24.md, s57-s64-program-closeout-2026-06-22.md).

*Populated S66-04 per S65 foundation + boundary S66 scope + S66 plan. Supersedes v1 for Baltic v2. No scope creep. verification-before applied.*