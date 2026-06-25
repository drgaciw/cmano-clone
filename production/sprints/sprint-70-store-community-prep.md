# Sprint 70 — Store + Community Prep

**Dates:** 2026-06-25+ (est. 6–8 days post S69)  
**Lead:** E7 Commercial Launch Prep  
**Goal:** Produce store page drafts (S70-01/02), community templates (S70-03), release-checklist-v3.md skeleton (S70-04); closeout (S70-05). Prep artifacts for Steam/community use in E7. Extend S46 B5 paths + S66 v2 corpus.  
**Capacity:** 6–8 days, 1 local / 3 cloud (cap 4). Cloud for store/community/checklist; local closeout.  
**Model:** Per roadmap-execute-plan-062526.md §3/§4 (serial S69→S72; parallel inside after baseline; GitNexus + verification-before; dispatching-parallel-agents + worktrees). Stage remains **Release**. Cite commercial-launch-scope-boundary-2026-06-25.md + future-sprint-roadpmap-062526.md §3/§6/§7/§10 + execute-plan + AGENTS + S69 + S66 v2 on all. Docs-only.  

## Tasks / Tracks (from execute-plan §3/§4 exact S70 table)

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S70-01/02 | Store page drafts + assets | community-manager (Cloud) | 2–3 | S70 baseline + S69 COMPLETE | `production/release/store/store-page-draft.md` (Steam short/long desc, features, tags; cite Baltic v2 corpus from release-checklist-v2 + baltic-v2-scenario-manifest.yaml); `asset-checklist.md`; `platform-notes.md`. GitNexus pre + verif-before RUN+READ. Cites: boundary + roadmap §3/4/6/7/10 + execute §3/4 + AGENTS + S69 closeout + S66 v2. |
| S70-03 | Community templates | community-manager (Cloud) | 1–2 | S70 baseline | `production/release/community-templates.md` (Steam/Discord/FAQ/support templates; Baltic v2 focused). Low risk docs. Cites enforced. |
| S70-04 | Checklist v3 skeleton | release-manager (Cloud) | 1–2 | S70 baseline + S66 v2 input + S69 | `production/release/release-checklist-v3.md` (skeleton superseding v2 for E7 prep slice; Baltic prereqs [x]; add E7 store/i18n/launch sections unchecked; cite v2 + S69). GitNexus pre + gates. |
| S70-05 | Closeout | c-sharp-devops-engineer (Local) | 1 | All prior tracks | gt submit/restack; re-run Phase 0 gates (0e/1232/0f/6/6/18/18/hash/ZERO); smoke closeout doc; update sprint-status + qa/; all artifacts cite full authorities; GitNexus detect pre-commit. |

### Wave order (per §4)

S70 baseline (coordinator) → (W1 store pages S70-01/02 ∥ W2 community S70-03 ∥ W3 checklist-v3 S70-04) → W4 Closeout.

**Sprint plans / kickoffs:** This doc + production/agentic/sprint-70-*.md (create at dispatch).  

## Baseline @ Start (S69 COMPLETE + verif 2026-06-25)

- S69 COMPLETE (smoke-sprint-69-closeout-2026-06-25.md)  
- Tests: **1232/0f** (breakdown as S69)  
- ReplayGolden: **6/6**  
- C2: **18/18**  
- Build: **0e/0w**  
- Hash: `17144800277401907079` preserved (incl v2 goldens)  
- ZERO DelegationBridge (no .cs edits)  
- GitNexus: 19962/37627/2462 (post S69-03); impacts CRITICAL exact §5 (178/97/127/52)  
- Baltic v2 corpus locked (S66 v2 + manifest)  
- Release stage: Release (no advance)  

**Verification-before (pre S70 dispatch + per track):** Full RUN+READ gates + GitNexus (search_tool then list/detect/impact on CRITICALs) + boundary/roadmap/execute/AGENTS/S69/S66 cites. Confirmed in S69 closeout + pre-authoring for S70.

## Risks & Mitigations

- Scope creep (prep vs submission): Enforce boundary in/out (E7 prep IN; submission/E9/multi/bridge OUT).  
- GitNexus/docs drift: Re-run pre every track + detect before commit. Low expected.  
- Parallel merge: Single owner per file; coordinator closeout only.  
- Citations: Mandatory on every artifact.  

## Definition of Done (S70)

- [ ] Store drafts + asset/platform notes published (S70-01/02)  
- [ ] Community templates published (S70-03)  
- [ ] release-checklist-v3.md skeleton (supersedes v2 for E7; cites S66 v2 + S69) (S70-04)  
- [ ] Closeout PASS (gates + GitNexus + smoke doc + status) (S70-05)  
- All artifacts self-contained with full MANDATORY cites + pre state + GitNexus pre + verif-before evidence.  
- Ready for S71 dispatch (i18n + launch docs).  

**S70 tracks partial complete (store/community/checklist skeletons + notes). Closeout S70-05 later per execute-plan. Low risk: docs only.**  

Cites: production/commercial-launch-scope-boundary-2026-06-25.md ; future-sprint-roadpmap-062526.md §3/§6/§7/§10 ; roadmap-execute-plan-062526.md §3/§4 ; AGENTS.md ; S69 artifacts ; S66 release-checklist-v2.md + Baltic v2 corpus. Pre state + gates PASS + GitNexus pre confirmed.  

*Independent subagent for S70 Store + community prep tracks (cloud per execute-plan, dispatching-parallel-agents).*
