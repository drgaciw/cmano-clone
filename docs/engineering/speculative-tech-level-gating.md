# Speculative / near-future weapon gating — developer guide

> **Scope (req 10, honest):** Project Aegis supports *speculative / near-future* weapons only through a
> **thin, deterministic engage-time gate** — a technology-level (TL) cap plus a "black-project mode"
> switch. The full req-10 vision (directed-energy weapon runtime, Kessler-syndrome risk, escalation
> tiers) was **demoted (S54, Wave 3)** and is deliberately *not* implemented. This page documents what
> actually ships so nobody wires against a runtime that isn't there. Full requirement:
> [`Game-Requirements/requirements/10-Speculative-Systems.md`](../../Game-Requirements/requirements/10-Speculative-Systems.md).

The subsystem lives in [`ProjectAegis.Sim/Scenario/`](../../src/ProjectAegis.Sim/Scenario/) (+ one enum in
`Sim/Doctrine/`) and is the **speculative abort** in the engage chain documented by
[engagement-pipeline.md](engagement-pipeline.md): `MvpEngagementResolver.Resolve` runs shooter/target
liveness first, then `_world.TryGetContext` (missing context → `NoFireControlTrack`), **then**
`SpeculativeEngageGate.Evaluate`, then policy/ROE. A context whose `HasFireControlTrack` is
**false** still runs the speculative gate — an over-cap weapon returns `TechnologyLevelExceeded`,
not `NoFireControlTrack`. Usable fire-control (`CecRemoteEngageGate.HasUsableFireControl`) is
checked **later**. The speculative gate can only ever **abort** — it never mutates state or consumes
rounds.

| Piece | Role |
|-------|------|
| [`ScenarioSpeculativeSettings`](../../src/ProjectAegis.Sim/Scenario/ScenarioSpeculativeSettings.cs) | Per-scenario knobs: `BlackProjectMode` (bool) + `MaxTechnologyLevel` (0–5). The authorization envelope. |
| [`SpeculativeEngageGate`](../../src/ProjectAegis.Sim/Scenario/SpeculativeEngageGate.cs) | Pure static `Evaluate(settings, in EngageContext)` → nullable `EngagementAbortReason`. Two checks, no RNG. |
| [`SpeculativePlatformCatalog`](../../src/ProjectAegis.Sim/Scenario/SpeculativePlatformCatalog.cs) + `SpeculativePlatformEntry` | Loads `data/catalog/speculative_platforms.json` (TL / black-project / maturity **metadata**). **Advisory-only — not wired into the engage path** (see boundaries). |
| [`TechnologyMaturityTag`](../../src/ProjectAegis.Sim/Doctrine/TechnologyMaturityTag.cs) | `Simulated(0)` / `Prototype(1)` / `Production(2)` maturity label on catalog entries. |

---

## The gate

[`SpeculativeEngageGate.Evaluate`](../../src/ProjectAegis.Sim/Scenario/SpeculativeEngageGate.cs) is the
whole runtime — two ordered comparisons against the per-shooter
[`EngageContext`](../../src/ProjectAegis.Sim/Engage/EngageContext.cs):

| Order | Check | Abort reason | Log code |
|-------|-------|--------------|----------|
| 1 | `context.WeaponTechnologyLevel > settings.MaxTechnologyLevel` | `TechnologyLevelExceeded` (`= 11`) | `TECHNOLOGY_LEVEL_EXCEEDED` |
| 2 | `context.WeaponRequiresBlackProject && !settings.BlackProjectMode` | `BlackProjectRequired` (`= 10`) | `BLACK_PROJECT_REQUIRED` |

If neither trips it returns `null` and the engage proceeds to the policy/ROE stage. TL is checked
first, so a weapon that is both over the cap **and** black-project-only reports
`TechnologyLevelExceeded`. The enum values live in
[`EngagementAbortReason`](../../src/ProjectAegis.Sim/Engage/EngagementAbortReason.cs) and the stable
string log codes in the generated [`AbortReasonCatalog`](../../src/ProjectAegis.Sim/Glossary/AbortReasonCatalog.Generated.cs)
(see [abort-reason-catalog.md](abort-reason-catalog.md)).

In [`MvpEngagementResolver.Resolve`](../../src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs) the call is:

```csharp
var speculativeAbort = SpeculativeEngageGate.Evaluate(_speculative, in ctx);
if (speculativeAbort != null)
    return EngageResult.Aborted(speculativeAbort.Value);
```

placed immediately after `TryGetContext` and ahead of the ROE/domain/readiness/magazine/envelope/DLZ
chain. A **dead shooter/target** or a **missing context** still short-circuits first. A present
context with `HasFireControlTrack == false` does **not** skip speculative gating — the later
`HasUsableFireControl` check is what emits `NoFireControlTrack` / `CecRemoteTrackUnavailable` when
the TL/black-project checks pass. No rounds are consumed on a speculative abort.

## Where the context values come from

The gate reads `EngageContext.WeaponTechnologyLevel` and `.WeaponRequiresBlackProject`. Today those are
**scenario-declared defaults**, not catalog-derived:
[`ScenarioEngageDefaults`](../../src/ProjectAegis.Sim/Scenario/ScenarioEngageDefaults.cs) carries them
(`WeaponTechnologyLevel` clamped to `0–5`, `WeaponRequiresBlackProject`) and stamps them onto every
`EngageContext` it builds in `ToEngageContext`. They are parsed from the scenario policy JSON `engage`
block (`weaponTechnologyLevel` / `weaponRequiresBlackProject`, defaulting to `0` / `false`).

So a scenario models a "speculative weapon" by declaring a high `engage.weaponTechnologyLevel` (and/or
`weaponRequiresBlackProject: true`) and then either raising `speculative.maxTechnologyLevel` /
enabling `speculative.blackProjectMode` to *allow* it, or leaving the defaults to *deny* it.

## Authoring — scenario policy JSON

Both blocks live in `data/scenarios/*.policy.json` (see [scenario-policy-authoring.md](scenario-policy-authoring.md)):

```jsonc
{
  "engage": {
    "weaponTechnologyLevel": 5,        // 0–5, clamped; default 0
    "weaponRequiresBlackProject": true // default false
  },
  "speculative": {
    "blackProjectMode": true,          // default false
    "maxTechnologyLevel": 5            // 0–5, clamped; default 2
  }
}
```

- **`speculative` omitted** → `ScenarioSpeculativeSettings.CampaignDefault` = `blackProjectMode: false`,
  `maxTechnologyLevel: 2`. That default **denies** any TL > 2 or black-project weapon, so speculative
  gear is off unless a scenario opts in.
- `MaxTechnologyLevel` is `Math.Clamp(value, 0, 5)` in the constructor; the loader
  (`ScenarioPolicyJsonLoader.ParseSpeculative`) falls back field-by-field to the campaign default.

Reference fixtures:
- [`baltic-patrol-black-project.policy.json`](../../data/scenarios/baltic-patrol-black-project.policy.json)
  — `blackProjectMode: true`, `maxTechnologyLevel: 5`: a TL-5 black-project weapon **passes** the gate.
- [`baltic-patrol-speculative-gated.policy.json`](../../data/scenarios/baltic-patrol-speculative-gated.policy.json)
  — campaign-default cap: the same TL-5 weapon **aborts** with `TechnologyLevelExceeded`.

## The metadata catalog (advisory-only)

[`SpeculativePlatformCatalog.LoadFromFile`](../../src/ProjectAegis.Sim/Scenario/SpeculativePlatformCatalog.cs)
reads [`data/catalog/speculative_platforms.json`](../../data/catalog/speculative_platforms.json) into
case-insensitive `SpeculativePlatformEntry` records `(PlatformId, GameTechnologyLevel,
RequiresBlackProject, TechnologyMaturity)`; an unrecognized `technologyMaturity` string falls back to
`Simulated`. The shipped file is demo metadata only:

| `platformId` | `gameTechnologyLevel` | `requiresBlackProject` | `technologyMaturity` |
|--------------|-----------------------|------------------------|----------------------|
| `npx-laser-orbital` | 5 | `true` | `Prototype` |
| `orbital-dew-demo` | 4 | `false` | `Simulated` |

**Boundary:** this catalog is **not** read by `SpeculativeEngageGate`, `MvpEngagementResolver`, or the
scenario loader — only by tests. The gate's TL / black-project inputs come from `ScenarioEngageDefaults`
(above), not from a catalog lookup. Treat the catalog as a metadata registry for authoring/tooling, and
do **not** assume a `platformId` here automatically drives an engage decision.

## Wiring

[`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs) resolves the
settings once and hands them to the resolver:

```csharp
var speculative = orchestrator.ScenarioPolicy?.Speculative
    ?? ScenarioSpeculativeSettings.CampaignDefault;
```

`ScenarioPolicyProfile.Speculative` is likewise `CampaignDefault` when the profile omits it, and
`MvpEngagementResolver`'s own `speculative` constructor arg defaults to `CampaignDefault` — so the gate
is always present and defaults to the restrictive envelope.

## Determinism & boundaries

- **Pure + replay-safe.** The gate is a `static` two-comparison function with no RNG, clock, or state;
  it only ever returns an abort reason. Adding/omitting it against the campaign default does not move
  the Baltic v2 replay hash.
- **Deny-by-default.** `CampaignDefault` (`TL 2`, black-project off) blocks speculative weapons unless a
  scenario explicitly widens the envelope.
- **No demoted runtime.** [`SpeculativeHonestyPinsTests`](../../src/ProjectAegis.Sim.Tests/Scenario/SpeculativeHonestyPinsTests.cs)
  is an adversarial pin that fails if any loaded `ProjectAegis.*` assembly defines `OrbitalDewPlatform`,
  `KesslerRiskMeter`, or `EscalationTier` — the demoted req-10 runtime types. Do not reintroduce them
  without a requirement/ADR change; extend the *gate*, not a new physics runtime.
- **Metadata ≠ enforcement.** `speculative_platforms.json` is advisory; enforcement is TL/black-project
  values on the `EngageContext` only.

## Pinned by tests

- [`ScenarioSpeculativeGateTests`](../../src/ProjectAegis.Sim.Tests/Scenario/ScenarioSpeculativeGateTests.cs)
  — TL-exceeded vs black-project-enabled fixtures, the resolver deny/allow paths + log codes, and the
  catalog load.
- [`SpeculativeHonestyPinsTests`](../../src/ProjectAegis.Sim.Tests/Scenario/SpeculativeHonestyPinsTests.cs)
  — the demotion pin, the `BlackProjectRequired` (TL-ok / black-mode-off) case, and the resolver log code.
- `ScenarioPolicyJsonLoaderTests` — the `speculative` + `engage` TL fields round-trip from JSON.

## Extending it

- **New abort condition:** add a comparison to `SpeculativeEngageGate.Evaluate` returning a new
  `EngagementAbortReason`; register the reason + `*_ABORT` string via the abort-reason manifest/codegen
  ([abort-reason-catalog.md](abort-reason-catalog.md)) so the log code stays stable.
- **New scenario knob:** add a field to `ScenarioSpeculativeSettings` (keep a clamped/defaulted value),
  extend `ScenarioSpeculativeJsonDto` + `ScenarioPolicyJsonLoader.ParseSpeculative`, and default it in
  `CampaignDefault`.
- **New catalog metadata:** add a row to `data/catalog/speculative_platforms.json`
  (`platformId` / `gameTechnologyLevel` / `requiresBlackProject` / `technologyMaturity`). Remember it is
  metadata only — if you want it to *gate*, feed the values through `ScenarioEngageDefaults`.
- Keep the gate **pure and deny-by-default**; never turn it into a mutation or a physics runtime.
