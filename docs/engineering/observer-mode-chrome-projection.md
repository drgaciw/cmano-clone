# Observer chrome — read-only vs command top-bar mode (DRG-249 / DRG-413 / MODE-03)

> Canonical overview: [observer-mode-topbar-chrome.md](observer-mode-topbar-chrome.md). This companion preserves the detailed projection and binder reference.

> **ADR:** this is **presentation chrome**, not the observer seam and not an order path
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3,
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md),
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md)). It reads the
> orchestrator's [`AttachReplayViewer`](replay-checkpoint-observer-runtime.md) flag and projects one
> **observer / command chrome** record — *is the player watching a replay (read-only), or in command?* —
> for the C2 top bar's mode label, order buttons, and delegation controls. It **never** sets the viewer
> flag, writes a controller slot, enqueues an order, or touches sim state.

The chrome answers a single command-picture question — *can I give orders right now, and if not, why?* — and
derives it entirely from the viewer flag the [replay runtime](replay-checkpoint-observer-runtime.md) already
owns. It is one pure presentation record plus a thin top-bar host:

1. [`ObserverModeChromePresentation`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/ObserverModeChromePresentation.cs)
   (`ProjectAegis.Delegation.UnityAdapter/Presentation/`) — the record that projects the mode label, USS
   class, and the `CanIssueOrders` / `CanDelegate` flags from the viewer flag, plus the order-attempt and
   reason-text helpers. No `UnityEngine`.
2. [`C2TopBarPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/C2TopBarPanelHost.cs)
   (`unity/ProjectAegis/Assets/Scripts/Runtime/`) — the top-bar `MonoBehaviour` that re-projects the chrome
   each refresh and paints the mode label, tooltip, class, and the Begin-Execution button from it.

The presentation record only **reads** the flag and routes order attempts through
[`C2PlayerCommandBridge.TryIssue`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/C2PlayerCommandBridge.cs);
the actual refusal is enforced by that bridge and the orchestrator — the chrome just surfaces it honestly.

| Piece | File | Role |
|-------|------|------|
| [`ObserverModeChromePresentation`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/ObserverModeChromePresentation.cs) | `UnityAdapter/Presentation/` | The projected record (`IsObserver`, `ModeLabel`, `CssClass`, `CanIssueOrders`, `CanDelegate`, `DisabledReasonCode`, `DisabledReasonText`) + the `Project` / `TryIssueOrder` / `TopBarModeText` / `DescribeReason` helpers. |
| [`ObserverOrderAttempt`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/ObserverModeChromePresentation.cs) | `UnityAdapter/Presentation/` | Outcome of an order attempt (`Accepted`, `ReasonCode?`, `ReasonText?`); the `Ok` singleton is the accepted case. |
| [`C2PlayerCommandBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/C2PlayerCommandBridge.cs) | `UnityAdapter/Bridge/` | The enforced order-issuance facade the chrome routes through; owns the `REPLAY_ATTACHED` / `NOT_HUMAN_CONTROL` / `UNKNOWN_UNIT` / `ENQUEUE_FAILED` reason codes. |
| [`C2TopBarPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/C2TopBarPanelHost.cs) | `unity/.../Runtime/` | Projects the chrome, applies the mode label / class / tooltip, gates Begin Execution, and exposes the last chrome via `LastObserverChrome`. |
| [`C2TopBarPanel.uss`](../../unity/ProjectAegis/Assets/UI/TopBar/C2TopBarPanel.uss) | `unity/.../UI/TopBar/` | The `c2-topbar-item--mode-observer` / `c2-topbar-item--mode-command` tints for the mode label. |

---

## The projection — `ObserverModeChromePresentation`

`Project` reads the viewer flag and returns one of two shared, immutable records — there is no per-call
allocation and no third state:

| Entry point | Reads | Returns |
|-------------|-------|---------|
| `Project(DelegationBridge)` | [`bridge.AttachReplayViewer`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs) (throws `ArgumentNullException` on null) | the chrome for that flag |
| `Project(bool attachReplayViewer)` | the raw flag | `Observer` when `true`, else `Command` |

Both overloads agree (`Project(bridge)` just forwards `bridge.AttachReplayViewer`), and projecting the chrome
**never** writes the flag.

| Field | Observer (`true`) | Command (`false`) |
|-------|-------------------|-------------------|
| `IsObserver` | `true` | `false` |
| `ModeLabel` | `ObserverLabel` = `"OBSERVER · READ-ONLY"` | `CommandLabel` = `"COMMAND"` |
| `CssClass` | `ObserverCssClass` = `"c2-topbar-item--mode-observer"` | `CommandCssClass` = `"c2-topbar-item--mode-command"` |
| `CanIssueOrders` | `false` | `true` |
| `CanDelegate` | `false` | `true` |
| `DisabledReasonCode` | [`C2PlayerCommandBridge.ReasonReplayAttached`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/C2PlayerCommandBridge.cs) = `"REPLAY_ATTACHED"` | `null` |
| `DisabledReasonText` | `ReplayAttachedReasonText` (the player-visible sentence) | `null` |

### The mode label — `TopBarModeText`

`TopBarModeText(projectedModeLabel)` decides what the top bar's mode slot shows:

- **Observer** chrome **replaces** the projected simulation-mode line, so a read-only session always reads
  `OBSERVER · READ-ONLY` instead of `MODE: Mixed`.
- **Command** chrome **keeps** the projected line (`MODE: Mixed`, `MODE: HumanOnly`, …) so the live mode stays
  visible, and falls back to `CommandLabel` only when the projected label is null or whitespace.

### Order attempts — `TryIssueOrder`

`TryIssueOrder(bridge, entity, commandId, simTime)` routes a player order through
`C2PlayerCommandBridge.TryIssue` and wraps the result in an `ObserverOrderAttempt`:

- Accepted → `ObserverOrderAttempt.Ok` (`Accepted: true`, no reason).
- Refused → `(false, code, DescribeReason(code))`, where `code` is the bridge's failure reason (defaulting to
  `ENQUEUE_FAILED`).

While a replay viewer is attached, the bridge's **first** check after resolving the command is the viewer
flag, so the attempt is refused with `REPLAY_ATTACHED` and **nothing is appended** to the order log — the
chrome does not pre-empt the bridge, it relays the bridge's own refusal. Outside observer mode the real
per-order refusals still flow through unchanged (unknown command, unknown unit, not-under-direct-control,
off-grid, enqueue failure).

`DescribeReason(code)` is the single place a refusal code becomes player-visible text:

| Reason code | Visible text |
|-------------|--------------|
| `REPLAY_ATTACHED` | `Observer mode: replay viewer attached — orders and delegation are disabled.` |
| `NOT_HUMAN_CONTROL` | `Order refused: unit is not under direct control.` |
| `UNKNOWN_UNIT` | `Order refused: unit not found.` |
| `ENQUEUE_FAILED` | `Order refused: could not queue order.` |
| `UNKNOWN_COMMAND` ([`C2CommandIssuance.ReasonUnknownCommand`](../../src/ProjectAegis.Delegation/Input/C2CommandIssuance.cs)) | `Order refused: unknown command.` |
| *(any other, e.g. `COMMS_OFF_GRID`)* | `Order refused: {code}` |

---

## The top-bar host — `C2TopBarPanelHost`

The host re-projects the chrome from the live bridge on every `Refresh()` (and on the headless
`ApplyPanelState` path), stores it in `_observerChrome`, and exposes the latest value as
`LastObserverChrome` for headless assertions. From that one record it drives the mode slot and the
Begin-Execution button:

- **Mode label** — `_mode.text = _observerChrome.TopBarModeText(_presentation.ModeLabel)`, the tooltip is the
  `DisabledReasonText` (empty in command mode), and the label's class list is reset to `c2-topbar-item` +
  `_observerChrome.CssClass` so the observer tint applies.
- **Begin Execution** — enabled only when the phase is `Planning` **and** `_observerChrome.CanIssueOrders`;
  while observing, the button is disabled with the refusal sentence as its tooltip. `OnBeginExecutionClicked`
  re-projects the chrome and bails if `CanIssueOrders` is false, so a stale enabled state cannot start
  execution during a replay.

The host calls `bridgeHost.BeginExecution()` (never `Bridge.BeginExecution()` or `Bridge.Orchestrator`), never
assigns `AttachReplayViewer`, and carries no `DelegationBridge.Tick` reference — the source fences in the
tests pin all three.

---

## Consumers

| Surface | Host | Path |
|---------|------|------|
| C2 top bar mode slot + Begin Execution | [`C2TopBarPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/C2TopBarPanelHost.cs) | `ObserverModeChromePresentation.Project(bridgeHost.Bridge)` → `TopBarModeText` / `CssClass` / `DisabledReasonText`; `CanIssueOrders` gates the Begin-Execution button. |
| Order issuance refusal text | any order surface | `TryIssueOrder` / `DescribeReason` turn a `C2PlayerCommandBridge` refusal code into a player-visible sentence. |

---

## Invariants

- **Read-only presentation** — the chrome only reads `AttachReplayViewer`; it never sets the flag, writes a
  controller slot, enqueues an order, or ticks the bridge. Projecting it leaves the flag unchanged (asserted
  by the tests).
- **The chrome surfaces, the bridge enforces** — `TryIssueOrder` routes through
  `C2PlayerCommandBridge.TryIssue`, so a refused order appends nothing to the order log; the chrome relays the
  bridge's `REPLAY_ATTACHED` reason rather than deciding on its own.
- **Observer replaces, command keeps** — observer chrome overrides the mode line with `OBSERVER · READ-ONLY`;
  command chrome preserves the projected `MODE:` line and only falls back to `COMMAND` on an empty label.
- **Colour is never the only signal** — the mode text itself changes (`OBSERVER · READ-ONLY` vs the live
  `MODE:` line) and the disabled button carries a reason tooltip, so observer state survives on a monochrome
  panel; the USS tint is an addition, not the sole cue.
- **Pure / deterministic** — `Project` returns one of two shared singletons; no RNG, no wall clock, no I/O, no
  `UnityEngine` in the record. Same flag ⇒ same chrome.
- **Off the sim fingerprint** — presentation chrome reading a flag the sim already owns; the Baltic v2 replay
  hash is untouched.

---

## See also

| Topic | Doc |
|-------|-----|
| The `AttachReplayViewer` observer flag and the human-order paths it blocks (the enforcement half) | [replay-checkpoint-observer-runtime.md](replay-checkpoint-observer-runtime.md) |
| The player order-issuance seam the chrome routes through | [c2-command-issuance-runtime.md](c2-command-issuance-runtime.md) / [player-command-issuance.md](player-command-issuance.md) |
| General read-model / binder / host rules and the presentation boundary | [c2-projection-layer.md](c2-projection-layer.md) |
| The adapter seam between Unity hosts and projections | [c2-presentation-bridges.md](c2-presentation-bridges.md) |
| Refusal / abort reason codes | [abort-reason-catalog.md](abort-reason-catalog.md) |

---

## Tests

[`ObserverModeChromePresentationTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/ObserverModeChromePresentationTests.cs)
pin the observer vs command chrome fields, the commands-disabled flags and visible reason, the order attempt
that returns `false` and appends nothing to the order log, the chrome-matches-enforced-refusal case, the
flag-off restore, the no-write-on-project guarantee, the non-observer refusal codes keeping their reason text,
the `Project(bool)` / `Project(bridge)` agreement, the observer-replaces / command-keeps mode-label rule, and
the null-bridge throw — plus the `C2TopBarPanelHost` source fences (projects the chrome before applying
labels, uses `bridgeHost.BeginExecution()`, never writes `AttachReplayViewer`, no `DelegationBridge.Tick`) and
the top-bar stylesheet classes. All green (12 tests in `ProjectAegis.Delegation.UnityAdapter.Tests`).
