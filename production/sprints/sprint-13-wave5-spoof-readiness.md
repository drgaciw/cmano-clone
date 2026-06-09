# Sprint 13 — Wave 5: spoof track + live readiness (req 19, 16)

**Status:** Complete (2026-06-04 / 2026-06-08)  
**Dates:** 2026-07-02 → 2026-07-16
**Branch:** `stack/wave5-spoof-readiness` (from `feat/wave5-attack-readiness-spoof` work)  
**Goal:** Close tracker P0 gaps for cyber spoof runtime and scenario-driven unit readiness in the engage pipeline.

**Superpowers plan:** [2026-06-04-requirements-wave5-implementation.md](../../docs/superpowers/plans/2026-06-04-requirements-wave5-implementation.md) (Tasks 1–2)

## GitNexus pre-flight

| Symbol | Risk | Action |
|--------|------|--------|
| `DelegationBridge` | CRITICAL | Single owner; minimal diff; impact report in PR |
| `SimulationSession` | HIGH (verify) | `gitnexus impact SimulationSession -d upstream -r cmano-clone` before edit |
| `SpoofTrackTimelineSimulator` | New / low fan-in | Prefer new file over bridge refactor |

## Must have

| ID | Story | Owner | Est. | Acceptance |
|----|-------|-------|------|------------|
| S13-01 | [wave5-001](../epics/wave5-engage-cyber-logistics-slice/story-001-spoof-track-runtime.md) | team-simulation | 2 | `baltic-patrol-spoof` golden + `CYBER_SPOOF_TRACK` |
| S13-02 | [wave5-002](../epics/wave5-engage-cyber-logistics-slice/story-002-spoof-c2-indicator.md) | team-unity | 1.5 | Unit detail / engage preview shows spoof abort |
| S13-03 | [wave5-003](../epics/wave5-engage-cyber-logistics-slice/story-003-live-readiness-json.md) | team-simulation | 2 | `baltic-patrol-readiness` without harness-only map |
| S13-04 | [wave5-004](../epics/wave5-engage-cyber-logistics-slice/story-004-readiness-order-log.md) | team-simulation | 1 | `AIR_NOT_READY` in order log when policy flips |
| S13-05 | `/replay-verify` + update golden if fingerprint changes | Engineering | 1 | PASS artifact dated |
| S13-06 | `gitnexus detect-changes` at PR handoff | Engineering | 0.25 | Only expected flows |

## Should have

| ID | Task | Est. | Acceptance |
|----|------|------|------------|
| S13-07 | `scenario_cyber_status` CLI documents spoof policy fields | 0.5 | Help text + sample JSON |
| S13-08 | Tracker row 19 → evidence paths updated | 0.25 | implementation-tracker |

## Nice to have

- Message log category tint for `CYBER_SPOOF_TRACK`  
- `EngagePreviewProjection` tooltip string in Unity host

## Verification

```bash
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "BalticReplayHarnessSpoofTests|BalticReplayHarnessReadinessPolicyTests"
```

## Dependencies

- Sprint 11 epic/story files  
- Locked comms stack (Sprint 8) for `CommsDenied` interaction tests