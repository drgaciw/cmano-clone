# Balance telemetry sink (Project Aegis Data)

Advisory win-rate drift detection for catalog entities, fed by agent-vs-agent
simulation outcomes. Implements requirement **DBI-5 (Balance Drift Detection)**
from [`06-Database-Intelligence.md`](../../../Game-Requirements/requirements/06-Database-Intelligence.md).

**Advisory only.** This subsystem never mutates the catalog and never bypasses
`WriteGate.IWriteGate`. It observes outcomes and produces findings; correcting an
imbalance is a separate, human-approved staging proposal (DBI-5.4).

**Off by default.** When the feature flag is disabled the factory returns a no-op
sink, so the P0 commit path and golden replays stay deterministic (DBI-5.1).

## Concepts

| Type | Role |
|------|------|
| `IBalanceTelemetrySink` | The hook injected wherever sim outcomes are observed. |
| `BalanceTelemetryAccumulator` | Real implementation: tallies wins/runs per entity and evaluates drift. |
| `NoOpBalanceTelemetrySink` | Zero-cost singleton used when the flag is off. |
| `BalanceTelemetrySinkFactory` | Picks the real or no-op sink from `BalanceDriftFeatureFlags`. |
| `BalanceDriftFeatureFlags` | `EnableBalanceDrift` — defaults to `false`. |
| `BalanceDriftOptions` | Tuneable thresholds (see below). |
| `BalanceDriftReport` / `BalanceDriftFinding` | Evaluation snapshot and per-entity flags. |
| `BalanceEntityKind` | `Platform` or `Weapon`. |

## Enable

```csharp
var sink = BalanceTelemetrySinkFactory.Create(
    new BalanceDriftFeatureFlags { EnableBalanceDrift = true });

// Optional per-entity baseline; defaults to BalanceDriftOptions.DefaultExpectedWinRate (0.5).
sink.RegisterExpectedWinRate("F-22A", expectedWinRate: 0.55);

// Record outcomes as agent-vs-agent runs complete.
sink.RecordOutcome("F-22A", BalanceEntityKind.Platform, won: true);

// Evaluate when you want an advisory report.
BalanceDriftReport report = sink.EvaluateDrift();
foreach (var finding in report.Findings)
{
    // finding.Code == "BALANCE_WIN_RATE_DRIFT"
    Console.WriteLine(finding.Message);
}
```

With `EnableBalanceDrift = false` (the default), `Create(...)` returns
`NoOpBalanceTelemetrySink.Instance`: `RecordOutcome` is a no-op, `EvaluateDrift`
returns `BalanceDriftReport.EmptyDisabled`, and `ComputeStateHash` returns the
empty-state golden hash.

## Drift rule (DBI-5.3)

A `BALANCE_WIN_RATE_DRIFT` finding is emitted for an entity only when **both**:

1. it has at least `MinimumSampleRuns` recorded runs (default **500**), and
2. `|actual win rate − expected win rate|` is **strictly greater than**
   `WinRateDriftThreshold` (default **±0.08**, i.e. ±8%).

A delta exactly at the threshold does **not** flag. Drift can be positive
(over-powered) or negative (under-powered). Defaults live in `BalanceDriftOptions`:

| Option | Default | Meaning |
|--------|---------|---------|
| `WinRateDriftThreshold` | `0.08` | Advisory band around expected win rate. |
| `MinimumSampleRuns` | `500` | Minimum runs before an entity is evaluated. |
| `DefaultExpectedWinRate` | `0.5` | Baseline when no per-entity override is registered. |

Entities are keyed by `(entityId, entityKind)`, so the same id can be tracked
independently as a platform and as a weapon.

## Determinism and CI golden hashes

`ComputeStateHash()` produces a SHA-256 over the accumulator rows and findings,
sorted ordinally by `(entityId, entityKind)` (`BalanceTelemetryStateHasher`).
The same outcomes always yield the same hash regardless of insertion order, which
lets CI pin behavior:

- `BalanceTelemetryGoldenHashes.EmptyState` — disabled / no-op sink.
- `BalanceTelemetryGoldenHashes.GoldenFixtureSequence` — the fixture in
  `BalanceTelemetryGoldenTests`.

If you intentionally change accumulation or hashing, re-run
`BalanceTelemetryGoldenTests` and update the pinned constant in
`BalanceTelemetryGoldenHashes`. An unexpected change there means the state shape
drifted and should be reviewed.

## Constraints

- **Never** route catalog writes through this sink — findings are advisory and
  must go through the write gate as human-approved proposals (DBI-5.2 / DBI-5.4).
- `RecordOutcome` rejects null/blank `entityId`; `RegisterExpectedWinRate`
  requires a rate in `[0, 1]`.
- Keep the sink out of any deterministic commit path unless the feature flag is
  explicitly enabled for that run.

## Tests

`src/ProjectAegis.Data.Tests/Telemetry/` — accumulator behavior, the sink
factory flag switch, and the golden state-hash fixture.
