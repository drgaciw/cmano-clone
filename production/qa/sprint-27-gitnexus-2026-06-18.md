# Sprint 27 GitNexus Re-index + Closeout Hygiene — 2026-06-18

**Repo:** `/home/username01/cmano-clone/cmano-clone`  
**Branch:** `main`  
**Trunk:** `main` @ S27 closeout (S27-01..16)  
**Command:** `npx gitnexus analyze . --force`  
**Duration:** 32.6s  

## Graph stats

| Metric | Value |
|--------|-------|
| Nodes | 11,192 |
| Edges | 22,977 |
| Clusters | 280 |
| Flows | 300 |

**Prior baseline (Sprint 26 closeout @ `ab30d35`):** 10,656 nodes / 22,048 edges — **+536 nodes / +929 edges** after S27 corpus pipeline, ADR-009 validators, and Phase C viewer stack.

## Status

```
Repository: cmano-clone
Indexed: 2026-06-18
Status: up-to-date (S27 closeout stack)
Indexed commit: 2e4fb07
```

## Sprint touch-set blast radius (manual note)

| Symbol | Risk | Notes |
|--------|------|-------|
| `CatalogWriteGate` | **CRITICAL** | Extend-only; S27-03..04 loadout/magazine + S27-14 ship-slice import |
| `CmoMarkdownImporter` | **HIGH** | Nightly job + mount/loadout/magazine + `ship-slice-100.md` |
| `DomainValidatorRegistry` | **MEDIUM** | S27-05 bounded validators; `combatDomainsEnabled=false` on Baltic |
| `CatalogPlatformBrowseProjection` | **LOW** | S27-08..09, S27-15 read-only browse + detail pane |
| `PlatformCatalogViewerHost` | **LOW** | UXML/USS viewer; no write path |
| `DelegationBridge` | **N/A** | **ZERO** file touches vs trunk |

## Verification gates (S27 closeout)

| Gate | Result |
|------|--------|
| `ReplayGoldenSuiteTests` | **6/6 PASS** |
| Full solution `dotnet test ProjectAegis.sln` | **741/741 PASS** |
| Replay golden drift | **NONE** (Baltic production fixtures) |
| `DelegationBridge.cs` ZERO touch | **PASS** |
| ADR-009 flag-on smoke (separate hash) | **PASS** — `combat-domains-smoke` world hash `17144800277401907079` |

### Per-project test counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 123 |
| ProjectAegis.Delegation.Tests | 195 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 136 |
| ProjectAegis.MissionEditor.Cli.Tests | 25 |
| ProjectAegis.Data.Tests | 257 |
| ProjectAegis.Data.Excel.Tests | 5 |

## Tracker updates (S27 closeout)

### Row 06 (Req 06 Database Intelligence)

**Updated:** Nightly CMO corpus job (sensor+weapon v1 off-CI); mount→loadout→magazine import through `CatalogWriteGate`; browse enrichment; curated `ship-slice-100.md` platform slice (S27-02..04, S27-09, S27-14). Next: nightly platform corpus v2; TL branching.

### Row 18 (Req 18 Combat Domains)

**Updated:** ADR-009 bounded validators (`DeterministicDamageApplyBatch`, `AirAspectDomainValidator`); `combat-domains-smoke.policy.json` test-only fixture with separate pinned hash; Baltic `combatDomainsEnabled=false` unchanged (S27-05, S27-16). Next: mine/land runtime; facility damage.

### Row 21 (Req 21 Platform Editor)

**Updated:** ADR-011 Phase C in-engine viewer — browse panel, filter projection, PlayMode smoke harness, detail pane for mobility/damage fields (S27-08, S27-09, S27-11, S27-15). Next: in-engine Excel write path.

## Branch hygiene — `stack/sprint26/*`

**Verify @ S27-13 closeout:** `git branch -a | grep sprint26` → **0 refs** (already pruned or merged direct-to-`main`).

Documented merged stack branches (safe to prune when local refs reappear):

```bash
git branch -d stack/sprint26/presentation-closeout
```

(Additional `stack/sprint26/*` refs may exist on other clones after S26 graphite work; prune only after confirming merged to `main`.)