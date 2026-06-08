# Playtest notes — Sprint 19 C2 sign-off (S19-01)

**Date:** 2026-06-08  
**Build:** `main` @ `7401fac`  
**Unity:** 6000.3.14f1 (batchmode + nographics)  
**Tester:** Agent batch runner + headless proxy (qa-lead review pending)

## Method

| Layer | Command / artifact |
|-------|-------------------|
| Check 1 (Play Mode console) | `pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario comms` → **PASS** |
| Check 1 (classify scenario) | `pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario classify -SkipBuild` → **PASS** |
| Checks 2–13 | `pwsh tools/unity/Invoke-ManualQaHeadlessGate.ps1` + PlayMode harness 7/7 |
| Log evidence | `unity-c2-playmode-signoff.log` (`C2PlayModeSignoffBatchRunner PASS` both scenarios) |

## Scenarios

| Scenario | Policy id | Batch check 1 | Notes |
|----------|-----------|---------------|-------|
| COMMS degrade | `baltic-patrol-comms` | PASS | DelegationSmoke scene; 8s Play Mode, no game console errors |
| Selection / contacts | `baltic-patrol-classify` | PASS | Scene rebuilt for classify policy; no game console errors |

## Checklist summary

All **13/13** rows recorded PASS in `production/qa/c2-manual-signoff-2026-06-02.md`.

- **Check 1:** Unity batch Play Mode (Editor-equivalent console gate); editor Search/MCP infrastructure noise filtered per runner allowlist.
- **Checks 2–13:** Unchanged headless proxy mapping (see `c2-automated-proxy-2026-06-02.md`).

## Fixes applied this session

- `C2PlayModeSignoffBatchRunner`: SessionState survives Enter Play Mode domain reload.
- `MapPlaceholderPanelHost`: guard `bridgeHost.Bridge` before refresh (startup NRE).
- `C2LeftDrawerPanel.uxml`: `Toggle` replaces `ToolbarToggle` for batch/headless UIToolkit factory compatibility.

## Verdict

**PASS** — S19-01 C2 operator checklist complete for `7401fac`. Optional human visual walk (click feel, tab polish) remains non-blocking per PI-006 headless proxy policy.