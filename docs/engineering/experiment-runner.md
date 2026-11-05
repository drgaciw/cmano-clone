# Headless seed-grid experiment runner

DRG-257 supplies the seed-only infrastructure stub for [INF-6.1](../../Game-Requirements/requirements/07-Agentic-Infrastructure.md) and the [agentic infrastructure GDD](../../design/gdd/agentic-infrastructure.md). It runs the existing `BalticBatchRunner.Run` and exports `BalticBatchRunner.ExportCsv`; fingerprints come from the actual simulation order log.

Run from the repository root using SDK **8.0.400**:

```powershell
dotnet run --project tools/experiment-runner/ProjectAegis.ExperimentRunner.csproj -- --help
dotnet run --project tools/experiment-runner/ProjectAegis.ExperimentRunner.csproj -- data/experiments/baltic-patrol-seed-grid.json results.csv
```

The standalone CLI is deliberately outside `ProjectAegis.sln`. Build and verify it explicitly:

```powershell
dotnet build tools/experiment-runner/ProjectAegis.ExperimentRunner.csproj
dotnet test tools/experiment-runner/tests/ProjectAegis.ExperimentRunner.Tests.csproj -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter FullyQualifiedName~Experiments -v minimal
```

The CLI process tests invoke the built executable in a separate process and cover help, successful repeated runs, malformed/invalid definitions, unknown scenarios, missing inputs, and argument errors.

## Definition contract

The [JSON schema](../../data/experiments/experiment-definition.schema.json) describes the [sample](../../data/experiments/baltic-patrol-seed-grid.json):

```json
{
  "scenarioId": "baltic-patrol",
  "seeds": [7, 8, 7],
  "ticks": 4,
  "doctrineId": "baseline-label",
  "roeId": "scenario-default-label"
}
```

`scenarioId` is a nonblank existing scenario policy identifier. `seeds` is the nonempty seed grid of signed 32-bit integers, and `ticks` is the positive 32-bit tick count. Numeric values must use JSON integer tokens. Execution crosses this single scenario with the seeds array **in input order**; duplicates request repeated runs. The sample produces three rows in order `7, 8, 7`, with equal fingerprints for the repeated seed on the same build.

JSON Schema checks numerical integer values, so a schema validator can accept `1.0` or `1e0`. The parser additionally requires integer token syntax and rejects those spellings; use `1`. The schema cannot enforce this lexical restriction.

`doctrineId` and `roeId` are optional nonblank opaque strings. **They are metadata only: no runtime doctrine or ROE overrides are applied.** Actual policy comes from `scenarioId`. Changing either label does not change the run or its fingerprint. Labels are retained in definition roundtrips and printed as JSON on stderr, so commas, quotes, Unicode, and line breaks are escaped. They do not add columns to the stable score CSV. Omit absent labels; explicit `null` is invalid JSON input for these fields.

The parser rejects malformed JSON, missing required fields, empty seed grids, invalid numeric tokens, nonpositive ticks, blank labels, duplicate properties, and unknown fields with a field-specific error. Unknown scenarios fail through the batch runner without fallback. Successful execution returns exit code `0`; input/execution/output errors return `1`; invalid argument counts return `2`. Validation/execution completes before the output file is opened. Successful runs replace the requested CSV file; a missing output directory is an error.

## Output and scope

The existing CSV schema and escaping remain unchanged:

```text
scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
```

The runner uses the existing batch defaults (MVP engagement enabled and BLUE scoring). Same definition, seeds, scenario data, and build reproduce the same fingerprints. CSV formatting uses the existing invariant-culture exporter. There are no fake fingerprints, wall-clock fields in the simulation, hidden policy mutations, or new score calculations.

This story does not deliver the complete INF-6.1 parameter sweep system. Doctrine/ROE sweeps, force ratios, Latin-hypercube sampling, build-ID artifact manifests, statistics, cloud execution, and Monte Carlo UI remain future work. The schema intentionally rejects unsupported sweep fields so they cannot appear effective.

## Architecture and Unity plugin refresh

The immutable DTO, strict parser, and seed-grid runner live in the existing engine-free UnityAdapter assembly. The dependency edge is standalone CLI → UnityAdapter → existing Baltic batch runner. No Unity host, scene, prefab, asmdef, existing policy, golden, catalog write path, `DelegationBridge`, or simulation symbol is edited. Headless execution follows [ADR-010 §2–3](../architecture/adr-010-headless-first-command-driven-ui.md), [ADR-007](../architecture/adr-007-c2-map-presentation.md), and [ADR-001](../architecture/adr-001-sim-assembly-boundary.md). No UI or player-command path is introduced.

Because the adapter gains public APIs, refresh ignored Unity plugin assemblies when validating a checkout:

```powershell
# First verify that <repo>/.tmp-unity-plugin-publish resolves inside this exact checkout;
# the copy script recursively deletes that scratch directory.
.\tools\copy-delegation-assemblies.ps1
```

The script publishes `netstandard2.1` and copies compatible DLLs. Never commit `net8.0` plugin outputs. Architecture review uses [unity-csharp-architect](../../production/agentic/skills/unity-csharp-architect/SKILL.md) and its [finish checklist](../../production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md).
