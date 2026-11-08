# BDA-assess advisory read-model (Slice B)

> **Scope:** the pure, headless **battle-damage-assessment (BDA) *assess*** read-model
> (`ProjectAegis.Delegation/BdaAssess/`, **DRG-216**, Combat-UX Slice B). It answers *"what is the
> assessed damage state of each contact?"* — `None` / `InProgress` / `Damaged` / `Destroyed` /
> `Unknown` — from the order log alone. Like the rest of the [projection layer](c2-projection-layer.md)
> it is **advisory only**: it enqueues no orders, resolves no combat, reads no UI state, contains no
> `UnityEngine`, runs off the `Tick` hot path, and is **not** folded into the `SimWorldHash` or the
> order-log fingerprint, so the Baltic v2 replay hash `17144800277401907079` is untouched
> (ADR-010 §2–3, ADR-001).

**Not to be confused with** [bda-contact-lifecycle-runtime.md](bda-contact-lifecycle-runtime.md):
that doc covers the *sim-side* `BdaContactLifecycleHotTickApplier` that promotes damaged/destroyed
platforms to `Lost` in the tactical picture during the tick. This doc covers the *read-side*
projection that classifies the resulting damage state for C2 display.

---

## API

```csharp
BdaAssessSnapshot BdaAssessProjection.Project(
    DecisionLog? log,
    ulong currentSimTick,                                 // currently unused; reserved for time-window rules
    IReadOnlyList<BdaAssessPendingTarget>? pendingTargets = null);

string BdaAssessProjection.ComputeFingerprint(BdaAssessSnapshot? snapshot);
```

- A `null` log, or a log with no contact history, returns `BdaAssessSnapshot.Empty`.
- The snapshot is one `BdaAssessContactState` **per contact** in the last-known picture, sorted
  **ordinal by `ContactId`**.
- `pendingTargets` is caller-supplied in-flight engage-assess evidence (see §Pending). The projection
  **never invents** pending rows — a target only shows `InProgress` when the caller passes it in.

### Row shape

```csharp
record BdaAssessContactState(
    string ContactId, string TargetId, string ObserverId,
    BdaAssessStateKind State, BdaAssessSourceKind Source,
    ulong SimTick, double SimTime, ulong CorrelationSequenceId);
```

`SimTick`/`SimTime` are the assessment's sim clock; `CorrelationSequenceId` is the order-log
sequence of the driving engagement outcome / damage / pending record (`0` when the state is derived
only from the contact's lifecycle).

| `BdaAssessStateKind` | # | Meaning |
|----------------------|---|---------|
| `None`               | 0 | live contact, no terminal BDA and no pending assess |
| `InProgress`         | 1 | an engage-assess is in flight for the target (from `pendingTargets`) |
| `Damaged`            | 2 | non-terminal damage (`DegradedL1`/`DegradedL2`, or platform HP > 0) |
| `Destroyed`          | 3 | confirmed kill / HP ≤ 0 |
| `Unknown`            | 4 | contact lifecycle is `"Unknown"` |

| `BdaAssessSourceKind` | # | Provenance |
|-----------------------|---|------------|
| `None`                | 0 | no BDA driver (live `None` row) |
| `PlatformDamage`      | 1 | order-log `PlatformDamageChange` (HP / reason code) |
| `EngagementOutcome`   | 2 | order-log `EngagementOutcome` with `Kill` code |
| `PendingEngagement`   | 3 | caller-supplied `BdaAssessPendingTarget` |
| `ContactLifecycle`    | 4 | derived from the contact's lifecycle string only |

---

## Per-contact resolution ladder

For each contact (ordinal `ContactId` order), the **first** matching rule wins — and every contact
emits an *explicit* state, **never a silent omission**:

1. **Terminal BDA from the order log** (`BuildTerminalAssessByContact` over `OrderLogBdaProjection`):
   - `DegradedL1`/`DegradedL2` BDA state → `Damaged` / `PlatformDamage`.
   - `Lost` BDA state → resolve terminal cause:
     - a matching `Kill` `EngagementOutcome` at the same tick → `Destroyed` / `EngagementOutcome`;
     - else a same-tick `PlatformDamageChange` → `Destroyed` when `NewHpPct <= 0` or the reason code
       is `Kill`, otherwise `Damaged` (both `PlatformDamage`);
     - else (overloaded `Lost` lifecycle, e.g. `DamageLevel >= 3` with remaining HP) → `Damaged` /
       `PlatformDamage`.
2. **Kill-on-lost-sensor** (`TryResolveKillDestroyed`): a contact whose lifecycle is `"Lost"` with a
   matching `Kill` outcome at its last-known tick → `Destroyed` / `EngagementOutcome`.
3. **Pending assess**: target present in `pendingTargets` → `InProgress` / `PendingEngagement`.
4. **Unknown lifecycle**: contact lifecycle `"Unknown"` → `Unknown` / `ContactLifecycle`.
5. **Otherwise** → `None` / `None` (live contact, nothing to assess).

### Why "last-known from the log"

The picture is built by `BuildLastKnownContactsFromLog` — it replays `log.ContactChanges` in
`(SimTick, SequenceId, ContactId)` order and keeps the **last** row per contact, so it **retains
`Lost` contacts** that [`ContactPictureProjection`](c2-projection-layer.md) drops from the active
picture. That is deliberate: a destroyed target has usually already gone `Lost`, so a "current
picture only" source would lose exactly the rows BDA-assess exists to report.

### Multi-contact-per-target fan-out

`OrderLogBdaProjection` folds BDA per **target**, but two observers can hold the same target as two
contacts. `FanOutBdaContactChanges` mirrors the `KillChainContactStateProjection` #575 fan-out:
per-target BDA changes are expanded to every contact of that target (re-stamped with each contact's
`ObserverId`/`ContactId`), so each contact row gets its own terminal assessment.

### Kill / damage correlation (deterministic tie-breaks)

- `TryFindKillOutcome` matches `EngagementOutcome` rows by `VictimTargetId == targetId`,
  `OutcomeCode == Kill`, and `SimTick`, choosing the latest by `SequenceId` then `EngagementId`.
- `FindPlatformDamageForChange` matches `PlatformDamageChange` by `UnitId == targetId` and `SimTick`,
  choosing the latest by `SequenceId`.
- Pending rows de-dupe per target keeping the newest by `(SimTick, CorrelationSequenceId)`.

All comparisons are ordinal; there is no wall clock and no RNG.

---

## Pending assess input

```csharp
record BdaAssessPendingTarget(string TargetId, ulong SimTick, double SimTime, ulong CorrelationSequenceId);
```

`BdaAssessPendingTarget` lets a caller inject in-flight engage-assess facts **without importing
`CombatEvents`** into this projection. The caller owns the target id; a blank id is skipped. This is
the only way a row becomes `InProgress`.

---

## Fingerprint

`BdaAssessProjection.ComputeFingerprint` is replay-stable (invariant culture, ordinal, no wall clock):

```
bda:empty                                   # null / empty snapshot
bda:c=<n>|<contactId>,<targetId>,<observerId>,<state int>,<source int>,<simTick>,<simTime R>,<correlationSequenceId>
        |…                                  # one segment per contact row
```

The enum integers are part of the fingerprint contract — extend the enums by appending, never by
renumbering.

---

## Invariants — do not break

| Invariant | Why |
|-----------|-----|
| **No silent omission** | Every contact in the picture emits an explicit `State`/`Source`; a live contact is `None`/`None`, an unknown-lifecycle contact is `Unknown` — the panel never has to guess a blank. |
| **Last-known, not active-only** | The picture retains `Lost`/destroyed contacts the active `ContactPictureProjection` drops. |
| **Advisory, not authority** | Read-only over the `DecisionLog`; enqueues no orders, resolves no combat, and reads no selection/hover/camera/visibility (reflection-pinned). |
| **Not fingerprinted into the sim** | The `bda:` fingerprint is display de-dup only; it never feeds `SimWorldHash`/the order-log fingerprint (Baltic v2 hash untouched). |
| **Deterministic** | Ordinal sorts, invariant-culture formatting, sequence-id tie-breaks; equal inputs ⇒ equal snapshot ⇒ equal fingerprint. |
| **Pending is caller-owned** | The projection never fabricates `InProgress`; it only reflects supplied `BdaAssessPendingTarget`s. |

---

## Producer / consumer map

```
order log (DecisionLog)                    ← read-only
  ├─ ContactChanges ─→ BuildLastKnownContactsFromLog (retains Lost)
  ├─ (BDA rows) ─────→ OrderLogBdaProjection ─→ FanOutBdaContactChanges
  ├─ EngagementOutcomes (Kill) ─┐
  └─ PlatformDamageChanges ─────┴─→ terminal resolution
                                       +
  caller-supplied BdaAssessPendingTarget[] (InProgress)
                                       ▼
                        BdaAssessProjection.Project → BdaAssessSnapshot (ordinal by ContactId)
                                       ▼
                        C2 assess panel host (Slice B chrome) — advisory display only
```

Sim-side lifecycle promotion (the `Lost` transitions this read-model reports on):
[bda-contact-lifecycle-runtime.md](bda-contact-lifecycle-runtime.md). Engage-time kill/damage source:
[engagement-pipeline.md](engagement-pipeline.md). General read-model / `Projection → Binder → State`
rules: [c2-projection-layer.md](c2-projection-layer.md).

---

## Runbook — extend BDA-assess

1. **Add a state or source** — append to `BdaAssessStateKind` / `BdaAssessSourceKind` (new integer,
   do **not** renumber; the ints are in the `bda:` fingerprint), add the resolution branch in the
   `Project` ladder at the correct precedence, and extend `BdaAssessProjectionTests`
   (`src/ProjectAegis.Delegation.Tests/BdaAssess/`, NUnit `[TestFixture]`, ~25 cases).
2. **Add a new terminal driver** — extend `TryResolveTerminalFromBdaChange` / `TryResolveKillDestroyed`
   with the new order-log record type and its deterministic tie-break; keep the last-known picture and
   fan-out intact.
3. **Never** move assessment into the tick path or let a host re-derive it — keep it a pure fold over
   the order log.
4. This read-model is outside the replay goldens, so adding states/sources moves no golden hash — but
   do refresh any pinned `bda:` fingerprint expectations. Run the full gate (build + solution tests +
   `PlayModeSmoke`).
