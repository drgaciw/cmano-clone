# Targetability-accept projection — the fail-closed Slice A acceptance gate (DRG-219)

The **targetability-accept projection** composes the three wave-1 Combat UX Slice A read models —
contact **provenance**, the **sensor-to-shooter** chain, and the **C2 authority** projection — into
one replay-stable **acceptance snapshot**: per contact, a single `Permitted` / `Withheld`
disposition plus a **named cause code** for every withhold. It is the headless *exit gate* that
answers "may this contact be accepted as a target right now?" without ever issuing an order. It is a
pure, engine-agnostic read model in
[`ProjectAegis.Delegation/TargetabilityAccept/`](../../src/ProjectAegis.Delegation/TargetabilityAccept/) —
static `Project(...)` calls, no mutation, no RNG, no wall-clock.

> **Scope / boundary (DRG-219, Combat UX Slice A):** this is a **headless** projection. It derives
> everything from a `DecisionLog` (via its child projectors), the catalog, and an authority context;
> it never mutates the sim or the order log, has **no `UnityEngine` dependency**, never touches
> `DelegationBridge`, and is **not wired to any host today**. The disposition is **advisory** —
> `Permitted` means "the acceptance facts line up", **never** a fire clearance. The load-bearing rule
> is **fail closed**: any missing evidence, stale track, incomplete chain, or withheld/approval-gated
> authority yields `Withheld` with an explicit cause — a contact is *never silently* accepted. The
> subsystem has its own [`TargetabilityAcceptFingerprint`](#determinism--tests) for replay-stability
> tests; it is **not** an input to the Baltic v2 `SimWorldHash`. Boundary per **ADR-010 §2–3**
> (headless-first, command-driven UI) and **ADR-001** (sim assembly boundary).

---

## Types

| Type | Kind | Role |
|------|------|------|
| [`TargetabilityAcceptProjection`](../../src/ProjectAegis.Delegation/TargetabilityAccept/TargetabilityAcceptProjection.cs) | `static` | The fold: two `Project(...)` overloads → snapshot. |
| [`TargetabilityAcceptSnapshot`](../../src/ProjectAegis.Delegation/TargetabilityAccept/TargetabilityAcceptTypes.cs) | `sealed record` | `(Contacts)`; `Empty` singleton. |
| [`TargetabilityAcceptContactRow`](../../src/ProjectAegis.Delegation/TargetabilityAccept/TargetabilityAcceptTypes.cs) | `sealed record` | `(ContactId, TargetId, Disposition, WithheldCauseCode, Provenance?, SensorToShooter?, Authority)` — carries the composed child facts alongside the verdict. |
| [`TargetabilityAcceptDisposition`](../../src/ProjectAegis.Delegation/TargetabilityAccept/TargetabilityAcceptTypes.cs) | `enum` | `Permitted=0` / `Withheld=1`. |
| [`TargetabilityAcceptCauseCodes`](../../src/ProjectAegis.Delegation/TargetabilityAccept/TargetabilityAcceptTypes.cs) | `static` | Stable named withhold causes — the provenance/chain codes plus the ROE/authority codes re-exported from [`C2AuthorityProjector`](../../src/ProjectAegis.Delegation/Skills/C2AuthorityProjector.cs). |
| [`TargetabilityAcceptFingerprint`](../../src/ProjectAegis.Delegation/TargetabilityAccept/TargetabilityAcceptFingerprint.cs) | `static` | Canonical `Compute(snapshot)` string for determinism tests. |
| [`TargetabilityAcceptChildFingerprints`](../../src/ProjectAegis.Delegation/TargetabilityAccept/TargetabilityAcceptTypes.cs) | `sealed record` | Declared `(Provenance, SensorToShooter, Authority)` helper — **not currently referenced** by the projection or the fingerprint (the fingerprint recomputes child segments inline). |

---

## The fold — `TargetabilityAcceptProjection.Project`

There are two overloads. The **composing** overload builds the three child snapshots from an order
log, then delegates to the **pure** overload:

```
// Composing overload — runs the three child projectors, then folds
Project(
    DecisionLog? log,
    ulong currentSimTick,
    in C2AuthorityProjectionContext authorityContext,   // one authority context, applied per row
    IKillChainFireControlSource? fireControl = null,
    ISensorToShooterShooterSource? shooters = null,
    ICatalogReader? catalog = null,
    string weaponId = CatalogWeaponIds.MvpDefault,
    int staleThresholdTicks = ContactProvenanceProjection.DefaultStaleThresholdTicks, // 30
    IReadOnlyList<ScenarioOrbatUnitDto>? orbatUnits = null,
    ScenarioCommsDisplaySettings? commsDisplay = null)
  → TargetabilityAcceptSnapshot

// Pure overload — folds already-built child snapshots
Project(
    ContactProvenanceSnapshot? provenance,
    SensorToShooterSnapshot? sensorToShooter,
    C2AuthorityProjection? authority)
  → TargetabilityAcceptSnapshot
```

- The composing overload returns `Empty` when `log is null`; otherwise it runs
  [`ContactProvenanceProjection.Project`](../../src/ProjectAegis.Delegation/Projection/ContactProvenanceProjection.cs),
  [`SensorToShooterProjection.Project`](../../src/ProjectAegis.Delegation/SensorToShooter/SensorToShooterProjection.cs),
  and [`C2AuthorityProjector.Project`](../../src/ProjectAegis.Delegation/Skills/C2AuthorityProjector.cs),
  then calls the pure overload. Note `SensorToShooterProjection` has **no** `commsDisplay` parameter —
  contact **provenance** owns the comms-degraded stale divisor, so the two children stay consistent.
- The pure overload returns `Empty` when `authority is null` (authority is mandatory — you cannot
  form a disposition without it). It indexes provenance by `ContactId` and chains by `ContactId`
  (both `StringComparer.Ordinal`), takes the **union** of contact ids, and emits one ordinal-sorted
  row per id. An empty union → `Empty`.
- The single `C2AuthorityProjection` is applied to **every** row — the authority context is a
  per-call actor/skill boundary, not per-contact.
- `TargetId` for a row is `provenance.Source.TargetId` → else `chain.TargetId` → else `""`.

---

## Disposition — the fail-closed ladder

`ResolveDisposition(provenance, chain, authority)` checks in a fixed order and returns at the **first**
failing gate, so the most fundamental evidence gap always wins the cause code. Only the final `else`
returns `Permitted`:

| # | Condition | Disposition | Cause code |
|---|-----------|-------------|------------|
| 1 | `provenance is null` (a chain-only row) | `Withheld` | `MissingProvenance` |
| 2 | provenance `QualityState` has `CatalogMiss` | `Withheld` | `CatalogMiss` |
| 3 | provenance `Freshness == Stale` | `Withheld` | `Stale` |
| 4 | provenance `SilentComms` flag **and** `OutOfCommsUnknown` | `Withheld` | `SilentComms` |
| 5 | `chain is null` **or** `!chain.IsComplete` | `Withheld` | mapped from `chain.PrimaryBreakCause` (below) |
| 6 | `authority.Targeting.Disposition == Withheld` | `Withheld` | `authority.Targeting.ReasonCode` (else `ApprovalRequired`) |
| 7 | `authority.Targeting.Disposition == ApprovalRequired` | `Withheld` | `authority.Targeting.ReasonCode` (else `ApprovalRequired`) |
| 8 | *otherwise* (fresh + complete chain + permitted targeting) | `Permitted` | `None` |

Rule 1 is the fail-closed keystone: a sensor-to-shooter chain that exists **without** provenance can
never become `Permitted`. Rules 6–7 fold an authority gate — including approval-required — into a
`Withheld`, so a contact awaiting weapons-release approval is *not* accepted (never a silent yes).

### Sensor-to-shooter break-cause mapping (rule 5)

`FormatSensorToShooterCause` maps the chain's
[`SensorToShooterBreakCause`](../../src/ProjectAegis.Delegation/SensorToShooter/SensorToShooterTypes.cs)
to a stable acceptance cause code:

| `PrimaryBreakCause` | Cause code |
|---------------------|------------|
| `LostSensor` | `LostSensor` |
| `StaleTrack` | `StaleTrack` |
| `NoFireControl` | `NoFireControl` |
| `NoEligibleShooter` | `NoEligibleShooter` |
| `DegradedTrack` | `DegradedTrack` |
| *(default / `None`)* | `StaleTrack` |

### Cause codes are never silent

Every `Withheld` row carries a non-empty [`TargetabilityAcceptCauseCodes`](../../src/ProjectAegis.Delegation/TargetabilityAccept/TargetabilityAcceptTypes.cs)
constant. The provenance/chain codes are local (`MissingProvenance`, `CatalogMiss`, `Stale`,
`SilentComms`, `LostSensor`, `StaleTrack`, `NoFireControl`, `NoEligibleShooter`, `DegradedTrack`),
while the ROE/authority codes (`WeaponsTight`, `RoeHoldFire`, `NoFireControlAuthority`,
`SharedTrackNoRelease`, `WeaponsReleaseRequired`, `ApprovalRequired`) are **re-exported** from
`C2AuthorityProjector` so the acceptance ledger and the authority chrome speak one vocabulary.

---

## Determinism & tests

- **Pure & deterministic.** No RNG, no wall-clock; provenance/chain indexing and the contact-id
  union iterate `StringComparer.Ordinal`, and rows are emitted ordinal-sorted by `ContactId`. The
  projection reads only its child snapshots — it is **off the `SimWorldHash`** and never writes the
  order log.
- **Own fingerprint.** [`TargetabilityAcceptFingerprint.Compute`](../../src/ProjectAegis.Delegation/TargetabilityAccept/TargetabilityAcceptFingerprint.cs)
  emits `tac:empty` for an empty snapshot, else `tac:c=<n>` followed by one `|`-delimited segment per
  contact — `ContactId, TargetId, (int)Disposition, WithheldCauseCode`, then the folded child
  fingerprints: [`ContactProvenanceFingerprint.Compute`](../../src/ProjectAegis.Delegation/Projection/ContactProvenanceFingerprint.cs)
  of the single provenance row (or `ContactProvenanceSnapshot.Empty`),
  [`SensorToShooterProjection.ComputeFingerprint`](../../src/ProjectAegis.Delegation/SensorToShooter/SensorToShooterProjection.cs)
  of the single chain (or the literal `sts:empty`), and an inline `auth:` fold of the ROE slice,
  targeting leg, and the **ordinal-action-sorted** authority actions. Identical inputs yield identical
  fingerprints.

| Suite | Location | Count |
|-------|----------|-------|
| `TargetabilityAcceptProjectionTests` | [`src/ProjectAegis.Delegation.Tests/TargetabilityAccept/TargetabilityAcceptProjectionTests.cs`](../../src/ProjectAegis.Delegation.Tests/TargetabilityAccept/TargetabilityAcceptProjectionTests.cs) | 11 |

Related: [autonomy-roe-gating.md](autonomy-roe-gating.md) (the *decision-time* ROE/autonomy gate this
acceptance view mirrors on the read side) · [pending-approval-queue.md](pending-approval-queue.md)
(the human-in-the-loop approval cousin behind a `QueueForApproval` verdict) ·
[c2-projection-layer.md](c2-projection-layer.md) (the read-model family incl.
`ContactPictureProjection` and the provenance projector). The `C2AuthorityProjector` / ROE slice
supplies rules 6–7; the sensor-to-shooter and provenance children are the wave-1 Slice A projectors
under `ProjectAegis.Delegation/`.
