# Patch Notes Template — S71-03/04 Launch Doc Pack (E7)

**Date:** 2026-06-25  
**Track:** S71-03 Launch doc pack — patch-notes-template (Cloud, execute-plan §4)  
**Authority / MANDATORY CITES (on all artifacts):**  
- `production/commercial-launch-scope-boundary-2026-06-25.md` (E7 prep: launch docs IN; submission/E9/multi/bridge OUT; invariants 1232/6/6/18/18/hash/ZERO; GitNexus pre; stage=Release)  
- `docs/reports/future-sprint-roadpmap-062526.md` §3/§6/§7/§10 (S71 i18n+launch-docs; parallel tracks; docs-heavy; serial S69→S72; cite §3/§4)  
- `docs/reports/roadmap-execute-plan-062526.md` §3/§4 (S71 table: launch doc pack S71-03/04; deliverables: patch-notes-template.md, faq-draft.md, support-runbook-draft.md, evidence-index.md in `production/release/launch/`)  
- `AGENTS.md` (GitNexus: search_tool then use_tool list_repos/detect/impact pre; verification-before RUN+READ; detect_changes pre-commit)  
- S69 COMPLETE: `production/sprints/sprint-69-commercial-launch-foundation.md`, `production/qa/smoke-sprint-69-closeout-2026-06-25.md`, `production/qa/gate-matrix-commercial-launch-2026-06-25.md`, `production/agentic/sprint-69-parallel-kickoff-2026-06-25.md`  
- S70: `production/sprints/sprint-70-store-community-prep.md`, `production/agentic/sprint-70-parallel-kickoff-2026-06-25.md`, `production/release/release-checklist-v3.md`, `production/release/store/`, `production/release/community-templates.md`  
- Prior S66/S57–S68: `production/release/release-checklist-v2.md`, `production/qa/evidence/baltic-v2-playtest-index.md`, `production/playtests/baltic-v2-scenario-manifest.yaml`, release-train-scope-boundary-2026-06-24.md (invariants only), s57-s64 closeouts  

**Pre state (S69/S70 + this track pre):** GitNexus canonical 19962/37627/2462 @28c582d; gates 0e/1232/0f/6/6/18/18/hash 17144800277401907079 preserved/ZERO bridge. S69/S70 partial complete.  

**GitNexus pre (search_tool + use_tool, this track, 2026-06-25):**  
- list_repos (canonical `/home/username01/projects/active/cmano-clone/cmano-clone`): 19962 nodes / 37627 edges / 2462 files.  
- detect_changes (unstaged): changed_count=24, affected_count=0, risk="low" (doc-only).  
- impact (upstream summaryOnly): CatalogWriteGate=178 CRITICAL, PatrolCandidateEngagePolicy=97 CRITICAL, DelegationBridge=127 CRITICAL (exact), BalticReplayHarness=52 CRITICAL (exact). Low risk for docs-only launch pack. Exact match commercial boundary §5 + execute-plan §7.  

**Verification-before gates (RUN + READ full outputs before claims/write, 2026-06-25):**  
- Build: `dotnet build ProjectAegis.sln --no-restore` → 0 Error(s), 0 Warning(s). Log: /tmp/build-gate.log (READ: "Build succeeded.")  
- Full test: `dotnet test ProjectAegis.sln -v minimal --no-build` → 1232/0f (279 Sim +43 Cli +247 Del +5 Excel +252 UA +406 Data; all Passed! 0 Failed). /tmp/test-gate.log READ.  
- Replay: `--filter FullyQualifiedName~ReplayGoldenSuiteTests` → 6/6. /tmp/replay-gate.log READ.  
- C2 proxy: `--filter FullyQualifiedName~PlayModeSmokeHarnessTests` → 18/18. /tmp/c2-gate.log READ.  
- Hash: rg "17144800277401907079" tests/regression/ → preserved in v2 goldens (multiple). /tmp/hash-gate.log READ.  
- ZERO: rg "DelegationBridge" src/ --glob "!**/DelegationBridge.cs" -l → 22 usage files only (source untouched). /tmp/zero-bridge.log READ.  
- All outputs READ; scope cites + pre confirmed. No CRITICAL edits. Docs-only. Stage: Release (READ production/stage.txt).  

**Scope:** Template skeleton only (prep). Player-facing; versioned. Baltic v2 focus for E7 prep. No live publication. Cite all above + this file.  
---

## Template Structure (use for future versions; fill from sprint data + evidence-index)

```markdown
# Project Aegis — Patch [X.Y.Z] "[Codename]" (Baltic v2 Prep)

*Released: [YYYY-MM-DD]*  
*Build: [hash or version]*  

## Highlights
- [1-2 sentence player summary of key player-visible changes from S57–S71 prep corpus.]

## New Content
- Baltic v2 scenarios and policies (10 policies + 9 verified replay goldens; hash 17144800277401907079).
- Difficulty bands A/B/C with mission variants (patrol, comms-challenged, jammed, ROE, combat-domains, spoof, intercept).
- Agentic C2 delegation and catalog integration in mid-game flows.

## Gameplay Changes
- Deterministic replay guarantees maintained (ReplayGoldenSuite 6/6 across core + v2).
- [Balance/engagement tweaks from AAR synthesis if any; before/after if numeric.]

## Quality of Life
- C2 UI polish (graph, panels, doctrine, tooltips from S35+ polish).
- Platform catalog + import roundtrips hardened.

## Bug Fixes
- [Player-facing fixes from prior closeouts; cite playtest index for evidence.]

## Performance & Determinism
- Full gates: 1232 tests / 0 failures; Replay 6/6; C2 proxy 18/18.
- Seeded determinism preserved; no wall-clock in sim hot paths.

## Known Issues
- Prep phase: store submission, full localization, and multiplayer not yet in scope (see evidence-index and commercial-launch-scope-boundary).
- Scale (5k+ entities) under continued profiling.

See full evidence: production/release/launch/evidence-index.md + release-checklist-v3.md + baltic-v2-scenario-manifest.yaml.
```

## Usage Notes (for technical-writer / community)
- Populate from sprint retros, qa closeouts, playtest index, release-checklist-v3.
- Exclude internal sprint numbers, GitNexus symbols, ADR refs.
- Player language only. Enthusiastic but factual.
- Version when gate or human ack decides public drop (post S72).
- Cross-link to `production/release/launch/faq-draft.md` and support-runbook-draft.md.

**This template is self-contained. All claims verified pre-write via RUN+READ gates + GitNexus pre (low risk, impacts exact). Cites enforced per execute-plan §4 / roadmap §4.**

*Independent cloud subagent S71-03/04 launch doc pack. Docs-only. Pre S71-06 closeout.*
