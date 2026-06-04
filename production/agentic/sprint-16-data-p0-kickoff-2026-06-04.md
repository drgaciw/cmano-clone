# Sprint 16 — Database Intelligence P0 stack kickoff

**Date:** 2026-06-04  
**Predecessor:** Sprint 15 closed (RTM gate 07–08, 12)  
**Worktree branch:** `stack/sprint16-data-p0-impl`  
**Trunk:** `main` (parallel to `stack/delegation/*`; do not stack on delegation)

## Goal

Execute the **first three Graphite slices** (DATA-0 → DATA-2) from the Database Intelligence P0 stack: spec/ADR baseline, `ProjectAegis.Data` assembly, and catalog schema v1. Satisfy mapped **doc 06 DBI** acceptance rows per slice before opening DATA-3.

**Runbook:** `docs/superpowers/plans/2026-05-30-database-intelligence-graphite-stack.md`  
**Implementation plan:** `docs/superpowers/plans/2026-05-30-database-intelligence-p0.md`  
**Design:** `docs/superpowers/specs/2026-05-30-database-intelligence-p0-design.md`  
**Requirement:** `Game-Requirements/requirements/06-Database-Intelligence.md`  
**Team:** `.claude/teams/database-intelligence-team.yaml`

## Stack topology (this sprint)

```
main
 └── stack/data/p0-spec          (DATA-0)  ← PR 1
      └── stack/data/assembly    (DATA-1)  ← PR 2
           └── stack/data/schema (DATA-2)  ← PR 3
```

**Deferred to Sprint 17+:** DATA-3 (`scenario-bind`), DATA-4 (`validation`), DATA-5 (`import-smoke`).

## Graphite bootstrap

```powershell
cd <cmano-clone-root>
git checkout main
gt sync
gt track stack/data/p0-spec
gt create stack/data/assembly -m "feat(data): add ProjectAegis.Data assembly"
gt create stack/data/schema -m "feat(data): catalog schema v1"
```

Without Graphite: stacked branches; each PR **base = parent slice branch**.

**Agent prompt (per slice):**

```text
Implement DATA-{N} from docs/superpowers/plans/2026-05-30-database-intelligence-p0.md.
Design: docs/superpowers/specs/2026-05-30-database-intelligence-p0-design.md.
Before edits: npx gitnexus impact <Symbol> -d upstream -r cmano-clone
After tests: npx gitnexus detect_changes --repo cmano-clone
Branch: stack/data/<slice>. Do not touch stack/delegation/*.
```

---

## PR 1 — DATA-0 (`stack/data/p0-spec`)

| Field | Value |
|-------|-------|
| **Branch** | `stack/data/p0-spec` |
| **Title** | `docs(data): P0 database intelligence design` |
| **Agent** | `database-intelligence-lead` |
| **Skills** | Spec + Graphite runbook |

### Scope

- Locked P0 design spec + ADR-006 alignment + stack runbook on branch (docs-only delta if already on `main`).
- `gt track stack/data/p0-spec` / open PR DATA-0.

### GitNexus symbols to impact (pre-merge)

| Symbol / artifact | Risk | Notes |
|-------------------|------|-------|
| *(no runtime symbols)* | **LOW** | Doc-only slice |
| `ADR-006` (boundary decision) | **LOW** | Traceability; no C# move |
| Future consumers (documented) | **INFO** | `ScenarioPolicyRepository`, `WeaponEnvelope` / `SimulationSession` called out in ADR-006 §Compliance |

Run `gitnexus impact` only if this PR touches **public API stubs** already on `main`; otherwise skip with note in PR body.

### Doc 06 acceptance (DBI rows) — DATA-0

| Row | Criterion (summary) | Sprint 16 gate |
|-----|---------------------|----------------|
| **NFR** | No `UnityEngine` in `ProjectAegis.Data`; Sim/Delegation may reference Data; Data references neither | ADR-006 + locked spec cite ADR-001 |
| **NFR** | Deterministic headless tests planned in `ProjectAegis.Data.Tests` | P0 plan §7 + test strategy in design spec |
| **DBI-4.3** | `DbReleaseRecord` links `ReleaseVersion` → `SnapshotId` | **Design lock** — schema named in spec §3.2 / §5 |
| **DBI-7.5** | TL-0–TL-5 branch DBs | **Explicit defer** — single `main` + tagged snapshots (Resolved §4) |
| **DBI-5.1–5.2** | Balance telemetry out of P0 commit path | **Explicit defer** — `IBalanceTelemetrySink` no-op in spec |

### Pre-ship

- [ ] PR describes traceability to `06-Database-Intelligence.md` locked status
- [ ] No edits under `stack/delegation/*`

---

## PR 2 — DATA-1 (`stack/data/assembly`)

| Field | Value |
|-------|-------|
| **Branch** | `stack/data/assembly` (base: `stack/data/p0-spec`) |
| **Title** | `feat(data): add ProjectAegis.Data` |
| **Agent** | `database-engineer` |
| **Skills** | `sqlite-schema-management`, gitnexus |

### Scope

- `ProjectAegis.Data` + `ProjectAegis.Data.Tests` + solution entry.
- Smoke test; `dotnet test ProjectAegis.sln` green.

### GitNexus symbols to impact (run before edits)

| Symbol | Kind | Path | Risk |
|--------|------|------|------|
| `ProjectAegis.Data` | assembly | `src/ProjectAegis.Data/` | **MEDIUM** — new downstream for Sim, Delegation, Adapter |
| `ProjectAegis.Data.Tests` | test project | `src/ProjectAegis.Data.Tests/` | **LOW** |
| `CatalogReaderFactory` | static | `src/ProjectAegis.Data/Catalog/CatalogReaderFactory.cs` | **LOW** — stub/factory entry if present |
| `ICatalogReader` | interface | `src/ProjectAegis.Data/Catalog/ICatalogReader.cs` | **LOW** — contract surface |
| Solution / project refs | build | `ProjectAegis.sln`, `*.csproj` | **MEDIUM** — CI graph |

```bash
npx gitnexus impact ICatalogReader -d upstream -r cmano-clone
npx gitnexus impact CatalogReaderFactory -d upstream -r cmano-clone
```

Refresh index after merge: `npx gitnexus analyze` (see `production/qa/sprint-14-gitnexus-data-layer-2026-06-04.md`).

### Doc 06 acceptance (DBI rows) — DATA-1

| Row | Criterion (summary) | Sprint 16 gate |
|-----|---------------------|----------------|
| **NFR** | `ProjectAegis.Data` Unity-free; layering per ADR-001 | Assembly builds **net8.0** (+ netstandard2.1 for Adapter if required) |
| **NFR** | `dotnet test` green for Data smoke | CI passes on slice branch |
| **DBI-8.3** | Doc 05 has no bypass of `IWriteGate` | **Structural** — no direct SQL write helpers in assembly scaffold |
| **DBI-1.3** | `DatabaseIntelligenceOrchestrator.Run` on Baltic catalog | **Stub OK** — test project exists; full run in DATA-4+ |

### Pre-ship

- [ ] `dotnet test ProjectAegis.sln`
- [ ] `npx gitnexus detect_changes --repo cmano-clone`
- [ ] `gt submit --stack --no-interactive` (when Graphite auth OK)

---

## PR 3 — DATA-2 (`stack/data/schema`)

| Field | Value |
|-------|-------|
| **Branch** | `stack/data/schema` (base: `stack/data/assembly`) |
| **Title** | `feat(data): catalog schema v1` |
| **Agent** | `database-modeler` |
| **Skills** | `database-layer-architecture` |

### Scope

- Catalog entities + migration v1 + in-memory test DB.
- Provenance columns on sensor bindings; stable catalog IDs; validation default hooks.

### GitNexus symbols to impact (run before edits)

| Symbol | Kind | Path | Risk |
|--------|------|------|------|
| `SqliteCatalogReader` | class | `src/ProjectAegis.Data/Catalog/SqliteCatalogReader.cs` | **CRITICAL** |
| `InMemoryCatalogReader` | class | `src/ProjectAegis.Data/Catalog/InMemoryCatalogReader.cs` | **MEDIUM** |
| `CatalogEntityMap` | static | `src/ProjectAegis.Data/Catalog/CatalogEntityMap.cs` | **HIGH** |
| `CatalogSensorBinding` | record | `src/ProjectAegis.Data/Catalog/CatalogSensorBinding.cs` | **HIGH** |
| `CatalogPlatformEntry` | record | `src/ProjectAegis.Data/Catalog/CatalogPlatformEntry.cs` | **HIGH** |
| `CatalogProvenanceTier` | static | `src/ProjectAegis.Data/Catalog/CatalogProvenanceTier.cs` | **MEDIUM** |
| `CatalogValidationDefaults` | static | `src/ProjectAegis.Data/Catalog/CatalogValidationDefaults.cs` | **MEDIUM** |
| `CatalogReviewStates` | static | `src/ProjectAegis.Data/Catalog/CatalogReviewStates.cs` | **MEDIUM** |
| `ICatalogClock` / `FixedCatalogClock` | interface/class | `src/ProjectAegis.Data/WriteGate/ICatalogClock.cs` | **MEDIUM** |
| `CatalogJsonImporter` | static | `src/ProjectAegis.Data/Catalog/CatalogJsonImporter.cs` | **HIGH** |
| `CatalogSeedBootstrap` | static | `src/ProjectAegis.Data/Catalog/CatalogSeedBootstrap.cs` | **HIGH** |
| `DetectionBindingKey` | type | `src/ProjectAegis.Data/Catalog/DetectionBindingKey.cs` | **MEDIUM** |
| Migration SQL | asset | `assets/data/catalog/migrations/*.sql` | **HIGH** — schema layout |

```bash
npx gitnexus impact SqliteCatalogReader -d upstream -r cmano-clone
npx gitnexus impact CatalogEntityMap -d upstream -r cmano-clone
npx gitnexus impact CatalogSensorBinding -d upstream -r cmano-clone
npx gitnexus impact CatalogJsonImporter -d upstream -r cmano-clone
```

### Doc 06 acceptance (DBI rows) — DATA-2

| Row | Criterion (summary) | Sprint 16 gate |
|-----|---------------------|----------------|
| **DBI-1.2** | Fixed sort keys; no `DateTime.Now` in commit/export paths | `ICatalogClock` / `FixedCatalogClock` injectable in Data |
| **DBI-1.4** | Relationship lookups consistent after batch commit | Schema + `CatalogEntityMap`; orphan staging prevented at DDL |
| **DBI-2.1** | Unit enums (nm, knots, Mach) with finding codes | `CatalogValidationDefaults` + enum/schema hooks |
| **DBI-2.2** | Sanity: max range > 0, Mach ≤ 25 | Defaults configurable in `CatalogValidationDefaults` |
| **DBI-2.3** | Outlier detect without mutating rows | Schema supports detect-only; no auto-normalize in migration |
| **DBI-3.1** | Sensor bindings reference valid `platformId` / `sensorId` | FK / referential DDL + test fixture |
| **DBI-6.1** | `ValueTier` ∈ {`source_fact`, `interpreted_value`, `gameplay_abstraction`} | Column on sensor rows; unknown → `gameplay_abstraction` |
| **DBI-6.2** | `CatalogJsonImporter` preserves provenance from JSON | Round-trip test in `ProjectAegis.Data.Tests` |
| **DBI-6.3** | `ReviewState` approved / provisional / rejected | `CatalogReviewStates`; rejected rows excluded from export contract |
| **DBI-7.3** | Canonical `PlatformId` / `SensorId` stable across releases | Entity keys in schema v1 |
| **DBI-1.1** | Sorted sensor bindings after commit | **Partial** — `GetSortedSensorBindings()` contract + in-memory DB test (full gate commit in DATA-4) |

**Post-P0 (not gated on DATA-2):** DBI-1.5, DBI-2.4–2.5, DBI-3.2–3.5, DBI-4.1–4.2, DBI-4.4–4.5, DBI-6.4–6.5, DBI-7.1–7.2, DBI-7.4–7.5, DBI-8.1–8.5.

### Pre-ship

- [ ] `dotnet test ProjectAegis.sln`
- [ ] In-memory catalog tests pass (schema v1 + seed fixture)
- [ ] `npx gitnexus detect_changes --repo cmano-clone`
- [ ] No changes to `stack/delegation/*`

---

## Worktree

| Worktree | Branch | Track |
|----------|--------|-------|
| `2026-06-04-bb909adc` (this clone) | `stack/sprint16-data-p0-impl` | DATA-0..2 implementation |

Rebase DATA-3+ after delegation merges touching `ScenarioPolicyRepository`. Stash unrelated WIP before `git checkout main` for stack work.

## Coordination

| Concern | Action |
|---------|--------|
| Delegation stack | Do not modify `stack/delegation/*` |
| Req 06 on `main` | P0 agents/MCP may already exist (#68); this sprint completes **stacked DATA-0..2** gaps per runbook |
| Mission editor | ADR-008 validation shares types later; keep scenario engine separate from catalog write gate (ADR-008 §Alt 4) |
| Tracker | Update `Game-Requirements/implementation-tracker-2026-06-04.md` row **06** when DATA-2 merges |

## Non-goals (Sprint 16)

- DATA-3 policy move (`ScenarioPolicyRepository` → Data)
- DATA-4 `CatalogWriteGate` / `ValidationPipeline` full wiring to `SimulationSession`
- DATA-5 CMO markdown import smoke
- Doc 05 OSINT agent implementation (see `production/sprints/sprint-16-backlog.md`)
- Wave 5 feature code on `feat/wave5-attack-readiness-spoof`

## Exit criteria

- [ ] Three PRs merged (or approved): DATA-0, DATA-1, DATA-2
- [ ] All **Sprint 16 gate** DBI rows above checked per PR
- [ ] GitNexus impact run for every **HIGH/CRITICAL** symbol in DATA-2 before merge
- [ ] `dotnet test ProjectAegis.sln` green on `stack/sprint16-data-p0-impl`

## References

- GitNexus inventory: `production/qa/sprint-14-gitnexus-data-layer-2026-06-04.md`
- ADR-006: `docs/architecture/adr-006-data-layer-boundary.md`
- Sprint 16 backlog stub: `production/sprints/sprint-16-backlog.md`