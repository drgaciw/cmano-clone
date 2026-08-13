# Speculative / near-future technology engage gate — TL & black-project

> **Scope.** This page documents the **speculative-technology engage gate** (req 10): the deterministic
> pre-resolve check that decides whether a *near-future / speculative* weapon is allowed to fire in the
> current scenario. It covers the pure
> [`SpeculativeEngageGate`](../../src/ProjectAegis.Sim/Scenario/SpeculativeEngageGate.cs), its scenario
> knobs [`ScenarioSpeculativeSettings`](../../src/ProjectAegis.Sim/Scenario/ScenarioSpeculativeSettings.cs)
> (loaded from the policy-JSON `speculative` block), the per-weapon inputs on
> [`EngageContext`](../../src/ProjectAegis.Sim/Engage/EngageContext.cs) (primed from the `engage` block via
> [`ScenarioEngageDefaults`](../../src/ProjectAegis.Sim/Scenario/ScenarioEngageDefaults.cs)), the
> metadata-only [`SpeculativePlatformCatalog`](../../src/ProjectAegis.Sim/Scenario/SpeculativePlatformCatalog.cs)
> (`data/catalog/speculative_platforms.json`) and its
> [`TechnologyMaturityTag`](../../src/ProjectAegis.Sim/Doctrine/TechnologyMaturityTag.cs), and the two
> abort reasons it can raise.
>
> This is the **first content gate** in the [engagement pipeline](engagement-pipeline.md) — it runs
> inside `MvpEngagementResolver.Resolve` right after shooter/target liveness and fire-control-track
> resolution, **before** policy/ROE, domain validators, readiness, magazine, and envelope. It is a pure
> integer/boolean comparison: **no RNG, no clock, deterministic and replay-safe**.
>
> **Deliberate scope (S54 demotion, tracker 10b).** This is a *lightweight metadata gate*, **not** a
> full directed-energy / orbital / escalation runtime. That demotion is pinned by
> [`SpeculativeHonestyPinsTests`](../../src/ProjectAegis.Sim.Tests/Scenario/SpeculativeHonestyPinsTests.cs),
> which asserts no `OrbitalDewPlatform` / `KesslerRiskMeter` / `EscalationTier` types exist in any
> `ProjectAegis.*` assembly.

Project Aegis is a *near-future* milsim: scenarios can field speculative kit (TL-5 directed-energy,
"black-project" weapons) but the **default campaign** must not let a unit fire tech it shouldn't have.
The gate makes that a data-driven, per-scenario decision instead of a hard-coded rule.

---

## The two checks

[`SpeculativeEngageGate.Evaluate(settings, in context)`](../../src/ProjectAegis.Sim/Scenario/SpeculativeEngageGate.cs)
returns `null` (allow) or an `EngagementAbortReason` (deny). The order is fixed — **TL first, then
black-project**:

| # | Condition | Abort reason | Log code |
|---|-----------|--------------|----------|
| 1 | `context.WeaponTechnologyLevel > settings.MaxTechnologyLevel` | `TechnologyLevelExceeded` | `ENGAGE_ABORT` / `TECHNOLOGY_LEVEL_EXCEEDED` |
| 2 | `context.WeaponRequiresBlackProject && !settings.BlackProjectMode` | `BlackProjectRequired` | `ENGAGE_ABORT` / `BLACK_PROJECT_REQUIRED` |

Both abort codes are part of the stable machine-readable
[abort-reason catalog](abort-reason-catalog.md) (`EngagementAbortReasonCodes.ToLogCode`), so a denied
speculative shot lands as a normal `ENGAGE_ABORT` row in the order log.

The gate reads **two independent axes**:

- a **ceiling** the scenario grants — `MaxTechnologyLevel` and `BlackProjectMode` (from the scenario), and
- the **weapon's own demands** — `WeaponTechnologyLevel` and `WeaponRequiresBlackProject` (from the
  firing context).

A TL-5 black-project weapon therefore needs **both** a high enough `MaxTechnologyLevel` **and**
`BlackProjectMode = true` to launch (pinned by
[`ScenarioSpeculativeGateTests`](../../src/ProjectAegis.Sim.Tests/Scenario/ScenarioSpeculativeGateTests.cs)
and `SpeculativeHonestyPinsTests`).

---

## Where it sits in the engage chain

```text
MvpEngagementResolver.Resolve(request)
  shooter alive?  ────────────────► ShooterDestroyed
  target alive?   ────────────────► TargetDestroyed
  fire-control track?  ───────────► NoFireControlTrack
  ▼
  SpeculativeEngageGate.Evaluate(_speculative, ctx)   ← THIS PAGE (TL / black-project)
      → TechnologyLevelExceeded | BlackProjectRequired
  ▼
  policy / ROE  →  domain validators  →  AirNotReady  →  EMCON  →  track
  →  magazine  →  envelope  →  DLZ  →  consume  →  launch     (see engagement-pipeline.md)
```

The gate is intentionally **early**: a unit must never burn a magazine round or trip downstream state
for a weapon it is categorically not allowed to fire. The full ordered chain and its other abort
reasons are documented in [`engagement-pipeline.md`](engagement-pipeline.md); this page is the deep-dive
on just the speculative slice.

---

## Scenario knobs — `ScenarioSpeculativeSettings`

The scenario side of the gate ([`ScenarioSpeculativeSettings`](../../src/ProjectAegis.Sim/Scenario/ScenarioSpeculativeSettings.cs)):

| Property | Default | Notes |
|----------|---------|-------|
| `BlackProjectMode` | `false` | When `false`, any weapon with `WeaponRequiresBlackProject` is denied. |
| `MaxTechnologyLevel` | `2` | Clamped to `[0,5]`. Weapons above this TL are denied. |

`ScenarioSpeculativeSettings.CampaignDefault` is `(BlackProjectMode: false, MaxTechnologyLevel: 2)` — the
conservative production baseline (no black-project kit, TL capped at Production-era).

### Loading from policy JSON

Settings come from the `speculative` block of a
[`data/scenarios/*.policy.json`](scenario-policy-authoring.md) file, parsed by
[`ScenarioPolicyJsonLoader`](../../src/ProjectAegis.Sim/Scenario/ScenarioPolicyJsonLoader.cs) into
[`ScenarioPolicyProfile.Speculative`](../../src/ProjectAegis.Sim/Scenario/ScenarioPolicyProfile.cs). A
missing block falls back to `CampaignDefault`; individual fields fall back per-field:

```jsonc
// data/scenarios/baltic-patrol-black-project.policy.json  (grants speculative fire)
{
  "id": "baltic-patrol-black-project",
  "speculative": { "blackProjectMode": true, "maxTechnologyLevel": 5 },
  "engage": {
    "rangeMeters": 45000, "envelopeMinMeters": 5000, "envelopeMaxMeters": 120000,
    "defaultMagazineRounds": 4, "hasFireControlTrack": true,
    "weaponTechnologyLevel": 5, "weaponRequiresBlackProject": true
  }
}
```

The sibling `baltic-patrol-speculative-gated.policy.json` uses the **same** TL-5 black-project `engage`
weapon but leaves `speculative` at `{ blackProjectMode: false, maxTechnologyLevel: 2 }` — so the identical
weapon that *launches* under the black-project profile is *denied* (`TechnologyLevelExceeded`) under the
gated profile. That pair is the canonical demonstration of the gate.

### Wiring into the resolver

[`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs) pulls the
active profile's settings into the resolver:

```csharp
var speculative = orchestrator.ScenarioPolicy?.Speculative
    ?? ScenarioSpeculativeSettings.CampaignDefault;
var resolver = new MvpEngagementResolver(world, magazines, /* … */, speculative: speculative);
```

`MvpEngagementResolver` also defaults its `speculative` constructor argument to `CampaignDefault`, so the
gate is **always active** even for a bare resolver — the safe default is "deny speculative fire".

---

## Weapon inputs — `EngageContext` (via `ScenarioEngageDefaults`)

The weapon's demands live on the firing context:

- [`EngageContext.WeaponTechnologyLevel`](../../src/ProjectAegis.Sim/Engage/EngageContext.cs) (default `0`)
- `EngageContext.WeaponRequiresBlackProject` (default `false`)

In the scenario-driven path these are primed from the policy `engage` block through
[`ScenarioEngageDefaults`](../../src/ProjectAegis.Sim/Scenario/ScenarioEngageDefaults.cs):
`weaponTechnologyLevel` is clamped to `[0,5]`, `weaponRequiresBlackProject` is passed through, and
`ToEngageContext(...)` copies both onto every `EngageContext` the resolver sees. `ScenarioEngageDefaults.MvpFallback`
leaves both at their conservative defaults (`0` / `false`) — i.e. a non-speculative weapon that the gate
always passes.

---

## Metadata catalog — `SpeculativePlatformCatalog`

[`SpeculativePlatformCatalog`](../../src/ProjectAegis.Sim/Scenario/SpeculativePlatformCatalog.cs) loads
[`data/catalog/speculative_platforms.json`](../../data/catalog/speculative_platforms.json) into a
case-insensitive `platformId → SpeculativePlatformEntry` lookup (`TryGet`). It is **metadata only** — a
place to record a platform's `GameTechnologyLevel`, `RequiresBlackProject`, and
[`TechnologyMaturityTag`](../../src/ProjectAegis.Sim/Doctrine/TechnologyMaturityTag.cs)
(`Simulated` / `Prototype` / `Production`) — and does **not** itself run any weapon behavior:

```jsonc
{
  "version": 1,
  "platforms": [
    { "platformId": "npx-laser-orbital", "gameTechnologyLevel": 5,
      "requiresBlackProject": true,  "technologyMaturity": "Prototype" },
    { "platformId": "orbital-dew-demo", "gameTechnologyLevel": 4,
      "requiresBlackProject": false, "technologyMaturity": "Simulated" }
  ]
}
```

Parsing is tolerant (`PropertyNameCaseInsensitive`; unknown/blank `technologyMaturity` ⇒ `Simulated`).
The `orbital-dew-demo` / `npx-laser-orbital` rows exist **purely to feed gate tests** — the honesty pins
explicitly forbid a real directed-energy/orbital runtime (see scope note above).

---

## Determinism & invariants

| Invariant | Where |
|-----------|-------|
| Pure comparison — no RNG, no clock, no I/O in `Evaluate` | `SpeculativeEngageGate.Evaluate` |
| Gate is always active; unset scenario ⇒ `CampaignDefault` (deny speculative) | `MvpEngagementResolver` + `SimulationSession` defaults |
| Runs **before** magazine consumption / downstream state | gate position in `Resolve` (above) |
| `MaxTechnologyLevel` / `WeaponTechnologyLevel` clamped to `[0,5]` | `ScenarioSpeculativeSettings` / `ScenarioEngageDefaults` |
| Deterministic abort → stable `ENGAGE_ABORT` log code | `EngagementAbortReasonCodes.ToLogCode` |
| No full DEW / Kessler / escalation runtime types | `SpeculativeHonestyPinsTests` (forbidden-type reflection scan) |

Because the gate is deterministic in `(settings, context)`, it participates cleanly in replay: the same
seeded scenario always produces the same allow/deny outcome and the same order-log rows. It does **not**
introduce any new RNG draw (contrast the [engagement outcome fold](engagement-pipeline.md), which does).

---

## Tests

Co-located under [`src/ProjectAegis.Sim.Tests/Scenario/`](../../src/ProjectAegis.Sim.Tests/Scenario/):

| File | Pins |
|------|------|
| `ScenarioSpeculativeGateTests.cs` | black-project policy allows the TL-5 weapon; campaign-default denies it (`TechnologyLevelExceeded`); resolver-level allow/deny; catalog loads the NPX entry |
| `SpeculativeHonestyPinsTests.cs` | `BlackProjectRequired` path (TL OK, black mode off); resolver abort → `BLACK_PROJECT_REQUIRED` log code; catalog metadata-only; **no** forbidden DEW/Kessler/escalation types |
| `ScenarioPolicyJsonLoaderTests.cs` | `speculative` block → `ScenarioPolicyProfile.Speculative` parse + per-field fallback |

Run just this slice:

```bash
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj --filter FullyQualifiedName~Speculative
```

---

## Runbook

**Let a scenario field speculative kit** — in the `*.policy.json`:

1. Raise the ceiling in the `speculative` block (`maxTechnologyLevel`, and `blackProjectMode: true` for
   black-project weapons).
2. Mark the weapon in the `engage` block (`weaponTechnologyLevel`, `weaponRequiresBlackProject`).
3. Both must clear the gate: `weaponTechnologyLevel ≤ maxTechnologyLevel` **and**
   (`!weaponRequiresBlackProject` **or** `blackProjectMode`).

**Register speculative platform metadata** — add a row to
[`data/catalog/speculative_platforms.json`](../../data/catalog/speculative_platforms.json)
(`platformId`, `gameTechnologyLevel`, `requiresBlackProject`, `technologyMaturity`). This is metadata for
tooling/tests; it does not by itself change engage behavior.

**Extend the gate** — add a new check to `SpeculativeEngageGate.Evaluate` returning a *new*
`EngagementAbortReason` (register the code first per [abort-reason-catalog.md](abort-reason-catalog.md)).
Keep it pure (no RNG/clock), keep TL/black-project ordering, and add a corresponding scenario knob to
`ScenarioSpeculativeSettings` + its JSON parse. **Do not** grow this into a full weapon runtime — the
honesty pins forbid it. Verify with the `~Speculative` filter, then the full `dotnet test ProjectAegis.sln`.

---

## Related docs

| Doc | Relationship |
|-----|--------------|
| [engagement-pipeline.md](engagement-pipeline.md) | The full ordered engage chain this gate is the first content step of. |
| [abort-reason-catalog.md](abort-reason-catalog.md) | The stable `TECHNOLOGY_LEVEL_EXCEEDED` / `BLACK_PROJECT_REQUIRED` log codes. |
| [scenario-policy-authoring.md](scenario-policy-authoring.md) | Authoring the `speculative` + `engage` blocks that drive the gate. |
| [autonomy-roe-gating.md](autonomy-roe-gating.md) | The ROE/autonomy gate that runs *after* this one in the chain. |
| [mission-editor-cli.md](mission-editor-cli.md) | CLI to validate/simulate scenarios that exercise the gate. |
