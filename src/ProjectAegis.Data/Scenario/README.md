# Scenario Authoring & Packaging

`ProjectAegis.Data.Scenario` — the canonical, headless model for scenario files:
load/edit/save the JSON document, guard concurrent edits with an `editVersion`,
resolve the catalog snapshot a scenario binds to, and package the runtime manifest
(`policyId` + `dbSnapshotId` + `seed`) the simulation harness consumes.

**Requirement trace:** Req-11 (scenario editor) / Req-16 (unit readiness) / Req-09
(near-future tech gates); ADR-008 (`dbRef` resolution + deterministic edit model);
TR-editor-004 (optimistic concurrency). **Posture:** *pure data + deterministic
seam* — JSON in, JSON out, no engine dependency; scenario `seed` and `dbSnapshotId`
make harness runs reproducible.

## Intent

A scenario is edited headlessly (CLI/MCP), validated, then packaged for the sim.
This subsystem owns the three jobs between "a file on disk" and "a run":

1. **Author** — `ScenarioDocumentEditor` mutates missions with type-checked verbs
   and optimistic concurrency, then serializes deterministically.
2. **Resolve** — `ScenarioPackage` maps `dbRef`/`dbSnapshotId` metadata to a concrete
   catalog snapshot id (falling back to the Baltic default).
3. **Bind policy/paths** — the policy index and `ScenarioDataPaths` locate
   `data/scenarios/*.policy.json` and the repo-relative scenario directory.

## Architecture

```
data/scenarios/<id>.json  ──ScenarioDocumentJsonLoader──►  ScenarioDocumentDto
        │                                                        │
        │  ScenarioDocumentEditor.Load(path)                     │ ScenarioPackage.FromDocument
        │   RequireEditVersion → mutate → CommitMutation         ▼
        │   AddPatrol/Strike · Update* · TryRemove          ScenarioPackage
        │                                                   (ScenarioId, PolicyId,
        ▼  Save(path) → ScenarioDocumentJsonWriter           DbSnapshotId, DbRef,
data/scenarios/<id>.json                                     Seed, EditVersion)
                                                                  │
        ScenarioPolicyJsonIndex ──► *.policy.json (cached)        ▼
        ScenarioDataPaths       ──► repo-relative dirs       harness / sim run
```

| Type | Role |
|------|------|
| `ScenarioPackage` | Immutable runtime manifest. `FromDocument(id, dto)` derives `PolicyId` (default `baltic-patrol`), `DbSnapshotId` (`ResolveDbSnapshotId`), `Seed` (def 42), `EditVersion`. |
| `ScenarioPackageLoader` | `LoadFromFile` / `TryLoadFromFile` — JSON document → `ScenarioPackage` (scenario id = file name without extension). |
| `ScenarioDataPaths` | Walks up from `AppContext.BaseDirectory` (≤10 levels) to find `data/scenarios` and the repo root. The DATA-3 persistence seam. |
| `ScenarioPolicyJsonCatalog` / `Policy.ScenarioPolicyJsonIndex` | Built-in policy ids + lazily-loaded `*.policy.json` profiles (cached, ordinally sorted). Profiles are *resolved* in Sim. |
| `UnitReadinessMapFactory` | Flattens `metadata.UnitReadiness` → `IReadOnlyDictionary<string,bool>` for the harness/validation. |
| `Authoring.ScenarioDocumentDto` / `ScenarioMetadataDto` / `ScenarioMissionDto` / `ScenarioWaypointDto` / `ScenarioNearFutureUnitDto` | The serialized model (metadata + missions). |
| `Authoring.ScenarioDocumentEditor` | Mutable editor with type-checked mission verbs, `ComputeFileHash`, `RequireEditVersion`, `CommitMutation` (bumps `EditVersion`). |
| `Authoring.ScenarioEditVersionGuard` | `TryCheck(expected, current, hash)` → `ScenarioEditConflictException` (`Code = "CONFLICT"`) on mismatch (TR-editor-004). |
| `Authoring.ScenarioDocumentJsonLoader` / `ScenarioDocumentJsonWriter` | Canonical, deterministic JSON read/write. |

## Usage

```csharp
using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Authoring;

// Edit with optimistic concurrency.
var editor = ScenarioDocumentEditor.Load("data/scenarios/baltic.json");
editor.RequireEditVersion(expectedEditVersion: 3);     // throws ScenarioEditConflictException on mismatch
editor.AddStrikeMission("strike-1", ["mig29-1"], ["sa10-site"]);
editor.CommitMutation();                                // EditVersion 3 -> 4
editor.Save("data/scenarios/baltic.json");

// Package for the harness.
if (ScenarioPackageLoader.TryLoadFromFile("data/scenarios/baltic.json", out var pkg))
{
    Console.WriteLine($"{pkg!.ScenarioId} policy={pkg.PolicyId} snapshot={pkg.DbSnapshotId} seed={pkg.Seed}");
}

// Discover policies and the scenarios directory.
foreach (var id in ScenarioPolicyJsonCatalog.AllIds()) Console.WriteLine(id);
var dir = ScenarioDataPaths.TryResolveScenariosDirectory();   // null if not found
```

## CLI / operational runbook

Authoring is exposed headlessly through the mission editor verbs (each mutating verb
takes `--edit-version` and fails with `CONFLICT` on a stale version):

| Verb | Purpose |
|------|---------|
| `scenario_create` | Write a new canonical scenario (`--out`, `--db-ref`, `--policy-id`, `--seed`). |
| `mission_add_patrol` / `mission_add_strike` | Append a mission (`--edit-version --id [--unit U]+ [--wp lat,lon]+ / [--target T]+`). |
| `mission_update_patrol` / `mission_update_strike` / `mission_delete` | Edit/remove an existing mission. |
| `scenario_near_future_spawn` | Stage Req-09 near-future units (gated by `MaxTechnologyLevel`). |

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  scenario_create --out data/scenarios/demo.json --db-ref baltic_patrol --seed 42
```

Validate before export with `scenario_validate` (see `Validation/README.md`); full
verb table and PowerShell wrappers live in `tools/mission-editor/README.md`.

## Constraints & gotchas

- **`editVersion` is the concurrency token.** Mutating verbs must pass the version
  they read; a mismatch yields `ScenarioEditConflictException` (`Code = "CONFLICT"`,
  carries `CurrentEditVersion` + `FileHash`). Always `CommitMutation()` after a
  successful edit so the next caller sees the bumped version.
- **Snapshot resolution order.** `ResolveDbSnapshotId` prefers an explicit
  `dbSnapshotId`, then a resolvable `dbRef` (`CatalogValidationDefaults.TryResolveBalticDbRef`),
  then the Baltic default — it never returns empty, so packaging always binds *some*
  snapshot. Set `dbSnapshotId`/`dbRef` deliberately to avoid silently pinning Baltic.
- **Mission ids are case-insensitive and unique.** Add verbs throw on a duplicate id;
  update/remove match case-insensitively and type-check (`Update*` throws if the
  mission type differs).
- **Policy profiles are cached process-wide.** `ScenarioPolicyJsonIndex` loads
  `*.policy.json` once (lazily) from the resolved `data/scenarios` dir; call
  `LoadFromDirectory` explicitly to point at a different directory or force a reload.
- **Path resolution is best-effort.** `ScenarioDataPaths` returns `null` when
  `data/scenarios` isn't found within 10 parent levels (e.g. an unusual working dir);
  handle the null rather than assuming the repo layout.
- **Policy *behavior* lives in Sim.** This layer only catalogs/loads policy JSON;
  the actual ROE/profile resolution is `ProjectAegis.Sim` (DATA-3 seam — profiles
  not yet fully moved into Data).

## Tests

| Area | Test |
|------|------|
| Path seam resolves scenarios dir / repo root | `ProjectAegis.Data.Tests/Scenario/ScenarioDataPathsTests` |
| Editor mission verbs + serialization | `Scenario/ScenarioDocumentEditorTests` |
| Optimistic concurrency guard | `Scenario/ScenarioEditVersionGuardTests` |
| Package resolution from document | `Scenario/ScenarioPackageTests` |

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~Scenario" -v minimal
```
