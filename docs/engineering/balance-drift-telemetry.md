# Balance drift telemetry (DBI-5)

Advisory win-rate drift detection for the catalog data layer. Lives in
`src/ProjectAegis.Data/Telemetry/` (namespace `ProjectAegis.Data.Telemetry`).

## Intent

Requirement 06 §5 ("Balance Drift Detection") asks the data layer to watch
agent-vs-agent outcomes and flag platforms/weapons whose win rate has drifted
from its expected baseline. This subsystem is the **advisory hook** for that
goal:

- **DBI-5.1** — the P0 catalog commit path never auto-adjusts stats from sim telemetry.
- **DBI-5.2** — findings are advisory only and **never** bypass the write gate (`IWriteGate`).
- **DBI-5.3** — flag when a platform/weapon win-rate delta exceeds **±8 %** over **≥ 500** runs (tuneable).
- **DBI-5.4** — full balance *proposals* (patch bundles through staging) are still deferred.

It reports drift; it does not mutate the catalog, enqueue proposals, or read sim
telemetry into the commit loop. See `docs/architecture/adr-006-data-layer-boundary.md`
and `docs/superpowers/specs/2026-05-30-database-intelligence-p0-design.md` for the
boundary this sits behind.

## Architecture

| Type | Role |
|------|------|
| `IBalanceTelemetrySink` | Hook surface: `RecordOutcome`, `RegisterExpectedWinRate`, `EvaluateDrift`, `ComputeStateHash`. |
| `BalanceTelemetryAccumulator` | Real implementation. Keeps a per-entity win/total tally and computes drift findings on demand. |
| `NoOpBalanceTelemetrySink` | Singleton no-op used when the feature flag is off (the P0 default). |
| `BalanceTelemetrySinkFactory` | Returns the accumulator or the no-op based on `BalanceDriftFeatureFlags`. |
| `BalanceDriftFeatureFlags` | `EnableBalanceDrift` (default `false`). |
| `BalanceDriftOptions` | Tuneable thresholds (see below). |
| `BalanceDriftReport` / `BalanceDriftFinding` | Result records returned by `EvaluateDrift`. |
| `BalanceEntityKind` | `Platform = 0`, `Weapon = 1`. |
| `BalanceTelemetryStateHasher` | Deterministic SHA-256 over sorted accumulator rows + findings. |
| `BalanceTelemetryGoldenHashes` | Pinned hashes for CI golden tests. |

> **Wiring status:** the sink is currently a standalone hook with no production
> caller — the sim loop does not yet feed it outcomes. Treat it as the reserved
> integration point for the future Balance Tuning Agent, not a live monitor.

## Usage

The sink is **opt-in**. With default flags `Create` returns the no-op singleton,
so nothing accumulates and `EvaluateDrift` reports `DriftDetectionEnabled = false`.

```csharp
using ProjectAegis.Data.Telemetry;

// Off by default → NoOpBalanceTelemetrySink.Instance.
IBalanceTelemetrySink sink = BalanceTelemetrySinkFactory.Create();

// Enable + tune.
sink = BalanceTelemetrySinkFactory.Create(
    new BalanceDriftFeatureFlags { EnableBalanceDrift = true },
    new BalanceDriftOptions
    {
        WinRateDriftThreshold = 0.08, // ±8 %
        MinimumSampleRuns = 500,
        DefaultExpectedWinRate = 0.5,
    });

// Optional: per-entity baseline (otherwise DefaultExpectedWinRate is used).
sink.RegisterExpectedWinRate("F-35C", 0.55);

// Feed one agent-vs-agent outcome per call.
sink.RecordOutcome("F-35C", BalanceEntityKind.Platform, won: true);

// Evaluate on demand (e.g. end of a run batch).
BalanceDriftReport report = sink.EvaluateDrift();
foreach (var finding in report.Findings)
{
    // finding.Code == "BALANCE_WIN_RATE_DRIFT"
    Console.WriteLine(finding.Message);
}
```

## Drift semantics

For each `(entityId, entityKind)` pair the accumulator tracks `wins` and
`totalRuns`. A `BALANCE_WIN_RATE_DRIFT` finding is emitted only when **both**
conditions hold:

1. `totalRuns >= MinimumSampleRuns` (below the floor, no findings — avoids noise on small samples).
2. `abs(actualWinRate - expectedWinRate) > WinRateDriftThreshold`.

The threshold comparison is **strictly greater than**: a delta sitting exactly on
the band (e.g. 58 wins / 100 runs against a 0.5 baseline = +0.08) is *not* flagged.
Both positive and negative drift are reported, with `DriftDelta` carrying the sign.

| `BalanceDriftOptions` field | Default | Meaning |
|-----------------------------|---------|---------|
| `WinRateDriftThreshold` | `0.08` | Absolute win-rate delta above which a flag fires. |
| `MinimumSampleRuns` | `500` | Minimum recorded runs before an entity is evaluated. |
| `DefaultExpectedWinRate` | `0.5` | Baseline used when no per-entity override is registered. |

`RegisterExpectedWinRate` requires the rate to be in `[0, 1]`; `RecordOutcome`
rejects a blank `entityId`.

## Determinism & golden tests

`ComputeStateHash` / `EvaluateDrift().StateHash` produce a stable SHA-256 over
ordinal-sorted entity rows and findings, so the same outcome sequence always
yields the same hash regardless of insertion order. Two hashes are pinned in
`BalanceTelemetryGoldenHashes`:

- `EmptyState` — no-op sink or disabled flag.
- `GoldenFixtureSequence` — the fixture sequence asserted in `BalanceTelemetryGoldenTests`.

If you change the hasher serialization or the fixture sequence, regenerate and
re-pin both constants. Coverage lives in
`src/ProjectAegis.Data.Tests/Telemetry/` (`BalanceTelemetryAccumulatorTests`,
`BalanceTelemetryGoldenTests`, `BalanceTelemetrySinkFactoryTests`).

## Constraints / pitfalls

- **Advisory only.** Never route catalog writes through this sink. A test asserts
  the accumulator does not implement `IWriteGate` and that evaluating drift leaves
  write-gate state untouched — keep it that way.
- **Off in P0.** The default flag is `false`; production builds get the no-op until
  balance detection is explicitly enabled.
- **Not auto-wired.** Recording outcomes is the caller's responsibility; the sim
  does not yet push results here.
- **`EvaluateDrift` is pull-based.** It snapshots current tallies; call it when you
  want a report rather than expecting streamed alerts.
