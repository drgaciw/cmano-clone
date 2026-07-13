# Sprint 25 — Data / Platform Track Plan

**Date:** 2026-06-17  
**Kickoff:** `production/sprints/sprint-25-phase-b-damage-assurance.md`  
**Maps to:** S25-02..07, S25-13

## Goal

Close ADR-011 Phase B: damage columns on Platforms sheet through reader → write-gate → importer → validator; merge S24-11 ClosedXML UX.

## Critical path (6.5d must-have)

| Story | Days | Key deliverable |
|-------|------|-----------------|
| S25-02 | 1 | Migration `009` + `CatalogPlatformDamage` types |
| S25-03 | 1.5 | `TryGetPlatformDamage` + export columns |
| S25-04 | 2 | `CatalogWriteGate` damage batch commit (extend-only) |
| S25-05 | 1.5 | Importer E2E + empty-diff golden |

## Parallel slot

| Story | Days | Notes |
|-------|------|-------|
| S25-07 | 1 | Rebase `stack/sprint24/closedxml-phase-b-ux` after S25-01 |

## Top risks

1. **CatalogWriteGate CRITICAL** — extend-only; full Phase A/B regression mandatory
2. **Scope creep** — platform HP + flags only; no combat runtime / component tables
3. **ICatalogReader HIGH** — additive `TryGetPlatformDamage` only

## Verify (closeout)

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Platform|CatalogPhaseB|CatalogPhaseBDamage|CatalogSortKey" -v minimal
dotnet test src/ProjectAegis.Data.Excel.Tests/ProjectAegis.Data.Excel.Tests.csproj -v minimal
```

## Graphite branches

`stack/sprint25/damage-schema-009` → `damage-reader-export` → `damage-write-gate` → `damage-importer` → `damage-validator`

Parallel: `stack/sprint25/closedxml-phase-b-ux`

*Condensed from parallel Data/Platform planning agent — 2026-06-17.*