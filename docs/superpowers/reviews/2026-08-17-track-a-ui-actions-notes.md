# Track A — UI visual actions notes (2026-08-17)

**Owner:** Track A (attention toast + interactive time compression)  
**Spec:** `docs/superpowers/specs/2026-08-17-cmd-39-attention-toast-clock-interrupt-draft.md` (status: **implemented partial**; REQ-20 not appended)  
**Forbidden paths:** DelegationBridge hotpath, CatalogWriteGate, kinematics, VFX, MessageLogProjection — not edited.

## GitNexus impact (pre-edit)

Index for `/home/username01/cmano-clone` was **380 commits behind HEAD**. `WatchAutoPauseGate` / `AttentionTierAlertProjection` were **not in the index** (post-index S115/S109). Upstream impact on `C2TopBarPanelHost` and `DelegationBridgeHost`: **LOW** (0 direct callers in the stale graph). New symbols are additive presentation + command façade. Risk treated as **LOW**; do not treat empty `Watch*` impact as proof of no dependents.

## Behavior

- **Toast host** (`AttentionToastPanelHost`) binds `AttentionToastBinder` → `AttentionTierAlertProjection.Diff` + `WatchAttentionQueue` / `WatchAutoPauseGate`. One active card (title, body, severity, ACK/DISMISS) + queued count. Pause-class cards stay ahead of Critical/Notable tier toasts. Routine tier crossings do not toast. Unchanged tiers do not re-toast.
- **Clock interrupt:** `Session.ReportWatchAttention` still owns auto-pause. Play Mode seeds one demo hostile-contact event (`watch:demo:hostile-1`) so the toast and pause are visible on the stub host. Resume is `TryResumeSim` (blocked while unresolved pause-class unless override).
- **Interactive compression:** top bar − / + / PAUSE·RESUME call `C2ClockCommand` → `SimulationSession.SetTimeAccelerationFactor` / `PauseSim` / `TryResumeSim`. Label is `TIME: Nx` or `TIME: PAUSED` from the **session clock** (ADR-010). `SimplePlayModeSimHost` skips ticks while paused and repeats `RunTick` up to 8× for the session factor. **No `DelegationBridge.Tick` edits.**

## Tests

| Fixture | What it pins |
|---------|----------------|
| `AttentionToastApplyStateTests` | Empty / pause-class / queue / routine skip / binder no-spam / ack unblocks resume / latest-per-agent log |
| `C2ClockCommandTests` | Label, presets, accel, pause/resume, gated resume, null session |
| `AttentionToastHostContractTests` | UXML names, host does not call Bridge.Tick, scene builder wiring, demo watch → toast |

## Editor steps

1. Open `unity/ProjectAegis` (Unity 6000.3 LTS). Copy plugins if needed (`./tools/copy-delegation-assemblies.ps1`).
2. Menu **Project Aegis → Build DelegationSmoke Scene (comms QA)** (or **Ensure UI Maturity Hosts** on an already-open smoke scene). Confirm GameObject `AttentionToast` + PanelSettings.
3. Enter Play Mode. Expect top-right `WATCH · PAUSE` toast and `TIME: PAUSED`.
4. ACK → RESUME → −/+ compression. No console / bridge errors.

## Residual (not this track)

- Live Baltic engage/attention crossings in Editor still depend on a richer Play Mode snapshot (stub `ActiveEngagementCount = 0`). Demo seed covers the toast/pause path.
- Message log coherence for `PolicyUpdate` / `AgentDecision` is sibling-owned (`MessageLogProjection`).
- CMD-39 REQ-20 append still owner-gated.
