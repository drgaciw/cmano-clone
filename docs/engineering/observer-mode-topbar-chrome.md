# Observer-mode top-bar chrome — the "OBSERVER · READ-ONLY vs COMMAND" mode seam (DRG-249 / MODE-03 / DRG-413)

> Detailed companion: [observer-mode-chrome-projection.md](observer-mode-chrome-projection.md).

> **ADR:** this is the **presentation / read-model seam**
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3
> — chrome is a **client**, not sim authority), with
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md) (C2 presentation) and
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md) (the adapter boundary).

The **observer-mode chrome** answers one top-bar question — *can I command right now, or am I just watching a
replay?* — by projecting the orchestrator's `AttachReplayViewer` flag (DRG-249, MODE-03) into a single immutable
record the top bar paints: the mode label (`OBSERVER · READ-ONLY` or `COMMAND`), its USS class, whether order and
delegation buttons are enabled, and a player-visible disabled reason. It also wraps order issuance so an attempt
made while observing is **refused with a visible reason** instead of silently doing nothing.

It is the **presentation half** of the observer feature. The enforcement half — the `AttachReplayViewer` flag
itself and the human-order / enqueue paths it blocks inside the orchestrator — is
[replay-checkpoint-observer-runtime.md](replay-checkpoint-observer-runtime.md). This chrome never **sets** the
flag, never touches a controller slot, and is **not** part of the Baltic v2 `SimWorldHash`.

The whole feature is one file plus the top-bar host:

| File | Role |
|------|------|
| [`ObserverModeChromePresentation.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/ObserverModeChromePresentation.cs) | the pure record + its `Project` / `TopBarModeText` / `TryIssueOrder` / `DescribeReason` statics, and `ObserverOrderAttempt` |
| [`C2TopBarPanelHost.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/C2TopBarPanelHost.cs) | the UI-Toolkit host that paints the mode label and gates the Begin-Execution button (DRG-413) |

---

## Input — the single `AttachReplayViewer` flag

`ObserverModeChromePresentation.Project` has two overloads, both pure:

```csharp
ObserverModeChromePresentation Project(DelegationBridge bridge);   // reads bridge.AttachReplayViewer
ObserverModeChromePresentation Project(bool attachReplayViewer);   // attachReplayViewer ? Observer : Command
```

[`DelegationBridge.AttachReplayViewer`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs)
is a thin proxy over `Orchestrator.AttachReplayViewer` — the same flag the replay runtime toggles. The bridge
overload throws `ArgumentNullException` on a null bridge (`Null_bridge_throws`) and **never mutates** the flag
(`Projecting_chrome_does_not_change_viewer_flag`); the two overloads agree
(`Project_from_flag_matches_project_from_bridge`). There are exactly two pre-built singleton results, so projection
allocates nothing.

---

## The two states

| Flag | `IsObserver` | `ModeLabel` | `CssClass` | `CanIssueOrders` | `CanDelegate` | `DisabledReasonCode` | `DisabledReasonText` |
|------|:---:|-------------|-----------|:---:|:---:|-------------|-------------|
| viewer attached | `true` | `OBSERVER · READ-ONLY` | `c2-topbar-item--mode-observer` | `false` | `false` | `REPLAY_ATTACHED` | the observer reason text |
| not attached | `false` | `COMMAND` | `c2-topbar-item--mode-command` | `true` | `true` | `null` | `null` |

The observer state disables **both** order issuance and delegation (assign / take-control / return-to-agent), and
the disabled reason it carries is `C2PlayerCommandBridge.ReasonReplayAttached` with the player-visible
`ReplayAttachedReasonText` ("Observer mode: replay viewer attached — orders and delegation are disabled.").
Clearing the flag restores full command chrome and order issuance
(`Flag_off_restores_command_chrome_and_order_issuance`). The chrome mirrors the real authority: when the unit's
control slot is cleared, `CanDelegate` is `false` *and* `TryTakeDirectControl` actually refuses
(`Viewer_attached_chrome_matches_delegation_authority_refusal`).

### Stable constants

| Constant | Value |
|----------|-------|
| `ObserverLabel` / `CommandLabel` | `OBSERVER · READ-ONLY` / `COMMAND` |
| `ObserverCssClass` / `CommandCssClass` | `c2-topbar-item--mode-observer` / `c2-topbar-item--mode-command` |
| `ReplayAttachedReasonText` | the observer disabled-reason text |

The two USS classes are defined in
[`C2TopBarPanel.uss`](../../unity/ProjectAegis/Assets/UI/TopBar/C2TopBarPanel.uss)
(`Top_bar_stylesheet_defines_observer_and_command_mode_classes`).

---

## The mode label — observer replaces, command preserves

```csharp
string TopBarModeText(string? projectedModeLabel);
```

The top bar already projects a simulation-mode line (`MODE: Mixed`, `MODE: HumanOnly`, …) from
`C2TopBarProjection`. `TopBarModeText` resolves the final text:

- **Observer** chrome **replaces** that line with `OBSERVER · READ-ONLY`, even when a projected label exists
  (`Observer_chrome_replaces_projected_mode_line`) — the read-only state must never be hidden behind a mode string.
- **Command** chrome **keeps** the projected label so `MODE: Mixed` stays visible
  (`Command_chrome_keeps_projected_simulation_mode_line`), falling back to `COMMAND` only when the projected label
  is null or whitespace.

---

## Order issuance — refuse with a visible reason

```csharp
ObserverOrderAttempt TryIssueOrder(DelegationBridge bridge, EntityKey entity, string commandId, double simTime);
```

This is a thin wrapper over
[`C2PlayerCommandBridge.TryIssue`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/C2PlayerCommandBridge.cs)
(CMD-31) that turns a raw success/failure-code into a player-visible `ObserverOrderAttempt`
(`Accepted`, `ReasonCode`, `ReasonText`). On success it returns the `ObserverOrderAttempt.Ok` singleton; on
failure it keeps the structured `ReasonCode` and attaches the text from `DescribeReason`. A null bridge throws
`ArgumentNullException`.

`C2PlayerCommandBridge.TryIssue` is the authority, and it fails **closed** in a fixed order — resolve the command
(`UNKNOWN_COMMAND`) → viewer-attached guard (`REPLAY_ATTACHED`) → registry lookup (`UNKNOWN_UNIT`) → control-slot
check (`NOT_HUMAN_CONTROL`, the unit must already be under `HumanController`; this façade never mutates slots) →
off-grid comms guard (`COMMS_OFF_GRID`, C3-01 / DRG-390) → enqueue (`ENQUEUE_FAILED`). While a viewer is attached
the attempt is refused with `REPLAY_ATTACHED` **and nothing is logged** — no player order lands in the decision log
(`Viewer_attached_order_attempt_returns_false_with_visible_reason_and_logs_nothing`). Non-observer refusals keep
their own codes with visible text (`Non_observer_refusals_keep_their_reason_code_with_visible_text`).

### `DescribeReason` — the reason-code → text table

| `ReasonCode` | Player-visible text |
|--------------|---------------------|
| `REPLAY_ATTACHED` | Observer mode: replay viewer attached — orders and delegation are disabled. |
| `NOT_HUMAN_CONTROL` | Order refused: unit is not under direct control. |
| `UNKNOWN_UNIT` | Order refused: unit not found. |
| `ENQUEUE_FAILED` | Order refused: could not queue order. |
| `UNKNOWN_COMMAND` | Order refused: unknown command. |
| any other code (e.g. `COMMS_OFF_GRID`) | `Order refused: <code>` (generic fallback) |

The generic fallback means a reason code with no dedicated line still surfaces *something* legible rather than a
blank tooltip.

---

## Consumer — the top bar (DRG-413)

The live wiring is [`C2TopBarPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/C2TopBarPanelHost.cs). Per
the host contract (`Top_bar_host_projects_observer_chrome_before_applying_labels`), inside the panel's `Refresh`
— **never** on the `DelegationBridge.Tick` hot path — the host:

1. projects `_observerChrome = ObserverModeChromePresentation.Project(bridgeHost.Bridge)` **before** applying any
   labels (a null bridge projects the command singleton via `RefreshObserverChrome`);
2. sets the mode label to `_observerChrome.TopBarModeText(_presentation.ModeLabel)`, copies `DisabledReasonText`
   into the label tooltip, clears the class list, and adds `_observerChrome.CssClass`;
3. gates the **Begin-Execution** button on `_observerChrome.CanIssueOrders` — disabled with the observer reason as
   its tooltip while observing, and the `OnBeginExecutionClicked` handler re-projects and returns early when
   `CanIssueOrders` is false, so a stale click can't start execution from observer mode;
4. exposes `LastObserverChrome` (DRG-413) for tests/inspection.

The host routes order attempts through `bridgeHost.BeginExecution()` and **never** reads `Bridge.Orchestrator`,
sets `AttachReplayViewer`, or calls `DelegationBridge.Tick` — it only renders and forwards.

---

## Determinism & invariants

- **Read-model, off the fingerprint.** Projected from one bool; writes no log, is not part of the `SimWorldHash`
  or order-log fingerprint. The Baltic v2 hash `17144800277401907079` is untouched.
- **Never sets the flag.** The chrome only *reads* `AttachReplayViewer`; the replay runtime owns writes
  ([replay-checkpoint-observer-runtime.md](replay-checkpoint-observer-runtime.md)).
- **Mirrors real authority.** `CanIssueOrders` / `CanDelegate` track what `C2PlayerCommandBridge.TryIssue` and the
  take-control path will actually permit — the chrome can't claim an order is allowed that the bridge would refuse.
- **Fail-closed, visible refusals.** An observer order attempt is refused with `REPLAY_ATTACHED`, logs nothing, and
  always carries player-visible text; every refusal code maps to a reason string (dedicated or generic fallback).
- **Observer label wins.** Observer chrome always replaces the projected `MODE:` line; command chrome preserves it.
- **Pure, no `UnityEngine` in the record.** `Project` / `TopBarModeText` / `TryIssueOrder` / `DescribeReason` touch
  only their arguments and the two singletons — no RNG, no wall clock, no controller-slot mutation.

---

## Tests (pins)

[`ObserverModeChromePresentationTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/ObserverModeChromePresentationTests.cs)
(NUnit, **12** cases). Highlights:

| Test | Covers |
|------|--------|
| `Viewer_attached_projects_observer_read_only_chrome_with_commands_disabled` | the observer record: label, CSS class, both flags off, `REPLAY_ATTACHED` reason. |
| `Viewer_attached_order_attempt_returns_false_with_visible_reason_and_logs_nothing` | observer order attempt refused + visible text + **nothing logged**. |
| `Viewer_attached_chrome_matches_delegation_authority_refusal` | `CanDelegate=false` tracks a real `TryTakeDirectControl` refusal. |
| `Flag_off_restores_command_chrome_and_order_issuance` | clearing the flag restores command chrome and lets an order land in the log. |
| `Projecting_chrome_does_not_change_viewer_flag` | projection is side-effect free. |
| `Non_observer_refusals_keep_their_reason_code_with_visible_text` | `UNKNOWN_COMMAND` / `NOT_HUMAN_CONTROL` / `UNKNOWN_UNIT` keep their codes + text. |
| `Project_from_flag_matches_project_from_bridge` | the two `Project` overloads agree. |
| `Observer_chrome_replaces_projected_mode_line` / `Command_chrome_keeps_projected_simulation_mode_line` | the `TopBarModeText` replace-vs-preserve rule. |
| `Top_bar_host_projects_observer_chrome_before_applying_labels` | the host projects chrome before labels and never touches the Tick/flag paths. |
| `Top_bar_stylesheet_defines_observer_and_command_mode_classes` | the UXML/USS mode-class contract. |
| `Null_bridge_throws` | both bridge-taking statics null-guard. |

Part of the ≥1638-test baseline.

---

## See also

- [replay-checkpoint-observer-runtime.md](replay-checkpoint-observer-runtime.md) — the enforcement half: the
  `AttachReplayViewer` flag and the human-order / enqueue paths it blocks inside the orchestrator.
- [c2-command-issuance-runtime.md](c2-command-issuance-runtime.md) — the `C2PlayerCommandBridge` / `C2CommandIssuance`
  façade this chrome routes order attempts through, and its reason-code catalog.
- [player-command-issuance.md](player-command-issuance.md) — the broader player-order path this chrome gates.
- [direct-control-override-runtime.md](direct-control-override-runtime.md) — the take-control / return-to-agent
  authority the `CanDelegate` flag mirrors.
- [c2-projection-layer.md](c2-projection-layer.md) — general read-model / presentation-boundary rules;
  [c2-presentation-bridges.md](c2-presentation-bridges.md) — the host-feed pattern the top bar uses.
- [domain-abort-explain-chrome.md](domain-abort-explain-chrome.md) / [weapon-abort-tooltip-chrome.md](weapon-abort-tooltip-chrome.md)
  — sibling read-only presentation-chrome binders over already-projected facts.
