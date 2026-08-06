# Game databases overview — Project Aegis

**Date:** 2026-07-20  
**Audience:** designers, engineers, scenario authors  
**Related:** [ADR-006 Data layer boundary](../architecture/adr-006-data-layer-boundary.md), [catalog-seeding.md](../engineering/catalog-seeding.md), [catalog-write-gate.md](../engineering/catalog-write-gate.md)

This document is an inventory of **data stores** used by the game: what is SQLite, what is JSON, what is in-memory, and how they connect at runtime.

---

## Executive summary

| Store | Type | Role in game | Path / form |
|-------|------|--------------|-------------|
| **Platform catalog** | SQLite | Canonical military platforms, sensors, weapons, mounts, loadouts, magazines | `assets/data/catalog/baltic_patrol.db` (~2.7 MB) |
| **In-memory catalog fixtures** | C# objects | Headless tests / Baltic v3 policy fallback | `InMemoryCatalogReader` (BalticPatrol / BalticV3) |
| **Scenario documents** | JSON | ORBAT, missions, reference points, edit metadata | `*.scenario.json` / authoring session files |
| **Scenario policies** | JSON | Mission timelines, detection, ROE defaults | `data/scenarios/*.policy.json` |
| **JSON catalog sources** | JSON | Seed / sample sensors & speculative platforms | `assets/data/catalog/*.json`, `data/catalog/*.json` |
| **Unity Editor caches** | SQLite (editor only) | Search / shader cache — **not game content** | `unity/ProjectAegis/Library/*.db` |

**Architecture rule (ADR-006):** Simulation and Delegation never open SQLite directly. They consume a read-only **`ICatalogReader`**. Catalog mutations go through a **write gate** (staging → validate → promote).

```
┌─────────────────────────────┐
│  assets/data/catalog/       │
│  baltic_patrol.db (SQLite)  │──► SqliteCatalogReader ──┐
└─────────────────────────────┘                         │
┌─────────────────────────────┐                         ▼
│  InMemoryCatalogReader      │──────────────► ICatalogReader
│  (BalticPatrol / V3 fixture)│                         │
└─────────────────────────────┘                         │
                                                        ▼
                          ProjectAegis.Sim / Delegation (rules, engage, validation)
                                                        ▲
┌─────────────────────────────┐                         │
│  Scenario JSON + policy     │──► ScenarioDocumentEditor / ScenarioPolicyRepository
│  (ORBAT, missions, ROE)     │
└─────────────────────────────┘
```

---

## 1. Primary game database: `baltic_patrol.db`

| Property | Value |
|----------|--------|
| **Path** | `assets/data/catalog/baltic_patrol.db` |
| **Engine** | SQLite 3 |
| **Approx. size** | 2.7 MB (as of 2026-07) |
| **Snapshot id** | `baltic_patrol` (79 platforms bound to this snapshot) |
| **Schema evolution** | SQL migrations `assets/data/catalog/migrations/001_…` … `011_…` |
| **Access API** | `SqliteCatalogReader` via `CatalogReaderFactory.ResolveForScenario` |

### 1.1 Why it exists

This is the **living military catalog** for Project Aegis: platforms (air / surface / subsurface), sensors, weapons, mounts, loadouts, magazines, and staging/audit tables for editor imports. Headless sim, scenario validation, and gauntlet QA all resolve against this file when present.

### 1.2 Table groups

#### Live (promoted) content

| Table | Rows (approx.) | Purpose |
|-------|----------------:|---------|
| `platform` | **79** | Platform identity, domain, nationality, combat radius, lat/lon anchors |
| `sensor` | **463** | Platform sensors + base Pd, provenance/TRL columns |
| `weapon_catalog` | **265** | Weapon types, ranges, guidance |
| `platform_mount` | **459** | Mounts on platforms (VLS, rail, gun, tube, pylon, …) |
| `platform_loadout` | **76** | Named loadouts |
| `platform_magazine` | **423** | Magazine quantities on loadout/mount/weapon chain |
| `platform_comms` | 0 | Platform ↔ link roles (schema ready) |
| `platform_mobility` | 0 | Speed / range / altitude / depth (Phase B schema) |
| `platform_signature` | 0 | RCS / IR / acoustic (Phase B schema) |
| `platform_emcon` | 0 | EMCON posture rows (Phase B schema) |
| `platform_damage` | 0 | Damage / readiness profiles |
| `link_catalog` | 0 | Tactical / satcom link definitions |
| `sensor_quarantine` | 0 | Rejected / provisional sensor imports |

#### Catalog governance / release

| Table | Rows (approx.) | Purpose |
|-------|----------------:|---------|
| `catalog_snapshot` | 11 | Snapshot ids + content hash / branch |
| `db_release` | 1 | Release version binding to snapshot + schema |
| `catalog_change_log` | **4232** | Field-level audit trail of catalog edits |

#### Staging (write-gate / import workspace)

| Table | Rows (approx.) | Purpose |
|-------|----------------:|---------|
| `catalog_staging_batch` | 91 | Import / propose batches |
| `catalog_staging_platform` | 196 | Proposed platforms |
| `catalog_staging_sensor` | 789 | Proposed sensors |
| `catalog_staging_mount` | 907 | Proposed mounts |
| `catalog_staging_loadout` | 195 | Proposed loadouts |
| `catalog_staging_magazine` | 761 | Proposed magazines |
| `catalog_staging_weapon` | 1384 | Proposed weapons |
| Other `catalog_staging_*` | 0+ | Comms, damage, EMCON, mobility, signature, link |

### 1.3 Platform inventory (live `platform` table)

**By domain**

| Domain | Count |
|--------|------:|
| surface | 35 |
| air | 24 |
| subsurface | 20 |
| **Total** | **79** |

**Top nationalities (sample)**

| Nationality | Count |
|-------------|------:|
| Russia | 28 |
| Sweden | 20 |
| Germany | 4 |
| Norway / Finland / Denmark | 3 each |
| (plus UK, Poland, Greece, Algeria, US, …) | |

**Example rows** (illustrative): Swedish/Russian/NATO Baltic-region hulls and aircraft with CMO-style ids (`k-31-visby-2009`, `a-19-gotland-…`, `em-sovremenny-…`, etc.).

### 1.4 Weapon catalog snapshot

| weapon_type | Count (approx.) |
|-------------|----------------:|
| Guided Weapon | 146 |
| Gun | 43 |
| Torpedo | 29 |
| Decoy / Bomb | 20 each |
| Rocket | 7 |

### 1.5 Schema migrations

Located under `assets/data/catalog/migrations/`:

| Migration | Theme |
|-----------|--------|
| 001–003 | Sensors: base Pd, review/TRL, quarantine |
| 004 | Platform validation seed |
| 005 | Provenance / audit / staging (req 06) |
| 006 | Snapshot content hash |
| 007–009 | Platform editor Phase A/B (+ damage) |
| 010 | TL snapshot branch |
| 011 | Link catalog staging |

### 1.6 How the game loads it

```
CatalogReaderFactory.ResolveForScenario(policyId)
  ├─ baltic-v3-* policy  → InMemoryCatalogReader.BalticV3Fixture()
  ├─ baltic_patrol.db exists → SqliteCatalogReader (open in place, no re-seed)
  ├─ DB missing → CatalogSeedBootstrap.SeedBalticPatrol then open
  └─ fail-open → InMemoryCatalogReader.BalticPatrolFixture()
```

Important: the **committed** multi-domain DB is **not** overwritten by the small Baltic seed bootstrap on every run.

---

## 2. In-memory catalog fixtures (not files)

Implemented in `ProjectAegis.Data` (`InMemoryCatalogReader`, `CatalogValidationDefaults`).

| Fixture | Platforms (ids) | Use |
|---------|-----------------|-----|
| **BalticPatrolFixture** | `u1`, `hostile-1`, `hostile-far` (+ optional `legacy-patrol-ship`) | Default headless / LiveValidate fallback |
| **BalticV3Fixture** | + `ucav-blue/red`, `usub-blue/red` | Policies whose id starts with `baltic-v3-` |
| **Phase B / magazine variants** | Baltic hulls + mobility/signature/EMCON or magazine rows | Specialized sim tests |

These are **deterministic test doubles**, not the full 79-platform product catalog.

---

## 3. Scenario data (JSON, not SQLite)

Scenarios are **documents**, not rows in `baltic_patrol.db`.

| Kind | Location | Contents |
|------|----------|----------|
| **Scenario document** | Authoring session path / `data/scenarios/examples/*.scenario.json` / validation goldens | Metadata (`dbRef`, seed, policyId), ORBAT units, missions, reference points, edit version |
| **Scenario policy** | `data/scenarios/*.policy.json` | Detection, ROE, mission timelines, comms, combat-domain hooks |
| **Validation goldens** | `assets/data/scenarios/validation/`, `data/scenarios/validation/` | CI/regression scenario packages |

**Binding:** scenario metadata `dbRef` (e.g. `baltic_patrol`) should resolve to a catalog snapshot. Mismatch / migration tooling lives in Data authoring (`ScenarioDbMigrationPreview`, etc.).

**Authoring path (map editor):**  
UI place → `MapAuthoringSurface` → `ScenarioEditCommandBus.PlaceUnit` / `UpsertUnit` → scenario **JSON** ORBAT (platform id is a **string reference** into the catalog, not a FK write into SQLite).

---

## 4. JSON catalog / content sidecars

| File | Role |
|------|------|
| `assets/data/catalog/sensors_baltic.json` | Small sensor seed for bootstrap when seeding a **fresh** DB |
| `data/catalog/sensors_baltic.json` | Repo-side sensor sample |
| `data/catalog/near_future_archetypes.json` | Near-future archetype definitions |
| `data/catalog/speculative_platforms.json` | Speculative platform stubs |
| `data/catalog/sensor_quarantine_sample.json` | Quarantine sample |
| `data/osint_facts.json` | OSINT fact side channel |

CMO markdown import waves enrich the SQLite DB via propose/staging tables (see `cmo-markdown-import.md`).

---

## 5. What is *not* a game content database

| Path | Purpose |
|------|---------|
| `unity/ProjectAegis/Library/Search/*.db` | Unity Editor search indexes |
| `unity/ProjectAegis/Library/ShaderCache.db` | Shader cache |
| `scratch/**/catalog-proposed.db` | Local import experiments (not shipping content) |
| `.git/.graphite_metadata.db` | Tooling metadata |

Do not treat Editor `Library/*.db` as simulation or catalog truth.

---

## 6. Runtime consumers (who reads what)

| Consumer | Store |
|----------|--------|
| Baltic replay harness / gauntlet | `ICatalogReader` → preferably `baltic_patrol.db` |
| Scenario LiveValidate | Today often **BalticPatrolFixture** in-memory (not full SQLite) unless wired otherwise |
| Mission Editor CLI catalog commands | SQLite + staging tables |
| Scenario Map Authoring UI platform **list** | **Hardcoded** `ScenarioPlatformDomainCatalog` (20 presets); form can still type any platform id |
| Platform catalog viewer / import panels | `ICatalogReader` + workbook bridges |

---

## 7. Write model (how the catalog changes)

1. **Propose** import/edit into `catalog_staging_*` (+ batch row).  
2. **Validate** / review (TRL, quarantine, gates).  
3. **Promote** into live tables (`platform`, `sensor`, …) with **`catalog_change_log`** audit.  
4. Optional **snapshot / db_release** bind for scenario compatibility.

CLI surfaces: Mission Editor catalog commands; OSINT/import pipelines; write-gate docs.

---

## 8. Quick reference — open the main DB

```bash
# Schema
sqlite3 assets/data/catalog/baltic_patrol.db ".schema platform"

# Counts
sqlite3 assets/data/catalog/baltic_patrol.db \
  "SELECT domain, COUNT(*) FROM platform GROUP BY domain;"

# Sample platforms
sqlite3 -header -column assets/data/catalog/baltic_patrol.db \
  "SELECT platform_id, display_name, domain, nationality FROM platform LIMIT 20;"
```

Headless C#:

```csharp
var reader = CatalogReaderFactory.ResolveForScenario("baltic-patrol");
// reader implements ICatalogReader — sensors, platforms, mounts, etc.
```

---

## 9. Related documentation

| Doc | Topic |
|-----|--------|
| [ADR-006](../architecture/adr-006-data-layer-boundary.md) | Data assembly boundary, no Unity in catalog |
| [catalog-seeding.md](../engineering/catalog-seeding.md) | Seed vs open committed DB |
| [catalog-write-gate.md](../engineering/catalog-write-gate.md) | Staging / approve |
| [cmo-markdown-import.md](../engineering/cmo-markdown-import.md) | CMO corpus import |
| [game-data-state-assessment-2026-07-09.md](game-data-state-assessment-2026-07-09.md) | Earlier content audit |
| [scenario-editor-platform-combo-uat-2026-07-20.md](scenario-editor-platform-combo-uat-2026-07-20.md) | Place/change platform UAT |

---

## 10. HTML companion

A browsable visual overview is published alongside this file:

**[game-databases-overview-2026-07-20.html](game-databases-overview-2026-07-20.html)**
