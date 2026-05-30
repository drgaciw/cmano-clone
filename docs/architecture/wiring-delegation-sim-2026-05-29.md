# Delegation ↔ Sim Wiring (ADR-002)

**Date:** 2026-05-29  
**Status:** Implemented (MVP)

## Summary

`ProjectAegis.Delegation` references `ProjectAegis.Sim`. `DelegationOrchestrator` constructs `AutonomyGate` with `RoePolicyAdapter` over `IPolicyEvaluator` (default `PassthroughPolicyEvaluator`).

## Call chain

```
AgentController → AutonomyGate.Evaluate
  → IRoeFilter (RoePolicyAdapter)
    → IPolicyEvaluator (PolicyEvaluator | PassthroughPolicyEvaluator)
  → autonomy branching (unchanged)
```

## MVP policy rules (`PolicyEvaluator`)

| ROE | Fire actions (`FireBallistic`, `FireGuided`) |
|-----|---------------------------------------------|
| HoldFire | Deny (`RoeHoldFire`) |
| WeaponsTight | Deny (`WeaponsTight`) |
| WeaponsFree | Allow |

Non-fire orders (`Move`, `Hold`, `Observe`, `Jam` from EW posture) pass at MVP.

## SDK / build (2026-05-29)

- Projects target **net8.0**; `global.json` pins SDK `8.0.400` with `"rollForward": "latestMajor"` so **SDK 10.0.x** can build.
- Running tests requires **Microsoft.NETCore.App 8.x** runtime (install via `winget install Microsoft.DotNet.Runtime.8` if `dotnet test` reports missing `8.0.0`).

## Tests

- `ProjectAegis.Sim.Tests/Policy/PolicyEvaluatorTests.cs`
- `ProjectAegis.Delegation.Tests/Roe/RoePolicyAdapterTests.cs`
- Existing tests still use `PassthroughRoeFilter` directly on `AutonomyGate`

## GitNexus

Re-run impact before changing `DelegationOrchestrator` or `RoePolicyAdapter`:

```bash
npx gitnexus impact --repo cmano-clone -d upstream DelegationOrchestrator
```

## Next (2026-05-29 update)

- ~~Per-unit `PolicySnapshot` on agent assign~~ — `AssignAgentToTarget` + `PolicySnapshotRegistry`
- ~~Policy denial order-log entries~~ — `DecisionLog.AppendPolicyDenial` on gate reject
- ~~`IEngagementResolver` MVP~~ — `MvpEngagementResolver` (DLZ, envelope, magazines)
- ~~Scenario-level ROE templates~~ — `ScenarioPolicyCatalog` + `scenarioPolicyId` on configurator
- ~~Engagement tick pipeline~~ — `SimTickPipeline` + `SimulationSession`
- ~~Order log union~~ — `DecisionLog.ChronologicalEntries()` + `ComputeFingerprint()`
- ~~JSON scenario policy import~~ — `data/scenarios/*.policy.json` via `ScenarioPolicyRepository`
- ~~Replay fingerprint gate (MVP)~~ — `ReplayOrderLogFingerprintTests`
- ~~Engage world priming~~ — `SimulationSession.PrimeEngageWorld` + `DefaultEngageContext` on MVP factory
- ~~Unity scaffold~~ — `tools/init-unity-project.ps1` → `unity/ProjectAegis` (Assets/Plugins, runtime scripts, manifest)
- ~~Headless play-mode gate~~ — `PlayModeSmokeHarnessTests` (mirrors `SimplePlayModeSimHost`)
- Unity Editor: open project, scene per `unity/ProjectAegis/PLAYMODE-SMOKE.md`; platform DB envelopes (doc 06)
