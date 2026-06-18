# Balance Telemetry & Drift Detection

`ProjectAegis.Data.Telemetry` — an **advisory-only** sink that watches platform/weapon
win rates across agent-vs-agent runs and flags balance drift. It never mutates the
catalog and never bypasses the write gate.

**Requirement trace:** DBI-5.1–5.3 (`Game-Requirements/requirements/06-Database-Intelligence.md`)
**Origin:** Sprint 22 story 22-6 (`production/sprints/sprint-22-platform-editor-db-doctrine.md`)

## Intent

The simulation can play platforms and weapons against each other many times. If a
catalog entity wins far more (or less) often than expected, its tuning has likely
drifted. This subsystem accumulates those outcomes and emits an advisory
`BalanceDriftReport`.

Three rules are load-bearing and enforced by tests:

- **DBI-5.1** — the P0 catalog commit path never auto-adjusts stats from telemetry.
- **DBI-5.2** — findings are advisory; they never block or bypass `WriteGate.IWriteGate`.
- **DBI-5.3** — a flag fires when an entity's win-rate delta exceeds **±8%** over
  **≥ 500** runs (both thresholds are tuneable via `BalanceDriftOptions`).

## Architecture

```
RecordOutcome(entityId, kind, won)  ──►  BalanceTelemetryAccumulator
RegisterExpectedWinRate(...)             (per-entity win/run counters)
                                                 │
                                EvaluateDrift()  ▼
                                         BalanceDriftReport
                                          ├─ Findings: BalanceDriftFinding[]
                                          └─ StateHash: deterministic SHA-256
```

| Type | Role |
|------|------|
| `IBalanceTelemetrySink` | Public hook: `RecordOutcome`, `RegisterExpectedWinRate`, `EvaluateDrift`, `ComputeStateHash`. |
| `BalanceTelemetryAccumulator` | Real implementation. Keeps per-`(entityId, kind)` win/run counters in a `SortedDictionary` (ordinal) so output is order-independent. |
| `NoOpBalanceTelemetrySink` | Singleton no-op used when the feature flag is off. Returns `BalanceDriftReport.EmptyDisabled`. |
| `BalanceTelemetrySinkFactory` | `Create(featureFlags, options)` — returns the accumulator when `EnableBalanceDrift` is true, otherwise the no-op singleton. |
| `BalanceDriftFeatureFlags` | `EnableBalanceDrift` — **defaults to `false`**. |
| `BalanceDriftOptions` | `WinRateDriftThreshold` (0.08), `MinimumSampleRuns` (500), `DefaultExpectedWinRate` (0.5). |
| `BalanceDriftReport` | `DriftDetectionEnabled`, `Findings`, `StateHash`. `EmptyDisabled` is the no-op result. |
| `BalanceDriftFinding` | One flag: code `BALANCE_WIN_RATE_DRIFT`, entity, sample count, expected/actual rate, delta, message. |
| `BalanceEntityKind` | `Platform = 0`, `Weapon = 1`. |
| `BalanceTelemetryStateHasher` | Internal. Deterministic SHA-256 over sorted rows + findings for golden tests. |
| `BalanceTelemetryGoldenHashes` | Pinned hashes for CI: `EmptyState` and `GoldenFixtureSequence`. |

## Usage

Always construct through the factory so the feature flag is honored:

```csharp
using ProjectAegis.Data.Telemetry;

var sink = BalanceTelemetrySinkFactory.Create(
    new BalanceDriftFeatureFlags { EnableBalanceDrift = true },
    new BalanceDriftOptions { WinRateDriftThreshold = 0.08, MinimumSampleRuns = 500 });

// Optional: per-entity baseline (defaults to 0.5 otherwise). Must be in [0, 1].
sink.RegisterExpectedWinRate("frigate-baltic", 0.55);

// Feed agent-vs-agent outcomes as they complete.
sink.RecordOutcome("frigate-baltic", BalanceEntityKind.Platform, won: true);

// Read advisory findings (e.g. for an AAR or balance dashboard).
BalanceDriftReport report = sink.EvaluateDrift();
foreach (var f in report.Findings)
{
    // f.Code == "BALANCE_WIN_RATE_DRIFT"
    Console.WriteLine(f.Message); // "Win-rate drift 12.0% exceeds ±8% over 600 runs."
}
```

When the flag is off (the default), `Create()` returns `NoOpBalanceTelemetrySink.Instance`:
`RecordOutcome` is a no-op and `EvaluateDrift()` returns `BalanceDriftReport.EmptyDisabled`
(empty findings, `StateHash == BalanceTelemetryGoldenHashes.EmptyState`).

## Constraints & gotchas

- **Advisory only.** Nothing here writes to the catalog or the write gate. To turn a
  finding into a change you must route it through the normal propose → approve flow
  (DBI-5.4, not yet implemented).
- **Sampling floor.** Entities with fewer than `MinimumSampleRuns` runs are skipped
  entirely — they produce no finding even if their win rate looks extreme.
- **Threshold is exclusive at the band edge.** A delta whose absolute value is `<=`
  `WinRateDriftThreshold` is *not* flagged; only a strictly greater delta is.
- **`RegisterExpectedWinRate` rejects out-of-range input** (`< 0` or `> 1` throws
  `ArgumentOutOfRangeException`); empty/whitespace `entityId` throws `ArgumentException`.
  It also retro-applies to any already-accumulated entity with the same id.
- **`won` is binary.** Draws are not modeled; callers must map them to win/loss.
- **Not yet wired into the sim.** As of Sprint 22 the sink is an emit-only hook with no
  production caller — it is exercised only by `ProjectAegis.Data.Tests/Telemetry`.
  The intended producer is the agent-vs-agent run path; the intended consumer is AAR /
  balance tooling.

## Determinism & golden tests

`ComputeStateHash()` and `EvaluateDrift().StateHash` are byte-stable: rows are sorted
by ordinal `EntityId` then `EntityKind`, and doubles are formatted with the round-trip
(`"R"`) invariant-culture specifier before hashing. This lets CI pin exact hashes.

If you change accumulator output, hashing, or the fixture, the golden test will fail and
you must re-pin the constants in `BalanceTelemetryGoldenHashes`:

| Constant | Scenario |
|----------|----------|
| `EmptyState` | No-op sink / disabled flag (empty input). |
| `GoldenFixtureSequence` | The fixed outcome sequence in `BalanceTelemetryGoldenTests`. |

```bash
# Run just this subsystem's tests
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~Telemetry" -v minimal
```

Covered by `BalanceTelemetryAccumulatorTests`, `BalanceTelemetryGoldenTests`, and
`BalanceTelemetrySinkFactoryTests`.
