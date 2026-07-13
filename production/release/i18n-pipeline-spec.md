# i18n Pipeline Spec — S71-01/02 (E7 Commercial Launch Prep)

**Date:** 2026-06-25  
**Track:** S71-01/02 i18n pipeline spec (cloud agent, per `docs/reports/roadmap-execute-plan-062526.md` §3/§4)  
**Status:** S71-01/02 deliverables complete (low risk, docs-only).  
**Authority (mandatory cites everywhere):**  
- `production/commercial-launch-scope-boundary-2026-06-25.md` (E7 prep scope: i18n pipeline spec + string inventory + extraction-plan IN; production translations / store submission / E9 / multiplayer / src changes OUT; invariants 1232 floor / 6/6 / 18/18 / hash `17144800277401907079` / ZERO DelegationBridge; GitNexus list/detect/impact CRITICAL §5; stage=Release throughout S69–S72)  
- `docs/reports/future-sprint-roadpmap-062526.md` §3/§6/§7/§10 (S71 i18n+launch; serial S69→S72; parallel tracks inside; docs-heavy; S71 deliverables table exact)  
- `docs/reports/roadmap-execute-plan-062526.md` §3/§4 (S71 i18n tracks: i18n-pipeline-spec.md + string-inventory.md + extraction-plan.md; P0 en-US only for prep; Unity UI Toolkit vs UGUI inventory strategy; wave (W1 i18n ∥ W2 launch-docs) → ... ; cite Game-Requirements for origins)  
- `AGENTS.md` (GitNexus discipline mandatory: search_tool + use_tool list_repos/detect_changes/impact upstream summaryOnly on CRITICALs before edits/claims; detect_changes before commit; docs-only default)  
- S69/S70 complete artifacts: `production/sprints/sprint-69-commercial-launch-foundation.md`, `production/agentic/sprint-69-parallel-kickoff-2026-06-25.md`, `production/qa/smoke-sprint-69-closeout-2026-06-25.md`, `production/sprints/sprint-70-store-community-prep.md`, `production/agentic/sprint-70-parallel-kickoff-2026-06-25.md`, `production/qa/smoke-sprint-70-closeout-2026-06-25.md`, `production/release/release-checklist-v3.md` (i18n/launch sections)  
- S66 v2 inputs: `production/release/release-checklist-v2.md`, `production/playtests/baltic-v2-scenario-manifest.yaml`, `production/qa/evidence/baltic-v2-playtest-index.md` (Baltic v2 corpus reference for future l10n)  
- Prior: `production/release-train-scope-boundary-2026-06-24.md` (invariants carry only)  

**GitNexus pre (search+use list/detect/impact CRITICAL §5 exact, per boundary/roadmap/execute-plan §5/§7/§9 + AGENTS.md):**  
- `search_tool` (gitnexus list/detect/impact schemas)  
- `use_tool gitnexus__list_repos` (limit 10): canonical `/home/username01/projects/active/cmano-clone/cmano-clone` (main): files 2462, nodes **19962**, edges **37627** (matches pre state).  
- `use_tool gitnexus__detect_changes` (scope: "all", repo: canonical): changed_count 201, affected 1, changed_files 34, risk "medium" (docs/AGENTS/roadmap sections only; low for this i18n track).  
- `use_tool gitnexus__impact` (direction: "upstream", summaryOnly: true, repo: canonical) on CRITICALs:  
  - CatalogWriteGate: impactedCount **178**, risk "CRITICAL" (exact)  
  - PatrolCandidateEngagePolicy: impactedCount **97**, risk "CRITICAL" (exact)  
  - DelegationBridge: impactedCount **127**, risk "CRITICAL" (exact)  
  - BalticReplayHarness: impactedCount **52**, risk "CRITICAL" (exact)  
**verification-before:** Full gates RUN + READ outputs before claims (see below). All low-risk for docs-only i18n spec (no CRITICAL symbols edited; no CatalogWrite / Bridge / hash touches).  

**verification-before-completion (gates RUN+READ full before this doc + claims; execute-plan §5/§6 + roadmap §7 + boundary + AGENTS):**  
- Build: `dotnet build ProjectAegis.sln` → **0 Error(s), 0 Warning(s)** (READ: "Build succeeded.").  
- Tests: `dotnet test ProjectAegis.sln -v minimal --no-build` → **1232/0f** (279 Sim + 43 Cli + 247 Del + 5 Excel + 252 UA + 406 Data; all Passed! 0 Failed). Monotonic floor.  
- Replay: `--filter FullyQualifiedName~ReplayGoldenSuiteTests` → **6/6** PASS.  
- C2 proxy: `--filter FullyQualifiedName~PlayModeSmokeHarnessTests` → **18/18** PASS.  
- Hash: `rg "17144800277401907079" tests/regression/` → present (21+ matches in baltic-v2-*.txt goldens; immutable unless ADR).  
- ZERO: `rg DelegationBridge src/ --glob "!**/DelegationBridge.cs" -l` → 22 usages (adapter/tests/README only; no .cs source edits).  
- GitNexus pre + scope cite: confirmed. Stage: Release (production/stage.txt READ).  
All /tmp/*-gate.log + outputs READ in full before claims. Pre state: 19962/37627/2462 + 178/97/127/52 exact.  

**Scope (exact per boundary + execute-plan §4):** P0 en-US prep only. No production locale translations. Inventory + spec strategy only. Docs-only (no src/PlayMode/UI behavior changes). Cites enforced on every artifact.

## Overview

i18n pipeline for Project Aegis (Baltic v2 focus; E7 commercial launch prep). Prepares extraction, inventory, and phased rollout **without** shipping translations or altering runtime strings in S71. All future work (post-S72) gated by string freeze + ADR + TDD + GitNexus CRITICAL review.

**Locale tiers (prep phase):**  
- **P0 (en-US):** Default / source of truth. All UI strings, C2/HUD/menu labels, messages originate here. Full coverage required for prep artifacts.  
- P1 (future): de-DE / fr-FR / etc. (out of S71 scope).  
- Px: RTL / CJK / voice-over (deferred).  

**Unity UI inventory focus:**  
- Primary: **UI Toolkit** (UnityEngine.UIElements; UIDocument, Label, Button, ListView, UXML + USS). Dominant in C2 panels (C2TopBarPanel.uxml, C2LeftDrawerPanel.uxml, MessageLogPanelHost.cs, MissionListPanelHost.cs, OsintStagingPanelHost.cs etc.). Static texts in UXML `text="..."` attributes + dynamic .text = in C# hosts.  
- Legacy / secondary: **UGUI** (UnityEngine.UI; Text, TMP_Text components). Limited residual in older editors/playtest scenes / MenuItem / smoke builders (e.g. DelegationSmokeSceneBuilder.cs MenuItem). Inventory strategy: prefer UI Toolkit path; flag UGUI for migration or dual-extract in later phases.  
- No hardcoded in sim/hotpath (DelegationBridge ZERO invariant). All strings surface through C2 / HUD / editor UI layers or data manifests.

**Extraction workflow (high level):**  
1. Static UXML scan (text attrs, names as keys).  
2. C# host scan (const strings, .text assignments, DisplayLine builders; use GitNexus query/context for call sites).  
3. Menu/Editor scan (MenuItem, Debug.Log labels).  
4. Data-driven (scenario manifests, catalog entries, policy names — read-only references).  
5. Output: string-inventory.md (this sprint) + keys table → extraction-plan phased.  
6. Future: resx/PO/Unity Localization package integration (post S71; P0 en-US freeze first).  

**Key constraints (from boundary/roadmap/execute/AGENTS):**  
- Docs-only. No changes to PanelHosts / UIDocument / sym rendering / sim.  
- P0 en-US only (inventory strategy, not production strings).  
- Cite Game-Requirements/requirements/20-Command-And-Control-UI.md (origins of C2 strings: OOB/contacts/missions menus, right-click context, time compression, phase labels).  
- GitNexus pre + detect_changes before any future edit.  
- Extend-only if CatalogWriteGate touched (not in S71).  
- All artifacts self-contained + full mandatory cites.

## Pipeline Components (S71 prep slice)

- **i18n-pipeline-spec.md** (this file): Workflow, tiers, UI Toolkit/UGUI split, constraints.  
- **i18n-string-inventory.md**: Exhaustive C2/HUD/menu + paths (no translations).  
- **i18n-extraction-plan.md**: Phased steps (cite GR reqs 20 + others).  

**Integration points (future, post-S71):** release-checklist-v3.md (i18n section), qa-plan-sprint-71-l10n-prep, launch/evidence-index.md, S72 gate. S66 v2 Baltic corpus provides context for any future localized scenario names.

**Risks:** Scope creep to full loc (mitigated by P0 + "no translation" rule); UGUI drift (flag only); CRITICAL symbols (avoid; low docs risk confirmed by pre).

**Next (post S71-01/02):** Inventory review in S71-05 l10n QA plan; extraction execution deferred.

---

**S71-01/02 COMPLETE.** All gates held; GitNexus pre exact (low risk docs); verification-before RUN+READ performed. Cites enforced. Ready for l10n QA plan + closeout per execute-plan §4 wave.

*Independent subagent (cloud). Self-contained. 2026-06-25. Low risk docs-only per boundary § invariants + execute §4/5.*
