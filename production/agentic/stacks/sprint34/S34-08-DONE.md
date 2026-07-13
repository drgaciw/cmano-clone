# S34-08 story-done — catalog_link_report CLI

**Story:** `production/epics/sprint-34-link-catalog-data/story-034-08-link-report-cli.md`  
**Status:** Complete  
**Completed:** 2026-06-19

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| Sorted link rows on stdout | `CatalogLinkReportCommandTests` Baltic golden | **PASS** |
| Cli tests PASS | Filter `LinkReport\|KillChain` 4/4 | **PASS** |
| Read-only — no `ApproveBatch` | `ICatalogReader.GetSortedLinks()` only | **PASS** |
| ZERO `DelegationBridge.cs` diff | Not touched | **PASS** |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "LinkReport|KillChain" -v minimal
# Passed: 4/4
```

## Files changed

| File | Change |
|------|--------|
| `src/ProjectAegis.Data/Catalog/LinkCatalogReport.cs` | **NEW** — canonical lines + linksHash |
| `src/ProjectAegis.Data/Catalog/LinkCatalogGoldenHashes.cs` | **NEW** — Baltic golden hash |
| `src/ProjectAegis.MissionEditor.Cli/CatalogLinkReportCommand.cs` | **NEW** — read-only link report |
| `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogLinkReportCommandTests.cs` | **NEW** — 2 tests |
| `src/ProjectAegis.MissionEditor.Cli/Program.cs` | Register verb + `--help` |