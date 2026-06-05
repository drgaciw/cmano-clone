# Epic: Baltic Headless Vertical Slice

> **Status:** Complete  
> **Created:** 2026-06-01  
> **Completed:** 2026-06-01  
> **Priority:** MVP  
> **Layer:** Core + Feature (sim/delegation only)

## Goal

Deliver a **plan → fight → replay** loop without C2 UI: run a seeded Baltic-style scenario headless, produce engage outcomes in the order log, and export a stable replay fingerprint.

## Scope (in)

- Scenario JSON engage defaults (`data/scenarios/*.policy.json`)
- `DelegationBridge` + `SimulationSession` engage pipeline (merged DELEG-5 / SIM stacks)
- Stable engagement order-log codes (`EngagementAbortReasonCodes`)
- Headless harness: multi-tick + fingerprint tests (`PlayModeSmokeHarnessTests`, `ReplayOrderLogFingerprintTests`)
- Logistics GDD P0: `MagazineChange` order-log rows

## Scope (out)

- Mission editor UI / MCP authoring
- Full Platform DB import
- C2 globe, message log UI
- `IPolicyEvaluator` inside `MvpEngagementResolver`

## Governing docs

| Doc | TR / section |
|-----|----------------|
| [order-log-replay.md](../../design/gdd/order-log-replay.md) | Engagement + fingerprint |
| [engagement-fire-control.md](../../design/gdd/engagement-fire-control.md) | DLZ / magazine MVP |
| [logistics-magazines.md](../../design/gdd/logistics-magazines.md) | AC-1..AC-5 (magazine rows) |
| [simulation-core-time.md](../../design/gdd/simulation-core-time.md) | World hash (future) |
| [policy-roe-emcon-wra.md](../../design/gdd/policy-roe-emcon-wra.md) | Scenario ROE |

## ADRs

- ADR-001 sim assembly boundary
- ADR-003 order-log schema
- ADR-004 tick pipeline order

## Acceptance (epic-level)

1. [x] `dotnet test ProjectAegis.sln` green on `main` (105 tests, 2026-06-01).
2. [x] Fixed seed + scenario id → identical `DecisionLog.ComputeFingerprint()` across two runs.
3. [x] `Launched` and stable abort codes (`NoFireControlTrack`) in engagement log.
4. [x] CLI: `dotnet run --project src/ProjectAegis.Delegation.Demo -- --seed 42 --scenario baltic-patrol --ticks 4`.

## Stories

| Story | Slug | Status |
|-------|------|--------|
| 001 | `story-001-replay-harness-cli.md` | Complete (#17) |
| 002 | `story-002-magazine-change-order-log.md` | Complete (#18) |
| 003 | `story-003-minimal-contact-observed-state.md` | Complete (#19) |

## Engine risk

Low — plain .NET 8; Unity adapter is thin facade.

## Gate

See `docs/reports/baltic-headless-slice-gate-2026-06-01.md` — **PASS** (headless vertical slice).