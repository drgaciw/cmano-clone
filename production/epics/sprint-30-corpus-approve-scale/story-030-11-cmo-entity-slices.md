---
id: S30-11
status: Complete
last_updated: 2026-06-18
type: Integration
priority: nice-to-have
graphite_branch: stack/sprint30/cmo-entity-slices
estimate_days: 2
dependencies:
  - S30-04 ship.md approve at scale
owner: team-data
sprint: 30
req_trace: Req 06 Phase 2; CMO entity corpus expansion; S30-04 nightly script foundation
---

# Story 030-11 — CMO Entity Nightly Slices

> **Epic:** sprint-30-corpus-approve-scale  
> **ADR:** ADR-006 (engine-free), ADR-011 (write-gate governance)

## Summary

Extend `tools/cmo-nightly-import.sh` and `tools/cmo-nightly-approve.sh` for **aircraft**, **submarine**, and **facility** corpora (off-CI). Wire `--entity` flags; add curated golden fixtures per domain for CI. Full corpora (`aircraft.md` 7387, `facility.md` 4511, `submarine.md` 732) never in `dotnet test`.

## Acceptance Criteria

- [x] `cmo-nightly-import.sh` supports `--entity aircraft|submarine|facility` (plus existing sensor|weapon|platform|all)
- [x] `cmo-nightly-approve.sh` supports matching entity approve path
- [x] Curated golden fixture per domain (e.g. `aircraft-slice-*.md`, `submarine-slice-*.md`, `facility-slice-*.md`) with WriteGate round-trip tests
- [x] Propose-only + quarantine JSON per batch; commits via `CatalogWriteGate` approve path only
- [x] **Not** wired into `dotnet test` CI (curated fixtures + `--max-records` only)
- [x] Full corpora remain nightly-only (not CI)
- [x] WriteGate regression PASS on curated per-domain fixtures
- [x] Off-CI nightly evidence per entity domain (optional; not sprint gate)
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Script flags wired for new entities
  - Given: `aircraft.md`, `submarine.md`, `facility.md` paths under `docs/reference/cmano-db/`
  - When: `./tools/cmo-nightly-import.sh --entity aircraft --dry-run` (and submarine, facility)
  - Then: planned runs printed; valid markdown paths resolved; chunk 500/batch default honored
  - Edge cases: invalid `--entity` rejected; `--max-records` cap respected; partial entity selection with `all`

- **AC-2**: Curated golden per domain
  - Given: curated slice fixture per entity domain with `MAX_RECORDS` cap
  - When: propose→approve dry-run or round-trip on curated fixture
  - Then: WriteGate tests PASS; pinned hash stable on curated slice; no full corpus in CI
  - Edge cases: empty slice; quarantine recovery; cross-entity batch isolation

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
# Script flag validation (dry-run)
./tools/cmo-nightly-import.sh --entity aircraft --dry-run
./tools/cmo-nightly-import.sh --entity submarine --dry-run
./tools/cmo-nightly-import.sh --entity facility --dry-run
# Curated gate per domain (CI-safe)
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity aircraft --chunk-size 500 --propose-only --dry-run
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity submarine --chunk-size 500 --propose-only --dry-run
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity facility --chunk-size 500 --propose-only --dry-run
# WriteGate regression
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal
# Off-CI full corpora (evidence only — never CI)
# ./tools/cmo-nightly-import.sh --entity aircraft --chunk-size 500 --propose-only
# ./tools/cmo-nightly-approve.sh --entity aircraft --release-version nightly-aircraft-$(date -u +%Y%m%d)
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
- S28-02 pattern: `production/epics/sprint-28-cmo-corpus-v2/story-028-02-nightly-platform-corpus.md`
- Corpus counts: `docs/reference/cmano-db/cmano-db-data.md`
- Kickoff: `production/sprints/sprint-30-tl-bind-corpus-scale.md` (S30-11)
- Track plan: `production/agentic/sprint-30-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md`