# S28-11 story-done — TL Branching Spike (Export-Only)

**Story:** `production/epics/sprint-28-cmo-corpus-v2/story-028-11-tl-branching-spike.md`  
**Status:** Complete  
**Date:** 2026-06-18  
**Type:** Doc-only (no runtime code)

## Deliverables

- `production/agentic/sprint-28-tl-branching-spike-2026-06-18.md` — TL-0–TL-5 export-only workflow, CI/nightly separation, rollback/diff model, phased implementation plan

## Verdict: **PROCEED (export-only evaluation)**

Defer physical TL-0–TL-5 forked branch databases and runtime scenario DB branch binding to post-S28 (migration `007+`). Proceed with documented export-only evaluation path using existing snapshot/release-train infrastructure.

## Acceptance criteria traceability

| AC | Evidence | Status |
|----|----------|--------|
| Spike doc with TL-0–TL-5 workflow outline | Spike doc § TL model + per-tier steps | **PASS** |
| Export-only workflow (diffable drops, snapshot hashes) | Spike doc § Export-only workflow + hash contract | **PASS** |
| PROCEED or DEFER verdict with rationale | **PROCEED (export-only); defer production forks** | **PASS** |
| No runtime branch binding in code | `rg TlBranch\|BranchDatabase src/**/*.cs` → zero matches | **PASS** |
| No TL-0–TL-5 production branch databases shipped | Doc-only story; no migrations or resolvers added | **PASS** |
| ZERO touch `DelegationBridge.cs` | No edits under `Delegation.UnityAdapter/Bridge/` | **PASS** |

## Verify

```bash
cd /home/username01/cmano-clone/cmano-clone

# AC-2: no runtime bindings
rg -l "TlBranch|BranchDatabase" src/ --glob "*.cs" || echo "zero matches"
# zero matches

# Spike doc present
ls production/agentic/sprint-28-tl-branching-spike-2026-06-18.md

# DelegationBridge untouched
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

## Blockers (for production TL forks — out of S28-11 scope)

| Blocker | Notes |
|---------|-------|
| `catalog_snapshot.branch` column | Reserved in P0 spec; migration 007 not authored |
| Scenario `tlBranch` package field | Mission editor binding work deferred |
| Per-entity provenance on all types | Hash contract extension needs DBI-6 post-P0 |
| Curator sign-off on multi-DB ops | Export-only eval must run before physical forks |
| Dedicated sprint capacity | Parallel corpus + branching overload risk (S28 cut line) |

## GitNexus symbols

| Symbol | Touch | Notes |
|--------|-------|-------|
| `OsintCatalogMapper` | Reference only | S22-07 `branch:doc-*` metadata pattern cited in spike |
| `DelegationBridge.cs` | **ZERO** | No adapter changes |

## Story verdict

**COMPLETE** — doc-only spike delivered; export-only PROCEED; production TL branch DBs DEFERRED.