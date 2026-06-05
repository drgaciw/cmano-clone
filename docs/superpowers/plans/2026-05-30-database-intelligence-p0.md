# Database Intelligence P0 — Implementation Plan

> **For agentic workers:** One Graphite PR per DATA-* slice. Track with `- [ ]`.

**Goal:** `ProjectAegis.Data` with versioned snapshots, scenario DB binding, staged writes, P0 validation.

**Architecture:** SQLite dev store, immutable snapshots, write gate; policy repo moves Sim→Data in DATA-3.

**Tech Stack:** .NET 8, NUnit, Microsoft.Data.Sqlite, `dotnet test`.

**Design:** `docs/superpowers/specs/2026-05-30-database-intelligence-p0-design.md`  
**Graphite:** `docs/superpowers/plans/2026-05-30-database-intelligence-graphite-stack.md`

---

## DATA-0 — Spec + ADR (`stack/data/p0-spec`)

- [x] Design spec + ADR-006 + runbook (this stack’s base PR)
- [ ] `gt track` / open PR DATA-0

## DATA-1 — Assembly (`stack/data/assembly`)

- [ ] `ProjectAegis.Data` + `.Tests` + solution entry
- [ ] Smoke test; `dotnet test`

## DATA-2 — Schema (`stack/data/schema`)

- [ ] Catalog entities + migration v1 + in-memory test DB

## DATA-3 — Scenario bind (`stack/data/scenario-bind`)

- [ ] `DbSnapshotStore`, `ScenarioPackage`, move `ScenarioPolicyRepository` from Sim
- [ ] `gitnexus impact ScenarioPolicyRepository` before move
- [ ] Update `SimulationModeConfigurator` references

## DATA-4 — Validation (`stack/data/validation`)

- [ ] `ValidationPipeline`, `WriteGate`, audit log
- [ ] `ICatalogReader.TryGetWeaponEnvelope` → wire `SimulationSession`

## DATA-5 — Import smoke (`stack/data/import-smoke`)

- [x] `CmoMarkdownImporter` subset; approve via write gate in test

---

## Verify

```powershell
dotnet test ProjectAegis.sln
npx gitnexus detect_changes --repo cmano-clone
```
