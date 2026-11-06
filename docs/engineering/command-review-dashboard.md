# Command Review dashboard (Slice C)

The **Command Review** family (`ProjectAegis.Delegation.UnityAdapter/CommandReview/`) is the
watch-officer *review surface* that composes the Slice A/B advisory read-models into one bounded,
accessible dashboard, plus the one **explicit human** task-group command path that hangs off it.
It answers three questions for the officer — *what is the current picture and own-force status?*
(`StatusFrame`), *why can/can't this contact be engaged, and with what?* (`AdviceFrame`), and
*who owns which responsibility, and where are the gaps?* (`CoordinationSnapshot`) — and then lets
the human issue a deliberate **Hold / Withdraw** group decision through the *existing* player-command
path.

Everything on the **read side** is a *client* of sim truth: it reads the order log read-only and
snapshot capabilities, is **not** folded into `SimWorldHash` or the order-log fingerprint, runs off
the `Tick` hot path (host `LateUpdate`/post-tick), and **issues no orders**. The **command side**
(`CoordinationCommandBridge`) issues nothing of its own — it funnels through
[`C2PlayerCommandBridge` → `TryEnqueueHumanOrder`](c2-command-issuance-runtime.md), so its one
`PlayerOrderRecord` order-log row and replay behaviour are inherited from that path, and it is
blocked whenever a replay viewer is attached. Advisory read-models are advisory, never authority
(ADR-010 §2–3, ADR-007, ADR-001); the Baltic v2 replay hash `17144800277401907079` is untouched by
anything in this doc.

Direct consumer of the Slice A frame documented in
[sensor-to-shooter-chain.md](sensor-to-shooter-chain.md): every bridge here takes the per-tick
`SliceAContactFrame` (or the same order log) as its input.

---

## Layers

| Layer | Type(s) | Role |
|-------|---------|------|
| **Status read-model** | `StatusFrame` / `StatusFrameBridge` / `StatusPresenter` | own-force + contact status folded from snapshot capabilities, projections, and the order log |
| **Advice read-model** | `AdviceFrame` / `AdviceBridge` / `AdviceRuntimeEvidenceBridge` / `AdviceSkillService` | fail-closed, evidence-cited engagement explanation for the selected contact |
| **Coordination read-model** | `CoordinationSnapshot` / `CoordinationBridge` / `CoverageTypes` | per-task-group responsibility, coverage, mission-intent, and named gaps |
| **Dashboard composer** | `CommandReviewDashboard` | folds the three above into bounded, text-labelled sections |
| **Command (write) path** | `CoordinationCommandBridge` / `CoordinationOrderSink` / `CoordinationScopeReconciler` | the one explicit human Hold/Withdraw group decision + its scoped downstream expansion |
| **Acceptance harness** | `CoordinationScenario` | deterministic, fixture-only end-to-end review→decide→tick→effect proof |

Producer: `DelegationBridgeHost` builds `_commandStatus` (`StatusFrameBridge.Build`),
`LastCoordination` (`CoordinationBridge.Build`), and `LastAdvice` (`RefreshAdvice` →
`AdviceBridge.Build`) each tick, then `CommandReviewDashboard.Build` combines them.

---

## 1. `StatusFrame` — own-force + contact status (Slice C)

`StatusFrameBridge.Build(DelegationBridge, ISimWorldSnapshot, SliceAContactFrame)` assembles an
immutable `StatusFrame` of `Contacts` / `Units` / `Emissions` / `ElectronicWarfare` /
`PlatformDegradation` / `Correlations`. Knowledge is explicit — `StatusKnowledge` is
`Unknown(0) / Nominal(1) / Degraded(2) / Offline(3)`, and **`Unknown` is never treated as healthy**
(`StatusFact.Unknown(subject)` is the sentinel).

- **Guards / provenance discipline.** Throws on null args, a non-finite/negative snapshot
  `SimTime`, a contact frame whose time/tick is invalid or **newer than the snapshot**, and any
  supplied unit/sensor/EW fact whose `SourceSimTime` is non-finite, negative, or future. A source
  that returns facts for the wrong `UnitId` is a hard error.
- **Contacts** copy provenance (source/confidence/freshness/age/comms/quality) straight from
  `SliceAContactFrame.Provenance` — no UI re-inference.
- **Units** default to all-`Unknown` unless the snapshot implements `IStatusUnitSource`; the
  order log then overrides `platform` (from `PlatformDamageChanges`: `HP ≤ 0 → Offline`,
  `damage/HP<100 → Degraded`, else `Nominal`) and `comms` (from `CommsStateChanges`:
  `Denied → Offline`, `Degraded → Degraded`, else `Nominal`); readiness comes from
  `bridge.Session.UnitReadiness` when tracked.
- **Emissions** come from `IStatusSensorSource` (known active emitters + EMCON) or fall back to the
  `EmconPostureProjection` policy posture with an explicit **`EMISSIONS: UNKNOWN`** row.
- **EW** comes from `IStatusElectronicWarfareSource` or an explicit `EW: UNKNOWN` row.
- **Platform degradation** is projected via `PlatformDegradeProjection` **only when every unit row
  has complete component knowledge** (sensors/mounts/comms/mobility all known); otherwise `null`.
- **Correlations** are a sequence-ordered `DAMAGE`/`COMMS` timeline copied from the log for
  `SimTime ≤ snapshot.SimTime`.

`StatusPresenter.Build(frame, filter)` renders redundant **text** rows (never colour-only), with a
`ProblemsOnly` filter that keeps units having any `Degraded`/`Offline` component.

## 2. `AdviceFrame` — fail-closed engagement explanation

`AdviceBridge.Build(bridge, snapshot, SliceAContactFrame, selectedContactId?)` produces a complete,
**non-authoritative** explanation for the selected contact. Evidence is resolved current-only:
prefer the snapshot's `IAdviceEvidenceSource.TryGetAdviceEvidence`, else compose native runtime
facts via `AdviceRuntimeEvidenceBridge` (which leaves weapon/range/resource scores explicitly
`unknown-no-exact-runtime-inputs` rather than inventing them).

It **fails closed** to `AdviceFrame.Unavailable(...)` with a specific `AdviceAvailability` +
operator fallback, in order:

| Condition | Availability | Fallback hint |
|-----------|--------------|---------------|
| contact frame time missing / non-finite / ≠ snapshot time | `Stale` | refresh the contact frame before review |
| supplied evidence `ModelAvailable == false` | `ModelUnavailable` | use current contact facts for manual review |
| selected contact not in frame | `EvidenceUnavailable` | select a current contact and refresh evidence |
| contact `LastSimTime` non-finite / future | `Stale` | refresh future-dated contact evidence |
| supplied contactId/simTime mismatch | `Stale` | refresh advisory evidence before review |
| threat/resource evidence keyed to the wrong contact/target | `EvidenceUnavailable` | discard mismatched … evidence and refresh |
| provenance `Stale` / `OutOfCommsUnknown` / `Stale`+`SilentComms` quality | `Stale` | refresh or restore the reporting link before relying on advice |

An `Available` frame carries cited `Evidence` pointers, `Assumptions`, per-candidate `Alternatives`
(`ResourceRank` scores) and excluded `ResourceCommitments`, `PolicyConstraints` (authority
disposition, weapon-policy/ROE, range/DLZ/envelope), and the projected `C2AuthorityProjection`. Two
invariants are hard-wired regardless of inputs: `HardConstraints` always includes
`advisory-only:no-authority-or-order` + `execution-must-revalidate-current-facts`, and both
`IsWeaponsReleaseAuthorization` and `IsFireOrder` are always `false`.

`AdviceSkillService.Invoke(skillId, advice)` exposes three **read-only** advisory functions
(`c2.advice.datalink.assess`, `c2.advice.resource.recommend`, `c2.advice.mission-package.explain`)
— all `SkillLane.Read`, `RequiredApproval.None`, wrapped in the auditable `SkillEnvelope` +
`ReplayProvenance`. **No advisory function names a command.**

## 3. `CoordinationSnapshot` — task-group responsibility & gaps

`CoordinationBridge.Build(bridge, snapshot, ICoordinationFacts?)` projects one
`CoordinationGroupSnapshot` per registered `GroupTarget` (ordinal-sorted), composing
`MissionPackageProjection`, `TaskGroupCoordProjection`, and `MissionIntentProjection` over the log
bounded to `SimTime`. It **never invents** roles, coverage areas, mission intent, or orders — the
optional authored facts (`Packages` / `Coverage` / `Intents` / `Assignments`, or a snapshot that
implements `ICoordinationFacts`) are the only source, and **package overlap alone never assigns a
package** (assignment is explicit).

- **Effects** (`CoordinationEffect`) map each package element to a unit + observed state:
  `Available` / `Lost` (not alive / unavailable) / `Detached` / `UnknownRole` (member with no
  authored role) / `UnknownAvailability` (role known, only last-known).
- **Coverage** (`CoverageAssessment`) is derived from an explicit `CoverageFact` only —
  `Covered` / `Gap` / `Unknown` (`COVERAGE_FACT_MISSING`), and geometry is attached only when
  usable (source-labelled, ≥3 boundary points, all normalized to `[0,1]`).
- **Gaps** (`CoordinationGap`) are retained, never silently dropped: `UNKNOWN_ROLE`, `LOST_MEMBER`,
  `LOST_<ROLE>` / `DETACHED_<ROLE>`, and `SCARCE_SHARED_UNIT` (one available unit owning multiple
  responsibilities).

## 4. `CommandReviewDashboard` — bounded, accessible composition

`CommandReviewDashboard.Build(status, advice, coordination, problemsOnly)` folds the three
read-models into ordered `CommandReviewSection`s: `advice` + `resources` first, then the status
sections (`sensors`, `emissions`, `damage`, `status-timeline`), then `mission-advice` and `groups`.
The advice section restates *"Recommendations do not authorize release."* Every section is capped at
**200 rows plus an explicit truncation-disclosure row** (empty views render an explicit
`No recorded facts for this view.`) — no silent omission, no colour-only state.

## 5. Command path — the one explicit human decision

`CoordinationCommandBridge.Submit(bridge, snapshot, groupId, decision, reviewedIntent?, pendingScopes?)`
is the **only** type here that can cause an order, and recommendations never call it. It validates
the *whole* group scope, then atomically enqueues one decision through
`C2PlayerCommandBridge.TryIssue` (`Hold → "hold" → OrderKind.Hold`,
`Withdraw → "rtb" → OrderKind.ReturnToBase`). `Reattack` is **always** refused
(`REATTACK_ADVISORY_ONLY`). The stable failure-reason catalog:

| Reason | When |
|--------|------|
| `UNKNOWN_GROUP` | null bridge/snapshot, or no matching `GroupTarget` |
| `REPLAY_ATTACHED` | a replay viewer is attached (`bridge.AttachReplayViewer`) |
| `INVALID_SIM_TIME` | snapshot `SimTime` non-finite/negative |
| `GROUP_DECISION_PENDING` | an unresolved pending scope already exists for the group |
| `MISSION_INTENT_SCOPE_MISMATCH` | `reviewedIntent` names a different group |
| `NOT_HUMAN_CONTROL` | the group slot's active controller is not a `HumanController` |
| `EMPTY_GROUP` | the group has no members |
| `MISSING_OR_LOST_MEMBER` | any member is detached, unregistered, or not alive |
| `MISSION_INTENT_CONSTRAINT` | effective intent forbids the decision (e.g. `Hold` blocks `Withdraw`) |
| `REATTACK_ADVISORY_ONLY` | decision is `Reattack` |
| `ENQUEUE_FAILED` | the player-command enqueue failed (or its own reason string) |

(`AUTHORITY_WITHHELD` is a declared reserved reason not currently returned by `Submit`.)

A success returns a `CoordinationApprovedScope` — the player-order sequence id, group entity,
`OrderKind`, execute tick, and the **captured member ids** — that authorizes downstream expansion:

- `CoordinationOrderSink` is an `IOrderSink` decorator: it matches a group-targeted order to an
  unconsumed approved scope (by entity + kind + sim-time + group id), consumes the scope **once**,
  re-verifies the group is still human-controlled and its **current membership exactly equals the
  captured ids**, then fans the order out to each live member entity. Non-group / unmatched orders
  pass through unchanged; a membership drift aborts the expansion (no partial fan-out).
- `CoordinationScopeReconciler.FindInvalidOrExpired(...)` returns scope ids whose group authority or
  membership was lost, or whose execute tick passed with no queued human order — hosts prune these
  after each bridge tick.

`CoordinationScenario` is a deterministic, **fixture-only** acceptance harness that wires the full
review → advisory intent → explicit decision → group enqueue → tick → concrete unit-effects flow;
it is a test/demo surface, not a product code path.

---

## Invariants — do not break

| Invariant | Why |
|-----------|-----|
| **No `UnityEngine` in `CommandReview/`** | the whole family is engine-agnostic and headless-testable; the Unity host only *consumes* it. |
| **Read side not fingerprinted** | `StatusFrame` / `AdviceFrame` / `CoordinationSnapshot` never feed `SimWorldHash` or the order-log fingerprint — the Baltic v2 hash is untouched. |
| **`Unknown` is never healthy** | `StatusKnowledge.Unknown`, `CoverageStatus.Unknown`, and every `*Unavailable` advice frame are explicit — no silent "assume nominal." |
| **Advisory, never authority** | advice carries `advisory-only:no-authority-or-order`; `IsWeaponsReleaseAuthorization`/`IsFireOrder` are always `false`; only `CoordinationCommandBridge` (an explicit human action) can enqueue an order. |
| **Command path reuses the player-command seam** | `CoordinationCommandBridge` issues through `C2PlayerCommandBridge`/`TryEnqueueHumanOrder`, is blocked under `REPLAY_ATTACHED`, and inherits that path's determinism/replay behaviour. |
| **Fail-closed & bounded** | mismatched/stale/future evidence yields an explicit unavailable frame; dashboard sections cap at 200 rows + a disclosure row; group expansion aborts on any membership drift. |
| **Facts never invented** | roles, coverage areas, mission intent, and package assignment come only from authored facts or the log — overlap alone assigns nothing. |

---

## Producer / consumer map

```
SliceAContactFrame  (per-tick; see sensor-to-shooter-chain.md)
order log (DecisionLog, read-only) + ISimWorldSnapshot capabilities
        │
        ├─ StatusFrameBridge.Build ─────────→ StatusFrame ──┐
        ├─ AdviceBridge.Build ──────────────→ AdviceFrame ──┼─→ CommandReviewDashboard.Build → sections
        └─ CoordinationBridge.Build ────────→ CoordinationSnapshot ┘
                                                             │
        (explicit human) CoordinationCommandBridge.Submit ──┴─→ C2PlayerCommandBridge → TryEnqueueHumanOrder
                                                                     │  (approved scope)
                                                             CoordinationOrderSink → downstream IOrderSink (per live member)
                                                             CoordinationScopeReconciler (prune stale scopes per tick)
        DelegationBridgeHost: _commandStatus / LastAdvice / LastCoordination (LateUpdate, off the Tick hot path)
```

Upstream Slice A read-models: [sensor-to-shooter-chain.md](sensor-to-shooter-chain.md). The input
side of the command seam (`C2PlayerCommandBridge`, failure-reason catalog, replay gate):
[c2-command-issuance-runtime.md](c2-command-issuance-runtime.md). General read-model rules and the
`Projection → Binder → State` contract: [c2-projection-layer.md](c2-projection-layer.md);
adapter-seam bridges: [c2-presentation-bridges.md](c2-presentation-bridges.md). The advisory ledger
projections this dashboard surfaces: [combat-ux-advisory-ledgers.md](combat-ux-advisory-ledgers.md)
and [track-custody-and-cde-assess.md](track-custody-and-cde-assess.md).

---

## Runbook — extend the dashboard

1. **Add a status fact source** — implement `IStatusUnitSource` / `IStatusSensorSource` /
   `IStatusElectronicWarfareSource` on the snapshot; keep `SourceSimTime` finite and ≤ snapshot time
   (future-dated facts throw), and never return a fact for a different `UnitId`.
2. **Add an advice availability reason** — extend `AdviceAvailability` and add the fail-closed arm in
   `AdviceBridge.BuildCore` with an explicit operator fallback; keep `IsFireOrder`/
   `IsWeaponsReleaseAuthorization` `false` and the two `HardConstraints` intact.
3. **Add a coordination gap / coverage code** — add the code in `CoordinationBridge.BuildGaps` /
   `ProjectCoverage`; keep it derived from authored `CoverageFact`/roles only (never inferred), and
   keep it in the retained-gaps output rather than dropping the capability.
4. **Add a group decision** — extend `CoordinationDecision` and its command-id map, add the
   constraint rule in `IsConstrained`, and route through `C2PlayerCommandBridge` — never enqueue
   directly. Advisory-only decisions must reject like `Reattack`.
5. Add tests under `src/ProjectAegis.Delegation.UnityAdapter.Tests/CommandReview/` and run the full
   gate (build + solution tests + `PlayModeSmoke`). These surfaces are outside the replay goldens, so
   no golden hash moves.
