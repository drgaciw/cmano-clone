# Catalog integrity & dependency reports — developer guide

The catalog ships a family of **read-only, deterministic analysis reports** a curator (or CI) can
run against a catalog database to answer "is this catalog internally consistent, and what depends on
what?" — without ever mutating it. They are the *inspection* counterpart to the write path in
[catalog-write-gate.md](catalog-write-gate.md): the write gate proposes/approves changes; these
reports read the committed (approved) catalog and emit a sorted, content-hashed picture for review.

Four MCP/CLI verbs make up the family:

| Verb | What it answers | Backing |
|------|-----------------|---------|
| `catalog_dependency_graph` | The full kill-chain wiring — every `platform→mount`, `platform→mount→weapon`, `platform→sensor`, and `platform→link` edge. | `ICatalogReader.GetSortedDependencyEdges()` + [`CatalogDependencyEdge`](../../src/ProjectAegis.Data/Catalog/CatalogDependencyEdge.cs) |
| `catalog_kill_chain_report` | Which of those edges are *impossible* (orphan refs, weapon out-ranging its sensor, speed/reach mismatches). | [`KillChainRules.Evaluate`](../../src/ProjectAegis.Data/Validation/KillChainRules.cs) (R1–R4, DBI-3.5: `KILL_CHAIN_ORPHAN_EDGE` / `KILL_CHAIN_RANGE_EXCEEDS_SENSOR` / `KILL_CHAIN_SPEED_MISMATCH` / `KILL_CHAIN_WEAPON_EXCEEDS_PLATFORM_REACH`) |
| `catalog_link_report` | The datalink catalog rows (`link_catalog`) with their type + nominal latency. | [`LinkCatalogReport`](../../src/ProjectAegis.Data/Catalog/LinkCatalogReport.cs) |
| `catalog_entity_map` | The entity→table→primary-key→order-by→runtime-DTO binding registry (req-06 P0 scope). | [`CatalogEntityMap`](../../src/ProjectAegis.Data/Catalog/CatalogEntityMap.cs) |

- **Source:** the CLI commands in
  [`src/ProjectAegis.MissionEditor.Cli/`](../../src/ProjectAegis.MissionEditor.Cli/)
  (`CatalogDependencyGraphCommand`, `CatalogKillChainReportCommand`, `CatalogLinkReportCommand`,
  `CatalogEntityMapCommand`), backed by `ProjectAegis.Data/Catalog/` + `ProjectAegis.Data/Validation/`.
- **Related:** the operational verb table lives in [mission-editor-cli.md](mission-editor-cli.md);
  how a headless run/test *gets* a catalog reader is in [catalog-seeding.md](catalog-seeding.md); the
  snapshot/release model these read against is in [catalog-release-train.md](catalog-release-train.md);
  the sensor/weapon envelopes the kill-chain rules compare come from the
  [detection](detection-pipeline.md) / [engagement](engagement-pipeline.md) layers. This page
  documents **what each report computes and its output contract** — the CLI doc only name-drops the
  verbs.

---

## Shared contract

All four verbs share the same design (DBI-4.5):

- **Read-only.** They open an `SqliteCatalogReader` (or the in-memory Baltic fixture) and never call
  `CatalogWriteGate`. Each command prints its own actor tag (e.g. `cli-kill-chain-report`).
- **DB resolution.** `--db <catalog.db>` when the file exists, else the resolved Baltic patrol DB
  (`CatalogReaderFactory.ResolveBalticPatrolDatabasePath()`); if neither exists the command throws
  `ArgumentException` (`catalog_entity_map` needs no DB — it is a static registry).
- **Deterministic + hashed.** Rows are sorted with **ordinal** comparers and (for the kill-chain and
  link reports) folded into a SHA-256 **content hash** over canonical `|`-delimited lines, so a clean
  Baltic catalog produces a stable hash suitable for CI goldens.
- **JSON output.** Indented, camelCase, `{ ok: true, verb: …, … }`; exit code `0`.

---

## 1. `catalog_dependency_graph`

Materializes the whole kill-chain graph via `GetSortedDependencyEdges()`. Each
[`CatalogDependencyEdge`](../../src/ProjectAegis.Data/Catalog/CatalogDependencyEdge.cs) is a record
where unused dimensions are empty strings and `Kind` is **derived** from which ids are populated:

| `CatalogDependencyEdgeKind` | Canonical line | Populated ids |
|-----------------------------|----------------|---------------|
| `PlatformToMount` | `mount:<platform>:<mount>` | `PlatformId`, `MountId` |
| `PlatformToMountToWeapon` | `weapon:<platform>:<mount>:<weapon>` | + `WeaponId` |
| `PlatformToSensor` | `sensor:<platform>:<sensor>` | `PlatformId`, `SensorId` |
| `PlatformToLink` | `link:<platform>:<link>:<commsFitting>` | `PlatformId`, `LinkId`, `CommsFittingId` (S36; synthetic from `platform_comms` + `link_catalog`) |

The payload carries `edgeCount`, the ordinal-sorted `canonicalLines`, `fullKillChainSurfaced: true`,
`chainTypes: ["mount","weapon","sensor","link"]`, and the structured `edges`. Only **approved** rows
are surfaced; the Baltic golden is stable.

## 2. `catalog_kill_chain_report`

Runs the bounded **detect-only** kill-chain impossibility rules (DBI-3.5, R1–R4) over the same
dependency edges and emits `DatabaseAgentFinding`s:

| Code | Rule |
|------|------|
| `KILL_CHAIN_ORPHAN_EDGE` | An edge references a missing platform / sensor / mount / weapon. |
| `KILL_CHAIN_RANGE_EXCEEDS_SENSOR` | A weapon's max range exceeds the platform's best approved sensor envelope (can't cue what it can't see). |
| `KILL_CHAIN_SPEED_MISMATCH` | Weapon/target speed regime mismatch. |
| `KILL_CHAIN_WEAPON_EXCEEDS_PLATFORM_REACH` | Weapon range exceeds the platform's effective reach. |

Findings are sorted and folded into `findingsHash` (`KillChainRules.ComputeFindingsHash` — SHA-256
over `Code|Severity|Message` lines). The payload reports `isEmpty`, `findingCount`, `findingsHash`,
the sorted `canonicalLines`, and the `findings`. This is **detect-only** — the report never blocks
anything. The *blocking* counterpart is `KillChainCommitGate`, which reuses `KillChainRules.Evaluate`
to reject a [`CatalogWriteGate`](catalog-write-gate.md) commit when the post-staging preview has
`error`-severity `KILL_CHAIN_*` findings.

## 3. `catalog_link_report`

Lists the datalink catalog. `reader.GetSortedLinks()` → `LinkCatalogReport.BuildCanonicalLines`
emits ordinal-sorted `LinkId|DisplayName|LinkType|LatencyMsNominal` lines and a `linksHash`. The
payload also includes the structured `links` (id, display name, type, nominal latency). Datalink
*integrity* (invalid type/latency, orphan comms) is a separate rule pack (`LinkCatalogRules`,
`LINK_TYPE_INVALID` / `LINK_LATENCY_INVALID` / `LINK_ORPHAN_COMMS`); this verb is the descriptive
row dump.

## 4. `catalog_entity_map`

The only DB-free verb. `CatalogEntityMap.All` is the static req-06 registry binding each catalog
entity to its `(EntityName, TableName, PrimaryKeyColumns, DeterministicOrderBy, RuntimeDto)`. It is
the single source of truth for **which SQLite table backs which runtime DTO and in what deterministic
order it is read** — the contract every reader/importer honours so catalog reads stay reproducible.
The verb prints the rows sorted by `EntityName`.

---

## Determinism & boundaries

- **Read-only / no write path.** None of these verbs touch `CatalogWriteGate`; they cannot mutate
  the catalog. Curator edits always go through propose → approve (see
  [catalog-write-gate.md](catalog-write-gate.md)).
- **Approved-only.** The dependency graph and kill-chain report surface only approved rows, so their
  output reflects the committed catalog, not staged proposals.
- **Ordinal + hashed.** Sorting and hashing are ordinal/UTF-8 SHA-256, so the Baltic fixture yields
  stable hashes; changes are reviewable diffs, and the Baltic replay hash is unaffected (this is
  catalog tooling, not a sim path).

## Pinned by tests

- CLI: `CatalogDependencyGraphCommandTests`, `CatalogKillChainReportCommandTests`,
  `CatalogLinkReportCommandTests`, `CatalogEntityMapCommandTests`
  ([`src/ProjectAegis.MissionEditor.Cli.Tests/`](../../src/ProjectAegis.MissionEditor.Cli.Tests/)).
- Backing: `DependencyGraphIndexTests`, `CatalogEntityMapTests`
  ([`src/ProjectAegis.Data.Tests/Catalog/`](../../src/ProjectAegis.Data.Tests/Catalog/)) and
  `KillChainRulePackTests`
  ([`src/ProjectAegis.Data.Tests/Validation/`](../../src/ProjectAegis.Data.Tests/Validation/)).

---

## Extending it

- **New dependency edge kind:** add it to `CatalogDependencyEdgeKind`, populate it in the reader's
  `GetSortedDependencyEdges()`, and give it a canonical-line format in
  `CatalogDependencyGraphCommand.FormatCanonicalLine` (keep the `kind:key:key` shape and ordinal
  sort).
- **New kill-chain rule:** add an `Evaluate*` step in `KillChainRules` with a `KILL_CHAIN_*` code;
  use `error` severity if it should also become a `KillChainCommitGate` blocker, and re-pin the
  golden hashes.
- **New entity/table:** add a row to `CatalogEntityMap.All` with a deterministic `OrderBy` (the
  reader and importers rely on it for reproducibility).
- Keep every report **read-only and ordinal-deterministic** — these are curator/CI inspection tools,
  not a write surface.
