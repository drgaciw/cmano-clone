---
id: S29-03
status: Complete
type: Integration
priority: must-have
graphite_branch: stack/sprint29/corpus-approve
estimate_days: 2
dependencies:
  - S29-01 green baseline
owner: team-data
sprint: 29
req_trace: Req 06 Phase 2; S28-02 propose-only carryover; nightly approve workflow
sprint_gate: true
---

# Story 029-03 — Nightly Corpus Approve Workflow

> **Epic:** sprint-29-corpus-approve  
> **ADR:** ADR-006 (engine-free), ADR-011 (write-gate governance)  
> **Sprint Gate:** Sprint fails if this story does not land through `CatalogWriteGate` with pinned snapshot evidence.

## Summary

Close the **propose→approve gap** from S28-02: curator `ApproveBatch` path for platform v2 nightly propose runs; `RecordRelease` + snapshot hash. **Nightly job only**; CI keeps curated fixtures + `--max-records`. No 7208-record sensor in `dotnet test`.

## Acceptance Criteria

- [x] Nightly job → propose → curator `ApproveBatch` path completes off-CI
- [x] `RecordRelease` + pinned snapshot hash recorded in evidence
- [x] All commits via `CatalogWriteGate` approve path (no direct SQLite writes)
- [x] **Not** wired into `dotnet test` CI (curated fixtures only)
- [x] Full 7208-record `sensor.md` remains nightly-only (not CI)
- [x] WriteGate regression PASS on curated fixtures
- [x] Evidence: `production/qa/sprint-29-nightly-approve-*.md`
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Nightly propose→approve completes
  - Given: platform corpus v2 nightly propose run (chunk 500/batch)
  - When: curator runs `ApproveBatch` + `RecordRelease`
  - Then: new snapshot_id + content_hash_sha256 pinned; release recorded
  - Edge cases: partial batch failure; quarantine JSON recovery; job timeout

- **AC-2**: CI isolation preserved
  - Given: `dotnet test` with `--max-records` on curated fixtures
  - When: full solution test runs
  - Then: no 7208-record sensor load; WriteGate tests use curated slice only
  - Edge cases: scope creep into CI mitigated by producer sign-off

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
# Local curated gate (not full nightly corpus)
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal
# Nightly job (off-CI evidence)
./tools/cmo-nightly-import.sh --entity platform --chunk-size 500 --propose-only
# Approve manually via catalog_write_approve before evidence capture
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — impact before any gate edit |
| `CmoMarkdownImporter` | HIGH |
| `PlatformWorkbookWriteBridge` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- S28-02 pattern: `production/epics/sprint-28-cmo-corpus-v2/story-028-02-nightly-platform-corpus.md`
- S28 evidence: `production/qa/sprint-28-nightly-cmo-import-2026-06-18.md`
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md` (S29-03)
- Track plan: `production/agentic/sprint-29-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*