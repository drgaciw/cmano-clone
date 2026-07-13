# Scenario Editor Phase 2.1 — Scope Boundary

**Date:** 2026-07-08  
**Branch:** `feat/scenario-editor-p2-1`  
**Design:** `docs/superpowers/specs/2026-07-08-scenario-editor-phase2-design.md`  
**Plan:** `docs/superpowers/plans/2026-07-08-scenario-editor-phase2-1-plan.md`  
**Status:** Engineering complete (Tasks 0–8 + Task 9 deferred checklist); gate results below

---

## In scope (shipped headless)

| Capability | Evidence |
|------------|----------|
| Geometry validity helper | `ScenarioGeometryValidity` + tests |
| ORBAT place/move/clone + RP upsert | `ScenarioDocumentEditor` APIs + tests |
| Session + command bus + undo/editVersion | `ScenarioAuthoringSession`, `ScenarioEditCommandBus` |
| Map surface + selection (headless) | `MapAuthoringSurface`, `SelectionInspectorModel` |
| Edit FSM + live findings | `EditModeController`, `LiveFindingsPresenter` |
| Attach mission from selection | Bus `Attach*FromSelection` |
| CLI/MCP parity | `orbat_*`, `reference_point_upsert` + mcp-tools.json |
| Integration parity | `ScenarioMapAuthoringIntegrationTests` |

## Out of P2.1

- Mission Board (P2.2)
- Visual event graph (P2.3)
- NL agents / CMO import / Lua (Phase 3)
- Full Unity EditorWindow (Task 9 deferred — see checklist)

---

## Task 0 — Map / C2 reuse spike

1. **Reuse:** APP-6 glyph contracts for later marker styling; lat/lon on ORBAT/RP DTOs.  
2. **Do not reuse as authoring host:** `C2PresentationController` (sim/bridge selection).  
3. **Do not overload:** runtime C2 UI hosts.  
4. **Decision:** `ProjectAegis.Delegation.UnityAdapter.Authoring` namespace.  
5. **Canonical mutations:** Data `ScenarioDocumentEditor` + bus only.

---

## Task 9 — Unity host

Deferred: `production/qa/scenario-editor-p2-1-editor-host-checklist.md`

---

## Invariants

| Invariant | Rule |
|-----------|------|
| Test floor | ≥1232 monotonic |
| ReplayGolden | 6/6 |
| Hash | `17144800277401907079` |
| PlayModeSmoke | ≥18/18 |
| DelegationBridge | ZERO hotpath production edits |
| CatalogWriteGate | Extend-only |
| Stage | Release |

---

## Gate commands

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter ReplayGolden
grep -r "17144800277401907079" tests/ data/ | head
git diff main --name-only | grep -i DelegationBridge || true
```

## Gate results (2026-07-08)

| Check | Result |
|-------|--------|
| Build | **PASS** 0 errors |
| Full test | **PASS** 1462 total, 0 failed (311+260+5+277+528+81) |
| PlayModeSmoke | **PASS** ≥18 (20 in phase0 filter) |
| ReplayGolden | **PASS** 6/6 |
| Hash present | **PASS** 18 files with `17144800277401907079` |
| DelegationBridge production | **PASS** 0 diff lines vs merge-base main |
| Phase0 smoke (quick) | **PASS** (after `smoke-ac6.sh` pipefail fix) |
| Stage | Release |

**Total commits on branch:** 10 feature/docs + gate docs  
**Task 9:** Unity EditorWindow deferred — checklist only
