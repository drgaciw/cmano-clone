# Hindsight integration (Project Aegis)

Optional sidecar memory for agent decisions, AAR, and campaign trust. **Disabled by default** so CI and golden replays stay deterministic.

## Enable

```csharp
var hindsight = new HindsightOptions
{
    BaseUrl = "http://localhost:8888",
    ScenarioSlug = "baltic",
    RunId = "run-001", // optional; defaults to order-log fingerprint prefix
};

var orchestrator = new DelegationOrchestrator(globalSeed: 42, hindsight: hindsight);
```

Requires a running Hindsight server (see [Hindsight docs](https://hindsight.vectorize.io/developer/api/main-methods)).

## Memory banks

| Bank ID pattern | Purpose |
|----------------|---------|
| `agent-{personality}-{agentId}` | Per-agent decision memory (retain on each `AgentDecision` append) |
| `aar-{scenario}-{runId}` | Session summary at `FinalizeScenario` |
| `agent-xp-{agentId}` | Trust signals at `FinalizeScenario` |
| `balance-tuning` | Reserved for balance-tuning agent (tooling; use `HindsightBankIds.BalanceTuning`) |

## Design constraints

- **Retain only** on the simulation hot path (async, fire-and-forget).
- **Recall / reflect** are for tooling and AAR agents, not inside `Tick()` or policy selection.
- Order-log fingerprints and replay hashes are unchanged when Hindsight is off or unreachable.
