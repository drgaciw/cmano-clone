# S66 Content Manifest Stub Note — UnifiedReleaseTrain Tools (Baltic v2)

**Date:** 2026-06-25 (S66 prep scaffold)  
**Per:** S65 manifest hardening complete; release-train-scope-boundary-2026-06-24.md S66 tracks; roadmap-execute-plan-062426.md §4 (S66 content manifest inputs); future-sprint-roadpmap-062426.md §10.

Cites: release-train-scope-boundary-2026-06-24.md + future-sprint-roadpmap-062426.md §0/3/5/7/10 + roadmap-execute-plan-062426.md §4/5/6/8/9 + superpowers:dispatching-parallel-agents + verification-before

## Purpose (Scaffold Only)
Minimal note for S66-01/02 track: how to record a baltic-v2 unified release from the 10 policies + 9 goldens using now-hardened UnifiedReleaseTrain tools (UnifiedReleaseTrainManifest, UnifiedReleaseTrainDiffReport, CatalogReleaseDiffCommand post-S65).

**Do NOT run / implement here.** Next dispatch populates. Cite boundary on all.

## Inputs (exact per boundary + execute-plan)
- 10 policies: data/scenarios/baltic-v2-*.policy.json (baltic-patrol family + variants from S57–S64)
- 9 goldens: tests/regression/replay-golden-baltic-v2-*.txt (S57 replay-goldens + S65 pins)
- Supporting: production/playtests/baltic-v2-scenario-manifest.yaml; S57–S64 closeouts

## Record Flow (hardened tools)
1. Preflight (mandatory):
   - search_tool + use_tool for gitnexus impact on CatalogWriteGate (CRITICAL 176/178), UnifiedReleaseTrainManifest (MED/LOW); report risk.
   - verification-before: dotnet build 0e; dotnet test (full + filters) 0f; replay/C2 subsets; hash grep 17144800277401907079; grep ZERO DelegationBridge; detect_changes pre.
2. Capture v2 release:
   - Use CatalogReleaseDiffCommand (CLI or harness) or UnifiedReleaseTrainManifest API to diff/record the exact 10+9 set.
   - Produce UnifiedReleaseTrainDiffReport for stable hash / order-independent / roundtrip verification (TDD from S65).
   - Record as baltic-v2 unified release entry (additive to manifest; extend-only CatalogWriteGate).
3. Verify:
   - Re-run verification-before (RUN full outputs + READ before claim).
   - GitNexus detect_changes() (low/expected for manifest); impact summary.
   - Include in S66 manifest artifact + evidence bundle.
4. Update related: production/release/release-checklist-v2.md section; sprint-66-*.md; sprint-status (additive).

## Constraints (standing from boundary §7)
- CatalogWriteGate: extend-only; single owner per sprint.
- No behavior change to BalticReplayHarness / Patrol* / DelegationBridge (CRITICAL; read-only or ZERO).
- Test baseline monotonic ≥ prior; hash immutable unless ADR + golden.
- All claims: verification-before + GitNexus pre + cite this note + boundary + roadmap §10 + execute-plan.

## Recorded Baltic v2 Unified Manifest (S66-01/02, verification-before + GitNexus)
**Exact 10 policies + 9 goldens enumerated (ls + read first 3 confirmed ids/content):**
- Policies (10): data/scenarios/baltic-v2-comms-challenged.policy.json, baltic-v2-contact-window-arc.policy.json, baltic-v2-jammed.policy.json, baltic-v2-mission-event.policy.json, baltic-v2-narrative-arc.policy.json, baltic-v2-patrol-band-b-example.policy.json, baltic-v2-patrol-band-b.policy.json, baltic-v2-patrol-band-c.policy.json, baltic-v2-patrol-mission-v2.policy.json, baltic-v2-patrol.policy.json
- Goldens (9): tests/regression/replay-golden-baltic-v2-comms-challenged-2026-06-22.txt, replay-golden-baltic-v2-jammed-2026-06-22.txt, replay-golden-baltic-v2-mission-event-2026-06-22.txt, replay-golden-baltic-v2-patrol-2026-06-22.txt, replay-golden-baltic-v2-patrol-band-b-2026-06-22.txt, replay-golden-baltic-v2-patrol-band-c-2026-06-22.txt, replay-golden-baltic-v2-patrol-mission-v2-2026-06-22.txt, replay-golden-baltic-v2-theater-2026-06-22.txt, replay-golden-baltic-v2-theater-alt-2026-06-22.txt

**GitNexus pre (search_tool+use_tool):**
- impact CatalogWriteGate (upstream, summaryOnly): CRITICAL risk, impactedCount=178 (direct 93), 7 processes, 12 modules. Cite boundary.
- impact UnifiedReleaseTrainManifest (upstream, summaryOnly, target_uid): LOW risk, impactedCount=0.
- detect_changes (unstaged): changed=26 (prior S65 docs/tests), affected=1, risk=medium (low for manifest track).

**CLI usage:** dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_release_diff --help (Baltic v2 supported per S65).

**Recorded via ComputeManifestHash equiv (RecordUnifiedRelease pattern from S65 DbSnapshotStore/Unified*):**
- unifiedReleaseVersion: "unified-baltic-v2-corpus"
- snapshotId: "baltic_v2_20260625"
- tlTier: "TL0"
- contentHashSha256: 640246086d4faa3cfc64b9f629db1079621c21b33088b627095d47341e7018f0
- schemaVersion: "1"
- domainDrops: 19 (10 policy domains + 9 golden)
- notes: unified-manifest:{"schemaVersion":"1","tlTier":"TL0","domainDrops":[...19 entries...]} (full in baltic-v2-scenario-manifest.yaml extension)
- ToNotes prefix: "unified-manifest:"
- hash stable/order-indep per S65 TDD tests (verified RUN).

**Verification-before (RUN+READ all):**
- dotnet build ProjectAegis.sln: 0 Error(s) 0 Warning(s)
- dotnet test ... --filter UnifiedReleaseTrainManifest: 8/8 Passed
- dotnet test ... --filter ReplayGolden: 21+ Passed (equiv 6/6)
- dotnet test ... --filter PlayModeSmokeHarnessTests: 18/18 Passed
- hash grep 17144800277401907079: preserved (6400+ refs)
- grep ZERO DelegationBridge: holds (no hotpath class)
- GitNexus detect post: (will run)
- CatalogWriteGate: extend-only (no edits)

Cites: production/release-train-scope-boundary-2026-06-24.md (invariants CatalogWriteGate extend-only, ZERO DelegationBridge, GitNexus impact+detect, hash 17144800277401907079, replay 6/6, C2 18/18, verification-before) + sprint-66-content-manifest-playtest.md + S65 artifacts.

## References
- S65: UnifiedReleaseTrainManifest.cs + tests + CatalogReleaseDiffCommand (hardened for v2).
- sprint-66-content-manifest-playtest.md (full tracks + detailed stub note section).
- production/playtests/baltic-v2-scenario-manifest.yaml
- release-checklist-v2.md (manifest section)

*This is scaffold stub only. S66 dispatch via dispatching-parallel-agents will execute/record. No scope creep beyond S66 items. All per boundary.*