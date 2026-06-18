---
id: S27-08
status: Ready
type: UI
priority: should-have
graphite_branch: stack/sprint27/platform-viewer-panel
estimate_days: 2
dependencies:
  - S27-04 complete
  - S27-09 recommended
owner: team-unity
sprint: 27
req_trace: Req 21 Phase C; ADR-011
---

# Story 027-08 — Platform Catalog Viewer Panel

> **Epic:** sprint-27-phase-c-presentation  
> **ADR:** ADR-011 (read-only Phase C), ADR-010

## Summary

Upgrade S26-10 spike to full `PlatformCatalogPanel.uxml` + `.uss` with search/filter on `PlatformId`; bind `PlatformCatalogViewerHost` by stable element names. **Read-only** — no write-gate calls.

## Acceptance Criteria

- [ ] UXML/USS assets (`platform-catalog-root`, `platform-catalog-search`, `platform-catalog-list`)
- [ ] Case-insensitive filter; stable sort preserved
- [ ] Headless `PlatformCatalogViewerTests` PASS
- [ ] No `IWriteGate` / `Propose*` / `ApproveBatch` in viewer path
- [ ] ZERO touch `DelegationBridge.cs`
- [ ] CLI `catalog_platform_browse` JSON shape unchanged

## QA Test Cases

- **AC-1**: Filter narrows list
  - Given: Baltic fixture with ≥3 platforms
  - When: filter text matches one PlatformId
  - Then: list shows only matching rows in stable order
  - Edge cases: empty filter shows all; no-match filter shows empty

**Manual (advisory):**
- Setup: Unity Editor with viewer scene
- Verify: list + filter visible
- Pass: screenshot archived or protocol placeholder + headless PASS

## References

- Spike: `production/qa/sprint-26-platform-viewer-spike-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`