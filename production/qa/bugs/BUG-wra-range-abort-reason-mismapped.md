# Bug Report

## Summary
**Title**: `FireAbortReason.WraRange` policy denial silently mislabeled as `ROE_HOLD_FIRE` in engagement results
**ID**: BUG-wra-range-abort-reason-mismapped
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Fixed (this loop) — TDD test + minimal production fix committed
**Reported**: 2026-07-06
**Reporter**: gameplay-qa-agent (qa-loop-09-doctrine-glossary)

## Classification
- **Category**: Gameplay (order log / replay / doctrine explainability)
- **System**: Doctrine/Policy → Engagement resolution (`ProjectAegis.Sim.Policy`, `ProjectAegis.Sim.Engage`, `ProjectAegis.Sim.Glossary`)
- **Frequency**: Never under the current shipped `PolicyEvaluator` (it never emits `WraRange`); Always if any `IPolicyEvaluator` implementation legitimately denies for range-band/WRA reasons — a documented, expected extension point.
- **Regression**: No — appears to be a since-inception gap; the mapping switch was never exhaustive over `FireAbortReason`.

## Environment
- **Build**: worktree `qa-loop-09-doctrine-glossary`, branch head at `5eab203`
- **Platform**: headless `dotnet test` (no Unity Editor available in this sandbox)
- **Scene/Level**: N/A — plain .NET sim layer (`ProjectAegis.Sim.Tests`)
- **Game State**: Any engagement resolved via `MvpEngagementResolver` with an `IPolicyEvaluator` injected

## Reproduction Steps
**Preconditions**: A `MvpEngagementResolver` constructed with an `IPolicyEvaluator` that denies an engage action with `FireAbortReason.WraRange` (a real, glossary-catalogued doctrine reason — see `data/glossary/abort_reason_manifest.json`, family `Doctrine`, log code `WRA_RANGE`; also generated as `AbortReasonCatalog.Doctrine.WRA_RANGE`).

1. Set up a valid in-envelope `EngageContext` (target in range, radar on, fire-control track present).
2. Inject a policy evaluator whose `Evaluate(...)` returns `PolicyVerdict.Deny(FireAbortReason.WraRange)`.
3. Call `resolver.Resolve(request)`.

**Expected Result**: The engagement result's abort reason should correspond to the WRA range-doctrine denial — mapped to `EngagementAbortReason.OutOfEnvelope` (`AbortReasonCatalog.Engage.OUT_OF_ENVELOPE`), consistent with the GDD (`design/gdd/policy-roe-emcon-wra.md`, TR-policy-005 "WRA before engagement geometry") and with every other blocked-weapon action producing an "explainable FireAbortReason logged for replay and briefing."

**Actual Result** (before fix): `MvpEngagementResolver.MapPolicyDenial` has a non-exhaustive `switch` over `FireAbortReason` that only explicitly handles `RoeHoldFire`, `WeaponsTight`, `WraSalvo`, `EmconOff`, `NoFireControlTrack`. Every other member — including `WraRange`, and also `CommsDenied` and all six `*AspectBlock` values if ever routed through this path — falls into the default arm `_ => EngagementAbortReason.RoeHoldFire`, so the engagement is misreported as a ROE Hold-Fire denial (`ROE_HOLD_FIRE`) in the order log / replay fingerprint / UI tooltip, even though the actual doctrine reason was a WRA range restriction. This corrupts the audit trail the GDD explicitly promises ("staff answer, not a mystery") and would silently defeat replay-fingerprint determinism checks that key off the abort-reason code.

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs` (`MapPolicyDenial`, lines ~156-165) — the fixed symbol
  - `src/ProjectAegis.Sim/Policy/FireAbortReason.cs` — defines `WraRange = 3`
  - `src/ProjectAegis.Sim/Glossary/AbortReasonManifest.cs` / `AbortReasonCatalog.Generated.cs` / `data/glossary/abort_reason_manifest.json` — doctrine glossary source of truth that catalogues `WRA_RANGE` but has no producer wired to it
  - `src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs` (`BindMvpEngagement`) — production wiring that passes `orchestrator.PolicyEvaluator` straight into `MvpEngagementResolver`, so any future/custom `IPolicyEvaluator` hits this bug
- **Related systems**: Order Log & Replay (fingerprint hashing depends on correct abort codes), C2 UI (weapon-hover "top-3 abort reasons"), Agent Delegation (`IPolicyEvaluator` is a public extension point per ADR-002)
- **Possible root cause**: The `MapPolicyDenial` switch was written to cover only the reasons the one shipped `PolicyEvaluator` implementation currently produces, rather than being kept exhaustive against the full `FireAbortReason` enum / doctrine glossary manifest. No test previously asserted the mapping for reasons outside that shipped set.

## Evidence
- **New failing test (red)**: `ProjectAegis.Sim.Tests.Glossary.DoctrineAbortReasonMappingTests.WraRange_policy_denial_maps_to_out_of_envelope_not_hold_fire`
  - Before fix: `Assert.Equal() Failure: Expected: OutOfEnvelope / Actual: RoeHoldFire`
  - After fix: Passed.
- **Regression run (green)**: `ProjectAegis.Sim.Tests` 282/282 (baseline 281/281 + 1 new test); `ProjectAegis.Delegation.Tests` 251/251 (unchanged); `ProjectAegis.Delegation.UnityAdapter.Tests` 260/260 (unchanged) — all pass after the fix.

## Impact / Blast Radius (manual analysis — GitNexus MCP/CLI not reachable inside this isolated worktree; `.gitnexus/run.cjs` absent)
`MvpEngagementResolver.MapPolicyDenial` is a `private static` method with exactly one call site (`Resolve`, same class, line 79). `MvpEngagementResolver` itself is constructed only in: (a) `ProjectAegis.Sim.Tests` fixtures, and (b) `SimulationSession.BindMvpEngagement` (production wiring), which is exercised transitively by `ProjectAegis.Delegation.Tests` and the Baltic replay/golden harnesses in `ProjectAegis.Delegation.UnityAdapter.Tests`. Grep confirms no shipped `IPolicyEvaluator` implementation (`PolicyEvaluator`, `PassthroughPolicyEvaluator`) currently emits `FireAbortReason.WraRange`, so the fix is behavior-neutral for all existing scenarios/replay goldens (confirmed: all three suites pass at unchanged counts). Risk classified **LOW** — the change only activates a previously-unreachable/mismapped code path and adds one new `switch` arm without altering any existing arm.

## Related Issues
- Sibling loop `qa-loop-02-roe-engage` covers ROE/Engagement bugs directly — this bug is scoped narrowly to the *doctrine glossary → engagement abort-reason mapping* boundary and does not touch ROE evaluation logic itself.
- Not fixed in this pass (flagged for follow-up, out of minimal-fix scope): `FireAbortReason.CommsDenied` and the six `*AspectBlock` reasons are also unhandled by `MapPolicyDenial`'s default arm. They are not currently reachable through this path either (comms-guard intercepts `CommsDenied` upstream in `SimulationSession`; aspect-block reasons are only produced via `DomainValidatorRegistry` → `MapDomainDenial`, a separate switch that already handles them correctly). Recommend a follow-up hardening pass to either make `MapPolicyDenial` fully exhaustive or throw a diagnostic exception on truly-unexpected reasons instead of defaulting to `RoeHoldFire`.

## Notes
Task also considered (and ruled out as not-a-bug): `SpeculativePlatformCatalog.ParseMaturity` defensively defaults unparseable `technologyMaturity` JSON values to `TechnologyMaturityTag.Simulated` — reviewed, behaves as documented/intended, no test gap worth flagging beyond what already exists in `ScenarioSpeculativeGateTests`.
