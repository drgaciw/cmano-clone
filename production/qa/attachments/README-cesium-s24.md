# Cesium Sprint 24 Editor Screenshot Protocol (S24-08)

**Status:** **BLOCKED** — headless Linux CI/agent host; Unity 6.3 Editor + Cesium ion token required.  
**QA verdict:** APPROVED WITH CONDITIONS (`production/qa/sprint-24-qa-signoff-2026-06-17.md`)  
**Automated proxy:** `PlayModeSmoke|Cesium|Globe` **14/14 PASS** on closeout stack.

## Required captures

Save to this directory:

| File | Scene / view | Acceptance |
|------|----------------|------------|
| `cesium-s24-globe-load.png` | `CesiumSpike.unity` — Baltic bbox, globe loaded | No console errors; terrain visible |
| `cesium-s24-depth-occlusion.png` | Same scene — zoom showing marker depth/occlusion | Markers readable; no z-fight |
| `cesium-s24-selection-oob.png` | Selection sync — map pick → OOB panel | OOB row highlights selected unit |

## Capture steps (Unity 6.3 Editor)

1. Open `unity/ProjectAegis` in Unity **6000.3.x** LTS.
2. Set Cesium ion token (Project Settings → Cesium) per team vault.
3. Open `Assets/Scenes/CesiumSpike.unity` (or documented spike scene from `S24-08-DONE.md`).
4. Enter Play Mode; wait for tileset load (Baltic patrol bbox).
5. `Game` view → capture at 1920×1080; save filenames above.
6. Repeat for depth/occlusion and selection→OOB cases per `production/qa/sprint-24-cesium-polish-2026-06-17.md` checklist.

## CI default

`DelegationSmoke.unity` keeps `useGlobeMap=false` — globe off in automated PlayMode; Editor evidence is the human gate.

## When unblocked

```bash
git add production/qa/attachments/cesium-s24-*.png
git commit -m "docs(qa): S24-08 Cesium Editor screenshot evidence"
```

Attach PR comment on #210 (`stack/sprint24/cesium-polish`) and clear QA condition in sign-off addendum.