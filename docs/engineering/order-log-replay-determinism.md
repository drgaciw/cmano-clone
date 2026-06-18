# Order log & replay determinism — runbook

> **Subsystem:** `ProjectAegis.Delegation.Decision` (the unified order log) +
> `ProjectAegis.Delegation.Replay` (fingerprint + checkpoints)
> **Decisions of record:** [ADR-003 — Unified order log schema](../architecture/adr-003-order-log-schema.md),
> [ADR-004 — Tick pipeline order](../architecture/adr-004-tick-pipeline-order.md)
> **Requirements:** Req 03 / Req 04 (deterministic, replayable sim), Doc 17 (single AAR timeline)
> **Process gate:** [`/replay-verify`](../../.claude/skills/replay-verify/SKILL.md) (determinism-engineer)

The order log is the **single append-only timeline** that records every consequential thing
that happens in a run — agent decisions, engagements, policy denials/updates, contact and
mission transitions, magazine/fuel changes, player orders. It serves two jobs at once:

1. **Explainability / AAR** — the message log and scoring CSV are *projections* of it, never a
   second source of truth (ADR-003).
2. **Determinism gate** — a stable text fingerprint (and its SHA-256) over the whole log proves
   that `(scenario, seed)` reproduces byte-for-byte. `/replay-verify` consumes that fingerprint.

This page documents the implementation as it exists in source today, and — most importantly —
**how to add a new entry kind without silently breaking the replay fingerprint**, which is the
single biggest footgun in this subsystem.

## Pipeline at a glance

```
emit site (sim tick, orchestrator, mission runtime, player order)
        │  Append*(record)  → OrderLogEntryFactories.FromXxx(...)
        ▼
IOrderLog.Append(OrderLogEntry)            ← assigns SequenceId if 0
        │  per-kind backing list (+ optional Hindsight sidecar)
        ▼
DecisionLog.ChronologicalEntries()         → entries OrderBy(SequenceId)
        ▼
DecisionLog.ComputeFingerprint()           → "Kind|Seq|SimTime|payload\n" per row (canonical text)
        ▼
OrderLogReplayFingerprint.ComputeSha256Hex → 64-char lowercase hex digest
        │
        ├─ ReplayCheckpointStore.Record(tick, worldHash, fingerprint, lastSeq)  (every N ticks)
        └─ /replay-verify diffs A vs B vs golden
```

## The interface and its single implementation

`IOrderLog` (`src/ProjectAegis.Delegation/Decision/IOrderLog.cs`) is intentionally tiny:

```csharp
public interface IOrderLog
{
    void Append(OrderLogEntry entry);
    IReadOnlyList<OrderLogEntry> ChronologicalEntries();
    string ComputeFingerprint();
}
```

`DecisionLog` is the only implementation. The orchestrator exposes both faces of the same
object — neighbours should append through `OrderLog`, read AAR detail through `DecisionLog`:

```csharp
public DecisionLog DecisionLog { get; }
public IOrderLog OrderLog => DecisionLog;   // SameAs DecisionLog (see IOrderLogContractTests)
```

An empty log returns an empty chronology and an **empty-string** fingerprint (not a hash of
nothing) — `ComputeFingerprint()` of a fresh log is `""`.

## The row: `OrderLogEntry` (discriminated by `Kind`)

```csharp
public sealed record OrderLogEntry(ulong SequenceId, OrderLogEntryKind Kind, double SimTime, object Payload);
```

`Payload` is a loosely-typed `object` whose concrete record type must match `Kind`. There are
**17** kinds today (`OrderLogEntryKind`, ordinals are stable — always append new kinds at the
end):

| Ordinal | Kind | Payload record | Fingerprint payload fields (`FormatPayload`) |
|--------:|------|----------------|-----------------------------------------------|
| 0 | `AgentDecision` | `AgentDecisionPayload` (or legacy `DecisionRecord`) | `SimTick · AgentId · ChosenOrderKind · ScoredIntents · RngDraw` |
| 1 | `PolicyDenial` | `PolicyDenialRecord` | `TargetId · Reason · AttemptedKind` |
| 2 | `Engagement` | `EngagementRecord` | `SimTick · ShooterTargetId · EngagementId · Launched · AbortReasonCode` |
| 3 | `ControllerChange` | `ControllerChangeRecord` | `TargetId · PreviousKind · NewKind · AgentId?` |
| 4 | `GroupMemberDetach` | `GroupMemberDetachRecord` | `GroupId · UnitId` |
| 5 | `GroupMemberRejoin` | `GroupMemberRejoinRecord` | `GroupId · UnitId` |
| 6 | `MagazineChange` | `MagazineChangeRecord` | `SimTick · ShooterTargetId · MountId · Delta · ReasonCode` |
| 7 | `ContactChange` | `ContactChangeRecord` | `SimTick · ObserverId · ContactId · TargetId · PreviousState · NewState` |
| 8 | `MissionTransition` | `MissionTransitionRecord` | `SimTick · EventId · PhaseCode` |
| 9 | `EventFired` | `EventFiredRecord` | `SimTick · EventId · EventCode` |
| 10 | `EngagementOutcome` | `EngagementOutcomeRecord` | `SimTick · EngagementId · VictimTargetId · OutcomeCode · PkDraw` |
| 11 | `PlayerOrder` | `PlayerOrderRecord` | `SimTick · ResolvedExecuteSimTick · UnitId · Kind · Source` |
| 12 | `PolicyUpdate` | `PolicyUpdateRecord` | `SimTick · PolicySnapshotId · Field · PreviousValue · NewValue` |
| 13 | `ModeChange` | `ModeChangeRecord` | `SimTick · UnitId? · PreviousMode · NewMode` |
| 14 | `CommsStateChange` | `CommsStateChangeRecord` | `SimTick · NodeId · PreviousState · NewState · Reason` |
| 15 | `FuelStateChange` | `FuelStateChangeRecord` | `SimTick · UnitId · PreviousState · NewState · RemainingFuelKg` |
| 16 | `FuelBurn` | `FuelBurnRecord` | `SimTick · UnitId · DeltaKg · RemainingFuelKg` |

## Appending: two surfaces, one path

There are two ways to append, and they converge on the same `Append(OrderLogEntry)`:

- **Typed helpers** (preferred at emit sites): `AppendEngagement`, `AppendPolicyUpdate`,
  `AppendFuelBurn`, … Each wraps the record via `OrderLogEntryFactories.FromXxx(record)` (which
  defaults `sequenceId = 0`) and calls `Append`. `IOrderLog` callers that only have the
  interface use `OrderLogExtensions.AppendContactTransition(...)`.
- **Raw `Append(OrderLogEntry)`** — used when the caller already built an entry (e.g. mission
  runtime emissions that carry their own `SequenceId`).

`Append` does three things (`DecisionLog.Append`):

1. **Sequence assignment.** `sequenceId = entry.SequenceId == 0 ? NextSequence() : entry.SequenceId`.
   `NextSequence()` is `++_sequenceId`, so generated IDs start at **1** — `0` is purely the
   "assign one for me" sentinel and can never collide with a real ID.
2. **Type-checked routing.** A `switch` on `Kind` *with* a payload type guard
   (`case … when entry.Payload is XxxRecord r`) stores `r with { SequenceId = sequenceId }` into
   the matching backing list. `AgentDecision` is special-cased: it accepts both the typed
   `AgentDecisionPayload` and a legacy `DecisionRecord`, and tracks its sequence in a parallel
   `_decisionSequences` list. **A `Kind`/payload mismatch falls through to `default` and throws
   `ArgumentException`** — fail-fast, at runtime.
3. **Hindsight sidecar.** `NotifyHindsight` calls the optional `HindsightHook` *after* the row is
   stored. It is observe-only: it never touches the backing lists, sequence counter, or
   fingerprint. Leaving `HindsightHook` null (the headless/CI default) changes nothing.

## The fingerprint (the contract `/replay-verify` checks)

`ChronologicalEntries()` rebuilds a flat list from every backing list, then sorts
`OrderBy(SequenceId)` — sequence id, not `SimTime`, is the canonical order.

`ComputeFingerprint()` emits one line per entry:

```
{Kind}|{SequenceId}|{SimTime:R}|{FormatPayload(entry)}\n
```

- `SimTime` and every numeric payload field (`RngDraw`, `PkDraw`, `RemainingFuelKg`,
  `DeltaKg`, …) use the **round-trip `"R"`** format specifier — full precision, culture-invariant.
- `OrderLogReplayFingerprint.ComputeSha256Hex(log)` UTF-8 encodes that text, SHA-256s it, and
  returns **lowercase hex, always 64 chars**. Same log ⇒ same hash; one differing field ⇒
  different hash (`OrderLogReplayFingerprintSha256Tests`).

> The fingerprint text is the durable contract. Both the raw text and the SHA-256 are surfaced
> by `BalticReplayHarness.Result` (`Fingerprint`, `FingerprintSha256`) and are what the
> `ReplayGolden*` baselines pin.

## Replay checkpoints (scrub-to-tick)

`ReplayCheckpoint(SimTick, WorldHash, LogFingerprint, LastSequenceId)` is a periodic snapshot
boundary. `ReplayCheckpointStore.Record(...)`:

- is **monotonic / append-only** — it ignores any tick `<=` the last recorded tick.
- pairs the log fingerprint with a `WorldHash` = `SimWorldHash.Combine(simHash, detectionHash, 0)`.
- `FindAtOrBefore(tick)` returns the latest checkpoint at or before a tick (assumes the store
  stays sorted, which the monotonic guard guarantees).

The cadence comes from `ScenarioReplaySettings.CheckpointIntervalTicks` (default **300**). In
`BalticReplayHarness.Run` a checkpoint is recorded when `interval > 0 && simTick % interval == 0`.

`SeededRng` (`src/ProjectAegis.Delegation/Decision/SeededRng.cs`) is the deterministic random
source feeding `RngDraw`/`PkDraw`: a seeded xorshift64 (`globalSeed ^ agentSalt*0x9E3779B9`,
clamped away from 0) producing `[0,1)` doubles. Same seed + same call order ⇒ same draws ⇒ same
fingerprint. Never replace it with `System.Random` or any wall-clock/GUID source.

## Adding a new entry kind (the checklist)

Adding a row type touches **six** places. Miss one of the last two and you get a *silent*
determinism bug — no compile error, but a corrupted or invisible fingerprint:

1. **`OrderLogEntryKind`** — add the value **at the end** (preserve existing ordinals).
2. **`XxxRecord`** — a `sealed record` with `ulong SequenceId`, `double SimTime` (and usually
   `ulong SimTick`) plus the payload fields.
3. **`OrderLogEntryFactories.FromXxx`** — `new(sequenceId, OrderLogEntryKind.Xxx, record.SimTime, record)`.
4. **`DecisionLog`** plumbing: a backing `List<XxxRecord>`, a public `IReadOnlyList<XxxRecord>`
   accessor, a `case OrderLogEntryKind.Xxx when entry.Payload is XxxRecord r` in `Append`, and an
   `AppendXxx(...)` helper.
5. **`ChronologicalEntries`** — add the `foreach` loop that re-emits your rows. **If you skip
   this, the entry is stored but never appears in the timeline or fingerprint at all.**
6. **`FormatPayload`** — add the `OrderLogEntryKind.Xxx when entry.Payload is XxxRecord r => …`
   arm. **If you skip this, the row serialises as `"?"`** — every row of your kind collapses to
   the same text, so distinct events become indistinguishable in the replay hash.

Then extend the golden baselines (`ReplayGoldenTests` and the Baltic `ReplayGolden*` suite) — an
explicit ADR-003 consequence — and treat the baseline change as deliberate (re-record only via
`/replay-verify --record` with approval).

## Pitfalls

- **`FormatPayload` `"?"` fallback is silent.** The `default => "?"` arm means a forgotten case
  compiles and runs, but erases your payload from the fingerprint. Always pair a new `Kind` with a
  `FormatPayload` arm *and* a fingerprint assertion in tests.
- **`ChronologicalEntries` is the gatekeeper.** `Append` succeeding does **not** mean the row is in
  the fingerprint — only kinds with a loop in `ChronologicalEntries` surface. The two methods must
  be kept in lockstep.
- **Order is by `SequenceId`, not `SimTime`.** Two events at the same `SimTime` are ordered by
  append order. Don't rely on `SimTime` for tie-breaks.
- **Reordering the enum is a breaking change** for anything persisting ordinals; the text
  fingerprint uses `Kind.ToString()` (the name), so renames change the fingerprint too. Append, do
  not reshuffle or rename.
- **Empty log fingerprint is `""`**, not a hash — guard callers that assume a 64-char hash.
- **The Hindsight hook is non-authoritative.** It must never feed values back into the log;
  determinism (and `/replay-verify`) ignores it entirely.

## Verify

```bash
# Order-log contract, fingerprint stability, and replay row types (headless, no Unity)
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "OrderLog|Replay|IOrderLog" -v minimal

# Baltic golden replay suite — pins the canonical fingerprints end-to-end
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGolden" -v minimal
```

A SHA-256 fingerprint must be 64 lowercase hex chars and identical across two fresh runs of the
same `(scenario, seed)`. A differing A-vs-B run is release-blocking (see `/replay-verify`).

## Source map

| Concern | File |
|---------|------|
| Append-only log surface | `src/ProjectAegis.Delegation/Decision/IOrderLog.cs` |
| Implementation (append, chronology, fingerprint) | `src/ProjectAegis.Delegation/Decision/DecisionLog.cs` |
| Row type + decision-record adapter | `src/ProjectAegis.Delegation/Decision/OrderLogEntry.cs` |
| Kind enum (17 variants) | `src/ProjectAegis.Delegation/Decision/OrderLogEntryKind.cs` |
| Record → entry factories | `src/ProjectAegis.Delegation/Decision/OrderLogEntryFactories.cs` |
| `IOrderLog` append helpers | `src/ProjectAegis.Delegation/Decision/OrderLogExtensions.cs` |
| Payload records | `src/ProjectAegis.Delegation/Decision/*Record.cs`, `AgentDecisionPayload.cs` |
| Deterministic RNG | `src/ProjectAegis.Delegation/Decision/SeededRng.cs` |
| SHA-256 fingerprint | `src/ProjectAegis.Delegation/Replay/OrderLogReplayFingerprint.cs` |
| Checkpoints | `src/ProjectAegis.Delegation/Replay/ReplayCheckpoint.cs`, `ReplayCheckpointStore.cs` |
| Checkpoint cadence | `src/ProjectAegis.Sim/Scenario/ScenarioReplaySettings.cs` |
| Headless replay runner | `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs` |
| Orchestrator faces | `src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs` (`OrderLog`, `DecisionLog`) |
| Tests | `IOrderLogContractTests`, `OrderLogFingerprintTests`, `OrderLogReplayFingerprintSha256Tests`, `ReplayGoldenTests`, Baltic `ReplayGolden*` suite |

## Related

- [ADR-003 — Unified order log schema](../architecture/adr-003-order-log-schema.md)
- [ADR-004 — Tick pipeline order](../architecture/adr-004-tick-pipeline-order.md)
- [Doctrine inheritance panel — ROE resolution & override](doctrine-inheritance-panel.md) (emits `PolicyUpdate` rows)
- [`/replay-verify` skill](../../.claude/skills/replay-verify/SKILL.md) — the reproducibility gate that consumes this fingerprint
