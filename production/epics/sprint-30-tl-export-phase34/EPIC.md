# Epic: Sprint 30 — TL Export Phase 3–4

> **Status:** Ready  
> **Sprint:** 30  
> **Dates:** 2026-10-16 → 2026-10-29  
> **Trunk:** `main` @ `e447159` (Sprint 29 complete; 878/878; QA APPROVED)  
> **Layer:** Content / Data  
> **GDD:** `design/gdd/` (catalog population); Req 06 continuity

## Goal

Close the **TL release-train loop** deferred from Sprint 29: **Phase 3** per-tier filtered `ICatalogReader` export (read-only; `platform_export_xlsx` / JSON drops honor `tlTier` filter) and **Phase 4** scenario package `tlBranch` field with load-time validation via `ScenarioValidationEngine` — bind at authoring, **not** mid-tick. **No** physical TL SQLite forks or `TlBranchDatabaseResolver` runtime behavior.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-006 | Accepted | Database branching release train; tier export + scenario binding sequencing |
| ADR-011 | Accepted | Write-gate governance; export manifest metadata; extend-only `CatalogWriteGate` |

## Graphite Stack (merge order)

```
main
 └── stack/sprint30/full-sln-gate              (S30-01 — shared day-1)
      └── stack/sprint30/tl-phase3-export     (S30-02) — per-tier export filters
           └── stack/sprint30/tl-phase4-binding (S30-03) — SPRINT GATE
```

**Dependencies:** S30-02 blocked on S30-01 green baseline. S30-03 blocked on S30-02.

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 02 | [TL export Phase 3](story-030-02-tl-phase3-export.md) | S30-02 | Integration | must-have | 2d | Ready |
| 03 | [TL export Phase 4 binding](story-030-03-tl-phase4-binding.md) | S30-03 | Integration | must-have | 2.5d | Ready |

Note: **S30-01** day-1 baseline lives in `sprint-30-closeout-devops` epic (shared gate).

## Sprint Gate (S30-03)

**Sprint fails** if S30-03 does not bind `tlBranch` at scenario load through validated package metadata with zero `TlBranchDatabaseResolver` mid-tick behavior.

## GitNexus Mandatory Rules

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `ICatalogReader`, `ScenarioValidationEngine`, export manifest types, `PlatformWorkbookWriteBridge`
- **No physical TL forks:** `rg TlBranchDatabase|BranchDatabase` → zero production bindings
- **Bind at load only:** scenario `tlBranch` validated at authoring/load — no mid-tick branch switch

## Producer Constraints (Inherited from S29)

1. **TL Phase 5 physical forks** — post-MVP; S30 binds scenarios to branch-tagged snapshots on single `main` catalog
2. **Nightly corpus** — full sensor corpus stays off-CI; tier export uses curated fixtures in CI
3. **GHA billing** — permanent local-gate advisory; Buildkite merge authority
4. **ADR-011 Excel-primary** — all writes via workbook + write gate

## Definition of Done

- [ ] S30-02 complete (must-have)
- [ ] S30-03 complete (**sprint gate**)
- [ ] Per-tier filtered export tests PASS; deterministic sort keys locked
- [ ] Scenario `tlBranch` validated at load; CLI `scenario_validate` surfaces findings
- [ ] Zero runtime `TlBranchDatabase` / `BranchDatabase` bindings
- [ ] `dotnet test` WriteGate|Snapshot|TlTier|Scenario filters green
- [ ] Evidence: `production/agentic/sprint-30-tl-phase3-*.md`, `production/agentic/sprint-30-tl-phase4-*.md`
- [ ] Tracker row 06 updated

## References

- S29-02 foundation: `production/agentic/sprint-29-tl-export-phase12-2026-06-18.md`
- S28-11 spike: `production/agentic/sprint-28-tl-branching-spike-2026-06-18.md`
- Kickoff: `production/sprints/sprint-30-tl-bind-corpus-scale.md`
- Parallel kickoff: `production/agentic/sprint-30-parallel-kickoff-2026-06-18.md`
- Track plan: `production/agentic/sprint-30-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md`
- Skill: `database-branching-release-train`