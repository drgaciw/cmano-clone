# Sprint 32 GitNexus Re-index + Closeout Hygiene — 2026-06-19

**Repo:** `/home/username01/cmano-clone/cmano-clone`  
**Branch:** `main` @ `d3db76dbca07237200f5e7ad69eb5d4fdcaa118f`  
**Trunk:** `main` @ S32 closeout (13/13 stories complete)  
**Command:** `npx gitnexus analyze . --force`  
**Duration:** 19.8s  

## Graph stats

| Metric | Value |
|--------|-------|
| Nodes | 15,064 |
| Edges | 30,605 |
| Clusters | 337 |
| Flows | 300 |

**S32 day-1 baseline:** 14,424 nodes / 29,218 edges — **+640 nodes / +1,387 edges** after unified release-train manifest, quarantine triage, facility/ECCM/mine/BDA bounded paths, Phase F damage Unity, release diff CLI, C2 sign-off upgrade.

**S31 closeout baseline:** 14,160 nodes / 28,928 edges — **+904 nodes / +1,677 edges** cumulative S32.

## Status

```
Repository: cmano-clone
Indexed: 2026-06-19
Status: up-to-date (S32 closeout stack)
Indexed commit: d3db76db
```

## Sprint touch-set blast radius (manual note)

| Symbol | Risk | Notes |
|--------|------|-------|
| `UnifiedReleaseTrainManifest` | **CRITICAL** | S32-02 consolidated curator drop + `RecordUnifiedRelease` |
| `MountLoadoutQuarantineTriage` | **HIGH** | S32-03 bounded FK repair envelope |
| `FacilityAspectDomainValidator` | **MEDIUM** | S32-04 `FACILITY_ASPECT_BLOCK` |
| `ScenarioDetectionTrial.EccmFactor` | **MEDIUM** | S32-05 bounded ECCM Phase 2 |
| `MineTransitHazardHotTickApplier` | **MEDIUM** | S32-08 scenario `mineHazard` hot-tick |
| `BdaContactLifecycleHotTickApplier` | **MEDIUM** | S32-09 damageLevel ≥ 3 → contact Lost |
| `PlatformCatalogViewerHost` | **LOW** | S32-06 Phase F damage columns |
| `catalog_release_diff` | **LOW** | S32-07 deterministic release diff CLI |

## Stack prune (`stack/sprint31/*`)

| Check | Result |
|-------|--------|
| Local `stack/sprint31/*` refs | **0** (no local branches @ closeout verify) |
| Merged branches (documented) | `full-sln-gate`, `nightly-sensor-approve`, `tl-release-train`, `tl-release-train-load`, `balance-drift-nightly`, `weapon-approve-scale`, `entity-approve-scale`, `combat-phase5`, `presentation-evidence`, `c2-signoff-refresh`, `closeout` |
| Prune action | Documented — merged branches only; no stale refs |

## References

- Closeout smoke: `production/qa/smoke-sprint-32-closeout-2026-06-19.md`
- S32-13 done: `production/agentic/stacks/sprint32/S32-13-DONE.md`
- Day-1 baseline: `production/qa/smoke-sprint-32-baseline-2026-06-18.md`