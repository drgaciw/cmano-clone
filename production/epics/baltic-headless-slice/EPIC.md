# Epic: Baltic Headless Vertical Slice

> **Status:** Ready for stories  
> **Created:** 2026-06-01  
> **Priority:** MVP  
> **Layer:** Core + Feature (sim/delegation only)

## Goal

Deliver a **plan → fight → replay** loop without C2 UI: run a seeded Baltic-style scenario headless, produce engage outcomes in the order log, and export a stable replay fingerprint.

## Scope (in)

- Scenario JSON engage defaults (`data/scenarios/*.policy.json`)
- `DelegationBridge` + `SimulationSession` engage pipeline (merged DELEG-5 / SIM stacks)
- Stable engagement order-log codes (`EngagementAbortReasonCodes`)
- Headless harness: multi-tick + fingerprint tests (`PlayModeSmokeHarnessTests`, `ReplayOrderLogFingerprintTests`)
- Logistics GDD P0: `MagazineChange` order-log rows (next story batch)

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

1. `dotnet test ProjectAegis.sln` green on `main` after stack merge.
2. Fixed seed + scenario id → identical `DecisionLog.ComputeFingerprint()` across two runs.
3. At least one scenario produces `Launched` and one produces stable abort code in engagement log.
4. Documented CLI or test entry point for “run N ticks, print fingerprint” (story).

## Stories (create via `/create-stories baltic-headless-slice`)

| Slug | Summary |
|------|---------|
| `replay-harness-cli` | N-tick headless runner prints SEED + fingerprint |
| `magazine-change-order-log` | `MagazineChange` rows per logistics GDD AC-2 |
| `minimal-contact-observed-state` | One contact → `ObservedState` for engage |

## Engine risk

Low — plain .NET 8; Unity adapter is thin facade.

## Dependencies

Merge order: **#13 → #14 → #15 → #16** (see `docs/engineering/graphite-stack-backlog-2026-06.md`).