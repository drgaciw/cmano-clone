# Playtest Report — C2 UI rev 2 (Selection / Order Lifecycle / Alerting / Scaling)

## Session Info
- **Date**: 2026-07-08
- **Build**: `c2-req20-integration` @ `0fa934f` (stacked on Phase 0 `04b8e12`)
- **Duration**: ~20 min equivalent (headless proxy batches + integration gate audit)
- **Tester**: QA lean proxy synthesis (headless Linux; no live Editor)
- **Platform**: Linux headless + `dotnet` harness (Unity host/UXML/USS verified via models only)
- **Session Type**: Theater-commander rev-2 delta — multi-unit ops, order feedback, interruption, scaling

## Test Focus
Whether the rev-2 command-model additions (req 20 §Selection, §Order lifecycle, §Alerting, NFR scaling) hold up against their acceptance criteria **at the projection/model layer**, with `DelegationBridge` untouched and Baltic determinism preserved. Visual/feel qualities are explicitly out of scope for this headless pass.

## Acceptance-criteria walkthrough

### AC-7 — Drag-box selects ≥2 units; one group engage → one logged intent per eligible unit; ineligible listed before commit
- **Result**: **PASS (headless)**. `SelectionBoxResolverTests` proves rect→unit-id resolution (≥2 units); `GroupOrderPlanTests` proves eligibility with ineligible/destroyed units named before commit; `GroupOrderFanOutTests` + `C2Rev2IntegrationProxyTests.MultiSelect_group_fan_out_issues_one_intent_per_eligible_unit` prove one intent per eligible survivor via the **existing** bridge command API.
- **Not observed**: on-map marquee rubber-band visual; OOB non-anchor multi-row highlight (deferred polish).

### AC-8 — Queued order shows lifecycle state in unit panel; cancelling emits a logged cancellation intent
- **Result**: **PARTIAL**. Display half **PASS** — `OrderLifecycleProjectionTests` proves `accepted→queued→executing→completed|denied|aborted`; `MessageLogPanelBinderTests` proves the state surfaces on `PLAYER_ORDER` rows; `OrderStateChip` binder wired to `RightUnitPanelHost`. Cancel half **BLOCKED** — no `PlayerOrderCancelled` bridge affordance exists (no cancel `OrderKind`/log-kind/queue-remove); deferred to **Phase 2b** scoped extension. Weapons-release gate is presentation-ready on a `WeaponsTight` proxy (real positive-control flag pending).

### AC-9 — Critical event with auto-pause enabled pauses the sim and shows a toast whose click focuses the source unit
- **Result**: **PARTIAL**. Toast + focus **PASS** — `ToastStackModelTests` (max 3 + `+N` overflow, click→focus target, replay suppression) and `AlertingIntegrationTests` (real `BalticReplayHarness` KILL_CONFIRMED → toast click → `SelectedUnitId` match). Auto-pause **command** value **PASS** (`AutoPausePolicyTests`, `C2Rev2IntegrationProxyTests.Critical_alert_yields_AutoPauseCommand_via_AutoPausePolicy`). **Sim actuation BLOCKED** — no pause-reason stack in baseline; deferred to **Phase 2b**.

### AC-10 — OOB and message log scroll 5k rows without frame drop (virtualization proof)
- **Result**: **PASS (mechanism)**. `PanelRefreshGateTests` proves the host no longer rebuilds the `ListView` on unchanged frames (`AppliedCount == 1` across 10 identical frames), restoring recycling. FPS itself is unmeasurable headlessly — Editor pass required for the true 5k-row scroll number.
- **Scaling (AC-1 adjacent)**: `C2AccessibilitySettingsTests` proves ScalePercent {100,125,150} → font-size with the 10px floor applied pre-scale; shared `PanelSettings.asset` set to Scale-With-Screen-Size @1920×1080 match-height.

## Bugs Encountered

| # | Description | Severity | Reproducible |
|---|-------------|----------|--------------|
| 1 | Pre-existing stale test path `Runtime/CesiumGlobeBridge.cs` (file moved to `Runtime/Cesium/`) | Low | Fixed in `1acf822` |
| — | No new defects from rev-2 integration | — | — |

## Quantitative Data
- **Full solution suite**: **1551 / 1551 PASS, 0 fail** (Sim 302, Delegation 322, Data.Excel 5, UnityAdapter 369, Data 478, MissionEditor.Cli 75).
- **C2 headless proxy** (`PlayModeSmokeHarnessTests`): green; **+3 rev-2 integration seams** (`C2Rev2IntegrationProxyTests`).
- **ReplayGolden**: green; **Baltic hash `17144800277401907079` unchanged**; **`DelegationBridge.cs` zero-diff**.
- ~180 new track tests across selection, lifecycle, alerting, symbology, scaling.

## Overall Assessment
- **AC-7 / AC-8-display / AC-9-toast / AC-8/10 filters+virtualization / scaling**: model-proven, merge-ready.
- **AC-8 cancel + AC-9 sim-pause**: presentation-complete, **actuation blocked on Phase 2b** protected-surface work (order-cancel affordance + pause-reason stack), approved 2026-07-08.
- **Determinism**: preserved (bridge untouched, hash frozen).

## Top 3 Priorities from this session
1. **Phase 2b** — add order-cancel bridge affordance + pause-reason stack (additive; keep the golden Baltic hash frozen) to actuate AC-8 cancel and AC-9 pause; determinism-engineer review.
2. **Editor PlayMode pass** — visually confirm marquee + APP-6 symbology + label declutter + pooled map together; toast rendering; message-log filter/severity/chip class stacking; ScalePercent font scaling; wire new hosts (`ToastStackPanelHost`, `AegisPanelSettingsBinder`) into a scene.
3. **Systems-designer** — define a real positive-control policy flag to replace the `WeaponsTight` gate proxy.

## Action Routing
- **Design changes:** positive-control policy flag → `/systems-design`; a11y §6.3 cascade (`input.cycle_unit`, `input.confirm`) → accessibility-specialist.
- **Balance:** N/A.
- **Bugs:** none open.
- **Polish:** OOB non-anchor multi-select highlight; Editor density review.

**Session mode:** Lean proxy synthesis — satisfies gate structure; **live-Editor + human session still recommended** for visual/feel ACs.
