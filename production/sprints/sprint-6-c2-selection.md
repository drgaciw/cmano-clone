# Sprint 6 — C2 selection sync

**Dates:** 2026-06-02 (1 day)  
**Goal:** Map ↔ OOB ↔ unit detail selection without mutating sim state.

## Delivered

- `C2PresentationController` + `C2SelectionResolver` (presentation-only)
- Map symbol click → friendly unit / hostile contact
- OOB ListView row click → unit selection
- Contacts tab row click → contact summary on unit panel
- Selection USS: `map-symbol--selected`, `oob-row--selected`
- Unit detail `CONTACT:` line when contact selected
- Tests: selection resolver, map/OOB binders, contact summary

## Verification

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
```

Unity: `PLAYMODE-SMOKE.md` § Sprint 6 QA checklist.

## Next

- Cesium Phase B spike (`docs/engineering/cesium-phase-b-spike-checklist.md`)
- Commit Sprints 3–6 when approved
- GDD system #18+ via `/design-system`