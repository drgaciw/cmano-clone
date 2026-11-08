# Epic: Sprint 31 — Corpus Approve Complete

> **Status:** Ready  
> **Sprint:** 31  
> **Dates:** 2026-10-30 → 2026-11-12  
> **Trunk:** `main` @ `3406bc4` (Sprint 30 complete; 956/956; QA APPROVED)  
> **Layer:** Content / Data  
> **GDD:** `design/gdd/` (catalog population); Req 06 continuity

## Goal

Complete the **off-CI corpus approve loop** deferred from Sprint 30: full **`sensor.md`** nightly approve at scale (7208 records, S31-02 sprint gate), optional **balance drift advisory** on nightly approve summary (S31-09), and optional **weapon** / **entity** full-corpus nightly approve (S31-10, S31-11) — all through `CatalogWriteGate.ApproveBatch` with pinned snapshot hashes. CI keeps curated slices only.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-006 | Accepted | Engine-free `ProjectAegis.Data` boundary |
| ADR-011 | Accepted | Write-gate propose→approve; nightly curator workflow |

## Graphite Stack (merge order)

```
main
 └── stack/sprint31/full-sln-gate                 (S31-01 — shared day-1)
      └── stack/sprint31/nightly-sensor-approve   (S31-02) — SPRINT GATE
           ├── stack/sprint31/balance-drift-nightly (S31-09 — nice)
           ├── stack/sprint31/weapon-approve-scale  (S31-10 — nice)
           └── stack/sprint31/entity-approve-scale  (S31-11 — nice)
```

**Sprint fails** if S31-02 full `sensor.md` approve workflow does not land through `CatalogWriteGate` with pinned snapshot evidence.

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 02 | [Nightly sensor.md approve at scale](story-031-02-nightly-sensor-approve.md) | S31-02 | Integration | must-have | 2d | Not Started |
| 09 | [Balance drift advisory on nightly approve](story-031-09-balance-drift-nightly.md) | S31-09 | Integration | nice-to-have | 0.5d | backlog |
| 10 | [Nightly weapon.md approve at scale](story-031-10-weapon-approve-scale.md) | S31-10 | Integration | nice-to-have | 2d | backlog |
| 11 | [Entity corpus nightly approve at scale](story-031-11-entity-approve-scale.md) | S31-11 | Integration | nice-to-have | 2d | backlog |

Note: **S31-01** day-1 baseline lives in `sprint-31-closeout-devops` epic (shared gate).

## GitNexus Mandatory Rules

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `CmoMarkdownImporter`, `ICatalogReader`, `PlatformWorkbookWriteBridge`, `CatalogBalanceDriftPipelineEvaluator`
- **Nightly only:** full sensor corpus (7208), weapon (4403), entity corpora stay off-CI; curated fixtures + `--max-records` in CI

## Producer Constraints (Inherited from S28–S30)

1. **Nightly corpus** — approve workflow off-CI; CI keeps curated slices
2. **GHA billing** — permanent local-gate advisory; Buildkite merge authority
3. **Chunk 500/batch** — propose-only + quarantine JSON; commits via approve path only
4. **Full corpora never in CI** — `sensor.md` (7208), `weapon.md` (4403), `aircraft.md` (7387), `facility.md` (4511), `submarine.md` (732) off-CI only

## Definition of Done

- [ ] S31-02 complete (must-have / sprint gate)
- [ ] Off-CI nightly job → full `sensor.md` propose → approve evidence with pinned snapshot hash
- [ ] `RecordRelease` + `ApproveBatch` path verified at scale; WriteGate regression PASS
- [ ] `dotnet test` CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot filters green on curated fixtures
- [ ] ReplayGolden 6/6 unchanged
- [ ] Evidence: `production/qa/sprint-31-nightly-sensor-*.md`
- [ ] Tracker row 06 updated
- [ ] S31-09 (nice): balance drift advisory in nightly summary when `enableBalanceDrift=true`; default off
- [ ] S31-10/11 (nice): weapon + entity full corpus approve scripts; per-domain evidence

## References

- S30-04 pattern: `production/epics/sprint-30-corpus-approve-scale/story-030-04-ship-approve-scale.md`
- S30-11 pattern: `production/epics/sprint-30-corpus-approve-scale/story-030-11-cmo-entity-slices.md`
- S29-10 pattern: `production/epics/sprint-29-corpus-approve/story-029-10-balance-drift-pipeline.md`
- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md`
- Parallel kickoff: `production/agentic/sprint-31-parallel-kickoff-2026-06-18.md` *(create at kickoff)*
- Track plan: `production/agentic/sprint-31-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md`