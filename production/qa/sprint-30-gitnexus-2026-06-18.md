# Sprint 30 GitNexus Re-index + Closeout Hygiene — 2026-06-18

**Repo:** `/home/username01/cmano-clone/cmano-clone`  
**Branch:** `main` @ `3406bc4`  
**Trunk:** `main` @ S30 closeout (13/13 stories complete)  
**Command:** `npx gitnexus analyze . --force`  
**Duration:** 36.7s  

## Graph stats

| Metric | Value |
|--------|-------|
| Nodes | 13,461 |
| Edges | 27,655 |
| Clusters | 325 |
| Flows | 300 |

**Prior baseline (Sprint 29 closeout):** 12,802 nodes / 26,402 edges — **+659 nodes / +1,253 edges** after TL Phase 3–4 binding, full `ship.md` approve, land validator, hot-tick hits, Baltic flag flip, datalink lag, and CMO entity slices.

**S30 day-1 baseline (@ S30-01):** 12,852 nodes / 26,452 edges — **+609 nodes / +1,203 edges** from must-have + should-have + nice-to-have stack landing.

## Status

```
Repository: cmano-clone
Indexed: 2026-06-18
Status: up-to-date (S30 closeout stack)
Indexed commit: 3406bc4
```

## Sprint touch-set blast radius (manual note)

| Symbol | Risk | Notes |
|--------|------|-------|
| `CatalogWriteGate` | **CRITICAL** | Extend-only; S30-04 full ship.md approve + S30-11 entity slices |
| `CatalogTlExportFilter` | **HIGH** | S30-02 Phase 3 per-tier export; `--tl-tier` CLI |
| `TlBranchRule` | **HIGH** | S30-03 Phase 4 `metadata.tlBranch` load-time validation |
| `LandAspectDomainValidator` | **MEDIUM** | S30-05 `LAND_ASPECT_BLOCK` bounded validator |
| `CatalogDamageHotTickApplier` | **MEDIUM** | S30-08 Hit → `damageLevel` 0–3 → HP ledger |
| `DatalinkSidePictureMerger` | **MEDIUM** | S30-10 `shareLagTicks` lag queue |
| `C2PlanningChrome` | **LOW** | S30-07 Begin Execution planning chrome |
| `DelegationBridge` | **N/A** | **ZERO** file touches vs trunk |

## Verification gates (S30 closeout)

| Gate | Result |
|------|--------|
| `ReplayGoldenSuiteTests` | **6/6 PASS** |
| Full solution `dotnet test ProjectAegis.sln` | **956/956 PASS** |
| Replay golden drift | **NONE** (Baltic production + smoke fixtures) |
| `DelegationBridge.cs` ZERO touch | **PASS** |
| ADR-009 smoke fixture (separate hash) | **PASS** — `combat-domains-smoke` world hash `17144800277401907079` (S27-16; unchanged) |
| Production Baltic combat-domains | **PASS** — S30-09 `combatDomainsEnabled=true`; world hash **unchanged** `17144800277401907079` |

### Per-project test counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 213 |
| ProjectAegis.Delegation.Tests | 213 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 185 |
| ProjectAegis.MissionEditor.Cli.Tests | 30 |
| ProjectAegis.Data.Tests | 310 |
| ProjectAegis.Data.Excel.Tests | 5 |

## Tracker updates (S30 closeout)

### Row 06 (Req 06 Database Intelligence)

**Updated:** **S30-02** TL export Phase 3 — `CatalogTlExportFilter` per-tier filtered `ICatalogReader` export + `--tl-tier` CLI; **S30-03** TL export Phase 4 — `metadata.tlBranch` + `TlBranchRule` load-time validation (sprint gate PASS); **S30-04** full `ship.md` nightly approve at scale — 4844 records off-CI, pinned hash `nightly-ship-s30-04-20260618`; **S30-11** CMO entity nightly slices — `aircraft-slice-100.md`, `submarine-slice-100.md`, `facility-slice-100.md` + `CmoMarkdownEntitySliceImportTests`. Next: full 7208-record sensor in CI; runtime TL fork selection beyond export metadata; balance drift advisory in nightly approve path.

### Row 18 (Req 18 Combat Domains)

**Updated:** ADR-009 Phase 4 bounded — **S30-05** `LandAspectDomainValidator` + `LAND_ASPECT_BLOCK`; **S30-08** hot-tick Hit → `damageLevel` 0–3 → HP ledger (`CatalogDamageHotTickApplier`); **S30-09** production `baltic-patrol.policy.json` `combatDomainsEnabled=true` with world hash `17144800277401907079` unchanged; **S30-10** `datalink.shareLagTicks` on `DatalinkSidePictureMerger` + `baltic-patrol-datalink-lag` fixture. Next: mine runtime; full facility combat beyond projection stub; BDA hot path beyond bounded slice.

### Row 21 (Req 21 Platform Editor)

**Updated:** ADR-011 Phase E + C2 planning — **S30-06** Editor presentation evidence batch (`*-s30-*.png` protocol placeholders; clears S29-04/07/08 lean conditions); **S30-07** Begin Execution planning chrome (`C2PlanningChromeTests` 7 tests; score freeze while Planning). Next: live Editor screenshots (advisory); damage workbook round-trip in Unity; CMO full corpus approve automation in CI (off-CI only today).

## Branch hygiene — `stack/sprint29/*`

**Verify @ S30-13 closeout:** `git branch -a | rg stack/sprint29` → **0 refs** (already pruned or merged direct-to-`main`).

Documented merged stack branches (safe to prune when local refs reappear):

```bash
git branch -d stack/sprint29/tl-export
git branch -d stack/sprint29/corpus-approve
git branch -d stack/sprint29/platform-import-ui
git branch -d stack/sprint29/c2-loop
git branch -d stack/sprint29/combat-phase3
git branch -d stack/sprint29/closeout
```

(Additional `stack/sprint29/*` refs may exist on other clones after S29 graphite work; prune only after confirming merged to `main`.)