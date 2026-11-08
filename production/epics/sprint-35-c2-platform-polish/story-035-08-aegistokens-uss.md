---
id: S35-08
status: Complete
completed: 2026-06-19
type: UI
priority: should-have
graphite_branch: stack/sprint35/aegistokens-uss
estimate_days: 1.5
dependencies:
  - S35-04 profiler baseline merged
owner: team-unity
sprint: 35
req_trace: design/art/art-bible.md §3; Platform Editor Phases C–H
governing_adrs: ADR-011
---

# Story 035-08 — AegisTokens USS Consolidation

> **Epic:** sprint-35-c2-platform-polish

## Summary

Create `Assets/UI/AegisTokens.uss` with art-bible canonical tokens; import into Platform Editor USS (Phases C–H). Schema-only presentation — no sim or write-gate behavior changes.

## Acceptance Criteria

- [x] `unity/ProjectAegis/Assets/UI/AegisTokens.uss` exists with §3 hex/rgba tokens
- [x] `PlatformCatalog/`, `PlatformImport/` USS import shared tokens
- [x] `dotnet test --filter "PlatformImport|PlatformCatalogViewer|PlatformComms|PlatformLinkCatalog"` — **≥51/51** PASS
- [x] `ReplayGoldenSuiteTests` — **6/6** PASS
- [x] ZERO touch `DelegationBridge.cs`
- [x] Evidence: `production/agentic/sprint-35-aegistokens-consolidation-YYYY-MM-DD.md`

## QA Test Cases

```
Test: Platform Editor headless suite after USS token import
  Given: AegisTokens.uss merged
  When: Platform filter suite
  Then: ≥51/51 PASS; no binding regressions
```

## Test Evidence Path

- `unity/ProjectAegis/Assets/UI/AegisTokens.uss`
- `production/agentic/sprint-35-aegistokens-consolidation-YYYY-MM-DD.md`

## Out of Scope

- C2 left-drawer full token migration (cut line item 2)
- Sim behavior changes