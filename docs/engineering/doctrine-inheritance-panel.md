# Doctrine inheritance panel — ROE resolution & runtime override

> **Subsystem:** `ProjectAegis.Delegation.Projection` (engine-free projection) +
> `ProjectAegis.Delegation.UnityAdapter.Bridge` (headless override command)
> **Decision of record:** [ADR-010 — Headless-first / command-driven UI](../architecture/adr-010-headless-first-command-driven-ui.md)
> **Requirements:** Req 13 (Doctrine ROE/EMCON/WRA) P0

The doctrine inheritance panel shows, for the selected unit, the **effective ROE**, the
**max-salvo** value, and **where those values came from** in the policy inheritance chain.
It also exposes a runtime ROE override. Per ADR-010 the logic is split into a pure C#
projection (testable without Unity) and a thin Unity host that binds that projection to UI
Toolkit elements. The runtime override never mutates the scenario; it goes through the
delegation orchestrator and is recorded in the decision log.

This page documents the behaviour exactly as it exists in source today, including the
distinction between the **scenario-projection path** (read-only, source-aware) and the
**runtime-override path** (orchestrator + decision log), which is the most common point of
confusion.

## Pipeline at a glance

```
ScenarioPolicyProfile.ResolveUnitPolicy(unitId, isFriendly)
        │  unit override > mission ROE > side default
        ▼
DoctrineInheritanceProjection.ProjectUnit(...)   → DoctrineInheritanceEntry  (source-aware)
        ▼
DoctrineInheritancePanelBinder.Bind(entry)       → DoctrineInheritancePanelState  (UI-ready)
        ▼
DoctrineInheritancePanelHost (Unity UI Toolkit)  ← labels, ROE dropdown, Apply button
        │  Apply clicked
        ▼
DelegationBridgeHost.TrySetDoctrineOverride(roe)
        ▼
DoctrineOverrideCommand.TryApply(orchestrator, unitId, roe, simTime)
        │  capture PolicySnapshot + append PolicyUpdateRecord (decision log)
        ▼
orchestrator.ResolveEffectivePolicyForUnit(unitKey) reflects the new ROE
```

## Inheritance resolution (the source of truth)

The effective policy and its provenance come from
`ScenarioPolicyProfile.ResolveUnitPolicy` (`src/ProjectAegis.Sim/Scenario/ScenarioPolicyProfile.cs`).
The implemented precedence — **highest wins** — is:

| Precedence | Source | Condition | `InheritanceSource` label |
|-----------|--------|-----------|---------------------------|
| 1 (highest) | Unit override | `UnitOverrides` contains the unit id | `SOURCE: Unit Override` |
| 2 | Mission ROE | `MissionRoe != null` **and** unit id is in `MissionUnitIds` | `SOURCE: Mission` |
| 3 (lowest) | Side default | otherwise — `FriendlyDefault` / `OpposingDefault` | `SOURCE: Scenario Default` |

`ResolveUnitPolicy` returns a `ResolvedUnitPolicy` (`Effective` policy +
`RoeInheritedFromMission` flag). Only the mission tier sets `RoeInheritedFromMission = true`;
that flag is what drives both the `SOURCE: Mission` label and the override-disable rule
below.

> Req 13's full design target is `unit > embarked > mission > group > side > scenario`.
> The shipped resolver implements the three tiers above; embarked/group tiers are not yet
> modelled in `ScenarioPolicyProfile`. Document and test against the three implemented
> tiers, not the design target.

## The projection: `DoctrineInheritanceEntry`

`DoctrineInheritanceProjection.ProjectUnit`
(`src/ProjectAegis.Delegation/Projection/DoctrineInheritanceProjection.cs`) maps a resolved
policy into a pre-formatted, display-ready record:

| Field | Example | Notes |
|-------|---------|-------|
| `EffectiveRoeLabel` | `ROE: WeaponsTight` | from `Effective.Roe` |
| `EffectiveMaxSalvoLabel` | `SALVO: 2` | from `Effective.MaxSalvo` |
| `InheritanceSource` | `SOURCE: Mission` | see precedence table |
| `IsInheritedFromMission` | `true` | mirrors `RoeInheritedFromMission` |
| `HasLocalOverride` | `false` | `UnitOverrides.ContainsKey(unitId)` |
| `OverrideButtonLabel` | `OVERRIDE: NONE` / `OVERRIDE: ACTIVE` | `ACTIVE` only when a unit override exists |

When `policy == null` every label degrades to an em-dash placeholder and
`OverrideButtonLabel` becomes `OVERRIDE: UNAVAILABLE`. `ProjectAllUnits` returns entries
ordered by unit id with `StringComparer.Ordinal` (deterministic, matching the rest of the
projection layer).

## The binder: `DoctrineInheritancePanelState`

`DoctrineInheritancePanelBinder.Bind`
(`src/ProjectAegis.Delegation/Projection/DoctrineInheritancePanelBinder.cs`) turns an entry
into the final UI state, including the ROE dropdown options and the **override-enabled
rule**:

```
CanOverride = !IsInheritedFromMission || HasLocalOverride
```

In other words, a mission-inherited unit with no local override **cannot** be overridden
from the panel (the Apply button is disabled), while scenario-default units and units that
already carry an override can be. `RoeOptions` is always the three ROE levels
(`HoldFire`, `WeaponsTight`, `WeaponsFree`); the option matching the current effective ROE
is flagged `IsCurrent`. A `null` entry yields the unavailable state with `CanOverride =
false` and no options.

## The runtime override: `DoctrineOverrideCommand`

`DoctrineOverrideCommand.TryApply`
(`src/ProjectAegis.Delegation.UnityAdapter/Bridge/DoctrineOverrideCommand.cs`) is the
headless entry point used by both the Unity host and the PlayMode harness. It:

1. Rejects a `null` orchestrator or an unparseable ROE label (case-insensitive `Enum.TryParse`).
2. Reads the current effective policy via `orchestrator.ResolveEffectivePolicyForUnit`.
3. Returns `false` (no-op) when the requested ROE equals the current ROE — **idempotent**.
4. Otherwise captures a `PolicySnapshot` and appends a `PolicyUpdateRecord`
   (`field = "roe"`, previous → new) to the decision log, then returns `true`.

`simTime` is converted to a `simTick` with `(ulong)Math.Max(0, (long)simTime)`. `MaxSalvo`
is carried over unchanged from the current policy — the command only changes ROE.

### Pitfall: override does not mutate the scenario projection

This is the key gotcha. `TryApply` records the override **in the orchestrator** (snapshot +
decision log); it does **not** add an entry to `ScenarioPolicyProfile.UnitOverrides`.
Consequently:

- `orchestrator.ResolveEffectivePolicyForUnit(unitKey)` reflects the new ROE immediately.
- A fresh `DoctrineInheritanceProjection.ProjectUnit(...)` (which reads
  `Bridge.Orchestrator.ScenarioPolicy`) will **still report the scenario-derived source**,
  because the scenario profile is unchanged.

The Unity host (`DelegationBridgeHost.RefreshDoctrineInheritance`) re-projects from the
scenario policy, so the panel's `SOURCE:` line tracks the *scenario* chain, while the
*effective* ROE used by the simulation tracks the *orchestrator*. Treat the panel source as
"where the scenario says this unit gets its ROE", not "the live override state".

### Pitfall: `CanOverride` is a UI affordance, not an enforcement boundary

`CanOverride` only enables/disables the Apply button. `DoctrineOverrideCommand.TryApply`
itself performs **no** mission-inheritance check — calling it directly on a mission-inherited
unit succeeds and logs a `PolicyUpdateRecord` (see
`PlayModeSmokeHarnessTests.Doctrine_override_round_trip_updates_policy_log_and_projection_bind`,
which overrides a `WeaponsTight` mission unit to `HoldFire`). Any new caller that bypasses
the binder must re-apply its own gating if mission lock is required.

## Unity host wiring

`DoctrineInheritancePanelHost`
(`unity/ProjectAegis/Assets/Scripts/Runtime/DoctrineInheritancePanelHost.cs`) is a
`MonoBehaviour` requiring a `UIDocument`. It queries these named elements from the UXML
tree (root `doctrine-root`):

| Element name | Type | Bound to |
|--------------|------|----------|
| `unit-id-label` | `Label` | `UnitIdLine` |
| `roe-label` | `Label` | `RoeLine` |
| `salvo-label` | `Label` | `SalvoLine` |
| `source-label` | `Label` | `SourceLine` |
| `override-label` | `Label` | `OverrideLine` |
| `roe-dropdown` | `DropdownField` | `RoeOptions` |
| `apply-override-button` | `Button` | `CanOverride` (enabled state) + Apply handler |

The host refreshes every `LateUpdate` by calling `bridgeHost.RefreshDoctrineInheritance()`,
re-binding `LastDoctrineInheritance`, and pushing label/dropdown values. Clicking Apply calls
`bridgeHost.TrySetDoctrineOverride(dropdown.value)`. The whole file is guarded by
`#if UNITY_5_3_OR_NEWER`, so it compiles only inside Unity; all decision logic lives in the
engine-free projection/command types that CI can test headlessly.

## Constraints

- **ADR-010, ZERO touch `DelegationBridge`.** The override seam is `DelegationBridgeHost`
  (`TrySetDoctrineOverride`, `RefreshDoctrineInheritance`, `LastDoctrineInheritance`), not
  the bridge itself. Run GitNexus impact (`impact({target: "DelegationBridge"})`) before any
  edit near this code; it is CRITICAL-risk.
- **Determinism.** Projection ordering uses `StringComparer.Ordinal`; ROE label parsing is
  invariant/case-insensitive. Keep new formatting culture-invariant.
- **No `UnityEngine` in core.** `ProjectAegis.Delegation` projections must stay engine-free;
  Unity-only types belong in the `UnityAdapter`/`unity/` host.

## Verify

```bash
# Engine-free projection + binder (headless, no Unity)
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "DoctrineInheritance" -v minimal

# Override command + PlayMode round-trip harness (headless adapter tests)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "Doctrine|PlayModeSmoke" -v minimal

# Confirm the bridge itself is untouched (ADR-010 zero-touch invariant)
rg "DelegationBridge" unity/ProjectAegis/Assets/Scripts/Runtime/DoctrineInheritancePanelHost.cs
```

## Source map

| Concern | File |
|---------|------|
| Inheritance resolution | `src/ProjectAegis.Sim/Scenario/ScenarioPolicyProfile.cs` (`ResolveUnitPolicy`) |
| Resolved policy + provenance | `src/ProjectAegis.Sim/Policy/ResolvedUnitPolicy.cs` |
| Projection (source-aware) | `src/ProjectAegis.Delegation/Projection/DoctrineInheritanceProjection.cs` |
| UI-ready state + override rule | `src/ProjectAegis.Delegation/Projection/DoctrineInheritancePanelBinder.cs`, `DoctrineInheritancePanelState.cs` |
| Headless override command | `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DoctrineOverrideCommand.cs` |
| Unity bridge seam | `unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs` |
| Unity UI host | `unity/ProjectAegis/Assets/Scripts/Runtime/DoctrineInheritancePanelHost.cs` |
| Tests | `DoctrineInheritanceProjectionTests`, `DoctrineInheritancePanelBinderTests`, `DoctrineOverrideCommandTests`, `PlayModeSmokeHarnessTests` |

## Related runbooks

- [Mission editor CLI / MCP reference](mission-editor-cli-reference.md)
- [Platform editor — Excel round-trip](platform-editor-excel-roundtrip.md)
- [ADR-010 — Headless-first / command-driven UI](../architecture/adr-010-headless-first-command-driven-ui.md)
