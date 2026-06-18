# Doctrine inheritance & ROE override runbook

Developer guide for the **doctrine inheritance panel** and the **headless ROE
override command** — how a unit's effective Rules of Engagement (ROE) and max
salvo are resolved from scenario/mission policy, how that resolution is projected
for the C2 doctrine UI, and how an operator override is applied without opening
the Unity Editor. The governing architectural decision is
[`ADR-010`](../architecture/adr-010-headless-first-command-driven-ui.md)
(headless-first, command-driven UI); the source carries the `req 13 P0` tag for
the human-in-the-loop doctrine slice.

Source locations:

- Read / projection (presentation only):
  `src/ProjectAegis.Delegation/Projection/DoctrineInheritanceProjection.cs`,
  `DoctrineInheritancePanelBinder.cs`, `DoctrineInheritancePanelState.cs`.
- Write / command path:
  `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DoctrineOverrideCommand.cs`.
- Underlying policy model:
  `src/ProjectAegis.Sim/Scenario/ScenarioPolicyProfile.cs`,
  `src/ProjectAegis.Sim/Policy/ResolvedUnitPolicy.cs`,
  `EffectivePolicy.cs`, `RoeLevel.cs`.

## Intent

A unit never carries doctrine in isolation — its effective policy is **inherited**
down a fixed precedence chain rooted in the scenario, with a mission layer and a
per-unit override layer on top. The doctrine panel exists to make that
provenance legible ("where did this ROE come from?") and to let an operator
re-task a unit's ROE at runtime through one auditable, deterministic command.

Two halves, deliberately split per ADR-010:

- **Projection (read).** Pure functions turn a `ScenarioPolicyProfile` into
  UI-ready label lines. No mutation, no orchestrator dependency — trivially
  unit-testable and Editor-free.
- **Command (write).** `DoctrineOverrideCommand.TryApply` mutates orchestrator
  runtime state (a new policy snapshot) and appends a `PolicyUpdateRecord` to the
  decision log. The same entry point serves the Unity UI button and any headless
  harness / MCP caller.

## Inheritance precedence

`ScenarioPolicyProfile.ResolveUnitPolicy(unitKey, isFriendly)` resolves the
effective policy in strict order — first match wins:

| Precedence | Source | Condition | `RoeInheritedFromMission` |
|------------|--------|-----------|---------------------------|
| 1 (highest) | **Unit override** | `UnitOverrides` contains `unitKey` | `false` |
| 2 | **Mission ROE** | `MissionRoe != null` and `unitKey ∈ MissionUnitIds` | `true` |
| 3 (fallback) | **Side default** | otherwise | `false` |

The side default is `FriendlyDefault` when `isFriendly`, else `OpposingDefault`
(which itself defaults to `EffectivePolicy.DefaultFree` = `WeaponsFree`). Mission
unit-id matching is case-insensitive (`StringComparer.OrdinalIgnoreCase`); unit
override keys are matched exactly.

`ResolvedUnitPolicy` returns the merged `EffectivePolicy` (`Roe` + `MaxSalvo`)
plus the `HasInheritedDoctrineFromMission` provenance flag the panel reads.

## Projection surface

`DoctrineInheritanceProjection.ProjectUnit` builds a `DoctrineInheritanceEntry`;
`ProjectAllUnits` does the same for a list, sorted by unit id with
`StringComparer.Ordinal` for stable rendering. `DoctrineInheritancePanelBinder.Bind`
then flattens an entry into the `DoctrineInheritancePanelState` the view binds to.

| Field | Value | Notes |
|-------|-------|-------|
| `UnitIdLine` | `UNIT: {id}` | `UNIT: —` when no entry |
| `RoeLine` | `ROE: {Roe}` | e.g. `ROE: WeaponsTight` |
| `SalvoLine` | `SALVO: {MaxSalvo}` | from effective policy |
| `SourceLine` | `SOURCE: Mission` \| `SOURCE: Unit Override` \| `SOURCE: Scenario Default` | provenance |
| `OverrideLine` | `OVERRIDE: ACTIVE` \| `OVERRIDE: NONE` \| `OVERRIDE: UNAVAILABLE` | reflects `UnitOverrides` presence |
| `CanOverride` | `bool` | gates the override controls (see below) |
| `RoeOptions` | 3 × `RoeLevelOption` | `HoldFire`, `WeaponsTight`, `WeaponsFree`; one flagged `IsCurrent` |

When the profile is `null`, the projection returns a fully populated
**placeholder** entry (all `—`, `OVERRIDE: UNAVAILABLE`) rather than `null`, so
the UI always has a renderable state.

### `CanOverride` rule

The binder enables override controls only when:

```
canOverride = !entry.IsInheritedFromMission || entry.HasLocalOverride
```

In other words, a unit currently **inheriting from the mission** has its override
controls *disabled* — mission doctrine is authoritative — unless it already has a
local override (which the operator may then re-edit). Scenario-default and
already-overridden units have controls enabled. A `null` entry yields
`CanOverride = false` and an empty `RoeOptions` list.

## Override write path

`DoctrineOverrideCommand.TryApply(orchestrator, unitId, roeLevelLabel, simTime)`
returns `bool` and is intentionally strict — it returns `false` (no-op) on every
guard rather than throwing:

1. **Validate.** Reject a `null` orchestrator or a `roeLevelLabel` that does not
   parse to a `RoeLevel` (case-insensitive).
2. **Resolve current.** `orchestrator.ResolveEffectivePolicyForUnit(unitKey)`,
   keyed by `OrderActionMapper.TargetIdToUlong(unitId)`.
3. **Idempotency.** If the resolved ROE already equals the requested level,
   return `false` — no snapshot, no log row.
4. **Capture.** Build `new EffectivePolicy(roeLevel, currentPolicy.MaxSalvo)`
   (salvo is preserved), capture it via
   `orchestrator.PolicySnapshots.Capture(...)` at
   `simTick = max(0, floor(simTime))`, getting a new `snapshotId`.
5. **Log.** Append a `PolicyUpdateRecord` (`Field = "roe"`, `PreviousValue`,
   `NewValue`, `PolicySnapshotId`) to `orchestrator.DecisionLog` via
   `AppendPolicyUpdate`.

`ResolveEffectivePolicyForUnit` reads the **latest captured snapshot** for the
unit, so a subsequent resolve reflects the override immediately, and the change
is replay-visible through the C1 policy-update row in the order log.

## Read/write asymmetry — the key pitfall

The projection reads the **static `ScenarioPolicyProfile`**
(`policy.UnitOverrides`, `ResolveUnitPolicy`); the command writes **orchestrator
runtime snapshots** (`PolicySnapshots` + decision log). They are *different
stores*.

Consequence: applying a `DoctrineOverrideCommand` changes what
`ResolveEffectivePolicyForUnit` returns and emits a `PolicyUpdateRecord`, but it
does **not** mutate `ScenarioPolicy.UnitOverrides`. A doctrine panel fed the
scenario profile will still show `SOURCE: Mission` / `OVERRIDE: NONE` for that
unit after a runtime override. If you need the panel to reflect a live override,
project from the orchestrator's resolved policy (or refresh the profile) rather
than assuming the static projection has changed. Do not "fix" this by having the
projection reach into orchestrator state — that would break the presentation-only
boundary ADR-010 relies on.

## Constraints / pitfalls

- **Presentation layer is pure.** `DoctrineInheritanceProjection` /
  `…PanelBinder` must stay side-effect-free and Editor-free. Keep all mutation in
  the command path.
- **Override is ROE-only today.** `TryApply` preserves `MaxSalvo` from the
  current policy; there is no salvo-override verb. Don't assume the command can
  change salvo.
- **No-op returns are normal.** `false` from `TryApply` means "nothing to do"
  (unchanged ROE, unparseable label, null orchestrator), not an error — don't
  surface it as a failure toast.
- **`simTime` floors to a tick.** Negative `simTime` clamps to tick `0`; the
  snapshot tick is `floor(simTime)`. Pass real sim time, not wall-clock.
- **Mission-inherited units are locked by design.** If `CanOverride` is `false`,
  the UI should disable the control — re-tasking mission doctrine is a mission-
  level edit, not a unit override.
- **Determinism.** Both paths are deterministic: projection sorts ordinally and
  the command derives its tick from `simTime`. No `DateTime.UtcNow`, no
  unordered iteration in rendering.

## Tests

- `src/ProjectAegis.Delegation.Tests/Projection/DoctrineInheritanceProjectionTests.cs`
  — precedence (mission inherit, unit override wins, scenario default, null
  placeholder) and label/provenance formatting.
- `src/ProjectAegis.Delegation.Tests/Projection/DoctrineInheritancePanelBinderTests.cs`
  — line mapping, the `CanOverride` rule, ROE-option `IsCurrent` flagging, and the
  unavailable state.
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DoctrineOverrideCommandTests.cs`
  — apply changes policy + logs a record, idempotent when unchanged, rejects bad
  labels, rejects null orchestrator.
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs`
  — end-to-end headless flow against the `baltic-patrol-mission-roe` scenario:
  mission-inherited panel (`CanOverride == false`), apply `HoldFire`, confirm the
  resolved ROE and the logged `PolicyUpdateRecord`, second apply is a no-op.

Run the focused suites:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter DoctrineInheritance
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter DoctrineOverrideCommand
```

## Related docs

- [`ADR-010`](../architecture/adr-010-headless-first-command-driven-ui.md) —
  headless-first, command-driven UI boundary that splits projection from command.
- [`catalog-write-gate-runbook.md`](catalog-write-gate-runbook.md) — the same
  propose/apply + audit-log discipline applied to catalog mutations.
