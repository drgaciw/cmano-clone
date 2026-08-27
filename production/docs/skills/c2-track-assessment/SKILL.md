---
name: c2-track-assessment
description: Use when an authorized C2 model must assess a contact or track from projections (lifecycle, freshness, organic vs shared, fire-control availability) without issuing weapons-release.
---

# Track assessment (`c2.track.assess`)

**Contract:** `production/docs/skills/agent-c2-skill-contract/CONTRACT.md`  
**Catalog:** `c2.track.assess`  
**Lanes:** `read`, `propose`  
**Submit:** no. Host uses `c2.skill.submit` after approval.

Classify a contact from the C2 picture. Say what the track is, how fresh it is, who owns the sensor, and whether fire-control is actually satisfied. Do not treat this output as a fire order.

## Phase 1: Read

Inputs: `contactId` and/or `targetId`, optional `observerId`.

Read, in order:

1. `ContactPictureProjection` / `ContactPictureEntry` for `ContactId`, `TargetId`, `ObserverId`, `LifecycleState`, `LastSimTick`, `LastSimTime`.
2. `SensorC2Projection.Build` for `HasFireControlTrackOnPrimaryContact`, `PrimaryHostileTargetId`, `ObserverRadarEmconActive`, `ActiveEngagementCount`.
3. `ContactDetailProjection` when a selected-contact panel row exists.
4. `EngageExplainProjection` when an `EngagePreview` is already in hand. Do not invent a preview.
5. `MessageLogProjection` rows for this contact (`CONTACT`, `POLICY_DENIAL`, `ENGAGE_ABORT`) keyed by `sequenceId`.

Set `authorityBasis.trackSource`:

- `organic` when the observer that holds fire-control is the shooter (or the same unit's organic sensor).
- `datalinkShared` when the contact arrived through side-picture / datalink only (ADR-018).
- `fusedWithoutOrganicFc` when both exist but `HasFireControlTrackOnPrimaryContact` is false.
- `unknown` when the projection does not say.

`output` on read:

```json
{
  "contactId": "c-12",
  "targetId": "t-4",
  "lifecycleState": "TRACKING",
  "ageTicks": 2,
  "trackSource": "organic",
  "fireControlSatisfied": true,
  "emconActive": true,
  "spoofOrAbortHint": null
}
```

`replayProvenance.submitted` is false. Do not append.

## Phase 2: Propose

Allowed `commandId` values: `set_sensors`, `set_emcon`, `hold`. Not `engage`.

Track assessment may recommend "go active" or "hold emitters." Weapons pairing is `c2.pairing.recommend`.

`requiredApproval`: `operator`.  
`engagementAuthorizationImplied`: false.  
`playerOverride.commandId`: `hold` (or `set_emcon` if the proposal itself raised EMCON).

Evidence must include `contactId` plus one of `sequenceId` or `unitId`.

## Verdicts

| Keyword | Meaning |
| --- | --- |
| PASS | Envelope valid, projections named, no fire implied |
| FAIL | Missing evidence, shared track treated as FC, or `engage` proposed |
| BLOCKED | Required projection or snapshot field absent |

## Next step

Need a shooter? Hand off to `c2.pairing.recommend` with this assessment as input. Need plain language? `c2.explain`.
