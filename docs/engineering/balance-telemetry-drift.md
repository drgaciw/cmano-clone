# Balance telemetry & drift detection — developer reference

Developer reference for the **balance telemetry sink** (`src/ProjectAegis.Data/Telemetry/`) — the
advisory hook that accumulates win/loss outcomes from agent-vs-agent sim runs and flags
**win-rate drift** so the Balance Tuning Agent can propose corrections. Like the
[CMO markdown importer](cmo-markdown-catalog-import.md) and the
[platform Excel round-trip](platform-editor-excel-roundtrip.md), it lives behind the
`ProjectAegis.Data` boundary ([ADR-006](../architecture/adr-006-data-layer-boundary.md)), but unlike
them it is **read-only with respect to the catalog**: it never opens SQLite, never touches
**`IWriteGate`** ([reference](catalog-write-gate.md)), and never proposes or commits anything. It
only *observes* and *reports*. Requirement coverage is
[req-06 §5 — Balance Drift Detection](../../Game-Requirements/requirements/06-Database-Intelligence.md)
(DBI-5).

| Question | Answer |
|----------|--------|
| What does it do? | Accumulates per-entity (platform/weapon) win rates from sim outcomes and emits **advisory** `BALANCE_WIN_RATE_DRIFT` findings when the rate deviates from expected by more than ±8%. |
| Where does the code live? | `src/ProjectAegis.Data/Telemetry/` (sink interface, accumulator, factory, options, finding/report records, state hasher). |
| Does it touch SQLite or the write gate? | **No.** It is advisory-only (DBI-5.2). It does not implement `IWriteGate` and never mutates catalog state — enforced by `BalanceTelemetryAccumulatorTests.Telemetry_does_not_mutate_write_gate_state`. |
| Is it on by default? | **No.** `BalanceDriftFeatureFlags.EnableBalanceDrift` defaults to `false`, so the factory returns a no-op sink (DBI-5 P0 reserved hook). |
| How do I get a sink? | `BalanceTelemetrySinkFactory.Create(featureFlags?, options?)`. |
| Is it wired into the sim yet? | **Not yet.** Only tests construct it today; production wiring (Balance Tuning Agent / sim outcome stream) is post-P0 (DBI-5.4). |

## Why it exists

`req-06` treats the catalog as a self-maintaining product that must stay **balanced** as agents
propose hundreds of new systems. Balance drift detection (§5) is explicitly **out of scope for the
P0 slice** — but P0 reserves a `IBalanceTelemetrySink` no-op so the commit path can compile against
the contract without reading sim telemetry (DBI-5.1). This subsystem fills in the **real**
accumulator behind that contract while keeping the advisory-only guarantee:

- **DBI-5.1** — the catalog commit path never auto-adjusts stats from telemetry. This code has no
  catalog dependency at all.
- **DBI-5.2** — balance findings are *advisory*; they never bypass the write gate. `EvaluateDrift`
  returns a plain report; acting on it is a separate, human-gated proposal.
- **DBI-5.3** — flag when a platform/weapon win-rate delta exceeds **±8%** over **≥ 500**
  agent-vs-agent runs (both tuneable via `BalanceDriftOptions`).

## Components

| Type | Kind | Role |
|------|------|------|
| `IBalanceTelemetrySink` | interface | The contract: `RecordOutcome`, `RegisterExpectedWinRate`, `EvaluateDrift`, `ComputeStateHash`. |
| `BalanceTelemetrySinkFactory` | static | `Create()` → real accumulator when the flag is on, else the no-op singleton. |
| `BalanceDriftFeatureFlags` | class | `EnableBalanceDrift` (default `false`). |
| `BalanceDriftOptions` | class | Tuneables: `WinRateDriftThreshold` (0.08), `MinimumSampleRuns` (500), `DefaultExpectedWinRate` (0.5). |
| `BalanceTelemetryAccumulator` | class | Real sink: ordinal-sorted per-entity win/run counters + drift evaluation. |
| `NoOpBalanceTelemetrySink` | class | Singleton no-op used when the flag is off; `EvaluateDrift` → `BalanceDriftReport.EmptyDisabled`. |
| `BalanceEntityKind` | enum | `Platform = 0`, `Weapon = 1`. |
| `BalanceDriftFinding` | record | One advisory drift result (see below). |
| `BalanceDriftReport` | record | `DriftDetectionEnabled`, `Findings`, `StateHash`. |
| `BalanceTelemetryStateHasher` | static (internal) | Deterministic SHA-256 over sorted rows + findings for CI golden tests. |
| `BalanceTelemetryGoldenHashes` | static | Pinned `EmptyState` and `GoldenFixtureSequence` hashes. |

## Flow

```
sim outcome stream (post-P0)            BalanceTelemetrySinkFactory.Create(flags, options)
        │  win / loss per entity                 │  flag off → NoOpBalanceTelemetrySink (advisory-disabled)
        ▼                                        ▼  flag on  → BalanceTelemetryAccumulator
sink.RecordOutcome(entityId, kind, won)  ──► per-(entityId, kind) Wins / TotalRuns counters
        │                                        │  (optional) sink.RegisterExpectedWinRate(entityId, rate)
        ▼
sink.EvaluateDrift()  ──► BalanceDriftReport { DriftDetectionEnabled, Findings[], StateHash }
        │  for each entity with TotalRuns ≥ MinimumSampleRuns
        │    drift = actual − expected;  |drift| > WinRateDriftThreshold ?  → BALANCE_WIN_RATE_DRIFT finding
        ▼
advisory findings  ──(human-gated, separate)──►  Balance Tuning Agent proposal → IWriteGate staging
```

The arrow into the write gate is **out of band**: this subsystem stops at the report. Nothing here
stages or commits.

## Drift evaluation rules (`BalanceTelemetryAccumulator`)

- **Keying** — outcomes accumulate per `(entityId, entityKind)` pair. The same `entityId` registered
  as both a `Platform` and a `Weapon` is tracked separately.
- **Expected win rate** — resolved when an entity is first seen: a per-entity override from
  `RegisterExpectedWinRate` if present, else `BalanceDriftOptions.DefaultExpectedWinRate` (0.5).
  Calling `RegisterExpectedWinRate` later updates any already-accumulated rows for that id.
  Overrides must be in `[0, 1]`; `RecordOutcome` requires a non-empty `entityId`.
- **Sample floor** — entities with `TotalRuns < MinimumSampleRuns` (default 500) produce **no**
  findings, regardless of how skewed their rate is.
- **Threshold is strict** — a finding fires only when `|actual − expected| > WinRateDriftThreshold`.
  A delta of *exactly* ±8% does **not** fire (`Drift_flag_does_not_fire_at_exactly_eight_percent_band`).
- **Direction-agnostic** — both over-performing (`+`) and under-performing (`−`) entities are
  flagged; `DriftDelta` carries the sign.
- **Deterministic ordering** — entities and findings are emitted sorted by `EntityId`
  (`StringComparer.Ordinal`) then `EntityKind`, so reports and the state hash are stable across runs.

A `BalanceDriftFinding` carries: `Code` (`"BALANCE_WIN_RATE_DRIFT"`), `EntityId`, `EntityKind`,
`SampleRuns`, `ExpectedWinRate`, `ActualWinRate`, `DriftDelta`, and a culture-invariant `Message`
(e.g. `Win-rate drift 20.0% exceeds ±8% over 100 runs.`).

## Usage

```csharp
using ProjectAegis.Data.Telemetry;

// Off by default → no-op sink, EvaluateDrift() reports DriftDetectionEnabled = false.
var sink = BalanceTelemetrySinkFactory.Create();

// Turn it on with custom thresholds.
var live = BalanceTelemetrySinkFactory.Create(
    new BalanceDriftFeatureFlags { EnableBalanceDrift = true },
    new BalanceDriftOptions { WinRateDriftThreshold = 0.08, MinimumSampleRuns = 500 });

live.RegisterExpectedWinRate("su-27-flanker", 0.55); // optional per-entity baseline

foreach (var run in agentVsAgentRuns)
{
    live.RecordOutcome(run.EntityId, BalanceEntityKind.Platform, won: run.Won);
}

BalanceDriftReport report = live.EvaluateDrift();
foreach (var finding in report.Findings)
{
    // advisory only — feed to the Balance Tuning Agent, never commit directly
    Console.WriteLine($"{finding.EntityId}: {finding.Message}");
}
```

## Determinism & golden hashes

`ComputeStateHash()` (and `BalanceDriftReport.StateHash`) is a lowercase SHA-256 over the sorted
accumulator rows (`entityId`, kind, wins, total runs, expected rate) followed by the sorted findings,
all formatted with `InvariantCulture` (`"R"` round-trip for doubles). This makes the sink's state
reproducible for CI golden tests:

- `BalanceTelemetryGoldenHashes.EmptyState` — the empty/disabled state (the well-known SHA-256 of the
  empty input, `e3b0c442…`), returned by the no-op sink and any accumulator with no recorded outcomes.
- `BalanceTelemetryGoldenHashes.GoldenFixtureSequence` — the pinned hash for the fixture sequence in
  `BalanceTelemetryGoldenTests`. If you change the accumulator's state layout, the hash format, or the
  fixture, that test fails — update the pinned constant deliberately, not reflexively.

> Determinism note: the state hash is a CI artifact, **not** something to call inside a simulation
> `Tick()`. The sink itself is allocation-light but the SHA-256 in `ComputeStateHash`/`EvaluateDrift`
> is a per-evaluation cost — evaluate at run-batch boundaries, not per outcome.

## Tests

| Test | Asserts |
|------|---------|
| `BalanceTelemetrySinkFactoryTests` | Flag off → `NoOpBalanceTelemetrySink`; flag on → real accumulator. |
| `BalanceTelemetryAccumulatorTests` | Disabled sink never accumulates; sample floor; ±8% strict band; negative drift; per-entity expected rate; and that telemetry does not implement `IWriteGate` or mutate gate state. |
| `BalanceTelemetryGoldenTests` | Fixture sequence produces the pinned, stable `GoldenFixtureSequence` hash and a `fixture-weapon` finding. |

Run them with:

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter Telemetry
```

## Common pitfalls

- **It's off until you flag it on.** `Create()` with no arguments returns the no-op sink;
  `EvaluateDrift().DriftDetectionEnabled` is `false` and `Findings` is empty. Pass
  `EnableBalanceDrift = true` to accumulate.
- **No findings below the sample floor.** With the default `MinimumSampleRuns = 500`, fewer than 500
  runs for an entity yields zero findings even at a 100% win rate. Lower `MinimumSampleRuns` in tests.
- **Exactly ±8% is *not* drift.** The comparison is strictly greater-than the threshold; tune
  `WinRateDriftThreshold` if you need inclusive behaviour.
- **Advisory only — never auto-commit.** Findings are inputs to a human-/agent-gated proposal that
  still flows through `IWriteGate` (DBI-5.2/5.4). Do not wire `EvaluateDrift` output into the commit
  path; that would violate the data-layer boundary.
- **Set expected rates before (or while) recording.** A new entity captures its expected rate on
  first outcome; `RegisterExpectedWinRate` afterward back-fills existing rows, but registering up
  front keeps intent obvious. Overrides outside `[0, 1]` throw.
- **Don't hand-edit the golden hashes.** A changed `GoldenFixtureSequence` means the state layout or
  fixture moved — confirm the change is intended, then repin.

## Related

- [Catalog write gate — developer reference](catalog-write-gate.md)
- [CMO markdown catalog import — developer reference](cmo-markdown-catalog-import.md)
- [Platform editor — Excel round-trip runbook](platform-editor-excel-roundtrip.md)
- [ADR-006 — Data layer boundary](../architecture/adr-006-data-layer-boundary.md)
- Requirement [06 §5 — Balance Drift Detection](../../Game-Requirements/requirements/06-Database-Intelligence.md)
- Code: `src/ProjectAegis.Data/Telemetry/`; tests: `src/ProjectAegis.Data.Tests/Telemetry/`
