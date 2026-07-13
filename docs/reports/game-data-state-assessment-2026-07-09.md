# Game Data State Assessment — `baltic_patrol` Catalog

**Prepared by:** Milsim Data Architect (orchestrator)
**Date:** 2026-07-09
**Scope:** Content state of the game database `assets/data/catalog/baltic_patrol.db`, its migration chain, JSON source catalogs, and scenario references.
**Verdict:** 🟠 **Schema-mature, content-empty.** The data *architecture* is production-grade; the data *content* is a P0 smoke slice. The DB is fit for pipeline and integration testing, not for gameplay.

---

## 1. Executive summary

The catalog database is a well-engineered, heavily-governed SQLite store (24 tables, 11 forward-only migrations, a 182-file C# data layer with staging, quarantine, provenance, and release-train machinery). Against that, the **actual seeded content is three placeholder platforms and two sensors.** Twenty of twenty-four tables are empty, including every table that drives combat resolution (weapons, mounts, loadouts, magazines, signatures, EMCON, mobility, comms, links).

In short: we have built an aircraft carrier's worth of data-governance infrastructure and loaded it with a rowboat. That is a defensible state for this phase — the seed exists to exercise the import→stage→validate→release pipeline — but it must not be mistaken for a content baseline. Three integrity gaps (empty provenance hash, inert change-log/audit trail, and schema-version drift) should be closed before content volume grows, because they get exponentially more expensive to retrofit later.

---

## 2. Database inventory

`baltic_patrol.db` — 236 KB, SQLite, 24 tables, `PRAGMA user_version = 0`.

### Populated tables (4 of 24)

| Table | Rows | Notes |
|---|---|---|
| `platform` | 3 | `u1`, `hostile-1`, `hostile-far` — positional only |
| `sensor` | 2 | both on `u1` (`radar-1` base_pd 1.0, `radar-2` base_pd 0.75), review_state `approved`, TRL 9 |
| `catalog_snapshot` | 1 | `baltic_patrol`, branch `TL-0`, **content hash empty** |
| `db_release` | 1 | `catalog-p0-2026-06-04`, schema_version `005` |

### Empty tables (20 of 24)

- **Combat model (10):** `weapon_catalog`, `link_catalog`, `platform_loadout`, `platform_magazine`, `platform_mount`, `platform_comms`, `platform_damage`, `platform_emcon`, `platform_mobility`, `platform_signature`
- **Governance (2):** `catalog_change_log`, `sensor_quarantine`
- **Staging (8):** `catalog_staging_batch`, `_sensor`, `_damage`, `_emcon`, `_link`, `_mobility`, `_signature`

Empty staging tables are expected (staging is transient). Empty **combat-model** tables are the material finding: the current DB cannot resolve a weapon engagement because there are no weapons, mounts, magazines, or target signatures to resolve against.

---

## 3. Data-quality findings

**F-1 — Platforms are positional stubs, not catalog entities (Medium).**
All three platform rows have empty `display_name`, `platform_class`, and `nationality`, and `game_technology_level = 0`. The migration-007/008 platform-editor columns exist but are unpopulated. These entities carry a lat/lon and a combat radius and nothing else — enough to place a contact on a map, not enough to describe a warship.

**F-2 — Provenance hash never populated (High).**
Migration `006_snapshot_content_hash` added `catalog_snapshot.content_hash_sha256` specifically for release-train binding, but the value is an empty string. The release-integrity mechanism is installed and switched off. Any "did this snapshot change?" check is currently trusting an empty hash.

**F-3 — Audit trail is inert (High).**
`catalog_change_log` is empty and every `*_utc_ticks` timestamp (`db_release.created_utc_ticks`, sensor `revised_utc_ticks`) is `0`. There is no temporal or authorship record for the data that *is* present. Migration 005 built the provenance-audit staging; nothing writes to it.

**F-4 — Schema-version drift (Medium).**
`db_release.schema_version = '005'`, but the migration chain runs through `011` and the physical schema reflects 011 (the `catalog_staging_link` table from 011 is present). Meanwhile `PRAGMA user_version = 0`, so the file itself carries no migration watermark. Three different places disagree about "what version is this DB." This is a release-train hazard.

**F-5 — Reviewer metadata blank on approved rows (Low).**
Both sensors are `review_state = approved` with empty `reviewer_id` and `citation_ref`. Approval without an approver undermines the review gate's audit value.

---

## 4. Source-of-truth drift (JSON vs. SQLite)

Four JSON catalogs live in `data/catalog/` and are **not** reflected in the DB:

| JSON source | Content | In DB? |
|---|---|---|
| `sensors_baltic.json` | radar-1, radar-2 on u1 | ✅ imported |
| `near_future_archetypes.json` | 4 archetypes (replicator, CCA wingman, swarm, hypersonic) | ❌ not loaded |
| `speculative_platforms.json` | 2 black-project/DEW platforms | ❌ not loaded |
| `sensor_quarantine_sample.json` | u9 provisional sensor | ❌ not loaded (by design — quarantine sample) |

Only the sensor import made it into SQLite. The near-future and speculative catalogs are defined in JSON and consumed at runtime (via `NearFutureArchetypeCatalog` / gating), so this is *architecturally* intentional — but it means there are two catalog surfaces (SQLite for the core loop, JSON for gated/speculative content) with no single reconciliation point. `CatalogJsonImporter` exists; it simply hasn't been pointed at these files. This is the classic dual-source-of-truth risk and worth an explicit decision, not drift by omission.

---

## 5. Referential integrity

Within the loaded slice, integrity is **clean**: no orphan sensors (both reference `u1`, which exists); scenario policy files reference `u1` (80×) and `hostile-1` (75×), both present. The example scenario introduces `patrol-1`, which is a scenario-local unit id rather than a catalog platform — expected, but a naming convention worth pinning down so scenario ids and catalog ids never collide silently. The quarantine sample's `u9` correctly does not appear in the live platform table.

No foreign-key violations were found. The good news: the schema's CHECK/PK/FK constraints are doing their job on the data that exists.

---

## 6. Risk assessment

| # | Risk | Sev | Rationale |
|---|---|---|---|
| R-1 | Combat model has no data | High | Weapons/mounts/loadouts/magazines/signatures/EMCON all empty — no engagement can be resolved from catalog |
| R-2 | Release integrity trusting empty hash (F-2) | High | Snapshot binding is unverifiable; regressions can ship undetected |
| R-3 | No audit trail as content scales (F-3) | High | Retrofitting history onto existing rows later is lossy and expensive |
| R-4 | Version drift across DB/release/pragma (F-4) | Med | Release-train and migration idempotency depend on an agreed version watermark |
| R-5 | Dual source of truth (JSON vs SQLite) | Med | Reconciliation gap will widen as speculative content grows |
| R-6 | "Approved" without approver (F-5) | Low | Governance theater unless review metadata is enforced |

---

## 7. Recommendations

**Now (before more content lands):**
1. Populate `content_hash_sha256` on snapshot write and fail the release gate on empty/mismatched hash (closes R-2). The mechanism is already built by migration 006 — wire it, don't rebuild it.
2. Backfill and enforce `catalog_change_log` + real UTC timestamps so every future insert is attributable (closes R-3).
3. Reconcile the version watermark: set `PRAGMA user_version`, bump `db_release.schema_version` to the true applied migration, and add a startup assertion that the three agree (closes R-4).

**Next (content baseline):**
4. Define a minimum-viable-combat content target for the Baltic slice — at least one weapon, one mount, one loadout, one magazine, and a target signature per hostile — so the engagement pipeline can be exercised end-to-end against real catalog data (attacks R-1).
5. Decide and document the JSON-vs-SQLite boundary: either import near-future/speculative archetypes via `CatalogJsonImporter` into a gated snapshot branch, or formally declare JSON the runtime source and add a drift check (attacks R-5).
6. Enforce reviewer_id/citation_ref as non-empty in the review→approve transition (closes R-6).

**Parallel-development note (per project charter):** the three "Now" fixes touch governance columns and the release gate, not the combat-model tables, so they can proceed on a separate branch without blocking content authors filling `weapon_catalog`/`platform_*`. Content seeding (rec 4) and governance hardening (recs 1–3) are cleanly separable workstreams.

---

## 8. Method & provenance

Read-only inspection on branch `ci/buildkite-pipeline-optimization`. Row counts and field completeness pulled directly from `baltic_patrol.db` via SQLite; migration intent read from `assets/data/catalog/migrations/00*.sql`; source drift compared against `data/catalog/*.json`; references cross-checked against `data/scenarios/*.json`. No data was modified. DB last committed 2026-06-19 (`8de98b1`); migration chain has since advanced to 011, consistent with the version-drift finding (F-4).
