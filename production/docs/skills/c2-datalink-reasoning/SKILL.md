---
name: c2-datalink-reasoning
description: Use when an authorized C2 model must reason about data-link or comms network health, share lag, or SA-only tracks, without treating a shared contact as weapons-release authority.
---

# Data-link reasoning (`c2.datalink.reason`)

**Contract:** `production/docs/skills/agent-c2-skill-contract/CONTRACT.md`  
**Catalog:** `c2.datalink.reason`  
**Lanes:** `read`, `propose`  
**Submit:** no.

Say whether the network can carry a track, and what that track is worth. ADR-018 is the whole point: a shared contact is SA. It does not satisfy fire-control.

## Phase 1: Read

Inputs: `fromUnitId`, `toUnitId`, optional `linkId` / `contactId`.

Read:

1. `DatalinkPictureProjection` → `DatalinkEdgeEntry` (`FromUnitId`, `ToUnitId`, `LinkType`, `Status` in `Up` | `Degraded` | `Down`).
2. `CommsStateProjection` and, when browsing platforms, `CatalogPlatformCommsProjection` / `PlatformCommsListProjection`.
3. `ContactPictureProjection` for contacts whose `ObserverId` is a peer, not the shooter.

`output` on read:

```json
{
  "edges": [
    {
      "fromUnitId": "ffg-1",
      "toUnitId": "awacs-1",
      "linkType": "Link16",
      "status": "Degraded"
    }
  ],
  "networkHealth": "degraded",
  "saOnly": true,
  "shareLagHint": "catalog-latency or comms-degraded; not a fire-control track"
}
```

If `Status` is `Down` or `Degraded`, say so in `assumptions`. Do not upgrade a degraded edge to `Up` because the model wants a pairing.

`authorityBasis.trackSource` for any contact that arrived over the link is `datalinkShared` unless an organic FC flag is also true on the shooter.

## Phase 2: Propose

Allowed `commandId` values: `set_emcon`, `set_sensors`, `hold`.

Never `engage`. A healthy link is not a weapons team.

`requiredApproval`: `operator`.  
`playerOverride.commandId`: `hold`.  
`rejectLeavesNoMutation`: true.

If the caller wants a shooter, return `BLOCKED` with handoff to `c2.pairing.recommend` and copy the edge evidence. Pairing still cannot set `weaponsRelease` on a shared-only track.

## Verdicts

| Keyword | Meaning |
| --- | --- |
| PASS | Edges projected, SA-only stated, no release implied |
| FAIL | Shared track used as FC, or `engage` proposed |
| BLOCKED | No edge list / comms projection available |

## Next step

Pairing: `c2.pairing.recommend`. Explanation of a denial: `c2.explain`.
