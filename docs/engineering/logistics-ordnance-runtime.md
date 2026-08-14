# Logistics ordnance runtime — magazine bands & Shotgun/Winchester gates

> **Gauntlet variables:** see also [gauntlet-logistics-variables.md](gauntlet-logistics-variables.md)
> (Joker / Bingo / Shotgun / Winchester as fingerprint-proven QA-ladder dimensions).
> **Fuel counterpart:** [logistics-fuel-runtime.md](logistics-fuel-runtime.md).

The **logistics ordnance runtime** is the weapons-remaining half of the logistics model — the
magazine analog to the [fuel](logistics-fuel-runtime.md) Joker/Bingo bands. It answers a single
tactical question each time a unit fires: *how much ordnance is left, and does that forbid the next
shot?* It has three moving parts, all engine-agnostic:

1. a pure band classifier — [`OrdnanceStateBands`](../../src/ProjectAegis.Sim/Logistics/OrdnanceStateBands.cs)
   (`NOMINAL` / `SHOTGUN` / `WINCHESTER`);
2. two kill-chain gates that consult the band inside the engagement resolver —
   [`LogisticsShotgunEngageGate`](../../src/ProjectAegis.Sim/Engage/LogisticsShotgunEngageGate.cs)
   (soft) and [`LogisticsWinchesterEngageGate`](../../src/ProjectAegis.Sim/Engage/LogisticsWinchesterEngageGate.cs)
   (hard);
3. a deterministic order-log row —
   [`OrdnanceStateChangeRecord`](../../src/ProjectAegis.Delegation/Decision/OrdnanceStateChangeRecord.cs)
   emitted on band crossings, plus its read-model line.

> **Scope.** This page is the *runtime* deep-dive (bands, gate placement, emit/latch, order-log
> evidence). The QA/oracle-token angle (which fingerprint tokens a scenario must prove, and the
> shipped `gauntlet-t2-*` pins) lives in [gauntlet-logistics-variables.md](gauntlet-logistics-variables.md);
> the full `engage` **JSON field reference** lives in
> [scenario-policy-authoring.md](scenario-policy-authoring.md). The abort **codes** are cataloged in
> [abort-reason-catalog.md](abort-reason-catalog.md).

---

## The band classifier (`OrdnanceStateBands`)

[`OrdnanceStateBands.Resolve(roundsRemaining, shotgunRoundsThreshold)`](../../src/ProjectAegis.Sim/Logistics/OrdnanceStateBands.cs)
is a pure function returning one of the literal strings `NOMINAL` / `SHOTGUN` / `WINCHESTER`:

| Condition (evaluated in order) | Band | Doctrine meaning |
|--------------------------------|------|------------------|
| `roundsRemaining ≤ 0` | `WINCHESTER` | Out of weapons — all rounds expended. |
| `shotgunRoundsThreshold > 0` **and** `roundsRemaining ≤ shotgunRoundsThreshold` | `SHOTGUN` | Pre-briefed minimum / defensive-residual ordnance. |
| otherwise | `NOMINAL` | Normal magazine state. |

`shotgunRoundsThreshold` of `0` **disables** the Shotgun band — a unit then reports `NOMINAL`
right down to its last round and only flips to `WINCHESTER` at empty. Rounds are integers; there
is no floating-point math and no RNG in the ordnance path.

---

## The two engage gates

Both gates live in the [engagement pipeline](engagement-pipeline.md) and are consulted by
[`MvpEngagementResolver.Resolve`](../../src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs) as
part of its ordered abort chain. Their inputs come from the
[`EngageContext`](../../src/ProjectAegis.Sim/Engage/EngageContext.cs) fields `RoundsRemaining`,
`SalvoSize`, and `ShotgunRoundsThreshold`, but the *authoritative* round count is `liveRounds`
resolved just before the gates:

```csharp
// MvpEngagementResolver.Resolve — the magazine ledger is authoritative once seeded;
// unseeded mounts fall back to EngageContext.RoundsRemaining so a never-seeded key
// is not mistaken for empty.
var liveRounds = _magazines.TryGetRounds(shooterId, mountId, out var tracked)
    ? tracked
    : ctx.RoundsRemaining;
```

| Gate | Kind | Fires when | Result |
|------|------|-----------|--------|
| `LogisticsShotgunEngageGate.Evaluate(ctx, liveRounds)` | **Soft** | `ShotgunRoundsThreshold > 0` **and** `SalvoSize > 1` **and** band is `SHOTGUN` | Abort `ShotgunOrdnance` (`SHOTGUN_ORDNANCE`). Single-round residual/defensive fire (`SalvoSize ≤ 1`) is still allowed. |
| `LogisticsWinchesterEngageGate.Evaluate(liveRounds)` | **Hard** | `liveRounds ≤ 0` | Abort `WinchesterOrdnance` (`WINCHESTER_ORDNANCE`). No offensive engage of any salvo size. |

### Placement in the resolver abort chain

Order matters for which abort reason a denied shot reports. The ordnance gates bracket the
sensor/EMCON/fire-control checks:

```
… → CatalogDamageWithdraw gate
    → LogisticsBingo gate            (fuel — logistics-fuel-runtime.md)
    → resolve liveRounds (ledger|ctx)
    → LogisticsShotgun gate          ← soft: multi-salvo denied in SHOTGUN band
    → TrackSpoofed / EMCON / CEC-FC / fire-control
    → LogisticsWinchester gate       ← hard: empty magazine denied (after FC)
    → MagazineEmpty (unseeded/consume fallback)
    → domain / hypersonic / envelope / DLZ
    → TryConsumeSalvo → launch
```

The Winchester gate sits **after** the doctrine/EMCON/fire-control checks and *replaces* the
pre-launch `MagazineEmpty` abort when the ledger is tracked-empty (the load-bearing
`WINCHESTER_ORDNANCE` code the QA saboteur asserts). The older `MagazineEmpty` abort and the final
`TryConsumeSalvo` guard remain for the unseeded-mount / consume path, so a shot is never launched
against a truly empty magazine.

---

## Order-log record, fingerprint & read-model

`OrdnanceStateChangeRecord` is appended through
[`DecisionLog`](../../src/ProjectAegis.Delegation/Decision/DecisionLog.cs) and folds into the
chronological order log, so it participates in the **order-log fingerprint**
([order-log-runtime.md](order-log-runtime.md)). It does **not** mix into `SimWorldHash`
([deterministic-hashing-and-rng.md](deterministic-hashing-and-rng.md)); do not treat an
ordnance band change as a world-hash input.

| Record | `OrderLogEntryKind` | Fingerprint token (`{}` = field) |
|--------|---------------------|----------------------------------|
| [`OrdnanceStateChangeRecord`](../../src/ProjectAegis.Delegation/Decision/OrdnanceStateChangeRecord.cs) | `OrdnanceStateChange = 18` | `{SimTick}\|{UnitId}\|{PreviousState}\|{NewState}\|{RoundsRemaining}` |

All five tokens are integers/strings — no `FingerprintFloat`, so the row is exactly reproducible.
Because the row is in the fingerprint, **changing a golden scenario's magazine counts or
`shotgunRoundsThreshold` changes its replay hash** — regenerate the affected golden.

The same row renders in the message log
([`MessageLogProjection`](../../src/ProjectAegis.Delegation/Projection/MessageLogProjection.cs))
under the **`ORDNANCE`** category: `Ordnance <unit>: NOMINAL → SHOTGUN (rem 1)`.

---

## How `SimulationSession` drives it

[`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs) is the
only place that primes the gates and emits the row; the ordnance runtime never runs inside
`DelegationBridge.Tick` hot code.

**Priming the `EngageContext`.** When a unit's engage context is built, the session copies
`ShotgunRoundsThreshold` and `SalvoSize` onto it:

```csharp
var shotgunThreshold = Orchestrator.ScenarioPolicy?.EngageDefaults?.ShotgunRoundsThreshold
    ?? template.ShotgunRoundsThreshold;
var primed = template with { SalvoSize = Math.Max(1, salvo),
                             ShotgunRoundsThreshold = Math.Max(0, shotgunThreshold), … };
```

The per-unit magazine ledger is seeded from the catalog via `CatalogMagazineLedgerSeeder`
(falling back to `DefaultMagazineRounds`), and capped to the scenario's `DefaultMagazineRounds`
when that is set — so `liveRounds` reflects real remaining ordnance.

**Emitting the row (`MaybeEmitOrdnanceStateChange`).** Immediately after a successful engage
appends its magazine `Fire` row (mount `0`), the session:

1. no-ops entirely when no magazine ledger is attached (`Magazines == null`);
2. reads `remaining = Magazines.GetRounds(shooter, mountId: 0)` and resolves the band against
   `EngageDefaults.ShotgunRoundsThreshold ?? 1`;
3. latches the last band per unit in an `Ordinal` dictionary and appends an
   `OrdnanceStateChangeRecord` **only on a band change** — a unit that fires while still `NOMINAL`
   emits nothing (first-sight `NOMINAL` is suppressed).

Only mount `0` is tracked today (the emit call passes `mountId: 0`).

---

## Scenario policy `engage` block

The relevant fields live on
[`ScenarioEngageJsonDto`](../../src/ProjectAegis.Data/Scenario/Policy/ScenarioPolicyJsonDto.cs)
(exposed as `EngageDefaults`):

```jsonc
"engage": {
  "defaultMagazineRounds": 2,     // per-mount rounds seeded when catalog is silent (default 2)
  "salvoSize": 1,                 // rounds consumed per engage; > 1 is gated in SHOTGUN (default 1)
  "maxSalvo": null,               // WRA cap: max rounds per engagement (policy GDD)
  "shotgunRoundsThreshold": 1     // remaining in (0, threshold] ⇒ SHOTGUN; 0 disables (default 1)
}
```

| Field | Default | Meaning |
|-------|---------|---------|
| `defaultMagazineRounds` | `2` | Rounds seeded per mount when the catalog does not specify a magazine. |
| `salvoSize` | `1` | Rounds consumed per engage; `> 1` is what the Shotgun soft gate denies. |
| `maxSalvo` | `null` | Weapons-release-authority cap (separate policy layer; see [autonomy-roe-gating.md](autonomy-roe-gating.md)). |
| `shotgunRoundsThreshold` | `1` | Shotgun band width; `0` disables Shotgun (only Winchester at empty). |

---

## Abort codes

Both codes come from the codegen manifest
([`data/glossary/abort_reason_manifest.json`](../../data/glossary/abort_reason_manifest.json)) —
see [abort-reason-catalog.md](abort-reason-catalog.md) for the manifest → enum workflow.

| Enum member | Log code | Layer |
|-------------|----------|-------|
| `EngagementAbortReason.ShotgunOrdnance = 25` | `SHOTGUN_ORDNANCE` | Soft logistics gate (multi-salvo only). |
| `EngagementAbortReason.WinchesterOrdnance = 26` | `WINCHESTER_ORDNANCE` | Hard logistics gate (empty magazine). |

---

## Determinism

The ordnance runtime is a pure function of `(scenario policy, magazine ledger, engage sequence)`:

- rounds and thresholds are **integers** — no floats, no `FingerprintFloat`, no `SeededRng`;
- the per-unit band latch iterates/keys in `Ordinal` order;
- the `OrdnanceStateChangeRecord` and its abort codes are in the fingerprint, so a scenario that
  crosses a band with an active magazine ledger contributes reproducible tokens.

Whether a given scenario emits ordnance rows depends on whether a magazine ledger is attached and a
band crossing actually occurs; scenarios with no magazine seeding (or no depletion) emit none.
**Do not change magazine counts or `shotgunRoundsThreshold` on a replay golden without regenerating
it.**

Pinned by
[`OrdnanceStateBandsTests`](../../src/ProjectAegis.Sim.Tests/Logistics/OrdnanceStateBandsTests.cs),
[`LogisticsShotgunEngageGateTests`](../../src/ProjectAegis.Sim.Tests/Engage/LogisticsShotgunEngageGateTests.cs),
[`LogisticsWinchesterEngageGateTests`](../../src/ProjectAegis.Sim.Tests/Engage/LogisticsWinchesterEngageGateTests.cs),
[`OrdnanceStateChangeOrderLogTests`](../../src/ProjectAegis.Delegation.Tests/Decision/OrdnanceStateChangeOrderLogTests.cs),
and the end-to-end
[`ReplayGoldenBalticMagazineTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/ReplayGoldenBalticMagazineTests.cs).
Shipped scenario pins: `data/scenarios/gauntlet-t2-ordnance-shotgun-winchester.policy.json`
(both bands + `WINCHESTER_ORDNANCE`) and `data/scenarios/gauntlet-t2-shotgun-salvo-deny.policy.json`
(the Shotgun multi-salvo soft deny).

---

## Extending the runtime

| Change | How | Replay impact |
|--------|-----|---------------|
| Tune the Shotgun threshold / magazine size | Edit the scenario `engage` block (gameplay values are data, not C# constants). | Changes band tokens / abort mix on any golden that crosses a band → regenerate it. |
| Add a new ordnance band / threshold | Extend `OrdnanceStateBands.Resolve` + the `ScenarioEngageJsonDto` field + the emit path. | Changes band tokens → regenerate affected goldens. |
| Add a new ordnance abort code | Add it to `abort_reason_manifest.json` and regenerate the enum ([abort-reason-catalog.md](abort-reason-catalog.md)); consult it from a gate. | New abort tokens → regenerate affected goldens. |
| Track more than mount `0` | Widen the `MaybeEmitOrdnanceStateChange` call site to iterate mounts. | New `OrdnanceStateChange` rows → new hash. |

Keep new fields additive and defaulted so existing content is unchanged until a scenario opts in.

---

## Common pitfalls

| Symptom | Cause / fix |
|---------|-------------|
| No `OrdnanceStateChange` rows despite firing | No magazine ledger attached (`Magazines == null`), or the unit never left `NOMINAL` (first-sight `NOMINAL` is suppressed; rows emit only on a band change). |
| A multi-salvo shot is denied `SHOTGUN_ORDNANCE` but single shots work | Working as designed — the Shotgun gate is *soft*: it only blocks `SalvoSize > 1`; residual single-round fire is allowed. |
| Never see `SHOTGUN`, only `WINCHESTER` | `shotgunRoundsThreshold` is `0`, which disables the Shotgun band. Set it `> 0`. |
| Expected `MagazineEmpty` but got `WINCHESTER_ORDNANCE` | When the ledger is tracked-empty, the Winchester gate (after fire-control) intentionally replaces the pre-launch `MagazineEmpty` abort. |
| A magazine golden's hash changed after an `engage` edit | Expected — ordnance rows are in the fingerprint. Regenerate the magazine/ordnance goldens (never the v2 `baltic-patrol` hash unless an ADR changes it). |

---

## See also

| Topic | Where |
|-------|-------|
| Fuel counterpart (Joker/Bingo bands, burn ledger) | [logistics-fuel-runtime.md](logistics-fuel-runtime.md) |
| QA-ladder tokens & shipped `gauntlet-t2-*` pins | [gauntlet-logistics-variables.md](gauntlet-logistics-variables.md) |
| The engage/kill-chain resolver that hosts the gates | [engagement-pipeline.md](engagement-pipeline.md) |
| `engage` JSON field reference | [scenario-policy-authoring.md](scenario-policy-authoring.md) |
| Abort-code manifest → enum workflow | [abort-reason-catalog.md](abort-reason-catalog.md) |
| Order-log / fingerprint / golden workflow | [determinism-and-replay.md](determinism-and-replay.md) |
