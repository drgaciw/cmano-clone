# Headless smoke evidence — 2026-06-02

**Branch:** `main` @ `2a08518` (Sprints 7–9 + QA golden committed)  
**Verifier:** automated `dotnet test` gate

## Commands run

```powershell
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln --filter "FullyQualifiedName~PlayModeSmokeHarnessTests|FullyQualifiedName~ReplayGolden|FullyQualifiedName~BalticBatch|FullyQualifiedName~Comms"
```

## Results

| Gate | Result | Count |
|------|--------|-------|
| Full solution test | PASS | 238 tests, 0 failed |
| PlayMode harness + Replay golden + Batch + Comms filter | PASS | 26 tests, 0 failed |

## Regression coverage added

| Scenario | Test | Golden file |
|----------|------|-------------|
| `baltic-patrol-comms` | `ReplayGoldenBalticCommsTests` | `tests/regression/replay-golden-baltic-comms-2026-06-02.txt` |
| Batch CSV | `BalticBatchRunnerTests` | — |
| C2 selection | `C2SelectionFlowTests` | — |

## Comms scenario spot-check (seed 42, 6 ticks)

- `CommsStateChange` at ticks 2 and 4 (Degraded, Denied)
- `PolicyDenial` with `CommsDenied` after link-down
- `MESSAGE=COMMS` lines in demo output (2 transitions)

## Unity manual (still required)

See `production/qa/c2-manual-signoff-2026-06-02.md` — not substitutable by headless gate.