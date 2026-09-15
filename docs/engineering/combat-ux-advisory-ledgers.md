# Combat-UX advisory read-model ledgers (DRG-223–230)

> **Scope.** The eight pure, headless **advisory read-models** added in the Combat-UX Slice A/B/C
> wave (DRG-223 → DRG-230) under
> [`ProjectAegis.Delegation/`](../../src/ProjectAegis.Delegation/). Each folds *injected facts* or
> an *order-log-derived picture* into a deterministic, replay-stable snapshot that a Command-Review
> presentation panel can display. They all follow the read-only projection contract of
> ADR-010 §2–3 / ADR-007: **no `UnityEngine` dependency**, **no mutation**, **no order enqueue,
> combat resolution, retask, or catalog write**, and they never touch the sim hot path or the
> replay fingerprint. Every one carries an explicit *advisory* flag (`IsOrder` / `IsFireOrder` /
> `IsWeaponsReleaseAuthorization` / `IsAutomaticEngagement`, all `false`) so a consumer can never
> mistake a row for an authorization.
>
> These are the *family* siblings of the single-subsystem read-model docs
> [combat-vfx-projection.md](combat-vfx-projection.md) and
> [combat-domains-hot-tick-hud.md](combat-domains-hot-tick-hud.md). None of them is combat
> authority — the *engage-time* kill-chain lives in [engagement-pipeline.md](engagement-pipeline.md)
> and the authorization gate is [autonomy-roe-gating.md](autonomy-roe-gating.md).

---

## The shared contract

Each subsystem lives in its own folder and ships the same small triad:

| Part | Role |
|------|------|
| `*Input` / `*Facts` / `*Types` | Read-only input record(s) — injected facts, **not** live sim handles. |
| `*Projection.Project(...)` | The pure fold: validates, filters blanks, builds rows, **ordinal-sorts**, returns a snapshot (or the canonical `*Snapshot.Empty`). |
| Fingerprint | A canonical, replay-stable string. Same inputs ⇒ same string; invariant culture; ordinal order; **no wall clock**. Empty input ⇒ a stable `"<prefix>:empty"` sentinel. |

Two DTO shapes recur across the wave:

- **Row ledgers** — take an `IReadOnlyList<…Facts/Input>` and emit a sorted `Rows` snapshot.
  Fingerprint is a dedicated `*Fingerprint.Compute(snapshot)` class:
  `EmploymentLedger`, `EngageNextAction`, `DeclutterFacts`, `EscalationGate`, `IdentityClass`.
- **Posture projectors** — take a single `*Input` and emit a snapshot with a human-readable
  `StatusLine` + an advisory `Kind` enum. Fingerprint is `ComputeFingerprint(snapshot)` on the
  projector itself: `TaskGroupCoord`, `PlatformDegrade`, `MissionIntent`.

> **Not fingerprinted into the sim.** These snapshots and their fingerprints are *presentation*
> hashes for change-detection / test pinning. They are **not** part of the `DecisionLog`
> `ComputeFingerprint()` or the `SimWorldHash`, so the Baltic v2 replay hash
> `17144800277401907079` is untouched. Only `IdentityClass` even *reads* the `DecisionLog` (and it
> reads, never writes).

## The wave at a glance

| DRG | Slice | Subsystem (folder) | Entry point | FP prefix | Tests |
|-----|-------|--------------------|-------------|-----------|-------|
| 223 | C | `TaskGroupCoord` | `TaskGroupCoordProjection.Project(input)` | `tgc:` | 5 |
| 224 | B | `EmploymentLedger` | `EmploymentLedgerProjection.Project(magazines)` | `el:` | 8 |
| 225 | A | `IdentityClass` | `IdentityClassProjection.Project(log, tick, …)` | `ic:` | 9 |
| 226 | B | `EngageNextAction` | `EngageNextActionProjection.Project(inputs)` | `ena:` | 10 |
| 227 | C | `PlatformDegrade` | `PlatformDegradeProjection.Project(input)` | `pdg:` | 4 |
| 228 | A | `EscalationGate` | `EscalationGateProjection.Project(inputs)` | `eg:` | 8 |
| 229 | C | `MissionIntent` | `MissionIntentProjection.Project(input)` | `mi:` | 4 |
| 230 | B | `DeclutterFacts` | `DeclutterFactsProjection.Project(engagements)` | `df:` | 10 |

All tests are NUnit `[Test]` fixtures under
[`src/ProjectAegis.Delegation.Tests/<Subsystem>/`](../../src/ProjectAegis.Delegation.Tests/) (58
total across the wave), and each asserts the empty-sentinel fingerprint plus the row/status rules
below.

## Where it sits

The read-models are a one-way presentation fold; no state flows back into the sim:

```
injected facts  ─or─  DecisionLog (IdentityClass only)
        │
        ▼
   <Subsystem>Projection.Project(...)          (pure, off the DelegationBridge.Tick hot path)
        │
        ▼
   <Subsystem>Snapshot  (sorted Rows / StatusLine + advisory flags == false)
        │
        ▼
   UnityAdapter/CommandReview presentation      (read-only display)
```

**Consumers today (headless-first).** Some snapshots are already wired into the UnityAdapter
Command-Review surface; the rest are validated by their test fixtures pending a presentation bind:

| Subsystem | Wired consumer |
|-----------|----------------|
| `TaskGroupCoord`, `MissionIntent` | [`CommandReview/CoordinationBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/CommandReview/CoordinationBridge.cs) |
| `PlatformDegrade` | [`CommandReview/StatusFrame`](../../src/ProjectAegis.Delegation.UnityAdapter/CommandReview/StatusFrame.cs) |
| `EngageNextAction` | [`Presentation/CombatDetailPresentation`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/CombatDetailPresentation.cs) |
| `EscalationGate`, `IdentityClass`, `EmploymentLedger`, `DeclutterFacts` | tests-only (no presentation binding yet) |

---

## Row ledgers

### `EmploymentLedger` (DRG-224) — magazine / salvo posture

`Project(IReadOnlyList<EmploymentLedgerMagazineFacts>)` emits one `EmploymentLedgerRow` per
`(ShooterId, WeaponFamilyId)` fact (blank ids skipped), sorted **shooter → weapon-family**
(ordinal). `SalvoSize` is floored at `1`. `WithholdReason` is resolved from the ledger arithmetic
against the [engage abort catalog](abort-reason-catalog.md):

| Condition | `WithholdReason` |
|-----------|------------------|
| `RoundsRemaining <= 0` | `AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE` |
| `RoundsRemaining < SalvoSize` | `AbortReasonCatalog.Engage.NO_AMMO` |
| otherwise | `null` (can fire this salvo) |

Ammo SoT is [logistics-ordnance-runtime.md](logistics-ordnance-runtime.md); this is the display fold.

### `EngageNextAction` (DRG-226) — "what unblocks this shot?"

`Project(IReadOnlyList<EngageNextActionInput>)` maps a withheld engagement's `WithholdReason` to the
next corrective action, sorted **shooter → weapon-family**. `ResolveNextActionCode` is a
case-insensitive token match:

| Withhold reason contains | `NextActionCode` |
|--------------------------|------------------|
| `WINCHESTER` / `NO_AMMO` | `EngageNextActionCodes.ReloadRearm` (`RELOAD_REARM`) |
| `ROE` / `WEAPONS_TIGHT` / `RoeHoldFire` | `EngageNextActionCodes.Approval` (`APPROVAL`) |
| anything else / blank | `null` |

### `DeclutterFacts` (DRG-230) — salvo/burst aggregation for map declutter

`Project(IReadOnlyList<DeclutterFactsEngagementFacts>)` **sums `RoundCount`** (floored at `1`) per
`(WeaponFamilyId, ZoomBandToken)` bucket (blank family or band skipped), emitting one
`DeclutterFactsRow` per bucket, sorted **weapon-family → zoom-band**. Zoom bands are the two
`DeclutterFactsZoomBand` tokens `tactical` / `operational`. A single-fact `Project(facts)` overload
wraps the list form.

### `EscalationGate` (DRG-228) — C2 authority → approval gate

`Project` (single or list) runs each `EscalationGateInput.AuthorityContext` through the shared
[`Skills/C2AuthorityProjector`](../../src/ProjectAegis.Delegation/Skills/C2AuthorityProjector.cs)
(DRG-209/DRG-182) and emits a row **only when a gate applies** — weapons-free organic targeting
emits nothing (`eg:empty`). `ResolveGateRow` precedence:

| Authority fact | `GateCode` | `RequiredAuthority` |
|----------------|-----------|---------------------|
| ROE = `HoldFire` | `HOLD_FIRE` | `RequiredApproval.None` |
| ROE = `WeaponsTight` | `WEAPONS_TIGHT` | `RequiredApproval.None` |
| Targeting `ApprovalRequired` | `HIGHER_HQ` | the projected `PendingApproval` (`Operator` / `WeaponsRelease`) |
| otherwise | *(no row)* | — |

Rows carry the authority's `ReasonCode` (falling back to the projector's canonical reason
constants) and sort by `ContactOrOrderId`. The `Skills` projector is the read-only companion of the
engage-time [autonomy / ROE gate](autonomy-roe-gating.md); it never authorizes fire itself.

### `IdentityClass` (DRG-225) — unknown-vs-known contact ledger

The only wave member that reads the order log. `Project(log, currentSimTick, catalog?,
commsDisplay?, staleThresholdTicks?, orbatUnits?)` folds
[`ContactPictureProjection`](c2-projection-layer.md) + `ContactProvenanceProjection` into one
`IdentityClassRow` per contact (sorted by `ContactId`), each with an `IdentityClassification`
(`Unknown` / `Tentative` / `Classified`), a named `ReasonCode`, and an `IdentityConfidenceBand`
(`Unknown` / `Low` / `Medium` / `High`). Classification precedence:

1. `provenance.OutOfCommsUnknown` ⇒ `Unknown` (reason `CommsGap`).
2. provenance `CatalogMiss` **and** lifecycle `Unknown` ⇒ `Unknown` (reason `CatalogMiss`).
3. lifecycle `Unknown` ⇒ `Unknown`; `Detected` ⇒ `Tentative`; `Classified`/`Identified`/BDA
   `DegradedL1`/`DegradedL2` ⇒ `Classified`.

Confidence prefers the provenance band; absent provenance it derives from lifecycle
(`Identified` → High, classified → Medium, `Detected` → Low). Reason labels are exposed via
`IdentityClassRow.ReasonLabel` and are **never empty** for a published row.

## Posture projectors

### `TaskGroupCoord` (DRG-223) — formation / package coordination

`Project(TaskGroupCoordInput)` returns `Empty` on a blank `GroupId`. Members are de-blanked and
ordinal-sorted. `ResolveGapCode` picks the **most specific** gap:

`Split` (fragmented/detached) → `NoC2` (no command node) → `Unassigned` (no package) → `None`.

The `StatusLine` renders `TGC: COORD OK — <package>` or the matching `TGC: GAP …` string, always
suffixed `(advisory — no orders)`.

### `PlatformDegrade` (DRG-227) — own-unit damage-control posture

`Project(PlatformDegradeInput)` emits one `PlatformDegradeUnitRow` per unit (blank ids skipped,
sorted by `UnitId`). Each unit folds four independent subsystem flags into named
`PlatformDegradeCode`s and a `PlatformDegradeSeverityBand` = **max** of the active per-system
severities:

| Flag | Code |
|------|------|
| `MobilityDegraded` | `MOBILITY` |
| `SensorDegraded` | `SENSOR` |
| `WeaponDegraded` | `WEAPON` |
| `CommsDegraded` | `COMMS` |
| none set | `NONE` (severity `None`) |

Severity band is `None` / `Light` / `Heavy`. The `StatusLine` reports
`PDG: ALL UNITS NOMINAL — <n> tracked` or `PDG: <d>/<n> UNITS DEGRADED`, where a unit is "healthy"
iff its only code is `NONE` at severity `None`.

### `MissionIntent` (DRG-229) — mission-command intent posture

`Project(MissionIntentInput)` returns `Empty` unless a `GroupId` **or** `UnitId` is supplied.
Constraints are de-blanked and ordinal-sorted. The `IntentCode` is a `MissionIntentCode` token
(`HOLD` / `NO_STRIKE` / `ATTACK`, or a passthrough), and `AdvisoryRetask` is a
`MissionIntentRetaskAdvice` (`None` / `Withdraw` / `ReAttack`). The `StatusLine` renders e.g.
`MI: HOLD — group <id> — constraints [...] — advisory retask WITHDRAW (advisory — no orders)`.

## Invariants

| Invariant | Why |
|-----------|-----|
| **No `UnityEngine` in the projection types** | All eight are pure `ProjectAegis.Delegation` folders; only the UnityAdapter consumers reference Unity. Keeps the read-models headless-testable. |
| **Advisory flags are always `false`** | `IsOrder` / `IsFireOrder` / `IsWeaponsReleaseAuthorization` / `IsAutomaticEngagement` are hard-wired `false`; the projectors have no order/fire/retask/catalog write path. |
| **Deterministic ordinal ordering** | Every list is ordinal-sorted (row ledgers by their key columns; posture members/constraints alphabetically). No wall-clock, no hash-set enumeration leakage. |
| **Empty-sentinel discipline** | Null/blank/no-match inputs return the canonical `*Snapshot.Empty` and the `"<prefix>:empty"` fingerprint — never null, never a partial row. |
| **Not fingerprinted into the sim** | Presentation hashes only; not in `DecisionLog.ComputeFingerprint()` / `SimWorldHash`, so the Baltic v2 hash is untouched. `IdentityClass` reads the log read-only. |
| **Named codes, never silent** | Every gated/degraded/withhold outcome carries a stable string code (`EscalationGateCode`, `PlatformDegradeCode`, `EngageNextActionCodes`, `IdentityClassReasonCodes`, `TaskGroupCoordGapCode`, `MissionIntentCode`). |

## Extending it

- **Add a row/posture field** — extend the `*Input`/`*Facts` record and the `*Row`/`*Snapshot`
  DTO, thread it through `Project`, and **append** it to the fingerprint (append-only keeps the
  empty sentinel and existing goldens stable). Add a `[Test]` asserting the new field's effect on
  the fingerprint.
- **Add a code** — add the `const string` to the code holder and the `case`/branch in the
  resolver. Unknown inputs already fall through to `null`/`NONE`, so existing fixtures stay valid.
- **Bind a new consumer** — read the snapshot in a UnityAdapter Command-Review bridge/presentation;
  never call these on the `DelegationBridge.Tick` hot path (run them in the post-Tick C2 refresh,
  like the other C2 feeds — see [c2-presentation-bridges.md](c2-presentation-bridges.md)).
- Nothing here needs a replay-golden re-bless — it is read-only presentation.

## Tests

| Suite (all `ProjectAegis.Delegation.Tests`, NUnit) | Covers |
|-----|--------|
| `TaskGroupCoordProjectionTests` (5) | Gap precedence Split/NoC2/Unassigned/None, member sort, `tgc:empty`. |
| `EmploymentLedgerProjectionTests` (8) | Winchester/NO_AMMO withhold arithmetic, salvo floor, shooter→family sort, `el:empty`. |
| `IdentityClassProjectionTests` (9) | Comms-gap/catalog-miss/lifecycle classification, confidence bands, reason labels, `ic:empty`. |
| `EngageNextActionProjectionTests` (10) | Ammo→`RELOAD_REARM`, ROE→`APPROVAL`, unknown→null, sort, `ena:empty`. |
| `PlatformDegradeProjectionTests` (4) | Per-system codes, max-severity fold, degraded-count status line, `pdg:empty`. |
| `EscalationGateProjectionTests` (8) | HoldFire/WeaponsTight/HigherHq rows via `C2AuthorityProjector`, weapons-free→empty, `eg:empty`. |
| `MissionIntentProjectionTests` (4) | Intent status lines, constraint sort, retask advice, `mi:empty`. |
| `DeclutterFactsProjectionTests` (10) | Round-count aggregation per family/zoom-band, sort, `df:empty`. |

## See also

- [combat-vfx-projection.md](combat-vfx-projection.md) — map combat VFX read-model (same contract).
- [combat-domains-hot-tick-hud.md](combat-domains-hot-tick-hud.md) — the six-domain hot-tick HUD read-model.
- [c2-projection-layer.md](c2-projection-layer.md) — the read-only projection contract these follow; source of `ContactPictureProjection`.
- [c2-presentation-bridges.md](c2-presentation-bridges.md) — the UnityAdapter host-feed pattern the Command-Review consumers use.
- [engagement-pipeline.md](engagement-pipeline.md) — the *engage-time* kill-chain that produces the withhold/abort reasons these ledgers display.
- [autonomy-roe-gating.md](autonomy-roe-gating.md) — the authorization gate; `EscalationGate` is its read-only display companion.
- [abort-reason-catalog.md](abort-reason-catalog.md) — the `WINCHESTER_ORDNANCE` / `NO_AMMO` codes `EmploymentLedger` surfaces.
