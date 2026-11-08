# Kill-chain contact-state projection — developer guide

This is the **kill-chain contact-state** projection (`ProjectAegis.Delegation/Projection/`, DRG-179):
a pure, deterministic read-model that folds order-log **contact** rows (and **BDA** damage rows) into
one Find / Fix / Track / Target (F2T2) picture per contact — *"where is each hostile in the
kill chain right now, and is its track fresh, degraded, or dropped?"*. It is the **foundation** the
rest of the Combat-UX read-model family sits on: the [sensor-to-shooter chain](sensor-to-shooter-chain.md)
(DRG-207), the [track-custody & drop-reason ledger](track-custody-drop-reason-ledger.md) (DRG-222), and
the BDA-assess projection (DRG-216) all consume its output rather than re-deriving phase from the raw
sensor lifecycle. Its load-bearing promise is that the F2T2 phase and the loss/degradation overlay come
**only** from sim / order-log truth (contacts, fire-control facts, BDA rows), never from UI selection,
hover, camera, or panel state (ADR-010).

It lives in the engine-agnostic `ProjectAegis.Delegation` core (net8.0, no `UnityEngine`), so it runs
under plain `dotnet test`. Unlike its downstream siblings, it already has a headless **bridge**
consumer — [`KillChainContactStateBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/KillChainContactStateBridge.cs)
(the read-only C2 publish façade); the Unity **host renderer** is DRG-180 and not wired yet. Verified
against source and pinned by the tests at the end.

- **Types (core):**
  [`KillChainContactState.cs`](../../src/ProjectAegis.Delegation/Projection/KillChainContactState.cs) —
  the `KillChainPhase` (`None` / `Find` / `Fix` / `Track` / `Target`), the `KillChainLossKind`
  loss/degradation overlay (`None` / `Stale` / `DegradedL1` / `DegradedL2` / `Lost`), the
  `KillChainTransitionKind` published-transition enum (`Find` / `Fix` / `Track` / `Target` /
  `Degraded` / `Lost`), the sim-authored `IKillChainFireControlSource` seam, the per-contact
  `KillChainContactState`, the correlated `KillChainContactTransition`, the ordered
  `KillChainContactSnapshot` (+ `Empty`), and the presentation-facing `KillChainContactRow` /
  `KillChainContactPanelState`.
- **Projection + fingerprint (core):**
  [`KillChainContactStateProjection.cs`](../../src/ProjectAegis.Delegation/Projection/KillChainContactStateProjection.cs) —
  the two `Project(…)` overloads (from a `DecisionLog` or a raw `IReadOnlyList<ContactChangeRecord>`),
  the fold/freshness/phase-resolution rules, and `ComputeFingerprint(KillChainContactSnapshot?)` →
  replay-stable canonical string.
- **Binder (core):**
  [`KillChainContactPanelBinder.cs`](../../src/ProjectAegis.Delegation/Projection/KillChainContactPanelBinder.cs) —
  maps a snapshot into `KillChainContactPanelState` display rows + transition lines (labels only; the
  host must **not** re-derive phase).
- **Bridge (adapter):**
  [`KillChainContactStateBridge.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/KillChainContactStateBridge.cs) —
  the read-only `Build(snapshot, log)` / `BindPanel(snapshot)` façade; fire-control comes from
  `ISimWorldSnapshot.HasFireControlTrackOnPrimaryContact` keyed to the primary hostile id, never UI
  selection.
- **Inputs it consumes (all pure order-log projections):** the sensor contact rows
  (`DecisionLog.ContactChanges`) via `ContactPictureProjection`; the BDA contact rows via
  `OrderLogBdaProjection.ProjectBdaContactChanges` (fanned out to every contact on a target); and the
  optional sim-authored `IKillChainFireControlSource` fire-control facts — all read off the
  [order log](order-log-runtime.md) (`DecisionLog`).
- **Related:** the contact lifecycle that produces the raw `Detected` / `Classified` / `Identified` /
  `Lost` labels is [detection-pipeline.md](detection-pipeline.md); the BDA damage rows come from
  [bda-contact-lifecycle-runtime.md](bda-contact-lifecycle-runtime.md) /
  [catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md); the two downstream
  read-models built directly on this one are [sensor-to-shooter-chain.md](sensor-to-shooter-chain.md)
  and [track-custody-drop-reason-ledger.md](track-custody-drop-reason-ledger.md); the general
  read-model rules are [c2-projection-layer.md](c2-projection-layer.md); the read-only C2 presentation
  seam is [c2-presentation-bridges.md](c2-presentation-bridges.md).

---

## The pipeline at a glance

```
DecisionLog (order log)  +  currentSimTick  (+ optional fireControl /
                                              stale+drop thresholds)
        │
        ▼  KillChainContactStateProjection.Project(log, tick, …)   — pure, no side effects
   ContactPictureProjection       →  sensor contact rows (by target)
   OrderLogBdaProjection          →  BDA contact rows (fanned out to all contacts on a target)
        │  merge sensor + BDA rows, then sort (SimTick, SequenceId, ContactId ordinal)
        ▼  per contact: fold each change into a track accumulator (ApplyChange)
   phase = Find → Fix → Track → Target      (monotonic capability booleans)
   loss  = None → Stale → DegradedL1 → DegradedL2 → Lost   (monotonic overlay)
        │
        ▼  per track: apply freshness at currentSimTick (ApplyFreshness)
   age > staleTicks ⇒ Stale ; age > dropTicks ⇒ Lost
        │
        ▼  emit each transition kind at most once (re-emitting on escalation / re-acquire)
   Snapshot  =  contacts (sorted ContactId ordinal)  +  transitions (sorted below)
        │
        ▼  KillChainContactStateProjection.ComputeFingerprint(snapshot)
   replay-stable canonical string ("kc:empty" | "kc:c=<n>|…#t=<m>|…")
```

A `null` log — or a log with no contact rows — returns `KillChainContactSnapshot.Empty`. Otherwise the
snapshot carries **one contact per track id** (the current F2T2 picture) plus the **published
transitions** (the F2T2 / loss history for the run). Contacts are sorted by `ContactId`
(`StringComparison.Ordinal`); transitions are sorted by `SimTick`, then `CorrelationSequenceId`, then
`(int)Kind`, then `ContactId` ordinal.

The two overloads differ only at the front: the `DecisionLog` overload merges the sensor contact rows
with the BDA-derived rows (a `Kill`/`Hit` on a target fans out across every contact holding that target
— see the fan-out test) and then calls the raw-list overload; a log with no BDA rows short-circuits
straight to the sensor rows.

---

## Phase vs. loss — two independent axes

The projection deliberately separates **where a contact is in the kill chain** (the phase) from
**how healthy its track is** (the loss overlay). Presentation binds both; it must not collapse them or
re-derive either (ADR-010).

**`KillChainPhase`** (F2T2, distinct from the sensor `Detected` / `Classified` / `Identified` labels):

| Phase | Meaning | Gate (`ResolvePhase`) |
|-------|---------|-----------------------|
| `None` (0) | Nothing captured yet. | not even detection captured |
| `Find` (1) | Detection captured. | `DetectionCaptured` |
| `Fix` (2) | Location good enough to hold a position. | `LocationSufficient` |
| `Track` (3) | Continuous custody. | `TrackContinuous` |
| `Target` (4) | Fire-control-quality track, engageable. | `Targetable` |

The capability booleans that gate the phase are **monotonic within a live track** and derived in
`RefreshCapabilities`:

- **`DetectionCaptured`** — sticky-true once the lifecycle reaches `Detected` (or any localized/lost
  state).
- **`LocationSufficient`** — `!Lost && (hasFireControl || lifecycle is Classified/Identified/Degraded ||
  already sufficient)`. Sticky-true unless the track is lost.
- **`TrackContinuous`** — `!Lost && !Stale && LocationSufficient && custodyEvidence`, where
  *custody evidence* is a localized lifecycle, more than one source sequence, or a last-update tick
  earlier than the evaluation tick (i.e. the track has actually been held across ticks, not just
  first-seen this tick).
- **`Targetable`** — `TrackContinuous && hasFireControl && Loss == None`.

A `Lost` track forces `TrackContinuous` and `Targetable` back to `false`.

**`KillChainLossKind`** is an independent overlay that only ever **promotes** (increases) within a live
track (`PromoteLoss`): `None → Stale → DegradedL1 → DegradedL2 → Lost`. `Stale` / `Lost` come from
*freshness* (age since last update); `DegradedL1` / `DegradedL2` come from *BDA* rows. The one place
phase and loss interact: a **BDA degradation** (`DegradedL1` / `DegradedL2`) that would otherwise leave
the contact `Target` is knocked back down to `Track` — a hit degrades *targetability* while custody
itself stays continuous. This `Track`-not-`Target` distinction is exactly what the
[sensor-to-shooter chain](sensor-to-shooter-chain.md) reports as `DegradedTrack` (vs. `StaleTrack`).

**Re-acquire:** if a fresh `Detected` / localized change arrives while the track is currently `Stale`
or `Lost`, the loss overlay is reset to `None` and the published-transition flags are cleared, so the
contact re-publishes `Find` (see the re-acquire test → `Find, Lost, Find`).

---

## Freshness (no UI clock)

Staleness and drop are computed from the **sim clock only** (`currentSimTick` vs. the track's last
update), never a wall clock. In `ApplyFreshness`, for any not-yet-`Lost` track:

- `age = currentSimTick − LastSimTick` (clamped to 0 if the tick is behind).
- `age > dropTicks` ⇒ promote to `Lost`.
- else `age > staleTicks` ⇒ promote to `Stale`.

The thresholds default to `DefaultStaleThresholdTicks = 30` (matches
`ScenarioContactLifecycle.Default.StaleThresholdTicks` / the sensor GDD) and
`DefaultDropThresholdTicks = 120` (the sensor GDD drop threshold, not yet on `ScenarioContactLifecycle`).
They are clamped so `staleTicks ≥ 1` and `dropTicks ≥ staleTicks`. Both are `Project` parameters, so a
caller (or a scenario) can tighten them without touching the projection.

---

## Fire-control facts (`IKillChainFireControlSource`)

`Targetable` (and therefore the `Target` phase) requires a fire-control-quality track. That fact is
**not** derived here — it is injected through the `IKillChainFireControlSource.HasFireControlTrack(contactId,
targetId)` seam so the truth stays sim-authored. In the bridge, `SnapshotFireControl` wires it to
`ISimWorldSnapshot.PrimaryHostileContactId` + `HasFireControlTrackOnPrimaryContact` — i.e. keyed to the
primary hostile id, **never** to UI selection or chrome. Passing no source (the default `null`) means no
contact is ever `Targetable`, so the picture tops out at `Track` (see the FC-lookup test).

---

## What a contact and a transition carry

`KillChainContactState` is an immutable presentation-facing record — the *current* kill-chain row for
one contact (sim-clock only; no UI selection, hover, camera, or panel state — enforced by a reflection
test):

| Field | Meaning |
|-------|---------|
| `ContactId` / `TargetId` / `ObserverId` | The track, the hostile it points at, and the friendly holding it. |
| `Phase` | `None` / `Find` / `Fix` / `Track` / `Target` (resolution above). |
| `Loss` | `None` / `Stale` / `DegradedL1` / `DegradedL2` / `Lost` overlay. |
| `DetectionCaptured` / `LocationSufficient` / `TrackContinuous` / `Targetable` | The four monotonic capability booleans that gate `Phase`. |
| `FirstSimTick` / `FirstSimTime` | When the track was first seen. |
| `LastSimTick` / `LastSimTime` | The track's last update (drives freshness). |
| `CorrelationSequenceId` | Order-log correlation stamp (the last contributing row). |
| `SourceSequenceIds` | The distinct order-log sequence ids that built this track, in order. |
| `SourceRefs` | The ordinal-sorted `contact:` / `observer:` / `target:` / `seq:` provenance refs. |

`KillChainContactTransition` is the same provenance stamped at a phase/loss change: `Kind`,
`PreviousPhase → NewPhase`, `Loss`, `SimTick` / `SimTime`, `CorrelationSequenceId`, and `SourceRefs`.
Each transition **kind** is published **at most once per track** — except that an escalating BDA
degradation re-publishes `Degraded` at each new level (`DegradedL1` then `DegradedL2`), and a re-acquire
clears the flags so `Find`/`Fix`/… can re-emit. Within a tick the publish order is `Find → Fix → Track →
Target`, then `Lost` (if lost) or `Degraded` (if any other loss).

`KillChainContactSnapshot` bundles the two `IReadOnlyList`s (+ a static `Empty`).

---

## The panel binder & bridge (display labels only)

`KillChainContactPanelBinder.Bind(snapshot)` maps the snapshot to a headless `KillChainContactPanelState`
— **labels only**, so a Unity host never re-derives phase (DRG-180 will render these):

- Counts: `"KC: <contactCount>"` and `"KC-TX: <transitionCount>"`.
- Per row: a phase label (`"KC: FIND"` / `"KC: FIX"` / `"KC: TRACK"` / `"KC: TARGET"` / `"KC: —"`), a
  CSS-style phase class (`kill-chain-phase--find` … `--none`), the `DET` / `LOC` / `TRK` / `TGT` flags
  (with `: —` when false), a loss label (`"LOSS: STALE"` / `"LOSS: DEGRADED-L1"` / `…-L2` / `"LOSS:
  LOST"` / `"LOSS: —"`), a `"T <lastSimTime>"` stamp, a `"SEQ: <id>"` correlation, and the joined
  source refs.
- Per transition line: `"<simTick> <contactId> <KIND> <prev>-><new> SEQ:<seq>"`.

`KillChainContactStateBridge` is the read-only adapter façade (ADR-010 §2–3): `Build(ISimWorldSnapshot,
DecisionLog)` projects at `currentTick = snapshot.SimTime <= 0 ? 0 : (ulong)snapshot.SimTime` with the
snapshot-keyed fire-control source (both args non-null or it throws `ArgumentNullException`);
`BindPanel(snapshot)` runs the binder. It **never** mutates sim authority and **never** touches
`DelegationBridge`.

---

## Design invariants — never break these

These are load-bearing and enforced by the tests below.

| Invariant | Rule |
|-----------|------|
| **Sim-authored truth only** | Phase, loss, and fire-control all come from contacts / BDA rows / the `IKillChainFireControlSource` seam — never UI selection, hover, camera, or panel state (a reflection test forbids those property names). |
| **Phase ⟂ loss** | Phase (F2T2) and the loss overlay are separate axes; BDA degradation demotes `Target → Track` but does not touch custody, and a `Stale`/`Lost` overlay breaks continuity without erasing what was detected. |
| **Monotonic within a live track** | Capability booleans are sticky-true and `Loss` only promotes — until a re-acquire (fresh detection over `Stale`/`Lost`) explicitly resets them. |
| **Sim-clock freshness** | Staleness/drop are computed from `currentSimTick` vs. the last update, never a wall clock. Thresholds are clamped (`stale ≥ 1`, `drop ≥ stale`). |
| **Pure / read-only** | `Project` consumes a read-only `DecisionLog` (or raw change list) + optional fire-control and returns immutable records. It never enqueues orders, resolves combat, mutates the log, or reads the wall clock. |
| **Deterministic / replay-stable** | Same log + tick + fire-control ⇒ identical contacts, transitions, and fingerprint. Inputs are re-sorted before the fold (unordered input yields the same result), ordinal ordering, invariant culture, no RNG. |
| **Immutable after construction** | Contacts/transitions/rows are `sealed record`s; the snapshot exposes `IReadOnlyList`s built from fresh arrays. |
| **Off the fingerprint** | The kill-chain fingerprint is a projection artifact for equality/regression, **not** a `SimWorldHash` or order-log-replay input — it never affects the Baltic v2 replay hash `17144800277401907079`. |

---

## The fingerprint

`KillChainContactStateProjection.ComputeFingerprint` yields `kc:empty` for a null/empty snapshot (no
contacts *and* no transitions), else `kc:c=<contactCount>` followed by one `|`-delimited segment per
contact, then `#t=<transitionCount>` followed by one `|`-delimited segment per transition. Each contact
segment packs `ContactId`, `TargetId`, `ObserverId`, the integer `Phase`, the integer `Loss`, the four
capability booleans as a packed `1`/`0` group, `FirstSimTick`, `FirstSimTime`, `LastSimTick`,
`LastSimTime` (round-trip `"R"` invariant-culture format), `CorrelationSequenceId`, the `+`-joined
`SourceSequenceIds`, and the `+`-joined `SourceRefs`. Each transition segment packs the integer `Kind`,
`ContactId`, integer `PreviousPhase`, integer `NewPhase`, integer `Loss`, `SimTick`, `SimTime`, and
`CorrelationSequenceId`.

> **Note (accuracy):** like the [sensor-to-shooter](sensor-to-shooter-chain.md#the-fingerprint) and
> [custody](track-custody-drop-reason-ledger.md#the-fingerprint) fingerprints, this joins raw id
> strings with `,` / `+` / `|` and does **not** length-prefix them, so it is not hardened against a
> separator embedded in an id colliding two distinct snapshots. It is a canonical string for
> equality/regression assertions on the sim-clean ids the projection produces — treat it as such, not
> as a collision-resistant hash. If contact/observer/target ids ever admit user/free text,
> length-prefix the segments and update the fingerprint tests.

---

## Extending without breaking replay

1. **Adding a phase or loss level?** Add the enum value(s) **and** the binder labels
   (`FormatPhaseLabel` / `FormatPhaseClass` / `FormatLoss`), slot it into `ResolvePhase` /
   `PromoteLoss`, and pin the new branch with a projection test. Keep the phase ⟂ loss separation.
2. **Adding a capability input?** Wire it through `RefreshCapabilities` (keep it monotonic within a
   live track), and add a test for both the set and the re-acquire-reset path.
3. **Adding a contact/transition field?** Add it to the record **and** to `ComputeFingerprint`'s
   per-contact/per-transition segment, then update the fingerprint tests. A field the fingerprint
   ignores is a silent replay hole.
4. **Changing a source lifecycle label?** The `IsDetection` / `IsLocalized` / `IsLost` /
   `IsDegradedL1` / `IsDegradedL2` helpers classify the raw `ContactChangeRecord.NewState` strings
   (`Detected` / `Classified` / `Identified` / `Lost` + the `BdaContactDamageStates` constants) — keep
   them in lockstep with the [detection](detection-pipeline.md) / [BDA](bda-contact-lifecycle-runtime.md)
   producers.
5. **Before landing:** run the fixtures below plus the full solution suite (`dotnet test`), and confirm
   `ReplayGolden 6/6` and the Baltic v2 hash `17144800277401907079` are unchanged (this projection is
   off the fingerprint, so a correct change here cannot move it — a moved hash means you touched
   something else). Because four downstream read-models consume this projection, also re-run their
   fixtures (`SensorToShooter`, `TrackCustody`, `BdaAssess`).

---

## Tests that pin this doc

All green as of writing (DRG-179). The fixtures are NUnit in the core Delegation test assembly plus the
UnityAdapter test assembly (both headless `dotnet test`).

| Test file | Covers |
|-----------|--------|
| [`KillChainContactStateProjectionTests.cs`](../../src/ProjectAegis.Delegation.Tests/Projection/KillChainContactStateProjectionTests.cs) | 19 cases: empty log ⇒ empty snapshot; `Detected` publishes `Find` only; `Classified` adds `Fix` + `Track`; `Identified` + fire-control adds `Target`; FC on `Detected` is `Fix` (not `Target`) until custody makes it `Track`; a `Lost` change captures the loss and clears targetability; a sim-clock-only stale promotion (no UI clock); the drop threshold promoting `Stale → Lost`; BDA `Hit` degradation → `DegradedL1` (and level-2 → `DegradedL2`, escalation publishing each level); a BDA `Kill` fanning out `Lost` to every contact on the target; a re-acquire after `Lost` re-publishing `Find`; source-sequence correlation; unordered input sorting before the fold; two-seeded-run fingerprint equality; and the DTO reflection test forbidding selection/hover/camera/visibility fields. |
| [`KillChainContactPanelBinderTests.cs`](../../src/ProjectAegis.Delegation.Tests/Projection/KillChainContactPanelBinderTests.cs) | 3 cases: the binder maps phase/loss/flag/transition labels; an empty snapshot ⇒ `KillChainContactPanelState.Empty`. |
| [`KillChainContactStateBridgeTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/KillChainContactStateBridgeTests.cs) | 6 cases: the read-only bridge publishes phase from the order log without UI state, keys fire-control to the primary hostile id, and binds the panel. |

Run just these fixtures:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "FullyQualifiedName~KillChainContact"

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~KillChainContactStateBridge"
```

---

*Verified against source at the paths above. If you change a phase/loss rule, a capability input, the
freshness thresholds, a contact/transition field, the binder labels, or the fingerprint layout, update
this doc (and the four downstream read-model docs when their contract shifts) together.*
