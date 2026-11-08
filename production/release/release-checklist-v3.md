# Release Checklist v3 — E7 Commercial Launch Prep (S70+ Skeleton)

**Date:** 2026-06-25  
**Status:** S70-04 skeleton (supersedes v2 for E7 prep slice; Baltic v2 prereqs carried as locked)  
**Track:** S70-04 Checklist v3 skeleton (Cloud, release-manager per roadmap-execute-plan-062526.md §4)  
**Authority / MANDATORY CITES:**  
- `production/commercial-launch-scope-boundary-2026-06-25.md` (E7 prep: store/i18n/launch/checklist IN; submission OUT; invariants; stage Release)  
- `docs/reports/future-sprint-roadpmap-062526.md` §3/§6/§7/§10  
- `docs/reports/roadmap-execute-plan-062526.md` §3/§4 (S70: checklist-v3 S70-04; cites v2 + S69)  
- `AGENTS.md` (GitNexus pre search+use list/detect/impact; verification-before RUN+READ)  
- S69 artifacts (COMPLETE): `production/qa/smoke-sprint-69-closeout-2026-06-25.md`, commercial-launch-scope-boundary-2026-06-25.md, gate-matrix-commercial-launch-2026-06-25.md, production/sprints/sprint-69-commercial-launch-foundation.md, production/agentic/sprint-69-parallel-kickoff-2026-06-25.md  
- S66 input: `production/release/release-checklist-v2.md` (Baltic v2 ops-complete prereqs; 10 policies + 9 goldens + playtest index; must be [x] before E7 sections) + baltic-v2-scenario-manifest.yaml + qa/evidence/baltic-v2-playtest-index.md + release-train-scope-boundary-2026-06-24.md (invariants carry)  

**Pre state:** GitNexus canonical 19962/37627/2462 low risk; gates PASS 0e/1232/0f/6/6/18/18/hash/ZERO=0 ; S69 COMPLETE.  

**GitNexus pre (this track):** search_tool + use_tool list_repos (canonical path /home/username01/projects/active/cmano-clone/cmano-clone): 19962/37627/2462; detect_changes unstaged 24/0 low (docs); impact on CRITICALs §5: CatalogWriteGate 178, Patrol 97, DelegationBridge 127 (exact), BalticReplayHarness 52. Low for docs. Exact match boundary/roadmap/execute-plan.  

**Verification-before (RUN+READ before claims/this write):**  
- `dotnet build ProjectAegis.sln` → 0e/0w (READ /tmp/gates-s70/build.log)  
- `dotnet test ProjectAegis.sln -v minimal` → 1232/0f (READ full; sums 279+43+247+5+252+406)  
- Replay filter → 6/6 (READ)  
- C2 filter → 18/18 (READ)  
- rg hash → preserved (v2 goldens) (READ)  
- rg DelegationBridge (no .cs source) → 22 usages (READ)  
- Scope cites + S69 COMPLETE + S66 v2 input confirmed. All full outputs READ.  

**Scope:** Skeleton superseding v2 for E7 prep. Baltic v2 items carried forward as prerequisites (locked from S66). Add E7 prep sections for store/i18n/launch (unchecked until S71/S72). Docs-only. Cite v2 + S69 everywhere. Stage Release.  

---

## Pre-requisites (S57–S68 + S69; carried from v2 + S69 closeout)

- [x] S57–S64 Baltic v2 content expansion COMPLETE (human ack; 10 policies + 9 goldens; per baltic-v2-scenario-manifest.yaml + s57-s64 closeouts)  
- [x] S65 manifest + gate + re-index (unified baltic-v2-corpus; 1232 baseline floor)  
- [x] S66 content manifest + playtest index + release-checklist-v2 (10+9 locked; v2 corpus) — see production/release/release-checklist-v2.md  
- [x] S67 regression lock + Buildkite preflight  
- [x] S68 release train gate + human ack ready ("i provide the ack")  
- [x] S69 foundation COMPLETE (boundary published superseding release-train for S69+; gate-matrix refreshed to 1232/0f/6/6/18/18; GitNexus re-index 19962/37627/2462; closeout PASS) — smoke-sprint-69-closeout-2026-06-25.md  

**Standing invariants (from commercial-launch-scope-boundary + v2 + execute-plan §6/§7; RUN+READ every sprint):**  
- Test ≥1232/0f monotonic  
- ReplayGolden 6/6  
- C2 proxy 18/18  
- Baltic hash `17144800277401907079` preserved (v2 goldens)  
- ZERO DelegationBridge.cs edits  
- CatalogWriteGate extend-only (docs avoid)  
- GitNexus pre (list/detect/impact CRITICALs exact §5) + verification-before  
- Stage: Release (no advance S72)  

**All pre-reqs + invariants verified in S69 closeout + this verif-before. Cite S66 v2 + S69 artifacts.**

---

## Future phase — Commercial launch execution (gated)

> **Deferred.** Store submission, locale production, and revenue launch are **not** in scope for S73–S80 Baltic v3 content. When the program advances to Launch stage execution, use:
>
> [`production/gate-checks/commercial-launch-execution-gate-TBD.md`](../gate-checks/commercial-launch-execution-gate-TBD.md)
>
> Prerequisites: Launch stage in `production/stage.txt`, asset pipeline >5%, i18n production pass, store accounts, Buildkite production promotion. Human ack required.

---

## Baltic v2 Content Manifest (from S66 v2; locked prereq)

**10 policies** (data/scenarios/ + baltic-v2-*.policy.json per manifest + release-checklist-v2): baltic-patrol*, -comms, -destroyed-target-reengage, -classify, -mission, -mission-roe, -catalog, -datalink, -combat-domains, -spoof, -readiness, -intercept + v2 variants (comms-challenged, jammed, patrol-band-b/c, mission-event, narrative-arc, theater etc).  

**9 goldens** (tests/regression/): replay-golden-baltic-v2-*.txt (patrol, comms-challenged, jammed, mission-event, patrol-band-b/c, patrol-mission-v2, theater, theater-alt + others; hash matches).  

**Unified manifest entry (S66):** releaseVersion=unified-baltic-v2-corpus; contentHash...; 19 drops; tlTier=TL0.  

**Playtest Corpus Index:** See production/qa/evidence/baltic-v2-playtest-index.md + production/playtests/ (S57–S64 human + batch; bands A/B/C; thinkalouds).  

[ ] Re-verify manifest roundtrip post S70 (S71+).  

---

## E7 Commercial Launch Prep Sections (NEW in v3; S70–S72)

**Store Prep (S70-01/02 + assets):**  
- [ ] store-page-draft.md (Steam short/long desc, features, tags; Baltic v2 corpus cited) — production/release/store/  
- [x] asset-checklist.md (capsule/screenshots/trailer placeholders) — production/release/store/  
- [ ] platform-notes.md (internal submission notes)  
- [x] **Asset manifest (Phase B 2026-06-25):** `design/assets/asset-manifest.md` (master; 42 items) + `design/assets/entity-inventory.md` (Phase 0b inventory; user review pending) + spec stubs `design/assets/specs/{c2-ui,baltic-patrol,store-capsule}-assets.md` — dashboard plan Phase B / asset-spec skill  
- [ ] Assets produced + reviewed (future; post S70 list — capsules/screenshots/trailer still Needed per manifest)  

**Community (S70-03):**  
- [ ] community-templates.md (Steam/Discord/FAQ/support/runbook drafts) — this dir  
- [ ] Templates linked in evidence-index (S71)  

**i18n / Launch Docs (S71):**  
- [x] i18n-pipeline-spec.md + string-inventory + extraction-plan (P0 en-US only) — S71-01/02 COMPLETE (cloud independent subagent; cites boundary + future-sprint-roadpmap-062526.md §3/6/7/10 + roadmap-execute-plan-062526.md §3/4 + AGENTS + S69/S70/S66 v2 + Game-Requirements; GitNexus pre + verif-before; see sprint-status s71_ + production/release/*.md)  
- [ ] launch/ pack: patch-notes-template.md, faq-draft.md, support-runbook-draft.md, evidence-index.md (links S57–S71 prep + S66 v2)  
- [ ] Localization QA plan (qa/) — `production/qa/qa-plan-sprint-71-l10n-prep-2026-06-25.md` created (S71-05; locale smoke strategy; cites execute-plan §3/§4 + boundary + GitNexus pre + verif; no PlayMode); ready S71/S72. See sprint-71-i18n-launch-docs.md + smoke-sprint-71 stub.  

**Checklist v3 Complete + Gate (S72):**  
- [ ] All E7 prep sections checked + indexed  
- [ ] release-checklist-v3.md complete for prep scope  
- [ ] S69–S71 closeouts PASS  
- [ ] Gates (1232/0f, 6/6, 18/18, hash, ZERO, GitNexus exact) PASS  
- [ ] Human ack: "commercial launch prep complete"  
- [ ] Stage remains Release (document separately)  

**Build & Gates (every track + closeout, same as v2 + execute §6):**  
[ ] dotnet build : 0e  
[ ] dotnet test : 0f >=1232  
[ ] Replay 6/6 + C2 18/18  
[ ] Hash preserved + ZERO bridge  
[ ] GitNexus pre (search+use list/detect/impact CRITICALs §5 exact) + detect low/docs  
[ ] verification-before (RUN+READ full before claim)  
[ ] Scope cite: this v3 + boundary + roadmap §3/6/7/10 + execute §3/4 + S69 + S66 v2 + AGENTS  

**Exact commands (from boundary/execute/S69):**  
```
export PATH="$HOME/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet build ProjectAegis.sln --no-restore -v q
dotnet test ProjectAegis.sln -v minimal --no-build
dotnet test ... --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
... PlayModeSmokeHarnessTests
rg "17144800277401907079" tests/regression/
rg DelegationBridge src/ --glob "!**/DelegationBridge.cs" -l
```

---

## Go/No-Go for Prep Complete (S72 target)

- All prereqs (S66 v2 + S69) [x]  
- Store/community/i18n/launch sections populated + indexed  
- Gates + GitNexus + verif-before PASS  
- Human ack obtained  
- **No** store submission / revenue / stage advance (per boundary)  

**Verdict (S70 skeleton):** Baltic v2 locked [x] (from v2); E7 prep sections skeleton added. Ready for S70 tracks to populate store S70-01/02 + community S70-03. Full completion in S71/S72.  

**S70-04 partial complete (skeleton).** Supersedes release-checklist-v2.md for E7 prep slice only (v2 remains authoritative for Baltic ops). All cites present. Low risk docs.  

Cites: commercial-launch-scope-boundary-2026-06-25.md + future-sprint-roadpmap-062526.md §3/§6/§7/§10 + roadmap-execute-plan-062526.md §3/§4 + AGENTS.md + S69 artifacts + S66 release-checklist-v2.md + Baltic v2 corpus. Pre state + GitNexus pre (list/detect/impact) + gates RUN+READ.  

*Independent subagent. S70 tracks partial complete (note closeout later).*
