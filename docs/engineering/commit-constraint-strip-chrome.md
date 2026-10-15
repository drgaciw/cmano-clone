# Commit constraint strip & refuse/drop bark chrome — the fire-commit "can I? / why not?" seam (S124-01 / W2-C2-01 + S124-03 / W2-C2-04)

> **ADR:** this is the **presentation / read-model seam**
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3
> — chrome binds already-projected facts, it is **not** sim authority), with
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md) (C2 presentation) and
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md) (the adapter boundary).

Two sibling binders cover the **fire-commit moment** on the attack menu. The **constraint strip** answers *can I
commit this option right now, what will it cost my magazine, and if not, why?* — a top-N blocking/advisory reason
line plus a magazine cost line. The **refuse/drop bark** answers *the commit didn't become a launch — say so
visibly, with the same stable reason code the message log carries.* Both are pure, advisory binders that fold
**already-projected** engage facts; neither re-derives sim truth, issues a fire order, or appears in the Baltic v2
`SimWorldHash`.

There is **no Unity host yet** — both are tests-only, contract-complete building blocks. UXML element names and USS
cue classes are a published contract a future attack-menu host paints against (sibling pattern:
[c2-presentation-bridges.md](c2-presentation-bridges.md)).

| File | Role |
|------|------|
| [`CommitConstraintStripBinder.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/CommitConstraintStripBinder.cs) | the strip: `CommitConstraintStripBinder.Bind` / `BindRows`, `CommitConstraintStripState`, `CommitConstraintEntry`, the source/cue/declutter catalogs |
| [`CommitRefusalBarkBinder.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/CommitRefusalBarkBinder.cs) | the bark: `Refused` / `TryDropped` / `ProjectDropped` / `BindRow`, `CommitRefusalBark`, `CommitBarkKind` |

---

## The constraint strip (S124-01 / W2-C2-01)

```csharp
CommitConstraintStripState Bind(
    string optionId,
    in EngageContext engageContext,
    EngagePreview preview,
    AttackOptionsPreviewState? attackMenu = null,
    WraSalvoRemainingState? wra = null,
    WeaponAbortTooltipState? abortTooltip = null);
```

`Bind` resolves the strip for one attack-menu `optionId` (`fire-single`, `fire-salvo`, or `hold-fire`). A null
`optionId` or `preview` throws `ArgumentNullException`. When `attackMenu` is null it is rebuilt from
`EngageAttackOptions.Build(in engageContext, preview)` — the **same** context and preview the order resolver uses —
so the strip can never disagree with the live menu.

### Can-commit & the attack-menu row

The strip mirrors the menu's own `Enabled`/`AbortReason` for the chosen option:

- **`hold-fire`** short-circuits: always committable, zero cost, no constraints — even when fire is blocked
  (`Hold_fire_commit_has_zero_cost_and_no_constraints_even_when_fire_is_blocked`).
- An **unknown** option (no matching menu row) is blocked with `UNKNOWN_OPTION` and zero cost
  (`Unknown_option_is_blocked_with_unknown_option_code_and_zero_cost`) — the same code the
  [`EngageAttackOrderResolver`](../../src/ProjectAegis.Delegation/Projection/EngageAttackOrderResolver.cs) refuses with.
- Otherwise `CanCommit = row.Enabled`; a disabled row's `AbortReason` becomes `PrimaryReasonCode`, which
  **equals** the façade's own refusal code (`Primary_reason_matches_command_facade_refusal_code` pins this against
  `EngageAttackOrderResolver.TryResolve`).

### Constraint merge — fixed source order, dedup, top-3

Constraints are gathered in a fixed precedence and capped at `MaxConstraints` (**3**), deduplicated ordinally by
code (first occurrence wins its rank). The source order (`CommitConstraintSources`) is:

| Rank source | Code from |
|-------------|-----------|
| `attack-option` | the menu row's disabled `AbortReason` (or `UNKNOWN_OPTION`) |
| `engage-preview` | `EngagePreview.AbortPreviewCode` |
| `wra-salvo` | DRG-259 `WraSalvoRemainingState.AbortReasonCode` (`WRA_SALVO`) |
| `abort-tooltip` | DRG-258 `WeaponAbortTooltipState.Reasons` (only when `IsDenied`) |

Each kept constraint is a `CommitConstraintEntry(Rank, Code, Source)` with `Rank` running `1, 2, 3`
(`Constraints_merge_sources_in_fixed_order_dedupe_and_cap_at_top_n`). A constraint is **not** necessarily blocking:
a committable fire can still carry an advisory `WRA_SALVO` row and stay `CanCommit = true`
(`Enabled_commit_with_advisory_wra_constraint_stays_committable`).

### The two text lines

`ConstraintLine` reads `COMMIT: READY` or `COMMIT: BLOCKED`, then each constraint code appended as ` | <code>`
(e.g. `COMMIT: BLOCKED | NO_FIRE_CONTROL_TRACK | WRA_SALVO | WeaponsTight`).

`CostLine` reads `COST: <n> rd | MAG <before> -> <after>`, with a ` (short <k>)` suffix when the cost exceeds the
rounds remaining. Cost is **1** for `fire-single`, `Math.Max(1, SalvoSize)` for `fire-salvo`, and **0** otherwise;
`RoundsAfter` is clamped at `0`. Example: a 2-round salvo against 1 remaining round →
`COST: 2 rd | MAG 1 -> 0 (short 1)` and `NO_AMMO` first in the blocked line
(`Short_magazine_salvo_is_blocked_with_no_ammo_first_and_shortfall_in_cost`).

### Cue classes & declutter tokens (never color-only)

| State | `CueClass` | `DeclutterToken` |
|-------|-----------|------------------|
| no option armed (`Empty`) | `commit-strip-cue--empty` | `[COMMIT:NONE]` |
| committable | `commit-strip-cue--ready` | `[COMMIT:READY]` |
| blocked | `commit-strip-cue--blocked` | `[COMMIT:BLOCKED]` |

The paired text token means a host never signals state by color alone. `CommitConstraintStripCueClasses.All` is the
list a host clears before applying the active cue. `CommitConstraintStripState.Empty` is the cleared placeholder
(blocked, no constraints, `[COMMIT:NONE]`).

### `BindRows` — the host paint contract

`BindRows(state)` maps the state onto exactly two `CommitConstraintStripRow`s, both carrying the state's `CueClass`:

| `ElementName` | Text |
|---------------|------|
| `commit-strip-constraints` | `ConstraintLine` |
| `commit-strip-cost` | `CostLine` |

`IsFireOrder` on the state is hard-wired `false`.

---

## The refuse/drop bark (S124-03 / W2-C2-04)

A `CommitRefusalBark` says *why a commit did not become a launch* and carries a `MessageLogLine` with the same
stable reason code. Two kinds (`CommitBarkKind`):

- **`Refused`** — the command façade rejected the commit before any order was issued (presentation-local; no
  order-log entry exists).
- **`Dropped`** — the order was accepted but the order log later recorded an engage abort or policy denial.

### Refused barks

```csharp
CommitRefusalBark Refused(string? unitId, string optionId, string? failureReason, double simTime);
CommitRefusalBark Refused(string? unitId, CommitConstraintStripState strip, double simTime);
```

`failureReason` is the façade's out-reason verbatim; a null/blank reason maps to `ENQUEUE_REJECTED`
(`Refused_without_facade_reason_uses_enqueue_rejected_code`). The strip overload reuses `strip.OptionId` and
`strip.PrimaryReasonCode` (`Refused_from_strip_uses_strip_primary_reason`). `BarkText` is `"<unit> REFUSED <option>:
<code>"` (unit omitted when absent). The `LogLine` is **presentation-local**: category `COMMIT_REFUSED`, text
`"Commit refused[ for <unit>]: <code> (<option>)"`, and **`SequenceId = 0`** — it never claims an order-log
sequence (`Refused_commit_produces_visible_bark_and_log_line_with_same_stable_code`).

### Dropped barks

```csharp
bool TryDropped(OrderLogEntry entry, out CommitRefusalBark? bark);
IReadOnlyList<CommitRefusalBark> ProjectDropped(IReadOnlyList<OrderLogEntry> entries, string? unitId = null);
```

`TryDropped` matches two order-log payloads and reuses
[`MessageLogProjection`](../../src/ProjectAegis.Delegation/Projection/MessageLogProjection.cs) so the bark's
`LogLine` **is** the projected message-log row (`bark.LogLine == MessageLogProjection.Project([entry]).Single()`):

| Payload (and `Kind`) | `ReasonCode` | `UnitId` |
|----------------------|-------------|----------|
| `EngagementRecord { Launched: false }` (`Engagement`) | `AbortReasonCode`, or `UNSPECIFIED` when blank | `ShooterTargetId.Value` |
| `PolicyDenialRecord` (`PolicyDenial`) | `Reason.ToString()` (the `FireAbortReason` name) | `TargetId.Value` |

A **launched** engagement, or any other payload, returns `false` with a null bark
(`Launched_engagement_is_not_a_drop`). `BarkText` is `"<unit> DROPPED: <code>"`, cue class `commit-bark-cue--dropped`.
`ProjectDropped` folds a log in order, optionally filtered to one unit
(`Project_dropped_keeps_log_order_and_filters_by_unit`).

### Cue classes & the host paint contract

| Kind | `CueClass` |
|------|-----------|
| `Refused` | `commit-bark-cue--refused` |
| `Dropped` | `commit-bark-cue--dropped` |

`CommitRefusalBarkCueClasses.All` is the clear-first list. `BindRow(bark)` maps onto one `CommitRefusalBarkRow`
(`commit-bark-line`, `BarkText`, `CueClass`). `IsFireOrder` is hard-wired `false`.

---

## The ADR-010 fence (S124-02 / W3-C2-02)

[`CommitStripProjectionFenceTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/CommitStripProjectionFenceTests.cs)
reflection-pins the presentation boundary on **both** binders:

- **No sim/session reach.** Neither source may contain `SimulationSession`, `DelegationOrchestrator`,
  `DelegationBridge`, `ISimWorldSnapshot`, `IOrderSink`, `ApplyOrder`, `TryEnqueue`, `HumanController`,
  `DecisionLog`, `Append`, `CatalogWriteGate`, `MvpEngagementResolver`, `PolicyEvaluator`, `BuildLiveEngageContext`,
  `UnityEngine`, `Random`, or `DateTime`.
- **Projection DTOs only.** Every public bind parameter must come from `System`, `System.Collections.Generic`,
  `ProjectAegis.Delegation.Projection`, or `…UnityAdapter.Presentation` — the only foreign types allowed are the
  read-only `EngageContext` and `OrderLogEntry`.
- **Stateless statics.** Both are static classes with no mutable static fields.
- **Never fire orders.** Every bound state carries `IsFireOrder = false`.

---

## Determinism & invariants

- **Advisory, off the fingerprint.** Both fold already-projected facts; neither writes the sim log, re-evaluates a
  gate, or participates in the `SimWorldHash` / order-log fingerprint. The Baltic v2 hash `17144800277401907079` is
  untouched.
- **Mirrors real authority.** The strip's `CanCommit` / `PrimaryReasonCode` track the attack-menu row and the
  order resolver's refusal code — the strip can't claim a commit is allowed the façade would refuse.
- **One reason, two surfaces.** A dropped bark reuses `MessageLogProjection`, so the bark code and the message-log
  row always agree; a refused bark is explicitly presentation-local (`SequenceId 0`, `COMMIT_REFUSED`).
- **Never color-only.** Every cue class is paired with a text declutter token or an explicit text line.
- **Deterministic.** Identical inputs produce identical lines, costs, and constraint lists
  (`Binding_is_deterministic_for_identical_inputs`); no RNG, no wall clock.

---

## Tests (pins)

NUnit, part of the ≥1638-test baseline:

- [`CommitConstraintStripBinderTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/CommitConstraintStripBinderTests.cs)
  — **12** cases (ready/single/salvo cost, short-magazine shortfall, façade-code parity, source-order dedup/cap,
  advisory-but-committable, supplied-menu, unknown option, `BindRows`, `Empty`).
- [`CommitRefusalBarkBinderTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/CommitRefusalBarkBinderTests.cs)
  — **10** cases / 12 with the parameterized `[TestCase]` rows (refused code + presentation-local log line,
  blank→`ENQUEUE_REJECTED`, no-unit, strip overload, dropped engagement/policy-denial parity with the message log,
  `UNSPECIFIED`, launched-is-not-a-drop, ordered/unit-filtered `ProjectDropped`, `BindRow`).
- [`CommitStripProjectionFenceTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/CommitStripProjectionFenceTests.cs)
  — **4** methods asserting the ADR-010 fence (forbidden tokens, projection-only params, stateless statics,
  never-a-fire-order).

---

## See also

- [weapon-abort-tooltip-chrome.md](weapon-abort-tooltip-chrome.md) — the DRG-258 abort-tooltip the strip folds in as
  its lowest-precedence constraint source; same read-only presentation discipline.
- [engage-explain-contract-projection.md](engage-explain-contract-projection.md) /
  [engagement-pipeline.md](engagement-pipeline.md) — the engage-time projections and gate that produce the preview
  codes and abort reasons the strip surfaces.
- [combat-event-log-projection.md](combat-event-log-projection.md) — the order-log engagement/policy-denial rows a
  dropped bark reads through `MessageLogProjection`.
- [c2-command-issuance-runtime.md](c2-command-issuance-runtime.md) — the player-order façade whose refusal codes a
  refused bark reports.
- [c2-projection-layer.md](c2-projection-layer.md) — general read-model / presentation-boundary rules;
  [c2-presentation-bridges.md](c2-presentation-bridges.md) — the host-feed pattern a future attack-menu host uses.
