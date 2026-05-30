# ADR-005: DOTS/ECS for World State

**Status:** Accepted  
**Date:** 2026-05-29

## Context

Doc 08 targets 5,000–10,000+ entities with Burst/Jobs.

## Decision

**World state** (units, sensors, weapons, contacts) lives in Unity DOTS/ECS. **Delegation** and **policy** remain plain C# assemblies testable without Unity player loop where possible.

## Consequences

- `ProjectAegis.Sim` splits: pure rules vs `Sim.DOTS` bridge (or systems in Unity project).
- Prototype policy evaluator in pure C# first; integrate ECS via snapshot builders.
