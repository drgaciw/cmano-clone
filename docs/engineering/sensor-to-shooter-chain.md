# Sensor-to-shooter chain & Slice A contact provenance

The **Combat-UX Slice A** read-model family: a set of pure, headless, **advisory** projections that
let a watch officer *inspect why a contact can or cannot be engaged* — the sensor → track →
targetability → eligible-shooter chain (**DRG-207 / DRG-181**) and the per-contact **provenance**
(source, confidence, freshness, comms, last-known — **DRG-206 / DRG-180**). Everything here is a
*client* of sim truth, never sim authority: it reads the order log read-only, is **not** folded into
the `SimWorldHash` or the order-log fingerprint, runs off the `Tick` hot path, and issues **no fire
orders** (ADR-010 §2–3, ADR-007, ADR-001). The Baltic v2 replay hash `17144800277401907079` is
untouched by anything in this doc.

Two layers, mirroring the rest of the [projection layer](c2-projection-layer.md):

| Layer | Assembly / folder | Types |
|-------|-------------------|-------|
| **Core read-models** (engine-agnostic, no `UnityEngine`) | `ProjectAegis.Delegation/SensorToShooter/`, `…/Projection/` | `SensorToShooterProjection`, `SensorToShooterTypes`, `ContactProvenanceProjection`, `ContactProvenance`, `ContactProvenanceFingerprint` |
| **Presentation chrome** (Unity-adapter) | `ProjectAegis.Delegation.UnityAdapter/Presentation/` + `…/Bridge/` | `SensorToShooterPresentation`/`Presenter`/`PanelBinder`, `SliceAContactLiveSurfaceBinder`, `SliceAContactFrame`/`SliceAContactFrameBridge`, `SensorToShooterPanelHost` (Unity) |

---

## 1. `SensorToShooterProjection` — the chain read-model (DRG-207)

`SensorToShooterProjection.Project(...)` turns kill-chain contact state plus the catalog engage
envelope into an inspectable **four-link chain per contact**:

```
Sensor → Track → Targetability → EligibleShooter
```

Two entry points:

- `Project(DecisionLog?, currentSimTick, IKillChainFireControlSource?, ISensorToShooterShooterSource?, ICatalogReader?, weaponId = CatalogWeaponIds.MvpDefault, staleThresholdTicks = 30, dropThresholdTicks = 120)` —
  composes [`KillChainContactStateProjection.Project`](track-custody-and-cde-assess.md) first, then the overload below.
- `Project(KillChainContactSnapshot?, ISensorToShooterShooterSource?, ICatalogReader?, weaponId)` —
  when the caller already holds a kill-chain snapshot (the `SliceAContactFrameBridge` path).

A `null` log, `null`/empty kill-chain snapshot, or zero contacts short-circuits to
`SensorToShooterSnapshot.Empty`. Contacts are processed in **ordinal `ContactId` order**.

### Link resolution rules

Each link is a `SensorToShooterChainLink(Kind, IsLinked, BreakCause, UnitId, ContactId, TargetId, Detail)`.
The break-cause enum is an on-disk/fingerprint contract:

| `SensorToShooterBreakCause` | # | Label (`SensorToShooterBreakCauseLabels`) |
|-----------------------------|---|-------------------------------------------|
| `None`                      | 0 | *(empty)* |
| `LostSensor`                | 1 | `lost sensor` |
| `StaleTrack`                | 2 | `stale track` |
| `NoFireControl`             | 3 | `no FC` |
| `NoEligibleShooter`         | 4 | `no eligible shooter` |
| `DegradedTrack`             | 5 | `degraded track` |

- **Sensor** (`unitId = ObserverId`): broken `LostSensor` when the contact `Loss == Lost` **or**
  `!DetectionCaptured` (`"sensor not detecting"`); otherwise linked (`sensor:{observerId}`).
- **Track** (`unitId = ContactId`): `LostSensor` when `Lost`; `StaleTrack` when `Loss == Stale`
  **or** `!TrackContinuous`; otherwise linked (`track:{contactId}`).
- **Targetability** (`unitId = ContactId`): `LostSensor` when `Lost`; `StaleTrack` when `Stale`;
  `DegradedTrack` when `DegradedL1`/`DegradedL2`; when `!Targetable`, resolves to `NoFireControl`
  if the track is otherwise continuous and un-lost (`TrackContinuous && Loss == None`), else
  `StaleTrack`; otherwise linked (`targetable`).
- **EligibleShooter**: if the targetability link is broken, it **inherits** that break cause (the
  chain never claims a shooter for an un-targetable contact). Otherwise it queries
  `ISensorToShooterShooterSource.GetCandidatesForTarget(targetId)`:
  - no candidates → `NoEligibleShooter`;
  - candidates are ordered **ordinal by `ShooterUnitId`**, and for each the scenario engage defaults
    are pushed through `CatalogEngageEnvelope.Apply(..., catalog, weaponId)` → `EngageContext`;
  - a candidate is skipped with detail `NO_AMMO` when `RoundsRemaining <= 0` or
    `RoundsRemaining < max(1, SalvoSize)`;
  - otherwise `EngagePreviewProjection.Project(...)` decides — the **first** candidate whose preview
    `CanFire` is the eligible shooter (`shooter:{unitId}`); a non-firing preview records its
    `AbortPreviewCode` as the detail;
  - if none qualifies → `NoEligibleShooter` with the last abort detail.

`PrimaryBreakCause` is the **first** broken link walking Sensor→Track→Targetability→Shooter;
`IsComplete` is true only when every link is linked. Note "complete" means **technical eligibility
only, not release authority** — the presentation layer is explicit about this.

### Fingerprint

`SensorToShooterProjection.ComputeFingerprint` is replay-stable (invariant culture, ordinal only):

```
sts:empty                                             # null / empty
sts:c=<n>|<contactId>,<targetId>,<observerId>,<isComplete 0|1>,<primaryBreak int>,<linkCount>
        ;<kind int>,<isLinked 0|1>,<breakCause int>,<unitId>,<detail>   # per link, repeated
```

---

## 2. `ContactProvenanceProjection` — the provenance read-model (DRG-206)

`ContactProvenanceProjection.Project(...)` folds order-log contact changes + comms state into one
`ContactProvenanceState` **per active contact** (ordinal `ContactId` sort). Two overloads: one from
a `DecisionLog` (runs `ContactPictureProjection` + `CommsStateProjection` internally) and one from a
pre-built `IReadOnlyList<ContactPictureEntry>` (the bridge path, so kill-chain BDA/degradation is
already folded in).

Per-contact resolution:

- **Age / freshness** — `age = currentSimTick − LastSimTick` (clamped to `0` if the tick went
  backwards); `Stale` when `age > effectiveStaleThreshold`. The effective threshold is
  `max(1, baseThreshold / CommsTrackStaleness.StaleThresholdDivisor(commsState, display))` — i.e.
  **degraded/denied comms shortens the stale window** (default base `30` ticks).
- **Confidence** (`ContactProvenanceConfidence`) from the lifecycle string: `Identified → High`,
  `Classified`/`DegradedL1`/`DegradedL2` → `Medium`, `Detected → Low`, else `Unknown`.
- **Quality flags** (`[Flags] ContactProvenanceQualityState`: `CatalogMiss=1`, `Stale=2`,
  `SilentComms=4`) — combinable, **never left implicit**. `CatalogMiss` is set when the target id
  (or its orbat-mapped platform id) resolves in neither `catalog.TryGetPlatformDomain` nor
  `TryGetPlatformPosition`; `SilentComms` when comms is `Degraded`/`Denied`.
- **`OutOfCommsUnknown`** — true only when comms is `Denied` (current state genuinely unknown).

`ContactProvenanceFingerprint.Compute` emits `cp:empty` or
`cp:c=<n>|contactId,observerId,targetId,sourceRef,confidence,freshness,ageTicks,lifecycle,targetId,lastSimTick,lastSimTime(R),outOfComms,quality` — again invariant-culture, ordinal, no wall clock.

---

## 3. `SliceAContactFrame` — the tick-level composition (fail-closed eligibility)

`SliceAContactFrameBridge.Build(ISimWorldSnapshot, DelegationBridge, ICatalogReader?, ISensorToShooterShooterSource?)`
assembles one read-only `SliceAContactFrame` (`KillChain`, `Provenance`, `Chains`, `Contacts`,
`Authorities`, `EligibilityAvailable`, `SimTick`, `SimTime`) **after** a sim tick — it never primes
engagement state or refills magazines. This is the single place the three read-models are wired
together, and it is where the load-bearing **fail-closed eligibility** rule lives:

- **Without an authoritative shooter source, eligibility fails closed** — `ISimWorldSnapshot` alone
  carries no per-shooter side, present geometry, or commitment facts, so `EligibilityAvailable` is
  `false` and the shooter link is reported as *unknown/withheld* downstream. Scenario defaults and
  historical `EngageWorld` contexts are **not** evidence of present shooter eligibility.
- The inner `LiveCandidateGuard` filters candidates to only present-tense-valid shooters: it drops
  self-targeting, dead members (`!IsMemberAlive`), unregistered targets, and units not
  `IsReadyForLaunch`, and it reads **live magazine rounds from the session ledger** — a *missing*
  ledger entry yields `0` rounds (never a seed/refill from configuration).
- An entry is added to `Authorities` (via `C2AuthorityProjector`) only when the eligible shooter is
  registered **and** the snapshot supplies an authoritative `C2AuthorityProjectionContext` with a
  known `TrackSource`. `FireControlSatisfied` is then ANDed with `chain.IsComplete` **and** a
  *current* track (provenance `Fresh` and not `OutOfCommsUnknown`); the context is stamped
  `Lane = Propose`, `RequiredApproval = WeaponsRelease`.

---

## 4. Presentation chrome

### `SensorToShooterPresenter` / `SensorToShooterPresentation` (DRG-181)

`SensorToShooterPresenter.Build(contactId, snapshot, eligibilityAvailable)` formats one chain into
UI lines. It **fails closed**:

- blank `contactId` → `SensorToShooterPresentation.Empty` (`"Select a contact…"`, `sts:empty`);
- `!eligibilityAvailable`, a `null`/empty snapshot, or no matching chain → an **Unknown**
  presentation (`"Chain: UNKNOWN — sensor-to-shooter facts unavailable"`, every link `UNKNOWN`,
  next-action = *"Obtain current sensor-to-shooter facts before relying on technical eligibility."*);
- a matched chain → per-link lines (`Sensor`/`Track`/`Targetability`/`Shooter` with `LINKED`/`BROKEN`
  + unit + cause + detail), a status line that is explicit that `COMPLETE` is *"technical
  eligibility only — not release authority"*, a per-break-cause **NextAction** remediation hint
  (reacquire the sensor, refresh the track, acquire an FC-quality track, inspect shooter
  readiness/envelope/ammo, improve track quality), and the single-chain fingerprint.

`SensorToShooterPanelBinder.Bind` maps the presentation to a flat `SensorToShooterPanelLabels`
bundle for UI Toolkit labels **without re-deriving** any chain facts.

### `SliceAContactLiveSurfaceBinder` (DRG-180)

`SliceAContactLiveSurfaceBinder.Bind(contactId, SliceAContactFrame, combatFrame?)` produces the
`ContactDetail` panel's live provenance surface — a `SliceAContactLiveSurfaceState` of
`SRC` / `CONF` / `AGE` / `LAST-KNOWN` / `COMMS` lines, each paired with a **non-color USS cue class**
(`contact-provenance-cue--{unknown,nominal,degraded,denied}`) so state is never color-only, plus a
**text declutter token** (`[EVIDENCE:{UNKNOWN,STALE,COMMS-DENIED,CATALOG-MISS,FRESH}]`) resolved by
precedence: `null → UNKNOWN`, `OutOfCommsUnknown → COMMS-DENIED`, `CatalogMiss → CATALOG-MISS`,
`Stale → STALE`, else `FRESH`. It also computes deep-link availability
(`ContactExplanationAvailable`, `EngagementExplanationAvailable`, `EngagementInspectionKey` — the
most-recent matching `CombatEvent` via `CombatMapPresenter.KeyFor`). Blank selection or a contact
with neither kill-chain nor provenance rows → `SliceAContactLiveSurfaceState.Empty` (all `—`).

### `SensorToShooterPanelHost` (Unity)

The `#if UNITY_5_3_OR_NEWER` MonoBehaviour host rebuilds in `LateUpdate` only when the
`(frame ref, selected contactId, fingerprint)` triple changes, shows the panel only when
`showPanel && a contact is selected`, and exposes an `Apply(presentation)` seam for the
headless/test path. It reads `DelegationBridgeHost.LastSliceAContacts` — the per-tick producer — and
is off the Tick hot path.

---

## Invariants — do not break

| Invariant | Why |
|-----------|-----|
| **No `UnityEngine` in the core read-models** | `SensorToShooter*` and `ContactProvenance*` are engine-agnostic and unit-testable headless. |
| **Not fingerprinted into the sim** | These are advisory read-models; neither the `sts:` nor `cp:` fingerprints feed `SimWorldHash`/the order-log fingerprint, so the Baltic v2 hash `17144800277401907079` is untouched. |
| **Read-only over the order log** | Projections never mutate `DecisionLog`; they run off the `Tick` hot path (host `LateUpdate` / bridge post-tick). |
| **Deterministic** | Ordinal sorts, invariant culture, no wall clock; equal inputs ⇒ equal fingerprints. |
| **Fail-closed eligibility** | Absent authoritative present-tense shooter facts, `EligibilityAvailable` is `false` and the shooter link/presentation is withheld — scenario defaults and historical contexts are never treated as current eligibility. |
| **Advisory, never authority** | "Chain COMPLETE" is technical eligibility only; the panel issues no fire orders and never re-derives sim truth (ADR-010 §2–3, ADR-007, ADR-001). |

---

## Producer / consumer map

```
order log (DecisionLog)                     ← read-only
  ├─ KillChainContactStateProjection ─┐
  ├─ ContactPictureProjection ────────┼─→ ContactProvenanceProjection ─┐
  ├─ CommsStateProjection ────────────┘                                │
  └─ (kill-chain snapshot) ─→ SensorToShooterProjection ──────────────┤
                                                                       ▼
                              SliceAContactFrameBridge.Build (per tick, fail-closed)
                                     → SliceAContactFrame {KillChain, Provenance, Chains, Authorities…}
                                                                       │
             ┌─────────────────────────────────────────────┬─────────┘
             ▼                                               ▼
  SensorToShooterPresenter → PanelBinder            SliceAContactLiveSurfaceBinder
             ▼                                               ▼
  SensorToShooterPanelHost (Unity LateUpdate)       ContactDetail live-surface labels/cues
```

Upstream kill-chain/custody detail: [track-custody-and-cde-assess.md](track-custody-and-cde-assess.md).
The engage-time source of the abort/withhold reasons the chain surfaces:
[engagement-pipeline.md](engagement-pipeline.md). General read-model rules and the
`Projection → Binder → State` contract: [c2-projection-layer.md](c2-projection-layer.md);
adapter-seam bridges: [c2-presentation-bridges.md](c2-presentation-bridges.md).

---

## Runbook — extend the chain / provenance

1. **Add a break cause** — append to `SensorToShooterBreakCause` (new integer, do not renumber),
   add its label in `SensorToShooterBreakCauseLabels.Format`, wire the detection in the relevant
   `Build*Link` helper, add a `NextAction` arm in `SensorToShooterPresenter`, and extend the
   projection tests. The enum int is part of the `sts:` fingerprint — regenerate any pinned goldens.
2. **Add a provenance quality flag** — extend `[Flags] ContactProvenanceQualityState` (next power of
   two), set it in `ProjectContact`, surface a declutter token + cue in
   `SliceAContactLiveSurfaceBinder`, and update `ContactProvenanceFingerprint` expectations.
3. **Never** move eligibility resolution out of `SliceAContactFrameBridge`/`LiveCandidateGuard` or
   let a host re-derive sim truth — keep the fail-closed rule in one place.
4. Add tests under `src/ProjectAegis.Delegation.Tests/SensorToShooter/` and
   `src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/`; run the full gate (build + solution
   tests + `PlayModeSmoke`). These read-models are outside the replay goldens, so no golden hash moves.
