# Sprint 113 — Asset Specced→Done Wave 3

**Dates:** 2026-08-11  
**Program:** Release Product Progress (S110–S114) — **S113**  
**Predecessor:** S112 COMPLETE  
**Stage:** **Release** · **Not Launch**  
**Authority:** `production/agentic/agentic-workflow-sprint-series-2026-08-09.md`, `design/assets/asset-manifest.md`, `design/assets/approved-criteria-2026-07-14.md`  
**QA:** `production/qa/qa-plan-sprint-113-asset-wave-3-2026-08-11.md`  
**Kickoff:** `production/agentic/sprint-113-parallel-kickoff-2026-08-11.md`

## Goal

Advance **≥2 Specced C2 children → Done** with honest on-disk USS under `production/assets/c2/`, matching Done bar (A1+A5 minimal content). **Never invent Approved** (Path A human phrase only). Assets + docs only.

## Tracks (surface-disjoint)

| Track | Story | Surface | Assets |
|-------|-------|---------|--------|
| A C2 shell children | S113-01 | `production/assets/c2/C2LeftDrawerPanel.uss`, `RightUnitDetailPanel.uss` | **007, 008** |
| B C2 overlay children | S113-02 | `production/assets/c2/DelegationBadgeOverlay.uss`, `PolicyEmconHud.uss` | **011, 012** |
| C Manifest + closeout | S113-03 | `design/assets/asset-manifest.md`, specs status lines, smoke | honesty |

**Rule:** Lanes A/B edit **only their USS files**. Lane C owns manifest/spec status after A+B merge.

## Must Have

| ID | AC |
|----|-----|
| S113-01 | ASSET-007 + ASSET-008 USS at Done quality (header Status: Done; AegisTokens; classes per spec); no C# |
| S113-02 | ASSET-011 + ASSET-012 same bar |
| S113-03 | Manifest: 4× Specced→**Done** with paths; counts honest (Specced −4, Done +4); specs status; smoke; stage Release |

## Non-goals

Approved promotion · Addressables bulk · store screenshots 027–034 · Launch · Phase N · DelegationBridge · C# hotpath

## Hard gates

Stage Release · ZERO Bridge · assets-only (suite cite last green if no C#) · no invented Approved

## Definition of Done

- [ ] 007/008/011/012 **Done** in manifest with production paths  
- [ ] Smoke closeout  
- [ ] Residual list (009/010/013/015–017 still Specced)
