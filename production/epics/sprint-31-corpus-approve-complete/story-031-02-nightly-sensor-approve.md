---
id: S31-02
status: Complete
type: Integration
priority: must-have
graphite_branch: stack/sprint31/nightly-sensor-approve
estimate_days: 2
dependencies:
  - S31-01 green baseline
owner: team-data
sprint: 31
req_trace: Req 06 Phase 2; S30 ship/entity approve carryover; full sensor.md nightly approve at scale
sprint_gate: true
---

# Story 031-02 — Nightly sensor.md Approve at Scale

> **Epic:** sprint-31-corpus-approve-complete  
> **ADR:** ADR-006 (engine-free), ADR-011 (write-gate governance)  
> **Sprint Gate:** Sprint fails if this story does not land through `CatalogWriteGate` with pinned snapshot evidence for full `sensor.md` (7208 records).

## Summary

Scale the **S29-03 propose→approve path** to the full sensor corpus (`docs/reference/cmano-db/sensor.md`, 7208 records, chunk 500/batch). Curator `ApproveBatch` + `RecordRelease` with pinned snapshot hash. **Nightly job only**; CI keeps curated fixtures + `--max-records`. Mirror S30-04 ship approve pattern for sensor domain.

## Acceptance Criteria

- [x] Off-CI nightly job → full `sensor.md` propose → curator `ApproveBatch` path completes
- [x] `RecordRelease` + pinned snapshot hash recorded in evidence (`sprint-31-nightly-sensor-*.md`)
- [x] All commits via `CatalogWriteGate.ApproveBatch` (no direct SQLite writes)
- [x] **Not** wired into `dotnet test` CI (curated sensor slice only)
- [x] WriteGate regression PASS on curated fixtures
- [x] ReplayGolden 6/6 unchanged on default path
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Full sensor.md propose→approve completes off-CI
  - Given: sensor corpus `sensor.md` (7208 records, chunk 500/batch)
  - When: curator runs `cmo-nightly-import.sh` propose then `cmo-nightly-approve.sh` with `ApproveBatch` + `RecordRelease`
  - Then: new `snapshot_id` + `content_hash_sha256` pinned; release recorded in `nightly-approve-summary.json`
  - Edge cases: partial batch failure; quarantine JSON recovery; job timeout; multi-batch approve ordering

- **AC-2**: CI isolation preserved
  - Given: `dotnet test` with `--max-records` on curated sensor fixture
  - When: full solution test runs
  - Then: no 7208-record `sensor.md` load in CI; WriteGate tests use curated slice only
  - Edge cases: scope creep into CI mitigated by producer sign-off; full corpora remain nightly-only

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
# Local curated gate (not full nightly corpus)
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal
# Curated round-trip (CI-safe)
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity sensor --chunk-size 500 --propose-only --dry-run
./tools/cmo-nightly-approve.sh --entity sensor --dry-run
# Nightly job (off-CI evidence — full sensor.md)
./tools/cmo-nightly-import.sh --entity sensor --chunk-size 500 --propose-only
./tools/cmo-nightly-approve.sh --entity sensor \
  --release-version nightly-sensor-s31-02-$(date -u +%Y%m%d)
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — impact before any gate edit |
| `CmoMarkdownImporter` | HIGH |
| `PlatformWorkbookWriteBridge` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- S30-04 pattern: `production/epics/sprint-30-corpus-approve-scale/story-030-04-ship-approve-scale.md`
- S29-03 pattern: `production/epics/sprint-29-corpus-approve/story-029-03-nightly-approve.md`
- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md` (S31-02)
- Track plan: `production/agentic/sprint-31-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md`