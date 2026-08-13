# Combat domain validators — the per-domain engage aspect gate & its two layers

> **Scope.** The **combat-domain** engage gates in
> [`src/ProjectAegis.Sim/Engage/`](../../src/ProjectAegis.Sim/Engage/) — the checks that decide
> whether a shot is *geometrically / doctrinally valid for the target's domain* (air, surface,
> subsurface, land, mine, facility) as part of one `MvpEngagementResolver.Resolve(EngageRequest)`
> pass. There are **two distinct layers** and the most important thing to understand is that they
> are **not** the same mechanism:
>
> 1. the **flag-gated ADR-009 `DomainValidatorRegistry`** (`IDomainValidator` + the six
>    `*AspectDomainValidator`s) — off by default (`combatDomainsEnabled = false`), and
> 2. the **always-on legacy req-18 `CombatDomainValidator.Validate`** static switch — runs every
>    engagement regardless of the flag.
>
> This doc is the domain-gate deep-dive that the [engagement pipeline](engagement-pipeline.md)
> guide only references as two entries in its long ordered gate chain. The abort codes produced
> here are cataloged in [abort-reason-catalog.md](abort-reason-catalog.md); the two-layer ROE/domain
> split and the rest of the kill chain live in [engagement-pipeline.md](engagement-pipeline.md).
>
> Boundary rationale: [ADR-009 (combat domain validators & deterministic damage order)](../architecture/adr-009-combat-domain-validators.md).
> Everything here is pure (`in EngageContext` → verdict), deterministic, and — critically — the
> ADR-009 layer is **default-off**, so it cannot move the Baltic v2 replay hash
> (`17144800277401907079`).

---

## Where it lives

| Type | File | Kind | Role |
|------|------|------|------|
| `CombatDomain` | `Sim/Engage/CombatDomain.cs` | `enum` | The six domains: `Air=0, Surface=1, Subsurface=2, Land=3, Mine=4, Facility=5`. `CombatDomainParser.Parse` is case-insensitive, **defaults to `Air`** on an unknown string. |
| `IDomainValidator` | `Sim/Engage/IDomainValidator.cs` | `interface` | `Domain` + `Validate(in EngageContext) → DomainValidateResult`. |
| `DomainValidateResult` | `Sim/Engage/DomainValidateResult.cs` | `readonly struct` | `Allow` or `Deny(FireAbortReason)`; carries the `FireAbortReason?` when denied. |
| `{Air,Surface,Subsurface,Land,Mine,Facility}AspectDomainValidator` | `Sim/Engage/*AspectDomainValidator.cs` | `sealed class` | The six real per-domain aspect gates (ADR-009). |
| `NoOpAirDomainValidator` / `NoOpSurfaceDomainValidator` | `Sim/Engage/NoOpDomainValidators.cs` | `sealed class` | Allow-all MVP stubs (kept for composition / tests). |
| `DomainValidatorRegistry` | `Sim/Engage/DomainValidatorRegistry.cs` | `sealed class` | Holds validators sorted by domain; dispatches to the ones matching a domain; `MvpStubs` default set. |
| `CombatDomainValidator` | `Sim/Engage/CombatDomainValidator.cs` | `static` | The **legacy req-18** switch that always runs (independent of the flag). |
| `MvpEngagementResolver` | `Sim/Engage/MvpEngagementResolver.cs` | `sealed class` | Runs both layers at two different points of one `Resolve` pass. |

---

## Layer 1 — ADR-009 `DomainValidatorRegistry` (flag-gated, default off)

### The seam

```csharp
public interface IDomainValidator
{
    CombatDomain Domain { get; }
    DomainValidateResult Validate(in EngageContext ctx);
}
```

Each of the six aspect validators has the **same shape** — allow when the shot is both in the
weapon envelope **and** in that domain's aspect flag, else deny with the domain-specific abort code:

```csharp
public DomainValidateResult Validate(in EngageContext ctx)
{
    if (ctx.Envelope.Contains(ctx.RangeMeters) && ctx.SurfaceAspectInEnvelope)
        return DomainValidateResult.Allow;
    return DomainValidateResult.Deny(FireAbortReason.SurfaceAspectBlock);
}
```

`EngageContext` carries one boolean per domain (`AirAspectInEnvelope`, `SurfaceAspectInEnvelope`,
`SubsurfaceAspectInEnvelope`, `LandAspectInEnvelope`, `MineAspectInEnvelope`,
`FacilityAspectInEnvelope`), all defaulting to `true`, plus the shared `Envelope` /`RangeMeters`.
Each validator reads **only its own** aspect flag, so the six are fully independent.

| Domain | Validator | Denies with |
|--------|-----------|-------------|
| `Air` | `AirAspectDomainValidator` | `FireAbortReason.AirAspectBlock` |
| `Surface` | `SurfaceAspectDomainValidator` | `FireAbortReason.SurfaceAspectBlock` |
| `Subsurface` | `SubsurfaceAspectDomainValidator` | `FireAbortReason.SubsurfaceAspectBlock` |
| `Land` | `LandAspectDomainValidator` | `FireAbortReason.LandAspectBlock` |
| `Mine` | `MineAspectDomainValidator` | `FireAbortReason.MineAspectBlock` |
| `Facility` | `FacilityAspectDomainValidator` | `FireAbortReason.FacilityAspectBlock` |

### The registry

```csharp
public DomainValidateResult Validate(CombatDomain domain, in EngageContext ctx)
{
    foreach (var validator in _validators)      // sorted by (int)Domain
    {
        if (validator.Domain != domain) continue;
        var result = validator.Validate(in ctx);
        if (!result.Allowed) return result;     // first denial wins
    }
    return DomainValidateResult.Allow;          // no matching validator → allow
}
```

- The constructor sorts validators **ordinal by `(int)CombatDomain`**, so iteration order is stable
  (`Air → Surface → Subsurface → Land → Mine → Facility`) regardless of registration order.
- Only validators whose `Domain` matches the requested domain run; the **first denial short-circuits**.
- A domain with **no** registered validator is implicitly **allowed** (so a registry can omit domains
  it does not gate — e.g. the "without facility" / "without mine" test registries still launch).
- `DomainValidatorRegistry.MvpStubs` is the default set: all six real aspect validators (despite the
  historical "stub" name, these are the real geometry/aspect gates — `NoOpDomainValidators` are the
  actual allow-all stubs, retained for composition/tests).

### Denial → order-log code

When the registry denies, `MvpEngagementResolver.MapDomainDenial` maps the `FireAbortReason` to an
`EngagementAbortReason` (and thence the stable `*_ASPECT_BLOCK` order-log code via
`EngagementAbortReasonCodes.ToLogCode`; see [abort-reason-catalog.md](abort-reason-catalog.md)):

| `FireAbortReason` | `EngagementAbortReason` | Order-log code |
|-------------------|-------------------------|----------------|
| `AirAspectBlock` | `AirAspectBlock` | `AIR_ASPECT_BLOCK` |
| `SurfaceAspectBlock` | `SurfaceAspectBlock` | `SURFACE_ASPECT_BLOCK` |
| `SubsurfaceAspectBlock` | `SubsurfaceAspectBlock` | `SUBSURFACE_ASPECT_BLOCK` |
| `LandAspectBlock` | `LandAspectBlock` | `LAND_ASPECT_BLOCK` |
| `MineAspectBlock` | `MineAspectBlock` | `MINE_ASPECT_BLOCK` |
| `FacilityAspectBlock` | `FacilityAspectBlock` | `FACILITY_ASPECT_BLOCK` |
| `NoFireControlTrack` / `EmconOff` | (passthrough) | matching code |
| *anything else* | `DomainNoSolution` | `DOMAIN_NO_SOLUTION` |

---

## Layer 2 — legacy `CombatDomainValidator` (always on, req 18)

Separately, `MvpEngagementResolver.Resolve` **always** calls the static
`CombatDomainValidator.Validate` — this predates ADR-009 and is **not** gated by
`combatDomainsEnabled`:

```csharp
public static EngagementAbortReason? Validate(CombatDomain domain, in EngageContext ctx)
{
    if (!ctx.MountOnline) return EngagementAbortReason.MountOffline;
    return domain switch
    {
        CombatDomain.Subsurface when !ctx.HasFireControlTrack => EngagementAbortReason.DomainNoSolution,
        CombatDomain.Subsurface when !ctx.ContactIdentified   => EngagementAbortReason.DomainNoSolution,
        CombatDomain.Land when ctx.RangeMeters > ctx.Envelope.MaxRangeMeters * 0.5 => EngagementAbortReason.OutOfEnvelope,
        _ => null,   // Air / Surface / Mine / Facility: no legacy gate
    };
}
```

- `MountOffline` is checked first, for every domain.
- **Subsurface** additionally requires a fire-control track **and** an identified contact.
- **Land** applies a coarse "half of max range" reach limit.
- **Mine** deliberately has **no** legacy gate (matching Facility). This is a **fix**: the arm
  previously hardcoded `CombatDomain.Mine => DomainNoSolution`, which made Mine the only domain that
  could *never* launch — even when ADR-009's `MineAspectDomainValidator` allowed the shot. The
  authoritative mine gate is now the aspect validator (when the flag is on); the legacy switch stays
  out of the way. (Pinned by `CombatDomainsEnabled_true_mine_aspect_allowed_launches_successfully`.)

---

## How the resolver wires both layers

Within a single `MvpEngagementResolver.Resolve` pass the two layers sit at **different points** of
the ordered gate chain (see [engagement-pipeline.md](engagement-pipeline.md) for the full sequence):

```
… speculative TL → policy / ROE →
  [Layer 1] if (combatDomainsEnabled)  _domainValidators.Validate(ctx.CombatDomain, ctx)   ← ADR-009, early
  → air-ops ready → damage-withdraw → bingo → shotgun → spoof → EMCON → CEC/FC → winchester → magazine-empty →
  [Layer 2] CombatDomainValidator.Validate(ctx.CombatDomain, ctx)                          ← legacy, always
  → hypersonic → envelope → DLZ → consume salvo → launch
```

- The constructor takes `bool combatDomainsEnabled = false` and an optional
  `DomainValidatorRegistry? domainValidators` (defaulting to `MvpStubs`). The scenario policy sets
  the flag via `EngageDefaults.CombatDomainsEnabled` (`ScenarioEngageDefaults.MvpFallback` is
  **false**).
- When the flag is **off**, Layer 1 is skipped entirely — even a deny-all registry has **zero**
  effect (`CombatDomainsEnabled_false_skips_registry_in_resolver`).

---

## Determinism & invariants

- **Flag-off ⇒ zero abort delta.** With `combatDomainsEnabled = false` the registry is never
  consulted, so ADR-009 cannot change any outcome. This is the load-bearing replay-safety property:
  the production Baltic v2 goldens run flag-off and the hash `17144800277401907079` is untouched. The
  flag-**on** path is exercised only by the **isolated** `combat-domains-smoke` scenario policy
  ([`CombatDomainsSmokePolicyTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/CombatDomainsSmokePolicyTests.cs)),
  which is separate from the ReplayGolden 6/6 catalog.
- **Pure & allocation-light.** Validators take `in EngageContext` and return a `readonly struct`
  verdict; no RNG, no wall-clock, no mutation.
- **Stable dispatch order.** The registry sorts ordinal by `(int)CombatDomain`, so iteration is
  deterministic regardless of registration order.
- **Two independent layers.** ADR-009 aspect gating (flag-gated, geometry/aspect) and the legacy
  req-18 switch (always-on, mount/track/reach) are distinct and both must pass; do not conflate them.

---

## Extend it

| You want to… | Do this |
|--------------|---------|
| Add a new domain | Add the value to `CombatDomain` (append — never renumber; the enum ordinal drives registry order and log codes), add its `*AspectInEnvelope` flag to `EngageContext`, write a `*AspectDomainValidator`, add a `FireAbortReason.*AspectBlock` + its `MapDomainDenial` arm + order-log code. |
| Change a domain's gate logic | Edit that one `*AspectDomainValidator.Validate` — the other domains and the registry are untouched. |
| Add a domain to a scenario | Set `combatDomainsEnabled: true` in the scenario's `engage` defaults (see [scenario-policy-authoring.md](scenario-policy-authoring.md)); keep it **out** of the v2 replay goldens. |
| Compose a custom validator set | Construct `new DomainValidatorRegistry([...])` — omit domains you do not gate (they default to allow); the registry re-sorts by domain. |
| Add an always-on doctrinal gate | Extend the legacy `CombatDomainValidator.Validate` switch **only** if the rule must apply even with the flag off (rare); otherwise prefer an ADR-009 validator. |

---

## See also

| Doc | For |
|-----|-----|
| [engagement-pipeline.md](engagement-pipeline.md) | The full ordered engage gate chain these two layers sit inside, and the three-draw outcome fold. |
| [abort-reason-catalog.md](abort-reason-catalog.md) | The stable `ENGAGE_ABORT` codes (`*_ASPECT_BLOCK`, `DOMAIN_NO_SOLUTION`) these denials emit. |
| [scenario-policy-authoring.md](scenario-policy-authoring.md) | The `engage` defaults that flip `combatDomainsEnabled`. |
| [catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md) | The transit-mine hazard runtime (distinct from the Mine **engage** domain gate). |
| [determinism-and-replay.md](determinism-and-replay.md) | Why the flag-off zero-delta property matters for the golden hash. |

## Tests

| Test | Assembly (framework) | Pins |
|------|----------------------|------|
| `DomainValidatorRegistryTests` | `ProjectAegis.Sim.Tests/Engage/` (xUnit) | `MvpStubs` allow all six domains in-envelope; **stable ordinal domain iteration order**; flag-off skips the registry (deny-all still launches); flag-on invokes it (no-op allows); `MvpFallback.CombatDomainsEnabled` defaults `false`; each domain's aspect-block aborts with the matching `*AspectBlock` + `*_ASPECT_BLOCK` log code; **Mine aspect-allowed launches** (never-launch fix); flag-off **zero-abort-delta** across a deny registry and the parametrized per-domain aspect blocks. |
| `CombatDomainValidatorTests` | `ProjectAegis.Sim.Tests/Engage/` (xUnit) | Legacy switch: `MountOffline` first; Subsurface needs identified contact (`DomainNoSolution`); each aspect validator allows in-envelope and denies out-of-aspect / out-of-range-at-boundary with its abort code. |
| `MvpEngagementResolverTests` | `ProjectAegis.Sim.Tests/Engage/` (xUnit) | End-to-end resolver behaviour with both layers in the chain. |
| `CombatDomainsSmokePolicyTests` / `BalticCombatDomainsPolicyTests` | `ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/` (NUnit) | The flag-on `combat-domains-smoke` policy loads `CombatDomainsEnabled = true`, pinned via harness seed 42 / 4 ticks, **isolated** from the Baltic ReplayGolden 6/6. |
