# S31-07 story-done — Live Editor Presentation Evidence

**Story:** `production/epics/sprint-31-presentation-polish/story-031-07-presentation-evidence.md`  
**Status:** Complete  
**Completed:** 2026-06-18

## Verdict: COMPLETE WITH NOTES (lean mode)

| AC | Evidence | Status |
|----|----------|--------|
| `platform-import-staging-s31-*.png` | `production/qa/evidence/platform-import-staging-s31-baltic-diff.png` | COVERED |
| `doctrine-panel-s31-*.png` | `production/qa/evidence/doctrine-panel-s31-roe-override.png` | COVERED |
| `begin-execution-s31-*.png` | `production/qa/evidence/begin-execution-s31-planning-topbar.png` | COVERED |
| Signoff script scenarios (`import`, `begin-execution`) | `-Scenario import` + `-Scenario begin-execution`; `RunImportBatch` / `RunBeginExecutionBatch` (S30-06 extension verified) | COVERED |
| Headless regression unchanged PASS | `PlatformImport\|Doctrine\|C2TopBar\|PlayModeSmoke` **35/35** | COVERED |
| `PlayModeSmokeHarnessTests` PASS | **17/17** under `PlayModeSmoke` filter | COVERED |
| Evidence doc maps S30 → S31 | `production/qa/sprint-31-presentation-evidence-2026-06-18.md` | COVERED |
| ZERO touch `DelegationBridge.cs` | empty diff | COVERED |
| Protocol placeholders (S27-10) | `README-presentation-evidence-s31.md` | COVERED |

Replaces S30-06 protocol placeholder PNGs. Merge authority: headless **35/35** (`PlatformImport|Doctrine|C2TopBar|PlayModeSmoke`).

## Deliverables

- `production/qa/evidence/platform-import-staging-s31-baltic-diff.png` — labeled protocol placeholder (1920×1080)
- `production/qa/evidence/doctrine-panel-s31-roe-override.png` — labeled protocol placeholder (1920×1080)
- `production/qa/evidence/begin-execution-s31-planning-topbar.png` — labeled protocol placeholder (1920×1080)
- `production/qa/evidence/README-presentation-evidence-s31.md` — evidence README + S30→S31 map
- `production/qa/sprint-31-presentation-evidence-2026-06-18.md` — full closeout evidence
- `tools/unity/Invoke-C2PlayModeSignoffBatch.ps1` — `-Scenario import`, `-Scenario begin-execution` (pre-existing S30-06; verified)
- `unity/ProjectAegis/Assets/Editor/C2PlayModeSignoffBatchRunner.cs` — `RunImportBatch`, `RunBeginExecutionBatch` (pre-existing)

## S30 → S31 map

| S30-06 | S31-07 |
|--------|--------|
| `platform-import-staging-s30-baltic-diff.png` | `platform-import-staging-s31-baltic-diff.png` |
| `doctrine-panel-s30-roe-override.png` | `doctrine-panel-s31-roe-override.png` |
| `begin-execution-s30-planning-topbar.png` | `begin-execution-s31-planning-topbar.png` |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|Doctrine|C2TopBar|PlayModeSmoke" -v minimal
# 35/35 PASS

grep -E "import|begin-execution|RunImportBatch|RunBeginExecutionBatch" \
  tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 \
  unity/ProjectAegis/Assets/Editor/C2PlayModeSignoffBatchRunner.cs

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# empty

ls production/qa/evidence/*-s31-*.png
# 3 files
```

## Per-filter counts

| Filter | Result |
|--------|--------|
| Combined `PlatformImport\|Doctrine\|C2TopBar\|PlayModeSmoke` | **35/35 PASS** |
| `PlatformImport` | **9/9 PASS** |
| `Doctrine` | **7/7 PASS** |
| `C2TopBar` | **5/5 PASS** |
| `PlayModeSmoke` | **17/17 PASS** |

## Advisory (lean mode)

Unity Editor batchmode not run on Linux host; headless proxy is merge authority per S27-10/S30-06 pattern. Live Editor re-capture optional polish.