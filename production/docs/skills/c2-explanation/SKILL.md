---
name: c2-explanation
description: Use when an authorized C2 model must produce a human-readable explanation of a track, pairing, policy denial, or abort code from projections and order-log sequence ids, without submitting commands.
---

# Explanation (`c2.explain`)

**Contract:** `production/docs/skills/agent-c2-skill-contract/CONTRACT.md`  
**Catalog:** `c2.explain`  
**Lanes:** `read` only  
**Submit:** never.

Turn already-known evidence into a sentence the operator can check. Cite `sequenceId` / abort codes. Do not add a new fact the projections did not supply.

Every `propose` from the other Slice A skills must include this shape. Calling `c2.explain` alone is for "why is this blocked?" and AAR.

## Phase 1: Read

Inputs: one of `proposalId`, `contactId`, `orderId`, `abortCode`, or `sequenceId`.

Read, as applicable:

1. `EngageExplainProjection` (`StatusLine`, `ReasonCode`, `ReasonPlain`, `IsBlocked`). Reuse `ExplainCode` mappings. Do not paraphrase a known abort into a different cause.
2. `AttentionExplainProjection` when the subject is attention/queue, not fire.
3. `MessageLogProjection` line for the `sequenceId`.
4. `PendingApprovalProjection` row when the subject is a queued order (`RISK: HIGH` / `RISK: LOW`).
5. `DoctrineInheritanceProjection` when the question is ROE/EMCON inheritance.

`output`:

```json
{
  "statusLine": "ENGAGE: BLOCKED — NO_FIRE_CONTROL",
  "reasonCode": "NO_FIRE_CONTROL",
  "reasonPlain": "No fire-control track — acquire or designate a track first.",
  "isBlocked": true,
  "citations": [
    {
      "kind": "orderLog",
      "id": "seq",
      "sequenceId": 184
    }
  ]
}
```

`rationale` may add one operator-facing sentence. It must not contradict `reasonPlain`.

`commandId` is null. `requiredApproval` is `none`. `replayProvenance.submitted` is false.

## Phase 2: Compose into a proposal

When nested under `c2.track.assess`, `c2.datalink.reason`, or `c2.pairing.recommend`, copy this object into `output.explanation`. Missing explanation on a proposal is FAIL for the parent skill.

## Verdicts

| Keyword | Meaning |
| --- | --- |
| PASS | Plain language plus at least one citation |
| FAIL | New causal claim, or abort code rewritten |
| BLOCKED | No projection or sequence id to cite |

## Next step

If the operator accepts a parent proposal, the host runs `c2.skill.submit`. This skill does not.
