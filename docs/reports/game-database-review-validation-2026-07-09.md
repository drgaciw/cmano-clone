# Project Aegis Game Database — Review & Validation

| Field | Value |
|-------|-------|
| **Date** | 2026-07-09 |
| **Verdict** | **CONCERNS** |
| **Authors** | Data architect + gaming/mil-sim expert (orchestrated parallel specialists) |
| **External source** | https://www.perplexity.ai/computer/a/ee975ade-95cc-5bf5-9285-bee30decac1e |
| **Production seed** | `cmano-clone/assets/data/catalog/baltic_patrol.db` (236 KB) |
| **Propose-only corpus** | `cmano-clone/scratch/nightly-cmo-20260618/catalog-proposed.db` (122 MB) |

---

## 1. Executive verdict

**Overall: CONCERNS**

| Tier | Fitness | Grade |
|------|---------|-------|
| Baltic production seed (MVP patrol harness) | **PASS for intended MVP scope** | B+ |
| Nightly proposed CMO-scale catalog | **FAIL as playable CMO-class DB** | D+ |
| Req 06 P0 schema/write-gate posture | **Mostly met** | B |
| Req 06 full vision / kill-chain completeness | **Partial / blocked at scale** | D |

The database stack is **architecturally serious** (staging write-gate, sensor provenance, quarantine, snapshots, `db_release`). It is **not** yet a CMO-class *playable* intelligence database: the large proposed corpus has volume without closed detect→track→engage graphs.

**Do not** treat `catalog-proposed.db` as production OOB content. **Do** ship Baltic seed for scenario harnesses.

---

## 2. Perplexity source review

| Item | Detail |
|------|--------|
| URL | `https://www.perplexity.ai/computer/a/ee975ade-95cc-5bf5-9285-bee30decac1e` |
| Tool | Firecrawl scrape (markdown + screenshot); Playwright/Chrome DevTools blocked by shared browser lock |
| Timestamp | 2026-07-09 (~21:07 UTC) |
| Outcome | **Private artifact — sign-in required** |

### Exact blocking messages (not invented)

> This artifact is private  
> Sign in if you are the owner of this artifact, or to request access  
> Continue with Google / Apple / email / SSO

Evidence:

- `{SCRATCH}/perplexity-source.md`
- `{SCRATCH}/perplexity-screenshot.png` (1920×1080 PNG from Firecrawl)

**Source limit:** External review content is unavailable unauthenticated. This is the sole external source-limit. All quantitative validation uses local catalog artifacts.

---

## 3. Quantitative validation matrix

Evidence: `{SCRATCH}/metrics.json`, `{SCRATCH}/catalog-structure.log`, `{SCRATCH}/catalog-intelligence-run.log`, `{SCRATCH}/catalog-intelligence-summary.json`.

### 3.1 Core entity counts

| Table / metric | Baltic seed | Proposed (nightly) |
|----------------|------------:|-------------------:|
| `platform` | 3 | 15,107 |
| `sensor` | 2 | 7,210 |
| `weapon_catalog` | 0 | 4,403 |
| `platform_mount` | 0 | 29,583 |
| `platform_loadout` | 0 | 6,795 |
| `platform_magazine` | 0 | **0** |
| `db_release` | 1 | 7 |
| `catalog_snapshot` | 1 | 3 |
| `sensor_quarantine` | 0 | 0 |
| `catalog_staging_batch` | 0 | 54 |
| `catalog_change_log` | 0 | 487,526 |
| Table count | 24 | 30 |
| File size | 241,664 B | 128,184,320 B |

### 3.2 Domain distribution (proposed)

| Domain | Platforms |
|--------|----------:|
| surface | 13,348 |
| subsurface | 924 |
| air | 495 |
| land | 340 |

Baltic: **surface only** (3).

### 3.3 Referential integrity & kill-chain gaps

| Check | Baltic | Proposed |
|-------|-------:|---------:|
| Sensor rows with no matching `platform.platform_id` | 0 | **7,207** |
| Mount orphans (no platform) | 0 | 0 |
| Loadout orphans (no platform) | 0 | 0 |
| Platforms with no sensor | 2 | **15,105** |
| Platforms with no mount | 3 | 8,314 |
| Platforms with **both** sensor and mount | **0** | **0** |
| Weapons with `max_range_meters <= 0` | 0 | 669 |
| Mount `review_state=provisional` | n/a | 29,583 (100%) |

### 3.4 Constraints & release-train presence

| Feature | Present? | Evidence |
|---------|----------|----------|
| CHECK on `sensor.base_pd` (0–1) | Yes | Baltic/proposed schema |
| CHECK on `sensor.trl_level` (1–9) | Yes | Schema |
| CHECK on weapon ranges ≥ 0 | Yes | `weapon_catalog` |
| CHECK on mount arc/capacity | Yes | `platform_mount` |
| SQL FKs on live content tables | **Sparse** | Only staging → `catalog_staging_batch` |
| `PRAGMA foreign_keys` enforced in app | **No** (grep) | SCHEMA-03 |
| `db_release` + `catalog_snapshot` + hash/branch | Yes | SNAP-01 |
| Staging write-gate tables | Yes | GATE-01 |
| Provenance on `sensor` | Yes (full P0 set) | PROV-01 |
| Provenance on weapon/loadout/magazine | **Missing** | PROV-03 |

### 3.5 Nightly approve / quarantine (JSON, 2026-06-18)

| Entity | Parsed/approved | Fitting quarantined | Notes |
|--------|----------------:|--------------------:|-------|
| sensor | 7,208 / 7,208 | 0 | Sensors approved |
| weapon | 4,403 / 4,403 | 0 | Weapons approved |
| ship/platform | 4,844 / 4,844 | **25,239** | reason: `orphan_weapon_id` |
| submarine | 732 / 732 | **3,218** | same |
| facility | 4,511 / 4,511 | **5,631** | same |
| aircraft | 7,387 (approve) | 0 (capped re-run artifacts) | mounts often unparsed |
| **Fitting total** | — | **~34,088** | triage repairable: **0** |

Sources: `scratch/nightly-cmo-20260618/*-summary.json`, `*-quarantine.json`, `mount-loadout-quarantine-triage-*.json`.

### 3.6 Shipped CLI: `catalog_intelligence_run`

Command:

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_intelligence_run --db <path>
```

| DB | `ok` | Exit | Key findings |
|----|------|------|--------------|
| Baltic seed | **true** | 0 | All 4 agents pass; `DIFF_CLEAN` |
| Proposed | **false** | 1 | `KILL_CHAIN_ORPHAN_EDGE` × **7,207**; `BASE_PD_OUTLIER` × 2,177; 3 pending staged batches |

This independently confirms SQL orphan counts and the “sensors as pseudo-platforms” model.

Unit tests (shipped entry path):

```text
dotnet test ... --filter CatalogIntelligenceRunCommandTests
Passed!  Failed: 0, Passed: 2
```

---

## 4. Parallel specialists dispatched

| Specialist | Mode | Returned |
|------------|------|----------|
| **Schema / provenance explore agent** | read-only subagent | SCHEMA-01..06, PROV-01..06, GATE-01..03, SNAP-01..02, CLEAN-01 + Req-06 checklist |
| **Gaming / domain fitness explore agent** | read-only subagent | GAME-01..02, KILLCHAIN-01..05, COVERAGE-01..03, UNIT-01..02, TL-01, QUAR-01, DOC-01 |
| **Orchestrator SQL + CLI** | implementer | Live `sqlite3` counts, orphan queries, `catalog_intelligence_run` on both DBs |
| **Firecrawl** | external scrape | Private-artifact capture |
| Skills aligned (guidance, not separate processes) | team-data / sqlite-schema-management / provenance-audit-modeling / military-database-research / simulation-accuracy-validation / database-branching-release-train / dispatching-parallel-agents | Used as review lenses |

---

## 5. Prioritized findings

### Critical

| ID | Finding | Evidence |
|----|---------|----------|
| **KILLCHAIN-01** | Sensors not bound to combat platforms in proposed corpus; importer slugs sensor titles as `platform_id` | 7,207 sensor orphans; `KILL_CHAIN_ORPHAN_EDGE` × 7207; specialist report |
| **KILLCHAIN-03** | Sim weapon envelopes ignore `weapon_catalog` (MVP hardcoded defaults) | Domain fitness agent; `TryGetWeaponEnvelope` path |
| **KILLCHAIN-04** | ~34k magazine/fitting quarantine (`orphan_weapon_id`); 0 repairable; `platform_magazine` live count **0** | quarantine JSON + SQL |

### High

| ID | Finding | Evidence |
|----|---------|----------|
| **KILLCHAIN-05** | Aircraft `**Weapons / Loadouts**` header not parsed | Domain fitness agent |
| **GAME-02** | Volume ≠ playable depth; 0 platforms with sensor+mount | SQL kill-chain matrix |
| **PROV-03** | Uneven provenance: weapons/loadouts/magazines lack tier/citation/confidence | Migration schema |
| **PROV-04** | JSON importer under-writes full sensor provenance set | Schema agent |
| **SCHEMA-03** | Sparse FKs; no `PRAGMA foreign_keys=ON` | Schema agent |
| **UNIT-02** | Default combat radius 1 nm on bulk platforms breaks reach sanity | Domain fitness |
| **COVERAGE-02** | Mobility/signature/EMCON mostly empty on bulk import | SQL table counts ≈ 0 |
| **QUAR-01** | Quarantine process works; backlog unrepairable under current envelope | triage JSON dry-run |

### Medium

| ID | Finding | Evidence |
|----|---------|----------|
| **GAME-01** | Baltic seed playable but catalog-thin (no mounts/weapons in seed) | counts: weapon/mount/loadout = 0 |
| **SCHEMA-01/02** | No `schema_migrations` ledger; version constant lags migration 011 | Schema agent |
| **UNIT-01** | 669 weapons with zero max range | SQL |
| **TL-01** | Nightly all TL-0 / sensors TRL-9; provisional mounts | approve JSON + SQL |
| **CLEAN-01** | Clean-room is process, not schema-enforced | Req 06 |

### Informational (positive)

| ID | Finding |
|----|---------|
| **PROV-01** | Sensor P0 provenance columns complete |
| **GATE-01** | Write-gate staging + change log solid |
| **SNAP-01** | Snapshot hash + `db_release` release train present |
| **INTEL-BALTIC** | `catalog_intelligence_run` **PASS** on Baltic |

---

## 6. Gaming / military-sim expert judgment

### Baltic MVP

- **Fit for:** policy-driven Baltic patrol scenarios, headless replay, vertical-slice detect/engage via scenario policy + 2 radars on `u1`.
- **Not fit for:** general OOB force generation, multi-domain ORBAT, catalog-driven kill chains.
- Engagement is **scenario-authored**, not catalog-authored (no seed weapons/mounts/loadouts).

### CMO-class ambition

A CMO-class DB needs closed graphs:

1. Platform → multi-sensor suite (typed: radar/sonar/ESM…)  
2. Platform → mounts → magazines → weapons with real ranges  
3. Sim reads those ranges for DLZ/engage  
4. Mobility/signature for transit and detectability  
5. Low quarantine / high FK integrity  

**Current proposed DB fails (1)–(3) at scale** despite having thousands of named entities. It is best classified as a **staging warehouse / pipeline stress test**, not a game database ready for open-world multi-domain scenarios.

### Clean-room / Req 06

- Importers route through write-gate for markdown path (good).  
- Citation/source_fact patterns exist for sensors (`cmano-db:sensor/N`).  
- No SQL ban on proprietary content; compliance is operational.  
- Dual-track community triage states not modeled (post-P0).

---

## 7. Recommended next actions (priority order)

1. **Bind sensors to real platforms** — parse platform `Sensors / EW` sections; stop treating sensor pages as platforms.  
2. **Wire `TryGetWeaponEnvelope` to `weapon_catalog`** (+ magazine quantities).  
3. **Stable weapon IDs** across ship weapon lines and weapon catalog (kill name-only matching).  
4. **Fix aircraft weapons header** (`**Weapons / Loadouts**`).  
5. **Enable FK validation** (app + optional `PRAGMA foreign_keys=ON`) for magazine→weapon/mount.  
6. **Extend provenance** to weapon/mount/loadout or shared provenance side-table.  
7. **Import mobility / non-placeholder combat radius** before bulk kill-chain validation.  
8. **Keep Baltic seed as production** until proposed graph repair is proven by `catalog_intelligence_run ok:true` with near-zero `KILL_CHAIN_ORPHAN_EDGE`.  
9. **Optional:** re-open Perplexity artifact with credentials to reconcile any external review notes with this local baseline.

---

## 8. Validation matrix (acceptance checklist)

| Criterion | Status |
|-----------|--------|
| Perplexity reviewed or private block captured | **Met** (private) |
| Baltic structural SQL + counts | **Met** |
| Proposed structural SQL + counts | **Met** |
| Orphan / kill-chain integrity queries | **Met** |
| `db_release` / `catalog_snapshot` / staging presence | **Met** |
| `catalog_intelligence_run` Baltic | **PASS** |
| `catalog_intelligence_run` proposed | **FAIL** (honest; 7207 orphan edges) |
| ≥2 specialist perspectives | **Met** (schema + gaming) |
| Written deliverable with verdict | **Met** (this file + in-repo copy) |
| Durable CLI unit test on Baltic path | **Met** (extended tests) |

---

## 9. Evidence index (`{SCRATCH}` = `/tmp/grok-goal-1eb4ab71042f/implementer`)

| Artifact | Purpose |
|----------|---------|
| `perplexity-source.md` | Private-artifact capture |
| `perplexity-screenshot.png` | Screenshot proof |
| `catalog-structure.log` | Full SQL inventory + integrity |
| `metrics.json` | Machine-readable counts |
| `catalog-intelligence-run.log` | Full CLI JSON for both DBs |
| `catalog-intelligence-summary.json` | Aggregated finding codes |
| `data-tests.log` | `dotnet test` CatalogIntelligenceRunCommandTests |
| `nightly-summaries.json` | Nightly approve summary dump |
| `game-database-review-validation-2026-07-09.md` | This report |

In-repo durable copy:

- `cmano-clone/docs/reports/game-database-review-validation-2026-07-09.md`

---

## 10. Closing statement

As a **data architect**: the schema and write-gate for Req 06 P0 are real and mostly right; multi-entity provenance and SQL-level referential integrity are the structural debt.

As a **gaming / mil-sim expert**: Baltic is a honest MVP fixture; the nightly proposed catalog is a **content warehouse with a broken kill chain**, not a CMO-class game database. Promote only after sensor binding, weapon ID resolution, magazine population, and sim envelope wiring produce a clean intelligence run on a realistic multi-domain sample set.

**Final verdict: CONCERNS** — ship Baltic; quarantine proposed for pipeline work only.
