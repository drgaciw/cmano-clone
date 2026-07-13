---
id: S31-11
status: complete
type: Integration
priority: nice-to-have
graphite_branch: stack/sprint31/entity-approve-scale
estimate_days: 2
dependencies:
  - S30-11 complete
owner: team-data
sprint: 31
req_trace: Req 06 Phase 2; S30-11 entity slice foundation; full aircraft/facility/submarine nightly approve
---

# Story 031-11 — Entity Corpus Nightly Approve at Scale

> **Epic:** sprint-31-corpus-approve-complete  
> **ADR:** ADR-006 (engine-free), ADR-011 (write-gate governance)

## Summary

Scale **S30-11 entity nightly slices** to full off-CI approve for **aircraft**, **facility**, and **submarine** corpora (`aircraft.md` 7387, `facility.md` 4511, `submarine.md` 732). Per-domain `ApproveBatch` + pinned hash evidence. **Never in CI** — curated golden slices only in `dotnet test`.

## Acceptance Criteria

- [x] Off-CI nightly approve completes per entity domain (aircraft, facility, submarine)
- [x] `RecordRelease` + pinned snapshot hash per domain in evidence
- [x] All commits via `CatalogWriteGate.ApproveBatch` (no direct SQLite writes)
- [x] **Not** wired into `dotnet test` CI (curated `*-slice-100` fixtures + `--max-records` only)
- [x] Full corpora remain nightly-only (not CI)
- [x] WriteGate regression PASS on curated per-domain fixtures
- [x] Per-domain evidence files (optional; not sprint gate)
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Full entity corpora propose→approve off-CI
  - Given: `aircraft.md`, `facility.md`, `submarine.md` under `docs/reference/cmano-db/`
  - When: curator runs nightly import propose then approve per `--entity` with `ApproveBatch` + `RecordRelease`
  - Then: per-domain `snapshot_id` + `content_hash_sha256` pinned in evidence
  - Edge cases: partial batch failure; quarantine recovery; cross-entity batch isolation

- **AC-2**: Curated golden per domain in CI
  - Given: curated slice fixture per entity domain with `MAX_RECORDS` cap
  - When: `dotnet test` WriteGate|CatalogImport filters run
  - Then: tests PASS on curated slices only; no full corpus load in CI
  - Edge cases: empty slice; invalid `--entity` rejected; full corpora never in test pipeline

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
# Curated gate per domain (CI-safe)
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity aircraft --chunk-size 500 --propose-only --dry-run
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity submarine --chunk-size 500 --propose-only --dry-run
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity facility --chunk-size 500 --propose-only --dry-run
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal
# Off-CI full corpora (evidence only — never CI)
# ./tools/cmo-nightly-import.sh --entity aircraft --chunk-size 500 --propose-only
# ./tools/cmo-nightly-approve.sh --entity aircraft --release-version nightly-aircraft-s31-11-$(date -u +%Y%m%d)
# ./tools/cmo-nightly-import.sh --entity facility --chunk-size 500 --propose-only
# ./tools/cmo-nightly-approve.sh --entity facility --release-version nightly-facility-s31-11-$(date -u +%Y%m%d)
# ./tools/cmo-nightly-import.sh --entity submarine --chunk-size 500 --propose-only
# ./tools/cmo-nightly-approve.sh --entity submarine --release-version nightly-submarine-s31-11-$(date -u +%Y%m%d)
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — no bypass |
| `CmoMarkdownImporter` | HIGH |
| `PlatformWorkbookWriteBridge` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- S30-11 pattern: `production/epics/sprint-30-corpus-approve-scale/story-030-11-cmo-entity-slices.md`
- S30-04 pattern: `production/epics/sprint-30-corpus-approve-scale/story-030-04-ship-approve-scale.md`
- Corpus counts: `docs/reference/cmano-db/cmano-db-data.md`
- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md` (S31-11)
- Track plan: `production/agentic/sprint-31-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md`