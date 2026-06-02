# Epic: World-State Hash Slice

> **Status:** Complete  
> **Created:** 2026-06-02  
> **Depends on:** pd-detection-loop, ew-jam-headless-slice

## Goal

Single **WORLD_HASH** combining sim core tick hash, detection sub-hash, and engage sub-hash for replay gates (ADR-004).

## Acceptance

1. `SimWorldHash.Combine` used by pipeline + harness.
2. CLI prints `WORLD_HASH=` stable across runs.
3. `dotnet test` green.