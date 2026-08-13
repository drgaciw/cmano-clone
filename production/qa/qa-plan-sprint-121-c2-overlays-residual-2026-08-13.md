# QA Plan — Sprint 121 C2 Overlays Residual-Scope (2026-08-13)

**Sprint:** S121 · **Linear:** [DRG-156](https://linear.app/drgamtd-workspace/issue/DRG-156) · **Stage:** Release  
**Scope:** Docs-only residual inventory — **no C# verification required**

## Stories

| Story | Evidence |
|-------|----------|
| S121-01 | Kickoff `production/agentic/sprint-121-parallel-kickoff-2026-08-13.md` published |
| S121-02 | Residual inventory in `production/sprints/sprint-121-c2-overlays.md` — landed S108 path cited, forbidden rebuilds listed |
| S121-03 | This QA plan stub |

## Verification (docs sprint)

| Check | Method | Pass criteria |
|-------|--------|---------------|
| No C# diff | `git diff --stat` | Only `production/**` paths |
| CMD-32/34 on trunk cited | Read sprint plan + grep symbols | `DatalinkPictureProjection`, `TacticalOverlayProjection` marked **Landed S108** |
| File-disjoint from S120 | Read sprint plan § S120 verdict | States **not blocked** on S120; different hosts/files |
| Forbidden rebuilds | Read sprint plan | Explicit table; no “rebuild overlay pipeline” language |
| Invariants cited | Sprint plan footer | Baltic hash, DelegationBridge zero-touch, stage Release |

## Explicitly out of scope (no QA run)

- `dotnet build` / `dotnet test` (no code changes)
- Play Mode smoke / Editor visual overlay sign-off
- ReplayGolden / PlayModeSmokeHarness rerun
- Overlay visual rendering acceptance

## Residual gaps documented (watch list for future product dispatch)

- Map canvas / Cesium ring and edge **geometry** not on trunk
- Default map UXML missing overlay count labels (optional HUD)
- Live comms-driven datalink status not wired
- Play Mode human checklist for visible overlays still open

---
*QA S121 residual-scope — 2026-08-13. Docs-only. Do not rebuild CMD-32/34.*
