# CMO markdown import — parse → propose → approve

Operational reference for the **CMO markdown import pipeline**: how the offline
cmano-db.com corpus exports under [`docs/reference/cmano-db/`](../reference/cmano-db/) are
parsed into catalog rows and staged through the write gate. The parser is
[`CmoMarkdownImporter`](../../src/ProjectAegis.Data/Import/CmoMarkdownImporter.cs); the
staging orchestrator is
[`CmoMarkdownImportProposer`](../../src/ProjectAegis.Data/Import/CmoMarkdownImportProposer.cs);
the CLI verb is
[`CatalogImportMarkdownCommand`](../../src/ProjectAegis.MissionEditor.Cli/CatalogImportMarkdownCommand.cs)
(`catalog_import_markdown`); the nightly runbook is the two-script
[`tools/cmo-nightly-import.sh`](../../tools/cmo-nightly-import.sh) →
[`tools/cmo-nightly-approve.sh`](../../tools/cmo-nightly-approve.sh) pair.

> **Where this sits in the data layer.** Importing is a *producer* of catalog change. It never
> writes SQLite directly — it parses markdown into row DTOs and stages them through
> `IWriteGate.Propose*Batch`. Approval (which UPSERTs the live rows and pins a snapshot) is a
> separate step documented in [`catalog-write-gate.md`](catalog-write-gate.md). Start with the
> [Data layer README](../../src/ProjectAegis.Data/README.md) for the surrounding map; this page
> is the deep-dive on the parse-and-propose stage.

---

## Pipeline at a glance

```text
docs/reference/cmano-db/<entity>.md            (offline cmano-db.com export; TB of reference)
        │  CmoMarkdownImporter.Read*Bindings(...)   ← pure parse, no I/O to SQLite
        ▼
  Row DTOs  (CatalogSensorBinding | CatalogWeaponRecord | CatalogPlatformBinding
             + derived CatalogMount / CatalogLoadout / CatalogMagazineEntry)
        │  CmoMarkdownImportProposer.Propose*FromMarkdown(...)
        │     • CatalogImportGate.PartitionForImport → quarantine bad rows
        │     • stable-sort, chunk (default 500), one batch per chunk
        ▼
  IWriteGate.Propose*Batch  ──► staging tables ("proposed")   [propose-only; nothing live yet]
        │  review the scratch DB / *-propose.json
        ▼
  catalog_write_approve  ──► validate → UPSERT live rows → audit log → snapshot hash
```

The import step is **propose-only by design**: it stages batches into a scratch DB and stops.
Nothing reaches the live catalog until a curator runs the approve step, so a bad corpus capture
can never silently corrupt catalog data.

---

## The seven entity categories

`--entity` selects a parse path. Entity aliases and the enum live in
[`CmoMarkdownImportEntity`](../../src/ProjectAegis.Data/Import/CmoMarkdownImportEntity.cs) and
`CatalogImportMarkdownCommand.ParseEntity`.

| `--entity` | Reference corpus (`docs/reference/cmano-db/`) | Approx. records | Default domain | Parse path (importer) |
|------------|----------------------------------------------|-----------------|----------------|-----------------------|
| `sensor` (default) | `sensor.md` | corpus-sized | — (sensor bindings) | `ReadSensorBindings` |
| `weapon` | `weapon.md` | 4403 | — (weapon records) | `ReadWeaponBindings` |
| `platform` | `ship.md` | 4844 | `surface` | `ReadPlatformBindings` (+ mounts/loadouts/magazines) |
| `aircraft` | `aircraft.md` | 7387 | `air` | `ReadPlatformBindings` |
| `submarine` | `submarine.md` | 732 | `subsurface` | `ReadPlatformBindings` |
| `facility` | `facility.md` | 4511 | `land` | `ReadPlatformBindings` |
| `ground-unit` | `ground-units.md` | 3289 | `land` | `ReadPlatformBindings` |

`ground-units.md` is a **derived view**, not an independently scraped corpus: it is the mobile
subset of `facility.md` (records whose `| Type |` is `Mobile Vehicle(s)`, `… - Wheeled`,
`… - Tracked`, or `Mobile Personnel`). `facility.md` remains the complete superset.

> **Known gap.** The import script accepts `--entity ground-unit`, but the *approve* companion
> ([`cmo-nightly-approve.sh`](../../tools/cmo-nightly-approve.sh)) does not yet list `ground-unit`
> in its entity switch (`sensor|weapon|platform|aircraft|submarine|facility|all`). Approve staged
> ground-unit batches with the generic verb —
> `catalog_write_approve --db <scratch.db> --batch <batchId>` — until the script is extended.

---

## The markdown format the parser expects

cmano-db exports are country-grouped markdown. The parser is intentionally line-oriented and
regex-driven — it does not use a full markdown AST.

```markdown
## Albania                          ← H2 country/operator section (nationality fallback)

### AAA Bty (100mm KS-19 x 6, ...) 1955   ← H3 = one record; heading text → DisplayName
<sub>[/facility/4519/](https://cmano-db.com/facility/4519/)</sub>   ← URL → numeric id + segment

| Field | Value |                  ← per-record attribute table
|---|---|
| Type | Mobile Vehicle(s) |         ← drives InferDomain
| Nationality | ... |                ← optional; falls back to the enclosing H2 country

**Sensors / EW**                     ← sensor bullets (sensor parse path)
- Fire Can [SON-9] - ... Radar Max Range: 37 km

**Weapons**                          ← weapon/mount/magazine parse
- 100mm KS-19 Frag — Gun — Air Max: 2.8 km. Surface Max: 7.4 km. Land Max: 7.4 km.
```

Key extraction rules (see the regexes at the top of `CmoMarkdownImporter`):

- **Record boundary** is the `### ` (H3) heading; a record is flushed when the next heading, the
  next `## ` country header, or EOF is reached.
- **Numeric id + URL segment** come from the `<sub>[/…/{id}/]</sub>` line matching
  `/(ship|aircraft|submarine|facility)/(\d+)/` (platforms) or `/sensor/(\d+)/` / `/weapon/(\d+)/`.
- **`platform_id`** is a slug of the display-name head (text before the first comma), lowercased,
  non-word runs collapsed to `-`, truncated to 64 chars (`SlugPlatformId`).
- **`--max-records N`** caps parsed records per entity (used by the CI smoke slices).

---

## Domain inference (`InferDomain`)

Platform `Domain` is derived from the `| Type |` string with an **ordered** rule set — order
matters because keywords overlap:

1. **Hull-type override first** — `carrier` / `cruiser` / `destroyer` → `surface` (so an
   *aircraft* carrier is not misread as air, and the `helicopter` keyword below cannot win).
2. **Air** — `aircraft`, `helicopter`, `fixed wing`, `fighter`, `bomber`, `uav`/`ucav`,
   `maritime patrol`, `elint`, `electronic warfare`, `anti-submarine` (matched here so it does
   **not** fall through to "submarine"), etc.
3. **Subsurface** — `submarine`, `ssk`/`ssn`/`ssbn`, `hunter-killer`, `uuv`/`rov`, `\bsdv\b`,
   `underwater`.
4. **`water (surface)` facilities** → `surface`.
5. **Land** — `facility`, or `\bland\b` (word-boundary so `Landing`, i.e. amphibious ships, stays
   in the corpus-default domain, not `land`).
6. Otherwise fall back to the entity's **default domain** (see the table above).

The per-entity default domain is supplied by the CLI (`CatalogImportMarkdownCommand`): aircraft →
`air`, submarine → `subsurface`, facility/ground-unit → `land`, everything else → `surface`.

---

## Correctness fixes worth knowing (why the parser looks the way it does)

These are the non-obvious behaviours the parser now guarantees. Each fixed a *silent* data-loss
or field-gap bug where the sim still ran but the catalog was wrong. Regression tests live in
[`CmoMarkdownPlatformImportTests`](../../src/ProjectAegis.Data.Tests/Import/CmoMarkdownPlatformImportTests.cs)
and the importer test suite.

| Fix | Symptom before | Behaviour now |
|-----|----------------|---------------|
| **Slug-collision disambiguation** | `platform_id` derives from display name, so different national operators of the same hull/airframe shared a slug. The write gate stages on `(batch_id, platform_id)` via INSERT-OR-REPLACE, so colliding records **silently dropped** each other. | Only the *colliding subset* gets the numeric id appended (`…-{numericId}`). Non-colliding and Baltic-mapped ids are left unchanged so existing mount/loadout/magazine references (which independently recompute the plain slug) keep matching. |
| **Multi-clause weapon range** | A single `**Weapons**` bullet can carry several domain range clauses (`Surface Max: 22.2 km. Land Max: 157.4 km.`). `Regex.Match` kept only the first, discarding a higher later range. | All `… Max: N km` clauses on the line are scanned; the **maximum** is kept. |
| **Country-section nationality** | Per-record tables rarely carry a `| Nationality |` row; records only sit under a `## <Country>` header. Nationality came out empty. | The enclosing `## ` country section is tracked and used as the nationality fallback. |
| **Per-entity default domain** | Domain fallback was hardcoded `surface`, so ~100% of facility records and ~63% of aircraft records defaulted to the wrong domain. | Fallback is the entity's natural domain (see `InferDomain` step 6). |
| **Real citation URL segment** | `CitationRef` / `SourceFactId` were hardcoded to `/ship/{id}/` for every entity, so aircraft/submarine/facility provenance pointed at fabricated ship URLs. | The actual matched path segment (`ship`/`aircraft`/`submarine`/`facility`) is used. |

Platform records import at `ReviewState = Provisional`; sensor bindings import as `Approved`
(TRL-9, `interpreted_value` provenance) — they still pass through the write gate's staging and
audit path.

---

## Derived catalog rows (platform entities)

For the platform-family entities the proposer parses **five** row kinds from the one markdown
file and stages each as its own batch, per chunk (`ProposePlatformsFromMarkdown`):

| Row kind | Source | Notes |
|----------|--------|-------|
| Platform | H3 record | `platform_id`, domain, class, nationality, provenance/citation |
| Mount | `**Weapons**` bullets | mount id = slug of weapon name; `MountType` from `InferMountType` (`gun`/`tube`/`vls`/`rail`) |
| Loadout | presence of `**Weapons**` | one `default` loadout per platform that has weapons |
| Magazine | weapon bullets resolved against the weapon lookup | qty 200 for guns else 16; **unresolved weapon name → quarantined** as `orphan_weapon_id`, not dropped silently |
| (quarantine) | — | fitting-quarantine entries are reported, never committed |

The weapon lookup for magazine resolution is built from the weapon corpus
(`BuildWeaponNameLookup` + `ResolveWeaponId`, longest-prefix match).

---

## CLI: `catalog_import_markdown`

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_import_markdown \
  --db <catalog.db> \
  --markdown <path.md> \
  [--entity sensor|weapon|platform|aircraft|submarine|facility|ground-unit] \
  [--map-baltic-platform-ids] \
  [--max-records N] \
  [--chunk-size 500] \
  [--report-out report.json]
```

| Flag | Default | Meaning |
|------|---------|---------|
| `--db` (required) | — | Target scratch/catalog DB. Auto-seeded with the Baltic default if it does not exist. |
| `--markdown` (required) | — | Path to the cmano-db export to parse. |
| `--entity` | `sensor` | Parse path (see the seven categories above). |
| `--map-baltic-platform-ids` | off | Remap known Baltic hulls to canonical scenario ids (`u1`, `hostile-1`, `ucav-blue`, …). Use only for the Baltic mini-fixtures, not the full corpus. |
| `--max-records N` | all | Cap parsed records (CI smoke slices). |
| `--chunk-size N` | 500 | Records per proposed batch. |
| `--report-out P` | — | Also write the JSON summary to a file. |

The verb prints a JSON summary and exits `0`. Key fields: `parsedCount`, `approvedCount`,
`quarantinedCount`, `batches[]` (with `batchId` + `recordCount`), and `nextStep`
(`catalog_write_approve …`). It never approves — that is a deliberate two-step split.

---

## Nightly runbook (off-CI, propose → approve)

The nightly corpus import is **not** wired into `dotnet test` CI — it operates on the full
multi-thousand-record corpora and stages into a dated scratch DB.

**1. Propose** (parse + stage; no live writes):

```bash
# All entities, full corpus:
./tools/cmo-nightly-import.sh
# Single entity, dry-run (validate paths only, no dotnet):
./tools/cmo-nightly-import.sh --entity aircraft --dry-run
# Small smoke slice against a fixture:
AIRCRAFT_MD=tools/cmano-db-crawler/fixtures/aircraft-slice-100.md \
  MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity aircraft --propose-only
```

Output lands in `scratch/nightly-cmo-<YYYYMMDD>/`: `catalog-proposed.db`,
`<entity>-propose.json` (batch ids), and `<entity>-quarantine.json`. Corpus/fixture paths are
overridable via `PLATFORM_MD`, `AIRCRAFT_MD`, `SUBMARINE_MD`, `FACILITY_MD`, `GROUND_UNIT_MD`.

**2. Approve** (curator commits the staged batches + pins snapshot hashes):

```bash
./tools/cmo-nightly-approve.sh --entity platform --run-date 20260716
./tools/cmo-nightly-approve.sh --entity aircraft --dry-run
```

It reads each `<entity>-propose.json`, calls `catalog_write_approve` per batch id, and writes
`nightly-approve-summary.json` (snapshot ids + `contentHashSha256` per batch). Pass
`--enable-balance-drift` to surface the balance-drift advisory in the summary.

---

## Common pitfalls

- **Import stages, it does not commit.** If catalog reads look unchanged after an import, you
  probably skipped the approve step. Check `ListPendingBatches()` / the scratch DB.
- **Don't use `--map-baltic-platform-ids` on the full corpus** — it is only for the curated
  Baltic mini-fixtures; on the corpus it would collapse unrelated hulls onto scenario ids.
- **Unresolved weapon references are quarantined, not dropped.** A non-zero `quarantinedCount` /
  `orphan_weapon_id` fitting entry means the weapon corpus was imported before/without the
  platform, or a name did not prefix-match — re-import weapons first.
- **`ground-unit` approve** uses the generic `catalog_write_approve` verb (see the Known gap).
- **Chunk size affects batch count, not row identity.** Rows are stable-sorted before chunking,
  so batch boundaries are deterministic for a given corpus + chunk size.

---

## See also

| Topic | Doc |
|-------|-----|
| Write-gate propose/approve internals, error-code catalog | [`catalog-write-gate.md`](catalog-write-gate.md) |
| Full Mission Editor CLI verb reference | [`mission-editor-cli.md`](mission-editor-cli.md) |
| Data-layer map (readers, snapshots, scenario binding) | [`ProjectAegis.Data/README.md`](../../src/ProjectAegis.Data/README.md) |
| Determinism rules (clock, ordering, hashing) | [`determinism-and-replay.md`](determinism-and-replay.md) |
| Reference corpora | [`docs/reference/cmano-db/`](../reference/cmano-db/) |
