---
id: S29-07
status: Not Started
type: UI
priority: should-have
graphite_branch: stack/sprint29/doctrine-visual
estimate_days: 1
dependencies:
  - S29-01 green baseline
owner: team-unity
sprint: 29
req_trace: Req 13 Doctrine; ADR-010 panel seam
---

# Story 029-07 — Doctrine Inheritance Panel Visual Sign-off

> **Epic:** sprint-29-c2-core-loop  
> **ADR:** ADR-010 (Accepted — headless-first; doctrine panel seam)

## Summary

Editor/PlayMode evidence for **Doctrine Inheritance Panel** (closes S22/S23 deferred visual gate). Headless tests unchanged — projection/binder/command tests remain merge authority. ZERO touch `DelegationBridge`.

## Acceptance Criteria

- [ ] `DoctrineInheritancePanelHost` wired with UXML/USS in `DelegationSmoke` scene
- [ ] Editor or PlayMode evidence captured: `production/qa/evidence/doctrine-panel-s29-*.png` or lean proxy doc
- [ ] ROE override round-trip visible in panel (apply → read-back)
- [ ] Headless doctrine tests unchanged PASS (`DoctrineOverrideCommandTests`, `DoctrineInheritanceProjectionTests`, `DoctrineInheritancePanelBinderTests`)
- [ ] `PlayModeSmokeHarnessTests` doctrine row PASS
- [ ] Doctrine writes route `DoctrineInheritancePanelHost` → `DelegationBridgeHost.TrySetDoctrineOverride` only
- [ ] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Editor/PlayMode visual evidence
  - Setup: open `DelegationSmoke` scene; select unit with doctrine inheritance
  - Verify: panel shows unit id, ROE, salvo, source, override controls
  - Pass condition: evidence file exists with screenshots + console excerpt (no missing-reference errors)

- **AC-2**: ROE override round-trip
  - Setup: unit with inherited ROE displayed
  - Verify: change ROE via dropdown + apply; read-back matches override
  - Pass condition: headless `DoctrineOverrideCommandTests` still PASS; visual matches projection

- **AC-3**: Headless regression unchanged
  - Given: existing doctrine test suite from S22/S23
  - When: full doctrine filter runs
  - Then: all prior tests PASS without modification
  - Edge cases: UXML element name drift vs host constants

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "Doctrine" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|Doctrine" -v minimal
# Optional Editor batch
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario doctrine
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `DoctrineInheritancePanelHost` | LOW |
| `DelegationBridgeHost` | MEDIUM — seam only |
| `DelegationBridge.cs` | ZERO touch |

## References

- UX spec: `design/ux/c2-command-post.md`
- S22 pattern: `production/agentic/sprint-22-pr-description-2026-06-17.md`
- S23-U01 plan: `production/agentic/sprint-23-plan-unity-2026-06-17.md`
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md` (S29-07)
- Track plan: `production/agentic/sprint-29-plan-unity-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*