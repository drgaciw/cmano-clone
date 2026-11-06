# Identity classification (unknown-vs-known) read-model

> **Scope.** The pure, headless read-model that answers "do we know *what* this contact is?" —
> folding the tactical contact picture and its provenance into a per-contact **`Unknown` /
> `Tentative` / `Classified`** identity ledger with a named, plain-language reason and a confidence
> band for each row. It lives in
> [`ProjectAegis.Delegation/IdentityClass/`](../../src/ProjectAegis.Delegation/IdentityClass/)
> (DRG-225, Combat UX Slice A) and follows the read-only projection contract of ADR-010 §2–3 /
> ADR-007 / ADR-001: it is derived from the order log, does **no mutation**, has **no `UnityEngine`
> dependency**, and is **advisory only** — it never enqueues a fire order or any authorization side
> effect, and never touches the sim hot path or the replay fingerprint.

---

## What it folds

```
IdentityClassProjection.Project(
    DecisionLog? log,
    ulong currentSimTick,
    ICatalogReader? catalog = null,
    ScenarioCommsDisplaySettings? commsDisplay = null,
    int staleThresholdTicks = ContactProvenanceProjection.DefaultStaleThresholdTicks,
    IReadOnlyList<ScenarioOrbatUnitDto>? orbatUnits = null)
  → IdentityClassSnapshot
```

- A **null** `log`, or a log whose `ContactPictureProjection.Project` is empty, returns
  `IdentityClassSnapshot.Empty`.
- Otherwise it projects the contact picture (`ContactPictureProjection`) and provenance
  (`ContactProvenanceProjection`, keyed by `ContactId`, ordinal), builds one `IdentityClassRow`
  per contact, and `Array.Sort`s the rows by `ContactId` (ordinal). Both folded projections are
  documented in [c2-projection-layer.md](c2-projection-layer.md).

## The snapshot

```csharp
public sealed record IdentityClassRow(
    string ContactId,
    IdentityClassification Classification,   // Unknown=0 / Classified=1 / Tentative=2
    string ReasonCode,
    IdentityConfidenceBand ConfidenceBand,   // Unknown=0 / Low=1 / Medium=2 / High=3
    ulong SimTick)                           // = contact.LastSimTick (not currentSimTick)
{
    public string ReasonLabel => IdentityClassReasonLabels.Format(ReasonCode); // never empty for published rows
}

public sealed record IdentityClassSnapshot(IReadOnlyList<IdentityClassRow> Rows)
{
    public static IdentityClassSnapshot Empty { get; }
}
```

## Classification precedence (`ResolveClassification`)

Evaluated top-down; the first match wins. It **fails closed to `Unknown`**:

1. Provenance `OutOfCommsUnknown` → **`Unknown`** (we can't see it, so we don't claim to know it).
2. Provenance `CatalogMiss` **and** lifecycle is `Unknown` → **`Unknown`**.
3. Lifecycle `Unknown` → **`Unknown`**.
4. Lifecycle `Detected` → **`Tentative`**.
5. A *classified* lifecycle → **`Classified`**.
6. Anything else → **`Unknown`** (floor).

A *classified lifecycle* (`IsClassifiedLifecycle`) is `Classified`, `Identified`, or the BDA damage
states `DegradedL1` / `DegradedL2` (`BdaContactDamageStates`, see
[bda-contact-lifecycle-runtime.md](bda-contact-lifecycle-runtime.md)) — a damaged-but-identified
track is still known.

## Reason codes — never silent (`ResolveReasonCode`)

Every published row carries a stable `ReasonCode` and a non-empty plain-language `ReasonLabel`
(`IdentityClassReasonLabels.Format`, which returns the raw code as a fallback). Precedence:

| Order | Condition | Reason code | Label |
|-------|-----------|-------------|-------|
| 1 | Provenance `OutOfCommsUnknown` | `CommsGap` | `comms gap` |
| 2 | `CatalogMiss` flag **and** classification `Unknown` | `CatalogMiss` | `catalog miss` |
| 3 | `Stale` flag **and** classification `Tentative` | `StaleTrack` | `stale track` |
| 4 | Lifecycle `Unknown` | `LifecycleUnknown` | `lifecycle unknown` |
| 5 | Lifecycle `Detected` | `LifecycleDetected` | `lifecycle detected` |
| 6 | Lifecycle `Identified` | `LifecycleIdentified` | `lifecycle identified` |
| 7 | Classified lifecycle | `LifecycleClassified` | `lifecycle classified` |
| 8 | (floor) | `LifecycleUnknown` | `lifecycle unknown` |

The `CatalogMiss` / `Stale` provenance quality flags come from
`ContactProvenanceQualityState` (`CatalogMiss=1`, `Stale=2`, `SilentComms=4`).

## Confidence band (`ResolveConfidenceBand`)

- Classification `Unknown` → **`Unknown`** (confidence is `Unknown` iff classification is).
- Otherwise, if provenance is present, map its `ContactProvenanceConfidence`
  (`High`/`Medium`/`Low` → `High`/`Medium`/`Low`, else `Unknown`).
- Otherwise (no provenance), fall back on lifecycle: `Identified` → `High`, other classified →
  `Medium`, `Detected` → `Low`, else `Unknown`.

## Determinism

`IdentityClassFingerprint.Compute` renders `ic:empty` for a null/empty snapshot, else
`ic:r=<count>` followed by one `|<ContactId>,<(int)Classification>,<ReasonCode>,<(int)ConfidenceBand>,<SimTick>`
segment per row, in the snapshot's ordinal-`ContactId` order. Invariant culture, no wall clock, no
RNG — byte-stable for identical inputs. This projection is **off** the `SimWorldHash` / order-log
replay fingerprint, so it cannot move the Baltic v2 hash `17144800277401907079`.

## Invariants

| Invariant | Why |
|-----------|-----|
| **Advisory only** | Emits identity posture rows; never enqueues a fire order or authorization. Sim-clock only. |
| **Read-only / no `UnityEngine`** | Pure `Delegation/IdentityClass/` fold over the order log; headless-testable. |
| **Off the fingerprint** | Derived view; never appended to `DecisionLog`, so the replay hash is untouched. |
| **Fail-closed to `Unknown`** | Out-of-comms and catalog-miss-on-unknown resolve to `Unknown`; the floor is `Unknown`. |
| **Never silent** | Every published row has a non-empty `ReasonCode` + `ReasonLabel`. |
| **Confidence tracks classification** | `ConfidenceBand` is `Unknown` exactly when `Classification` is `Unknown`. |
| **Deterministic ordinal ordering** | Rows sorted by `ContactId`; fingerprint is replay-stable. |

## Extending it

- **Add a reason code** — add the constant to `IdentityClassReasonCodes`, its label to
  `IdentityClassReasonLabels` (and the `Format` switch), and insert the branch at the right
  precedence in `ResolveReasonCode`. Keep a non-empty label so the never-silent invariant holds.
- **Add a classification / confidence input** — thread the new provenance/catalog fact into
  `ResolveClassification` / `ResolveConfidenceBand`; keep the `Unknown` floor.
- Nothing here needs a replay-golden re-bless — it is read-only presentation.

## Tests

| Suite | Assembly | Covers |
|-------|----------|--------|
| `IdentityClassProjectionTests` (9) | `ProjectAegis.Delegation.Tests` (NUnit) | Empty log → empty/`ic:empty`; the never-silent `LifecycleUnknown` / `CommsGap` / `StaleTrack` reasons; `Classified` (Medium) and `Identified` (High) confidence; `Detected` → `Tentative`/Low; all rows publish non-empty labels; replay-stable fingerprint + ordinal `ContactId` ordering. |

## See also

- [c2-projection-layer.md](c2-projection-layer.md) — `ContactPictureProjection` + `ContactProvenanceProjection`, the two folds this reads, and the read-only projection contract.
- [bda-contact-lifecycle-runtime.md](bda-contact-lifecycle-runtime.md) — the `BdaContactDamageStates` (`DegradedL1`/`DegradedL2`) counted as a classified lifecycle.
- [sensor-modality-detection.md](sensor-modality-detection.md) — the detection lifecycle (`Unknown`/`Detected`/`Classified`/`Identified`) that drives classification.
- [order-log-runtime.md](order-log-runtime.md) — the `DecisionLog` contact source of truth.
