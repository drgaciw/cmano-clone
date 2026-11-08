---
id: S30-04
status: Complete
last_updated: 2026-06-18
type: Integration
priority: must-have
graphite_branch: stack/sprint30/ship-approve-scale
estimate_days: 2
dependencies:
  - S30-01 green baseline
owner: team-data
sprint: 30
req_trace: Req 06 Phase 2; S29-03 curated approve carryover; full ship.md nightly approve at scale
sprint_gate: true
---

# Story 030-04 — Nightly ship.md Approve at Scale

> **Epic:** sprint-30-corpus-approve-scale  
> **ADR:** ADR-006 (engine-free), ADR-011 (write-gate governance)  
> **Sprint Gate:** Sprint fails if this story does not land through `CatalogWriteGate` with pinned snapshot evidence for full `ship.md` (4844 records).

## Summary

Scale the **S29-03 propose→approve path** from curated `ship-slice-100` to the full platform corpus (`docs/reference/cmano-db/ship.md`, 4844 records, chunk 500/batch). Curator `ApproveBatch` + `RecordRelease` with pinned snapshot hash. **Nightly job only**; CI keeps curated fixtures + `--max-records`. No 7208-record sensor in `dotnet test`.

## Acceptance Criteria

- [x] Off-CI nightly job → full `ship.md` propose → curator `ApproveBatch` path completes
- [x] `RecordRelease` + pinned snapshot hash recorded in evidence (`sprint-30-nightly-ship-*.md`)
- [x] All commits via `CatalogWriteGate.ApproveBatch` (no direct SQLite writes)
- [x] **Not** wired into `dotnet test` CI (curated fixtures only)
- [x] Full 7208-record `sensor.md` remains nightly-only (not CI)
- [x] WriteGate regression PASS on curated fixtures
- [x] Evidence: `production/qa/sprint-30-nightly-ship-*.md`
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Full ship.md propose→approve completes off-CI
  - Given: platform corpus `ship.md` (4844 records, chunk 500/batch)
  - When: curator runs `cmo-nightly-import.sh` propose then `cmo-nightly-approve.sh` with `ApproveBatch` + `RecordRelease`
  - Then: new `snapshot_id` + `content_hash_sha256` pinned; release recorded in `nightly-approve-summary.json`
  - Edge cases: partial batch failure; quarantine JSON recovery; job timeout; multi-batch approve ordering

- **AC-2**: CI isolation preserved
  - Given: `dotnet test` with `--max-records` on curated fixtures (`ship-slice-100.md`)
  - When: full solution test runs
  - Then: no 4844-record `ship.md` load; no 7208-record sensor load; WriteGate tests use curated slice only
  - Edge cases: scope creep into CI mitigated by producer sign-off

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
# Local curated gate (not full nightly corpus)
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal
# Curated round-trip (CI-safe)
PLATFORM_MD=tools/cmano-db-crawler/fixtures/ship-slice-100.md \
  MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity platform --chunk-size 500 --propose-only
./tools/cmo-nightly-approve.sh --entity platform --dry-run
# Nightly job (off-CI evidence — full ship.md)
./tools/cmo-nightly-import.sh --entity platform --chunk-size 500 --propose-only
./tools/cmo-nightly-approve.sh --entity platform \
  --release-version nightly-ship-s30-04-$(date -u +%Y%m%d)
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — impact before any gate edit |
| `CmoMarkdownImporter` | HIGH |
| `PlatformWorkbookWriteBridge` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- S29-03 pattern: `production/epics/sprint-29-corpus-approve/story-029-03-nightly-approve.md`
- S29 evidence: `production/qa/sprint-29-nightly-approve-2026-06-18.md`
- S28-02 pattern: `production/epics/sprint-28-cmo-corpus-v2/story-028-02-nightly-platform-corpus.md`
- Kickoff: `production/sprints/sprint-30-tl-bind-corpus-scale.md` (S30-04)
- Track plan: `production/agentic/sprint-30-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md`