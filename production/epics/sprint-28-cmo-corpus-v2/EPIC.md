# Epic: Sprint 28 — CMO Corpus v2

> **Status:** Ready  
> **Sprint:** 28  
> **Dates:** 2026-09-18 → 2026-10-01  
> **Trunk:** `main` @ `a93b55e` (741/741; ReplayGolden 6/6)  
> **Layer:** Content / Data  
> **GDD:** `design/gdd/` (catalog population); Req 06 continuity

## Goal

Extend the **nightly off-CI CMO pipeline** beyond sensor+weapon v1 to **platform corpus v2 slices**, land **import golden hygiene** for curated platform runs, and optionally spike **TL-gated branch DB workflow** — without wiring full 7208-record `sensor.md` into `dotnet test` CI.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-006 | Accepted | Engine-free `ProjectAegis.Data` boundary |
| ADR-011 | Accepted | Write-gate import; propose-only nightly path |

## Graphite Stack (merge order)

```
main
 └── stack/sprint28/full-sln-gate              (S28-01 — shared day-1)
      └── stack/sprint28/corpus-v2             (S28-02) — CRITICAL nightly
           ├── stack/sprint28/corpus-golden   (S28-03) — sprint gate
           └── stack/sprint28/tl-branch-spike (S28-11 — nice)
```

**Sprint fails** if S28-03 platform corpus round-trip does not land through the write gate.

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 02 | [Nightly CMO corpus v2 — platform slices](story-028-02-nightly-platform-corpus.md) | S28-02 | Integration | must-have | 2d | Ready |
| 03 | [Platform corpus E2E + golden hygiene](story-028-03-corpus-golden-hygiene.md) | S28-03 | Integration | must-have | 1d | Not Started |
| 11 | [TL branching spike (export-only)](story-028-11-tl-branching-spike.md) | S28-11 | Config | nice-to-have | 1d | Not Started |

Note: **S28-01** day-1 baseline lives in `sprint-28-closeout-devops` epic (shared gate).

## GitNexus Mandatory Rules

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `CmoMarkdownImporter`, `ICatalogReader`, `CatalogPlatformBrowseProjection`
- **Nightly only:** full sensor corpus (7208) stays off-CI; curated fixtures + `--max-records` in CI

## Producer Constraints (Inherited from S27)

1. **Nightly corpus** — platform v2 in job; full sensor corpus stays off-CI
2. **GHA billing** — permanent local-gate advisory; Buildkite merge authority
3. **Chunk 500/batch** — propose-only + quarantine JSON; no direct SQLite writes

## Definition of Done

- [ ] S28-02..03 complete (must-have)
- [ ] Nightly job v2: platform slices (producer-approved scope)
- [ ] `dotnet test` CmoMarkdown|WriteGate|Platform filters green on curated fixtures
- [ ] ReplayGolden 6/6 unchanged
- [ ] Evidence: `production/qa/sprint-28-nightly-cmo-import-*.md`
- [ ] Tracker row 06 updated

## References

- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md`
- Parallel kickoff: `production/agentic/sprint-28-parallel-kickoff-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md` *(create before implementation)*
- Track plan: `production/agentic/sprint-28-plan-data-2026-06-18.md` *(create at kickoff)*