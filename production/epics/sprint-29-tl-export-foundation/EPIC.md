# Epic: Sprint 29 — TL Export Foundation

> **Status:** Ready  
> **Sprint:** 29  
> **Dates:** 2026-10-02 → 2026-10-15  
> **Trunk:** `main` @ `1d93e86` (801/801; ReplayGolden 6/6)  
> **Layer:** Content / Data  
> **GDD:** `design/gdd/` (catalog population); Req 06 continuity

## Goal

Land **TL export Phases 1–2** from S28-11 PROCEED verdict: `tlTier` on export manifests, migration `007` `catalog_snapshot.branch` (`TL-0`…`TL-5`), metadata only — **no** runtime `tlBranch` binding or physical branch DBs.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-006 | Accepted | Engine-free `ProjectAegis.Data` boundary |
| ADR-011 | Accepted | Write-gate governance; export manifest metadata |

## Graphite Stack (merge order)

```
main
 └── stack/sprint29/full-sln-gate              (S29-01 — shared day-1)
      └── stack/sprint29/tl-export-phase12     (S29-02) — CRITICAL metadata path
```

**Dependency:** S29-02 blocked on S29-01 green baseline.

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 02 | [TL export Phase 1–2](story-029-02-tl-export-phase12.md) | S29-02 | Integration | must-have | 2d | Ready |

Note: **S29-01** day-1 baseline lives in `sprint-29-closeout-devops` epic (shared gate).

## GitNexus Mandatory Rules

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `CmoMarkdownImporter`, `ICatalogReader`, export manifest types
- **No runtime binding:** `rg TlBranch|BranchDatabase` → zero production bindings

## Producer Constraints (Inherited from S28)

1. **TL export-only until Phase 4** — no physical branch DBs in S29
2. **Nightly corpus** — full sensor corpus stays off-CI
3. **GHA billing** — permanent local-gate advisory; Buildkite merge authority

## Definition of Done

- [ ] S29-02 complete (must-have)
- [ ] Migration `007` applies cleanly; `catalog_snapshot.branch` column present
- [ ] Export drops carry `tlTier` manifest field
- [ ] Zero runtime `TlBranch` / `BranchDatabase` bindings
- [ ] `dotnet test` WriteGate|Platform|CatalogImport filters green
- [ ] Evidence: `production/agentic/sprint-29-tl-export-phase12-*.md`
- [ ] Tracker row 06 updated

## References

- S28-11 spike: `production/agentic/sprint-28-tl-branching-spike-2026-06-18.md`
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md`
- Parallel kickoff: `production/agentic/sprint-29-parallel-kickoff-2026-06-18.md` *(create at kickoff)*
- Track plan: `production/agentic/sprint-29-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*