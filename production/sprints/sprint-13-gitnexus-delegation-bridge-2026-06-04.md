# Sprint 13 — GitNexus DelegationBridge (fallback)

**Date:** 2026-06-04  
**Symbol:** `DelegationBridge`  
**CLI:** `gitnexus impact` failed — multiple indexed repos; specify `repo` label failed string match

## Risk (documented)

**CRITICAL** per program kickoff — upstream callers include Unity presentation and headless harness.

## Grep blast radius (direct references, sample)

| Area | Files |
|------|-------|
| Bridge | `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs` |
| Host | `unity/.../DelegationBridgeHost.cs`, `C2PresentationController.cs`, `RightUnitPanelHost.cs` |
| Tests | `DelegationBridgeAttackOptionTests.cs`, `UnitDetailBridgeTests.cs`, `PlayModeSmokeHarnessTests.cs` |
| Harness | `BalticReplayHarness` (via adapter tests / smoke filters) |

## Sprint 13 action

**No bridge edits** — doc 04 mapping only. Re-run MCP/CLI impact before any PR that touches `DelegationBridge` APIs.