# Sprint 31 GitNexus Re-index + Closeout Hygiene — 2026-06-18

**Repo:** `/home/username01/cmano-clone/cmano-clone`  
**Branch:** `main` @ `3406bc4`  
**Trunk:** `main` @ S31 closeout (12/13 stories complete; S31-12 backlog)  
**Command:** `npx gitnexus analyze . --force`  
**Duration:** 22.1s  

## Graph stats

| Metric | Value |
|--------|-------|
| Nodes | 14,160 |
| Edges | 28,928 |
| Clusters | 337 |
| Flows | 300 |

**S30 closeout baseline:** 13,496 nodes / 27,690 edges — **+664 nodes / +1,238 edges** after sensor nightly approve, TL release-train resolution, mine/facility/BDA bounded paths, balance drift advisory, weapon + entity nightly approve.

## Status

```
Repository: cmano-clone
Indexed: 2026-06-18
Status: up-to-date (S31 closeout stack)
Indexed commit: 3406bc4
```

## Sprint touch-set blast radius (manual note)

| Symbol | Risk | Notes |
|--------|------|-------|
| `CatalogWriteGate` | **CRITICAL** | Extend-only; S31-02 sensor 7208 + S31-10 weapon + S31-11 entity nightly |
| `TlReleaseTrainRule` | **HIGH** | S31-03 sprint gate — snapshot resolution at load |
| `MineAspectDomainValidator` | **MEDIUM** | S31-04 `MINE_ASPECT_BLOCK` |
| `CatalogDamageHotTickApplier` | **MEDIUM** | S31-05 facility hot-tick HP apply |
| `OrderLogBdaProjection` | **MEDIUM** | S31-06 damageLevel → contact status |
| `NightlyApproveBalanceDriftSummary` | **LOW** | S31-09 advisory (default off) |

## Stack prune (`stack/sprint30/*`)

| Check | Result |
|-------|--------|
| Local `stack/sprint30/*` refs | **0** (no local branches @ closeout verify) |
| Prune action | Documented — merged branches only; no stale refs |

## References

- Closeout smoke: `production/qa/smoke-sprint-31-closeout-2026-06-18.md`
- S31-13 done: `production/agentic/stacks/sprint31/S31-13-DONE.md`