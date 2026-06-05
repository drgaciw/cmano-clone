# Architecture Review — Coverage Refresh

**Date:** 2026-06-03  
**Mode:** `/architecture-review coverage` (post ADR-006 Accept + ADR-008 Proposed + validation implementation)  
**Verdict:** **CONCERNS** (unchanged; gaps narrowed)

---

## Delta since 2026-06-02

| Change | Impact |
|--------|--------|
| ADR-006 **Accepted** | `platform-db-basepd-slice` unblocked; `CatalogReaderFactory` + SQLite harness path live |
| ADR-008 **Accepted** | `ProjectAegis.Data.Validation` + `ProjectAegis.MissionEditor.Cli` (`scenario_validate`, export gate) |
| Migration `004_platform_validation.sql` | `catalog_snapshot` + `platform` for dbRef / reachability |
| TR-editor-002/004/005 | **Partial** (was Gap) via ADR-008 |
| DATA-3 | **Partial** — `ScenarioDataPaths` in Data; policy profiles remain in Sim (Sim type coupling) |

---

## Traceability counts (`tr-registry.yaml`)

| Status | Count | % |
|--------|-------|---|
| Covered | 11 | 23% |
| Partial | 28 | 60% |
| Gap | 8 | 17% |
| **Total** | **47** | 100% |

Editor validation TRs moved from **Gap → Partial**. Remaining **gaps** include logistics fuel (TR-logistics-003), sensor datalink (TR-sensor-004), combat-domain validators (TR-combat-dom-001..003), agentic Hindsight P1 (TR-agentic-002..003), and selected engage/sensor items.

---

## Remaining coverage gaps (ADR queue)

1. `/architecture-decision combat-domain-validators` — TR-combat-dom-001..003  
2. `/architecture-decision logistics-fuel-model` — TR-logistics-003  
3. `/architecture-decision sensor-side-picture` — TR-sensor-004  
4. `/architecture-decision agentic-aar-infrastructure` — TR-agentic-002..003  
5. ~~Accept ADR-008~~ **Done** (2026-06-03)

---

## Engine / consistency

- No new cross-ADR conflicts detected.  
- ADR-005 remains DOTS/ECS (not engagement); RTM corrected in prior session.  
- Validation engine: pure .NET, no `UnityEngine`, deterministic finding order — aligns with ADR-001/004/006.

---

## QA evidence (this refresh)

- `dotnet test ProjectAegis.sln` — run at session end (required before merge)  
- `ValidationGoldenTests` + `ScenarioValidationEngineTests` — 8 validation tests in Data.Tests  
- PlayMode smoke — unchanged contract via Baltic harness  

---

## Recommended next actions

1. ~~Wire MCP `scenario_validate`~~ → `tools/mission-editor/Invoke-ScenarioValidate.ps1` + `ProjectAegis.MissionEditor.Cli`  
2. ~~Pin golden hashes~~ → `ValidationGoldenHashes` (CI regression)
3. Fresh `/architecture-review full` in new session (skill: separate from ADR retrofit)  
4. DATA-3 completion: extract policy **DTOs** to Data; keep `ScenarioPolicyProfile` in Sim until policy types split  
5. Pre-production: `/test-setup`, `/ux-design` checklist items