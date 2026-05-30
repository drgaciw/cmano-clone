# ADR-003: Unified Order Log Schema

**Status:** Accepted  
**Date:** 2026-05-29

## Context

`DecisionLog` stores `DecisionRecord` for agent AAR. Doc 17 requires engagements, policy denials, contact changes, and player orders in one timeline.

GitNexus: **`DecisionLog` upstream impact LOW**.

## Decision

Evolve to **`IOrderLog`** with discriminated `OrderLogEntry` types and stable `sequenceId`. `DecisionRecord` becomes one variant. Replay hash covers full log.

## Consequences

- Extend `ReplayGoldenTests` golden files.
- Message log is a **projection**, not a second source of truth.
