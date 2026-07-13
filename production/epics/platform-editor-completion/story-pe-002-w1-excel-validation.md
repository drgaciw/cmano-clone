# Story PE-002 — Excel validation + protection (PE-W1)

**Status:** Complete  
**Type:** Feature (PLE-1.2, OQ5)  
**Completed:** 2026-07-09  

## Acceptance

1. Enum list validation on known workbook enum columns beyond Emcon  
2. `_Meta` / PK protection best-effort via ClosedXML  
3. Empty-diff goldens still green  
4. Doc 21 PLE-1.2 checked with evidence  

## Evidence

| Deliverable | Path |
|-------------|------|
| Enum catalog | `src/ProjectAegis.Data.Excel/PlatformWorkbookEnumCatalog.cs` |
| ClosedXML UX | `src/ProjectAegis.Data.Excel/ClosedXmlPlatformWorkbookIo.cs` (`ApplySheetUx`) |
| Validation tests | `src/ProjectAegis.Data.Excel.Tests/ClosedXmlValidationMetadataTests.cs` |
| Catalog unit tests | `src/ProjectAegis.Data.Excel.Tests/PlatformWorkbookEnumValidationTests.cs` |
| Doc 21 | `Game-Requirements/requirements/21-Platform-Editor.md` PLE-1.2 + OQ5 |

### Enum columns covered (export-time list validation)

Sensors: ReviewState, ValueTier, TrlLevel  
Mounts: MountType, ReviewState  
Loadouts: Role, IsDefault  
Comms: Role, SatcomCapable, ReviewState, ValueTier, TrlLevel  
LinkCatalog: LinkType  
Emcon: Condition, Posture  

### OQ5 honesty residual

ClosedXML protection is **best-effort soft UX**: passwordless sheet protect (Excel "Unprotect Sheet" removes it), PK columns locked / non-PK unlocked, `_Meta` fully protected. Does not prevent ZIP/XML-level edits or guarantee cryptographic integrity. Importer still validates content; validation lists do not gate import alone.

### Verification (worktree)

```
dotnet build ProjectAegis.sln
dotnet test src/ProjectAegis.Data.Excel.Tests — 13 passed
dotnet test src/ProjectAegis.Data.Tests --filter Platform — 154 passed
```
