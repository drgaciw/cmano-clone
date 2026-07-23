---
name: aegis-markdown-fixture-author
description: >
  Author cmano-db markdown fixtures that CmoMarkdownImporter parses correctly: H2 country,
  H3 record, sub URL line, field tables, Weapons/Sensors bullets, slug rules, entity types.
  Use when creating or editing files under tools/cmano-db-crawler/fixtures/.
---

# Aegis Markdown Fixture Author

How to write markdown that [`CmoMarkdownImporter`](../../src/ProjectAegis.Data/Import/CmoMarkdownImporter.cs) parses into catalog row DTOs. Fixtures live under `tools/cmano-db-crawler/fixtures/`; full corpus exports live under `docs/reference/cmano-db/`.

Deep reference: [cmo-markdown-import.md](../../docs/engineering/cmo-markdown-import.md).

## When to use

- Creating or editing Baltic wave fixtures, CI minis, or multidomain QA slices
- Fixing parse/quarantine failures after `catalog_import_markdown`
- User asks to "add a sensor fixture", "author platform markdown", or "fix import parse"

## Canonical example

Study [`baltic-sweden-1990-sensors.md`](../../tools/cmano-db-crawler/fixtures/baltic-sweden-1990-sensors.md) — sensor rows with H3 titles, `<sub>` URLs, and field tables.

Platform + weapons example: [`baltic-platform-mini.md`](../../tools/cmano-db-crawler/fixtures/baltic-platform-mini.md).

## Record structure

```markdown
## Sweden                          ← H2 country/operator (nationality fallback)

### K 22 Gavle [ex-Goteborg Class] 1955   ← H3 = one record; heading → DisplayName
<sub>[/ship/9001/](https://cmano-db.com/ship/9001/)</sub>   ← URL → numeric id + segment

| Field | Value |                  ← per-record attribute table
|---|---|
| Type | Frigate |
| Nationality | Sweden |            ← optional; falls back to H2 country
| Max Speed | 28 kt |

**Weapons**                          ← weapon/mount/magazine parse (platform entities)
- RIM-66 Standard MR — Guided Weapon — Air Max: 74 km. Surface Max: 74 km.

**Sensors / EW**                     ← sensor bullets (sensor parse path)
- Fire Can [SON-9] - ... Radar Max Range: 37 km
```

## Extraction rules

- **Record boundary:** `### ` (H3) heading; flush on next H3, next `## ` country, or EOF.
- **Numeric id:** from `<sub>[/…/{id}/]</sub>` matching `/sensor/(\d+)/`, `/weapon/(\d+)/`, or `/(ship|aircraft|submarine|facility)/(\d+)/`.
- **`platform_id` slug:** text before first comma in H3, lowercased, non-word runs → `-`, max 64 chars (`SlugPlatformId`).
- **Slug collisions:** colliding slugs get `-{numericId}` appended for the colliding subset only.
- **Multi-clause weapon ranges:** all `… Max: N km` clauses on a line are scanned; **maximum** is kept.
- **Nationality:** per-record `| Nationality |` or enclosing `## Country` section.

## Entity types (`--entity`)

| `--entity` | Corpus file | Default domain | Parse path |
|------------|-------------|----------------|------------|
| `sensor` (default) | `sensor.md` | — | `ReadSensorBindings` |
| `weapon` | `weapon.md` | — | `ReadWeaponBindings` |
| `platform` | `ship.md` | `surface` | `ReadPlatformBindings` + mounts/loadouts/magazines |
| `aircraft` | `aircraft.md` | `air` | `ReadPlatformBindings` |
| `submarine` | `submarine.md` | `subsurface` | `ReadPlatformBindings` |
| `facility` | `facility.md` | `land` | `ReadPlatformBindings` |
| `ground-unit` | `ground-units.md` | `land` | `ReadPlatformBindings` (mobile facility subset) |

Platform entities derive **five row kinds:** platform, mount, loadout, magazine, quarantine. Unresolved weapon names → quarantine (`orphan_weapon_id`), not silent drop.

## Domain inference (`InferDomain`)

Order matters — hull-type override first (`carrier`/`cruiser`/`destroyer` → `surface`), then air keywords, subsurface, `water (surface)` facilities, land, else entity default domain.

## Baltic platform id remap

`--map-baltic-platform-ids` remaps known Baltic hulls to scenario ids (`u1`, `hostile-1`, `ucav-blue`, …).

**Scope:** Baltic mini-fixtures only — never on full corpus (would collapse unrelated hulls).

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_import_markdown \
  --db scratch/dual-track-smoke/catalog-proposed.db \
  --markdown tools/cmano-db-crawler/fixtures/baltic-platform-mini.md \
  --entity platform \
  --map-baltic-platform-ids
```

## Validate your fixture

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_import_markdown \
  --db scratch/fixture-check/catalog-proposed.db \
  --markdown tools/cmano-db-crawler/fixtures/<your-fixture>.md \
  --entity <sensor|weapon|platform|…> \
  --report-out scratch/fixture-check/propose.json
```

Check `parsedCount`, `quarantinedCount`, and `batches[]` in the JSON report. Zero quarantine before production approve.

## Common pitfalls

- Missing or malformed `<sub>[/segment/id/]</sub>` line → record skipped or wrong id.
- Weapons imported after platforms → mount/magazine quarantine; import weapons first.
- Using `--map-baltic-platform-ids` on non-Baltic fixtures → wrong scenario id collapse.
- Fabricated citation URLs (wrong segment for entity type) → provenance errors.

## Further reading

- [cmo-markdown-import.md](../../docs/engineering/cmo-markdown-import.md)
- [dual-track-cmo-analysis-and-catalog.md](../../docs/engineering/dual-track-cmo-analysis-and-catalog.md)
- Importer tests: `src/ProjectAegis.Data.Tests/Import/CmoMarkdown*.cs`
