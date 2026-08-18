# BUG-gauntlet-emcon-dimension-not-exercised

| Field | Value |
|---|---|
| **Run** | gauntlet-20260728-2000 |
| **Class** | `scenario-data` (with a secondary `oracle` consequence) |
| **Severity** | High — silently voids one of the six complexity-ladder dimensions |
| **Status** | OPEN — filed, not fixed in this run (see *Remediation scoping*) |
| **Tiers affected** | 2, 3, 4, 5 (EMCON row of the ladder matrix) |
| **Owner** | qa-lead / sim-data-specialist |
| **Related** | `BUG-t2-escort-passive-emcon-claim-unimplemented` (exists on branch `07-27-qa_gauntlet_tier_2_...`, not on this branch) |

## Summary

The QA Gauntlet complexity ladder declares an **EMCON** dimension that escalates from
"Unrestricted emissions" (T1) through "Passive-only one side" (T2), "Timed EMCON phases" (T3),
"Dynamic EMCON change on detection" (T4), to "Contested EM" (T5).

**None of it reaches the engine.** Every one of the 24 shipped `gauntlet-*` scenario policies is
missing the engine-bound top-level `emcon` block. The three scenarios that claim an EMCON posture
carry it as `gauntlet.emcon` — a bare string on a DTO that does not declare the field, so
`System.Text.Json` silently discards it.

Consequence: **oracle 5 (EMCON plausibility) is vacuous across tiers 2–5** — it cannot fail,
because no scenario in the ladder ever changes an emissions posture. A tier can go green while
the dimension it claims to test is inert.

## Evidence

**1. The field the scenarios use does not exist on the DTO.**

`src/ProjectAegis.Data/Scenario/Policy/ScenarioPolicyJsonDto.cs:65-75` —
`ScenarioGauntletJsonDto` declares exactly four properties:

```csharp
public sealed class ScenarioGauntletJsonDto
{
    public string? Intent { get; set; }
    public string? Oracle { get; set; }
    public List<string>? CatalogRefs { get; set; }
    public List<ScenarioGauntletUnitJsonDto>? Units { get; set; }
}
```

There is no `Emcon` property. The three scenarios below therefore have their EMCON silently dropped:

| Scenario | Value found at `gauntlet.emcon` |
|---|---|
| `gauntlet-t2-escort-passive` | `"passive-blue-standin"` |
| `gauntlet-t3-emcon-phases`   | `"phased"` |
| `gauntlet-t5-roe-change`     | `"contested"` |

**2. A real, engine-bound binding exists — and no gauntlet scenario uses it.**

`ScenarioPolicyJsonDto.cs:23` binds a **top-level** `emcon` block:

```csharp
public ScenarioEmconJsonDto? Emcon { get; set; }     // → { "units": { "<unitId>": { "radar": "Active"|"Off" } } }
```

Scenarios that use it correctly: `baltic-patrol-emcon-off`, `baltic-v3-*` (7 files).
Scenarios in the gauntlet ladder that use it: **0 of 24**.

**3. Empirical: zero EMCON signal in the entire ladder.**

`EMCON_OFF` fingerprint-token count across tiers 1–5 + extra (22 scenarios × 3 seeds, ×2 runs): **0**.

**4. Positive control proves the token is reachable.**

```
dotnet run --project src/ProjectAegis.Delegation.Demo -- --batch \
  --scenarios baltic-patrol-emcon-off --seeds 42 --ticks 10
→ EMCON_OFF token count = 10
```

**5. Retrofit probe proves the block materially changes behaviour.**

A candidate variant of `gauntlet-t2-escort-passive` with a real block
(`"emcon": {"units": {"k-31-visby-2009": {"radar": "Off"}}}`), 3 seeds, 10 ticks:

| Variant | score | kills | missilesFired | denials |
|---|---|---|---|---|
| shipped (`gauntlet.emcon` stand-in) | 50 | 1 | 4 / 3 / 1 | 10 |
| candidate (real top-level `emcon`)  | **-50** | **0** | **0** | 10 |

Artifacts: `emcon-proof/`, `emcon-candidate/compare.csv`.

## Secondary finding — the sensor-side EMCON gate is unobservable

The candidate above changed behaviour decisively yet still emitted **`EMCON_OFF` = 0**.

Two independent EMCON gates exist and only one is observable:

- **Engage-side** — `EngageContext.RadarEmconActive == false` → `EngagementAbortReason.EmconOff`
  → fingerprint token `EMCON_OFF`. Observable.
- **Sensor-side** — `DeterministicDetectionLoop.RollTick` and `ScenarioContactSimulator.Tick`
  silently `continue` when `RequiresActiveRadar` and the unit is passive. **No log entry at all.**

When the passive unit is the *observer*, it never gains a contact, never reaches the engage path,
and emits no token — the posture is provable only by differential CSV metrics against a control
sibling, never by a fingerprint substring. Any future EMCON oracle must assert on a
control-sibling metric delta, **not** on `EMCON_OFF`, unless the passive unit is itself the shooter.

## Why a naive retrofit is not the fix

Setting the observer's radar to `Off` turns the scenario degenerate: blue goes blind, never fires
(0 kills, 0 missiles), and scores −50. It would fail its own `gauntlet.expect`
(`minKills: 1`, `minScore: 30`). A faithful retrofit needs scenario **redesign** — a shooter that is
itself passive, or a control sibling for differential proof — plus regeneration of every numeric
`expect` bound from a real batch CSV per `tools/qa-gauntlet/README-expect-regen.md`.

Note also that tiers 3–5 claim **timed phases**, **dynamic change on detection**, and **contested EM
with deception emitters**. `ScenarioUnitEmconJsonDto` exposes a single static `Radar` string —
there is no phasing, no trigger, no deception emitter. **Those three ladder cells are not
representable by the current engine at all** and cannot be closed by data alone.

## Remediation scoping (why this run filed rather than fixed)

Not fixed in this run, deliberately:

1. **An approved plan already owns this work.**
   `docs/superpowers/plans/2026-07-27-gauntlet-variability.md` schedules exactly this retrofit
   ("replace every EMCON 'stand-in' in the corpus with the real scenario-level `emcon` block —
   closing `BUG-t2-escort-passive-emcon-claim-unimplemented` at the root") across all three
   scenarios plus 10 new ones. It is **not landed on this branch**. An ad-hoc fix here would
   collide with it.
2. **It would destroy the regression baseline.** Rewriting shipped ladder scenarios and their
   `expect` bounds breaks score-drift comparability with prior runs — which is currently the
   ladder's strongest signal (0 drift across 60 pairs this run).
3. **Tiers 3–5 need engine work, not data work** (see above), so the defect cannot be fully closed
   by the scenario regeneration path the skill prescribes for `scenario-data`.

## Recommended fix

1. Land `docs/superpowers/plans/2026-07-27-gauntlet-variability.md` (Tasks covering the three
   EMCON retrofits), regenerating each `expect` from a real batch CSV.
2. Add a **structural guard** so this class of silent drop cannot recur — either fail
   `Invoke-ScenarioValidate` on unknown keys under `gauntlet.*`, or add
   `dimensionsClaimed` + `verify_dimension_coverage.py` as the plan specifies.
3. For tiers 3–5, either extend `ScenarioUnitEmconJsonDto` (phase/trigger/deception support) or
   **downgrade the ladder matrix wording** to match real engine capability. Do not leave the
   matrix claiming behaviour the engine cannot express.
