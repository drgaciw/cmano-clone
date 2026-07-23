---
name: aegis-catalog-curator
description: >
  Master weekly curator workflow for Project Aegis catalog updates: fixture-first edits,
  catalog_import_markdown propose, quarantine review, catalog_write_approve, optional workbook.
  Use when curating Baltic/scenario catalog deltas, running nightly import review, or bridging
  Track A analysis notes into Track B fixtures. Human always approves production paths.
---

# Aegis Catalog Curator

Operational skill for the **Track B** curated catalog path in `cmano-clone`. Track A (local CMO `*.db3` analysis in `cmo_db_inspector`) never writes into this repo — see the firewall in [dual-track-cmo-analysis-and-catalog.md](../../docs/engineering/dual-track-cmo-analysis-and-catalog.md).

## When to use

- Weekly or ad-hoc catalog deltas for Baltic / scenario-relevant entities
- Reviewing `*-propose.json` / quarantine after `catalog_import_markdown` or nightly import
- Approving staged batches into scratch or production catalog DBs
- Optional follow-up: platform workbook tuning (`BasePd`, signatures)
- User asks to "curate catalog", "propose from fixture", or "approve catalog batch"

## Hard rules

- **Human approves production paths.** Agents propose and review; a human runs `catalog_write_approve` (or `cmo-nightly-approve.sh`) before live catalog rows change.
- **Fixture-first.** Prefer small edits under `tools/cmano-db-crawler/fixtures/` over full corpus re-import.
- **Never commit `*.db3`.** CMO game databases stay local; Aegis catalogs are `*.db` under `assets/data/catalog/`.
- **Never write SQLite directly.** All mutations flow through `IWriteGate` (ADR-006).
- **`import-qa-slice` approves immediately** — use only for QA fixture rebuilds, not untrusted corpus.

## Fixture locations

| Path | Use |
|------|-----|
| `tools/cmano-db-crawler/fixtures/` | Curated slices (Baltic waves, CI minis, multidomain QA) |
| `docs/reference/cmano-db/` | Full offline corpus (harvest/render; large) |

Markdown shape and entity types: [cmo-markdown-import.md](../../docs/engineering/cmo-markdown-import.md).

## Weekly workflow (Track B)

1. **Corpus refresh (only if public DB version moved):**
   ```bash
   cd tools/cmano-db-crawler
   node harvest.mjs all
   node render.mjs all
   cd ../..
   ```
2. **Prefer fixture-first propose** for intentional deltas; else nightly propose.
3. **Propose from fixture** (scratch DB — never write live tables directly):
   ```bash
   dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_import_markdown \
     --db scratch/dual-track-smoke/catalog-proposed.db \
     --markdown tools/cmano-db-crawler/fixtures/sensor-mini.md \
     --entity sensor \
     --report-out scratch/dual-track-smoke/sensor-propose.json
   ```
4. **Review** `*-propose.json`, quarantine entries, and `quarantinedCount` in the report. Fix fixture or import order (weapons before platforms when mounts reference weapons).
5. **Approve** each batch (human gate):
   ```bash
   dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_write_approve \
     --db scratch/dual-track-smoke/catalog-proposed.db \
     --batch <batchId-from-report>
   ```
6. **Optional:** workbook tweak via [platform-workbook-roundtrip.md](../../docs/engineering/platform-workbook-roundtrip.md).
7. **Optional smoke:** kill-chain / Baltic replay if scenario-impacting (see [aegis-catalog-quality-gate](../aegis-catalog-quality-gate/SKILL.md)).
8. **Commit** markdown + catalog artifacts only — never game DB files.

## Baltic platform id remap

For Baltic mini-fixtures only (not full corpus):

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_import_markdown \
  --db scratch/dual-track-smoke/catalog-proposed.db \
  --markdown tools/cmano-db-crawler/fixtures/baltic-platform-mini.md \
  --entity platform \
  --map-baltic-platform-ids
```

## Full corpus refresh (when cmano-db version moves)

```bash
./tools/cmo-nightly-import.sh [--entity …] [--max-records N] [--propose-only]
# review scratch/nightly-cmo-<YYYYMMDD>/*-propose.json and quarantine
./tools/cmo-nightly-approve.sh --entity <…> --run-date YYYYMMDD
```

## QA one-shot (propose + approve — use deliberately)

Rebuilds toward `assets/data/catalog/baltic_patrol.db` from multidomain fixtures:

```bash
dotnet run --project tools/cmano-db-crawler/import-qa-slice -- \
  [optional-db] [platform.md] [weapon.md] [sensor.md]
```

## Track A bridge (analysis → fixtures)

1. Spot-check ranges/RCS in `cmo_db_inspector` (local `.db3` only).
2. Confirm facts on public [cmano-db.com](https://cmano-db.com) pages.
3. Hand-edit fixtures under `tools/cmano-db-crawler/fixtures/` — never paste game-DB blobs.

## Subagent delegation

| Role | Skill |
|------|-------|
| Markdown authoring | [aegis-markdown-fixture-author](../aegis-markdown-fixture-author/SKILL.md) |
| Approve gate | [aegis-write-gate-approve](../aegis-write-gate-approve/SKILL.md) |
| Pre-merge checks | [aegis-catalog-quality-gate](../aegis-catalog-quality-gate/SKILL.md) |

Orchestration map: [aegis-catalog-sdlc-orchestration.md](../../docs/engineering/aegis-catalog-sdlc-orchestration.md).

## Further reading

- [dual-track-cmo-analysis-and-catalog.md](../../docs/engineering/dual-track-cmo-analysis-and-catalog.md)
- [cmo-markdown-import.md](../../docs/engineering/cmo-markdown-import.md)
- [catalog-write-gate.md](../../docs/engineering/catalog-write-gate.md)
- [ADR-013 CMO scenario import policy](../../docs/architecture/adr-013-cmo-scenario-import-policy.md)
