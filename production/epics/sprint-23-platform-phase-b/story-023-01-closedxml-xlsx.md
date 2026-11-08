---
id: S23-01
status: Complete
type: Integration
priority: must-have
graphite_branch: stack/sprint23/closedxml-xlsx-io
estimate_days: 2.5
dependencies:
  - S23-02 green baseline
  - S22 complete
  - src/ProjectAegis.Data.Excel/ClosedXmlPlatformWorkbookIo.cs exists
  - ADR-011 Accepted
owner: c-sharp-engineer / team-data
sprint: 23
req_trace: PLE-2.1, PLE-6.1, Req 21
---

# Story 023-01 — ClosedXML `.xlsx` Adapter

> **Epic:** sprint-23-platform-phase-b  
> **Sprint:** 23 — Platform Phase B I/O + Doctrine Polish  
> **ADR:** ADR-011 (Accepted), ADR-006 (engine-free boundary)

## Summary

Wire `ClosedXmlPlatformWorkbookIo` into `platform_export_xlsx` / `platform_import_xlsx` CLI paths (flag or default when ClosedXML restored); add integration test proving empty-diff golden on binary `.xlsx`; parity with `CanonicalTextWorkbookIo` round-trip contract (PLE-2.1). Closes Sprint 22 sign-off **C2**.

## Acceptance Criteria

- [x] CLI export produces real `.xlsx` (not canonical text)
- [x] Import round-trip yields empty diff on unedited workbook
- [x] `ClosedXmlPlatformWorkbookIo` integration test PASS
- [x] `Platform|WriteGate` scoped tests green
- [x] GitNexus impact on `IPlatformWorkbookIo` checked
- [x] Parity: same logical diff whether via `CanonicalTextWorkbookIo` or `ClosedXmlPlatformWorkbookIo`
- [x] Text column format `@` prevents numeric coercion (existing adapter contract)
- [x] `_Meta.SourceSnapshotId` and `WorkbookHash` survive binary write/read (PLE-1.1)

## Verify Commands

```powershell
dotnet restore src/ProjectAegis.Data.Excel/ProjectAegis.Data.Excel.csproj
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "Platform|ClosedXml|WriteGate" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter "Mcp|Platform" -v minimal
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- platform_export_xlsx --help
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- platform_export_xlsx --snapshot <id> --output /tmp/test.xlsx
# verify: file /tmp/test.xlsx → Zip/OLE binary; NOT plain text
```

## GitNexus Symbols to Impact-Check

| Symbol | Risk | Rule |
|--------|------|------|
| `IPlatformWorkbookIo` | HIGH | `npx gitnexus impact IPlatformWorkbookIo --direction upstream` before edit |
| `PlatformWorkbookImporter` | HIGH | Impact before sheet/import changes |
| `PlatformWorkbookExporter` | HIGH | Impact before sheet/export changes |
| `ClosedXmlPlatformWorkbookIo` | HIGH | Primary adapter wiring target |
| `CanonicalTextWorkbookIo` | MEDIUM | Parity contract — extend-only fallback |
| `PlatformExportXlsxCommand` | MEDIUM | CLI export path |
| `PlatformImportXlsxCommand` | MEDIUM | CLI import path |

After edits: `npx gitnexus detect_changes --repo cmano-clone` before commit.

## Files to Create / Modify

| Action | Path |
|--------|------|
| Modify | `src/ProjectAegis.Data.Excel/ClosedXmlPlatformWorkbookIo.cs` |
| Modify | `src/ProjectAegis.MissionEditor.Cli/Commands/PlatformExportXlsxCommand.cs` |
| Modify | `src/ProjectAegis.MissionEditor.Cli/Commands/PlatformImportXlsxCommand.cs` |
| Modify | `src/ProjectAegis.Data/Platform/PlatformWorkbookImporter.cs` (I/O selection) |
| Modify | `src/ProjectAegis.Data/Platform/PlatformWorkbookExporter.cs` (I/O selection) |
| Modify | `ProjectAegis.sln` (add `ProjectAegis.Data.Excel` if missing) |
| Create | `src/ProjectAegis.Data.Tests/Platform/ClosedXmlPlatformWorkbookIoTests.cs` |
| Extend | `src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookRoundTripTests.cs` |
| Extend | `src/ProjectAegis.MissionEditor.Cli.Tests/McpToolsManifestTests.cs` (if CLI surface changes) |

## References

- Kickoff: `production/sprints/sprint-23-platform-phase-b-doctrine-polish.md` (S23-01)
- Implementation plan: `docs/superpowers/plans/sprint-23-implementation.md`
- ADR-011: `docs/architecture/adr-011-platform-editor-excel-roundtrip.md`
- Req 21: `Game-Requirements/requirements/21-Platform-Editor.md`

## Completion Notes

- **Completed:** 2026-06-17
- **Implementation commit:** `7339ee1` — `feat(data): wire ClosedXML xlsx adapter for platform workbook I/O [S23-01]`
- **Criteria:** all 8 passing (lean review mode)
- **Test Evidence:**
  - `src/ProjectAegis.Data.Tests/Platform/ClosedXmlPlatformWorkbookIoTests.cs` (6 tests)
  - `src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookRoundTripTests.cs` (ClosedXml round-trip extension)
  - `src/ProjectAegis.MissionEditor.Cli.Tests/McpToolsManifestTests.cs` (platform CLI verbs in MCP manifest)
- **Verify runs (2026-06-17):**
  - `dotnet test …Data.Tests… --filter "Platform|ClosedXml|WriteGate"` → **43/43 PASS**
  - `dotnet test …MissionEditor.Cli.Tests… --filter "Mcp|Platform"` → **4/4 PASS**
  - `platform_export_xlsx` smoke → `io: ClosedXmlPlatformWorkbookIo`, binary `.xlsx` (ZIP PK header)
- **GitNexus:** `npx gitnexus impact IPlatformWorkbookIo --direction upstream --repo cmano-clone` → 4 symbols impacted, **LOW** risk; `PlatformExportXlsxCommand.Run` in affected processes
- **Code Review:** Skipped (lean mode)