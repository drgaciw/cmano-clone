# SWARM-B8 / DRG-100 — Agent delegation for swarm intents

**Date:** 2026-08-09  
**Surface:** `src/ProjectAegis.Delegation/Sim/**`, `src/ProjectAegis.Delegation.Tests/Sim/**`  
**Requirement:** SWARM-23

## ACs

| AC | Evidence |
|----|----------|
| Agent Move/Attack/Hold | issuer tests |
| Agent Mode Assault/Screen | `Agent_issues_Mode_*` |
| Actor attribution | fingerprint includes agent id |
| Link lost → LINK_LOST | `Link_lost_maps_to_failure_reason` |

## Types

- `SwarmAgentIntentIssuer`, `SwarmAgentOrderRequest`, `SwarmAgentOrderResult`, `SwarmAgentOrderLogPayload`, `SwarmOrderActor`
