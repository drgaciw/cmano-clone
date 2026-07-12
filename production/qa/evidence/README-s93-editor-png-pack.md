# S93 Editor PNG Pack — Deferral & Capture Protocol

**Date:** 2026-07-09  
**Status:** **DEFERRED** — no Unity Editor on cloud VM  
**Authority:** Dashboard wave plan Phase C Task C1 skip path; PE-W3 defer; ASSET-042 protocol in `design/assets/specs/c2-ui-assets.md` L287–302

---

## Why deferred

- Cloud Agent VM: headless .NET only — Unity 6.3 LTS Editor not installed
- Headless PlayMode smoke **20/20** remains merge authority
- S93 closeout does **not** depend on PNG captures (placeholder capsules acceptable)

---

## Minimum capture set (when local Editor available)

| Capture | Scenario / context | Output filename |
|---------|-------------------|-----------------|
| C2 map patrol | Baltic band B | `baltic-band-b-map-s93.png` |
| Platform catalog panel | Platform Editor proxy scene | `platform-catalog-s93.png` |
| Platform import staging | Staging diff visible | `platform-staging-s93.png` |
| C2 top bar + policy | mission-roe policy | `c2-policy-panel-s93.png` |

**Resolution:** 1920×1080 PNG  
**Target folder:** `production/qa/evidence/` (ASSET-042 naming)  
**Optional store screenshots:** `production/assets/screenshots/` (ASSET-027+)

---

## Local host setup

1. Unity 6.3 LTS + `./tools/init-unity-project.ps1` per [`unity/ProjectAegis/PLAYMODE-SMOKE.md`](../../../unity/ProjectAegis/PLAYMODE-SMOKE.md)
2. Open scenes per PlayMode smoke harness routes
3. Capture at UI scale 100%, art bible §8 evidence protocol
4. Append paths to [`production/release/launch/evidence-index.md`](../release/launch/evidence-index.md)

---

## Related assets

| Asset IDs | Status @ S93 |
|-----------|--------------|
| ASSET-027–034 | Specced — capture deferred |
| ASSET-042 | Specced — protocol documented here |

**S93 produced:** ASSET-023–025 placeholders at `production/assets/store/` (not Editor captures).

---

*Deferral recorded 2026-07-09. Re-capture when local Unity Editor host available.*
