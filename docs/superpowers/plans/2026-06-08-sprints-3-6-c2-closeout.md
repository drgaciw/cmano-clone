# Sprints 3–6 Closeout — C2 Shell / Map / Selection / ADR-007

> **For agentic workers:** REQUIRED SUB-SKILL: `superpowers:dispatching-parallel-agents` (parallel wave) then orchestrator merge. Per-story gate: `/story-done`.

**Goal:** Close Sprints 3–6 from **~85% → 100%** — fill projection/binder/selection test gaps, resolve SensorC2↔C2LeftDrawer overlap, add Unity adapter seams (G1), run `/story-done` on all epics, mark `production/sprint-status.yaml` `done` with evidence.

**Architecture:** Headless-first (ADR-010). Presentation-only selection via `C2SelectionResolver` + `C2PresentationController`; map Phase A placeholder per ADR-007 (Cesium = S20+). **GitNexus `impact` before editing** `DelegationBridge`, `C2PresentationController`, `MapPictureBridge`, `SimulationSession`.

**Baseline (2026-06-08):** `dotnet test ProjectAegis.sln` green (403+). Sprint-status marks S3–S5 stories `done` but S3 lacks formal `status: complete`; spirit1 gap analysis scores minimal sensor C2 at **85%** (G1 graph isolation, G3 Unity-not-in-CI).

**References:**
- [production/sprint-status.yaml](../../../production/sprint-status.yaml) — S3–S6 blocks
- [production/sprints/sprint-6-c2-selection.md](../../../production/sprints/sprint-6-c2-selection.md)
- [production/sprints/sprint-5-c2-map-globe.md](../../../production/sprints/sprint-5-c2-map-globe.md)
- [production/sprints/sprint-4-c2-map-prep.md](../../../production/sprints/sprint-4-c2-map-prep.md)
- [docs/architecture/adr-007-c2-map-presentation.md](../../../docs/architecture/adr-007-c2-map-presentation.md)
- [Game-Requirements/reviews/spirit1-vertical-slice-gap-analysis-2026-06-05.md](../../../Game-Requirements/reviews/spirit1-vertical-slice-gap-analysis-2026-06-05.md)
- Prior pattern: [2026-06-08-sprint-2-sensor-c2-closeout.md](./2026-06-08-sprint-2-sensor-c2-closeout.md)

---

## Definition of Done

- [x] All c2-left-drawer-slice + sensor-c2-ui-slice + sprint 4–6 stories pass `/story-done` with test-criterion traceability tables
- [x] **New tests:** `C2TopBarProjectionTests`, expanded `C2SelectionFlowTests`, adapter-level selection-flow integration test(s)
- [x] **Binder coverage:** MapPanelBinder + OobTreePanelBinder + UnitDetailPanelBinder exercised in one harness row (classify scenario)
- [x] **SensorC2 overlap:** Contacts tab selection path documented; no duplicate/conflicting selection between `SensorC2PanelHost` and `C2LeftDrawerPanelHost` Contacts tab
- [x] **G1 adapter seam:** `IC2PresentationFeed` in `ProjectAegis.Delegation.UnityAdapter` referenced by Unity `*PanelHost` types — graph-auditable path
- [x] **UnitReadinessMap / CatalogEntityMap:** existing tests green (`UnitReadinessEngageTests`, `CatalogEntityMapTests`); no S3–6 code change required
- [x] Full local gate PASS: `dotnet build` + `dotnet test ProjectAegis.sln` + `PlayModeSmokeHarnessTests`
- [x] Closeout artifacts: this plan + `production/agentic/sprints-3-6-closeout-2026-06-08.md` + `production/qa/smoke-sprints-3-6-closeout-2026-06-08.md`
- [x] `production/sprint-status.yaml` — formal `status: complete` for sprints 3, 4, 5, 6 with `tests_passed`, `evidence`, `closeout_note`

---

## Orchestrator Loop (run first, then after agents return)

### Phase 0 — Baseline (orchestrator, sequential)

```bash
npx gitnexus analyze   # if index stale
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests -v minimal
```

Record: test count, commit SHA, any pre-existing failures.

### Phase 1 — Parallel dispatch (4 agents, single message, 4× Task)

Dispatch **Agents A–D** concurrently. Each agent is self-contained; do **not** share session history.

### Phase 2 — Integrate (orchestrator)

1. Review each agent summary; resolve file conflicts (likely touch: tests, yaml, epics).
2. Re-run full gate (commands above).
3. Run `/story-done` on each story (Agent D may have done this — verify).
4. Write `production/agentic/sprints-3-6-closeout-2026-06-08.md`.
5. `gitnexus_detect_changes()` before any commit.

---

## Agent A — C2 Shell: TopBar + Left Drawer + Message Log

**Domain:** Sprint 3 — `C2TopBarProjection`, message log, OOB, mission list projections + binders.

**Gap:** `C2TopBarProjection` has **zero** unit tests; top bar COMMS/score merge untested in isolation.

### Prompt (copy to Task)

```
Close Sprint 3 C2 shell projection gaps in cmano-clone (D:\mycode\cmano-clone).

SCOPE (only these areas):
- src/ProjectAegis.Delegation/Projection/C2TopBarProjection.cs
- src/ProjectAegis.Delegation/Projection/MessageLog*
- src/ProjectAegis.Delegation/Projection/OobTree*
- src/ProjectAegis.Delegation/Projection/MissionList*
- src/ProjectAegis.Delegation.Tests/Projection/ (new C2TopBarProjectionTests.cs)
- production/epics/c2-left-drawer-slice/

REQUIRED:
1. Run gitnexus impact on C2TopBarProjection before editing.
2. TDD: Create C2TopBarProjectionTests with cases:
   - Projects sim time format SIM HH:MM:SS
   - Merges LossesScoringProjection tally into score line
   - Merges CommsStateProjection into top bar label
   - Phase/compression/mode labels pass through
3. Verify existing MessageLog/OobTree/MissionList tests still green; add binder edge case only if a gap is found (e.g. empty OOB).
4. Update story files in c2-left-drawer-slice with Test traceability tables (pattern: sensor-c2 story-002).
5. Run: dotnet test src/ProjectAegis.Delegation.Tests --filter "C2TopBar|MessageLog|OobTree|MissionList"

CONSTRAINTS:
- Do NOT edit SimulationSession, DelegationOrchestrator, or sim tick pipeline.
- Do NOT touch map/selection code (Agent B owns that).
- Presentation-only — no gameplay state in UI layer.

RETURN: Summary of tests added, files changed, test count, any BLOCKING gaps for /story-done.
```

---

## Agent B — Map / Selection / Flow Coverage

**Domain:** Sprints 5–6 — `MapPictureProjection`, `MapPanelBinder`, `C2SelectionResolver`, `C2SelectionFlowTests`, ADR-007 Phase A alignment.

**Gap:** Selection flow has only 2 unit tests; no adapter integration row tying map+OOB+unit detail; `C2PresentationController` untested headlessly.

### Prompt (copy to Task)

```
Close Sprint 5–6 map/selection test gaps in cmano-clone (D:\mycode\cmano-clone).

SCOPE:
- src/ProjectAegis.Delegation/Projection/MapPictureProjection.cs
- src/ProjectAegis.Delegation/Projection/MapPanelBinder.cs
- src/ProjectAegis.Delegation/Projection/C2SelectionResolver.cs
- src/ProjectAegis.Delegation.Tests/Projection/C2SelectionFlowTests.cs (expand)
- src/ProjectAegis.Delegation.Tests/Projection/MapPanelBinderTests.cs (expand if needed)
- src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs (add selection-flow row)
- unity/ProjectAegis/Assets/Scripts/Runtime/C2PresentationController.cs — ONLY if extracting testable logic to Delegation.Tests is needed; prefer testing via existing projection APIs

REQUIRED:
1. gitnexus impact on C2SelectionResolver and MapPanelBinder before edits.
2. Expand C2SelectionFlowTests:
   - OOB row click → map highlight sync (friendly)
   - Contacts-tab-equivalent: hostile contact list row → contact summary (mirror map click)
   - Default selection skipped when selection already set
3. Add PlayModeSmokeHarnessTests row (or dedicated C2SelectionFlowIntegrationTests):
   - baltic-patrol-classify harness → resolve default u1 → map+OOB binders agree → contact click resolves summary
4. Verify ADR-007 Phase A: placeholder layout seed deterministic (existing MapPictureProjectionTests); document in test name/comments.
5. Run: dotnet test --filter "C2Selection|MapPicture|MapPanel"

CONSTRAINTS:
- Do NOT implement Cesium/globe (ADR-007 Phase B = done in S20).
- Do NOT edit C2TopBar or message log (Agent A).
- Selection must remain presentation-only (no order log mutation).

RETURN: New test names, scenarios covered, PlayMode filter results, binder/selection coverage assessment (ADEQUATE/INADEQUATE).
```

---

## Agent C — SensorC2 Overlap + Unity C2 Host Adapter Seams

**Domain:** Sprint 2↔3 overlap — `SensorC2*` vs `C2LeftDrawer` Contacts tab; Unity `*PanelHost` graph traceability (G1).

**Gap:** spirit1 G1 — MonoBehaviour hosts isolated from GitNexus graph; Contacts selection may duplicate SensorC2 panel.

### Prompt (copy to Task)

```
Close SensorC2 overlap and Unity C2 host traceability (G1) in cmano-clone (D:\mycode\cmano-clone).

SCOPE:
- src/ProjectAegis.Delegation/Projection/SensorC2*
- src/ProjectAegis.Delegation.UnityAdapter/Bridge/SensorC2Bridge.cs
- NEW: src/ProjectAegis.Delegation.UnityAdapter/Bridge/IC2PanelHostBridge.cs (or IC2PresentationFeed.cs) — thin seam
- unity/ProjectAegis/Assets/Scripts/Runtime/*PanelHost.cs (C2TopBar, C2LeftDrawer, MapPlaceholder, RightUnit, MessageLog, SensorC2, OobTree, MissionList)
- src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/SensorC2*
- production/epics/sensor-c2-ui-slice/

REQUIRED:
1. gitnexus impact on SensorC2Bridge and DelegationBridgeHost before edits.
2. Audit Contacts tab in C2LeftDrawerPanelHost vs SensorC2PanelHost:
   - Document ownership in epic EPIC.md (which panel owns contact list for MVP)
   - Ensure both use C2PresentationController selection APIs consistently OR deprecate duplicate path with comment + test
3. Add adapter seam interface consumed by at least SensorC2PanelHost + one other host (e.g. MapPlaceholderPanelHost):
   - Interface lives in UnityAdapter assembly (not UnityEngine)
   - Host calls interface; implementation delegates to existing bridge/projection
   - Enables gitnexus edge from host → adapter
4. Add/update integration test proving SensorC2 + left drawer contacts do not fight selection state on classify scenario.
5. Update PLAYMODE-SMOKE.md § selection if wiring changes.
6. Run SensorC2 + PlayMode smoke filters.

CONSTRAINTS:
- Minimal interface surface — no broad refactor of all hosts unless needed for G1.
- Do NOT break headless tests; Unity hosts stay behind #if UNITY_5_3_OR_NEWER.
- Do NOT edit map projection math (Agent B).

RETURN: Seam design summary, overlap resolution, hosts touched, test evidence, gitnexus context snippet for new interface.
```

---

## Agent D — Data Maps + story-done + yaml + QA Evidence

**Domain:** `UnitReadinessMap`, `CatalogEntityMap`, epic/yaml closure, traceability docs, smoke evidence.

### Prompt (copy to Task)

```
Close Sprints 3–6 documentation, yaml, and data-map traceability in cmano-clone (D:\mycode\cmano-clone).

SCOPE:
- src/ProjectAegis.Delegation/Sim/UnitReadinessMap.cs
- src/ProjectAegis.Data/Catalog/CatalogEntityMap.cs
- production/epics/c2-left-drawer-slice/ (all stories)
- production/epics/sensor-c2-ui-slice/ (verify Complete + traceability)
- production/sprint-status.yaml (sprints 3, 4, 5, 6 blocks)
- docs/architecture/requirements-traceability.md (C2 rows only)
- docs/architecture/architecture-traceability-index.md (C2/map rows)
- production/qa/smoke-sprints-3-6-closeout-2026-06-08.md (CREATE)
- production/agentic/sprints-3-6-closeout-2026-06-08.md (CREATE skeleton)

REQUIRED:
1. Verify UnitReadinessMap + CatalogEntityMap tests pass; add cross-reference note in implementation tracker if sprint 3–6 C2 work depends on them (wave5 engage guard uses readiness — link only, no scope creep).
2. Run /story-done workflow (lean review mode) on:
   - production/epics/c2-left-drawer-slice/story-001-full-message-log.md
   - production/epics/c2-left-drawer-slice/story-002-oob-tree-projection.md
   - production/epics/c2-left-drawer-slice/story-003-mission-list-projection.md
   - Re-verify sensor-c2-ui-slice stories if Agent C changes overlap docs
3. Update production/sprint-status.yaml:
   - sprint: 3 → status: complete, completed: 2026-06-08, tests_passed, evidence paths, closeout_note
   - sprint: 4, 5, 6 → refresh tests_passed count, evidence, closeout_note referencing this plan
4. Create smoke evidence doc with: build SHA, total tests, PlayMode count, replay golden, binder/selection test list, ADR-007 Phase A checklist.
5. Update c2-left-drawer-slice/EPIC.md status + acceptance checkboxes.

CONSTRAINTS:
- Do NOT implement code features (Agents A–C own code); only docs/yaml/story status unless a one-line comment fix is needed.
- yaml updates follow sprint-2 closeout pattern (evidence array, closeout_note).
- Ask user before marking stories Complete if Agent A/B/C report BLOCKING gaps.

RETURN: story-done verdicts table, yaml diff summary, smoke doc path, remaining BLOCKERS list.
```

---

## Post-merge Verification Checklist (orchestrator)

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "PlayModeSmokeHarnessTests|ReplayGolden" -v minimal
# Optional: npx gitnexus detect_changes
```

| Check | Pass criteria |
|-------|----------------|
| C2TopBarProjectionTests | ≥4 cases, all green |
| C2SelectionFlowTests | ≥4 cases incl. OOB+contact paths |
| PlayMode selection row | classify scenario selection smoke green |
| SensorC2 overlap | EPIC.md documents single owner; no selection fight |
| G1 seam | Interface in UnityAdapter; ≥2 hosts reference it |
| sprint-status.yaml | S3–S6 `status: complete` + evidence |
| story-done | All c2-left-drawer stories Complete with traceability |

---

## Risk Register

| Risk | Mitigation |
|------|------------|
| Agents edit same test files | B owns C2SelectionFlow/PlayMode; A owns C2TopBar; C owns SensorC2; D owns yaml only |
| HIGH impact on DelegationBridge | Mandatory gitnexus impact; orchestrator reviews bridge diffs |
| story-done BLOCKED on UI evidence | Lean mode: headless binder tests + PLAYMODE-SMOKE proxy suffice per sprint-18 precedent |
| Scope creep into Cesium/APP-6 | Explicitly out of scope — ADR-007 Phase B/C closed in S20–S21 |

---

## Suggested commit (after user approval)

```
feat(c2): close sprints 3-6 — topbar tests, selection flow, adapter seams, yaml

- C2TopBarProjectionTests + expanded C2SelectionFlow coverage
- IC2PanelHostBridge seam for Unity graph traceability (G1)
- SensorC2/left-drawer overlap documented
- sprint-status S3-S6 complete + closeout evidence
```