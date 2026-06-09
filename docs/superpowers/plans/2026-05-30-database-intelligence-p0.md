# Database Intelligence P0 — Implementation Plan

> **For agentic workers:** One Graphite PR per DATA-* slice. Track with `- [ ]`.

**Goal:** `ProjectAegis.Data` with versioned snapshots, scenario DB binding, staged writes, P0 validation.

**Architecture:** SQLite dev store, immutable snapshots, write gate; policy repo moves Sim→Data in DATA-3.

**Tech Stack:** .NET 8, NUnit, Microsoft.Data.Sqlite, `dotnet test`.

**Design:** `docs/superpowers/specs/2026-05-30-database-intelligence-p0-design.md`  
**Graphite:** `docs/superpowers/plans/2026-05-30-database-intelligence-graphite-stack.md`

**Stack status:** All slices **shipped Sprint 16–17 @ `main`** (`production/agentic/sprint-17-closeout-2026-06-04.md`).

---

## DATA-0 — Spec + ADR (`stack/data/p0-spec`)

- [x] Design spec + ADR-006 + runbook (this stack’s base PR) — shipped Sprint 16 @ main
- [x] `gt track` / open PR DATA-0 — shipped Sprint 16 @ main (docs-only slice on trunk)

## DATA-1 — Assembly (`stack/data/assembly`)

- [x] `ProjectAegis.Data` + `.Tests` + solution entry — shipped Sprint 16 @ main
- [x] Smoke test; `dotnet test` — shipped Sprint 16 @ main (44+ Data tests PASS)

## DATA-2 — Schema (`stack/data/schema`)

- [x] Catalog entities + migration v1 + in-memory test DB — shipped Sprint 16 @ main (migrations `001`–`005`)

## DATA-3 — Scenario bind (`stack/data/scenario-bind`)

- [x] `DbSnapshotStore`, `ScenarioPackage`, move `ScenarioPolicyRepository` from Sim — shipped Sprint 16 @ main
- [x] `gitnexus impact ScenarioPolicyRepository` before move — shipped Sprint 16 @ main
- [x] Update `SimulationModeConfigurator` references — shipped Sprint 16 @ main

## DATA-4 — Validation (`stack/data/validation`)

- [x] `ValidationPipeline`, `WriteGate`, audit log — shipped Sprint 17 @ main (`CatalogWriteGate`, change log)
- [x] `ICatalogReader.TryGetWeaponEnvelope` → wire `SimulationSession` — shipped Sprint 17 @ main

## DATA-5 — Import smoke (`stack/data/import-smoke`)

- [x] `CmoMarkdownImporter` subset; approve via write gate in test — shipped Sprint 17 @ main

---

## Verify

```powershell
dotnet test ProjectAegis.sln
npx gitnexus detect_changes --repo cmano-clone
```