# Dual-track: CMO `.db3` analysis + Aegis curated catalog

Operational firewall between **local CMO game-database analysis** and **Project Aegis catalog population**. Aegis is fed only through markdown / curated fixtures / workbook / OSINT via the write gate—never by migrating or mounting proprietary CMO `*.db3` files.

> **Sibling docs.** Markdown import deep-dive: [cmo-markdown-import.md](cmo-markdown-import.md). Write gate: [catalog-write-gate.md](catalog-write-gate.md). Policy: [ADR-013](../architecture/adr-013-cmo-scenario-import-policy.md). Seeding: [catalog-seeding.md](catalog-seeding.md).

---

## Decision (locked)

| Track | Repo | Data source | Role |
|-------|------|-------------|------|
| **A — Analysis** | External tool [`cmo_db_inspector`](https://github.com/yiyuezhuo/cmo_db_inspector) (local clone) | Licensed CMO install `*.db3` under the game `DB` folder | Explore sensors/aircraft/**ships**, radar equation, missile KP notes |
| **B — Simulation catalog** | This repo (`cmano-clone`) | cmano-db.com markdown + fixtures under `tools/cmano-db-crawler/fixtures/` | Seed Aegis SQLite via `IWriteGate` only |

### Catalog DB paths (Track B — do not conflate)

| Catalog | Path | Role |
|---------|------|------|
| **Baltic fixture / QA** | `assets/data/catalog/baltic_patrol.db` | ReplayGolden, gauntlet, CI fixture path — leave untouched by enterprise load |
| **Enterprise public corpus** | `assets/data/catalog/aegis_public_corpus.db` | Full cmano-db load (~65 MB, Git LFS) — promote only via curator scripts |

```text
Track A (local)                      Track B (this repo)
─────────────────                    ──────────────────────────────
Steam/Matrix *.db3 (read)            cmano-db markdown / fixtures
        │                                      │
        ▼                                      ▼
 cmo_db_inspector UI                   catalog_import_markdown
  (Ship/Aircraft/Sensor selectors)             │
        │                                      ▼
 emit_fixture_notes ──citations──►   IWriteGate propose → approve
 analyst notes only                          │
 (never commit .db3;                         ▼
  never write Aegis *.db)          baltic_patrol.db  |  aegis_public_corpus.db
```

### Forbidden

- Copying CMO `*.db3` into this repository or shipping them with the product
- Pointing `SqliteCatalogReader` / catalog tooling at a CMO game database
- Auto-ETL from proprietary CMO tables into live catalog tables (bypass write gate)
- Treating inspector physics output as a drop-in substitute for `basePd` / envelopes

Legal/policy anchors: [ADR-013](../architecture/adr-013-cmo-scenario-import-policy.md), Goal 5 in `Game-Requirements/requirements/01-Project-Overview.md`, `Game-Requirements/Data-Population-CMAODB.md`.

### Ownership

| Responsibility | Owner |
|----------------|-------|
| Open local `.db3`, radar equation, missile KP, plots | `cmo_db_inspector` |
| Harvest/render cmano-db → committed markdown | `tools/cmano-db-crawler` |
| Parse → stage → approve → snapshot | This repo write gate only ([ADR-006](../architecture/adr-006-data-layer-boundary.md)) |
| Human-tuned `BasePd` / `RcsBandDbsm` | Platform workbook ([platform-workbook-roundtrip.md](platform-workbook-roundtrip.md)) |
| Calibration notes from the physics lab | Human notes → hand-edit fixtures/workbook |

**Field overlap is conceptual only.** Inspector radar equation / RCS inform design judgment; Aegis collapses published range into `basePd` (`InferBasePd`) and weapon max ranges into meter envelopes. Signatures are workbook Phase B—not filled by markdown import.

---

## Track A — analysis only (outside this repo)

1. Use a supported Python (3.11 recommended) venv in the inspector clone; open the game `DB` folder:
   ```shell
   python -m cmo_db_inspector.start_app "D:\Program Files (x86)\Matrix Games\Command Modern Operations\DB"
   ```
2. Spot-check ranges, RCS, and radar-equation results in the Gradio UI (**Ship**, Aircraft, Sensor selectors).
3. Keep findings as **private notes**. Do not commit `.db3` files, raw SQLite dumps, or proprietary table extracts into `cmano-clone` or the inspector git tree.
4. When a catalog change is warranted, confirm the fact on a **public** [cmano-db.com](https://cmano-db.com) page (or other allowed source), then edit Track B fixtures/workbook—never paste game-DB blobs.

**Firewall:** the inspector **never** opens or writes `aegis_public_corpus.db` / `baltic_patrol.db`. Track A → Track B handoff is citation text only (notes / `emit_fixture_notes` → human paste into fixtures).

**Bridge (`emit_fixture_notes`):** from the inspector repo, emit a citation-required Aegis fixture template (no `.db3` reads; no Aegis DB writes):

```shell
python -m cmo_db_inspector.emit_fixture_notes \
  --entity sensor \
  --name "Pilot Mk.2" \
  --citation "https://cmano-db.com/sensor/83001/" \
  --field "Type=Radar" \
  --field "Range Max=88.9 km"
```

Missing importer-critical fields become `TODO` placeholders. Paste the markdown into `tools/cmano-db-crawler/fixtures/` (or hand-edit) only after confirming values on the public page.

---

## Track B — fixture-first curated update path

Prefer **small fixture edits** for Baltic / scenario-relevant deltas. Use the full corpus + nightly scripts when the public DB version moves.

### Fixture locations

| Path | Use |
|------|-----|
| [`tools/cmano-db-crawler/fixtures/`](../../tools/cmano-db-crawler/fixtures/) | Curated slices (CI minis, Baltic waves, multidomain QA) |
| [`docs/reference/cmano-db/`](../reference/cmano-db/) | Full offline corpus (harvest/render; large) |

Markdown shape (H2 country, H3 record, `<sub>[/sensor\|weapon\|…/{id}/]</sub>`, field table)—see [cmo-markdown-import.md](cmo-markdown-import.md).

### Fixture-first propose → approve (CLI)

From the repo root (requires .NET 8 SDK):

```bash
# 1) Propose from a mini fixture into a scratch DB (never write live tables directly)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_import_markdown \
  --db scratch/dual-track-smoke/catalog-proposed.db \
  --markdown tools/cmano-db-crawler/fixtures/sensor-mini.md \
  --entity sensor \
  --report-out scratch/dual-track-smoke/sensor-propose.json

# 2) Review report / quarantine in the JSON; list pending batches if needed
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_write_approve \
  --db scratch/dual-track-smoke/catalog-proposed.db \
  --batch <batchId-from-report>
```

Baltic mini-fixtures that remap scenario platform ids:

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_import_markdown \
  --db scratch/dual-track-smoke/catalog-proposed.db \
  --markdown tools/cmano-db-crawler/fixtures/baltic-platform-mini.md \
  --entity platform \
  --map-baltic-platform-ids
```

`--map-baltic-platform-ids` is for **Baltic mini-fixtures only**, not the full corpus.

### QA one-shot (propose + approve)

```bash
dotnet run --project tools/cmano-db-crawler/import-qa-slice -- [db] [platform.md] [weapon.md] [sensor.md]
```

Defaults rebuild toward `assets/data/catalog/baltic_patrol.db` using multidomain fixtures—use deliberately.

### Full corpus refresh (when public DB version moves)

```bash
cd tools/cmano-db-crawler
node harvest.mjs all    # → _raw/ (gitignored)
node render.mjs all     # → docs/reference/cmano-db/*.md
cd ../..
./tools/cmo-nightly-import.sh [--entity …] [--max-records N] [--propose-only]
# review scratch/nightly-cmo-<YYYYMMDD>/*-propose.json and quarantine
./tools/cmo-nightly-approve.sh --entity <…> --run-date YYYYMMDD
```

### Parallel Track B front doors

| Path | When |
|------|------|
| Fixtures + `catalog_import_markdown` | Intentional curated deltas (Baltic waves, CI minis) |
| **Enterprise public corpus** | Full cmano-db load → `aegis_public_corpus.db` ([enterprise-public-corpus-catalog.md](enterprise-public-corpus-catalog.md)) |
| Nightly import/approve | Full corpus propose after harvest (scratch → promote) |
| Platform workbook | Tune `BasePd`, signatures, mounts after import ([platform-workbook-roundtrip.md](platform-workbook-roundtrip.md)) |
| OSINT propose | Speculative / near-future sensors (separate provenance) |

---

## Weekly curator checklist

**Track A (any day):** open local `.db3` in the inspector; spot-check ranges/RCS; keep calibration notes private.

**Track B (weekly):**

1. Refresh markdown corpus only if cmano-db / public DB version moved (`harvest.mjs` / `render.mjs`).
2. Prefer fixture-first deltas for Baltic/scenario entities; else nightly propose.
3. Review quarantine / `*-propose.json`.
4. Approve batches (`cmo-nightly-approve.sh` or `catalog_write_approve`).
5. Optional: workbook tweak `BasePd` / `RcsBandDbsm`; optional OSINT staging review.
6. Smoke kill-chain / Baltic fixture if scenario-impacting.
7. Commit **markdown + catalog artifacts only**—never game DB files (`*.db3`).

---

## Policy guard: no proprietary `*.db3` in this repo

- Root [`.gitignore`](../../.gitignore) ignores `*.db3` (CMO product databases).
- Committed Aegis catalogs remain `*.db` under `assets/data/catalog/` (different extension; write-gate owned).
- PR checklist: reject any added `*.db3`, CMO Steam/Matrix path dumps, or “temporary” game-DB copies under `scratch/` that escape gitignore.
- Reviewers: if a PR adds a SQLite reader against CMO table names (`DataAircraft`, `DataSensor`, …), treat as policy violation unless Legal has explicitly approved a clean-room exception (none today).
- **CI/local gate:** [`scripts/verify-catalog-import.ps1`](../../scripts/verify-catalog-import.ps1) runs early in [`tools/verify-ci-local.ps1`](../../tools/verify-ci-local.ps1) and bash parity [`tools/buildkite/dotnet-ci.sh`](../../tools/buildkite/dotnet-ci.sh) (after restore, before Release build). Fails on any `git ls-files '*.db3'` hit and on CmoMarkdown import test regressions. Buildkite’s blocking `:hammer:` step uses the same bash gate via `agent-dotnet-ci.sh`—no separate catalog-only pipeline step.

---

## Verification smoke

```bash
# Aegis — fixture propose (scratch)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_import_markdown \
  --db scratch/dual-track-smoke/catalog-proposed.db \
  --markdown tools/cmano-db-crawler/fixtures/sensor-mini.md \
  --entity sensor \
  --report-out scratch/dual-track-smoke/sensor-propose.json

# Inspector — local DB only (outside this repo)
# python -m cmo_db_inspector.start_app "<CMO\DB>"
```

Confirm: no `*.db3` under the Aegis working tree; inspector never writes into `assets/data/catalog/` (neither Baltic nor enterprise).

---

## Worked example (Baltic Sweden 1990 wave)

Live fixture-first curator cycle using the Baltic Sweden 1990 wave fixtures. Scratch DB only—no production catalog writes.

### Setup

```powershell
New-Item -ItemType Directory -Force -Path scratch/dual-track-weekly
```

### 1) Propose weapons (mounts may reference these)

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_import_markdown \
  --db scratch/dual-track-weekly/catalog-proposed.db \
  --markdown tools/cmano-db-crawler/fixtures/baltic-sweden-1990-weapons.md \
  --entity weapon \
  --report-out scratch/dual-track-weekly/weapons-propose.json
```

**Report (`weapons-propose.json`):**

| Field | Value |
|-------|-------|
| `parsedCount` | 13 |
| `quarantinedCount` | 0 |
| `batchId` | `batch-weapon-13-0` |

### 2) Propose sensors

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_import_markdown \
  --db scratch/dual-track-weekly/catalog-proposed.db \
  --markdown tools/cmano-db-crawler/fixtures/baltic-sweden-1990-sensors.md \
  --entity sensor \
  --report-out scratch/dual-track-weekly/sensors-propose.json
```

**Report (`sensors-propose.json`):**

| Field | Value |
|-------|-------|
| `parsedCount` | 42 |
| `quarantinedCount` | 0 |
| `batchId` | `batch-42-0` |

### 3) Approve batches (scratch DB only)

Both batches had `quarantinedCount: 0`, so approve proceeded on the scratch DB:

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_write_approve \
  --db scratch/dual-track-weekly/catalog-proposed.db \
  --batch batch-weapon-13-0

dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_write_approve \
  --db scratch/dual-track-weekly/catalog-proposed.db \
  --batch batch-42-0
```

**Approve snapshot output:**

| Batch | `releaseVersion` | `snapshotId` | `sensorRowCount` | `contentHashSha256` (prefix) |
|-------|------------------|--------------|------------------|------------------------------|
| `batch-weapon-13-0` | `catalog-approve-batch-weapon-13-0` | `baltic_patrol` | 2 | `ad3c8e20…` |
| `batch-42-0` | `catalog-approve-batch-42-0` | `baltic_patrol` | 44 | `6f36b519…` |

> **Note:** `sensorRowCount` reflects cumulative sensor rows in the scratch catalog after each approve—not weapon row counts for the weapons batch.

### 4) Verify

```powershell
./scripts/verify-catalog-import.ps1
```

### 5) Platform wave (weapons + mounts + magazines together)

Import **platforms after weapons** using the gated CLI only if you will approve platform rows first, then run mount/loadout triage. A naive `--entity platform` propose on top of an already-approved weapon slice yields `quarantinedCount: 21` (`out_of_envelope` mounts) and `catalog_write_approve --batch batch-platform-*` fails with `KILL_CHAIN_ORPHAN_EDGE` until magazine/loadout FKs exist.

For Baltic Sweden 1990 fixtures, prefer the one-shot QA slice (weapons + platforms + sensors, auto-approve on scratch):

```bash
dotnet run --project tools/cmano-db-crawler/import-qa-slice -- \
  scratch/dual-track-weekly/catalog-platforms-wave.db \
  tools/cmano-db-crawler/fixtures/baltic-sweden-1990-platforms.md \
  tools/cmano-db-crawler/fixtures/baltic-sweden-1990-weapons.md \
  tools/cmano-db-crawler/fixtures/baltic-sweden-1990-sensors.md
```

**Report (`platforms-qa-slice.json`):**

| Field | Value |
|-------|-------|
| `platformPropose.PlatformCount` | 8 |
| `platformPropose.MountCount` | 21 |
| `platformPropose.MagazineCount` | 21 |
| `platformPropose.FittingQuarantinedCount` | 0 |
| `sensorPropose.parsedCount` | 42 |
| `approvedCount` | 7 |
| `failedCount` | 0 |

> **Curator note:** `import-qa-slice` commits immediately — use only on scratch DBs. Production promotion still requires explicit human `catalog_write_approve` on the gated path.

Artifacts written under `scratch/dual-track-weekly/`:

- `catalog-proposed.db` — scratch catalog (weapons + sensors approved via gated path)
- `catalog-platforms-wave.db` — scratch catalog (full Sweden 1990 wave via import-qa-slice)
- `weapons-propose.json`, `sensors-propose.json`, `platforms-propose.json` — gated propose reports
- `platforms-qa-slice.json` — import-qa-slice report (platform wave)
