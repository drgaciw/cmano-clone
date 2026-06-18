# Epic: Sprint 27 — Phase C Presentation

> **Status:** Ready  
> **Sprint:** 27  
> **Dates:** 2026-09-04 → 2026-09-17  
> **Trunk:** `main` @ `ab30d35`  
> **Layer:** Presentation  
> **GDD / ADR:** ADR-007 (map symbology), ADR-011 Phase C viewer, ADR-010 headless-first UI

## Goal

Wire **Addressables** for `Map/App6FrameAtlas`, promote **ADR-011 Phase C** platform catalog viewer from S26 spike to UXML/USS panel with search/filter, and close presentation evidence gaps — headless proxy = merge authority.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-010 | Accepted | Headless-first; ZERO touch `DelegationBridge` |
| ADR-011 | Accepted | Phase C read-only viewer |
| ADR-007 | Accepted | APP-6 atlas presentation |

## Graphite Stack

```
main
 └── stack/sprint27/full-sln-gate              (S27-01)
      └── stack/sprint27/addressables-app6-atlas     (S27-07)
           └── stack/sprint27/platform-viewer-panel      (S27-08)
                ├── stack/sprint27/platform-viewer-harness    (S27-11)
                ├── stack/sprint27/presentation-evidence       (S27-10)
                └── stack/sprint27/platform-viewer-detail      (S27-15 — nice)
```

**Dependency:** S27-08 blocked on S27-04 (data import gate) + S27-09 (browse enrichment recommended first).

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 07 | [Addressables APP-6 atlas](story-027-07-addressables-app6.md) | S27-07 | Integration | should-have | 1d | Ready |
| 08 | [Platform viewer panel](story-027-08-platform-viewer-panel.md) | S27-08 | UI | should-have | 2d | Ready |
| 10 | [Editor presentation evidence](story-027-10-editor-evidence.md) | S27-10 | Visual | should-have | 0.5d | Ready |
| 11 | [Viewer smoke harness](story-027-11-viewer-smoke-harness.md) | S27-11 | Integration | should-have | 0.5d | Ready |
| 15 | [Browse detail pane](story-027-15-browse-detail-pane.md) | S27-15 | UI | nice-to-have | 0.5d | Ready |

## GitNexus Mandatory Rules

- **ZERO touch:** `DelegationBridge.cs`
- **Read-only viewer:** no `CatalogWriteGate` / `IWriteGate` calls in viewer path
- `useGlobeMap=false` on `DelegationSmoke.unity` unchanged

## Definition of Done

- [ ] Headless `PlatformCatalogViewerTests` PASS
- [ ] App6 + PlayMode smoke filters green
- [ ] Optional Editor evidence (advisory)
- [ ] Tracker row 21 Phase C progress note

## References

- Spike: `production/qa/sprint-26-platform-viewer-spike-2026-06-18.md`
- Kickoff: `production/sprints/sprint-27-cmo-corpus-combat-bounded.md`
- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`