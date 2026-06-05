# Story 001 — AgentDecision typed payload

> **Status:** Complete  
> **Last Updated:** 2026-06-02

## Acceptance

- [x] `AgentDecisionPayload` record (simTick + agent fields per GDD)
- [x] `OrderLogEntry.FromDecisionRecord` uses typed payload
- [x] `DecisionLog` stores payloads; legacy `DecisionRecord` payload still accepted
- [x] Fingerprint unchanged (same canonical fields)
- [x] Unit tests: `AgentDecisionPayloadTests`, updated contract tests