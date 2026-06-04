# Sprint 18 — Kickoff

**Dates:** 2026-06-04 → 2026-06-18 (proposed)  
**Trunk:** `main`  
**Predecessor:** Sprint 17 complete — Database Intelligence P0 (DATA-1..DATA-5) on `main` @ `cde26fe`

## Sprint goal

Close the **QA / operator loop** for Wave 5 (Unity C2 + attack menu sign-off) and open **post-P0 data** work (OSINT spike, catalog Phase 2 planning) without reopening locked requirement docs 01–12.

## Must have

| ID | Task | Agent / skill | Acceptance |
|----|------|---------------|------------|
| S18-01 | Execute Unity C2 manual sign-off | `team-qa`, human | 13/13 checks in `c2-manual-signoff`; verdict PASS/FAIL recorded |
| S18-02 | Refresh headless smoke evidence | `smoke-check` | `production/qa/smoke-2026-06-*.md` @ current `main` SHA |

## Should have

| ID | Task | Agent / skill | Acceptance |
|----|------|---------------|------------|
| S18-03 | OSINT / Dynamic Systems spike (doc 05) | `team-data` + design | Spike doc in `production/agentic/` with PROCEED/DEFER |
| S18-04 | CMO Phase 2 import design | `database-branching-release-train` | ADR or plan: bulk markdown → write gate, snapshot binding |
| S18-05 | GitHub Actions billing triage | Producer | Required check enabled OR documented local gate SOP |

## Nice to have

| ID | Task | Notes |
|----|------|-------|
| S18-06 | `/map-systems` for gameplay GDDs | Separate from locked reqs program |
| S18-07 | Hindsight retain Sprint 17 outcome | `dev-cmano-clone` when `:8888` up |

## GitNexus rules

- **CRITICAL:** `DelegationBridge` — impact before any edit
- **HIGH:** `SimulationSession`, `ICatalogReader` — impact before catalog/sim wiring
- Data-only work: prefer `ProjectAegis.Data` / `Import/` — avoid Delegation unless required

## Parallel worktree layout (suggested)

```
main
 ├── stack/sprint18/qa-signoff-docs     (S18-01 docs only)
 ├── stack/sprint18/osint-spike         (S18-03)
 └── stack/sprint18/catalog-phase2-plan (S18-04)
```

## Quality gates (unchanged)

```powershell
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
```

Replay when touching sim/delegation: `--filter ReplayGolden|ReplayOrderLog`

## References

- `production/agentic/sprint-17-closeout-2026-06-04.md`
- `production/sprints/sprint-16-backlog.md`
- `production/milestones/post-mvp-requirements-program.md`