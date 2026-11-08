# Epic: Sprint 29 — Corpus Approve Workflow

> **Status:** Ready  
> **Sprint:** 29  
> **Dates:** 2026-10-02 → 2026-10-15  
> **Trunk:** `main` @ `1d93e86` (801/801; ReplayGolden 6/6)  
> **Layer:** Content / Data  
> **GDD:** `design/gdd/` (catalog population); Req 06 continuity

## Goal

Close the **propose→approve gap** from S28-02: curator `ApproveBatch` path for platform v2 nightly propose runs; `RecordRelease` + snapshot hash — **off-CI only**. Optionally surface **balance drift advisory** on import/approve diffs (S29-10).

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-006 | Accepted | Engine-free `ProjectAegis.Data` boundary |
| ADR-011 | Accepted | Write-gate propose→approve; nightly curator workflow |

## Graphite Stack (merge order)

```
main
 └── stack/sprint29/full-sln-gate              (S29-01 — shared day-1)
      └── stack/sprint29/corpus-approve        (S29-03) — SPRINT GATE
           └── stack/sprint29/balance-drift-pipeline (S29-10 — nice)
```

**Sprint fails** if S29-03 approve workflow does not land through `CatalogWriteGate` with pinned snapshot evidence.

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 03 | [Nightly corpus approve workflow](story-029-03-nightly-approve.md) | S29-03 | Integration | must-have | 2d | Ready |
| 10 | [Balance drift in catalog pipeline](story-029-10-balance-drift-pipeline.md) | S29-10 | Integration | nice-to-have | 0.5d | Not Started |

Note: **S29-01** day-1 baseline lives in `sprint-29-closeout-devops` epic (shared gate).

## GitNexus Mandatory Rules

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `CmoMarkdownImporter`, `ICatalogReader`, `PlatformWorkbookWriteBridge`
- **Nightly only:** full sensor corpus (7208) stays off-CI; curated fixtures + `--max-records` in CI

## Producer Constraints (Inherited from S28)

1. **Nightly corpus** — approve workflow off-CI; CI keeps curated slices
2. **GHA billing** — permanent local-gate advisory; Buildkite merge authority
3. **Chunk 500/batch** — propose-only + quarantine JSON; commits via approve path only

## Definition of Done

- [ ] S29-03 complete (must-have)
- [ ] Nightly job → propose → approve evidence doc with pinned snapshot hash
- [ ] `RecordRelease` + `ApproveBatch` path verified; WriteGate regression PASS
- [ ] `dotnet test` CmoMarkdown|WriteGate|Platform|Snapshot filters green on curated fixtures
- [ ] ReplayGolden 6/6 unchanged
- [ ] Evidence: `production/qa/sprint-29-nightly-approve-*.md`
- [ ] Tracker row 06 updated

## References

- S28-02 pattern: `production/epics/sprint-28-cmo-corpus-v2/story-028-02-nightly-platform-corpus.md`
- S28 evidence: `production/qa/sprint-28-nightly-cmo-import-2026-06-18.md`
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md`
- Parallel kickoff: `production/agentic/sprint-29-parallel-kickoff-2026-06-18.md` *(create at kickoff)*
- Track plan: `production/agentic/sprint-29-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*