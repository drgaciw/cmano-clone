# ProjectAegis.Delegation.Demo

Console entry point over the **Baltic replay harness**
([`BalticReplayHarness`](../ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs)).
It runs one or more scenarios headless and prints the exact fields that
[`tests/regression/replay-golden-*.txt`](../../tests/regression/) pins — making it the tool
you use to **regenerate replay goldens** and to spot-check determinism from the shell.

> This is a thin CLI, not a service. It has no state and starts no server; each run is a fresh
> deterministic simulation of `(scenario, seed, ticks)`. For the invariants behind the output,
> see the [determinism & replay developer guide](../../docs/engineering/determinism-and-replay.md).

---

## Usage

```bash
# Single run (defaults: --seed 42 --scenario baltic-patrol --ticks 4, engagement ON)
dotnet run --project src/ProjectAegis.Delegation.Demo -- [--seed N] [--scenario ID] [--ticks N] [--no-engage] [--csv]

# Batch run (multiple scenarios × seeds)
dotnet run --project src/ProjectAegis.Delegation.Demo -- --batch [--scenarios a,b] [--seeds 42,7] [--ticks N] [--csv-out path.csv]

# Batch across every policy under data/scenarios
dotnet run --project src/ProjectAegis.Delegation.Demo -- --batch --all-scenarios

# Experiment seed-grid batch (definition file drives scenario, seeds, ticks, and sweeps)
dotnet run --project src/ProjectAegis.Delegation.Demo -- --batch --experiment path.experiment.json [--csv-out path.csv]

# Help
dotnet run --project src/ProjectAegis.Delegation.Demo -- --help
```

### Flags

| Flag | Applies to | Meaning (default) |
|------|-----------|-------------------|
| `--seed N` | single | Scenario seed (`42`). |
| `--scenario ID` | single | Scenario policy id (`baltic-patrol`). |
| `--ticks N` | both | Number of ticks to run (`4`). |
| `--no-engage` | both | Disable `MvpEngagement` (detection/decisions only). |
| `--csv` | single | Also print the scoring CSV row for the run. |
| `--batch` | — | Switch to batch mode. |
| `--scenarios a,b` | batch | Comma-separated scenario ids (`baltic-patrol,baltic-patrol-comms,baltic-patrol-classify`). |
| `--seeds 42,7` | batch | Comma-separated seeds (`42`). |
| `--all-scenarios` | batch | Discover every policy under `data/scenarios` instead of `--scenarios`. |
| `--csv-out path.csv` | batch, experiment | Write the batch CSV to a file (otherwise printed to stdout). |
| `--experiment path` | experiment | Run the seed-grid batch defined by an experiment JSON file. Requires `--batch`. |

`--experiment` reads scenario, seeds, ticks, and sweeps from its file, so it rejects being combined
with `--scenarios`, `--seeds`, `--all-scenarios`, `--ticks`, `--no-engage`, `--seed`, `--scenario`, or
`--csv`. See [Experiment batch](#experiment-batch---batch---experiment) below.

Exit codes: `0` success, `1` harness exception or an unreadable/invalid experiment definition,
`2` unknown or misused argument (including `--experiment` without `--batch`).

---

## Single-run output contract

Each single run prints these lines (field ↔ golden key is 1:1):

```text
SEED=42 SCENARIO=baltic-patrol TICKS=4 ENGAGEMENTS=<n>
FINGERPRINT=<canonical order-log text>
FINGERPRINT_SHA256=<sha256 hex of the fingerprint>
DETECTION_WORLD_HASH=<ulong>
WORLD_HASH=<ulong>
REPLAY_CHECKPOINT=<simTick>:<worldHash>:<lastSequenceId>    # one per checkpoint
MESSAGE=<CATEGORY>|<text>                                   # KILL_CONFIRMED/INTERCEPT_SUCCESS/HIT/MISS/COMMS
```

- `WORLD_HASH`, `DETECTION_WORLD_HASH`, `FINGERPRINT_SHA256` are the three values copied into
  the matching `replay-golden-*.txt`.
- `REPLAY_CHECKPOINT` lines expose the per-checkpoint world hash + last order-log
  `SequenceId`, useful for bisecting *where* two runs diverge.
- `MESSAGE` lines are a filtered view of harness messages (combat + comms categories only).

---

## Experiment batch (`--batch --experiment`)

`--batch --experiment <file>` runs a **seed grid** defined by a JSON file (DRG-257 / INF-6.1) and prints
one CSV row per grid point. Each point still calls [`BalticReplayHarness.Run`](../ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs);
the path does not modify the harness, `BalticBatchRunner`, `SimulationSession`, or `DelegationBridge`.

The file is validated by
[`ExperimentDefinitionLoader`](ExperimentDefinitionLoader.cs) against
[`data/experiments/experiment-definition.schema.json`](../../data/experiments/experiment-definition.schema.json)
(the loader re-implements the schema checks and rejects any unknown property):

| Field | Required | Meaning (default) |
|-------|----------|-------------------|
| `scenarioId` | yes | Scenario policy id. Must be a policy discoverable under `data/scenarios`. |
| `ticks` | yes | Tick count for every grid point (integer ≥ 1). |
| `seeds` | yes | Seed grid — a non-empty array of unique integers, executed in order. |
| `side` | no | Side the `score`/`kills`/… columns tally (`BLUE`). |
| `mvpEngagement` | no | Passed to `BalticReplayHarness.Run` (`true`). |
| `doctrineId`, `roeId`, `forceRatioId` | no | Opaque scalar ids recorded on each row when that axis is not swept. |
| `grid.method` | with `grid` | `cartesian` (full product of active axes) or `latin-hypercube` (deterministic discrete sample sized to the longest active axis). |
| `grid.parameters` | no | Which axes vary: any of `seed`, `doctrineId`, `roeId`, `forceRatioId`. Omit to vary every axis that has sweep values. `seed` is always executed in full. |
| `sweeps.doctrineIds`, `sweeps.roeIds`, `sweeps.forceRatioIds` | no | Discrete levels for the opaque axes. An axis with no levels falls back to its scalar id. |
| `schemaVersion` | no | Must be `1` when present. |

**Opaque ids are recorded, not applied.** `doctrineId`, `roeId`, and `forceRatioId` are written to the
CSV but are not fed into the simulation, so the fingerprint and world-hash columns depend only on
`(scenarioId, seed, ticks, mvpEngagement)`. Sweeping doctrine or ROE changes the labels, not the run.

CSV columns (header emitted on every run):

```text
scenarioId,seed,ticks,side,doctrineId,roeId,forceRatioId,gridMethod,score,kills,missilesFired,denials,fingerprint,fingerprintSha256,worldHash,detectionWorldHash,buildId
```

`buildId` is the demo assembly's informational version, so a definition plus a build reproduce a row.
With `--csv-out` the CSV is written to the file (`Wrote <n> rows to <path>`); otherwise it goes to stdout.

Example definition and its output (2 seeds × 2 doctrines, cartesian):

```json
{
  "scenarioId": "baltic-patrol",
  "ticks": 4,
  "seeds": [42, 7],
  "grid": { "method": "cartesian", "parameters": ["seed", "doctrineId"] },
  "sweeps": { "doctrineIds": ["aggressive", "cautious"] },
  "roeId": "weapons-tight"
}
```

```text
scenarioId,seed,ticks,side,doctrineId,roeId,forceRatioId,gridMethod,score,kills,missilesFired,denials,fingerprint,fingerprintSha256,worldHash,detectionWorldHash,buildId
baltic-patrol,42,4,BLUE,aggressive,weapons-tight,,cartesian,100,1,1,0,…,2cc200ec…,17144800277401907079,15600,1.0.0+…
baltic-patrol,42,4,BLUE,cautious,weapons-tight,,cartesian,100,1,1,0,…,2cc200ec…,17144800277401907079,15600,1.0.0+…
baltic-patrol,7,4,BLUE,aggressive,weapons-tight,,cartesian,100,1,1,0,…,4443e889…,6759168507833323181,6879,1.0.0+…
baltic-patrol,7,4,BLUE,cautious,weapons-tight,,cartesian,100,1,1,0,…,4443e889…,6759168507833323181,6879,1.0.0+…
```

The two doctrines share a fingerprint and world hash for each seed — the opaque label did not touch the sim.

---

## Regenerating a golden

The source of truth for each golden's `(seed, scenario, ticks)` is
`ReplayGoldenRegressionCatalog` (see [`tests/regression/README.md`](../../tests/regression/README.md)).
To refresh one after an **intentional** behavior change:

```bash
dotnet run --project src/ProjectAegis.Delegation.Demo -- --seed <SEED> --scenario <policy-id> --ticks <N>
```

Copy `WORLD_HASH`, `DETECTION_WORLD_HASH`, and `FINGERPRINT_SHA256` into the matching
`tests/regression/replay-golden-*.txt`, then re-run the gate:

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter FullyQualifiedName~ReplayGoldenSuiteTests
```

> **Never re-record a golden to make a red test go green.** A changed golden *is* a behavior
> change — confirm it is intended and say so in the commit message. Editing `baltic-v3-*`
> goldens must never touch the production Baltic v2 hash `17144800277401907079`
> ([hard invariants](../../AGENTS.md#hard-invariants--never-break-these)).

---

## See also

| Topic | Doc |
|-------|-----|
| Determinism rules, hashing, golden workflow | [`docs/engineering/determinism-and-replay.md`](../../docs/engineering/determinism-and-replay.md) |
| Golden fixtures + catalog | [`tests/regression/README.md`](../../tests/regression/README.md) |
| Replay harness / batch runner (Unity adapter) | [`../ProjectAegis.Delegation.UnityAdapter/README.md`](../ProjectAegis.Delegation.UnityAdapter/README.md) |
| Simulation core (hashes, seeded RNG) | [`../ProjectAegis.Sim/README.md`](../ProjectAegis.Sim/README.md) |
| Tick pipeline order + world-hash layers | [`adr-004-tick-pipeline-order.md`](../../docs/architecture/adr-004-tick-pipeline-order.md) |
