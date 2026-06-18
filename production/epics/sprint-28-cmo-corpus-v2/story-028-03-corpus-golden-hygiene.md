---
id: S28-03
status: Not Started
type: Integration
priority: must-have
graphite_branch: stack/sprint28/corpus-golden
estimate_days: 1
dependencies:
  - S28-02 complete
owner: team-data
sprint: 28
req_trace: Req 06 DBI-4.4 empty-diff; Req 21 PLE-2.3
---

# Story 028-03 — Platform Corpus E2E + Golden Hygiene

> **Epic:** sprint-28-cmo-corpus-v2  
> **Sprint gate:** Sprint 28 **fails** if this story does not land.

## Summary

Extend import golden tests for v2 nightly platform output on **curated fixtures**; pin stable hash on curated platform run; full WriteGate + replay regression unchanged. Platform corpus round-trip must land through the write gate.

## Acceptance Criteria

- [ ] `CmoMarkdownImportGoldenTests` extended for platform v2 curated slice + hash pin
- [ ] Re-import identical curated platform slice → stable ordering hash
- [ ] WriteGate regression: sensor + Phase A/B + damage + platform PASS
- [ ] `ReplayGoldenSuiteTests` — 6/6 unchanged
- [ ] Platform corpus round-trip through `CatalogWriteGate` (propose→approve on fixture)
- [ ] No 7208-record sensor in CI tests
- [ ] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Golden hash stable on curated platform run
  - Given: approved Baltic + curated platform import state
  - When: re-import same markdown slice
  - Then: empty diff or identical hash
  - Edge cases: hash drift from sort key change; mount/loadout/magazine ordering

- **AC-2**: WriteGate regression unchanged
  - Given: pre-merge WriteGate test baseline
  - When: platform golden tests run
  - Then: all WriteGate|CmoMarkdown|Platform filters PASS
  - Edge cases: extend-only violation on `CatalogWriteGate`

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogSortKey" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — extend-only |
| `CmoMarkdownImporter` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- S27-04 pattern: `production/epics/sprint-27-cmo-corpus-import/story-027-04-import-golden-hygiene.md`
- S26-04 pattern: `production/agentic/stacks/sprint26/S26-04-DONE.md`
- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md` (S28-03)
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md` *(create before implementation)*