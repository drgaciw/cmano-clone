# ADR-002: IPolicyEvaluator Replaces IRoeFilter

**Status:** Accepted  
**Date:** 2026-05-29

## Context

`IRoeFilter.Evaluate(Order)` + `PassthroughRoeFilter` is wired in `DelegationOrchestrator` and `AutonomyGate`. Doc 13 / GDD policy requires Policy Snapshot, EMCON, WRA, and `FireAbortReason`.

GitNexus: **`IRoeFilter` upstream impact HIGH**.

## Decision

Introduce **`IPolicyEvaluator`** in `ProjectAegis.Sim.Policy` with `PolicySnapshot` input. Keep **`IRoeFilter`** as thin adapter during migration; remove from production orchestrator path once tests pass.

## Consequences

- Update `AutonomyGate` to call evaluator before autonomy branching.
- `ReplayGoldenTests` must include policy denial fingerprints when enabled.
