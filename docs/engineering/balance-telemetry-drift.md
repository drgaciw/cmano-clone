# Balance telemetry — advisory win-rate drift

> **Subsystem:** `ProjectAegis.Data.Telemetry`
> **Decision of record:** [ADR-006 — Data layer boundary](../architecture/adr-006-data-layer-boundary.md)
> **Requirements:** Req 06 (Database Intelligence — DBI-5 balance drift)

Balance telemetry is the **post-P0 advisory hook** that watches agent-vs-agent simulation
outcomes and flags platforms/weapons whose win rate has drifted away from expectation. It is
deliberately **read-only with respect to the catalog**: it never mutates a row, never opens
SQLite, and never touches the [write gate](catalog-write-gate.md). A drift finding is a
*signal for a human reviewer*, not an auto-tuning action (DBI-5.1, DBI-5.2).

This page documents the subsystem exactly as it behaves in source today, including the fact
that the sink is **off by default** and **not yet wired into any simulation caller** — it is a
complete, unit-tested library awaiting integration.

## Why it exists

Catalog stats (sensor `basePd`, weapon envelopes, platform metadata) drive simulation
outcomes. If one platform wins far more than it should across many matches, that is evidence
the catalog needs rebalancing — but the P0 rule is that **sim telemetry never auto-adjusts the
catalog** (DBI-5.1). This subsystem closes the loop *advisorily*: it accumulates outcomes,
computes a win rate per entity, and raises an advisory `BALANCE_WIN_RATE_DRIFT` finding when
the delta from the expected rate exceeds a tuneable band over a minimum sample. The human then
decides whether to open a rebalancing proposal through the normal write-gate path.

## The contract

`IBalanceTelemetrySink` (`src/ProjectAegis.Data/Telemetry/IBalanceTelemetrySink.cs`) is the
only public surface a caller needs:

| Method | Purpose |
|--------|---------|
| `RecordOutcome(entityId, entityKind, won)` | Tally one agent-vs-agent result for an entity |
| `RegisterExpectedWinRate(entityId, expectedWinRate)` | Override the baseline expected rate for one entity (`[0, 1]`) |
| `EvaluateDrift()` | Return a `BalanceDriftReport` (findings + deterministic state hash) |
| `ComputeStateHash()` | Return the deterministic state hash alone (for CI golden tests) |

`BalanceEntityKind` is `Platform = 0` or `Weapon = 1`. Outcomes for the same `entityId` under
different kinds are tracked **separately**.

## Two implementations, chosen by a feature flag

The sink is **disabled by default**. Resolve the correct implementation through the factory
rather than constructing one directly:

```csharp
var sink = BalanceTelemetrySinkFactory.Create(
    new BalanceDriftFeatureFlags { EnableBalanceDrift = true },   // default: false
    new BalanceDriftOptions());                                   // optional thresholds
```

| `EnableBalanceDrift` | Returned sink | Behavior |
|----------------------|---------------|----------|
| `false` (default) | `NoOpBalanceTelemetrySink.Instance` | Records nothing; `EvaluateDrift()` returns `BalanceDriftReport.EmptyDisabled`; `ComputeStateHash()` returns the pinned empty-state hash |
| `true` | `BalanceTelemetryAccumulator` | Real accumulation and drift evaluation |

The no-op is a true singleton, so when the flag is off the hot path costs nothing — `Create()`
with no arguments returns the same `Instance` every time
(`BalanceTelemetrySinkFactoryTests.Default_feature_flags_return_no_op_sink`). This is the
P0 posture (DBI-5 P0 note): the hook exists so callers can be wired now, but it stays inert
until the flag is flipped.

## Thresholds (`BalanceDriftOptions`)

| Option | Default | Meaning |
|--------|---------|---------|
| `WinRateDriftThreshold` | `0.08` | Absolute win-rate delta **above which** a flag is raised (±8%, DBI-5.3) |
| `MinimumSampleRuns` | `500` | Minimum recorded runs for an entity **before** it is evaluated at all (DBI-5.3) |
| `DefaultExpectedWinRate` | `0.5` | Baseline expected rate when no per-entity override is registered |

## Drift semantics

`EvaluateDrift()` walks every accumulated entity in ordinal `(EntityId, EntityKind)` order and
emits at most one `BALANCE_WIN_RATE_DRIFT` finding per entity:

- Entities with `TotalRuns < MinimumSampleRuns` are **skipped** — not enough evidence
  (`Insufficient_samples_do_not_emit_flags`).
- `drift = ActualWinRate − ExpectedWinRate`. A finding fires only when `|drift|` **strictly
  exceeds** the threshold. The band is inclusive: a delta of exactly +8% does **not** fire
  (`Drift_flag_does_not_fire_at_exactly_eight_percent_band`).
- Drift is **two-sided** — an entity winning too little (e.g. 30% vs 50% expected, delta
  −0.2) is flagged just like one winning too much
  (`Negative_drift_below_baseline_is_flagged`).
- Per-entity expected rates are honored: register 0.7 for an entity and a 79% observed rate
  yields a delta of +0.09, which clears the ±8% band and fires
  (`Per_entity_expected_win_rate_is_honored`).

Each `BalanceDriftFinding` carries `Code`, `EntityId`, `EntityKind`, `SampleRuns`,
`ExpectedWinRate`, `ActualWinRate`, `DriftDelta`, and a human-readable `Message`. Findings are
**advisory records only** — nothing downstream blocks on them.

## Determinism & the state hash

The accumulator is built for reproducible CI. Entities live in an ordinal `SortedDictionary`,
findings are emitted in sorted order, and `BalanceTelemetryStateHasher` computes a SHA-256 over
the tab-delimited, `InvariantCulture` (`"R"` round-trip format) rendering of every accumulator
row plus the findings. The same outcome sequence always produces the same hash
(`Golden_fixture_sequence_produces_stable_state_hash`), and the disabled/no-op sink reports the
pinned empty-state hash. Both golden values are pinned in `BalanceTelemetryGoldenHashes`:

| Constant | Value | Source |
|----------|-------|--------|
| `EmptyState` | `e3b0c442…7852b855` (SHA-256 of empty input) | no-op / disabled sink |
| `GoldenFixtureSequence` | `61cc768e…1ca96bee` | fixture in `BalanceTelemetryGoldenTests` |

If you change accumulation, ordering, or the hash rendering, these constants must be
re-pinned — that is the intended tripwire.

## ⚠️ What a developer will hit

These are the real surprises in this subsystem, verified against source.

1. **It is off by default and not wired to a caller yet.** No production code outside
   `ProjectAegis.Data.Telemetry` currently calls `RecordOutcome`. The library is complete and
   unit-tested; the integration work is for whoever owns the agent-vs-agent run loop to (a)
   resolve a sink via `BalanceTelemetrySinkFactory`, (b) flip `EnableBalanceDrift`, and (c)
   feed match results in. Until then `EvaluateDrift()` returns the disabled report everywhere.

2. **Findings never reach the catalog.** This is by design (DBI-5.1/5.2), not a gap. A drift
   flag is a prompt to open a *manual* rebalancing proposal through the
   [write gate](catalog-write-gate.md) — it is not staged, not committed, and cannot bypass
   review. The test `Telemetry_does_not_mutate_write_gate_state` asserts the accumulator does
   not even implement `IWriteGate` and leaves pending batches untouched.

3. **Sample gating hides early data.** With the default `MinimumSampleRuns = 500`, an entity
   recorded 499 times produces **no** findings regardless of how lopsided its win rate is.
   Lower `MinimumSampleRuns` in tests/fixtures (the test suite uses 50–100) to exercise drift
   without simulating hundreds of runs.

## Usage example

```csharp
using ProjectAegis.Data.Telemetry;

var sink = BalanceTelemetrySinkFactory.Create(
    new BalanceDriftFeatureFlags { EnableBalanceDrift = true },
    new BalanceDriftOptions { MinimumSampleRuns = 100, WinRateDriftThreshold = 0.08 });

// Optional: set a non-0.5 expectation for an asymmetric matchup.
sink.RegisterExpectedWinRate("DDG-51", 0.5);

foreach (var result in agentVsAgentResults)        // your run loop
{
    sink.RecordOutcome(result.PlatformId, BalanceEntityKind.Platform, result.Won);
}

BalanceDriftReport report = sink.EvaluateDrift();
foreach (var finding in report.Findings)
{
    // Advisory: surface to a human reviewer; do NOT mutate the catalog here.
    logger.LogWarning("{Code}: {Message}", finding.Code, finding.Message);
}
```

## Constraints & gotchas

- **Advisory only:** never write a catalog row in response to a finding. All catalog mutation
  goes through `IWriteGate` under human/agent approval (DBI-5.2, ADR-006).
- **No SQLite, no wall-clock:** the accumulator is pure in-memory state; keep it that way so it
  stays deterministic and replay-safe.
- **Resolve through the factory:** do not assume an enabled sink. `Create()` with default flags
  returns the no-op singleton — callers must handle the disabled report shape.
- **`entityId` is required:** `RecordOutcome`/`RegisterExpectedWinRate` throw
  `ArgumentException` on null/blank ids; `expectedWinRate` outside `[0, 1]` throws
  `ArgumentOutOfRangeException`.

## Verify

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "Telemetry" -v minimal
```

Covers `BalanceTelemetrySinkFactoryTests` (flag → sink selection),
`BalanceTelemetryAccumulatorTests` (sample gating, ±8% band edge, two-sided drift, per-entity
expectations, write-gate isolation), and `BalanceTelemetryGoldenTests` (pinned state hash).

## See also

- [Catalog write gate runbook](catalog-write-gate.md) — the only sanctioned path for the
  rebalancing proposals a drift finding might prompt
- [Platform editor Excel round-trip](platform-editor-excel-roundtrip.md) — the authoring
  surface that feeds the write gate
- [ADR-006 — Data layer boundary](../architecture/adr-006-data-layer-boundary.md) — why balance
  drift is advisory and out of the P0 commit path
- Source — `src/ProjectAegis.Data/Telemetry/`, tests in
  `src/ProjectAegis.Data.Tests/Telemetry/`
