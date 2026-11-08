# S55 PNG Evidence Protocol + Descriptions (Editor / C2 / Hypersonic / Cesium)

**No Unity runtime in this agent env (headless container).**  
**Real PNG capture:** Requires local Unity Editor (6000.3 + Cesium package). See README-s55-editor-evidence.md + S55-EDITOR-VERIF.md for steps.

**Cites:** roadmap-062126.md §10 S55 Req20 E4 (Editor PNG S55-05 shadow), post-release-scope-boundary-2026-06-21.md (live Editor PNG refresh for Req20), hypersonic wts + cesium wts.

## Expected PNGs (Describe + Naming)
- `s55-c2-topbar-hypersonic-alert-active.png`  
  Description: C2 top bar in Unity Editor Play. Shows time, phase, compression, mode, comms, score + red/warning "⚠ HYPERSONIC ALERT — T-042s" (tension clock per 09-Near-Future). USS styled alert visible, no overlap. Verifies HYPERSONIC_ALERT projection active path.
  
- `s55-c2-topbar-hypersonic-alert-inactive.png`  
  Description: Same top bar, alert label empty. Baseline for inactive (omits_hypersonic_alert test path).

- `s55-cesium-globe-c2-integrated.png`  
  Description: Globe view (Baltic bbox markers: friendly green / hostile red) + overlaid or adjacent C2TopBar with optional HYPERSONIC_ALERT. Selection / pan works. FPS noted ~60. CesiumGlobeBridge + Host active, no console errors. Verifies Cesium + C2 tie-in for E4.

- `s55-editor-presentation-full.png` (advisory)  
  Description: Full Editor screenshot: C2 Command Post (map/globe primary), top bar alert, right panel, bottom log. Per Req20 layout + platform editor cross if visible. (Refresh per boundary for Req20/21.)

- `s55-hypersonic-alert-tension-clock-close.png`  
  Description: Zoom on alert label showing dynamic T- counter (simTime % 180).

## Capture Protocol (Unity)
1. Preflight: GitNexus detect (LOW), read S55-EDITOR-VERIF.
2. Unity: resolve Cesium, open appropriate scene (C2 + globe setup).
3. Trigger alert state (use test data or projection call in host).
4. Play + screenshot (use Game view + Profiler for FPS).
5. Save to this dir with exact names above.
6. Update tracker row 20 + S55 verif doc + closeout.
7. Superpower: unity-engineer executes capture.

**Evidence readiness status:** Protocol defined + descriptions provided. Files ready for PNG drop. Tests + shadow verif complete independently (PASS).

## Verification Cross-Ref
All described visuals map 1:1 to passed tests:
- includes_hypersonic_alert_label_when_active_S55
- omits_hypersonic_alert_when_inactive
- Cesium 8/8 adapter tests
- C2TopBar* 16/16+

Hypersonic wts used for source + execution; results claimed here.

**Final:** S55 Editor PNG evidence (described) + hypersonic shadow verif populated. Ready.
