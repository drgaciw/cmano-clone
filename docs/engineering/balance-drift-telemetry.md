# Balance Drift Telemetry (Advisory)

> **Scope:** `ProjectAegis.Data.Telemetry` — win-rate accumulation + advisory drift flags.
> **Authoritative ADR:** [ADR-006 — Data Layer Boundary](../architecture/adr-006-data-layer-boundary.md)
> **Requirements:** Req 06 — Database Intelligence §5 Balance Drift Detection (`DBI-5.1`–`DBI-5.4`)
> **Last updated:** 2026-06-18

This runbook covers the balance drift telemetry subsystem under
[`src/ProjectAegis.Data/Telemetry/`](../../src/ProjectAegis.Data/Telemetry/). It is the
companion to the [Catalog Write-Gate & Determinism Contract](catalog-write-gate-determinism.md):
that doc explains how catalog rows are *written*; this one explains how agent-vs-agent sim
outcomes are *observed* to flag balance problems — **without ever touching the catalog**.

## Intent: advisory only, never a write path

The single most important property of this subsystem is in its interface contract:

> **Advisory only — never mutates catalog or bypasses `IWriteGate`.**
> — [`IBalanceTelemetrySink`](../../src/ProjectAegis.Data/Telemetry/IBalanceTelemetrySink.cs)

Per `DBI-5.1`/`DBI-5.2`, the P0 catalog commit path **does not** read sim telemetry and balance
findings are advisory. This subsystem records outcomes and computes drift reports; acting on a
finding is a *separate* human-gated proposal that re-enters the normal staging workflow
(`DBI-5.4`). Nothing here can change a `base_pd`, a win rate, or any other catalog field.

`BalanceTelemetryAccumulator` deliberately does **not** implement `IWriteGate`, and a regression
test asserts this (`Telemetry_does_not_mutate_write_gate_state`).

## Default-off via feature flag

The subsystem ships **disabled**. `DBI-5` (full ±8% detection) is sequenced *after* P0, so the
default is a no-op hook.

```csharp
// Default flags → NoOpBalanceTelemetrySink.Instance (records nothing, flags nothing)
IBalanceTelemetrySink sink = BalanceTelemetrySinkFactory.Create();

// Opt in to the real accumulator
var sink = BalanceTelemetrySinkFactory.Create(
    new BalanceDriftFeatureFlags { EnableBalanceDrift = true });
```

| Flag | Default | Effect when default |
|------|---------|---------------------|
| [`BalanceDriftFeatureFlags.EnableBalanceDrift`](../../src/ProjectAegis.Data/Telemetry/BalanceDriftFeatureFlags.cs) | `false` | Factory returns [`NoOpBalanceTelemetrySink`](../../src/ProjectAegis.Data/Telemetry/NoOpBalanceTelemetrySink.cs); `EvaluateDrift()` returns `BalanceDriftReport.EmptyDisabled`; state hash is the empty-state constant. |

Always construct the sink through [`BalanceTelemetrySinkFactory.Create`](../../src/ProjectAegis.Data/Telemetry/BalanceTelemetrySinkFactory.cs)
so the flag is honoured — do not `new` the accumulator directly in production code.

## API surface

[`IBalanceTelemetrySink`](../../src/ProjectAegis.Data/Telemetry/IBalanceTelemetrySink.cs):

| Member | Purpose |
|--------|---------|
| `RecordOutcome(entityId, entityKind, won)` | Tally one agent-vs-agent result for a platform/weapon. |
| `RegisterExpectedWinRate(entityId, expectedWinRate)` | Override the baseline win rate for an entity (must be in `[0, 1]`). |
| `EvaluateDrift()` | Build the current [`BalanceDriftReport`](../../src/ProjectAegis.Data/Telemetry/BalanceDriftReport.cs) (findings + state hash). |
| `ComputeStateHash()` | Deterministic SHA-256 of accumulator state for CI golden tests. |

[`BalanceEntityKind`](../../src/ProjectAegis.Data/Telemetry/BalanceEntityKind.cs) is
`Platform = 0` or `Weapon = 1`. Outcomes are keyed by `(entityId, entityKind)`, so the same id
can carry independent platform and weapon tallies.

### Typical usage

```csharp
var sink = BalanceTelemetrySinkFactory.Create(
    new BalanceDriftFeatureFlags { EnableBalanceDrift = true });

// Optional: per-entity baseline; otherwise DefaultExpectedWinRate (0.5) is used.
sink.RegisterExpectedWinRate("DDG-1000", 0.55);

foreach (var run in agentVsAgentRuns)
{
    sink.RecordOutcome(run.PlatformId, BalanceEntityKind.Platform, run.BlueWon);
}

BalanceDriftReport report = sink.EvaluateDrift();
foreach (var finding in report.Findings)
{
    // Advisory: surface to the Balance Tuning Agent / review queue — never auto-apply.
    Console.WriteLine(finding.Message);
}
```

## Drift evaluation rules

Findings are produced by `BalanceTelemetryAccumulator.EvaluateDrift()` and tuned by
[`BalanceDriftOptions`](../../src/ProjectAegis.Data/Telemetry/BalanceDriftOptions.cs):

| Option | Default | Meaning (`DBI-5.3`) |
|--------|---------|---------------------|
| `WinRateDriftThreshold` | `0.08` | A flag fires only when `abs(actual − expected)` is **strictly greater** than ±8%. |
| `MinimumSampleRuns` | `500` | Entities below the sample floor are skipped entirely (no flag, regardless of drift). |
| `DefaultExpectedWinRate` | `0.5` | Baseline used when no per-entity expected rate is registered. |

A [`BalanceDriftFinding`](../../src/ProjectAegis.Data/Telemetry/BalanceDriftFinding.cs) carries
`Code = "BALANCE_WIN_RATE_DRIFT"`, the entity id/kind, `SampleRuns`, expected vs. actual win
rate, the signed `DriftDelta`, and a human-readable `Message`. Negative drift (underperforming
relative to baseline) is flagged the same as positive drift.

**Boundary behaviour (verified by tests):**

- Exactly ±8% does **not** flag (the threshold is exclusive) — `Drift_flag_does_not_fire_at_exactly_eight_percent_band`.
- Below `MinimumSampleRuns`, nothing is emitted — `Insufficient_samples_do_not_emit_flags`.
- A late `RegisterExpectedWinRate` retro-applies to already-accumulated entities with that id.

## Determinism & golden hashes

Like the rest of `ProjectAegis.Data`, telemetry state hashing is deterministic so CI can pin it:

- [`BalanceTelemetryStateHasher`](../../src/ProjectAegis.Data/Telemetry/BalanceTelemetryStateHasher.cs)
  computes a SHA-256 over **ordinal-sorted** accumulator rows then sorted findings. Sorting is by
  `EntityId` (`StringComparer.Ordinal`) then `EntityKind`, so input order never changes the hash.
- Floating-point fields use `"R"` round-trip formatting with `InvariantCulture` (same rule as
  `CatalogSnapshotHasher`) — no locale/OS drift.
- Pinned constants live in
  [`BalanceTelemetryGoldenHashes`](../../src/ProjectAegis.Data/Telemetry/BalanceTelemetryGoldenHashes.cs):
  `EmptyState` (no-op / disabled) and `GoldenFixtureSequence` (the
  `BalanceTelemetryGoldenTests` fixture). **Changing accumulation, sort, or hash logic means
  re-pinning the affected constant in the same PR** — a hash diff in CI is a signal, not noise.

`internal` accumulator state (`BalanceTelemetryEntitySnapshot`) never leaks across the assembly
boundary; only `BalanceDriftReport` / `BalanceDriftFinding` records are public.

## Testing & verification

| Test | Asserts |
|------|---------|
| `BalanceTelemetrySinkFactoryTests.Default_feature_flags_return_no_op_sink` | flag-off → singleton no-op sink |
| `BalanceTelemetrySinkFactoryTests.Enabled_feature_flag_returns_real_accumulator` | flag-on → real accumulator |
| `BalanceTelemetryAccumulatorTests.Disabled_sink_does_not_accumulate_or_flag` | no-op records nothing, empty-state hash |
| `BalanceTelemetryAccumulatorTests.Drift_flag_fires_when_delta_exceeds_eight_percent` | >±8% over sample floor flags |
| `BalanceTelemetryAccumulatorTests.Drift_flag_does_not_fire_at_exactly_eight_percent_band` | exclusive threshold |
| `BalanceTelemetryAccumulatorTests.Negative_drift_below_baseline_is_flagged` | underperformance flagged |
| `BalanceTelemetryAccumulatorTests.Per_entity_expected_win_rate_is_honored` | registered baseline used |
| `BalanceTelemetryAccumulatorTests.Telemetry_does_not_mutate_write_gate_state` | no `IWriteGate` coupling / side effects |
| `BalanceTelemetryGoldenTests.Golden_fixture_sequence_produces_stable_state_hash` | pinned golden hash + repeatability |

Run the telemetry slice:

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "Telemetry" -v minimal
```

## Common pitfalls

| Pitfall | Symptom | Fix |
|---------|---------|-----|
| `new BalanceTelemetryAccumulator()` in production | Drift flags emitted while feature is meant to be off | Construct via `BalanceTelemetrySinkFactory.Create(flags)` |
| Expecting a flag below 500 runs | No findings on a clearly skewed entity | Lower `MinimumSampleRuns` *only* in tests; production floor is `DBI-5.3` |
| Expecting exactly ±8% to flag | Missing finding at the band edge | Threshold is exclusive; provide >±8% in tests |
| Treating a finding as a catalog change | Attempt to write back win-rate corrections | Findings are advisory; corrections re-enter the write gate as a human-gated batch (`DBI-5.4`) |
| Editing hash/sort logic without re-pinning | CI red on `BalanceTelemetryGoldenTests` | Re-pin `BalanceTelemetryGoldenHashes` in the same PR |
| Empty/whitespace `entityId` | `ArgumentException` from `RecordOutcome` / `RegisterExpectedWinRate` | Pass a non-empty canonical id |

## See also

- [Catalog Write-Gate & Determinism Contract](catalog-write-gate-determinism.md)
- [ADR-006 — Data Layer Boundary](../architecture/adr-006-data-layer-boundary.md)
- `Game-Requirements/requirements/06-Database-Intelligence.md` §5 (`DBI-5.1`–`DBI-5.4`) and Resolved Design Decisions §3
- `balance-tuning-memory-agent` (cross-session trait/balance tuning memory)
