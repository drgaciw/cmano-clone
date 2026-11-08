# Sprint 29 GitNexus Re-index + Closeout Hygiene — 2026-06-18

**Repo:** `/home/username01/cmano-clone/cmano-clone`  
**Branch:** `main`  
**Trunk:** `main` @ S29 closeout (13/13 stories complete)  
**Command:** `npx gitnexus analyze . --force`  
**Duration:** 21.8s  

## Graph stats

| Metric | Value |
|--------|-------|
| Nodes | 12,802 |
| Edges | 26,402 |
| Clusters | 314 |
| Flows | 300 |

**Prior baseline (Sprint 28 closeout @ `1d93e86`):** 11,851 nodes / 24,408 edges — **+699 nodes / +1,331 edges** after TL export Phase 1–2, nightly approve workflow, Unity import UI, Baltic combat enable, Phase B sim consumption, and C2 Begin Execution.

**S29 day-1 baseline (@ S29-01):** 12,240 nodes / 24,819 edges — **+310 nodes / +920 edges** from must-have + should-have stack landing.

## Status

```
Repository: cmano-clone
Indexed: 2026-06-18
Status: up-to-date (S29 closeout stack)
Indexed commit: 465cb65
```

## Sprint touch-set blast radius (manual note)

| Symbol | Risk | Notes |
|--------|------|-------|
| `CatalogWriteGate` | **CRITICAL** | Extend-only; S29-03 nightly approve + S29-02 `RecordApprovedImport` branch arg |
| `PlatformWorkbookWriteBridge` | **HIGH** | S29-04 Unity import UI propose→acknowledge→approve; no bypass |
| `PlatformImportPanelHost` | **MEDIUM** | S29-04 Phase E staging review UX |
| `CatalogExportManifest` | **MEDIUM** | S29-02 `tlTier` metadata on export drops; no runtime binding |
| `DbSnapshotStore` | **MEDIUM** | Migration `010_tl_snapshot_branch.sql`; `RecordRelease` nightly path |
| `PhaseBCatalogConsumer` | **MEDIUM** | S29-06 mobility/signatures/EMCON catalog → sim readiness/detection |
| `BalticCombatDomainsPolicy` | **MEDIUM** | S29-05 isolated golden `combatDomainsEnabled=true`; production Baltic unchanged |
| `C2TopBarPanelHost` | **LOW** | S29-08 Begin Execution UX; score freeze while Planning |
| `DelegationBridge` | **N/A** | **ZERO** file touches vs trunk |

## Verification gates (S29 closeout)

| Gate | Result |
|------|--------|
| `ReplayGoldenSuiteTests` | **6/6 PASS** |
| Full solution `dotnet test ProjectAegis.sln` | **847/847 PASS** |
| Replay golden drift | **NONE** (Baltic production fixtures) |
| `DelegationBridge.cs` ZERO touch | **PASS** |
| ADR-009 smoke fixture (separate hash) | **PASS** — `combat-domains-smoke` world hash `17144800277401907079` (S27-16; unchanged) |
| Baltic combat-domains golden (isolated) | **PASS** — `baltic-patrol-combat-domains` world hash `17144800277401907079` (S29-05) |

### Per-project test counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 166 |
| ProjectAegis.Delegation.Tests | 208 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 169 |
| ProjectAegis.MissionEditor.Cli.Tests | 26 |
| ProjectAegis.Data.Tests | 273 |
| ProjectAegis.Data.Excel.Tests | 5 |

## Tracker updates (S29 closeout)

### Row 06 (Req 06 Database Intelligence)

**Updated:** TL export Phase 1–2 — migration `010_tl_snapshot_branch.sql`, `CatalogTlTier` + `CatalogExportManifest.tlTier` on export drops (metadata only; zero `TlBranch`/`BranchDatabase` runtime); **S29-03** nightly corpus approve — `tools/cmo-nightly-approve.sh` off-CI companion, `RecordRelease` + pinned snapshot hash on curated platform slices; **S29-06** Phase B catalog rows consumed in sim readiness/detection paths (mobility/signatures/EMCON via `ICatalogReader`; no SQLite in Sim). Next: TL Phase 4 runtime binding; full `ship.md` nightly approve at scale; balance drift in catalog pipeline (S29-10 deferred).

### Row 18 (Req 18 Combat Domains)

**Updated:** ADR-009 Phase 3 bounded — **S29-05** `baltic-patrol-combat-domains` isolated golden with `combatDomainsEnabled=true`; world-state hash `17144800277401907079` pinned; domain validators active on flag-on path; production Baltic `combatDomainsEnabled=false` unchanged; `combat-domains-smoke` separate pin unchanged. **S29-06** Phase B metadata feeds readiness/engage gates. Next: mine/land runtime; flip flag on production Baltic when program approves; hot-tick damage apply (S29-09 deferred).

### Row 21 (Req 21 Platform Editor)

**Updated:** ADR-011 Phase E Unity import UI — **S29-04** `PlatformImportPanelHost` + `PlatformImportStagingProjection`; in-engine workbook pick → `PlatformWorkbookWriteBridge` propose → diff preview → acknowledge → approve; headless round-trip tests on Baltic fixture; CLI `platform_import_xlsx` + `catalog_write_approve` authority preserved. Evidence S29-04, S29-07 (doctrine visual sign-off). Next: damage sim full hot-tick apply; CMO full corpus nightly approve at scale.

## Branch hygiene — `stack/sprint28/*`

**Verify @ S29-13 closeout:** `git branch -a | rg stack/sprint28` → **0 refs** (already pruned or merged direct-to-`main`).

Documented merged stack branches (safe to prune when local refs reappear):

```bash
git branch -d stack/sprint28/full-sln-gate
git branch -d stack/sprint28/corpus-v2
git branch -d stack/sprint28/corpus-golden
git branch -d stack/sprint28/excel-write
git branch -d stack/sprint28/platform-write-ui
git branch -d stack/sprint28/combat-phase2
git branch -d stack/sprint28/live-magazines
git branch -d stack/sprint28/tl-branch-spike
git branch -d stack/sprint28/closeout
```

(Additional `stack/sprint28/*` refs may exist on other clones after S28 graphite work; prune only after confirming merged to `main`.)