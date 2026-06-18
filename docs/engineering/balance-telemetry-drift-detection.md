# Balance telemetry — win-rate drift detection

> **Engineering reference + runbook.** This page documents how the balance drift subsystem (`src/ProjectAegis.Data/Telemetry/`) behaves today and how to drive it. Scope and acceptance criteria live in [Req 06 — Database Intelligence §5 (DBI-5)](../../Game-Requirements/requirements/06-Database-Intelligence.md); the P0 design rationale is in [`docs/superpowers/specs/2026-05-30-database-intelligence-p0-design.md`](../superpowers/specs/2026-05-30-database-intelligence-p0-design.md).

Balance telemetry watches **agent-vs-agent simulation outcomes** and raises an **advisory flag** when a platform's or weapon's observed win rate drifts more than **±8%** from its expected baseline over a meaningful sample. It is deliberately **advisory only**: it never mutates the catalog and never touches `IWriteGate`. Findings are inputs for the Balance Tuning Agent / a human, not automatic edits (DBI-5.2).

## Intent

| Goal | How it is met |
|------|---------------|
| Detect over-/under-powered entities | Per-entity win-rate accumulator with a tuneable drift threshold (`BalanceDriftOptions`) |
| Never auto-balance | The sink has **no** path to live tables and does not implement `IWriteGate`; `EvaluateDrift` returns a read-only report (DBI-5.1, DBI-5.2) |
| Off by default | `BalanceTelemetrySinkFactory` returns a no-op sink unless `EnableBalanceDrift` is set (DBI-5 is post-P0) |
| Deterministic + CI-testable | Sorted accumulators, `InvariantCulture` formatting, and a SHA-256 state hash drive golden tests |
| Engine-free (ADR-006) | Lives in `ProjectAegis.Data` with no spreadsheet/engine/sim dependency; sim feeds it through the `IBalanceTelemetrySink` port |

## Architecture at a glance

```
agent-vs-agent run ──► IBalanceTelemetrySink.RecordOutcome(entityId, kind, won)
                                 │
            BalanceTelemetrySinkFactory.Create(featureFlags, options)
                       ├─ EnableBalanceDrift == false ──► NoOpBalanceTelemetrySink  (default; records nothing)
                       └─ EnableBalanceDrift == true  ──► BalanceTelemetryAccumulator
                                                                │  (in-memory, per-entity tallies)
                              EvaluateDrift() ──► BalanceDriftReport
                                                  ├─ DriftDetectionEnabled
                                                  ├─ Findings: BalanceDriftFinding[]   (advisory)
                                                  └─ StateHash (SHA-256, for golden tests)
```

The accumulator holds state in memory only — there is no persistence and no commit. A consumer calls `RecordOutcome` once per simulated engagement, then calls `EvaluateDrift()` (or `ComputeStateHash()`) to read the current picture.

### Key types (`src/ProjectAegis.Data/Telemetry/`)

| Type | Responsibility |
|------|----------------|
| `IBalanceTelemetrySink` | Port the sim writes to: `RecordOutcome`, `RegisterExpectedWinRate`, `EvaluateDrift`, `ComputeStateHash`. Advisory only — never mutates catalog or bypasses `IWriteGate` |
| `NoOpBalanceTelemetrySink` | Default singleton when the feature flag is off; records nothing, always returns `BalanceDriftReport.EmptyDisabled` |
| `BalanceTelemetryAccumulator` | Real per-entity win/total tallies; produces findings and the deterministic state hash |
| `BalanceTelemetrySinkFactory` | Selects no-op vs real accumulator from `BalanceDriftFeatureFlags` |
| `BalanceDriftFeatureFlags` | `EnableBalanceDrift` (default `false`) |
| `BalanceDriftOptions` | Tuneable thresholds: `WinRateDriftThreshold` (0.08), `MinimumSampleRuns` (500), `DefaultExpectedWinRate` (0.5) |
| `BalanceEntityKind` | `Platform = 0`, `Weapon = 1` |
| `BalanceDriftFinding` | One advisory drift record (code, entity, samples, expected/actual/delta, message) |
| `BalanceDriftReport` | `DriftDetectionEnabled` + ordered `Findings` + `StateHash` |
| `BalanceTelemetryStateHasher` | Internal SHA-256 over sorted accumulator rows + findings for golden stability |
| `BalanceTelemetryGoldenHashes` | Pinned hashes (`EmptyState`, `GoldenFixtureSequence`) for CI scenarios |

## Drift evaluation rules

An entity is keyed by `(EntityId, EntityKind)`. For each entity, `EvaluateDrift()` (and `BuildFindings`) applies, in order:

1. **Sample gate.** Skip entities with `TotalRuns < MinimumSampleRuns` (default 500). Too little data ⇒ no finding (DBI-5.3's "≥ 500 agent-vs-agent runs").
2. **Drift band.** Compute `drift = actualWinRate − expectedWinRate`. Skip when `Math.Abs(drift) <= WinRateDriftThreshold` (default 0.08). The band is **inclusive** — a delta of exactly ±8% does *not* flag.
3. **Flag.** Otherwise emit a `BALANCE_WIN_RATE_DRIFT` finding (positive delta = over-powered, negative = under-powered).

`actualWinRate = Wins / TotalRuns` (0 when no runs). Findings are sorted by `(EntityId, EntityKind)` for deterministic output.

### Expected win rate

- The baseline is `BalanceDriftOptions.DefaultExpectedWinRate` (0.5) unless overridden.
- `RegisterExpectedWinRate(entityId, rate)` sets a per-entity baseline (must be in `[0, 1]`); it applies to future records **and** retroactively updates already-tracked accumulators for that id.
- Example: with an expected rate of 0.7, an observed 0.79 yields `DriftDelta = 0.09`, which exceeds 0.08 and flags.

## Determinism constraints (read before editing this code)

This subsystem feeds golden tests; non-determinism breaks them.

- **Sorted state.** Entities and findings are kept in `SortedDictionary`/`OrderBy` with `StringComparer.Ordinal`, then by `(int)EntityKind`. Preserve this ordering or the golden hash changes.
- **Always `InvariantCulture`.** Win rates and deltas are formatted with `"R"` + `CultureInfo.InvariantCulture` inside the hasher and the finding message. Locale drift is the most likely correctness bug.
- **State hash = SHA-256, lowercase hex.** `BalanceTelemetryStateHasher` serializes `EntityId, Kind, Wins, TotalRuns, ExpectedWinRate`, a `--findings--` separator, then each finding. `ComputeStateHash()` and `EvaluateDrift().StateHash` must agree.
- **No wall clock / no RNG.** The accumulator is pure tallying; outcomes are supplied by the caller.

## Wiring status (important)

- **Off by default.** `BalanceTelemetrySinkFactory.Create()` with default flags returns `NoOpBalanceTelemetrySink.Instance`. A disabled sink ignores `RecordOutcome` entirely and reports `DriftDetectionEnabled == false` with `StateHash == BalanceTelemetryGoldenHashes.EmptyState`.
- **Not yet bound to a live sim loop.** Today the sink is exercised only by `ProjectAegis.Data.Tests` and referenced in sprint/QA planning docs; no production caller feeds `RecordOutcome` from a real agent-vs-agent run. The port and accumulator exist so the post-P0 sim integration (DBI-5.3/5.4) and Balance Tuning Agent can plug in without touching the commit path.
- **Catalog isolation is enforced by design and test.** `BalanceTelemetryAccumulator` does not implement `IWriteGate`; `BalanceTelemetryAccumulatorTests.Telemetry_does_not_mutate_write_gate_state` asserts the type carries no gate interface and that evaluating drift leaves gate batches untouched.

## Usage

```csharp
// Enable + tune (post-P0; default is a no-op sink)
var sink = BalanceTelemetrySinkFactory.Create(
    featureFlags: new BalanceDriftFeatureFlags { EnableBalanceDrift = true },
    options: new BalanceDriftOptions
    {
        WinRateDriftThreshold = 0.08,   // ±8% advisory band (DBI-5.3)
        MinimumSampleRuns = 500,        // ignore entities below this sample
        DefaultExpectedWinRate = 0.5,
    });

// Optional per-entity baseline (otherwise DefaultExpectedWinRate is used)
sink.RegisterExpectedWinRate("F-18E", expectedWinRate: 0.55);

// One call per simulated engagement
sink.RecordOutcome("F-18E", BalanceEntityKind.Platform, won: true);

// Read the advisory picture — no catalog side effects
BalanceDriftReport report = sink.EvaluateDrift();
foreach (var f in report.Findings)
{
    // f.Code == "BALANCE_WIN_RATE_DRIFT"; f.DriftDelta > 0 ⇒ over-powered
    Console.WriteLine(f.Message);
}
```

## Common pitfalls

- **No findings appear.** Most often the entity has fewer than `MinimumSampleRuns` outcomes, or the feature flag is off (no-op sink silently drops every `RecordOutcome`). Check `report.DriftDetectionEnabled`.
- **Exactly ±8% doesn't flag.** The threshold is inclusive (`Math.Abs(drift) <= threshold`); only deltas strictly beyond the band flag. Lower `WinRateDriftThreshold` if you need tighter sensitivity.
- **Golden hash drift in CI.** Any change to ordering, the hashed fields, or number formatting changes `StateHash`. If intentional, re-pin `BalanceTelemetryGoldenHashes.GoldenFixtureSequence`; if not, you introduced non-determinism.
- **Expecting a staged proposal.** This subsystem never proposes or commits. Turning a finding into a catalog change is a separate, human-gated step (DBI-5.4, via the same write gate as the Platform Editor — see [`platform-editor-excel-roundtrip.md`](platform-editor-excel-roundtrip.md)).
- **`RegisterExpectedWinRate` out of range.** Values outside `[0, 1]` throw `ArgumentOutOfRangeException`; empty/whitespace `entityId` throws `ArgumentException`.

## Where things live

| Path | Content |
|------|---------|
| `src/ProjectAegis.Data/Telemetry/` | Port, no-op + real sink, options, findings, report, state hasher, golden hashes |
| `src/ProjectAegis.Data.Tests/Telemetry/` | Accumulator behavior, factory selection, and golden-hash tests |
| `Game-Requirements/requirements/06-Database-Intelligence.md` | DBI-5 acceptance criteria |
| `docs/superpowers/specs/2026-05-30-database-intelligence-p0-design.md` | P0 design + Resolved Design Decisions §3 |

## Verify

```bash
# Engine-free telemetry tests (no ClosedXML / engine needed)
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "Telemetry" -v minimal

# Golden-hash stability only
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "BalanceTelemetryGoldenTests" -v minimal
```
