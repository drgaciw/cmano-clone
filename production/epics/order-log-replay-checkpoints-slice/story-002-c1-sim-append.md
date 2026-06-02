# Story 002 — C1 sim append via IOrderLog

> **Epic:** order-log-replay-checkpoints-slice  
> **Status:** Complete

## Acceptance

- [x] `OrderLogEntryFactories` for engagement, contact, mission, policy rows
- [x] `DecisionLog.Append` handles all `OrderLogEntryKind` values
- [x] Baltic harness + `SimulationSession` + `AgentController` use `OrderLog.Append`