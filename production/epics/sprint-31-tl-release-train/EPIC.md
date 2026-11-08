# Epic: Sprint 31 — TL Release Train

> **Status:** Ready  
> **Sprint:** 31  
> **Dates:** 2026-10-30 → 2026-11-12  
> **Trunk:** `main` @ `3406bc4` (Sprint 30 complete; 956/956; QA APPROVED)  
> **Layer:** Content / Data  
> **GDD:** `design/gdd/` (catalog population); Req 06 continuity

## Goal

Close the **TL release-train loop** deferred from Sprint 30 Phase 4: resolve **`dbRef` / `snapshotId`** from scenario **`tlBranch`** + release train metadata at **package load** (S31-03). Bind at authoring/load — **not** mid-tick. **No** physical TL SQLite forks or `TlBranchDatabaseResolver` runtime behavior.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-006 | Accepted | Database branching release train; snapshot resolution at load |
| ADR-011 | Accepted | Write-gate governance; export manifest metadata; extend-only `CatalogWriteGate` |

## Graphite Stack (merge order)

```
main
 └── stack/sprint31/full-sln-gate              (S31-01 — shared day-1)
      └── stack/sprint31/tl-release-train-load (S31-03) — SPRINT GATE
```

**Dependencies:** S31-03 blocked on S31-01 green baseline and S30-03 Phase 4 binding.

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 03 | [TL release-train snapshot resolution at load](story-031-03-tl-release-train-load.md) | S31-03 | Integration | must-have | 2d | Complete |

Note: **S31-01** day-1 baseline lives in `sprint-31-closeout-devops` epic (shared gate).

## Sprint Gate (S31-03)

**Sprint fails** if S31-03 does not resolve TL-tagged snapshots at load without physical branch databases or mid-tick DB switching.

## GitNexus Mandatory Rules

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `ICatalogReader`, `ScenarioValidationEngine`, export manifest types, `PlatformWorkbookWriteBridge`
- **No physical TL forks:** `rg TlBranchDatabase|BranchDatabase` → zero production bindings
- **Bind at load only:** scenario `tlBranch` + release train resolves snapshot at load — no mid-tick branch switch

## Producer Constraints (Inherited from S29–S30)

1. **TL Phase 5 physical forks** — post-MVP; S31 resolves branch-tagged snapshots on single `main` catalog
2. **Nightly corpus** — full sensor corpus stays off-CI; tier export uses curated fixtures in CI
3. **GHA billing** — permanent local-gate advisory; Buildkite merge authority
4. **ADR-011 Excel-primary** — all writes via workbook + write gate

## Definition of Done

- [x] S31-03 complete (**sprint gate**)
- [ ] `dbRef` / `snapshotId` resolved from `tlBranch` + release train at package load
- [ ] Mismatch surfaces via `scenario_validate` and `ScenarioValidationEngine`
- [ ] Zero runtime `TlBranchDatabase` / `BranchDatabase` bindings
- [ ] `dotnet test` WriteGate|Snapshot|TlTier|Scenario filters green
- [ ] Evidence: `production/agentic/sprint-31-tl-release-train-*.md`
- [ ] Tracker row 06 updated

## References

- S30-03 foundation: `production/epics/sprint-30-tl-export-phase34/story-030-03-tl-phase4-binding.md`
- S29-02 foundation: `production/agentic/sprint-29-tl-export-phase12-2026-06-18.md`
- S28-11 spike: `production/agentic/sprint-28-tl-branching-spike-2026-06-18.md`
- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md`
- Parallel kickoff: `production/agentic/sprint-31-parallel-kickoff-2026-06-18.md` *(create at kickoff)*
- Track plan: `production/agentic/sprint-31-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md`
- Skill: `database-branching-release-train`