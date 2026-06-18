---
id: S23-03
status: Ready
type: UI
priority: must-have
graphite_branch: stack/sprint23/doctrine-editor-visual
estimate_days: 2.5
dependencies:
  - S23-02 green baseline
  - S22-05 headless proxy done
  - ADR-010 Accepted
owner: c-sharp-engineer / team-unity
sprint: 23
req_trace: Req 13 (Doctrine ROE/EMCON/WRA), Req 20 §4.1
---

# Story 023-03 — Unity Doctrine Inheritance Panel Editor Visual Sign-Off

> **Epic:** sprint-23-platform-phase-b  
> **Sprint:** 23 — Platform Phase B I/O + Doctrine Polish  
> **ADR:** ADR-010 (headless-first; ZERO touch `DelegationBridge`)

## Summary

`DoctrineInheritancePanelHost` PlayMode batch + manual evidence; WRA/ROE/EMCON fields visible and bound; `SetDoctrineOverride` dispatch verified in Editor; **ZERO touch** `DelegationBridge`. Closes Sprint 22 sign-off **C4**.

## Acceptance Criteria

- [ ] PlayMode smoke PASS (doctrine row + harness)
- [ ] Manual evidence at `production/qa/sprint-23-doctrine-editor-signoff-*.md`
- [ ] WRA/ROE/EMCON fields visible and bound to `ResolvedUnitPolicy` projection
- [ ] Inheritance order explainable (unit > embarked > mission > group > side > scenario)
- [ ] `SetDoctrineOverride` dispatch verified in Editor
- [ ] Grep confirms zero `DelegationBridge.cs` edits
- [ ] Headless regression unchanged: `Doctrine|PlayModeSmoke` filter green

## Verify Commands

```powershell
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "Doctrine|PlayModeSmoke" -v minimal
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "Doctrine" -v minimal
rg "DelegationBridge" unity/ProjectAegis/Assets/Scripts/Runtime/DoctrineInheritancePanelHost.cs
npx gitnexus impact DelegationBridge --direction upstream
# Unity Editor (local): Invoke-C2PlayModeSignoffBatch.ps1 or doctrine PlayMode harness
```

## GitNexus Symbols to Impact-Check

| Symbol | Risk | Rule |
|--------|------|------|
| `DelegationBridge` | **CRITICAL** | **ZERO touch** — grep + impact upstream before merge |
| `DoctrineInheritancePanelHost` | LOW | Impact before UXML/wiring edits |
| `DelegationBridgeHost` | LOW | Seam for `TrySetDoctrineOverride` per ADR-010 |
| `DelegationSmokeSceneBuilder` | LOW | Scene wiring extension |

After edits: `npx gitnexus detect_changes --repo cmano-clone` before commit.

## Files to Create / Modify

| Action | Path |
|--------|------|
| Modify | `unity/ProjectAegis/Assets/Scripts/Runtime/DoctrineInheritancePanelHost.cs` |
| Create | `unity/ProjectAegis/Assets/UI/DoctrineInheritance/DoctrineInheritancePanel.uxml` |
| Create | `unity/ProjectAegis/Assets/UI/DoctrineInheritance/DoctrineInheritancePanel.uss` |
| Modify | `unity/ProjectAegis/Assets/Scripts/Editor/DelegationSmokeSceneBuilder.cs` |
| Modify | `unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity` (rebuild via scene builder) |
| Extend | `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs` |
| Create | `production/qa/sprint-23-doctrine-editor-signoff-*.md` (manual evidence) |

**Forbidden:** `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs`

## References

- Kickoff: `production/sprints/sprint-23-platform-phase-b-doctrine-polish.md` (S23-03)
- Implementation plan: `docs/superpowers/plans/sprint-23-implementation.md`
- Unity plan: `production/agentic/sprint-23-plan-unity-2026-06-17.md` (S23-U01)
- ADR-010: `docs/architecture/adr-010-headless-first-command-driven-ui.md`
- Req 13: `Game-Requirements/requirements/13-Doctrine-ROE-EMCON-WRA.md`