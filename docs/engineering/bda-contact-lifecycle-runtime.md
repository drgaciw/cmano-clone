# BDA contact-lifecycle runtime — Lost promotion from battle damage

When a platform is destroyed or damaged past the point of usefulness, the tactical **contact
picture** must reflect it: the hostile track a client is watching flips to **Lost**, and — for the
player's own units — the watch officer gets an attention card. This page documents the **bounded
BDA (battle-damage-assessment) contact-lifecycle seam** that turns per-tick catalog damage rows into
sim-kernel `Lost` contact transitions.

It is a small **producer → buffer → consumer** hand-off that sits *between* two already-documented
subsystems: it reads the [catalog damage runtime](catalog-damage-readiness-runtime.md)'s per-tick
`PlatformDamageChange` rows and drives the [detection / contact FSM](detection-pipeline.md)'s `Lost`
state. It is flag-gated on `combatDomainsEnabled`, entirely RNG-free, and deterministic, so it never
perturbs the Baltic v2 replay goldens.

Governed by **ADR-009** (combat-domain validators / bounded deterministic damage,
`TR-combat-dom-003`). The whole seam is **additive-only**: when a scenario does not enable combat
domains the registry is never even constructed, and behaviour is byte-identical to the legacy Baltic
fixtures.

- **Source:** the pure promotion rules in
  [`src/ProjectAegis.Sim/Catalog/BdaContactLifecycleHotTickApplier.cs`](../../src/ProjectAegis.Sim/Catalog/BdaContactLifecycleHotTickApplier.cs);
  the idempotent hand-off buffer in
  [`src/ProjectAegis.Sim/Engage/BdaContactLifecycleRegistry.cs`](../../src/ProjectAegis.Sim/Engage/BdaContactLifecycleRegistry.cs);
  the contact FSM verb it drives in
  [`src/ProjectAegis.Sim/Sensors/PdDetectionContactSimulator.cs`](../../src/ProjectAegis.Sim/Sensors/PdDetectionContactSimulator.cs).
- **Related:** the HP rows it consumes come from the
  [catalog damage runtime](catalog-damage-readiness-runtime.md); the `Unknown → … → Lost` contact
  FSM it mutates is in the [detection pipeline](detection-pipeline.md); the own-side loss it raises
  feeds the [watch attention & auto-pause](watch-attention-autopause.md) spine; the read-model
  mirror that derives the same `Lost` rows *from the order log* is `OrderLogBdaProjection` in the
  [C2 projection layer](c2-projection-layer.md); determinism rules are in
  [`determinism-and-replay.md`](determinism-and-replay.md).

---

## Mental model — a per-tick hand-off

```
[catalog damage hot-tick]                 [sim sensor slice]
PlatformDamageChange rows                  PdDetectionContactSimulator
        │                                          ▲
        ▼  (producer, SimulationSession)           │ (consumer, Baltic harness)
ResolveSortedLostTargets(changes)                  │
   → Registry.MarkLost(id)  ── idempotent set ──►  ApplyFromRegistry
   → ReportOwnSideLoss(id)     + pending list      → DrainNewLostTargets (sorted)
        │                                          → ApplyTargetBdaLost(id)
        ▼                                          → ContactTransition(… → Lost)
   watch attention (own side only)                 → order log (contact transitions)
```

The **producer** runs inside the damage phase; the **consumer** runs inside the sensor phase of the
same tick. The `BdaContactLifecycleRegistry` is the deterministic buffer that carries the newly-lost
platform ids across that phase boundary without either side reaching into the other.

---

## Where it lives

| File | Role |
|------|------|
| [`BdaContactLifecycleHotTickApplier.cs`](../../src/ProjectAegis.Sim/Catalog/BdaContactLifecycleHotTickApplier.cs) | Pure static rules: `IsEnabled`, `ShouldPromoteToLost`, `ResolveSortedLostTargets`, and the `ApplySortedTargets` / `ApplyFromRegistry` verbs that fold registry output into the contact FSM. |
| [`BdaContactLifecycleRegistry.cs`](../../src/ProjectAegis.Sim/Engage/BdaContactLifecycleRegistry.cs) | The idempotent per-run buffer: `MarkLost`, `DrainNewLostTargets`, `PromotedCount`. |
| [`PdDetectionContactSimulator.ApplyTargetBdaLost`](../../src/ProjectAegis.Sim/Sensors/PdDetectionContactSimulator.cs) | Flips every live contact whose `TargetId` matches to `ContactLifecycleState.Lost` (ordinal order, skips already-`Lost`), emitting a [`ContactTransition`](../../src/ProjectAegis.Sim/Sensors/ContactTransition.cs). |
| [`SimulationSession.cs`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs) | Producer wiring: constructs the registry (only when combat domains are on) and calls `ApplyBdaContactLifecycleHotTick` after the damage hot-tick. |
| [`BalticReplayHarness.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs) | Consumer wiring: after the sensor slice, drains the registry via `ApplyFromRegistry` and appends the resulting `Lost` transitions to the order log. |

---

## The promotion rule — `ShouldPromoteToLost`

`BdaContactLifecycleHotTickApplier` operates on a `DamageLifecycleApply` view of each order-log
`PlatformDamageChange` — `(PlatformId, DamageLevel, NewHpPct, ReasonCode)`. A platform is promoted to
`Lost` when **any** of these hold:

| Condition | Meaning |
|-----------|---------|
| `NewHpPct <= 0` | Hull depleted — the platform is dead. |
| `ReasonCode == "Kill"` | An engagement `Kill` outcome (`PlatformDamageChangeReasonCodes.Kill`). |
| `ReasonCode == "Hit"` **and** `DamageLevel >= CombatDamageLevel.MaxLevel` (`3`) | A hit that pushed the platform to the top combat-damage band (`0–3`, `25%`/level). |

Any other row (ambient drain, mine transit, a sub-maximal `Hit`) is **not** a Lost event. The rules
deliberately **mirror `OrderLogBdaProjection`** so the sim-kernel contact state and the order-log
read-model agree on when a contact goes `Lost`.

`ResolveSortedLostTargets(changes)` applies the rule to a batch, **de-duplicates** by platform id
(ordinal `HashSet`), and returns the survivors **sorted ordinal** — the explicit loop + final sort is
an allocation-tuned replacement for `Distinct().OrderBy(...)` that produces byte-identical output, so
the lost-target order is stable for replay hashing.

---

## The buffer — `BdaContactLifecycleRegistry`

A `sealed` per-run object with a two-part contract:

- **`MarkLost(id)`** adds `id` to an ordinal `_promoted` set. It returns `true` only the **first**
  time an id is seen and enqueues it on a `_pending` list; every later call for the same id is a
  no-op returning `false`. A contact is therefore promoted to `Lost` **exactly once** per run.
- **`DrainNewLostTargets()`** returns the pending ids **sorted ordinal** and clears the pending list
  (leaving `_promoted` intact so re-marks stay idempotent). Empty drains return a shared empty array.
- **`PromotedCount`** exposes the cumulative number of distinct promoted ids.

Because `_promoted` persists but `_pending` is drained, the registry only ever surfaces *new* losses
to the consumer — repeated damage rows for an already-dead platform never re-emit a `Lost`
transition.

---

## Wiring

### Producer — `SimulationSession.ApplyBdaContactLifecycleHotTick`

The registry is created in the constructor **only** when the scenario's `EngageDefaults` has
`CombatDomainsEnabled` true (`BdaContactLifecycleHotTickApplier.IsEnabled`); otherwise it stays
`null` and the whole seam is inert.

Each tick, immediately after `CatalogDamageHotTickTracker.ApplyTick` appends its
`PlatformDamageChange` rows and rebinds withdraw trials, the session:

1. maps the tick's damage changes to `DamageLifecycleApply` records,
2. runs `ResolveSortedLostTargets`, and
3. for each lost id, calls `Registry.MarkLost(id)` **and** `ReportOwnSideLoss(id, simTick, "bda:lost")`.

`ReportOwnSideLoss` is a **no-op for non-own-side ids**, so the registry tracks lost platforms on
**both** sides (they all need a contact-picture `Lost`), while only *own-side* losses raise a
[watch-attention](watch-attention-autopause.md) card. Hostile losses stay silent in the watch queue.

### Consumer — the Baltic sensor phase

The producer never touches the contact FSM itself. Draining the buffer and applying it is the
consumer's job, wired today in `BalticReplayHarness`: after the per-tick sensor slice (and after the
separate confirmed-kill pass), when a session has a live registry and a detection simulator it runs

```csharp
foreach (var lostTransition in BdaContactLifecycleHotTickApplier.ApplyFromRegistry(
             pdSim, simTick, harness.SimTime, bdaLifecycle))
{
    bridge.Orchestrator.OrderLog.AppendContactTransition(lostTransition);
}
```

`ApplyFromRegistry` = `DrainNewLostTargets()` → `ApplySortedTargets` → one `ApplyTargetBdaLost` call
per id. `ApplyTargetBdaLost` walks the simulator's tracks in ordinal order, flips every non-`Lost`
contact for that target to `Lost`, and returns the `ContactTransition`s, which the harness appends to
the order log (feeding the detection sub-hash and the C2 contact picture).

> **Not the same as a confirmed kill.** The BDA lifecycle seam is *damage-driven* (`Hit` at max band
> / `Kill` reason / HP ≤ 0). The distinct [`KilledTargetRegistry`](../../src/ProjectAegis.Sim/Engage/KilledTargetRegistry.cs)
> / `ApplyTargetKill` pass handles confirmed engagement kills. Both feed contact transitions; keep
> them separate when reasoning about which path removed a contact.

---

## Determinism invariants

| Rule | Why |
|------|-----|
| **Flag-gated** on `combatDomainsEnabled` | No registry → no new order-log rows → legacy fixtures are byte-identical. |
| **No RNG** anywhere in the seam | Promotion is a pure function of the deterministic damage rows; nothing draws from `SeededRng`. |
| **Ordinal sort at every fan-out** | `ResolveSortedLostTargets`, `DrainNewLostTargets`, and `ApplyTargetBdaLost` all order by id ordinal so transition order is reproducible. |
| **Promote-once idempotency** | `MarkLost` de-dupes via `_promoted`; re-damage of a dead platform never re-emits `Lost`. |
| **Read-only over damage rows** | The producer only *reads* `PlatformDamageChange`; it never mutates the HP ledger or the decision log. |

---

## Tests

| Coverage | File |
|----------|------|
| Promotion rules, sorted/de-duped resolution, `ApplyFromRegistry` fold (7 cases) | [`BdaContactLifecycleHotTickApplierTests.cs`](../../src/ProjectAegis.Sim.Tests/Catalog/BdaContactLifecycleHotTickApplierTests.cs) |
| End-to-end damage → contact-picture behaviour | the catalog-damage and detection suites referenced in [catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md) and [detection-pipeline.md](detection-pipeline.md) |

---

## Extending it

- **Add a new Lost trigger** (e.g. a new damage reason): extend `ShouldPromoteToLost` and add a case
  to `BdaContactLifecycleHotTickApplierTests`. Keep it a pure function of the `DamageLifecycleApply`
  fields, and mirror the change in `OrderLogBdaProjection` so the read-model stays consistent.
- **Wire the consumer into a new host** (e.g. a live Bridge path): call
  `BdaContactLifecycleHotTickApplier.ApplyFromRegistry(pdSim, simTick, simTime, registry)` in that
  host's sensor phase and append the returned transitions to the order log — do not drain the
  registry from more than one place per tick.
- **Never** add a Lost trigger that depends on wall-clock time or RNG; that would break the replay
  goldens governed by [`determinism-and-replay.md`](determinism-and-replay.md).
