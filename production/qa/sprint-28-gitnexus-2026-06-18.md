# Sprint 28 GitNexus Re-index + Closeout Hygiene — 2026-06-18

**Repo:** `/home/username01/cmano-clone/cmano-clone`  
**Branch:** `main`  
**Trunk:** `main` @ S28 closeout (S28-01..11; S28-12 deferred)  
**Command:** `npx gitnexus analyze . --force`  
**Duration:** 33.6s  

## Graph stats

| Metric | Value |
|--------|-------|
| Nodes | 11,851 |
| Edges | 24,408 |
| Clusters | 299 |
| Flows | 300 |

**Prior baseline (Sprint 27 closeout @ `2e4fb07`):** 11,192 nodes / 22,977 edges — **+659 nodes / +1,431 edges** after S28 corpus v2, Phase D Excel write path, surface/subsurface validators, and catalog bridges.

**S28 day-1 baseline (@ `e680075`):** 11,327 nodes / 23,129 edges — **+524 nodes / +1,279 edges** from should-have/nice-to-have stack landing.

## Status

```
Repository: cmano-clone
Indexed: 2026-06-18
Status: up-to-date (S28 closeout stack)
Indexed commit: d210d3d
```

## Sprint touch-set blast radius (manual note)

| Symbol | Risk | Notes |
|--------|------|-------|
| `CatalogWriteGate` | **CRITICAL** | Extend-only; S28-03 platform v2 golden + S28-04 Phase D workbook propose/approve |
| `CmoMarkdownImporter` | **HIGH** | S28-02 nightly platform slices (`ship.md`); chunk 500/batch; propose-only |
| `PlatformWorkbookWriteService` | **HIGH** | S28-04 ADR-011 Phase D export→edit→propose round-trip |
| `DomainValidatorRegistry` | **MEDIUM** | S28-05 surface/subsurface validators; `combatDomainsEnabled=false` on Baltic |
| `PhaseBCatalogDamageReadinessStub` | **MEDIUM** | S28-08 damage consumer wire; readiness/withdraw only |
| `CatalogMagazineResolver` | **MEDIUM** | S28-06 live magazine counts from catalog reader |
| `PlatformCatalogExportBridge` | **LOW** | S28-07 read-only export/diff; no write-gate bypass |
| `DelegationBridge` | **N/A** | **ZERO** file touches vs trunk |

## Verification gates (S28 closeout)

| Gate | Result |
|------|--------|
| `ReplayGoldenSuiteTests` | **6/6 PASS** |
| Full solution `dotnet test ProjectAegis.sln` | **787/787 PASS** |
| Replay golden drift | **NONE** (Baltic production fixtures) |
| `DelegationBridge.cs` ZERO touch | **PASS** |
| ADR-009 flag-on smoke (separate hash) | **PASS** — `combat-domains-smoke` world hash `17144800277401907079` (S27-16; unchanged) |

### Per-project test counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 149 |
| ProjectAegis.Delegation.Tests | 197 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 146 |
| ProjectAegis.MissionEditor.Cli.Tests | 25 |
| ProjectAegis.Data.Tests | 265 |
| ProjectAegis.Data.Excel.Tests | 5 |

## Tracker updates (S28 closeout)

### Row 06 (Req 06 Database Intelligence)

**Updated:** Nightly CMO corpus v2 — `tools/cmo-nightly-import.sh` extended for platform slices (`ship.md`); S28-03 `ShipSlice100PlatformV2` golden hash + WriteGate round-trip on curated `ship-slice-100.md`; S28-11 TL branching spike **PROCEED (export-only)** — diffable drops tagged by TL tier; no runtime branch binding (S28-02..03, S28-11). Next: full `ship.md` nightly approve workflow; balance drift consumer in catalog pipeline; TL production forks deferred.

### Row 18 (Req 18 Combat Domains)

**Updated:** ADR-009 Phase 2 — `SurfaceAspectDomainValidator` + `SubsurfaceAspectDomainValidator` (`SURFACE_ASPECT_BLOCK`, `SUBSURFACE_ASPECT_BLOCK`); S28-08 `CatalogDamageWithdrawEngageGate` wires Phase B damage catalog into readiness/withdraw (no hot-tick apply); S28-09 facility damage projection stub (order-log only); S28-10 `enableBalanceDrift` telemetry consumer (default false); Baltic `combatDomainsEnabled=false` unchanged (S28-05, S28-08..10). Next: mine/land runtime; flip flag on Baltic when golden passes; full facility combat.

### Row 21 (Req 21 Platform Editor)

**Updated:** ADR-011 Phase D in-engine Excel write path — `PlatformWorkbookWriteService` + `PlatformWorkbookWriteBridge` via `CatalogWriteGate` propose→approve; CLI `platform_import_xlsx` routed through write service; S28-07 viewer export/diff hook (`PlatformCatalogExportBridge`, read-only); evidence S28-04, S28-07. Next: Unity import UI chrome; damage sim full hot-tick apply; CMO full corpus nightly approve.

## Branch hygiene — `stack/sprint27/*`

**Verify @ S28-13 closeout:** `git branch -a | grep sprint27` → **0 refs** (already pruned or merged direct-to-`main`).

Documented merged stack branches (safe to prune when local refs reappear):

```bash
git branch -d stack/sprint27/full-sln-gate
git branch -d stack/sprint27/nightly-cmo-corpus
git branch -d stack/sprint27/cmo-loadout-magazine
git branch -d stack/sprint27/import-golden-hygiene
git branch -d stack/sprint27/adr009-validators-bda
git branch -d stack/sprint27/addressables-app6-atlas
git branch -d stack/sprint27/platform-viewer-panel
git branch -d stack/sprint27/platform-viewer-harness
git branch -d stack/sprint27/presentation-evidence
git branch -d stack/sprint27/browse-projection-enrich
git branch -d stack/sprint27/platform-corpus-slice
git branch -d stack/sprint27/closeout-gitnexus
```

(Additional `stack/sprint27/*` refs may exist on other clones after S27 graphite work; prune only after confirming merged to `main`.)