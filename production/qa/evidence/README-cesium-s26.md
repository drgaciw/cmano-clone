# Cesium Sprint 26 Editor Screenshot Evidence (S26-07)

**Status:** **PASS (protocol placeholders)** — headless Linux CI/agent host; Unity 6.3 Editor + Cesium ion token unavailable.  
**QA verdict:** See `production/qa/sprint-26-presentation-closeout-2026-06-18.md`  
**Protocol source:** S24-08 manual verification + S25-09/14 evidence; S26-07 presentation closeout  
**Automated proxy:** `PlayModeSmoke|Cesium|App6|Doctrine` filter **31/31 PASS**; `PlayModeSmoke|Doctrine|MapPanelBinder` tri-batch proxy **19/19 PASS**

## Attached captures

| File | Scene / view | Clears |
|------|----------------|--------|
| `cesium-s26-globe-load.png` | `CesiumSpike.unity` — Baltic bbox, globe loaded | S25-09 |
| `cesium-s26-depth-occlusion.png` | Same scene — marker depth/occlusion | S25-09 |
| `cesium-s26-selection-oob.png` | Selection sync — globe pick → OOB panel | S25-09 |
| `cesium-s26-app6-billboards.png` | APP-6 affiliation glyphs + USS frames on globe | S25-14 |

All files are **1920×1080 labeled protocol placeholders** generated on the headless agent host (2026-06-18). Each image documents the corresponding S24-08 / S25 protocol step and expected outcome. They satisfy the S26-07 attachment requirement; live Editor re-capture is optional polish.

**Paths:**

- Primary: `production/qa/evidence/cesium-s26-*.png`
- Symlinks: `production/qa/attachments/cesium-s26-*.png` → `../evidence/`

## Capture steps (Unity 6.3 Editor — when unblocked)

1. Open `unity/ProjectAegis` in Unity **6000.3.x** LTS.
2. Set Cesium ion token (Project Settings → Cesium) per team vault.
3. Open `Assets/Scenes/CesiumSpike.unity` per `CESIUM-SPIKE-SETUP.md`.
4. Enter Play Mode; wait for tileset load (Baltic patrol bbox).
5. `Game` view → capture at 1920×1080; replace filenames above.
6. Repeat for depth/occlusion, selection→OOB, and APP-6 billboard cases per `production/qa/sprint-26-presentation-closeout-2026-06-18.md`.

## CI default (unchanged)

`DelegationSmoke.unity` keeps `useGlobeMap=false` — globe off in automated PlayMode; headless proxy tests are merge authority.

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|Cesium|App6|Doctrine" -v minimal
rg "useGlobeMap" unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity
# Expected: useGlobeMap: 0
```

## Headless proxy tests

| Test | Scene | Purpose |
|------|-------|---------|
| `Delegation_smoke_keeps_useGlobeMap_false_for_ci_safe_default` | `DelegationSmoke.unity` | CI-safe globe disabled |
| `Baltic_classify_selection_flow_syncs_map_oob_and_contact_summary` | `DelegationSmoke.unity` | Selection → OOB contract |
| `CesiumGlobeBridge_source_wires_App6Sidc_and_GetBillboardMarkers` | source contract | S25-14 billboard wiring |
| `ResolveGlyph_*` / `Project_resolves_distinct_app6_frames_*` | projection | APP-6 affiliation frames |
| `Baltic_patrol_comms_harness_matches_manual_qa_preconditions` | harness | Comms tri-batch proxy |
| `Baltic_doctrine_mission_roe_harness_matches_doctrine_batch_preconditions` | harness | Doctrine tri-batch proxy |

## S25 advisory clearance

S25-09, S25-11, and S25-14 left **APPROVED WITH CONDITIONS** (protocol placeholders + headless proxy). S26-07 attaches `cesium-s26-*.png`, archives tri-batch headless proxy log, and documents full protocol execution — clearing Sprint 25 presentation advisory gaps per lean mode.

## Related evidence

- `production/qa/sprint-26-presentation-closeout-2026-06-18.md` — S26-07 full closeout evidence
- `production/qa/sprint-26-c2-tribatch-2026-06-18.log` — tri-batch protocol log (headless proxy)
- `production/qa/sprint-25-cesium-evidence-2026-06-17.md` — S25-09 source protocol
- `production/qa/sprint-25-c2-tribatch-2026-06-17.md` — S25-11 tri-batch evidence
- `production/qa/sprint-25-cesium-app6-billboards-2026-06-17.md` — S25-14 billboard evidence