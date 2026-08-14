# Order log runtime — developer guide

The **order log** (`DecisionLog`, `ProjectAegis.Delegation/Decision/`) is the single append-only timeline
that records *everything the simulation decided and did* during a run: agent decisions, policy denials,
engagements and their outcomes, contact-picture transitions, controller/group changes, mission and event
firings, comms/fuel/damage/ordnance state changes, and player orders. It is the **source of truth** that
almost every other subsystem reads from — the projection layer draws the tactical picture off it
([c2-projection-layer.md](c2-projection-layer.md)), the trust/XP surface aggregates it
([trust-signal-emit-surface.md](trust-signal-emit-surface.md)), the Hindsight sidecar streams it
([hindsight-session-memory-sidecar.md](hindsight-session-memory-sidecar.md)), and the replay determinism
gate hashes it ([determinism-and-replay.md](determinism-and-replay.md)).

This guide documents the log *as a runtime data structure*: the discriminated-union row model, the append +
sequence mechanics, the read/write surface split, the deterministic fingerprint, who produces it, and — most
importantly — **how to add a new entry kind without breaking replay goldens**. The *design decision* behind
it is [ADR-003 (unified order-log schema)](../architecture/adr-003-order-log-schema.md); the read-only AAR
boundary is [ADR-019](../architecture/adr-019-agentic-aar-readonly-order-log.md). It is verified against
source and pinned by the tests listed at the end.

- **The log:** [`DecisionLog`](../../src/ProjectAegis.Delegation/Decision/DecisionLog.cs) — the concrete
  append-only implementation.
- **Row model:** [`OrderLogEntry`](../../src/ProjectAegis.Delegation/Decision/OrderLogEntry.cs)
  (`(SequenceId, Kind, SimTime, Payload)`) + the
  [`OrderLogEntryKind`](../../src/ProjectAegis.Delegation/Decision/OrderLogEntryKind.cs) enum (19 variants).
- **Factories:** [`OrderLogEntryFactories`](../../src/ProjectAegis.Delegation/Decision/OrderLogEntryFactories.cs)
  — the C1 `From*` helpers that wrap each typed record into an `OrderLogEntry`.
- **Surface split:** [`IOrderLog`](../../src/ProjectAegis.Delegation/Decision/IOrderLog.cs) (adds `Append`)
  over [`IReadOnlyOrderLog`](../../src/ProjectAegis.Delegation/Decision/IReadOnlyOrderLog.cs) (read-only,
  handed to AAR / analysis consumers).
- **Fingerprint:** `DecisionLog.ComputeFingerprint()` (canonical text) →
  [`OrderLogReplayFingerprint.ComputeSha256Hex`](../../src/ProjectAegis.Delegation/Replay/OrderLogReplayFingerprint.cs)
  (SHA-256), using [`FingerprintFloat`](../../src/ProjectAegis.Delegation/Decision/FingerprintFloat.cs) /
  [`ScoredIntentFingerprint`](../../src/ProjectAegis.Delegation/Decision/ScoredIntentFingerprint.cs) for
  culture-invariant float / intent formatting.
- **Owner + producers:** [`DelegationOrchestrator`](../../src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs)
  owns the instance (`OrderLog` / `DecisionLog`); the sim-side rows are appended by
  [`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs) and the
  [`BalticReplayHarness`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs).

---

## Design invariants — never break these

These are load-bearing and enforced by tests. Preserve them when adding an entry kind or a producer.

| Invariant | Rule |
|-----------|------|
| **Append-only, monotonic sequence** | Every row gets a strictly increasing `ulong` `SequenceId` (from `NextSequence()` when the incoming `SequenceId == 0`). Rows are never mutated or removed after append. `SequenceId` — not wall-clock and not `SimTime` — is the canonical timeline order. |
| **`SimTime` can tie; `SequenceId` breaks the tie** | Many rows share a tick (`SimTime`). The chronological view and the fingerprint both order strictly by `SequenceId`, so two same-tick rows always hash in append order, deterministically. |
| **Fingerprint is byte-identical for identical runs** | `ComputeFingerprint()` must return the exact same string for the same `(scenario, seed)` run. This is *the* replay determinism invariant — the Baltic v2 hash `17144800277401907079` is derived from it. Two calls on the same log return the same bytes. |
| **Every float goes through `FingerprintFloat`** | Any floating-point field that reaches the fingerprint (RNG draws, Pk, HP%, fuel kg) must be formatted with `FingerprintFloat.Format` / `.Time`, never raw `ToString()`. Culture / negative-zero drift here silently breaks goldens. |
| **Read consumers take `IReadOnlyOrderLog`** | AAR / analysis / projection code that must not mutate the log takes `IReadOnlyOrderLog` (no `Append`). This makes the read-only guarantee *structural* (ADR-019), so nothing downstream can desync the fingerprint. Never widen an AAR signature to `IOrderLog` or the concrete `DecisionLog`. |
| **Message log is a projection, not a second log** | Human-readable / UI text is derived from the order log by a projection (`MessageLogProjection`), never stored as its own authoritative timeline (ADR-003). |
| **The sidecar hook is inert to hashing** | `HindsightHook` is notified *after* append and must never influence `SequenceId`, ordering, or the fingerprint. It is a fire-and-forget observer. |

---

## The row model

Every entry is one immutable `OrderLogEntry` record:

```csharp
public sealed record OrderLogEntry(ulong SequenceId, OrderLogEntryKind Kind, double SimTime, object Payload);
```

`Payload` is a discriminated union keyed by `Kind` (ADR-003): the runtime type of `Payload` is fixed per
`Kind`. `DecisionLog.Append` pattern-matches on `Kind` **and** the payload type (`when entry.Payload is …`),
routes the row into its typed backing list, stamps the resolved `SequenceId`, and adds it to the single
`_chronological` list. An unknown `Kind` (or a `Kind`/payload mismatch) throws `ArgumentException` — there is
no silent drop.

### The 19 entry kinds

`OrderLogEntryKind` is a stable, explicitly-numbered enum (the numbers are part of the contract — never
renumber or reuse a value). Each kind has one payload record, at least one producer, and a fixed set of
fingerprint fields.

| # | `Kind` | Payload record | Typed accessor | Fingerprint fields (after `Kind\|SequenceId\|SimTime`) |
|---|--------|----------------|----------------|--------------------------------------------------------|
| 0 | `AgentDecision` | `AgentDecisionPayload` (legacy `DecisionRecord` auto-migrated) | `Records` | `SimTick\|AgentId\|ChosenOrderKind\|ScoredIntents\|RngDraw` |
| 1 | `PolicyDenial` | `PolicyDenialRecord` | `PolicyDenials` | `TargetId\|Reason\|AttemptedKind` |
| 2 | `Engagement` | `EngagementRecord` | `Engagements` | `SimTick\|ShooterTargetId\|EngagementId\|Launched\|AbortReasonCode` |
| 3 | `ControllerChange` | `ControllerChangeRecord` | `ControllerChanges` | `TargetId\|PreviousKind\|NewKind\|AgentId?` |
| 4 | `GroupMemberDetach` | `GroupMemberDetachRecord` | `GroupMemberDetaches` | `GroupId\|UnitId` |
| 5 | `GroupMemberRejoin` | `GroupMemberRejoinRecord` | `GroupMemberRejoins` | `GroupId\|UnitId` |
| 6 | `MagazineChange` | `MagazineChangeRecord` | `MagazineChanges` | `SimTick\|ShooterTargetId\|MountId\|Delta\|ReasonCode` |
| 7 | `ContactChange` | `ContactChangeRecord` | `ContactChanges` | `SimTick\|ObserverId\|ContactId\|TargetId\|PreviousState\|NewState` |
| 8 | `MissionTransition` | `MissionTransitionRecord` | `MissionTransitions` | `SimTick\|EventId\|PhaseCode` |
| 9 | `EventFired` | `EventFiredRecord` | `EventFired` | `SimTick\|EventId\|EventCode` |
| 10 | `EngagementOutcome` | `EngagementOutcomeRecord` | `EngagementOutcomes` | `SimTick\|EngagementId\|VictimTargetId\|OutcomeCode\|PkDraw` |
| 11 | `PlayerOrder` | `PlayerOrderRecord` | `PlayerOrders` | `SimTick\|ResolvedExecuteSimTick\|UnitId\|Kind\|Source` |
| 12 | `PolicyUpdate` | `PolicyUpdateRecord` | `PolicyUpdates` | `SimTick\|PolicySnapshotId\|Field\|PreviousValue\|NewValue` |
| 13 | `ModeChange` | `ModeChangeRecord` | `ModeChanges` | `SimTick\|UnitId?\|PreviousMode\|NewMode` |
| 14 | `CommsStateChange` | `CommsStateChangeRecord` | `CommsStateChanges` | `SimTick\|NodeId\|PreviousState\|NewState\|Reason` |
| 15 | `FuelStateChange` | `FuelStateChangeRecord` | `FuelStateChanges` | `SimTick\|UnitId\|PreviousState\|NewState\|RemainingFuelKg` |
| 16 | `FuelBurn` | `FuelBurnRecord` | `FuelBurns` | `SimTick\|UnitId\|DeltaKg\|RemainingFuelKg` |
| 17 | `PlatformDamageChange` | `PlatformDamageChangeRecord` | `PlatformDamageChanges` | `SimTick\|UnitId\|PreviousHpPct\|NewHpPct\|ReasonCode\|DamageLevel` |
| 18 | `OrdnanceStateChange` | `OrdnanceStateChangeRecord` | `OrdnanceStateChanges` | `SimTick\|UnitId\|PreviousState\|NewState\|RoundsRemaining` |

Notes:

- **`AgentDecision` has a legacy path.** For back-compat, `Append` accepts either the modern
  `AgentDecisionPayload` **or** a legacy `DecisionRecord`; the latter is migrated in place via
  `AgentDecisionPayload.FromDecisionRecord`. New code should append the payload (or use `OrderLogEntry.FromDecisionRecord`).
  `Records` re-materializes `DecisionRecord`s from the stored payloads on each access (it is a snapshot, not a
  live view).
- **Float fields are formatted, not raw.** `RngDraw`, `PkDraw`, `RemainingFuelKg`, `DeltaKg`, and the HP%
  fields all go through `FingerprintFloat.Format`; `SimTime` goes through `FingerprintFloat.Time`; scored
  intents through `ScoredIntentFingerprint.Format`. See [determinism-and-replay.md](determinism-and-replay.md)
  for the invariant-culture / negative-zero rules.

---

## Append & sequence mechanics

`Append(OrderLogEntry)` is the single write door:

1. **Resolve the sequence.** `sequenceId = entry.SequenceId == 0 ? NextSequence() : entry.SequenceId`. Most
   producers pass `0` and let the log assign the next monotonic id; the `From*` factories default
   `sequenceId` to `0` for exactly this reason.
2. **Route by `Kind` + payload type.** The `switch` adds the row to its typed backing list (stamping
   `record with { SequenceId = sequenceId }` so the typed record carries the resolved id) and calls
   `AppendChronologicalEntry`.
3. **Keep `_chronological` sorted by `SequenceId`.** The common case (new id ≥ last) is an O(1) append; an
   out-of-order id is binary-search-inserted. Because ids are normally handed out monotonically, this is an
   append in practice.
4. **Notify the sidecar last.** `NotifyHindsight` fires the `HindsightHook` (if any) with the resolved
   sequence — after all state is committed, never affecting ordering or the hash.

Typed convenience appenders wrap the factories so callers don't build `OrderLogEntry` by hand, e.g.:

```csharp
log.AppendEngagement(engagementRecord);      // → OrderLogEntryFactories.FromEngagement → Append
log.AppendContactChange(contactRecord);      // contact-picture transition
log.AppendFuelStateChange(fuelChangeRecord); // NOMINAL/JOKER/BINGO band change
```

`DecisionLog.NextSequence()` is `internal`; sequence assignment is owned by the log, not the caller.

---

## Read surfaces

There are three ways to read the log, in increasing structure:

- **`ChronologicalEntries()`** — the single unified `IReadOnlyList<OrderLogEntry>` timeline, `SequenceId`-sorted
  (ADR-003 MVP). This is what the fingerprint and most whole-run consumers walk.
- **Typed accessors** — `Records`, `Engagements`, `ContactChanges`, `FuelBurns`, … : per-kind
  `IReadOnlyList<T>` views for consumers that only care about one row type (e.g. a projection that only needs
  engagements).
- **`ComputeFingerprint()`** — the canonical newline-delimited text (see below).

The interface split enforces who may write:

| Interface | Members | Hand to |
|-----------|---------|---------|
| `IReadOnlyOrderLog` | `ChronologicalEntries()`, `ComputeFingerprint()`, `Records`, `PolicyDenials`, `Engagements`, `ControllerChanges` | AAR / analysis / projection consumers (ADR-019) — **cannot** append |
| `IOrderLog` | everything above **+** `Append(OrderLogEntry)` | the orchestrator and sim producers only |

`DelegationOrchestrator` exposes both: `DecisionLog` (concrete) for its own producers and `OrderLog`
(`IOrderLog`) as the append surface; AAR-facing APIs must take `IReadOnlyOrderLog`.

---

## The fingerprint

`ComputeFingerprint()` builds one line per chronological entry:

```
{Kind}|{SequenceId}|{FingerprintFloat.Time(SimTime)}|{payload fields}\n
```

`OrderLogReplayFingerprint.ComputeSha256Hex(log)` then UTF-8-encodes that text and returns the lowercase
SHA-256 hex. The full text captures the *decisions and events*, not just end-state — two runs that reach the
same world but decided differently will diverge here. The golden workflow (pin `WORLD_HASH`,
`DETECTION_WORLD_HASH`, and optionally `FINGERPRINT_SHA256`) and the debugging playbook live in
[determinism-and-replay.md](determinism-and-replay.md) and [baltic-replay-harness.md](baltic-replay-harness.md).

---

## Producers & consumers

**Producers (append `IOrderLog`):**

- `DelegationOrchestrator` — the delegation-side rows: agent decisions, policy denials, controller changes,
  group detach/rejoin, mode changes (see [agent-decision-pipeline.md](agent-decision-pipeline.md),
  [autonomy-roe-gating.md](autonomy-roe-gating.md), [direct-control-override-runtime.md](direct-control-override-runtime.md)).
- `SimulationSession` — the sim-side rows appended around each tick: engagements + outcomes, magazine and
  ordnance changes, contact-picture transitions, mission/event firings, comms/fuel/damage state changes
  (see [engagement-pipeline.md](engagement-pipeline.md), [detection-pipeline.md](detection-pipeline.md),
  [comms-degradation-runtime.md](comms-degradation-runtime.md), [logistics-fuel-runtime.md](logistics-fuel-runtime.md),
  [catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md)).
- `BalticReplayHarness` — the headless runner that drives the above for goldens / the QA Gauntlet.
- `DelegationBridge` / `DoctrineOverrideCommand` / `PlayModeSmokeOrbatSeeder` (UnityAdapter) — player-order
  ingress, ROE `PolicyUpdate` overrides, and smoke-harness seeding, respectively.

**Consumers (read `IReadOnlyOrderLog` / typed views):**

- The **projection layer** — turns the log into UI view models ([c2-projection-layer.md](c2-projection-layer.md));
  the message log is one such projection.
- The **trust/XP emit surface** — aggregates per-agent metrics post-run ([trust-signal-emit-surface.md](trust-signal-emit-surface.md)).
- The **replay fingerprint / golden gate** ([determinism-and-replay.md](determinism-and-replay.md)).
- The optional **Hindsight sidecar** via `HindsightHook` ([hindsight-session-memory-sidecar.md](hindsight-session-memory-sidecar.md)).

---

## Adding a new entry kind (runbook)

Adding a row kind touches the fingerprint, so it **will** move the Baltic v2 hash unless the new kind never
fires in the v2 scenarios (prefer the latter, or gate it behind a scenario feature). Steps:

1. **Append the enum value at the end.** Add the next number to `OrderLogEntryKind`. Never renumber, reorder,
   or reuse an existing value — the numbers are part of the on-disk contract.
2. **Add the payload record.** A `sealed record` with a leading `ulong SequenceId` (stamped at append via
   `with { SequenceId = … }`) and a `double SimTime`. Put every float behind `FingerprintFloat`.
3. **Wire `DecisionLog`.** Add a backing `List<T>`, an `IReadOnlyList<T>` accessor, a `case` in `Append`
   (route + stamp + `AppendChronologicalEntry`), and a `FormatPayload` arm listing the fields in a fixed
   order. Add a typed `Append{Kind}` convenience method.
4. **Add the factory.** A `From{Kind}` helper in `OrderLogEntryFactories` (default `sequenceId = 0`).
5. **Decide the read surface.** If AAR / projections need the new kind, expose the typed accessor; only add it
   to `IReadOnlyOrderLog` if it belongs on the shared read contract (ADR-019 — additive read-only members are
   expected; a mutating member is not).
6. **Pin it with tests.** Follow the per-kind fixtures below: a round-trip through `Append`/accessor and a
   fingerprint-stability assertion.
7. **Before landing:** run the suites below plus the full solution suite (`dotnet test`), then confirm the
   golden posture per [AGENTS.md](../../AGENTS.md): `ReplayGolden 6/6`, `PlayModeSmoke ≥20/20`, and either the
   Baltic v2 hash `17144800277401907079` is unchanged **or** an ADR explicitly re-blesses it. Regenerate
   goldens only through the documented workflow, never by hand-editing hashes.

---

## Tests that pin this doc

All green as of writing. NUnit fixtures in the Delegation test assembly
(`src/ProjectAegis.Delegation.Tests/Decision/`):

| Test file | Cases | Covers |
|-----------|-------|--------|
| [`DecisionLogTests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/DecisionLogTests.cs) | 3 | AAR-stream order preservation; chronology + fingerprint stability across calls; append-path hash immutability. |
| [`IOrderLogContractTests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/IOrderLogContractTests.cs) | 2 | Orchestrator `OrderLog` is the same instance as `DecisionLog` (empty → empty fingerprint); legacy `DecisionRecord` → `AgentDecisionPayload` round-trip in chronology. |
| [`OrderLogC1RowTypesTests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/OrderLogC1RowTypesTests.cs) | 2 | The C1 discriminated-union row types + factories. |
| [`OrderLogSimAppendTests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/OrderLogSimAppendTests.cs) | 1 | Sim-side rows append and surface correctly. |
| [`OrderLogFingerprintTests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/OrderLogFingerprintTests.cs) | 1 | Canonical fingerprint text shape. |
| [`OrderLogReplayFingerprintSha256Tests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/OrderLogReplayFingerprintSha256Tests.cs) | 2 | `OrderLogReplayFingerprint.ComputeSha256Hex` lowercase-hex SHA-256 over the canonical text. |
| [`OrderLogScoredIntentsFingerprintTests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/OrderLogScoredIntentsFingerprintTests.cs) | 2 | Deterministic scored-intent formatting inside the `AgentDecision` payload. |
| [`ReplayOrderLogFingerprintTests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/ReplayOrderLogFingerprintTests.cs) | 3 | End-to-end replay fingerprint stability. |
| [`AgentDecisionPayloadTests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/AgentDecisionPayloadTests.cs) | 3 | `AgentDecisionPayload` ↔ `DecisionRecord` migration. |
| [`EngagementOrderLogContractTests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/EngagementOrderLogContractTests.cs) | 4 | `Engagement` row contract. |
| [`ContactChangeOrderLogTests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/ContactChangeOrderLogTests.cs) | 3 | `ContactChange` row + `FromContactTransition`. |
| [`MagazineChangeOrderLogTests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/MagazineChangeOrderLogTests.cs) | 2 | `MagazineChange` row. |
| [`FuelStateChangeOrderLogTests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/FuelStateChangeOrderLogTests.cs) · [`FuelBurnOrderLogTests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/FuelBurnOrderLogTests.cs) | 1 + 1 | `FuelStateChange` / `FuelBurn` rows. |
| [`OrdnanceStateChangeOrderLogTests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/OrdnanceStateChangeOrderLogTests.cs) | 1 | `OrdnanceStateChange` row. |
| [`PolicyDenialLogTests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/PolicyDenialLogTests.cs) | 2 | `PolicyDenial` row. |

Run just the order-log fixtures:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "FullyQualifiedName~Decision.DecisionLog|FullyQualifiedName~Decision.IOrderLog|FullyQualifiedName~Decision.OrderLog|FullyQualifiedName~Decision.ReplayOrderLog|FullyQualifiedName~Decision.AgentDecisionPayload|FullyQualifiedName~Decision.EngagementOrderLog|FullyQualifiedName~Decision.ContactChangeOrderLog|FullyQualifiedName~Decision.MagazineChangeOrderLog|FullyQualifiedName~Decision.FuelStateChangeOrderLog|FullyQualifiedName~Decision.FuelBurnOrderLog|FullyQualifiedName~Decision.OrdnanceStateChangeOrderLog|FullyQualifiedName~Decision.PolicyDenialLog"
```

---

*Verified against source at the paths above. If you add or change an entry kind, a payload record, or the
`FormatPayload` shape, update this doc, [determinism-and-replay.md](determinism-and-replay.md), and
[ADR-003](../architecture/adr-003-order-log-schema.md) together — and confirm the replay golden posture.*
