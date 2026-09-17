# Engage-explain contract projection — combat-event facts → engagement-explanation row

> **ADR:** the **presentation / read-model seam** is
> [`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3
> (a projection is a **client**, not sim authority) and
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md) (the adapter boundary).
> Do **not** cite Git ADR-018 here (that is sensor side-picture / datalink). Combat UX **Slice B**,
> DRG-215; the combat-event facts it reads are the DRG-211 lifecycle (#583); the eventual
> explanation surface is DRG-168.

The **engage-explain contract projection** turns the ordered **combat-event facts** for one
engage-assess leg into a single presentation-facing [`EngageExplainContractDto`](../../src/ProjectAegis.Delegation/EngageExplainContract/EngageExplainContractDto.cs)
— the *"why permitted / why withheld"* row a C2 client shows for a **selected** engagement. It is a
**read-model** derived from combat-event facts; it is **not** authoritative sim state, is **not**
part of the Baltic v2 `SimWorldHash`, and issues no orders. Four engine-agnostic pieces under
[`ProjectAegis.Delegation/EngageExplainContract/`](../../src/ProjectAegis.Delegation/EngageExplainContract/):

1. [`EngageExplainCombatEventTypes`](../../src/ProjectAegis.Delegation/EngageExplainContract/EngageExplainCombatEventTypes.cs)
   — the local mirror of the DRG-211 combat-event facts (phase enum + input record + snapshot).
2. [`EngageExplainContractProjection`](../../src/ProjectAegis.Delegation/EngageExplainContract/EngageExplainContractProjection.cs)
   — the two pure `Project(…)` overloads.
3. [`EngageExplainContractDto`](../../src/ProjectAegis.Delegation/EngageExplainContract/EngageExplainContractDto.cs)
   — the presentation row.
4. [`EngageExplainContractFingerprint`](../../src/ProjectAegis.Delegation/EngageExplainContract/EngageExplainContractFingerprint.cs)
   — the replay-stable canonical fingerprint.

---

## Inputs — combat-event facts (a mirror of DRG-211)

`EngageExplainCombatEventPhase` mirrors the canonical
[`CombatEventPhase`](../../src/ProjectAegis.Delegation/CombatEvents/CombatEventTypes.cs) (DRG-211 / #583)
**field-for-field** — the numbers are an on-disk contract:

| Value | Phase |
|-------|-------|
| `1` | `IntentAccepted` |
| `2` | `Authorized` |
| `3` | `AuthorizationRefused` |
| `4` | `Firing` |
| `5` | `InFlight` |
| `6` | `TerminalOutcome` |

The local copy exists (per its own doc comment) **"for adapter-free compilation on this SHA"** — the
delegation-core projection does not take a hard dependency on the `CombatEvents` type; the adapter
casts `(EngageExplainCombatEventPhase)e.Phase` at the seam (see *Consumer* below).

- **`EngageExplainCombatEventInput`** — a `sealed record` mirroring `CombatEvent` field-for-field:
  `Phase`, `ShooterId`, `TargetId`, `WeaponFamilyId`, `Outcome`, `CorrelationId`, `SimTime`,
  `SimTick`, `ExplanationRef`. Presentation-only input — **no** UI selection / hover / camera / panel
  state.
- **`EngageExplainCombatEventSnapshot`** — the ordered facts for one engage-assess leg. It **owns an
  immutable copy** (`events.ToArray()`) on construction, so a caller mutating its own list afterward
  cannot leak into a captured snapshot. `Empty` is the zero-event snapshot.

---

## The projection — `EngageExplainContractProjection`

Two static, pure overloads. **No** `Tick` hook, order enqueue, or combat resolve.

### `Project(EngageExplainCombatEventInput)` — one decisive event

Used when the caller has **already selected** the decisive event. It switches on `Phase`:

| `Phase` | Result |
|---------|--------|
| `Authorized` | `CreatePermitted` — `WhyPermitted` set, `WhyWithheld` `null` |
| `AuthorizationRefused` | `CreateWithheld` — `WhyWithheld` set, `WhyPermitted` `null` |
| any other phase | `EngageExplainContractDto.Empty` |

### `ProjectFromSnapshot(EngageExplainCombatEventSnapshot?)` — an ordered leg

`null` **or** zero events → `Empty`. Otherwise it scans **all** events, keeping the last `Authorized`
and the last `AuthorizationRefused`, then resolves in this order:

1. a refusal is present → `CreateWithheld(refused)` — **refusal wins over authorization**;
2. else an authorization is present → `CreatePermitted(authorized)`;
3. else → `Empty`.

Rule 1 is the **keystone**: when an explicit `AuthorizationRefused` fact exists in the leg, the row is
**never** a silent permit — the withhold always takes precedence.

### Reason resolution — `ResolveReason` (fail-safe, never silent)

Both `CreatePermitted` and `CreateWithheld` derive their reason string by precedence:

1. `ExplanationRef` if non-whitespace (e.g. `"ENGAGE: CLEAR"`, `"abort:DLZ_OUT"`, `"policy:RoeHoldFire"`
   flowing in from the [engagement pipeline](engagement-pipeline.md) / [ROE gate](autonomy-roe-gating.md));
2. else `Outcome` if non-whitespace;
3. else the literal fallback `"authorization:granted"` (permit) / `"authorization:refused"` (withhold).

So even a fact with an empty `ExplanationRef` **and** empty `Outcome` yields a **named** reason — the
row is never `null`/empty on a populated leg. Every emitted row also carries the event's
`WeaponFamilyId`, `CorrelationId`, and `SimTime`, so it stays correlatable back to the order log.

---

## The row — `EngageExplainContractDto`

A `sealed record`:

```csharp
EngageExplainContractDto(
    string? WhyPermitted,
    string? WhyWithheld,
    string  WeaponFamilyId,
    ulong   CorrelationId,
    double  SimTime)
```

Exactly one of `WhyPermitted` / `WhyWithheld` is non-`null` on a populated row.
`Empty` = `(null, null, "", 0, 0)`. The DTO is presentation-facing but populated **strictly from
combat-event facts** — no UI-derived truth.

---

## Fingerprint — `EngageExplainContractFingerprint.Compute`

`null` **or** a value-empty DTO → `"eec:empty"`. Empty is detected **by value**, so a `with { }`
copy of `Empty` still fingerprints empty. Otherwise:

```
eec:wp=<WhyPermitted>|ww=<WhyWithheld>|wf=<WeaponFamilyId>|cid=<CorrelationId>|st=<SimTime:R>
```

`InvariantCulture`, ordinal ordering, `SimTime` formatted round-trippable (`"R"`), **no wall clock** —
the same inputs always yield the same string.

---

## Consumer — `CombatDetailPresenter`

Unlike the pure projection siblings that have no host binding yet, this projection **is** actively
consumed. [`CombatDetailPresenter.Build`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/CombatDetailPresentation.cs)
is the host-side reader: for a selected `(shooterId, targetId, correlationId)` tuple it filters the
real `CombatEventSnapshot` leg, projects it via

```csharp
EngageExplainContractProjection.ProjectFromSnapshot(new EngageExplainCombatEventSnapshot(
    leg.Select(e => new EngageExplainCombatEventInput((EngageExplainCombatEventPhase)e.Phase,
        e.ShooterId, e.TargetId, e.WeaponFamilyId, e.Outcome, e.CorrelationId,
        e.SimTime, e.SimTick, e.ExplanationRef)).ToArray()));
```

and uses `explanation.WhyWithheld` / `explanation.WhyPermitted` to render the detail card's
**`Policy:`** line (`WhyWithheld` when the refusal is a `policy:` refusal, else `WhyPermitted`). The
cast `(EngageExplainCombatEventPhase)e.Phase` is where the mirrored enum meets the canonical DRG-211
`CombatEventPhase`. `CombatDetailPresenter` is a read-only presenter — a plain C# record with **no**
`UnityEngine` dependency, and it is **not** on the `DelegationBridge` hotpath.

---

## Determinism & invariants

- **Read-model, off the fingerprint.** The projection is derived *from* combat-event facts (themselves
  fingerprinted upstream via the DRG-211 combat-event log); it never writes the log and is **not** part
  of the `SimWorldHash` or order-log fingerprint. The Baltic v2 hash `17144800277401907079` is
  untouched — changing reason mapping cannot move a replay golden.
- **No silent authorization deny.** In the ordered fold an explicit `AuthorizationRefused` always wins
  over an `Authorized` in the same leg, and `ResolveReason` never returns `null`/empty, so every
  withheld row carries a named cause.
- **Immutable snapshot.** `EngageExplainCombatEventSnapshot` copies its event list on construction;
  caller mutation cannot mutate a captured leg.
- **No UI-derived truth.** The input / DTO / snapshot surfaces are asserted free of
  `Selection` / `Hover` / `Camera` / `Panel` / `Visible` / `Chrome` / `IsSelected` members.
- **Pure.** Both `Project` overloads and `Compute` read only their arguments — no RNG, no wall clock.
- **Adapter-free core.** The delegation-core projection carries its **own** phase enum; the
  `UnityAdapter` casts the real `CombatEventPhase` at the seam.

---

## Tests (pins)

All in [`EngageExplainContractProjectionTests`](../../src/ProjectAegis.Delegation.Tests/EngageExplainContract/EngageExplainContractProjectionTests.cs)
(NUnit, `ProjectAegis.Delegation.Tests/EngageExplainContract`, **9** tests):

| Test | Covers |
|------|--------|
| `Permitted_path_populates_why_permitted_from_authorized_combat_event` | `Authorized` leg → `WhyPermitted` from `ExplanationRef`, carries weapon/`CorrelationId`/`SimTime`. |
| `Refused_path_populates_why_withheld_from_abort_combat_event_facts` | `AuthorizationRefused` → `WhyWithheld` from an `abort:…` `ExplanationRef`. |
| `Refused_path_populates_why_withheld_from_policy_combat_event_facts` | `Project` single-event `policy:RoeHoldFire` refusal. |
| `Fingerprint_is_identical_for_identical_inputs` | `Compute` stability. |
| `Fingerprint_value_equal_empty_and_with_copy_fingerprint_as_eec_empty` | `Empty`, value-empty, and `with { }`-copy all → `eec:empty`. |
| `Snapshot_owns_immutable_copy_isolated_from_caller_list_mutation` | Snapshot defensive copy. |
| `Permitted_empty_ref_and_outcome_yields_authorization_granted_not_refused` | Fallback `authorization:granted`. |
| `Refused_empty_ref_and_outcome_yields_authorization_refused` | Fallback `authorization:refused`. |
| `Dto_surface_omits_ui_derived_truth_fields` | Reflection guard against UI-derived fields. |

Part of the ≥1638-test baseline.

---

## See also

- [autonomy-roe-gating.md](autonomy-roe-gating.md) — the decision-time authorization gate that
  produces the `AuthorizationRefused` / `policy:` reasons this row explains.
- [engagement-pipeline.md](engagement-pipeline.md) — the engage/kill-chain resolver whose abort codes
  (`abort:…`) flow in as `ExplanationRef`.
