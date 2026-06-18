# Epic: Sprint 23 — Platform Phase B I/O + Doctrine Polish

> **Status:** Ready  
> **Sprint:** 23  
> **Dates:** 2026-07-08 → 2026-07-22  
> **Trunk:** `main` @ `7253381`  
> **Predecessor:** Sprint 22 (complete, pushed 2026-06-17)

## Goal

Close Sprint 22 deferred quality gates by wiring **ClosedXML binary `.xlsx` round-trip I/O** (Req 21), establishing a green **full-solution `ProjectAegis.sln` test baseline**, and delivering **Unity Editor visual sign-off** for the Doctrine Inheritance Panel (Req 13) toward MVP polish.

## Capacity

| Metric | Value |
|--------|-------|
| Total days | 10 |
| Buffer (20%) | 2 days |
| Effective dev-days | 8 |

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-010 | Accepted | Headless-first UI; ZERO touch `DelegationBridge` (S23-03) |
| ADR-011 | Accepted | ClosedXML `.xlsx` round-trip; Phase B sheet scope (S23-01, S23-05) |
| ADR-006 | Accepted | Engine-free `ProjectAegis.Data` boundary |

## Graphite Stack (merge order)

```
main @ 7253381
 ├── stack/sprint23/full-sln-gate          (S23-02 — day 1)
 ├── stack/sprint23/closedxml-xlsx-io      (S23-01)
 ├── stack/sprint23/doctrine-editor-visual (S23-03)
 ├── stack/sprint23/approve-batch-multi    (S23-04 — should-have)
 ├── stack/sprint23/phase-b-schema         (S23-05 — should-have)
 └── stack/sprint23/canonical-determinism  (S23-06 — nice-to-have)
```

Closeout on `main`: S23-07 GitNexus re-index + full sln re-run + `/smoke-check sprint`.

## Stories

| # | Story | ID | Type | Priority | Branch | Est. | Status |
|---|-------|-----|------|----------|--------|------|--------|
| 02 | [Full-solution test gate baseline](story-023-02-full-sln-gate.md) | S23-02 | Config | must-have | `stack/sprint23/full-sln-gate` | 1d | Ready |
| 01 | [ClosedXML `.xlsx` adapter](story-023-01-closedxml-xlsx.md) | S23-01 | Integration | must-have | `stack/sprint23/closedxml-xlsx-io` | 2.5d | Ready |
| 03 | [Doctrine Inheritance Panel Editor sign-off](story-023-03-doctrine-editor.md) | S23-03 | UI | must-have | `stack/sprint23/doctrine-editor-visual` | 2.5d | Ready |
| 04 | [ApproveBatch multi-entity commit](story-023-04-approve-batch.md) | S23-04 | Integration | should-have | `stack/sprint23/approve-batch-multi` | 2.5d | Ready |
| 05 | [Phase B schema foundation spike](story-023-05-phase-b-schema.md) | S23-05 | Config | should-have | `stack/sprint23/phase-b-schema` | 2d | Ready |
| 06 | [CanonicalId determinism](story-023-06-canonical-id.md) | S23-06 | Logic | nice-to-have | `stack/sprint23/canonical-determinism` | 1d | Ready |
| 07 | [GitNexus re-index closeout](story-023-07-closeout-gitnexus.md) | S23-07 | Config | nice-to-have | `main` | 0.5d | Ready |

**Critical path:** `S23-02 → S23-01` (data I/O) parallel with `S23-02 → S23-03` (Unity polish).

## GitNexus Mandatory Rules

- **Before ANY symbol edit:** `gitnexus impact` upstream on target symbol
- **CRITICAL extend-only:** `CatalogWriteGate` (S23-04)
- **ZERO touch:** `DelegationBridge` (S23-03)
- **HIGH:** `IPlatformWorkbookIo`, `PlatformWorkbookImporter`, `PlatformWorkbookExporter` (S23-01)
- After edits: `npx gitnexus detect_changes --repo cmano-clone` before commit

## Definition of Done

- [ ] All Must Have stories complete (S23-01, S23-02, S23-03)
- [ ] `dotnet test ProjectAegis.sln` — 0 failures at closeout
- [ ] Scoped story filters green per kickoff Quality Gates
- [ ] QA plan + sign-off (`production/qa/qa-plan-sprint-23-2026-07-08.md`)
- [ ] Tracker rows 13 + 21 updated
- [ ] No S1/S2 bugs in delivered features

## References

- Kickoff: `production/sprints/sprint-23-platform-phase-b-doctrine-polish.md`
- Implementation plan: `docs/superpowers/plans/sprint-23-implementation.md`
- QA plan: `production/qa/qa-plan-sprint-23-2026-07-08.md`
- Parallel kickoff: `production/agentic/sprint-23-parallel-kickoff-2026-06-17.md`
- Sprint status: `production/sprint-status.yaml` (`sprint23_stories`)