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
| `--csv-out path.csv` | batch | Write the batch CSV to a file (otherwise printed to stdout). |

Exit codes: `0` success, `1` harness exception, `2` unknown argument.

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
