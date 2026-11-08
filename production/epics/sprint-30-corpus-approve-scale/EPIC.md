# Epic: Sprint 30 — Corpus Approve at Scale

> **Status:** Ready  
> **Sprint:** 30  
> **Dates:** 2026-10-16 → 2026-10-29  
> **Trunk:** `main` @ `3406bc4` (878/878; ReplayGolden 6/6)  
> **Layer:** Content / Data  
> **GDD:** `design/gdd/` (catalog population); Req 06 continuity

## Goal

Scale the **S29-03 propose→approve path** to the full platform corpus (`ship.md`, 4844 records, chunk 500/batch) off-CI; optionally extend nightly scripts for **CMO entity slices** (`aircraft` / `submarine` / `facility`) with curated golden per domain (S30-11).

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-006 | Accepted | Engine-free `ProjectAegis.Data` boundary |
| ADR-011 | Accepted | Write-gate propose→approve; nightly curator workflow |

## Graphite Stack (merge order)

```
main
 └── stack/sprint30/full-sln-gate              (S30-01 — shared day-1)
      └── stack/sprint30/ship-approve-scale    (S30-04) — SPRINT GATE
           └── stack/sprint30/cmo-entity-slices (S30-11 — nice)
```

**Sprint fails** if S30-04 full `ship.md` approve workflow does not land through `CatalogWriteGate` with pinned snapshot evidence.

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 04 | [Nightly ship.md approve at scale](story-030-04-ship-approve-scale.md) | S30-04 | Integration | must-have | 2d | Ready |
| 11 | [CMO entity nightly slices](story-030-11-cmo-entity-slices.md) | S30-11 | Integration | nice-to-have | 2d | Not Started |

Note: **S30-01** day-1 baseline lives in `sprint-30-closeout-devops` epic (shared gate).

## GitNexus Mandatory Rules

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `CmoMarkdownImporter`, `ICatalogReader`, `PlatformWorkbookWriteBridge`
- **Nightly only:** full sensor corpus (7208) stays off-CI; curated fixtures + `--max-records` in CI

## Producer Constraints (Inherited from S28–S29)

1. **Nightly corpus** — approve workflow off-CI; CI keeps curated slices
2. **GHA billing** — permanent local-gate advisory; Buildkite merge authority
3. **Chunk 500/batch** — propose-only + quarantine JSON; commits via approve path only
4. **Full corpora never in CI** — `ship.md` (4844), `aircraft.md` (7387), `facility.md` (4511), `submarine.md` (732) off-CI only

## Definition of Done

- [ ] S30-04 complete (must-have)
- [ ] Off-CI nightly job → full `ship.md` propose → approve evidence with pinned snapshot hash
- [ ] `RecordRelease` + `ApproveBatch` path verified at scale; WriteGate regression PASS
- [ ] `dotnet test` CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot filters green on curated fixtures
- [ ] ReplayGolden 6/6 unchanged
- [ ] Evidence: `production/qa/sprint-30-nightly-ship-*.md`
- [ ] Tracker row 06 updated
- [ ] S30-11 (nice-to-have): script flags wired; curated golden per entity domain

## References

- S29-03 pattern: `production/epics/sprint-29-corpus-approve/story-029-03-nightly-approve.md`
- S29 evidence: `production/qa/sprint-29-nightly-approve-2026-06-18.md`
- S28-02 pattern: `production/epics/sprint-28-cmo-corpus-v2/story-028-02-nightly-platform-corpus.md`
- Kickoff: `production/sprints/sprint-30-tl-bind-corpus-scale.md`
- Parallel kickoff: `production/agentic/sprint-30-parallel-kickoff-2026-06-18.md`
- Track plan: `production/agentic/sprint-30-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md`