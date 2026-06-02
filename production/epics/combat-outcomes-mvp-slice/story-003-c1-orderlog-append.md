# Story 003 — C1 IOrderLog.Append + FromDecisionRecord

> **Epic:** combat-outcomes-mvp-slice (cross-cutting order-log-replay)  
> **Status:** Complete

## Acceptance

- [x] `OrderLogEntry.FromDecisionRecord(record, simTick)`
- [x] `IOrderLog.Append(OrderLogEntry)` on `DecisionLog`
- [x] `AgentController` appends via factory (not raw `DecisionRecord` list)