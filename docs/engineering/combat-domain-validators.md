# Combat-domain validators — developer guide

The engage/kill-chain resolver ([`MvpEngagementResolver`](../../src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs))
runs **two independent domain gates** while resolving a shot, one for each of the six
[`CombatDomain`](../../src/ProjectAegis.Sim/Engage/CombatDomain.cs) values
(`Air`, `Surface`, `Subsurface`, `Land`, `Mine`, `Facility`):

1. **ADR-009 aspect validators** — an *opt-in* [`DomainValidatorRegistry`](../../src/ProjectAegis.Sim/Engage/DomainValidatorRegistry.cs)
   of pluggable [`IDomainValidator`](../../src/ProjectAegis.Sim/Engage/IDomainValidator.cs) instances that
   check the weapon **envelope + per-domain aspect flag** before launch. Runs only when the resolver
   is built with `combatDomainsEnabled: true`.
2. **Legacy `CombatDomainValidator`** (req 18) — an *always-on* static gate with a few
   hard-coded per-domain rules (mount online, subsurface firing solution, land half-range).

This page documents what each validator actually checks and how the registry dispatches. The
**overall ordered gate chain** these two steps sit inside (steps 5 and 12 of the kill chain) is in
[engagement-pipeline.md](engagement-pipeline.md); the machine-readable abort codes they emit are in
[abort-reason-catalog.md](abort-reason-catalog.md).

- **Source:** [`src/ProjectAegis.Sim/Engage/`](../../src/ProjectAegis.Sim/Engage/) — engine-agnostic
  pure C#. Both gates are pure functions of the [`EngageContext`](../../src/ProjectAegis.Sim/Engage/EngageContext.cs);
  no RNG, no wall-clock, no mutable state.
- **Design:** [`adr-009-combat-domain-validators.md`](../architecture/adr-009-combat-domain-validators.md).

> **Additive by construction.** The registry defaults to `combatDomainsEnabled: false`, so the
> aspect validators are inert unless a scenario/harness opts in. Every aspect flag on `EngageContext`
> defaults to `true`, and the legacy gate's rules are unchanged, so existing Baltic v2 replay goldens
> are untouched.

---

## Where it lives

| File | Role |
|------|------|
| [`CombatDomain.cs`](../../src/ProjectAegis.Sim/Engage/CombatDomain.cs) | The domain enum (`Air = 0` … `Facility = 5`) + `CombatDomainParser.Parse` (case-insensitive; unknown → `Air`). |
| [`IDomainValidator.cs`](../../src/ProjectAegis.Sim/Engage/IDomainValidator.cs) | The plug-in contract: a `Domain` and `Validate(in EngageContext) → DomainValidateResult`. |
| [`DomainValidatorRegistry.cs`](../../src/ProjectAegis.Sim/Engage/DomainValidatorRegistry.cs) | Holds validators sorted by ordinal `CombatDomain`; `Validate(domain, ctx)` runs only the matching ones. `MvpStubs` is the default aspect set. |
| [`DomainValidateResult.cs`](../../src/ProjectAegis.Sim/Engage/DomainValidateResult.cs) | `Allow` / `Deny(FireAbortReason)` outcome struct. |
| `{Air,Surface,Subsurface,Land,Mine,Facility}AspectDomainValidator.cs` | The six ADR-009 aspect validators (one per domain). |
| [`NoOpDomainValidators.cs`](../../src/ProjectAegis.Sim/Engage/NoOpDomainValidators.cs) | Allow-all stubs (`NoOpAirDomainValidator`, `NoOpSurfaceDomainValidator`) for tests / MVP wiring. |
| [`CombatDomainValidator.cs`](../../src/ProjectAegis.Sim/Engage/CombatDomainValidator.cs) | The always-on legacy static gate (req 18). |

---

## Two tracks, one domain

| | ADR-009 aspect validators | Legacy `CombatDomainValidator` |
|--|---------------------------|-------------------------------|
| Shape | Instances behind `DomainValidatorRegistry` (DI) | `static` method |
| Enabled | Only when `combatDomainsEnabled: true` | Always |
| Kill-chain step | **5** (after policy/ROE, before readiness) | **12** (after magazine, before DLZ) |
| Checks | Weapon envelope **and** the domain's aspect flag | Mount online + subsurface solution + land half-range |
| Deny type | [`FireAbortReason`](../../src/ProjectAegis.Sim/Policy/FireAbortReason.cs) → mapped to `EngagementAbortReason` | [`EngagementAbortReason`](../../src/ProjectAegis.Sim/Engage/EngagementAbortReason.cs) directly |

Both are dispatched by `ctx.CombatDomain`, so exactly one domain's rules apply per shot.

---

## The registry (`DomainValidatorRegistry`)

The registry is constructed from a set of `IDomainValidator`s and **sorts them by ordinal
`CombatDomain`** so dispatch order is stable and deterministic regardless of registration order:

```csharp
_validators = validators.OrderBy(v => (int)v.Domain).ToArray();
```

`Validate(domain, ctx)` iterates the (sorted) validators, skips any whose `Domain` doesn't match,
runs the rest, and returns the **first** `Deny` (short-circuit) or `Allow` if none deny. The default
`DomainValidatorRegistry.MvpStubs` registers all six aspect validators; the resolver falls back to it
when no registry is injected.

In the resolver (step 5), a deny is surfaced through `MapDomainDenial`, which maps the validator's
`FireAbortReason` onto the order-log `EngagementAbortReason`:

```csharp
if (_combatDomainsEnabled)
{
    var domainResult = _domainValidators.Validate(ctx.CombatDomain, in ctx);
    if (!domainResult.Allowed)
        return EngageResult.Aborted(MapDomainDenial(domainResult.AbortReason!.Value));
}
```

---

## The six aspect validators (ADR-009)

Every aspect validator has the **same shape** — allow iff the target range is inside the weapon
envelope **and** the domain-specific aspect flag is set, otherwise deny with that domain's block
reason:

```csharp
if (ctx.Envelope.Contains(ctx.RangeMeters) && ctx.<Domain>AspectInEnvelope)
    return DomainValidateResult.Allow;
return DomainValidateResult.Deny(FireAbortReason.<Domain>AspectBlock);
```

| Domain | `EngageContext` aspect flag | Deny reason (`FireAbortReason` → `EngagementAbortReason`) |
|--------|-----------------------------|-----------------------------------------------------------|
| `Air` | `AirAspectInEnvelope` | `AirAspectBlock` |
| `Surface` | `SurfaceAspectInEnvelope` | `SurfaceAspectBlock` |
| `Subsurface` | `SubsurfaceAspectInEnvelope` | `SubsurfaceAspectBlock` |
| `Land` | `LandAspectInEnvelope` | `LandAspectBlock` |
| `Mine` | `MineAspectInEnvelope` | `MineAspectBlock` |
| `Facility` | `FacilityAspectInEnvelope` | `FacilityAspectBlock` |

All aspect flags default to `true` on `EngageContext`, so a shot is only blocked when a scenario
(or an upstream aspect model) explicitly clears the flag or the range falls outside the envelope.
The `<Domain>AspectBlock` order-log codes are `EngagementAbortReason 16–22` (see
[abort-reason-catalog.md](abort-reason-catalog.md)).

`NoOpDomainValidators` provides allow-all `Air`/`Surface` stubs used where a registry needs a domain
present but no aspect gate is wanted.

---

## The legacy gate (`CombatDomainValidator`, req 18)

The always-on static gate runs at step 12 and returns an `EngagementAbortReason?` (`null` = allow):

1. **Mount online** — `!ctx.MountOnline` → `MountOffline` (checked first, all domains).
2. **Subsurface** — needs both a fire-control track and an identified contact:
   `!HasFireControlTrack` **or** `!ContactIdentified` → `DomainNoSolution`.
3. **Land** — half-range rule: `RangeMeters > Envelope.MaxRangeMeters * 0.5` → `OutOfEnvelope`.
4. **Air / Surface / Mine / Facility** — no legacy-specific rule (`null`).

> **Mine is deliberately un-gated here.** The `Mine` arm returns `null`; the authoritative mine
> gate is the ADR-009 `MineAspectDomainValidator` (active only when `combatDomainsEnabled`). A prior
> version unconditionally denied every mine-domain shot with `DomainNoSolution` regardless of the
> aspect verdict or the opt-in flag — making `Mine` the only domain that could never launch — so
> the arm was corrected to `null`.

---

## Extending safely

- **New aspect rule for a domain?** Implement `IDomainValidator` for that `CombatDomain`, deny with
  the domain's `FireAbortReason.<Domain>AspectBlock`, and register it in `DomainValidatorRegistry`
  (order is normalised by the registry). It only runs when `combatDomainsEnabled` is on.
- **New domain?** Add the `CombatDomain` enum value (append — never renumber; the ordinal drives
  dispatch order), add its `EngageContext` aspect flag (default `true`), add the `FireAbortReason` /
  `EngagementAbortReason` block codes via the [abort manifest](abort-reason-catalog.md), and add a
  validator.
- **New legacy rule?** Add a `switch` arm in `CombatDomainValidator.Validate`. Keep it a pure
  function of `EngageContext`; returning a non-null reason for a domain that used to launch will move
  combat goldens.
- Neither gate may read wall-clock, RNG, or dictionary/hashset iteration order — both feed
  deterministic engage outcomes and the world hash. Re-run the replay goldens + QA Gauntlet after any
  change here.

---

## Tests

`src/ProjectAegis.Sim.Tests/Engage/` pins both gates:

| Test file | Covers |
|-----------|--------|
| [`DomainValidatorRegistryTests`](../../src/ProjectAegis.Sim.Tests/Engage/DomainValidatorRegistryTests.cs) | `MvpStubs` allow paths, per-domain aspect denials, ordinal dispatch, envelope gate. |
| [`CombatDomainValidatorTests`](../../src/ProjectAegis.Sim.Tests/Engage/CombatDomainValidatorTests.cs) | Mount-offline, subsurface solution/identified, land half-range, and the mine-can-launch fix. |

---

## See also

| Doc | For |
|-----|-----|
| [engagement-pipeline.md](engagement-pipeline.md) | The full ordered kill-chain these two gates sit inside (steps 5 and 12) and the `EngageContext` input surface. |
| [abort-reason-catalog.md](abort-reason-catalog.md) | The stable `ENGAGE_ABORT` codes (`*AspectBlock`, `MountOffline`, `DomainNoSolution`, `OutOfEnvelope`) and the manifest → codegen workflow. |
| [catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md) | Another ADR-009 runtime slice (damage/readiness) that runs after engage. |
| [`adr-009-combat-domain-validators.md`](../architecture/adr-009-combat-domain-validators.md) | The design decision behind the pluggable per-domain validator registry. |
