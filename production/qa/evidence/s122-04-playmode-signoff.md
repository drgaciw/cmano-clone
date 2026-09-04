# S122-04 Play Mode Re-Signoff Evidence Pack

**Story:** S122-04 / [DRG-208](https://linear.app/drgamtd-workspace/issue/DRG-208)
**Sprint:** S122 Slice A Unity-MCP (`production/sprints/sprint-122-slice-a-unity-mcp.md`)
**Date:** 2026-09-04
**Status:** **CONDITIONAL** — Game View captured; console is not 0-error

Play Mode visual pack exists. S122-04 is **not Complete**: `console-get-logs` recorded Errors/Exceptions (MCP tool-validation + a pre-existing `DelegationBridgeHost.Session` NRE on C2 top bar OnEnable). No `DelegationBridge.Tick` edits.

## Update 2026-09-04T17:31Z (Editor live)

| Check | Result |
|-------|--------|
| Editor | 6000.3.22f1 PID 2558129 · `unity/ProjectAegis` |
| Pin | `./tools/pin-unity-mcp-8080.sh` → Custom `http://localhost:8080` |
| GET `:8080` | HTTP 400 (MCP session required — expected) |
| POST initialize / `ping` | HTTP 200 · ping echoed `s122-visual` |
| `editor-application-get-state` | success |
| Scene | `Assets/Scenes/DelegationSmoke.unity` opened + saved after Ensure UI Maturity Hosts (`ContactDetail`, `SensorToShooter`) |
| Play Mode | entered, Game View 1477×721 captured, then stopped |

**PNGs**

- `production/qa/evidence/s122-01/contact-quality-cues.png` — SOURCE ESM / COMMS UNKNOWN (out-of-comms) / LAST KNOWN / EXPLAIN engage/c1 (presentation force-shown; live feed had no contacts at t=0 paused)
- `production/qa/evidence/s122-02/sensor-to-shooter-chain.png` — SENSOR TO SHOOTER / CHAIN COMPLETE / four LINKED lines
- `production/qa/evidence/s122-03/overlay-counts.png` — **ENVELOPES: 2** · **DATALINKS: 1** on MapPlaceholder (live bind, u1 selected)
- `production/qa/evidence/s122-04/console-get-logs.txt` — dump; **not** 0 errors

**Console (not PASS for 0-error AC)**

- `NullReferenceException` `DelegationBridgeHost.get_Session` via `C2TopBarPanelHost.OnEnable` (pre-existing; not S122 bind)
- MCP `logTypeFilter` validation Error/Exception from a bad `console-get-logs` call
- HubConnection negotiation noise during plugin start

**Not claimed:** C2 Play Mode signoff ×5 (Editor lock). S122-05 headless gates are in `production/qa/gauntlet/gauntlet-20260904-s122-ui/` (138+11+17).

---

## Earlier probe (superseded)

Play Mode re-signoff was BLOCKED until Editor came up. Historical probe below.

S122-04 is **not done**. No Game View screenshots were captured. `console-get-logs` was **not** run. This file is a fail-closed checklist only.

---

## Probe

| Field | Result |
|-------|--------|
| Command | `curl -s -o /dev/null -w '%{http_code}' --connect-timeout 2 http://localhost:8080 \|\| true` |
| HTTP code | `000` (non-2xx) |
| Timestamp (UTC) | `2026-09-04T17:04:57Z` |
| Detail | Connection refused on `localhost:8080` (`::1` and `127.0.0.1`) |
| Verbose | `Failed to connect to localhost port 8080 after 0 ms: Could not connect to server` |
| Unity Editor | Not running (not started by this lane) |
| MCP mutations | None |

Expected for this isolated lane: Unity-MCP on `http://localhost:8080` is DOWN. Parallel C# / UXML writers are on a dirty shared tree; this lane must not start the Editor or fight Play Mode.

---

## Checklist (when Editor is up)

Do **not** mark these items done. Run them serially in the Editor `/team-unity` lane **after** S122-01 / S122-02 / S122-03 C# and UXML bind land.

1. [ ] `./tools/pin-unity-mcp-8080.sh`
2. [ ] `/team-unity` ping / unity-tool-list
3. [ ] Open C2 scene, enter Play Mode
4. [ ] `screenshot-game-view`: ContactDetail quality cues (SOURCE / COMMS UNKNOWN / LAST KNOWN / EXPLAIN)
5. [ ] `screenshot-game-view`: Sensor-to-shooter chain panel
6. [ ] `screenshot-game-view`: Map overlay counts ENVELOPES / DATALINKS
7. [ ] `console-get-logs`: 0 errors
8. [ ] Store PNGs under `production/qa/evidence/s122-04/`

Acceptance (from sprint plan; **not met**):

- MCP `screenshot-game-view` + `console-get-logs` evidence pack (or 5 C2 Play Mode signoffs)
- 0 console errors

PNG destination `production/qa/evidence/s122-04/` is **not** created here. Create it when real screenshots exist.

---

## Serial Editor lane (after S122-01/02/03)

When `:8080` returns 2xx and bind stories have landed, a human or serial Editor lane should:

1. Confirm HTTP 2xx on `http://localhost:8080`.
2. Pin MCP if this clone has not: `./tools/pin-unity-mcp-8080.sh`.
3. Load `/team-unity`; `ping` then `unity-tool-list`.
4. Open the C2 scene. Enter Play Mode. Do **not** mutate `DelegationBridge` via `script-execute` or reflection.
5. Capture Game View screenshots for the three visual checks above.
6. Capture `console-get-logs` and confirm 0 errors.
7. Store PNGs under `production/qa/evidence/s122-04/` and update this file Status to PASS only if all eight checklist items are complete.

This lane did **not** run those steps.

---

## Explicit non-claims

- Play Mode was **not** entered.
- `screenshot-game-view` was **not** called.
- `console-get-logs` was **not** captured.
- No PNG evidence exists for S122-04.
- S122-04 / DRG-208 is **not** signed off.

---

## Isolation notes

This lane edited **only** this markdown file.

- No C# (`.cs`)
- No UXML / USS
- No `.meta` / `.unity` / asmdef
- No `ContactDetail*`, `SensorToShooter*`, `MapPlaceholder*`, `DelegationBridge*`
- No `production/sprint-status.yaml` or sprint markdown
- No git commit / stash / reset / checkout
- Unity Editor was not started
