# Baltic batch replay (headless)

Multi-scenario CSV export for agent-vs-agent research (GDD `agentic-infrastructure.md`).

## Quick run

From repo root:

```powershell
dotnet run --project src/ProjectAegis.Delegation.Demo -- --batch --scenarios baltic-patrol,baltic-patrol-comms --seeds 42,7 --ticks 6 --csv-out batch-scores.csv
```

All JSON policies under `data/scenarios/`:

```powershell
dotnet run --project src/ProjectAegis.Delegation.Demo -- --batch --all-scenarios --seeds 42 --ticks 4 --csv-out all-scenarios.csv
```

## CSV columns

`scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint`

## CI

Covered by `BalticBatchRunnerTests` in `dotnet test ProjectAegis.sln`.