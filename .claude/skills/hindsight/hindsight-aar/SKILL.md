---
name: hindsight-aar
description: "Use for After Action Review of simulation runs: decision streams, trust signals, kill-chain style analysis. Examples: \"AAR for Baltic run\", \"analyze agent EW decisions\", \"post-scenario review\""
---

# Hindsight AAR (simulation)

Project Aegis emits structured **decision records** via `DecisionLog` / `IOrderLog`. When `HindsightOptions` is enabled, the runtime retains to `aar-*` and `agent-*` banks automatically at finalize.

## Spec alignment

- Decision record fields: `docs/superpowers/specs/2026-05-28-agent-delegation-framework-design.md` §6
- C# hook: `src/ProjectAegis.Delegation/Hindsight/`
- Order log evolution: `docs/architecture/adr-003-order-log-schema.md`

## Banks

| Bank | Population |
|------|------------|
| `agent-{personality}-{agentId}` | Each `AgentDecision` append (live retain) |
| `aar-{scenario}-{runId}` | `FinalizeScenario` session summary |
| `agent-xp-{agentId}` | Trust signals at finalize |

`runId` defaults to order-log fingerprint prefix when not set in `HindsightOptions`.

## Enable runtime retain (local playtest)

```csharp
var orchestrator = new DelegationOrchestrator(
    globalSeed: 42,
    hindsight: new HindsightOptions
    {
        BaseUrl = "http://localhost:8888",
        ScenarioSlug = "baltic",
        RunId = "run-001",
        AarReflectQuery = "Summarize ROE violations and attention overload",
    });
```

## Agent workflow (post-run)

1. Confirm server: `.\tools\hindsight\Test-HindsightServer.ps1`
2. **Recall** per-agent bank: `How did agent handle high-risk engagements under attention overload?`
3. **Reflect** on AAR bank: `Summarize kill-chain gaps and policy denials`
4. Cross-check with **GitNexus**: `gitnexus_query({ query: "delegation decision log" })` for code paths
5. Retain findings to `dev-cmano-clone` if they drive code changes

Spawn **`hindsight-aar-analyst`** for full pass.

## Manual retain (headless log export)

If sim ran without Hindsight, retain a compressed summary:

```powershell
.\tools\hindsight\Invoke-Hindsight.ps1 -Operation retain -BankId aar-baltic-manual-01 -Content @"
mission_succeeded=false decisions=47 policy_denials=3
Agent ew-02: jammed 3 contacts, missed 1 at sim_time=3200 attention overload
"@
```

## Checklist

```
- [ ] Scenario slug and run id documented
- [ ] Per-agent recall for contested decisions
- [ ] AAR reflect query issued on aar-* bank
- [ ] GitNexus used for code-level follow-up (not memory-only conclusions)
- [ ] Determinism: no recall inside Tick() / replay CI
```
