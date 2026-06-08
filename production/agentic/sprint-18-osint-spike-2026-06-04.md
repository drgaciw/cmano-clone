# Sprint 18 — OSINT / Dynamic Systems Agent spike (S18-03)

**Date:** 2026-06-04  
**Requirement:** `Game-Requirements/requirements/05-Dynamic-Systems-Agent.md` (Locked)  
**Branch:** `stack/sprint18/osint-spike`  
**GitNexus:** `CatalogDiffProposalAgent` — **LOW** (1 upstream: orchestrator)

## Verdict: **PROCEED** (MVP slice S18-OSINT-1)

Implement a **headless discovery → proposal gate** in `ProjectAegis.Data` that feeds the existing write-gate / diff pipeline. Defer schedulers, MCP search, and Unity review UI to Sprint 19+.

## What exists today

| Piece | Status |
|-------|--------|
| Write gate + staging | `CatalogWriteGate`, `IWriteGate` |
| Diff agent | `CatalogDiffProposalAgent` lists pending batches |
| Orchestrator | `DatabaseIntelligenceOrchestrator` (no retrieval agent yet) |
| Doc 05 thresholds | Confidence **≥ 0.65** → staging proposal; below → log-only |

## MVP slice delivered this spike

| Artifact | Purpose |
|----------|---------|
| `OsintDiscoveryRecord` | Normalized digest/on-demand hit (source URL, snippet, score) |
| `OsintProposalGate` | Applies DSA-2.1 threshold; routes to proposal vs log-only |
| `OsintProposalGateTests` | Contract tests for threshold + deterministic ordering |

## Out of scope (Sprint 19+)

- Daily digest worker / cron
- RSS/arXiv/patent connectors
- `enableRealtimeSocialStream` (must stay `false` per DSA-1.3)
- Unity staging review UI
- Live DB writes without `CatalogWriteGate.ApproveBatch`

## Integration path (next stack)

```
OsintDigestRunner (future)
  → OsintProposalGate.Partition(discoveries)
  → CatalogDiffProposalAgent / CatalogWriteGate.ProposeSensorBatch (human approve)
  → DatabaseIntelligenceOrchestrator validation agents
```

## Risks

| Risk | Mitigation |
|------|------------|
| Bypass write gate | All catalog commits stay on `IWriteGate`; OSINT only produces `CatalogSensorBinding` proposals |
| Non-determinism | No wall-clock in gate; stable sort by `(sourceUrl, canonicalId)` |
| CRITICAL `DelegationBridge` | **No edits** in OSINT slice |

## Acceptance for spike close

- [x] Spike doc with PROCEED/DEFER — **PROCEED**
- [x] Contract types + tests in `ProjectAegis.Data`
- [x] End-to-end digest job — `OsintDigestRunner` + `OsintDigestRunnerTests` (S19-05, 2026-06-08)