# SWARM-C4 Expend / kamikaze pulse — QA (2026-08-09)

**Issue:** DRG-108 · **Req:** SWARM-19 · **PR:** (this branch)

## Surface
- `src/ProjectAegis.Sim/Swarm/Expend/**`
- `SwarmController.IssueExpend` + `ExpendOrderLog`
- Tests: `SwarmExpendTests`

## Behavior
- Authorized expend spends N drones via integrity timeline reason `expend-pulse`
- Denied when `expendAuthorized=false` (caller maps B7 `ExpendAuthorized` / `ExpendUnauthorized`)
- Clamps to remaining count; irreversible (no auto-regen)
- Link lost blocks via existing `EnsureOrdersAccepted`

## Tests
```
dotnet test --filter FullyQualifiedName~Expend
```
