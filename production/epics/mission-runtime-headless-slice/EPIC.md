# Epic: Mission Runtime Headless Slice

> **Status:** MVP Complete (headless timeline + order log)  
> **Priority:** P0 — completes "plan" leg of plan→fight→replay  
> **Created:** 2026-06-02  
> **Depends on:** [order-log-replay.md](../../design/gdd/order-log-replay.md), [agentic-mission-editor.md](../../design/gdd/agentic-mission-editor.md)  
> **Design blocker:** Requirements review **C4** (mission runtime vs phase-gate only)

## Goal

Headless `MissionRuntime` tick: locked `fire_order`, `EventFired` and `MissionTransition` order-log rows, same-tick semantics as mission editor AC-2.

## Acceptance

1. Scenario JSON references mission timeline; harness emits `MissionTransition` rows.
2. `fire_order` total order documented and tested (deterministic).
3. No wall-clock or unordered dictionary iteration in runtime path.
4. `dotnet test` + `/replay-verify` PASS on seeded Baltic+mission fixture.

## Stories

Run `/design-system mission-runtime` then `/create-stories mission-runtime-headless-slice`.