# Track custody & CDE-assess advisory read-models (DRG-220 / DRG-222)

> **Scope.** The two remaining pure, headless **advisory read-models** of the Combat-UX wave that
> the family doc [combat-ux-advisory-ledgers.md](combat-ux-advisory-ledgers.md) (DRG-223 → DRG-230)
> did not cover: **`CdeAssess`** (DRG-220, collateral / CDE-assess risk) and **`TrackCustody`**
> (DRG-222, custody + drop-reason ledger), both under
> [`ProjectAegis.Delegation/`](../../src/ProjectAegis.Delegation/). Like their eight siblings each
> folds *injected facts* and/or an *order-log-derived picture* into a deterministic, replay-stable
> snapshot for a Command-Review presentation panel, and each follows the read-only projection
> contract of ADR-010 §2–3 / ADR-007: **no `UnityEngine` dependency**, **no mutation**, **no order
> enqueue, combat resolution, retask, or catalog write**, and they never touch the sim hot path or
> the replay fingerprint. Neither ever carries an authorize/release verdict — `CdeAssess` is a
> *risk classification*, `TrackCustody` is a *custody bookkeeping* picture.
>
> These are the direct family siblings of [combat-ux-advisory-ledgers.md](combat-ux-advisory-ledgers.md),
> [combat-vfx-projection.md](combat-vfx-projection.md), and
> [combat-domains-hot-tick-hud.md](combat-domains-hot-tick-hud.md). None of them is combat
> authority — the *engage-time* kill-chain lives in [engagement-pipeline.md](engagement-pipeline.md)
> and the authorization gate is [autonomy-roe-gating.md](autonomy-roe-gating.md).

---

## The shared contract

Both subsystems live in their own folder and ship the same small triad as the rest of the wave:

| Part | Role |
|------|------|
| `*Types` (`*Input` / `*Row` / `*LedgerEntry` / enums) | Read-only input + output record(s) — injected facts, **not** live sim handles. |
| `*Projection.Project(...)` | The pure fold: validates, resolves named codes, builds rows, **ordinal-sorts**, returns a snapshot (or the canonical `*Snapshot.Empty`). |
| `*Fingerprint.Compute(snapshot)` | A canonical, replay-stable string. Same inputs ⇒ same string; invariant culture; ordinal order; **no wall clock**. Empty snapshot ⇒ a stable `"<prefix>:empty"` sentinel. |

> **Not fingerprinted into the sim.** These snapshots and their fingerprints are *presentation*
> hashes for change-detection / test pinning. They are **not** part of the `DecisionLog`
> `ComputeFingerprint()` or the `SimWorldHash`, so the Baltic v2 replay hash
> `17144800277401907079` is untouched. Both *read* the `DecisionLog` (`CdeAssess` reads
> `PolicyDenials`; `TrackCustody` composes three existing projections over the log) — they read,
> never write.

Where they differ from the eight row-ledger / posture-projector siblings:

- **`CdeAssess` is a single-leg projector.** `Project` takes one `CdeAssessInput` (one shooter →
  target → weapon-family leg) and always returns a **one-row** snapshot — it never returns `Empty`
  (the `Empty`/`cde:empty` sentinel is defensive, for a null/empty snapshot handed to the
  fingerprint). Risk is a three-value `CdeAssessRiskKind` (`Low` / `Elevated` / `Withheld`).
- **`TrackCustody` is a composed picture + ledger.** It is the wave's heaviest order-log reader:
  it composes the `KillChainContactStateProjection`, `CommsStateProjection`,
  `ContactProvenanceProjection`, and `ContactPictureProjection` into a snapshot that carries **two**
  ordinal lists — a current-picture `Rows` and a published-break `Entries` ledger.

## The two subsystems at a glance

| DRG | Subsystem (folder) | Entry point | FP prefix | Tests |
|-----|--------------------|-------------|-----------|-------|
| 220 | `CdeAssess` | `CdeAssessProjection.Project(input, log?)` | `cde:` | 13 |
| 222 | `TrackCustody` | `TrackCustodyProjection.Project(log, tick, …)` | `tc:` | 8 |

All 21 tests are NUnit fixtures under
[`src/ProjectAegis.Delegation.Tests/CdeAssess/`](../../src/ProjectAegis.Delegation.Tests/CdeAssess/)
and
[`src/ProjectAegis.Delegation.Tests/TrackCustody/`](../../src/ProjectAegis.Delegation.Tests/TrackCustody/);
each asserts the empty/single-row rules, the named-code resolution, and fingerprint stability.

## Where it sits

Both are a one-way presentation fold; no state flows back into the sim:

```
injected facts (CdeAssess)  ─or─  DecisionLog (both; TrackCustody via 4 projections)
        │
        ▼
   <Subsystem>Projection.Project(...)      (pure, off the DelegationBridge.Tick hot path)
        │
        ▼
   <Subsystem>Snapshot   (CdeAssess: 1 Row · TrackCustody: sorted Rows + Entries)
        │
        ▼
   UnityAdapter / Command-Review presentation   (read-only display)
```

**Consumers today.** Neither snapshot is bound to a presentation surface yet — both are validated by
their test fixtures pending a Command-Review binding, exactly like the tests-only members of the
[eight-ledger wave](combat-ux-advisory-ledgers.md). When binding one, read the snapshot in a
UnityAdapter Command-Review bridge in the post-Tick C2 refresh (never on the Tick hot path — see
[c2-presentation-bridges.md](c2-presentation-bridges.md)).

---

## `CdeAssess` (DRG-220) — collateral / CDE-assess risk

`CdeAssessProjection.Project(CdeAssessInput input, DecisionLog? log = null)` classifies one engage
leg's collateral-damage-estimate (CDE) posture into an advisory `CdeAssessRiskKind` and emits a
single `CdeAssessRow`. The `input` carries the leg ids (`ShooterId` / `TargetId` /
`WeaponFamilyId`), the sim clock (`SimTick` / `SimTime`), a `CorrelationId`, an optional
[`EngagePreview`](../../src/ProjectAegis.Delegation/Projection/EngagePreviewProjection.cs)
(`DlzLabel` / `CanFire` / `AbortPreviewCode`), an optional `RangeClassLabel`, and the explicit CDE
withhold facts (`CollateralWithheld` + `CollateralWithholdReason`).

### Risk precedence

`Project` resolves risk in a fixed order — the first matching branch wins:

| # | Condition | `RiskKind` | `WithholdReason` | `PolicyConstraintText` |
|---|-----------|-----------|------------------|------------------------|
| 1 | `CollateralWithheld == true` | `Withheld` | `CollateralWithholdReason ?? "CDE_WITHHOLD"` | `CDE/collateral withhold: <reason>` |
| 2 | else a shooter-scoped engage `PolicyDenialRecord` applies | `Withheld` | `denial.Reason.ToString()` | `Policy denial: <Reason> (tick <SimTick>)` |
| 3 | else `Preview.CanFire == false` | `Elevated` | `null` | `PolicyConstraintClear` |
| 4 | else | `Low` | `null` | `PolicyConstraintClear` |

An explicit CDE withhold (row 1) therefore **overrides** a clear preview *and* a policy denial;
a policy denial (row 2) overrides a blocked preview.

### Assumptions & geometry

Every row carries three named `Assumptions` strings (public `const`s on the projector) so the
display is never a silent verdict — one CDE assumption (`AssumptionCdeWithhold` /
`AssumptionNoCdeWithhold`), one preview assumption (`AssumptionPreviewInRange` /
`…OutOfRange` / `AssumptionNoPreview`), and one policy assumption (`AssumptionPolicyDenial` /
`AssumptionNoPolicyDenial`). `GeometryRangeClass` is assembled from the available inputs —
`range:<RangeClassLabel>`, the preview's `DlzLabel`, and `abort:<AbortPreviewCode>` — joined with
` | `, falling back to `GeometryRangeUnknown` (`"geometry/range: not supplied"`) when nothing is
supplied.

### Policy-denial binding (shooter-scoped, parity with CombatEvents)

`FindPolicyDenial` mirrors `CombatEventProjection.FindPolicyDenial` (DRG-211 P1 lesson — do not
invent a second denial contract for the same log shape): it scans `log.PolicyDenials` for entries
where `AttemptedKind == OrderKind.Engage`, the record's **`TargetId.Value == input.ShooterId`**, and
`SimTick >= input.SimTick`, taking the **last** match.

The `TargetId == ShooterId` test is deliberate, not a bug: a `PolicyDenialRecord` stores the
*commanded own unit* on `TargetId` (see `AgentController` / `SimulationSession`), not the hostile
victim. So a denial is bound to the **shooter** that was told to hold fire. `PolicyDenialRecord`
exposes no `CorrelationId`, so attempt-correlated binding is unavailable without a `DecisionLog`
schema change; the `>= tick` last-wins rule is the closest replay-stable proxy. Consequences pinned
by tests: a **victim-scoped** denial (`TargetId == "hostile-1"` when the shooter is `u1`) does *not*
withhold, and a **stale** shooter denial (`SimTick < input.SimTick`) does not apply to a later
attempt.

### Fingerprint

`CdeAssessFingerprint.Compute` returns `cde:empty` for a null/empty snapshot, else
`cde:r=<count>` followed by one `|`-delimited record per row: risk-kind int, then the string ids,
`CorrelationId`, `SimTime` (`"R"` round-trip, invariant culture), `SimTick`, `GeometryRangeClass`,
`PolicyConstraintText`, `WithholdReason` (or empty), and the assumptions (count + each). **Every
string field is length-prefixed (`len:value`)** so a `,` or `|` inside an id cannot collide with the
delimiters — a property directly pinned by
`Fingerprint_resists_comma_and_pipe_collisions_in_ids`.

### Advisory-only DTO

The row and snapshot deliberately carry **no** `Authorized` / `CanFire` (or `Selection` / `Hover` /
`Camera` / `Panel` / `Visible` / `Chrome` / `IsSelected`) property — the reflection-driven
`Dto_surface_omits_ui_derived_truth_fields` test fails the build if one is ever added, guaranteeing
a consumer can never mistake a `Low`/`Elevated` row for a fire authorization. Both `CdeAssessRow`
and `CdeAssessSnapshot` also defensively deep-copy their inputs (`Assumptions` and the row array)
so a caller cannot mutate a published snapshot via a shared list or an `IReadOnlyList` cast-back.

---

## `TrackCustody` (DRG-222) — custody + drop-reason ledger

`TrackCustodyProjection.Project(DecisionLog? log, ulong currentSimTick, …)` answers "do we still
hold this track, and if not, **why did it die?**". It composes four existing order-log projections
and emits a `TrackCustodySnapshot` with two ordinal lists — `Rows` (current custody picture, one per
kill-chain contact, sorted by `ContactId`) and `Entries` (published custody breaks / drops derived
from the kill-chain transitions). It returns `TrackCustodySnapshot.Empty` when `log` is null or the
kill-chain projection has no contacts.

### Inputs & composition

| Parameter | Default | Feeds |
|-----------|---------|-------|
| `log`, `currentSimTick` | — | `KillChainContactStateProjection` (custody SoT) |
| `fireControl` (`IKillChainFireControlSource?`) | `null` | kill-chain fire-control track resolution |
| `catalog` (`ICatalogReader?`), `commsDisplay`, `orbatUnits` | `null` | `ContactProvenanceProjection` |
| `staleThresholdTicks` | `30` (`KillChainContactStateProjection.DefaultStaleThresholdTicks`) | stale / drop timing |
| `dropThresholdTicks` | `120` (`…DefaultDropThresholdTicks`) | stale / drop timing |

`CommsStateProjection.Project(log)` supplies the current `CommsState`; `ContactPictureProjection`
supplies the set of `ContactId`s still in the active tactical picture (used to distinguish an
explicit drop from a timeout drop).

### Custody & cause resolution

`Custody` is `Dropped` iff the kill-chain `Loss == KillChainLossKind.Lost`, else `Held`. The
`TrackCustodyCause` is then resolved so that **a dropped or broken track never carries a silent
`None`** (its `CauseLabel` is a stable plain-language string — `"lost sensor"` / `"stale"` /
`"comms denied"` / `"explicit drop"` / `"unknown"`):

| Custody | Order of checks | `Cause` |
|---------|-----------------|---------|
| `Dropped` | `Lost` **and not** in active picture | `ExplicitDrop` |
| `Dropped` | else `Loss == Lost` | `LostSensor` (timeout drop) |
| `Dropped` | else comms-denied break | `CommsDenied` |
| `Dropped` | else | `Unknown` |
| `Held` | `Loss == Stale` | `Stale` |
| `Held` | else comms-denied break | `CommsDenied` |
| `Held` | else | `None` (label empty) |

A **comms-denied break** requires both the current `CommsState == Denied` *and* provenance evidence
for that contact (`OutOfCommsUnknown`, or the `SilentComms` quality flag). An **explicit drop**
(lifecycle `Lost` that removed the contact from the active picture) is distinguished from a
**timeout drop** (`Lost` while still in the picture) purely by the active-picture membership.

### The ledger `Entries`

`Entries` are built from the kill-chain `Transitions`, not the contact snapshot: a `Lost`
transition becomes a `Dropped` entry whose cause is resolved by the same `ResolveDropCause` rules
(a minimal `KillChainContactState` is synthesized from the transition to reuse the resolver), and a
`Degraded` transition with `Loss == Stale` becomes a `Held` / `Stale` entry. All other transition
kinds are skipped. So `Rows` is the *current* picture while `Entries` is the *audit trail* of the
breaks that produced it, each correlated to its order-log `CorrelationSequenceId`.

### Fingerprint

`TrackCustodyFingerprint.Compute` returns `tc:empty` only when **both** `Rows` and `Entries` are
empty. Otherwise it emits `tc:r=<rowCount>` + one `|`-delimited record per row, then `#e=<entryCount>`
+ one record per entry. Each record appends the ids, the custody/cause enum ints, the tick, the
`SimTime` (`"R"`, invariant culture), and the `CorrelationSequenceId`. (Unlike `CdeAssess`, the
custody fields are appended raw rather than length-prefixed — the ids here are contact / target /
observer ids, not free-text.)

---

## Invariants

| Invariant | Why |
|-----------|-----|
| **No `UnityEngine` in either folder** | Both are pure `ProjectAegis.Delegation` subsystems; only a future UnityAdapter consumer references Unity. Keeps them headless-testable. |
| **No authorization / UI-truth on the DTO** | `CdeAssess` is a *risk classification*, not a fire verdict — the `Dto_surface_omits_…` reflection test bans `Authorized`/`CanFire`/selection/chrome fields. `TrackCustody` carries only custody bookkeeping. |
| **Named codes, never silent** | Every withheld/dropped/broken outcome carries a stable code (`CdeAssessRiskKind` + assumption/policy strings; `TrackCustodyCause` + `TrackCustodyCauseLabels`). A dropped `TrackCustody` row/entry is never `None`. |
| **Deterministic ordinal ordering** | `TrackCustody` rows sort by `ContactId` (ordinal); `CdeAssess` is a single leg. No wall clock, no hash-set enumeration leakage. |
| **Empty/single-row discipline** | `CdeAssess.Project` always returns exactly one row; `TrackCustody` returns `Empty`/`tc:empty` on a null log or no contacts. Fingerprints emit the `"<prefix>:empty"` sentinel, never null. |
| **Immutable after construction** | `CdeAssess` deep-copies `Assumptions` + the row array so a shared-list cast-back cannot mutate a published snapshot. |
| **Not fingerprinted into the sim** | Presentation hashes only; not in `DecisionLog.ComputeFingerprint()` / `SimWorldHash`, so the Baltic v2 hash `17144800277401907079` is untouched. Both read the log read-only. |

## Extending it

- **`CdeAssess` — add a risk input** — extend `CdeAssessInput`, thread it through the precedence
  ladder in `Project`, and **append** the new field to `CdeAssessFingerprint` (append-only keeps the
  empty sentinel and existing fixtures stable). Add a `[Test]` asserting its effect on the
  classification and fingerprint. Never add an `Authorized`/`CanFire` field — the DTO-surface test
  will fail the build.
- **`TrackCustody` — add a drop cause** — add the value to `TrackCustodyCause`, its label to
  `TrackCustodyCauseLabels.Format`, and the branch to `ResolveCause` / `ResolveDropCause`. Keep the
  "dropped rows are never `None`" invariant; add a fixture pinning the new label.
- **Bind a new consumer** — read the snapshot in a UnityAdapter Command-Review bridge in the
  post-Tick C2 refresh, like the other C2 feeds ([c2-presentation-bridges.md](c2-presentation-bridges.md)).
  Never call these on the `DelegationBridge.Tick` hot path.
- Nothing here needs a replay-golden re-bless — both are read-only presentation.

## Tests

| Suite (all `ProjectAegis.Delegation.Tests`, NUnit) | Covers |
|-----|--------|
| `CdeAssessProjectionTests` (13) | The Low/Elevated/Withheld precedence ladder, CDE-withhold-over-preview-and-policy, shooter-scoped vs victim-scoped vs stale policy denials, assumptions + geometry-range assembly, snapshot/assumption immutability, per-weapon-family distinct rows, identical-input fingerprint stability, comma/pipe collision resistance, and the `Authorized`/`CanFire`/UI-truth DTO-surface ban. |
| `TrackCustodyProjectionTests` (8) | Empty-log → `tc:empty`, held-fresh has no cause, stale names `Stale` + publishes a ledger entry, timeout drop → `LostSensor`, explicit lifecycle `Lost` → `ExplicitDrop`, comms-denied on a held contact, the "dropped rows never have an empty cause label" invariant, and replay-stable ordinal-sorted fingerprint. |

Verified green with
`dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "FullyQualifiedName~CdeAssess|FullyQualifiedName~TrackCustody"`
(**21 passed / 0 failed**).

## See also

- [combat-ux-advisory-ledgers.md](combat-ux-advisory-ledgers.md) — the eight-ledger family (DRG-223 → DRG-230) these two complete; same read-only contract.
- [combat-vfx-projection.md](combat-vfx-projection.md) · [combat-domains-hot-tick-hud.md](combat-domains-hot-tick-hud.md) — the single-subsystem read-model siblings.
- [c2-projection-layer.md](c2-projection-layer.md) — the read-only projection contract these follow; source of `ContactPictureProjection` / `ContactProvenanceProjection` / the kill-chain projection `TrackCustody` composes.
- [c2-presentation-bridges.md](c2-presentation-bridges.md) — the UnityAdapter host-feed pattern a future Command-Review consumer would use.
- [engagement-pipeline.md](engagement-pipeline.md) — the *engage-time* kill-chain / policy path that produces the denials `CdeAssess` reads and the losses `TrackCustody` folds.
- [autonomy-roe-gating.md](autonomy-roe-gating.md) — the authorization gate; `CdeAssess` is a read-only risk companion, never a fire verdict.
