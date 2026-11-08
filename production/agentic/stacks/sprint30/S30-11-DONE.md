# S30-11 story-done — CMO Entity Nightly Slices

**Story:** `production/epics/sprint-30-corpus-approve-scale/story-030-11-cmo-entity-slices.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Branch:** `stack/sprint30/cmo-entity-slices`  
**Prerequisite:** S30-04 COMPLETE (`production/qa/sprint-30-nightly-ship-2026-06-18.md`)

## Verdict: COMPLETE

| AC | Evidence | Status |
|----|----------|--------|
| `cmo-nightly-import.sh` supports `--entity aircraft\|submarine\|facility` | Dry-run verify below | **PASS** |
| `cmo-nightly-approve.sh` supports matching entity approve path | Entity blocks + dry-run path | **PASS** |
| Curated golden fixture per domain + WriteGate round-trip | `*-slice-100.md` + `CmoMarkdownEntitySliceImportTests` | **PASS** |
| Propose-only + quarantine JSON; `CatalogWriteGate` approve only | Existing S29-03/S30-04 path unchanged | **PASS** |
| Not wired into `dotnet test` CI | Curated fixtures + `MAX_RECORDS` only | **PASS** |
| Full corpora remain nightly-only | `aircraft.md` (7387), `submarine.md` (732), `facility.md` (4511) | **PASS** |
| WriteGate regression PASS on curated per-domain fixtures | Data 194/194 filtered; sln 912/912 | **PASS** |
| ZERO touch `DelegationBridge.cs` | `git diff` empty | **PASS** |

## Deliverables

### Scripts (S30-04 foundation extended)

- `tools/cmo-nightly-import.sh` — `--entity aircraft|submarine|facility|all`; env overrides `AIRCRAFT_MD`, `SUBMARINE_MD`, `FACILITY_MD`
- `tools/cmo-nightly-approve.sh` — matching approve blocks for aircraft/submarine/facility

### Curated fixtures (CI-safe)

| Domain | Fixture | IDs | Golden hash |
|--------|---------|-----|-------------|
| Aircraft | `tools/cmano-db-crawler/fixtures/aircraft-slice-100.md` | `/aircraft/5001`–`5100` | `5d82b0aa…91d81` |
| Submarine | `tools/cmano-db-crawler/fixtures/submarine-slice-100.md` | `/submarine/6001`–`6100` | `dc0b03fa…46c69` |
| Facility | `tools/cmano-db-crawler/fixtures/facility-slice-100.md` | `/facility/7001`–`7100` | `ba5d2b5f…699cf9` |

### Code

- `CmoMarkdownImportEntity` — `Aircraft`, `Submarine`, `Facility` (route to `ProposePlatformsFromMarkdown`)
- `CatalogImportMarkdownCommand.ParseEntity` — accepts new entity flags
- `CmoMarkdownImporter` — `Resolve*Slice100FixturePath()` + `ResolveReference*MarkdownPath()` helpers
- `CatalogSortKeyGoldenHashes` — pinned hashes per domain slice
- `CmoMarkdownEntitySliceImportTests` — propose→approve, scale metadata, nightly hash pin, golden reimport
- `CatalogImportMarkdownCommandTests` — CLI entity slice smoke (3 tests)

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

# Script flag validation (dry-run)
./tools/cmo-nightly-import.sh --entity aircraft --dry-run
./tools/cmo-nightly-import.sh --entity submarine --dry-run
./tools/cmo-nightly-import.sh --entity facility --dry-run
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity aircraft --chunk-size 500 --propose-only --dry-run

# WriteGate regression
dotnet test src/ProjectAegis.Data.Tests \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot" -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "ReplayGoldenSuiteTests" -v minimal

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## Test counts (2026-06-18)

| Suite | Result |
|-------|--------|
| Data.Tests (filtered) | **194/194 PASS** (+12 vs S30-04 baseline 182) |
| Cli.Tests (full) | **30/30 PASS** (+3 entity slice tests) |
| Solution | **912/912 PASS** |
| ReplayGoldenSuite | **6/6 PASS** |

## Off-CI scale metadata (parse-only; never CI)

| Corpus | Records | Chunk 500/batch |
|--------|---------|-----------------|
| `aircraft.md` | 7387 | 15 (14×500 + 387) |
| `submarine.md` | 732 | 2 (500 + 232) |
| `facility.md` | 4511 | 10 (9×500 + 11) |

## Curated propose→approve (CI-safe pattern)

```bash
AIRCRAFT_MD=tools/cmano-db-crawler/fixtures/aircraft-slice-100.md \
  MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity aircraft --chunk-size 500 --propose-only
./tools/cmo-nightly-approve.sh --entity aircraft --dry-run
```

## Not touched (by design)

- `CatalogWriteGate.cs` (extend-only; no signature changes)
- `DelegationBridge.cs`

## Unblocks

- Off-CI nightly evidence runs per entity domain (optional curator sign-off)
- Sprint 30 corpus gate — platform entity family complete (ship + aircraft + submarine + facility)