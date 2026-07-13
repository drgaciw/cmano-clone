# S32-10 story-done — Live Editor Presentation Evidence

**Story:** `production/epics/sprint-32-platform-editor-phase-f/story-032-10-presentation-evidence.md`  
**Status:** Complete  
**Completed:** 2026-06-19

## Verdict: COMPLETE WITH NOTES (lean mode)

| AC | Evidence | Status |
|----|----------|--------|
| `*-s32-*.png` — damage viewer + import staging | `platform-catalog-damage-s32-viewer-columns.png`, `platform-import-staging-s32-baltic-diff.png` | COVERED |
| Evidence doc maps S31 → S32 | `production/qa/sprint-32-presentation-evidence-2026-06-19.md` | COVERED |
| Headless regression unchanged PASS (≥35/35) | `PlatformImport\|Doctrine\|C2TopBar\|PlayModeSmoke\|PlatformCatalogViewer` **47/47** | COVERED |
| Signoff script scenarios (`import`, `doctrine`) | `-Scenario import` + `-Scenario doctrine`; `RunImportBatch` / `RunDoctrineBatch` (S30-06 extension verified) | COVERED |
| Lean PASS WITH NOTES (no Editor host) | `README-presentation-evidence-s32.md` | COVERED |
| ZERO touch `DelegationBridge.cs` | empty diff | COVERED |

Extends S31-07 evidence with S32-06 Phase F damage viewer + MaxHp import staging placeholders. Merge authority: headless **47/47**.

## Deliverables

- `production/qa/evidence/platform-catalog-damage-s32-viewer-columns.png` — labeled protocol placeholder (1920×1080)
- `production/qa/evidence/platform-import-staging-s32-baltic-diff.png` — labeled protocol placeholder (1920×1080)
- `production/qa/evidence/README-presentation-evidence-s32.md` — evidence README + S31→S32 map
- `production/qa/sprint-32-presentation-evidence-2026-06-19.md` — full closeout evidence
- `tools/unity/Invoke-C2PlayModeSignoffBatch.ps1` — `-Scenario import`, `-Scenario doctrine` (pre-existing S30-06; verified, no changes)

## S31 → S32 map

| S31-07 | S32-10 |
|--------|--------|
| *(none — damage viewer new)* | `platform-catalog-damage-s32-viewer-columns.png` |
| `platform-import-staging-s31-baltic-diff.png` | `platform-import-staging-s32-baltic-diff.png` |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|Doctrine|C2TopBar|PlayModeSmoke|PlatformCatalogViewer" -v minimal
# 47/47 PASS

grep -E "import|doctrine|RunImportBatch|RunDoctrineBatch" \
  tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 \
  unity/ProjectAegis/Assets/Editor/C2PlayModeSignoffBatchRunner.cs

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# empty

ls production/qa/evidence/*-s32-*.png
# 2 files
```

## Per-filter counts

| Filter | Result |
|--------|--------|
| Combined `PlatformImport\|Doctrine\|C2TopBar\|PlayModeSmoke\|PlatformCatalogViewer` | **47/47 PASS** |
| `PlatformImport` | **10/10 PASS** |
| `Doctrine` | **7/7 PASS** |
| `C2TopBar` | **5/5 PASS** |
| `PlayModeSmoke` | **17/17 PASS** |
| `PlatformCatalogViewer` | **11/11 PASS** |

## Advisory (lean mode)

Unity Editor batchmode not run on Linux host; headless proxy is merge authority per S27-10/S31-07 pattern. Live Editor re-capture optional polish.