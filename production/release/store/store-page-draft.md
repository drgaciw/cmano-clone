# Store Page Draft (Steam) — S70-01/02

**Date:** 2026-06-25  
**Status:** S70 draft (independent cloud track per roadmap-execute-plan-062526.md §3/§4)  
**Track:** S70-01/02 Store page drafts (Cloud, community-manager per execute-plan §4 S70 table)  
**Authority / MANDATORY CITES (everywhere):**  
- `production/commercial-launch-scope-boundary-2026-06-25.md` (E7 prep only: store drafts IN; submission/marketing/E9/multiplayer/bridge edits OUT; invariants 1232/6/6/18/18/hash/ZERO; GitNexus pre; stage Release)  
- `docs/reports/future-sprint-roadpmap-062526.md` §3/§6/§7/§10 (S70 Store+community; parallel tracks; docs-heavy; serial S69→S72)  
- `docs/reports/roadmap-execute-plan-062526.md` §3/§4 (S70 table: store pages S70-01/02, community S70-03, checklist-v3 S70-04, closeout S70-05; deliverables store-page-draft.md + asset-checklist.md + platform-notes.md)  
- `AGENTS.md` (GitNexus `search_tool`+`use_tool` list/detect/impact CRITICALs pre; verification-before RUN+READ gates before claims)  
- S69 artifacts: `production/qa/smoke-sprint-69-closeout-2026-06-25.md` (S69 COMPLETE; gates PASS 0e/1232/0f/6/6/18/18/hash/ZERO=0; GitNexus 19962/37627/2462 low risk), `production/commercial-launch-scope-boundary-2026-06-25.md`, `production/qa/gate-matrix-commercial-launch-2026-06-25.md`, `production/sprints/sprint-69-commercial-launch-foundation.md`, `production/agentic/sprint-69-parallel-kickoff-2026-06-25.md`  
- S66 input: `production/release/release-checklist-v2.md` (Baltic v2 corpus prereqs) + `production/playtests/baltic-v2-scenario-manifest.yaml` + `production/qa/evidence/baltic-v2-playtest-index.md` + release-train-scope-boundary-2026-06-24.md (invariants only)  

**Pre state (orchestrator):** GitNexus canonical 19962/37627/2462 low risk; gates PASS 0e/1232/0f/6/6/18/18/hash `17144800277401907079`/ZERO=0 ; S69 COMPLETE.  

**GitNexus pre (this track, search+use before edit/claim):**  
- search_tool (schemas) + use_tool gitnexus__list_repos (canonical `/home/username01/projects/active/cmano-clone/cmano-clone`): 19962 nodes / 37627 edges / 2462 files.  
- gitnexus__detect_changes (unstaged): changed_count=24, affected_count=0, risk="low" (doc-only md sections in AGENTS/roadmaps/READMEs).  
- gitnexus__impact (summaryOnly, upstream): CatalogWriteGate=178 CRITICAL, PatrolCandidateEngagePolicy=97 CRITICAL, DelegationBridge=127 CRITICAL (exact), BalticReplayHarness=52 CRITICAL (exact). Low risk for docs-only. Confirmed exact §5 of boundary/execute-plan/roadmap.  

**Verification-before (RUN+READ full outputs before any claim, 2026-06-25):**  
- Build: `dotnet build ProjectAegis.sln --no-restore -v q` → 0e/0w (log /tmp/gates-s70/build.log READ).  
- Full test: `dotnet test ProjectAegis.sln -v minimal --no-build` → 1232/0f (279 Sim +43 Cli +247 Del +5 Excel +252 UA +406 Data; /tmp/gates-s70/test.log READ).  
- Replay: filter ReplayGoldenSuiteTests → 6/6 (/tmp/gates-s70/replay.log READ).  
- C2: filter PlayModeSmokeHarnessTests → 18/18 (/tmp/gates-s70/c2.log READ).  
- Hash: rg 17144800277401907079 tests/regression/ → preserved in v2 goldens + README (/tmp/gates-s70/hash.log READ).  
- ZERO: rg DelegationBridge src/ --glob "!**/DelegationBridge.cs" -l → 22 usage files only (/tmp/gates-s70/zero.log READ).  
- Scope: boundary + roadmap §3/6/7/10 + execute §3/4 + AGENTS + S69 + S66 v2 cited. All outputs READ.  

**Scope compliance:** Docs-only E7 commercial launch prep (store drafts). No src/ sim / catalog writes / PlayMode / DelegationBridge / hash changes. Baltic v2 corpus (S57–S66) cited as input. Stage remains Release.  

---

## Steam Store Page Sections (Draft)

**Short Description** (≤300 chars, per Steam guidelines; cite S46 B5 template + Baltic v2 manifest):  
"Command authentic Baltic naval operations in a deterministic, agentic military simulation. 10+ policy variants, full C2 map, replay goldens, and delegation AI. Stage: Release (prep)."

**Long Description** (Steam rich text friendly; structured from Baltic v2 corpus per release-checklist-v2 + baltic-v2-scenario-manifest.yaml + S57–S64 playtests):  
Project Aegis is a modern military simulation focused on the Baltic theater.

**Core Features (Baltic v2 corpus):**  
- Deterministic tick-accurate simulation with golden replay baselines (hash 17144800277401907079 preserved across 9+ v2 goldens).  
- 10 Baltic patrol / mission policies + variants: comms-challenged, jammed, datalink latency, combat-domains, spoof, readiness, intercept, destroyed-target-reengage, classify, mission-roe, catalog, theater.  
- Agentic delegation & C2 command post: policy/ROE/EMCON/WRA evaluation, midgame delegation, NPE Baltic C2 playtest validated.  
- Full order log replay, scenario authoring, sensor/EW modeling, logistics/magazines.  
- Difficulty bands A/B/C from playtest manifest; human think-aloud validated (S57–S64).  

**Authenticity & Polish:**  
- Derived from open military research + declassified sources (see docs/military-simulation/).  
- Baltic v2 content expansion complete (S57–S64 + S66 manifest).  
- Headless verified: 1232+ tests, 6/6 ReplayGolden, 18/18 C2 proxy.  

**Release Status (E7 prep):** This page is draft for commercial launch prep only (S70). No store submission, no paid marketing. See production/release/ for full prep pack.  

**Features List** (bullet format for Steam; tagline from GDD/design + baltic scope):  
• Realistic Baltic Sea patrol, comms, datalink, and combat-domain scenarios (10 policies + 9 replay goldens)  
• Deterministic simulation — identical replays guaranteed (pinned hash, verified gates)  
• Advanced C2 interface with delegation AI, policy engine (ROE/EMCON/WRA), and mission editor  
• Full replay system, order logs, and regression suite for after-action review  
• Playtested difficulty progression (bands A/B/C; NPE to stress)  
• Extensible catalog and platform import tooling (extend-only CatalogWriteGate)  
• Headless .NET core + Unity adapter (no external deps for core sim)  

**Tags** (Steam relevant; from genre-convention + design):  
Military Simulation, Wargame, Strategy, Real-Time Tactics, Naval, Modern Warfare, Simulation, Indie, Singleplayer, Replay, C2, Agentic AI, Baltic, Deterministic  

**About the Game / System Requirements** (placeholder, internal):  
- Singleplayer focus (Stage Release prep; multiplayer deferred)  
- .NET 8 + Unity 6.3 LTS runtime  
- Low system reqs (headless core runs on modest hardware)  

**Media / Capsule Notes:** See companion `asset-checklist.md` and `platform-notes.md`.  

---

**S70-01/02 partial (drafts).** Cite all authorities. Low risk: docs only. Next: community S70-03, checklist-v3 S70-04, closeout S70-05. Full S70 closeout later. Evidence: GitNexus pre + gates RUN+READ + S69 COMPLETE + S66 v2 + Baltic v2 manifest.  

*Independent subagent for S70 Store + community prep tracks (cloud per execute-plan, dispatching-parallel-agents). Self-contained.*
