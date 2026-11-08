# Balance-drift telemetry pipeline — advisory win-rate drift

The **balance-drift telemetry pipeline** is a deterministic, **advisory-only** subsystem that
watches agent-vs-agent win rates and raises a flag when an entity's observed win rate drifts too
far from what the designer expected. It has two independent consumption surfaces that share one
core:

1. **Sim path.** During a headless run, engagement outcomes are folded per shooter into a
   win-rate accumulator; when a scenario opts in, the run carries a live
   [`BalanceDriftReport`](../../src/ProjectAegis.Data/Telemetry/BalanceDriftReport.cs).
2. **Catalog path.** When a catalog import / approve / platform-workbook release runs, the diff's
   entity set is evaluated against the same accumulator and the resulting advisory is attached to
   the command's JSON output.

The one rule that governs the whole subsystem: **it never mutates catalog or world state, never
blocks a commit, and never bypasses the [`CatalogWriteGate`](catalog-write-gate.md)**. It is a
read-side observer. It is also **off by default** — with the feature flag unset every seam
resolves to a no-op that returns a fixed empty-state hash, so replay goldens are untouched.

> **Scope.** This page is the runtime/pipeline deep-dive. The scenario `telemetry` **JSON field
> reference** lives in [scenario-policy-authoring.md](scenario-policy-authoring.md); the catalog
> write workflow it hangs off is in [catalog-write-gate.md](catalog-write-gate.md) and
> [catalog-release-train.md](catalog-release-train.md). The core lives in
> [`ProjectAegis.Data/Telemetry/`](../../src/ProjectAegis.Data/Telemetry/); the sim consumer lives
> in [`ProjectAegis.Sim/Telemetry/`](../../src/ProjectAegis.Sim/Telemetry/). Both are
> engine-agnostic. Story lineage: **DBI-5** (P0 no-op), **S22-06** (sim consumer + goldens),
> **S29-10** (catalog import/approve evaluator), **S31-09** (nightly-approve summary).

---

## Architecture at a glance

```
                         IBalanceTelemetrySink  (the seam)
                                │
             ┌──────────────────┴───────────────────┐
   flag off  │                                       │  flag on
   NoOpBalanceTelemetrySink              BalanceTelemetryAccumulator
   (empty report, EmptyState hash)       (real win-rate rows + ±8% drift findings)
                                                    │
                                          BalanceTelemetryStateHasher
                                          (SHA-256 over sorted rows → golden hash)

  ── Sim path ───────────────────────────────────────────────────────────────
  SimulationSession → BalanceDriftAdvisoryConsumer.RecordEngagementOutcome(shooter, outcome)
      → sink.RecordOutcome(...) → sink.EvaluateDrift() → BalanceDriftReport (LastReport)

  ── Catalog path ───────────────────────────────────────────────────────────
  CmoMarkdownImportProposer / PlatformWorkbookWriteService / catalog_write_approve
      → CatalogBalanceDriftPipelineEvaluator.EvaluateForDiff(settings, diffEntityIds)
      → BalanceDriftReport (filtered to the diff) → advisory note strings in JSON output
```

Everything above the seam is pure and deterministic: given the same recorded outcomes in the same
order, the accumulator produces byte-identical findings and the same SHA-256 state hash.

---

## The sink seam (`IBalanceTelemetrySink`)

[`IBalanceTelemetrySink`](../../src/ProjectAegis.Data/Telemetry/IBalanceTelemetrySink.cs) is the
four-method contract the whole pipeline is written against:

| Method | Purpose |
|--------|---------|
| `RecordOutcome(entityId, entityKind, won)` | Fold one agent-vs-agent result into the running win/total counts for `(entityId, entityKind)`. |
| `RegisterExpectedWinRate(entityId, expectedWinRate)` | Pin the baseline the entity is measured against (must be in `[0, 1]`). |
| `EvaluateDrift()` | Snapshot the current state into a `BalanceDriftReport` (findings + state hash). |
| `ComputeStateHash()` | The deterministic SHA-256 of the current state, for CI golden pinning. |

There are exactly two implementations, chosen by
[`BalanceTelemetrySinkFactory.Create`](../../src/ProjectAegis.Data/Telemetry/BalanceTelemetrySinkFactory.cs)
from a [`BalanceDriftFeatureFlags`](../../src/ProjectAegis.Data/Telemetry/BalanceDriftFeatureFlags.cs):

- **`EnableBalanceDrift == false` (default)** →
  [`NoOpBalanceTelemetrySink.Instance`](../../src/ProjectAegis.Data/Telemetry/NoOpBalanceTelemetrySink.cs).
  Every method is a no-op; `EvaluateDrift()` returns `BalanceDriftReport.EmptyDisabled` and
  `ComputeStateHash()` returns `BalanceTelemetryGoldenHashes.EmptyState`. This is the DBI-5 P0
  guarantee: with the flag unset the subsystem is inert.
- **`EnableBalanceDrift == true`** →
  [`BalanceTelemetryAccumulator`](../../src/ProjectAegis.Data/Telemetry/BalanceTelemetryAccumulator.cs),
  the real implementation described below.

---

## The accumulator math

[`BalanceTelemetryAccumulator`](../../src/ProjectAegis.Data/Telemetry/BalanceTelemetryAccumulator.cs)
keeps one `EntityAccumulator` per `(entityId, entityKind)` key in an **`Ordinal`-sorted**
dictionary (sort stability is what makes the state hash deterministic). Each row tracks `Wins`,
`TotalRuns`, and the `ExpectedWinRate` it is measured against.

**Expected win rate resolution.** When a row is first seen, its baseline is the per-entity
override registered via `RegisterExpectedWinRate`, else
[`BalanceDriftOptions.DefaultExpectedWinRate`](../../src/ProjectAegis.Data/Telemetry/BalanceDriftOptions.cs)
(0.5). Registering an override later also back-patches every existing row with that `entityId`.

**Finding generation** (`EvaluateDrift` → `BuildFindings`), per row, in
`(EntityId Ordinal, EntityKind)` order:

1. Skip rows with `TotalRuns < MinimumSampleRuns` (default **500**) — not enough sample to judge.
2. Compute `drift = ActualWinRate − ExpectedWinRate` where `ActualWinRate = Wins / TotalRuns`.
3. Skip rows where `|drift| <= WinRateDriftThreshold` (default **±0.08 = ±8%**).
4. Otherwise emit a
   [`BalanceDriftFinding`](../../src/ProjectAegis.Data/Telemetry/BalanceDriftFinding.cs) with code
   **`BALANCE_WIN_RATE_DRIFT`**, the sample count, expected/actual/delta, and an invariant-culture
   message like `Win-rate drift +12.0% exceeds ±8% over 600 runs.`

There is currently one finding code and two entity kinds
([`BalanceEntityKind`](../../src/ProjectAegis.Data/Telemetry/BalanceEntityKind.cs)):
`Platform = 0`, `Weapon = 1`.

### Tuneable thresholds (`BalanceDriftOptions`)

| Option | Default | Meaning |
|--------|---------|---------|
| `WinRateDriftThreshold` | `0.08` | `\|actual − expected\|` above which a finding is raised. |
| `MinimumSampleRuns` | `500` | Minimum `TotalRuns` before a row is even evaluated. |
| `DefaultExpectedWinRate` | `0.5` | Baseline used when no per-entity override is registered. |

---

## Determinism & golden hashing

[`BalanceTelemetryStateHasher.Compute`](../../src/ProjectAegis.Data/Telemetry/BalanceTelemetryStateHasher.cs)
builds a tab/newline-delimited text block — first every entity row
(`entityId · kind · wins · total · expectedWinRate` with `"R"` round-trip float formatting under
`InvariantCulture`), then a `--findings--` separator, then every finding — each list re-sorted by
`(EntityId Ordinal, EntityKind, Code)`, and takes a lowercase-hex **SHA-256** of the UTF-8 bytes.
Because the input is fully sorted and culture-invariant, the hash is stable across platforms and
runs.

[`BalanceTelemetryGoldenHashes`](../../src/ProjectAegis.Data/Telemetry/BalanceTelemetryGoldenHashes.cs)
pins two constants used by CI golden tests:

- **`EmptyState`** — the SHA-256 of the empty block (`e3b0c442…b7852b855`), returned by every
  disabled/no-op path so "off" is a single canonical value.
- **`GoldenFixtureSequence`** — the hash produced by the fixed outcome sequence in
  [`BalanceTelemetryGoldenTests`](../../src/ProjectAegis.Data.Tests/Telemetry/BalanceTelemetryGoldenTests.cs);
  if the accumulator math or hash formatting changes, that test fails and the golden must be
  regenerated deliberately. Broader coverage lives in
  [`BalanceTelemetryAccumulatorTests`](../../src/ProjectAegis.Data.Tests/Telemetry/BalanceTelemetryAccumulatorTests.cs)
  and [`CatalogBalanceDriftPipelineTests`](../../src/ProjectAegis.Data.Tests/Telemetry/CatalogBalanceDriftPipelineTests.cs).

This state hash is **separate from** the sim world-state / order-log replay hash
(see [determinism-and-replay.md](determinism-and-replay.md)). Balance telemetry is a read-side
observer and does not feed the replay fingerprint — which is exactly why enabling it cannot move
the Baltic replay goldens.

---

## Sim path — engagement outcomes → drift advisories

[`BalanceDriftAdvisoryConsumer`](../../src/ProjectAegis.Sim/Telemetry/BalanceDriftAdvisoryConsumer.cs)
is the sim-side wrapper. It builds a sink from the scenario's telemetry settings and, when
enabled, registers each configured trial's expected win rate up front. Its hot method:

- `RecordEngagementOutcome(shooterUnitId, outcomeCode)` — a **win is `Kill` or `Hit`**
  (`EngagementOutcomeCodes`), anything else counts as a loss — and records it as a
  `BalanceEntityKind.Platform` outcome for the shooter. When `Enabled` is false, or the shooter id
  is blank, it returns immediately (no sink call, no allocation).
- After each recorded outcome it refreshes `LastReport = sink.EvaluateDrift()`.

**Wiring.** [`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs)
constructs the consumer once from `orchestrator.ScenarioPolicy?.BalanceTelemetry` and, after each
resolved engagement, calls
`BalanceDriftConsumer?.RecordEngagementOutcome(order.Target.Value, result.OutcomeCode)`. When the
scenario doesn't enable telemetry the consumer is present but `Enabled == false`, so the per-engage
call is a cheap guard-and-return.

### Scenario `telemetry` block

The sim path is driven entirely by the scenario policy's optional `telemetry` object, parsed by
[`ScenarioPolicyJsonLoader`](../../src/ProjectAegis.Sim/Scenario/ScenarioPolicyJsonLoader.cs) into
[`ScenarioBalanceTelemetrySettings`](../../src/ProjectAegis.Sim/Scenario/ScenarioBalanceTelemetrySettings.cs).
A live example ships as
[`data/scenarios/baltic-patrol-balance-drift.policy.json`](../../data/scenarios/baltic-patrol-balance-drift.policy.json):

```json
{
  "id": "baltic-patrol-balance-drift",
  "friendlyRoe": "WeaponsFree",
  "opposingRoe": "WeaponsFree",
  "engage": { "rangeMeters": 45000, "defaultMagazineRounds": 4, "hasFireControlTrack": true },
  "telemetry": {
    "enableBalanceDrift": true,
    "minimumSampleRuns": 100,
    "winRateDriftThreshold": 0.08,
    "defaultExpectedWinRate": 0.5,
    "balanceTrials": [
      { "entityId": "u1", "entityKind": "Platform", "expectedWinRate": 0.5 }
    ]
  }
}
```

Field notes (all keys are case-insensitive; the block is entirely optional):

| Key | Maps to | Notes |
|-----|---------|-------|
| `enableBalanceDrift` | `EnableBalanceDrift` | Defaults `false` → consumer is a no-op. |
| `winRateDriftThreshold` / `minimumSampleRuns` / `defaultExpectedWinRate` | `BalanceDriftOptions` | An options object is only built if at least one is set; otherwise defaults (0.08 / 500 / 0.5) apply. |
| `balanceTrials[]` | `ScenarioBalanceTrial` | Per-entity `expectedWinRate` overrides. `entityKind` parses case-insensitively and **throws** `InvalidDataException` on an unknown kind. |

---

## Catalog path — diff advisories on write

The catalog side never records outcomes; it *reads* the accumulator's current findings and filters
them to the entities a given write touches.
[`CatalogBalanceDriftPipelineEvaluator.EvaluateForDiff(settings, diffEntityIds)`](../../src/ProjectAegis.Data/Telemetry/CatalogBalanceDriftPipelineEvaluator.cs)
is the entry point:

1. Disabled settings → `BalanceDriftReport.EmptyDisabled`.
2. Empty/whitespace diff → an enabled-but-empty report with the `EmptyState` hash.
3. Otherwise resolve a sink (injected for tests, else the factory), take its full report, and
   **filter findings to the diff's entity-id set**, re-sorted by `(EntityId, EntityKind, Code)`.
   The state hash is the full report's hash when any finding survives, else `EmptyState`.

`FormatAdvisoryNotes(report)` renders each surviving finding as a stable string
`balance_drift_advisory:{EntityId}:{Code}:{Message}` for embedding in command output.

**Consumers** (all gated on the same `CatalogBalanceDriftPipelineSettings`, default
[`Disabled`](../../src/ProjectAegis.Data/Telemetry/CatalogBalanceDriftPipelineSettings.cs)):

| Consumer | How it surfaces the advisory |
|----------|------------------------------|
| [`catalog_write_approve --enable-balance-drift`](mission-editor-cli.md) CLI ([`CatalogWriteApproveCommand`](../../src/ProjectAegis.MissionEditor.Cli/CatalogWriteApproveCommand.cs)) | Evaluates the batch's staging entity ids and attaches a `balanceDriftAdvisory` object to the JSON payload. |
| [`CmoMarkdownImportProposer`](../../src/ProjectAegis.Data/Import/CmoMarkdownImportProposer.cs) | Returns a `BalanceDriftAdvisory` on its result record for the proposed diff (see [cmo-markdown-import.md](cmo-markdown-import.md)). |
| [`PlatformWorkbookWriteService`](../../src/ProjectAegis.Data/Platform/PlatformWorkbookWriteService.cs) | Adds advisory notes to the write result (see [platform-workbook-roundtrip.md](platform-workbook-roundtrip.md)). |
| [`NightlyApproveBalanceDriftSummary`](../../src/ProjectAegis.Data/Import/NightlyApproveBalanceDriftSummary.cs) | S31-09 nightly-approve JSON summary: opens the write gate read-only, lists one/many batches' staging entity ids, evaluates, and serializes a `NightlyApproveBalanceDriftAdvisoryDto`. |

Each consumer opens a [`CatalogWriteGate`](catalog-write-gate.md) only to *list* staging entity
ids (`ListStagingEntityIds`); it proposes and approves through the normal gate — the advisory is a
side report, not a write.

---

## Invariants — never break these

| Invariant | Why |
|-----------|-----|
| **Advisory-only.** No finding ever blocks a catalog commit or aborts an engagement. | DBI-5.2: telemetry informs, it does not gate. |
| **Never bypasses the write gate.** Catalog paths only *read* staging ids. | Preserves the extend-only [`CatalogWriteGate`](catalog-write-gate.md) contract. |
| **Off by default.** Absent flag → `NoOp` sink → `EmptyState` hash everywhere. | DBI-5 P0: zero behavior change until a scenario/command opts in. |
| **Deterministic state hash.** Sorted rows + `InvariantCulture` `"R"` floats + SHA-256. | CI golden pinning (`BalanceTelemetryGoldenHashes`). |
| **Replay-safe.** The state hash is independent of the sim world/order-log replay hash. | Enabling telemetry cannot move the Baltic v2/v3 replay goldens. |

---

## How to extend it

- **A new tuneable threshold** → add an `init` property to `BalanceDriftOptions`, thread it through
  `BuildFindings`, and add a scenario JSON key in `ScenarioTelemetryJsonDto` +
  `ScenarioPolicyJsonLoader.ParseBalanceTelemetry`. Regenerate `GoldenFixtureSequence` only if the
  fixture's output actually changes, and say so in the commit.
- **A new finding code** → emit a new `BalanceDriftFinding.Code` in `BuildFindings`. Because the
  code participates in the state hash and advisory-note format, add/adjust golden coverage in
  `BalanceTelemetryGoldenTests` and the accumulator tests in the same change.
- **A new entity kind** → append to `BalanceEntityKind` (append-only: the integer value is part of
  keys and the hash). Record outcomes for it via the appropriate consumer.
- **A new catalog consumer** → call `CatalogBalanceDriftPipelineEvaluator.EvaluateForDiff` with the
  write's diff entity ids and surface `FormatAdvisoryNotes` (or the DTO) — never let a finding
  change the write's success/failure.

---

## Related

- [scenario-policy-authoring.md](scenario-policy-authoring.md) — the `telemetry` JSON block in context of the full scenario policy schema.
- [catalog-write-gate.md](catalog-write-gate.md) · [catalog-release-train.md](catalog-release-train.md) — the write workflow the catalog advisories hang off.
- [cmo-markdown-import.md](cmo-markdown-import.md) · [platform-workbook-roundtrip.md](platform-workbook-roundtrip.md) — two catalog consumers that report advisories.
- [determinism-and-replay.md](determinism-and-replay.md) — the separate sim replay hash this subsystem deliberately does not touch.
