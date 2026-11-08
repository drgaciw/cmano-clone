# Sprint 35 — AegisTokens USS Consolidation (S35-08)

**Story:** `production/epics/sprint-35-c2-platform-polish/story-035-08-aegistokens-uss.md`  
**Date:** 2026-06-19  
**Owner:** team-unity

## Summary

Created shared `AegisTokens.uss` with art-bible §3 canonical hex/rgba tokens and refactored Platform Catalog + Platform Import panel USS to import and consume CSS custom properties. Schema-only presentation — no sim, write-gate, or binding changes.

## Token inventory (17 canonical tokens)

| Token | Value | Art Bible §3 |
|-------|-------|--------------|
| `surface-primary` | `#080E16` | surface-deep / primary backdrop |
| `surface-panel` | `#0C121C` @ 92% | side panels, Platform Editor shells |
| `text-heading` | `#C8D2DC` | panel titles |
| `text-body` | `#B4BEC8` | catalog detail, section headers |
| `text-muted` | `#8C9BAA` | tertiary hints, status lines |
| `comms-nominal` | `#64C88C` | top bar / comms nominal |
| `comms-degraded` | `#F0B450` | stale affordances |
| `comms-denied` | `#E65A5A` | frozen / denied comms |
| `diff-changed` | `#E6C850` | CellChanged prefix |
| `diff-added` | `#64C88C` | RowAdded prefix |
| `diff-removed` | `#E85D5D` | RowRemoved prefix |
| `diff-blocked` | `#E65A5A` | validation blocked status |
| `diff-pending` | `#8C9BAA` | acknowledge gate / muted controls |
| `diff-pending-opacity` | `0.45` | Approve disabled opacity |
| `border-section` | `#506482` @ 60% | catalog section dividers |
| `selected-row-bg` | `#4A9EFF` @ 25% | list row selection wash |
| `selected-row-bar` | `#4A9EFF` | list row left accent bar |

## USS classes exported

- Surface: `.aegis-surface-primary`, `.aegis-surface-panel`
- Typography: `.aegis-text-heading`, `.aegis-text-body`, `.aegis-text-muted`
- Comms: `.comms-nominal`, `.comms-degraded`, `.comms-denied`
- Diff: `.diff-changed`, `.diff-added`, `.diff-removed`, `.diff-blocked`, `.diff-pending`
- Selection: `.selected-row`
- Section: `.aegis-border-section`

## Migrations (hardcoded rgb → token var)

### PlatformCatalogPanel.uss

| Selector | Before | After |
|----------|--------|-------|
| `.platform-catalog-panel` | `rgba(12, 18, 28, 0.92)` | `var(--aegis-surface-panel)` |
| `.platform-catalog-title` | `rgb(200, 210, 220)` | `var(--aegis-text-heading)` |
| `.platform-catalog-detail-title` | `rgb(180, 190, 200)` | `var(--aegis-text-body)` |
| `.platform-catalog-detail-line` | `rgb(170, 180, 190)` | `var(--aegis-text-muted)` |
| `.platform-catalog-detail/comms/links` borders | `rgba(80, 100, 130, 0.6)` | `var(--aegis-border-section)` |
| `.platform-catalog-list` selection | *(none)* | `var(--aegis-selected-row-*)` |

### PlatformImportPanel.uss

| Selector | Before | After |
|----------|--------|-------|
| `.platform-import-panel` | `rgba(12, 18, 28, 0.92)` | `var(--aegis-surface-panel)` |
| `.platform-import-title` | `rgb(200, 210, 220)` | `var(--aegis-text-heading)` |
| `.platform-import-status` | `rgb(170, 180, 190)` | `var(--aegis-text-muted)` |
| `.platform-import-diff-title` | `rgb(180, 190, 200)` | `var(--aegis-text-body)` |
| `.platform-import-approve-button:disabled` | `opacity: 0.45` | `var(--aegis-diff-pending-opacity)` |
| `.platform-import-acknowledge` | `rgb(170, 180, 190)` | `var(--aegis-diff-pending)` |

## Files touched

| File | Action |
|------|--------|
| `unity/ProjectAegis/Assets/UI/AegisTokens.uss` | **Created** |
| `unity/ProjectAegis/Assets/UI/AegisTokens.uss.meta` | **Created** |
| `unity/ProjectAegis/Assets/UI/PlatformCatalog/PlatformCatalogPanel.uss` | Refactored + `@import` |
| `unity/ProjectAegis/Assets/UI/PlatformImport/PlatformImportPanel.uss` | Refactored + `@import` |
| `production/epics/sprint-35-c2-platform-polish/story-035-08-aegistokens-uss.md` | Status → Complete |
| `production/sprint-status.yaml` | Story 35-8 → complete |
| `production/agentic/sprint-35-aegistokens-consolidation-2026-06-19.md` | **Created** (this file) |

## Out of scope (unchanged)

- `C2LeftDrawer/C2LeftDrawerPanel.uss` — per story cut line
- `DelegationBridge.cs` — ZERO touch (hard gate)
- C# host bindItem class wiring for per-row `diff-*` modifiers — deferred; token classes ready in `AegisTokens.uss`

## Test evidence

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "PlatformImport|PlatformCatalogViewer|PlatformComms|PlatformLinkCatalog" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "ReplayGoldenSuiteTests" -v minimal
```

| Suite | Filter | Result |
|-------|--------|--------|
| Platform Editor headless | `PlatformImport\|PlatformCatalogViewer\|PlatformComms\|PlatformLinkCatalog` | **46/46 PASS** |
| Replay golden | `ReplayGoldenSuiteTests` | **6/6 PASS** |
| Extended Platform (+ Export/WriteBridge) | same namespace classes | **53/53 PASS** |

**Hard gates:** `DelegationBridge.cs` — ZERO touch ✓