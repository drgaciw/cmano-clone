# Track custody & drop-reason ledger — developer guide

This is the **track custody + drop-reason ledger** (`ProjectAegis.Delegation/TrackCustody/`, DRG-222):
a pure, deterministic read-model that folds the kill-chain contact state, contact provenance, comms
state, and the active tactical picture into one custody picture — *"do we still hold this track, and
if it died, **why**?"*. Its load-bearing promise is that a dropped or broken track **never** shows a
silent/empty cause: an operator always sees a named reason (lost sensor / stale / comms denied /
explicit drop / unknown).

It lives in the engine-agnostic `ProjectAegis.Delegation` core (net8.0, no `UnityEngine`), so it runs
under plain `dotnet test`. It is a projection-layer building block: as of writing it has **no host or
bridge consumer yet** — it is exercised only by its own test fixture, awaiting a presentation surface
(the natural consumer is a C2 contact-detail panel behind the read-only presentation seam in
[c2-presentation-bridges.md](c2-presentation-bridges.md), which already wraps the same order-log
projections). Verified against source and pinned by the tests at the end.

- **Types (core):**
  [`TrackCustodyTypes.cs`](../../src/ProjectAegis.Delegation/TrackCustody/TrackCustodyTypes.cs) —
  `TrackCustodyState` (`Held` / `Dropped`), `TrackCustodyCause`
  (`None` / `LostSensor` / `Stale` / `CommsDenied` / `ExplicitDrop` / `Unknown`), the stable
  `TrackCustodyCauseLabels` plain-language map, the presentation-facing `TrackCustodyRow`, the
  correlated `TrackCustodyLedgerEntry`, and the ordered `TrackCustodySnapshot` (+ `Empty`).
- **Projection (core):**
  [`TrackCustodyProjection.cs`](../../src/ProjectAegis.Delegation/TrackCustody/TrackCustodyProjection.cs) —
  `Project(DecisionLog? log, ulong currentSimTick, …)` and the custody/cause resolution rules.
- **Fingerprint (core):**
  [`TrackCustodyFingerprint.cs`](../../src/ProjectAegis.Delegation/TrackCustody/TrackCustodyFingerprint.cs) —
  `Compute(TrackCustodySnapshot?)` → replay-stable canonical string.
- **Inputs it consumes (all pure order-log projections):** the kill-chain contact state +
  transitions from `KillChainContactStateProjection`, the `CommsStateProjection` comms state, the
  `ContactProvenanceProjection` per-contact provenance, and the active-picture id set from
  `ContactPictureProjection` — all read off the [order log](order-log-runtime.md) (`DecisionLog`).
- **Related:** the general read-model rules are [c2-projection-layer.md](c2-projection-layer.md); the
  contact lifecycle that produces `Lost` / `Stale` transitions is
  [detection-pipeline.md](detection-pipeline.md) and
  [bda-contact-lifecycle-runtime.md](bda-contact-lifecycle-runtime.md); the comms `Denied` state is
  [comms-degradation-runtime.md](comms-degradation-runtime.md); the read-only C2 presentation seam
  that would surface it is [c2-presentation-bridges.md](c2-presentation-bridges.md).

---

## The pipeline at a glance

```
DecisionLog (order log)  +  currentSimTick  (+ optional fireControl / catalog /
                                              commsDisplay / stale+drop thresholds / orbat)
        │
        ▼  TrackCustodyProjection.Project(log, tick, …)   — pure, no side effects
   KillChainContactStateProjection  →  contacts + transitions
   CommsStateProjection             →  current comms state
   ContactProvenanceProjection      →  per-contact provenance (comms-break signals)
   ContactPictureProjection         →  active-picture id set (explicit-drop discriminator)
        │
        ▼  per contact: custody = Lost ? Dropped : Held ; cause = resolution below
   Rows  (one per kill-chain contact — held or dropped — sorted by ContactId ordinal)
   Entries (one per Lost / Stale kill-chain transition — the drop-reason ledger)
        │
        ▼  TrackCustodyFingerprint.Compute(snapshot)
   replay-stable canonical string ("tc:empty" | "tc:r=<n>|…#e=<m>|…")
```

A `null` log — or a log with no kill-chain contacts — returns `TrackCustodySnapshot.Empty`. Otherwise
the snapshot carries a **row per contact** (the current custody picture) and a **ledger entry per
custody-changing transition** (the append-style drop/break history for the run).

---

## Custody & cause resolution (the precedence that matters)

**Custody** is the simple part: a contact whose kill-chain `Loss` is `Lost` is `Dropped`; everything
else is `Held`.

**Cause** is resolved in a fixed, fail-loud order so a dead track always carries a named reason.

For a **`Dropped`** row (`ResolveDropCause`):

1. **`ExplicitDrop`** — the contact was explicitly `Lost` in its lifecycle *and* is no longer in the
   active tactical picture (`ContactPictureProjection`). Explicit lifecycle `Lost` removes the contact
   from the picture; a timeout drop leaves it, which is exactly what discriminates the two.
2. **`LostSensor`** — otherwise, a kill-chain `Loss == Lost` (the timeout / sensor-loss drop).
3. **`CommsDenied`** — otherwise, a comms-denied break (see the predicate below).
4. **`Unknown`** — the fail-closed floor. A dropped row is never `None`.

For a **`Held`** row (`ResolveCause`):

1. **`Stale`** — kill-chain `Loss == Stale` (degraded but not dropped).
2. **`CommsDenied`** — a comms-denied break.
3. **`None`** — a fresh, fully-held track with no break. This is the *only* case with an empty
   `CauseLabel`.

The **comms-denied break** predicate (`HasCommsDeniedBreak`) requires *all* of: the current
`CommsState == Denied`, non-null provenance, and the provenance reports either `OutOfCommsUnknown`
or the `SilentComms` quality flag. Comms being denied globally is not enough — the specific contact's
provenance must show the break.

`TrackCustodyCauseLabels.Format` maps each cause to a stable plain-language string
(`"lost sensor"` / `"stale"` / `"comms denied"` / `"explicit drop"` / `"unknown"`); `None` maps to the
empty string. `TrackCustodyRow.CauseLabel` / `TrackCustodyLedgerEntry.CauseLabel` surface it so callers
and tests match on the label, not on free text.

---

## What a row and a ledger entry carry

`TrackCustodyRow` is an immutable presentation-facing record — the *current* custody of one contact
(sim-clock only; no UI selection, hover, camera, or panel state):

| Field | Meaning |
|-------|---------|
| `ContactId` / `TargetId` / `ObserverId` | The track, the hostile it points at, and the friendly holding it. |
| `Custody` | `Held` / `Dropped`. |
| `Cause` | `None` / `LostSensor` / `Stale` / `CommsDenied` / `ExplicitDrop` / `Unknown` (resolution above). |
| `CauseLabel` (derived) | Plain-language cause; empty **only** for a `Held` / `None` track. |
| `LastKnownTick` / `LastKnownSimTime` | The contact's last-known sim stamp. |
| `CorrelationSequenceId` | Order-log correlation stamp carried through from the kill-chain state. |

`TrackCustodyLedgerEntry` is the same shape stamped at the transition (`SimTick` / `SimTime` instead of
`LastKnown*`). Entries are built from the kill-chain **transitions**: a `Lost` transition emits a
`Dropped` entry with its resolved drop cause; a `Degraded` transition whose `Loss == Stale` emits a
`Held` / `Stale` entry; all other transitions are skipped. The entry list is the drop-reason **ledger**
— the "why did this track die / degrade, and when" history — while the rows are the current picture.

Rows are sorted by `ContactId` (`StringComparison.Ordinal`) for a stable, replay-safe order.

---

## Design invariants — never break these

These are load-bearing and enforced by the tests below.

| Invariant | Rule |
|-----------|------|
| **No silent drop** | A `Dropped` (or degraded-`Stale`) row/entry never carries `Cause.None` / an empty `CauseLabel`. Operators always see *why* a track died. |
| **Fail-closed cause** | The drop-cause chain ends at `Unknown`, never `None`, when no more specific cause matches. |
| **Pure / read-only** | `Project` consumes a read-only `DecisionLog` (+ optional readers) and returns immutable records. It never enqueues orders, resolves combat, mutates the log, or reads the wall clock. |
| **Order-log-derived** | Every input is a pure projection off the same order log; the custody picture never invents contacts the kill-chain didn't produce (empty log ⇒ `Empty`). |
| **Deterministic / replay-stable** | Same log + tick ⇒ identical rows, entries, and fingerprint. Ordinal ordering, invariant culture, no RNG, no wall clock. |
| **Immutable after construction** | Rows/entries are `sealed record`s; the snapshot exposes `IReadOnlyList`s built from fresh arrays. |
| **Off the fingerprint** | The custody fingerprint is a projection artifact for equality/regression, **not** a `SimWorldHash` or order-log-replay input — it never affects the Baltic v2 replay hash `17144800277401907079`. |

---

## The fingerprint

`TrackCustodyFingerprint.Compute` yields `tc:empty` for a null/empty snapshot (no rows *and* no
entries), else `tc:r=<rowCount>` followed by one `|`-delimited segment per row, then `#e=<entryCount>`
followed by one `|`-delimited segment per entry. Each segment packs the ids, the integer enum values
for `Custody`/`Cause`, the tick, the sim time (round-trip `"R"` invariant-culture format), and the
correlation sequence id, comma-joined.

> **Note (accuracy):** unlike the CDE-advisory fingerprint, this fingerprint joins raw id strings with
> `,` / `|` and does **not** length-prefix them, so it is not hardened against a `,`/`|` embedded in an
> id colliding two distinct snapshots. It is a canonical string for equality/regression assertions on
> the sim-clean ids the projection actually produces — treat it as such, not as a
> collision-resistant hash. If custody ids ever admit user/free text, length-prefix the segments (see
> [`CdeAssessFingerprint`](../../src/ProjectAegis.Delegation/CdeAssess/CdeAssessFingerprint.cs) for the
> hardened pattern) and update the fingerprint tests.

---

## Extending without breaking replay

1. **Adding a custody cause?** Add the `TrackCustodyCause` enum value **and** its
   `TrackCustodyCauseLabels` string, slot it into the ordered `ResolveDropCause` / `ResolveCause`
   precedence, and pin the new branch with a projection test. Keep the fail-closed floor (`Unknown`
   for dropped, `None` only for a clean-held track).
2. **Adding a row/entry field?** Add it to the record (immutable; defensively copied if it is a
   collection) **and** to `TrackCustodyFingerprint.AppendRow` / `AppendEntry`, then update the
   fingerprint tests. A field the fingerprint ignores is a silent replay hole.
3. **Changing a transition→entry mapping?** Update `MapTransition` and add a test for the new
   transition kind; keep the "dropped/stale entries always name a cause" invariant.
4. **Before landing:** run the fixture below plus the full solution suite (`dotnet test`), and confirm
   `ReplayGolden 6/6` and the Baltic v2 hash `17144800277401907079` are unchanged (this projection is
   off the fingerprint, so a correct change here cannot move it — a moved hash means you touched
   something else).

---

## Tests that pin this doc

All green as of writing (DRG-222). The fixture is NUnit in the core Delegation test assembly
(headless `dotnet test`).

| Test file | Covers |
|-----------|--------|
| [`TrackCustodyProjectionTests.cs`](../../src/ProjectAegis.Delegation.Tests/TrackCustody/TrackCustodyProjectionTests.cs) | Empty log ⇒ empty snapshot + `tc:empty` fingerprint; fresh held track has `None` cause + empty label; stale track names the `Stale` cause and publishes a held/`Stale` ledger entry; timeout drop names `LostSensor` and marks `Dropped`; explicit lifecycle `Lost` names `ExplicitDrop`; denied comms names `CommsDenied` on a still-held contact; dropped rows/entries never have an empty cause label; fingerprint is replay-stable for identical inputs and rows sort by `ContactId` ordinal. |

Run just this fixture:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "FullyQualifiedName~TrackCustody"
```

---

*Verified against source at the paths above. If you change a custody/cause rule, a transition→entry
mapping, a row field, or the fingerprint layout, update this doc (and, once a host consumes it, the
consuming presentation doc) together.*
