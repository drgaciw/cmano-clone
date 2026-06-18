# Replay & determinism harness

How Project Aegis proves the simulation is reproducible: the world-state hash,
the order-log fingerprint, the Baltic replay harness, and the pinned golden
regression gate that blocks non-deterministic builds in CI.

This is the operational companion to the `/replay-verify` skill
(`.claude/skills/replay-verify/SKILL.md`). The skill describes the *gate
procedure*; this doc describes the *machinery* it drives and how to read,
regenerate, and extend the goldens.

## Intent

The core invariant (req 03, req 04, delegation spec §2.5): a given
`(scenario, seed, ticks)` must produce a **byte-identical order log and
end-state** on every run, in every fresh process. A build whose replay diverges
is not reproducible — and a non-reproducible sim is broken even if every other
unit test passes.

Two failure modes are distinguished deliberately:

| Failure | Meaning | Severity |
|---------|---------|----------|
| **A ≠ B** (two fresh runs differ) | Intra-run non-determinism — the sim itself is unstable | Release-blocking |
| **A ≠ golden** (run differs from pinned baseline) | Behavior drift — sim changed since the baseline was recorded | Needs human decision (intentional re-record vs. regression) |

## Determinism primitives

Three deterministic digests are computed per run. All three are pinned in goldens.

### 1. World-state hash — `SimWorldHash` (ADR-004)

`src/ProjectAegis.Sim/Core/SimWorldHash.cs`. A `ulong` composed from independent
simulation layers via a fold-and-mix (`Fold` is the SplitMix64 finalizer), each
layer tagged so layer order cannot be silently swapped:

| Layer | Tag | Source |
|-------|-----|--------|
| Core | `LayerCore = 1` | `bridge.Session.Sim.LastWorldHash` |
| Detection | `LayerDetection = 2` | `pdSim.LastDetectionHash` (`DetectionWorldHash`) |
| Engage | `LayerEngage = 3` | engage mix (0 in the current Baltic harness) |
| Combat outcome | `LayerCombatOutcome = 4` | optional kill mix |

```csharp
// BalticReplayHarness final combine (engage mix = 0)
var worldHash = SimWorldHash.Combine(simHash, detectionHash, 0);
```

The detection layer (`DetectionWorldHash`) is also pinned **on its own** so a
detection-only regression is localised without decoding the combined hash.

### 2. Order-log fingerprint — `IOrderLog.ComputeFingerprint()` (ADR-003)

`DecisionLog.ComputeFingerprint()` renders every order-log entry, sorted by
`SequenceId`, into one canonical line:

```
<Kind>|<SequenceId>|<SimTime:R>|<payload>\n
```

`SimTime` uses the round-trip (`"R"`) format so float time is exact. Payload
shape is per `OrderLogEntryKind` (see `DecisionLog.FormatPayload`), e.g.:

```
EngagementOutcome|5|1|1|1|hostile-1|Kill|0.006501966157579321
```

The `FingerprintMustContain` fragments in the regression catalog assert against
this text (e.g. `"|Kill|"`, `"CommsStateChange"`, `"CYBER_SPOOF_TRACK"`).

### 3. Fingerprint SHA-256 — `OrderLogReplayFingerprint`

`src/ProjectAegis.Delegation/Replay/OrderLogReplayFingerprint.cs` hashes the
UTF-8 fingerprint text with SHA-256, lower-case hex. This is the compact,
diff-friendly digest pinned as `FINGERPRINT_SHA256=` in goldens.

### Checkpoints — scrub-to-tick

`ReplayCheckpoint` / `ReplayCheckpointStore`
(`src/ProjectAegis.Delegation/Replay/`) record a `(SimTick, WorldHash,
LogFingerprint, LastSequenceId)` boundary at a fixed interval
(`ScenarioReplaySettings.CheckpointIntervalTicks`, default `300`; the Baltic
checkpoint goldens use small intervals like `2`). The store is append-only and
monotonic in `SimTick`; `FindAtOrBefore` supports replay scrubbing. Checkpoints
localise *where* a divergence began rather than only reporting the end-state.

## The harness

`BalticReplayHarness.Run(seed, scenarioPolicyId, ticks, ...)`
(`src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs`) is the
single headless entry point. It is plain .NET — **no Unity Editor required** —
and is the runner referenced by `/replay-verify`. It:

1. Loads the scenario policy (`ScenarioPolicyRepository.EnsureDefaultJsonLoaded`)
   and resolves a catalog reader (`BalticPatrol` fixture fallback).
2. Builds detection (`PdDetectionContactSimulator` or `ScenarioContactSimulator`),
   mission timeline, and a `DelegationBridge` seeded from `seed`.
3. Steps `ticks` times through the fixed tick pipeline (ADR-004): mission events
   → detection transitions → `bridge.Tick` → kill propagation → checkpoint
   record.
4. Returns a `Result` with `Fingerprint`, `FingerprintSha256`, `WorldHash`,
   `DetectionWorldHash`, `EngagementCount`, `Checkpoints`, messages, sensor/C2
   snapshot, and a scoring CSV row.

`BalticBatchRunner` (same folder) fans `Run` across scenarios × seeds for
agent-vs-agent CSV export; it shares the identical per-run code path, so batch
output stays consistent with single-run goldens.

### Driving the harness

| Surface | Command / call | Use |
|---------|----------------|-----|
| Demo CLI (single) | `dotnet run --project src/ProjectAegis.Delegation.Demo -- --seed 42 --scenario baltic-patrol --ticks 4` | Print/regenerate hashes for a golden |
| Demo CLI (batch) | `dotnet run --project src/ProjectAegis.Delegation.Demo -- --batch --all-scenarios --csv-out out.csv` | Scoring CSV across all policies |
| MCP CLI verb | `dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_simulate_sample --path <scenario.json> [--ticks N]` | Validate-then-simulate a scenario file; emits JSON with the same digests (see `mission-editor-mcp-cli-reference.md`) |

The Demo `--single` output is the canonical golden format:

```
SEED=42 SCENARIO=baltic-patrol TICKS=4 ENGAGEMENTS=1
FINGERPRINT=...
FINGERPRINT_SHA256=080a4cbf...649917
DETECTION_WORLD_HASH=15600
WORLD_HASH=17144800277401907079
REPLAY_CHECKPOINT=<tick>:<worldHash>:<lastSequenceId>
```

`scenario_simulate_sample` runs the **validation export gate first**: exit `2`
on missing file, exit `1` when validation blocks export, exit `0` with the
digest JSON on success. The 32-tick `baltic-patrol-catalog` sample is itself
pinned in `SimulateSampleGoldenHashes.cs`.

## Golden files & the CI gate

Pinned baselines live in `tests/regression/replay-golden-*.txt`
(see `tests/regression/README.md`). File format:

```
# <human note: scenario seed=N ticks=N ...>
# Regenerate: dotnet run --project src/ProjectAegis.Delegation.Demo -- --seed N --scenario <id> --ticks N
WORLD_HASH=<ulong>
DETECTION_WORLD_HASH=<ulong>
FINGERPRINT_SHA256=<hex>          # optional; asserted only when present
<order-log fingerprint lines...>  # human-readable reference
```

Checkpoint goldens (`*-checkpoints-*.txt`) instead pin
`REPLAY_CHECKPOINT=<tick>:<worldHash>:<lastSequenceId>` lines.

The seed/scenario/ticks for each file are declared in
`ReplayGoldenRegressionCatalog.cs`, with required fingerprint fragments. The
blocking gate is `ReplayGoldenSuiteTests` — for each catalog case it runs the
harness **twice** (asserts `A == B`, the intra-run check) and then asserts the
pinned `WORLD_HASH` / `DETECTION_WORLD_HASH` / `FINGERPRINT_SHA256` and the
required fragments (`ReplayGoldenAssertions`).

Run the gate locally:

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter FullyQualifiedName~ReplayGoldenSuiteTests
```

CI runs the same filter on every PR via Buildkite
(`tools/buildkite/dotnet-ci.sh`, `.buildkite/pipeline.yml`; see
`docs/engineering/buildkite-ci.md`).

## Regenerating a golden (deliberate)

Only re-record after confirming the behavior change is **intentional** — silent
re-recording hides regressions (`/replay-verify` Phase 6 routes drift to a human
decision).

1. Look up the case in `ReplayGoldenRegressionCatalog.cs` (seed, policy, ticks).
2. Run the Demo CLI with those exact arguments.
3. Copy `WORLD_HASH`, `DETECTION_WORLD_HASH`, and `FINGERPRINT_SHA256` into the
   matching `replay-golden-*.txt`. For checkpoint goldens, copy the
   `REPLAY_CHECKPOINT=` lines.
4. Note the reason in the commit message and re-run `ReplayGoldenSuiteTests`.

## Adding a new replay regression

1. Add a `baltic-patrol-*` scenario policy (or reuse one) so
   `ScenarioPolicyRepository` resolves it.
2. Add a `ReplayGoldenRegressionCatalog.Case` with the golden filename, seed,
   policy id, ticks, `MvpEngagement`, and `FingerprintMustContain` fragments
   that capture the behavior under test.
3. Generate the golden file (steps above) and commit it under
   `tests/regression/`. The new case is picked up automatically by the
   `TestCaseSource` in `ReplayGoldenSuiteTests`.

## Common pitfalls

- **`A ≠ B` is the worst case.** Internal non-determinism is release-blocking and
  takes priority over baseline drift. Use `/replay-verify <scenario> --bisect`
  then `/determinism-audit` to root-cause; hand the first diverging tick to the
  `determinism-engineer`.
- **Don't read sim time with anything but `"R"` formatting** when contributing to
  a fingerprint — non-round-trip float formatting silently loses bits and breaks
  reproducibility.
- **Detection vs. world hash.** A failure that pins only `DETECTION_WORLD_HASH`
  but not the combined `WORLD_HASH` points at the sensor layer; both differing
  points further upstream (core sim).
- **Checkpoint interval is policy-driven.** `CheckpointIntervalTicks` defaults to
  `300`; short Baltic goldens override it. A run shorter than one interval emits
  no checkpoints — expected, not a bug.
- **Cross-platform.** A PASS in one environment does not guarantee cross-platform
  reproduction; flag platform-sensitive float/transcendental math for a
  target-hardware check before relying on a golden across OSes.

## See also

- `docs/architecture/adr-003-order-log-schema.md` — order-log entry schema.
- `docs/architecture/adr-004-tick-pipeline-order.md` — fixed pipeline + layered hash.
- `docs/engineering/mission-editor-mcp-cli-reference.md` — `scenario_simulate_sample` verb.
- `docs/engineering/balance-drift-telemetry.md` — separate `BalanceTelemetryStateHasher` golden pattern.
- `.claude/skills/replay-verify/SKILL.md` / `.claude/skills/determinism-audit/SKILL.md` — gate procedures.
