---
name: c2-sensor-to-shooter-pairing
description: Use when an authorized C2 model must recommend a sensor-to-shooter pair or best-resource option from projections, including organic fire-control checks, without implying engagement authorization.
---

# Sensor-to-shooter pairing (`c2.pairing.recommend`)

**Contract:** `production/docs/skills/agent-c2-skill-contract/CONTRACT.md`  
**Catalog:** `c2.pairing.recommend`  
**Lanes:** `read`, `propose`  
**Submit:** no. `engage` on a proposal still needs `weaponsRelease` approval, then `c2.skill.submit`.

Name a shooter, a sensor, and a contact. Rank options. Stop before fire.

This is the skill that will be abused. Time pressure plus a clean pair looks like a launch. The envelope's `engagementAuthorizationImplied` stays false. The player (or a later submit after approval) is the one who issues `engage`.

## Phase 1: Read

Inputs: `contactId` / `targetId`, optional candidate `shooterUnitIds`.

Read:

1. `c2.track.assess` output if present; otherwise the same projections that skill uses.
2. `c2.datalink.reason` when the sensor is not the shooter.
3. `EngagePreviewProjection` / `EngageExplainProjection` for abort codes (`NO_FIRE_CONTROL`, `EMCON`, `DLZ`, `ROE`, `NO_AMMO`, …).
4. `UnitDetailProjection`, `MagazineLoadoutProjection` for magazine and mount facts.
5. `SensorC2Projection` for `HasFireControlTrackOnPrimaryContact`.

Rank candidates deterministically: organic FC first, then magazine remaining, then range/DLZ clear, then unit id ordinal. No hash-set order.

`output` on read:

```json
{
  "contactId": "c-12",
  "pairs": [
    {
      "shooterUnitId": "ffg-1",
      "sensorUnitId": "ffg-1",
      "trackSource": "organic",
      "fireControlSatisfied": true,
      "abortPreviewCode": null,
      "rank": 1
    }
  ],
  "bestResource": "ffg-1",
  "releaseEligible": false
}
```

`releaseEligible` is a host-side hint that *if* someone later submits `engage`, organic FC is present. It is not authorization.

Drop pairs whose only track is `datalinkShared` or `fusedWithoutOrganicFc` from any `engage` recommendation. They may appear in the list with `fireControlSatisfied: false` so the operator sees why they lost.

## Phase 2: Propose

Allowed `commandId` values: `engage`, `set_sensors`, `hold`.

Rules:

- `commandId: "engage"` requires `trackSource: "organic"` and `fireControlSatisfied: true`. Otherwise FAIL.
- `requiredApproval` is `weaponsRelease` when `commandId` is `engage`, else `operator`.
- `engagementAuthorizationImplied` is false.
- `playerOverride.commandId` is `hold`.
- Nested `explanation` from `c2.explain` is required (abort code plain language, or "clear but still needs approval").
- TTL applies. A stale pair is not a launch.

## Phase 3: After approval

The host calls `c2.skill.submit` with this `proposalId`. Submit still runs `IPolicyEvaluator` and refuses `REPLAY_ATTACHED`, `NOT_HUMAN_CONTROL`, and `NO_FIRE_CONTROL`.

## Verdicts

| Keyword | Meaning |
| --- | --- |
| PASS | Ranked pairs, organic FC stated, no implied release |
| FAIL | Shared track proposed as `engage`, or authorization implied |
| BLOCKED | No FC snapshot, no magazine projection, or empty candidate set |

## Next step

Explain the pair or the abort: `c2.explain`. Submit is a host verb, not this skill.
