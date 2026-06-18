---
id: S28-02
status: Ready
type: Integration
priority: must-have
graphite_branch: stack/sprint28/corpus-v2
estimate_days: 2
dependencies:
  - S28-01 green baseline
owner: team-data
sprint: 28
req_trace: Req 06 Phase 2; DBI-2.4 bulk path; S27 carryover platform v2
---

# Story 028-02 — Nightly CMO Corpus v2 — Platform Slices

> **Epic:** sprint-28-cmo-corpus-v2  
> **ADR:** ADR-006 (engine-free), ADR-011 (write-gate governance)

## Summary

Extend `tools/cmo-nightly-import.sh` beyond sensor+weapon v1 to **platform corpus v2 slices** — propose-only with quarantine JSON, chunk 500/batch. **Nightly job only**; CI keeps curated fixtures + `--max-records`. No 7208-record sensor in `dotnet test`.

## Acceptance Criteria

- [ ] `tools/cmo-nightly-import.sh` extended for platform entity slices (v2 scope)
- [ ] Nightly job runs platform slices with chunk 500/batch
- [ ] Propose-only path; quarantine JSON artifacts per batch
- [ ] No direct SQLite writes; all commits via `CatalogWriteGate` approve path
- [ ] **Not** wired into `dotnet test` CI (curated fixtures only)
- [ ] Full 7208-record `sensor.md` remains nightly-only (not CI)
- [ ] Evidence: `production/qa/sprint-28-nightly-cmo-import-*.md`
- [ ] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Nightly platform job completes propose-only
  - Given: platform corpus markdown path (producer-approved v2 scope)
  - When: nightly job runs with chunk 500
  - Then: staged batches + quarantine artifact; no commit without approve
  - Edge cases: job timeout; partial failure recovery documented

- **AC-2**: CI isolation preserved
  - Given: `dotnet test` with `--max-records` on curated fixtures
  - When: full solution test runs
  - Then: no 7208-record sensor load; platform tests use curated slice only
  - Edge cases: scope creep into CI mitigated by producer sign-off

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
# Local curated gate (not full nightly corpus)
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|Platform|CatalogImport" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal
# Nightly job (off-CI evidence)
./tools/cmo-nightly-import.sh --entity platform --chunk-size 500 --propose-only
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogWriteGate` | CRITICAL — impact before any gate edit |
| `CmoMarkdownImporter` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- S27-02 pattern: `production/epics/sprint-27-cmo-corpus-import/story-027-02-nightly-cmo-corpus.md`
- S27 evidence: `production/qa/sprint-27-nightly-cmo-import-2026-06-18.md`
- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md` (S28-02)
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md` *(create before implementation)*