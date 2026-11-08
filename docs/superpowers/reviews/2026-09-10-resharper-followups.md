# ReSharper runtime integration follow-ups

These seven findings expose absent production capability providers. They remain
open integration work, separate from analyzer cleanup as required by Phase 1.
The probes are valid extension points and their missing-provider paths fail
closed. No warning is suppressed and no provider is invented to satisfy analysis.

Production composition reviewed: `SimplePlayModeSimHost` implements
`ISimWorldSnapshot` without these optional interfaces;
`DelegationBridgeHost.RunTick` passes that snapshot to the projection bridges.
Tests provide capabilities explicitly, but do not replace shipped integration.

| ID | Capability / location | Owner | Required integration and acceptance |
|---|---|---|---|
| RSH-001 | `ISensorToShooterShooterSource`, `SliceAContactFrameBridge.Build` | Runtime composition maintainer | Supply authoritative shooter candidates through the existing explicit provider seam. Preserve empty-ammo and missing-evidence rejection. |
| RSH-002 | `ISliceAContactAuthoritySource`, `SliceAContactFrameBridge.Build` | Runtime authority maintainer | Supply actor/contact/time-bound authority facts; technical eligibility must never grant weapons clearance. |
| RSH-003 | `ICoordinationFacts`, `CoordinationBridge.Build` | Coordination maintainer | Wire existing authored `CoordinationScenarioFacts` through the optional facts argument. Unknown roles and coverage must remain unknown when absent. |
| RSH-004 | `IAdviceEvidenceSource`, `AdviceBridge.Build` | Advice maintainer | Supply current selected-contact evidence only when identity and exact simulation time match. Preserve the honest `AdviceRuntimeEvidenceBridge` fallback. |
| RSH-005 | `IStatusUnitSource`, `StatusFrameBridge.Build` | Runtime status maintainer | Supply read-only unit component facts with matching unit identity and valid source time; absent data must not imply nominal health. |
| RSH-006 | `IStatusSensorSource`, `StatusFrameBridge.Build` | Runtime sensor maintainer | Supply observed emitter IDs and EMCON through snapshot capability; policy posture alone cannot prove emissions silence. |
| RSH-007 | `IStatusElectronicWarfareSource`, `StatusFrameBridge.Build` | Runtime EW maintainer | Supply current time-bound EW facts; absent data remains `EW: UNKNOWN`. |

Review each entry when its production snapshot producer is implemented. Keep
these entries deferred in `tools/resharper/ledger.json` until that integration is
tested in the runtime composition, not merely against another test double.

Existing headless evidence: `SliceAContactFrameTests`, `CoordinationBridgeTests`,
`CoordinationCommandBridgeTests`, `AdviceBridgeTests`, and `StatusFrameTests`
exercise missing and supplied capabilities, identity mismatch, stale/future
facts, and withheld authority. The 2026-09-10 baseline passes 3,216 solution tests.

Architecture constraints: **ADR-010 §2–3**, **ADR-007**, and **ADR-001**. Runtime
producers must use read-only snapshot/projection composition. `DelegationBridge.cs`
remains zero-touch. These follow-ups do not authorize new UI authority or
changes to existing catalog write paths.
