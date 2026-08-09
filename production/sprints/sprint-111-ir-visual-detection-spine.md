# Sprint 111 — IR / Visual Detection Spine (DRG-10)

**Dates:** 2026-08-09 → 2026-08-13  
**Program:** Release Product Progress (S110–S114) — **S111**  
**Predecessor:** S110 COMPLETE  
**Stage:** **Release** · **Not Launch**  
**Authority:** `production/agentic/agentic-workflow-sprint-series-2026-08-09.md`  
**Linear:** [DRG-10](https://linear.app/drgamtd-workspace/issue/DRG-10)  
**QA:** `production/qa/qa-plan-sprint-111-ir-visual-2026-08-09.md`  
**Kickoff:** `production/agentic/sprint-111-parallel-kickoff-2026-08-09.md`

## Goal

Ship a **deterministic IR + visual (EO) detection spine** alongside radar: modality enum, env-mask model, trial field, RF-jam applies to radar only, catalog extend-only modality column + fixture sensors. Not full atmospheric physics.

## Tracks (surface-disjoint)

| Track | Story | Surface | Linear |
|-------|-------|---------|--------|
| A Sim spine | S111-01 | `src/ProjectAegis.Sim/Sensors/**`, `Scenario/ScenarioDetectionTrial.cs`, `Sim.Tests/Sensors/**` | DRG-10 child or same |
| B Catalog | S111-02 | `assets/data/catalog/migrations/**`, `src/ProjectAegis.Data/Catalog/**`, Data.Tests | DRG-10 catalog |
| C Closeout | S111-03 | production docs / Linear | — |

## Must Have

| ID | AC |
|----|-----|
| S111-01 | `SensorModality` Radar/Infrared/Visual; `IrVisualDetection` env masks; trial `Modality` default Radar; RF jam from ScenarioJamResolver only when Radar; IR/Visual do not require active radar |
| S111-02 | Migration ADD COLUMN `modality` DEFAULT `'Radar'`; CatalogSensorBinding optional Modality; ≥1 IR + ≥1 Visual fixture binding; extend-only |
| S111-03 | Sim.Tests + Data.Tests green; Replay 6/6; ZERO DelegationBridge; smoke closeout |

## Non-goals

Full EO weather · Launch · Phase N · Baltic hash change · DelegationBridge hotpath · sim clock (S112)

## Hard gates

Suite ≥1638/0f · Replay 6/6 · hash preserved · CatalogWriteGate extend-only · stage Release
