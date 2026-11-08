# Epic: Sprint 32 — Release Train Ops

> **Status:** Ready  
> **Sprint:** 32  
> **Dates:** 2026-11-13 → 2026-11-26  
> **Trunk:** `main` @ `3406bc4` (Sprint 31 complete; 1006/1006; QA APPROVED)  
> **Layer:** Content / Data  
> **GDD:** `design/gdd/` (catalog population); Req 06 continuity

## Goal

Operationalize the **S31 corpus-complete drop** into a **unified release-train manifest** (S32-02 — sprint gate), remediate **mount/loadout quarantine** FK gaps (S32-03), and deliver deterministic **release diff CLI** (S32-07). Bind at authoring/load — **not** mid-tick. **No** physical TL SQLite forks.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-006 | Accepted | Database branching release train; snapshot resolution at load |
| ADR-011 | Accepted | Write-gate governance; export manifest metadata; extend-only `CatalogWriteGate` |

## Graphite Stack (merge order)

```
main
 └── stack/sprint32/full-sln-gate              (S32-01 — shared day-1)
      └── stack/sprint32/unified-release-train-manifest (S32-02) — SPRINT GATE
           ├── stack/sprint32/mount-loadout-quarantine (S32-03)
           └── stack/sprint32/release-diff-cli       (S32-07)
```

**Dependencies:** S32-02 blocked on S32-01 green baseline. S32-03 blocked on S32-02. S32-07 blocked on S32-02.

Note: **S32-01** day-1 baseline lives in `sprint-32-closeout-devops` epic (shared gate).

## Sprint Gate (S32-02)

**Sprint fails** if S32-02 does not produce an operational unified release-train manifest with `scenario_validate` resolving manifest-backed `dbRef`.

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 02 | [Unified release-train manifest](story-032-02-unified-release-train-manifest.md) | S32-02 | Integration | must-have | 2d | Not Started |
| 03 | [Mount/loadout quarantine triage](story-032-03-mount-loadout-quarantine.md) | S32-03 | Integration | must-have | 2.5d | Not Started |
| 07 | [Release diff report CLI](story-032-07-release-diff-cli.md) | S32-07 | Integration | should-have | 1.5d | Not Started |

## GitNexus Mandatory Rules

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `ICatalogReader`, `ScenarioValidationEngine`, export manifest types, `RecordRelease`
- **No physical TL forks:** `rg TlBranchDatabase|BranchDatabase` → zero production bindings
- **Bind at load only:** manifest resolves snapshot at load — no mid-tick branch switch
- **WriteGate only** — no live-table mutation on diff/report paths

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| — | — | S32-03 quarantine triage |
| — | — | S32-07 release diff CLI |

**Minimum shippable beyond must-have:** **S32-07** release diff CLI + **S32-13** closeout.

## Definition of Done

- [ ] S32-02 complete (**sprint gate**); consolidated manifest published via `RecordRelease`
- [ ] `scenario_validate` resolves manifest-backed `dbRef`
- [ ] S32-03 quarantine count reduction evidenced; bounded FK repair rules documented
- [ ] S32-07 `catalog_release_diff` verb; empty-diff golden on re-import
- [ ] `dotnet test` WriteGate|Snapshot|TlTier|Scenario|TlRelease|Quarantine filters green
- [ ] Evidence: `production/agentic/sprint-32-release-train-manifest-*.md`
- [ ] Tracker row 06 updated

## References

- S31-03 foundation: `production/epics/sprint-31-tl-release-train/story-031-03-tl-release-train-load.md`
- S30-03 foundation: `production/epics/sprint-30-tl-export-phase34/story-030-03-tl-phase4-binding.md`
- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md`
- Parallel kickoff: `production/agentic/sprint-32-parallel-kickoff-2026-06-18.md`
- Track plan: `production/agentic/sprint-32-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*
- Skill: `database-branching-release-train`