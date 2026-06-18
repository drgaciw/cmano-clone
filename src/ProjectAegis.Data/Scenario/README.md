# Scenario — packages, authoring & policy catalog

This subsystem owns the **scenario side of the data layer**: loading a runtime
scenario manifest, editing scenario documents with optimistic concurrency, and
indexing scenario policy JSON. It is the bridge that binds a scenario to an exact
catalog snapshot (for replay) and to a doctrine/ROE policy (for the sim).

The simulation consumes these artifacts; this layer only reads, edits, and resolves
them. Profile objects (the runtime policy types) are materialized in the Sim via
`ScenarioPolicyJsonLoader` — this layer provides the JSON catalog and DTOs (the
DATA-3 persistence seam).

## Layout

| Path | Responsibility |
|------|----------------|
| `ScenarioPackage` / `ScenarioPackageLoader` | Runtime manifest: scenario id + policy id + catalog snapshot binding. |
| `ScenarioDataPaths` | Resolves the repo-relative `data/scenarios` directory and repo root. |
| `UnitReadinessMapFactory` | Builds a `unitId → readyForLaunch` map from metadata (req 16). |
| `ScenarioPolicyJsonCatalog` / `Policy/` | Built-in + on-disk `*.policy.json` index and DTOs. |
| `Authoring/` | Mutable scenario document, JSON load/write, and edit-version concurrency (ADR-008). |

## Scenario package (`ScenarioPackage`)

A small immutable manifest read from a canonical scenario JSON document
(doc 06 / doc 11 P0):

| Property | Meaning |
|----------|---------|
| `ScenarioId` | File name (without extension) of the scenario document. |
| `PolicyId` | Doctrine/ROE policy id (defaults to `baltic-patrol`). |
| `DbSnapshotId` | Exact catalog snapshot the scenario binds to (for replay). |
| `DbRef` | Optional human-friendly catalog ref. |
| `Seed` | Scenario RNG root (default `42`). |
| `EditVersion` | Optimistic-concurrency version stamp. |

`ScenarioPackage.FromDocument` derives the package from `ScenarioMetadataDto`.
Snapshot resolution (`ResolveDbSnapshotId`) prefers an explicit `DbSnapshotId`, then
tries to resolve `DbRef` via `CatalogValidationDefaults.TryResolveBalticDbRef`, and
finally falls back to `CatalogValidationDefaults.BalticSnapshotId`.

`ScenarioPackageLoader.LoadFromFile(path)` / `TryLoadFromFile(path, out package)`
read the document and build the package; `TryLoadFromFile` returns `false` (and a
`null` package) when the file is missing.

## Authoring (`Authoring/`)

`ScenarioDocumentEditor` is the mutable, in-memory editing surface used by the
MCP mission-editor tools. It exposes mission CRUD plus **optimistic concurrency**:

- `Load(path)` / `CreateNew(...)` — open an existing document or start a fresh one
  (a new document starts at `EditVersion = 1`).
- Mission editing: `AddPatrolMission`, `AddStrikeMission`, `UpdatePatrolMission`,
  `UpdateStrikeMission`, `UpdateFerryMission`, `TryRemoveMission`. Duplicate ids and
  type mismatches throw `InvalidOperationException`.
- `RequireEditVersion(expected)` — guards a mutation against a stale client copy.
- `CommitMutation()` — increments `EditVersion` after a successful edit.
- `ComputeFileHash()` — SHA-256 over the canonically serialized document (lowercase hex).
- `Save(path)` — serialize via `ScenarioDocumentJsonWriter`.

`ScenarioEditVersionGuard.TryCheck(expected, current, fileHash?)` returns a
`ScenarioEditConflictException` (code `CONFLICT`, carrying `CurrentEditVersion` and
`FileHash`) when versions diverge, and `null` when they match — this is the
`TR-editor-004` / ADR-008 conflict contract surfaced by the MCP tools.

`ScenarioMetadataDto` carries `DbRef`, `DbSnapshotId`, `EditVersion`, `Seed` (default
`42`), `PolicyId`, per-unit `UnitReadiness` (req 16), `MaxTechnologyLevel` (default
`2`, req 09), and optional `NearFutureUnits`.

## Policy catalog (`Policy/`)

`ScenarioPolicyJsonCatalog` exposes the merged set of policy ids: a built-in list
(`baltic-patrol`, `baltic-patrol-opp-hold-fire`, `restricted-engagement`,
`test-sandbox-dual-side`) unioned with on-disk `*.policy.json` files. `AllIds()`
returns the union ordinally sorted; `TryGetJson(id)` returns the parsed DTO.

`ScenarioPolicyJsonIndex` does the disk work: `LoadFromDirectory(dir)` parses every
`*.policy.json` (case-insensitive property matching) and caches the result;
`EnsureDefaultJsonLoaded()` lazily loads from `ScenarioDataPaths.TryResolveScenariosDirectory()`.
A missing directory caches an empty map rather than throwing.

`ScenarioPolicyJsonDto` is the wire shape of a policy file — ROE
(`FriendlyRoe`/`OpposingRoe`), engagement (`Engage`), detection, EMCON, comms,
logistics, mission/doctrine overrides, delegation, replay cadence, and more. The
runtime profile is built from this DTO in the Sim.

## Path resolution (`ScenarioDataPaths`)

`TryResolveScenariosDirectory()` walks up to 10 parent directories from
`AppContext.BaseDirectory` looking for `data/scenarios`; `TryResolveRepoRoot()`
returns its parent. Both return `null` when not found (callers degrade gracefully).

## Determinism & invariants

- Policy ids and lookups are ordinally sorted/cased so enumeration is stable.
- Document hashing is over canonical serialization, so the same content hashes the
  same on any machine — the basis for the edit-version conflict check.
- Snapshot binding ties a scenario to an exact catalog content hash for replay (see
  [Snapshots](../Snapshots/README.md)).
- This layer reads/edits only; the simulation owns runtime materialization.

## Tests

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter Scenario -v minimal
```

`src/ProjectAegis.Data.Tests/Scenario/` covers package resolution
(`ScenarioPackageTests`), path resolution (`ScenarioDataPathsTests`), the document
editor (`ScenarioDocumentEditorTests`), and edit-version concurrency
(`ScenarioEditVersionGuardTests`).

## See also

- [ProjectAegis.Data overview](../README.md)
- [Snapshots — deterministic snapshots & release train](../Snapshots/README.md) (snapshot binding)
- [Validation — scenario & catalog validation](../Validation/README.md) (validates these documents)
- [MissionEditor.Cli verb reference](../../ProjectAegis.MissionEditor.Cli/README.md) (drives authoring)
- [ADR-008 — mission-editor validation engine](../../../docs/architecture/adr-008-mission-editor-validation-engine.md)
- [Requirement 06 — Database Intelligence](../../../Game-Requirements/requirements/06-Database-Intelligence.md)
