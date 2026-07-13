# Sprint 71 — i18n + Launch Docs

**Dates:** 2026-06-25+ (est. 6–8 days post S70)  
**Lead:** E7 Commercial Launch Prep  
**Goal:** i18n pipeline spec + string inventory + extraction plan (S71-01/02); launch doc pack (patch-notes-template, faq, support-runbook, evidence-index) (S71-03/04); localization QA plan (S71-05: locale smoke strategy); closeout (S71-06). Prep i18n/launch for post-prep (P0 en-US only; no production translations). Extend S46 B5 + S70 checklist v3.  
**Capacity:** 6–8 days, **1 local / 3 cloud** (cap 4). Cloud for i18n/launch/l10n-qa; local closeout.  
**Model:** Per roadmap-execute-plan-062526.md §3/§4 (serial S69→S72; parallel inside after baseline; GitNexus + verification-before; dispatching-parallel-agents + worktrees). Stage remains **Release**. Cite commercial-launch-scope-boundary-2026-06-25.md + future-sprint-roadpmap-062526.md §3/§6/§7/§10 + roadmap-execute-plan-062526.md §3/§4 + AGENTS.md + S69/S70 + S66 v2 on all. Docs-only.  

## Tasks / Tracks (from execute-plan §3/§4 exact S71 table)

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S71-01/02 | i18n pipeline spec + string inventory + extraction plan | localization-lead (Cloud) | 2–3 | S71 baseline + S70 COMPLETE + release-checklist-v3 i18n section | `production/release/i18n-pipeline-spec.md`, `i18n-string-inventory.md`, `i18n-extraction-plan.md` (P0 en-US only; Unity UI Toolkit/UGUI inventory; extraction workflow; cite Game-Requirements/requirements/ + boundary + S70; no prod translation). GitNexus pre + verif-before. Cites mandatory. |
| S71-03/04 | Launch doc pack | technical-writer (Cloud) | 2 | S71 baseline + S71-01 inventory | `production/release/launch/patch-notes-template.md`, `faq-draft.md`, `support-runbook-draft.md`, `evidence-index.md` (links S57–S71 prep + S66 v2 + S70 artifacts). All under launch/. Low risk docs. Cites enforced. |
| S71-05 | Localization QA plan | qa-lead (Cloud) | 1 | S71 baseline + S71-01/02 string inventory (post) | `production/qa/qa-plan-sprint-71-l10n-prep-2026-06-25.md` — locale smoke strategy for post-prep future work; P0 en-US focus; string freeze notes; manual hardcode/UI smoke strategy; **no PlayMode changes in S71 unless user ack + TDD**. GitNexus pre + verif. |
| S71-06 | Closeout | c-sharp-devops-engineer (Local) | 1 | All prior tracks | gt submit/restack; re-run Phase 0 gates (0e/1232/0f/6/6/18/18/hash/ZERO); smoke-sprint-71-closeout-*.md; update sprint-status.yaml + qa/; all artifacts cite full authorities; GitNexus detect pre-commit. Stack/sprint71 gt notes updated. |

### Wave order (per §4)

(W1 i18n-pipeline ∥ W2 launch-docs) → W3 l10n-qa-plan (after string inventory) → W4 Closeout.

**Sprint plans / kickoffs:** This doc + production/agentic/sprint-71-*.md (create/update at dispatch / this prep for closeout readiness).

## Baseline @ Start (S70 COMPLETE + verif 2026-06-25)

- S70 COMPLETE (smoke-sprint-70-closeout-2026-06-25.md + sprint-status s70)
- Tests: **1232/0f** (monotonic; breakdown: 279 Sim + 43 Cli + 247 Del + 5 Excel + 252 UA + 406 Data)
- ReplayGolden: **6/6**
- C2: **18/18**
- Build: **0e/0w**
- Hash: `17144800277401907079` preserved (incl v2 goldens)
- ZERO DelegationBridge (no .cs edits)
- GitNexus: 19962/37627/2462 @28c582d; impacts CRITICAL exact §5/§7 (CatalogWriteGate 178, Patrol 97, DelegationBridge 127, BalticReplayHarness 52)
- Release stage: Release (no advance)
- S70 artifacts + release-checklist-v3.md (i18n/launch sections skeleton)

**Verification-before (pre S71 dispatch + per track + closeout):** Full RUN+READ gates + GitNexus (search_tool then list_repos + detect_changes + impact upstream summaryOnly on CRITICALs per execute-plan §5/§6/§7) + boundary/roadmap/execute/AGENTS/S69/S70/S66 cites. Confirmed in S70 closeout + this prep.

## GT Notes (stack/sprint71)

See `stack/sprint71/WORKTREE-README.md` (created in this prep). Prefixes exact from execute-plan §4. Local closeout owns restack + verif + smoke + status. Cite this plan + execute-plan §4/5.

## Risks & Mitigations

- Scope creep (i18n prep vs full loc): Enforce P0 en-US only; no translation work; no PlayMode unless ack + TDD (execute §4 S71-05).
- Citations drift: Mandatory on every artifact; preflight cites.
- Parallel merge: Single owner per file; coordinator closeout only.
- GitNexus: Re-run pre every track + detect before commit. Low expected (docs-only).
- PlayMode: Explicitly scoped out for S71 l10n QA unless ack.

## Definition of Done (S71)

- [ ] i18n pipeline spec + inventory + extraction (S71-01/02)
- [ ] launch/ pack complete + evidence-index linking S57–S71 (S71-03/04)
- [ ] qa-plan-sprint-71-l10n-prep-*.md (locale smoke strategy; no PlayMode) (S71-05)
- [ ] Closeout PASS (gates + GitNexus + smoke doc + status + gt notes) (S71-06)
- All artifacts self-contained with full MANDATORY cites + pre state + GitNexus pre + verif-before evidence.
- Ready for S72 dispatch (gate).

**S71-01/02 i18n pipeline spec track COMPLETE (cloud agent, independent subagent per execute-plan §4):**  
- Created: `production/release/i18n-pipeline-spec.md` (extraction workflow, locale tiers P0 en-US for prep, Unity UI Toolkit vs UGUI inventory; cites boundary + roadmap-062526 §3/6/7/10 + execute-plan §3/4 + AGENTS + S69/S70/S66 v2 + GR).  
- Created: `production/release/i18n-string-inventory.md` (C2/HUD/menu strings with paths from UXML + PanelHosts + editor; no translation; P0 en-US; cites all mandatory).  
- Created: `production/release/i18n-extraction-plan.md` (phased extraction; Phase 0 baseline complete; cites Game-Requirements/requirements/20-Command-And-Control-UI.md + related + all authorities).  
GitNexus pre (search+use list/detect/impact CRITICAL §5 exact): 19962/37627/2462 low; 178/97/127/52 CRIT. verification-before: gates 0e/1232/0f/6/6/18/18/hash/ZERO RUN+READ before claims. All self-contained + cites. Low risk docs-only.  

**S71 tracks readiness for dispatch + closeout prep (l10n QA + stub).** Low risk: docs only. S71-05/06 focus in this independent subagent run. S71-01/02 deliverables landed; update release-checklist-v3 i18n section on closeout.

Cites: production/commercial-launch-scope-boundary-2026-06-25.md ; docs/reports/future-sprint-roadpmap-062526.md §3/§6/§7/§10 ; docs/reports/roadmap-execute-plan-062526.md §3/§4/§5/§9 ; AGENTS.md ; S69/S70 complete (smoke + plan/kickoff) + S66 release-checklist-v2.md + Baltic v2 corpus/manifest + release-checklist-v3.md ; GitNexus pre + verif-before (19962/37627/2462 + 178/97/127/52 exact; gates 0e/1232/0f/6/6/18/18/hash/ZERO RUN+READ). Independent subagent for S71-01/02 i18n pipeline spec track + S71-05 l10n QA plan + S71 closeout prep (cloud/local mix per plan, execute-plan §4).

*Docs only. Cites everywhere. GitNexus pre + verif-before executed.*
