---
id: S31-10
status: Complete
type: Integration
priority: nice-to-have
graphite_branch: stack/sprint31/weapon-approve-scale
estimate_days: 2
dependencies:
  - S31-02 complete
owner: team-data
sprint: 31
req_trace: Req 06 Phase 2; S30-04 ship approve pattern; full weapon.md nightly approve at scale
---

# Story 031-10 — Nightly weapon.md Approve at Scale

> **Epic:** sprint-31-corpus-approve-complete  
> **ADR:** ADR-006 (engine-free), ADR-011 (write-gate governance)

## Summary

Mirror **S30-04 ship approve** for the full weapon corpus (`docs/reference/cmano-db/weapon.md`, 4403 records, chunk 500/batch). Curator `ApproveBatch` + `RecordRelease` with pinned snapshot hash. **Nightly job only**; CI keeps curated `weapon-slice-50` fixture + `--max-records`.

## Acceptance Criteria

- [x] Off-CI nightly job → full `weapon.md` propose → curator `ApproveBatch` path completes
- [x] `RecordRelease` + pinned snapshot hash recorded in evidence
- [x] All commits via `CatalogWriteGate.ApproveBatch` (no direct SQLite writes)
- [x] **Not** wired into `dotnet test` CI (curated `weapon-slice-50` only)
- [x] WriteGate regression PASS on curated fixtures
- [x] ReplayGolden 6/6 unchanged on default path
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Full weapon.md propose→approve completes off-CI
  - Given: weapon corpus `weapon.md` (4403 records, chunk 500/batch)
  - When: curator runs `cmo-nightly-import.sh` propose then `cmo-nightly-approve.sh` with `ApproveBatch` + `RecordRelease`
  - Then: new `snapshot_id` + `content_hash_sha256` pinned; release recorded in nightly summary
  - Edge cases: partial batch failure; quarantine JSON recovery; multi-batch approve ordering

- **AC-2**: CI isolation preserved
  - Given: `dotnet test` with `--max-records` on curated `weapon-slice-50` fixture
  - When: full solution test runs
  - Then: no 4403-record `weapon.md` load in CI; WriteGate tests use curated slice only
  - Edge cases: scope creep into CI; full corpus remains nightly-only

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal
# Curated round-trip (CI-safe)
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity weapon --chunk-size 500 --propose-only --dry-run
./tools/cmo-nightly-approve.sh --entity weapon --dry-run
# Nightly job (off-CI evidence — full weapon.md)
./tools/cmo-nightly-import.sh --entity weapon --chunk-size 500 --propose-only
./tools/cmo-nightly-approve.sh --entity weapon \
  --release-version nightly-weapon-s31-10-$(date -u +%Y%m%d)
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — no bypass |
| `CmoMarkdownImporter` | HIGH |
| `PlatformWorkbookWriteBridge` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- S30-04 pattern: `production/epics/sprint-30-corpus-approve-scale/story-030-04-ship-approve-scale.md`
- S30-11 pattern: `production/epics/sprint-30-corpus-approve-scale/story-030-11-cmo-entity-slices.md`
- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md` (S31-10)
- Track plan: `production/agentic/sprint-31-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md`