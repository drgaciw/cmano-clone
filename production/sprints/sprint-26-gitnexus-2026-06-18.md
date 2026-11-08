# Sprint 26 GitNexus Re-index + Closeout Hygiene — 2026-06-18

**Repo:** `/home/username01/cmano-clone/cmano-clone`  
**Branch:** `main`  
**Trunk:** `main` @ S26 closeout (S26-01..11)  
**Command:** `npx gitnexus analyze . --force`  
**Duration:** 19.3s  

## Graph stats

| Metric | Value |
|--------|-------|
| Nodes | 10,656 |
| Edges | 22,048 |
| Clusters | 273 |
| Flows | 300 |

**Prior baseline (Sprint 26 should-have @ `9638a2c`):** 10,385 nodes / 21,510 edges — **+271 nodes / +538 edges** after S26-09..10 nice-to-have stack.

## Status

```
Repository: cmano-clone
Indexed: 2026-06-18
Status: up-to-date (S26 closeout stack)
```

## Sprint touch-set blast radius (manual note)

| Symbol | Risk | Notes |
|--------|------|-------|
| `CatalogWriteGate` | **CRITICAL** | Extend-only; S26-02..04 CMO import unchanged at closeout |
| `MvpEngagementResolver` | **MEDIUM** | S26-09 optional `DomainValidatorRegistry` hook; default off |
| `CatalogPlatformBrowseProjection` | **LOW** | S26-10 read-only browse; no write path |
| `DelegationBridge` | **N/A** | **ZERO** file touches vs trunk |

## Verification gates (S26 closeout)

| Gate | Result |
|------|--------|
| `ReplayGoldenSuiteTests` | **6/6 PASS** |
| Full solution `dotnet test ProjectAegis.sln` | **698/698 PASS** |
| Replay golden drift | **NONE** |
| `DelegationBridge.cs` ZERO touch | **PASS** |

### Per-project test counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 115 |
| ProjectAegis.Delegation.Tests | 182 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 121 |
| ProjectAegis.MissionEditor.Cli.Tests | 24 |
| ProjectAegis.Data.Tests | 251 |
| ProjectAegis.Data.Excel.Tests | 5 |

## Tracker row 06 (Req 06 Database Intelligence)

**Updated:** CMO Phase 2 bounded weapon + platform markdown import through `CatalogWriteGate` (S26-02..04); golden hygiene + replay unchanged. Next: full corpus nightly job; TL branching.

## Branch hygiene — `stack/sprint25/*`

Local refs remain (merged S25 stack). Safe to prune after closeout commit:

```bash
git branch -d stack/sprint25/full-sln-gate \
  stack/sprint25/damage-schema-009 \
  stack/sprint25/damage-reader-export \
  stack/sprint25/damage-write-gate \
  stack/sprint25/damage-importer \
  stack/sprint25/damage-validator \
  stack/sprint25/closedxml-phase-b-ux \
  stack/sprint25/doctrine-emcon-readonly \
  stack/sprint25/cesium-editor-evidence \
  stack/sprint25/planning-kickoff
```

(Additional S25 branches may exist on remote; prune only after confirming merged.)