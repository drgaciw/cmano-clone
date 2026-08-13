# Combat-domain aspect validators — the ADR-009 domain gate registry

> **Scope.** The pluggable **combat-domain aspect validators** in
> [`src/ProjectAegis.Sim/Engage/`](../../src/ProjectAegis.Sim/Engage/) (ADR-009): the
> `IDomainValidator` seam, the ordering `DomainValidatorRegistry`, the six per-domain aspect gates
> (`AirAspectDomainValidator` … `FacilityAspectDomainValidator` + `MineAspectDomainValidator`), the
> allow-all `NoOp*` stubs, and the **legacy** always-on `CombatDomainValidator` — plus how the
> engagement resolver wires the two layers. This is the deep-dive behind **step 5** (and the legacy
> **step 12**) of the kill-chain documented in [`engagement-pipeline.md`](engagement-pipeline.md),
> which lists only the *block reasons*; this page documents the *aspect geometry* and the registry
> mechanism itself.
>
> Boundary rationale: [ADR-009 (combat-domain validators)](../architecture/adr-009-combat-domain-validators.md),
> req 18. Everything here is a **pure function of `EngageContext`** — no RNG, no wall-clock, no
> mutation — so it is deterministic and cannot perturb the replay hash (`17144800277401907079`).
> Distinct from the **damage-time** [mine *transit* hazard](catalog-damage-readiness-runtime.md)
> (a per-tick applier): this is the **engage-time** mine/aspect launch gate.

---

## Two layers, one intent

Domain checks run in **two independent places** in `MvpEngagementResolver`, deliberately split by
ADR-009:

| Layer | Type | Runs | Opt-in? |
|-------|------|------|---------|
| **Aspect validators** (this page's focus) | `DomainValidatorRegistry` of `IDomainValidator` | Step 5 — after policy/ROE, before geometry/magazine | **Yes** — only when `combatDomainsEnabled` |
| **Legacy domain gate** | `CombatDomainValidator` (static, req 18) | Step 12 — mount/track/half-range checks | No — always on |

The aspect layer is the ADR-009 authoritative envelope+aspect gate per `CombatDomain`; the legacy
layer is the older req-18 mount/solution gate that predates it. Both must pass.

---

## `IDomainValidator` & `DomainValidateResult`

```csharp
public interface IDomainValidator
{
    CombatDomain Domain { get; }
    DomainValidateResult Validate(in EngageContext ctx);   // runs after policy, before geometry/magazine
}
```

`DomainValidateResult` is a `readonly struct` with two factories:

- `DomainValidateResult.Allow` — passed (`Allowed = true`, no reason).
- `DomainValidateResult.Deny(FireAbortReason reason)` — blocked, carrying the domain-specific
  `FireAbortReason`.

The `in`-parameter keeps the (large) `EngageContext` value type copy-free.

---

## The six aspect validators

Every aspect validator follows the **same uniform rule** — allow iff the target range is inside the
weapon envelope **and** the per-domain aspect flag is set, else deny with that domain's block reason:

```csharp
// e.g. SurfaceAspectDomainValidator
if (ctx.Envelope.Contains(ctx.RangeMeters) && ctx.SurfaceAspectInEnvelope)
    return DomainValidateResult.Allow;
return DomainValidateResult.Deny(FireAbortReason.SurfaceAspectBlock);
```

| Validator | `CombatDomain` | Aspect flag (in `EngageContext`) | Deny reason |
|-----------|----------------|----------------------------------|-------------|
| `AirAspectDomainValidator` | `Air` (0) | `AirAspectInEnvelope` | `AirAspectBlock` |
| `SurfaceAspectDomainValidator` | `Surface` (1) | `SurfaceAspectInEnvelope` | `SurfaceAspectBlock` |
| `SubsurfaceAspectDomainValidator` | `Subsurface` (2) | `SubsurfaceAspectInEnvelope` | `SubsurfaceAspectBlock` |
| `LandAspectDomainValidator` | `Land` (3) | `LandAspectInEnvelope` | `LandAspectBlock` |
| `MineAspectDomainValidator` | `Mine` (4) | `MineAspectInEnvelope` | `MineAspectBlock` |
| `FacilityAspectDomainValidator` | `Facility` (5) | `FacilityAspectInEnvelope` | `FacilityAspectBlock` |

The `*AspectInEnvelope` flags on `EngageContext` all **default to `true`** — an unset scenario is
permissive; a scenario that computes an out-of-aspect geometry flips the flag to block the shot. The
resolver maps the `FireAbortReason` to the public `EngagementAbortReason` (`AirAspectBlock = 16` …
`FacilityAspectBlock = 22`; `MineAspectBlock = 21`) surfaced in the order log — see
[`abort-reason-catalog.md`](abort-reason-catalog.md).

> **Mine aspect vs. mine transit.** `MineAspectDomainValidator` is the **engage-time** gate on
> *firing a mine-domain weapon*. It is unrelated to the **damage-time** mine *transit* hazard
> applier (a platform crossing a mined zone) documented in
> [`catalog-damage-readiness-runtime.md`](catalog-damage-readiness-runtime.md).

---

## `DomainValidatorRegistry`

```csharp
public DomainValidateResult Validate(CombatDomain domain, in EngageContext ctx);
```

- **Stable ordering.** The constructor sorts the supplied validators `OrderBy((int)v.Domain)`, so
  iteration is always in `CombatDomain` ordinal order (`Air → Surface → Subsurface → Land → Mine →
  Facility`) regardless of registration order — determinism does not depend on how the list was
  built.
- **First-deny wins.** `Validate` runs every validator whose `Domain` matches the request; the first
  `!Allowed` result short-circuits and is returned, else `Allow`. Validators for other domains are
  skipped, so you can register several validators for one domain and they chain.
- **`MvpStubs`.** The product default registry wires the **six real aspect validators** above (not
  the no-ops). `MvpEngagementResolver` falls back to `DomainValidatorRegistry.MvpStubs` when a
  caller passes no registry.

### `NoOp*` stubs

[`NoOpDomainValidators`](../../src/ProjectAegis.Sim/Engage/NoOpDomainValidators.cs) provides
allow-all `NoOpAirDomainValidator` / `NoOpSurfaceDomainValidator` for tests or scenarios that want a
domain explicitly ungated. They are **not** part of `MvpStubs`.

---

## Resolver wiring

`MvpEngagementResolver` takes both a flag and a registry:

```csharp
new MvpEngagementResolver(
    …,
    combatDomainsEnabled: false,                 // default OFF
    domainValidators: null);                      // null → DomainValidatorRegistry.MvpStubs
```

- **Step 5 (aspect layer).** Only when `combatDomainsEnabled` is true does the resolver call
  `_domainValidators.Validate(ctx.CombatDomain, ctx)`; a deny aborts the engagement with the mapped
  `*AspectBlock` reason. With the flag off, the ADR-009 aspect gates are skipped entirely.
- **Step 12 (legacy layer).** `CombatDomainValidator.Validate(ctx.CombatDomain, ctx)` runs
  unconditionally:
  - `!MountOnline` → `MountOffline`;
  - `Subsurface` without a fire-control track **or** an unidentified contact → `DomainNoSolution`;
  - `Land` beyond **half** the envelope max range (`RangeMeters > Envelope.MaxRangeMeters * 0.5`) →
    `OutOfEnvelope`;
  - `Mine` and `Facility` have **no** legacy-specific gate (Mine is intentionally deferred to the
    ADR-009 `MineAspectDomainValidator`; a prior bug where the legacy arm denied *every* Mine
    engagement with `DomainNoSolution` — making Mine the only domain that could never launch — was
    fixed).

`combatDomainsEnabled` and the `combatDomain` per-scenario field are authored in
[`scenario-policy-authoring.md`](scenario-policy-authoring.md).

---

## Determinism & invariants

- **Pure.** Every `Validate` is a total function of `EngageContext` (range, envelope, aspect flags) —
  no RNG, no clock, no I/O, no mutation.
- **Order-stable.** The registry sorts by `(int)CombatDomain`, so multi-validator iteration is
  reproducible independent of registration order.
- **Opt-in aspect layer / always-on legacy layer.** The ADR-009 aspect gates only run under
  `combatDomainsEnabled`; the req-18 legacy gate always runs. This keeps the default v2 replay
  goldens (which do not enable combat domains) on the pinned hash `17144800277401907079`;
  `combatDomainsEnabled` fixtures carry their own goldens.
- **Extend-safe reasons.** Adding a domain reuses the existing `FireAbortReason` /
  `EngagementAbortReason` codes — never renumber the enum (see
  [`abort-reason-catalog.md`](abort-reason-catalog.md)).

---

## Extend it

| You want to… | Do this |
|--------------|---------|
| Add a domain aspect gate | Implement `IDomainValidator` for the `CombatDomain`, register it in `DomainValidatorRegistry` (it will be sorted into ordinal position); reuse the domain's `*AspectBlock` reason. Runs only under `combatDomainsEnabled`. |
| Chain a second check for one domain | Register another `IDomainValidator` for the same `CombatDomain`; the registry runs both, first-deny wins. |
| Ungate a domain in a test/scenario | Register the matching `NoOp*` validator (or a custom allow-all) instead of the aspect validator. |
| Change the legacy gate | Edit the `CombatDomainValidator.Validate` switch — but prefer adding an ADR-009 `IDomainValidator` over growing the legacy static. |
| Add a new abort reason | Append to `FireAbortReason` / `EngagementAbortReason` and map it in the resolver; never renumber. |

---

## See also

| Doc | For |
|-----|-----|
| [engagement-pipeline.md](engagement-pipeline.md) | The full ordered kill-chain these validators sit in (steps 5 & 12). |
| [abort-reason-catalog.md](abort-reason-catalog.md) | The stable `EngagementAbortReason` codes the deny results map to. |
| [scenario-policy-authoring.md](scenario-policy-authoring.md) | Authoring `combatDomainsEnabled` / `combatDomain`. |
| [catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md) | The separate damage-time mine *transit* hazard applier. |
| [determinism-and-replay.md](determinism-and-replay.md) | Why the engage path must stay pure. |

## Tests

`src/ProjectAegis.Sim.Tests/Engage/` (xUnit), part of the solution baseline:

| Test | Pins |
|------|------|
| `DomainValidatorRegistryTests` | Ordinal domain ordering; first-deny short-circuit; per-domain allow/deny (Air/Surface/Subsurface/Land/Mine/Facility aspect flags); `MvpStubs` wiring; Mine as a real (non-stub) aspect gate. |
| `CombatDomainValidatorTests` | Legacy gate: `MountOffline`; subsurface `DomainNoSolution` without track/identity; land half-range `OutOfEnvelope`; Mine/Facility no legacy block. |
