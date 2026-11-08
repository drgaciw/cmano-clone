# Bug Report

## Summary
**Title**: Scripted `contacts[].appearAtTick` scenario mechanic is silently dead whenever `detection[]` is also present
**ID**: BUG-scenario-contacts-shadowed-by-detection
**Severity**: S2-Major (silent behavior gap — no error, no warning, scenario intent is not exercised; not a crash/data-corruption/CRITICAL-impact issue)
**Priority**: P2
**Status**: Open — quarantined, not fixed (GitNexus MCP tools were unavailable this session; per CLAUDE.md's "never edit a function/class without impact analysis first" this defect's fix surface — `BalticReplayHarness.RunCore` — was not touched)
**Reported**: 2026-07-28
**Reporter**: QA Gauntlet run `gauntlet-20260727-1455`, Tier 4. Found while investigating why `gauntlet-20260727-1455-t4-s3`'s scripted reinforcement-contact + ROE-escalation trigger produced byte-identical results (score -840, kills 0, denials 168) to a completely different scenario (`gauntlet-forge-20260727-1455-t4-c1`) that has no trigger and no contacts mechanic at all — the coincidence turned out to be a real, confirmed code path collision, not chance.

## Classification
- **Category**: Sim/harness code (`src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs`)
- **System**: Gauntlet/scenario batch execution (`BalticReplayHarness.RunCore`)
- **Frequency**: Always, deterministically, for any scenario combining `detection[]` (non-empty) with `contacts[]` (scripted appearances)
- **Regression**: Unknown — `contacts[]` may never have been exercised in combination with `detection[]` in the existing corpus before this run; no evidence found that this combination was previously tested.

## The defect

`BalticReplayHarness.RunCore` (`src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs:139-164`) decides which contact-generation mechanism to use with a single `if`/`else if`:

```csharp
if (detectionTrials.Count > 0 && profile != null)
{
    pdSim = new PdDetectionContactSimulator(
        SimSeed.FromScenario((ulong)seed),
        detectionTrials,
        profile.UnitRadarEmcon,
        profile.Jammers,
        profile.ContactLifecycle,
        catalogReader);
    // ... datalink merger, etc.
}
else if (profile?.ContactSeeds.Count > 0)
{
    scheduleSim = new ScenarioContactSimulator(profile.ContactSeeds, profile.UnitRadarEmcon);
}
```

`ScenarioContactSimulator` (`src/ProjectAegis.Sim/Sensors/ScenarioContactSimulator.cs`) is the component that turns a policy's `contacts[].appearAtTick` entries into scripted mid-run contact appearances (e.g. a reinforcement submarine surfacing at tick 10). It is **only ever constructed in the `else if` branch** — which is unreachable for any policy that also has a non-empty `detection[]` array, since `detectionTrials.Count > 0` will be true and the `if` branch runs instead. There is no error, warning, or log entry when this happens; `profile.ContactSeeds` is silently never consulted.

Since essentially every gauntlet policy in this corpus (all four tiers) uses `detection[]` as its primary sensor-contact mechanism, `contacts[]` is dead on arrival for any of them — the two mechanisms were apparently designed/tested independently and never combined.

## Reproduction

`production/qa/gauntlet/gauntlet-20260727-1455/tier-4/scenario-3.policy.json` (`gauntlet-20260727-1455-t4-s3`) declares:

```json
"contacts": [{
  "observerId": "ssn-774-virginia-blk-i-ii",
  "targetId": "pl-877-kilo-paltus-1992",
  "contactId": "c-s3-reinforce-1",
  "appearAtTick": 10,
  "hasFireControlTrack": false
}],
"mission": { "triggers": [{
  "id": "asw-reinforcement-detect-roe",
  "observerId": "ssn-774-virginia-blk-i-ii",
  "targetClass": "Subsurface",
  "roe": "WeaponsFree",
  "unitIds": ["ssn-774-virginia-blk-i-ii", "ka-27m-helix-a", "s-185-u-35-type-212a"]
}]}
```

Intent: at tick 10, a reinforcement submarine contact should appear, triggering a per-unit ROE escalation from `WeaponsTight` to `WeaponsFree` for the three named BLUE units.

Batch run (seeds 42, 7, 123; 24 ticks — `production/qa/gauntlet/gauntlet-20260727-1455/tier-4/results.csv`): score **-840**, kills **0**, missilesFired **5**, denials **168** — **identical across all three seeds**, and identical to `gauntlet-forge-20260727-1455-t4-c1` (a different 7v7 roster with no `contacts`/`triggers` at all).

Fingerprint inspection (`ComputeFingerprint()` output, `DecisionLog.cs`) confirms:
- `pl-877-kilo-paltus-1992` (the reinforcement target) appears only in the tick-0 `CATALOG_UNIT`/`MAGAZINE_SEED` setup events — **never** in a `ContactChange` entry.
- The contact id `c-s3-reinforce-1` does not appear anywhere in the fingerprint.
- All 8 `PolicyUpdate` events occur at `SimTick=0` (initial binding); none occur later, so the trigger's ROE escalation never fires (nothing to escalate — the contact it depends on never appeared).
- `168 = 7 blue units × 24 ticks` — exactly matching the "every blue unit denied every tick, unconditionally" pattern of a scenario with **no** working escalation path at all.

Control check: the scenario's **jammer** (`jammers[].activeFromTick=14`, a `PdDetectionContactSimulator`-consumed mechanic) *did* have an observable effect (a `Detected→Lost` `ContactChange` transition present in the fingerprint) — confirming this is specifically a `contacts[]`/`ScenarioContactSimulator` gap, not a blanket failure of all timed mechanics.

## Why it matters

1. Any gauntlet scenario intending to combine scripted contact appearances with normal detection-based sensing (a natural, common design — "most of the fight is visible, but a reinforcement shows up later") silently gets neither an error nor the feature; it just quietly runs as if `contacts[]` were never written.
2. This makes `contacts[]` effectively **unusable in this corpus as currently authored**, since no scenario found in this run has an empty `detection[]` array.
3. The tier-4 forge candidate wave's `hard-case-replay` recipe (not yet actionable this run, but planned) and any future "seeded random inject" scenario design depending on `contacts[]` will hit the same silent gap.

## Suggested fix (not implemented — quarantined pending GitNexus impact analysis)

`RunCore` should run both mechanisms when both are configured (`ScenarioContactSimulator` for scripted `contacts[]` appearances *and* `PdDetectionContactSimulator` for `detection[]` trials), rather than treating them as mutually exclusive. This will require impact analysis on `RunCore` and both simulator classes before any change — not attempted this session (GitNexus MCP tools were unavailable throughout).

## Immediate mitigation applied this run

`gauntlet-20260727-1455-t4-s3`'s `gauntlet.intent`/`gauntlet.oracle` text has been corrected in place (not deleted) to honestly describe the *actual* confirmed behavior (contact never appears, trigger never fires, scenario behaves identically to a flat WeaponsTight/WeaponsFree scenario with no escalation) rather than the originally-intended-but-nonfunctional mechanic, and cross-references this bug report. Its `gauntlet.expect` envelope (already regenerated from the real batch CSV) is unaffected by this correction — the numeric bounds already reflect true observed behavior, only the prose describing *why* was misleading.

## Related Issues
- None known — first time this combination has been exercised in this corpus, as far as this run's investigation could determine.
