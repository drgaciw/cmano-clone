# Allocation plan — the headless multi-target manual weapon-allocation preview (DRG-410)

> **ADR:** this is **read-only presentation** of a proposed manual fire allocation, not an order path
> and not a magazine mutation ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3,
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md),
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md)). It answers one
> planning question — *if the commander drags these shooter × weapon × target lines onto the board, which
> can fire and how many rounds does each one cost?* — and nothing else. It **never** issues an engage
> command, mutates a magazine, or writes to the order log.

The allocation plan is the preview behind a manual, multi-target weapon-assignment board: the player lines
up several shooters against several targets, asks for a quantity per line, and the projection returns a
per-cell *can-fire / abort* verdict plus running shooter and target totals, drawing each accepted line
against a **copy** of the magazine so over-allocation is rejected in the preview instead of at launch. It is
one pure static class — [`AllocationPlanProjection`](../../src/ProjectAegis.Delegation/Projection/AllocationPlanProjection.cs)
(`ProjectAegis.Delegation/Projection/`) — with no Unity, no orchestrator, and no I/O.

| Piece | Role |
|-------|------|
| `AllocationPlanProjection.Project(request)` | The pure fold: `AllocationPlanRequest` → `AllocationPlan`. A `null` request (or a request with null lists) projects an empty, non-fire plan. |
| `AllocationPlanProjection.Fingerprint(plan)` | Replay-stable, length-prefixed signature. Same inputs ⇒ same string; no wall clock. The fingerprint lives **on the projection** — there is no separate `*Fingerprint` file. |
| `AllocationPlanRequest` | Inputs: `Shooters[]`, `Targets[]`, `Allocations[]`, and `Magazines[]` (`EmploymentLedgerMagazineFacts`). Null lists are treated as empty. |
| `AllocationPlanAllocation` | One requested line: `ShooterId`, `WeaponId`, `TargetId`, `Quantity`, the `EngageContext` + `EngagePreview`, and an optional advisory `WeaponRecommendation`. |
| `AllocationPlanCell` | One evaluated line: `CanFire`, `AcceptedQuantity`, the domain `FireAbortReason?`, the catalog/ledger `ReasonCode?`, plain-language `ReasonText`, and `MagazineRemaining` after all draws. |
| `AllocationPlanShooterTotal` / `AllocationPlanTargetTotal` | Per-shooter accepted rounds + remaining magazine (summed across that shooter's weapons), and per-target accepted rounds. |
| `AllocationPlan` | The immutable result: `Cells`, `ShooterTotals`, `TargetTotals`, and `IsFireOrder` — **always `false`**. `AllocationPlan.Empty` is the empty, non-fire singleton. |

---

## The fold — `Project`

`Project` indexes the selected shooter and target ids, projects the magazine facts into a per-`(shooter,
weapon)` round count (via [`EmploymentLedgerProjection`](combat-ux-advisory-ledgers.md), negative rounds
clamped to `0`), then evaluates each allocation **in a deterministic order** against a mutable copy of that
count. Accepted lines draw the copy down; later lines see the reduced balance.

### Deterministic ordering

Allocations are evaluated sorted by `(ShooterId, WeaponId, TargetId)` ordinal, ties broken by original
request index — **not** in the order the lines were handed in. This makes the preview order-independent: the
same set of lines (and the same magazines) yields the same cells, totals, and fingerprint no matter how the
caller shuffles them, so the draw-down order is stable and replay-safe.

### Per-cell abort ladder

Each line is `Accept`ed (clear) or `Reject`ed with the first matching reason, checked in this order:

| Check | Reason code | Notes |
|-------|-------------|-------|
| Shooter id not in `Shooters[]` | `UNKNOWN_SHOOTER` | No rounds drawn. |
| Target id not in `Targets[]` | `UNKNOWN_TARGET` | No rounds drawn. |
| `EngagePreview.CanFire == false` | the preview's `AbortPreviewCode` (e.g. `NO_FIRE_CONTROL_TRACK`, `DLZ_OUT`, `EMCON_OFF`) | Mapped onto a `FireAbortReason` when it is one. |
| Attack-menu option disabled for a non-ammo reason | the menu `DisabledReason` | Ammo-only menu failures are ignored here — the ledger quantity is authoritative (see below). |
| Matching `WeaponRecommendation` withheld by policy/engage | the recommendation's `WithheldReasonCode` (e.g. `ROE_WEAPONS_TIGHT`) | Only when the recommendation's shooter/weapon/target match the line. |
| `Quantity > Recommendation.PolicyConstraints.MaxSalvo` | `WRA_SALVO` | Checked independently of the preview — `ThreatAssessment` only compares `EngageContext.SalvoSize`, so the requested quantity is re-checked here (Codex P2 on DRG-410). |
| `Quantity <= 0` or `Quantity > rounds` with `rounds > 0` | `NO_AMMO` | Over-allocation: the request wanted more than the (remaining) magazine holds. |
| `Quantity > rounds` with `rounds <= 0` | `WINCHESTER_ORDNANCE` | The magazine is already drawn empty (e.g. by an earlier accepted line). |
| otherwise | — | `Accept`: `AcceptedQuantity = Quantity`, the copy is drawn down by `Quantity`, `ReasonText` is the clear engage explanation. |

A rejected line always keeps `AcceptedQuantity = 0` and never draws the magazine, so an over-allocated or
unknown line cannot starve a later valid line. `ReasonText` on every cell (clear or aborted) comes from
[`EngageExplainProjection`](engage-explain-contract-projection.md), so the plan board and the engage-explain
panel read the same plain-language reason.

### Over-allocation and winchester

Because accepted lines draw a shared magazine copy, multiple lines from one shooter compete for the same
rounds in sorted order. If a shooter has 2 rounds and three single-round lines are requested, the first two
`Accept` (drawing the copy to `0`) and the third rejects with `WINCHESTER_ORDNANCE`; the earlier two stand.
A single line asking for more than the magazine holds rejects with `NO_AMMO` without drawing anything. Either
way the shooter total's `MagazineRemaining` reflects the post-draw balance, and `MagazineRemaining` on each
cell is the final copy value after **all** lines are evaluated.

### Totals

`ShooterTotals` lists each distinct shooter (ordinal-sorted) with its summed `AcceptedQuantity` and the
rounds remaining across all of that shooter's weapons after the draws. `TargetTotals` lists each distinct
target with the rounds accepted onto it. Both are derived from the cells and the drawn-down copy — never from
a second magazine read.

---

## The advisory fence — this is a preview, not a trigger

The projection deliberately reads the engage machinery and **discards its authority**:

- `EngageAttackOrderResolver.TryResolve` is called to learn *why* a menu option is legal, then its resolved
  order value is thrown away — a legal menu option allocates no rounds and fires nothing.
- A matching `WeaponRecommendation`'s `IsFireOrder` / `IsWeaponsReleaseAuthorization` /
  `IsAutomaticEngagement` flags are **never** copied onto the cell or the plan. A recommendation can only
  *withhold* (add an abort), never authorize: a line with a "feasible, fire-order" recommendation still just
  produces a clear preview cell, and `AllocationPlan.IsFireOrder` stays `false`.
- `AllocationPlan.IsFireOrder` is hard-wired `false` for every plan `Project` produces, including the empty
  plan.

`AllocationPlanProjectionTests.Source_fence_excludes_unity_orders_and_sim_session` pins this by asserting the
source file contains none of `UnityEngine`, `MonoBehaviour`, `ApplyOrder`, `DecisionLog`, `SimulationSession`,
`DelegationBridge`, `TryIssuePlayerCommand`, or `TryEnqueueAttackOption`.

---

## The fingerprint

`Fingerprint(plan)` returns `ap:empty` for a `null` or fully-empty plan; otherwise it emits
`ap:fire=<0|1>|c=<cells>…|s=<shooterTotals>…|t=<targetTotals>…` with each cell's shooter/weapon/target,
requested + accepted quantity, can-fire bit, abort reason, reason code, magazine remaining, and a
length-prefixed reason text (so a `,`/`|` inside a reason string cannot collide with the delimiters). Same
inputs — in any request order — produce the same string, which is what the determinism test asserts. The
fingerprint is a presentation signature for change detection and replay comparison; it is **not** a
`SimWorldHash` or order-log fingerprint input, so the Baltic v2 replay hash is untouched.

---

## Invariants

- **Preview, not an order** — never issues an engage command, enqueues a player order, mutates a magazine, or
  writes the order log; the resolver's order value and the recommendation's fire flags are discarded.
- **`IsFireOrder` is always `false`** — on every cell and plan, including `AllocationPlan.Empty`.
- **Ledger quantity is authoritative for ammo** — magazine counts come from `EmploymentLedgerProjection`,
  are drawn against a copy, and are never written back; ammo-only attack-menu failures are ignored in favor
  of the ledger balance.
- **Over-allocation is rejected in the preview** — a line asking for more than the remaining magazine aborts
  (`NO_AMMO` / `WINCHESTER_ORDNANCE`) and draws nothing, so an invalid line never starves a valid one.
- **Order-independent / deterministic** — a `static` fold with a stable ordinal sort; no RNG, no wall clock,
  no I/O, no `UnityEngine`. Same shooters/targets/allocations/magazines ⇒ same plan and fingerprint.
- **Off the sim fingerprint** — presentation chrome; the plan is advisory and not an input to the Baltic v2
  replay hash.

---

## See also

| Topic | Doc |
|-------|-----|
| The `EmploymentLedger` magazine/salvo read-model the counts come from | [combat-ux-advisory-ledgers.md](combat-ux-advisory-ledgers.md) |
| The `WeaponRecommendation` the optional advisory line is matched against | [threat-assessment-projection.md](threat-assessment-projection.md) |
| The engage preview / DLZ gate that supplies each line's `CanFire` + abort code | [engagement-pipeline.md](engagement-pipeline.md) |
| The plain-language reason text each cell carries | [engage-explain-contract-projection.md](engage-explain-contract-projection.md) |
| The abort reason codes (`NO_AMMO`, `WINCHESTER_ORDNANCE`, `WRA_SALVO`, domain blocks) | [abort-reason-catalog.md](abort-reason-catalog.md) |
| General read-model / projection / no-mutation rules | [c2-projection-layer.md](c2-projection-layer.md) |

---

## Tests

[`AllocationPlanProjectionTests`](../../src/ProjectAegis.Delegation.Tests/Projection/AllocationPlanProjectionTests.cs)
pins the 2×3 can-fire grid + totals, the mixed clear/domain-abort plan, the "recommendation fire flag neither
authorizes nor blocks a clear cell", the `MaxSalvo` rejection (`WRA_SALVO`), over-allocation (`NO_AMMO`, no
magazine draw), the later-line `WINCHESTER_ORDNANCE` with earlier lines standing, order-independent replay
determinism, `UNKNOWN_TARGET` drawing no rounds, the empty/null non-fire plan, and the source fence that keeps
Unity orders and the sim session out of the projection.
