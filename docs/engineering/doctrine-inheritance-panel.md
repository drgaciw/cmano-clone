# Doctrine inheritance panel subsystem

> **Engineering reference + runbook.** Requirement detail lives in [Req 13 — Doctrine, ROE, EMCON, WRA](../../Game-Requirements/requirements/13-Doctrine-ROE-EMCON-WRA.md); the headless-first/command-driven UI rule is [ADR-010](../architecture/adr-010-headless-first-command-driven-ui.md). This page documents how the code behaves today and how to drive it.

The doctrine inheritance panel shows, per selected unit, the **effective rules of engagement (ROE)** and **max salvo (WRA)**, **where that value was inherited from** (scenario default vs mission vs a local override), and whether the operator may override it. It is the UI realisation of Req 13's "visual inheritance chain" — policy is a visible contract, not a hidden modifier.

The subsystem is built **headless-first** (ADR-010): all resolution, projection, and binding are plain `ProjectAegis.*` types covered by NUnit tests; the Unity `MonoBehaviour` is a thin view that only reads pre-computed state and dispatches commands through a bridge seam. `DelegationBridge.cs` is **never** touched.

## Intent

| Goal | How it is met |
|------|---------------|
| Explainable policy | Each unit reports an `InheritanceSource` (`Mission` / `Unit Override` / `Scenario Default`) alongside its effective ROE/salvo |
| No "my weapon won't fire" mystery | The panel surfaces the resolved ROE and its provenance so a hold-fire is attributable to a specific policy layer |
| Headless + CI-testable (ADR-010) | Resolution → projection → binding are pure functions tested without Unity; the panel host only renders state |
| Safe override path | The operator override goes through `DelegationBridgeHost.TrySetDoctrineOverride` → `DoctrineOverrideCommand`, which records to the policy snapshot registry + decision log — never a blind mutation |
| Deterministic | Multi-unit projection is `Ordinal`-sorted; no wall clock (sim time is supplied), so projection feeds golden/PlayMode tests |

## Architecture at a glance

```
ScenarioPolicyProfile.ResolveUnitPolicy(unitId, isFriendly)        ← read path (inheritance resolution)
        │  returns ResolvedUnitPolicy(EffectivePolicy, RoeInheritedFromMission)
        ▼
DoctrineInheritanceProjection.ProjectUnit / ProjectAllUnits         (presentation-only view model)
        │  DoctrineInheritanceEntry(UnitId, ROE label, SALVO label, SOURCE, flags, OVERRIDE label)
        ▼
DoctrineInheritancePanelBinder.Bind(entry)                          (UI-ready lines + ROE dropdown options)
        │  DoctrineInheritancePanelState
        ▼
DoctrineInheritancePanelHost (Unity, UIDocument)                    ← view only; reads bridgeHost.LastDoctrineInheritance
        │  Apply button click
        ▼
DelegationBridgeHost.TrySetDoctrineOverride(roeLabel)               ← write path (ADR-010 command seam)
        ▼
DoctrineOverrideCommand.TryApply(orchestrator, unitId, roe, simTime)
        ├─ orchestrator.PolicySnapshots.Capture(...)                (new policy snapshot)
        └─ orchestrator.DecisionLog.AppendPolicyUpdate(...)         (auditable PolicyUpdateRecord)
```

### Key types

| Type | Location | Responsibility |
|------|----------|----------------|
| `ScenarioPolicyProfile` | `src/ProjectAegis.Sim/Scenario/ScenarioPolicyProfile.cs` | Inheritance root: side defaults, mission ROE + assigned unit set, per-unit overrides. `ResolveUnitPolicy` does the resolution |
| `ResolvedUnitPolicy` | `src/ProjectAegis.Sim/Policy/ResolvedUnitPolicy.cs` | `EffectivePolicy` + `RoeInheritedFromMission` provenance flag |
| `EffectivePolicy` | `src/ProjectAegis.Sim/Policy/EffectivePolicy.cs` | `(RoeLevel Roe, int MaxSalvo = 8)`; `DefaultFree => WeaponsFree` |
| `RoeLevel` | `src/ProjectAegis.Sim/Policy/RoeLevel.cs` | `HoldFire`, `WeaponsTight`, `WeaponsFree` |
| `DoctrineInheritanceProjection` | `src/ProjectAegis.Delegation/Projection/DoctrineInheritanceProjection.cs` | Pure resolution → `DoctrineInheritanceEntry`; `ProjectAllUnits` sorts by unit id |
| `DoctrineInheritancePanelBinder` | `src/ProjectAegis.Delegation/Projection/DoctrineInheritancePanelBinder.cs` | Entry → `DoctrineInheritancePanelState`; builds ROE dropdown + `CanOverride` |
| `DoctrineInheritancePanelState` / `RoeLevelOption` | `src/ProjectAegis.Delegation/Projection/DoctrineInheritancePanelState.cs` | UI-ready record consumed by the host |
| `DoctrineInheritancePanelHost` | `unity/ProjectAegis/Assets/Scripts/Runtime/DoctrineInheritancePanelHost.cs` | UI Toolkit view; wires labels/dropdown/button, refreshes per `LateUpdate` |
| `DelegationBridgeHost` | `unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs` | Seam: `RefreshDoctrineInheritance`, `LastDoctrineInheritance`, `TrySetDoctrineOverride` |
| `DoctrineOverrideCommand` | `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DoctrineOverrideCommand.cs` | Headless override handler: parses ROE, snapshots, logs |

## Inheritance resolution (read path)

`ScenarioPolicyProfile.ResolveUnitPolicy(unitKey, isFriendly)` resolves **most-specific-first** and reports where the value came from:

1. **Unit override** — `UnitOverrides[unitKey]` exists → that `EffectivePolicy`, `RoeInheritedFromMission = false`.
2. **Mission ROE** — `MissionRoe != null` **and** the unit is in `MissionUnitIds` (case-insensitive) → mission policy, `RoeInheritedFromMission = true`.
3. **Side default** — otherwise `FriendlyDefault` (friendly) or `OpposingDefault` (opposing), `RoeInheritedFromMission = false`.

`DoctrineInheritanceProjection.ProjectUnit` maps the result to display labels:

| Field | Value |
|-------|-------|
| `EffectiveRoeLabel` | `"ROE: {Roe}"` (e.g. `ROE: WeaponsTight`) |
| `EffectiveMaxSalvoLabel` | `"SALVO: {MaxSalvo}"` |
| `InheritanceSource` | `SOURCE: Mission` if inherited from mission; else `SOURCE: Unit Override` if a unit override exists; else `SOURCE: Scenario Default` |
| `IsInheritedFromMission` | mirrors `RoeInheritedFromMission` |
| `HasLocalOverride` | `UnitOverrides` contains the unit |
| `OverrideButtonLabel` | `OVERRIDE: ACTIVE` when a unit override exists, else `OVERRIDE: NONE` |

When `policy == null` the projection returns an **"unavailable"** entry (`ROE: —`, `SOURCE: —`, `OVERRIDE: UNAVAILABLE`) rather than throwing.

> **Note on labels vs Req 13.** Req 13 specifies a deeper chain (unit > embarked > mission > group > side > scenario). The **P0 implementation** models three layers (unit override → mission → side default). Group/embarked layers are not yet resolved; do not document them as live.

## Binding (UI-ready state)

`DoctrineInheritancePanelBinder.Bind(entry)`:

- Returns the **unavailable** state (`CanOverride = false`, empty options) for a `null` entry.
- `CanOverride = !IsInheritedFromMission || HasLocalOverride`. Mission-inherited units are **locked** unless they already carry a local override (you cannot silently break the mission contract; you must already have opted out).
- ROE dropdown options are the full `RoeLevel` set (`HoldFire`, `WeaponsTight`, `WeaponsFree`); the option whose label is contained in the current ROE label is marked `IsCurrent`.

## Override (write path)

The Unity host's Apply button calls `DelegationBridgeHost.TrySetDoctrineOverride(roeLabel)`, which delegates to `DoctrineOverrideCommand.TryApply`. The command:

- Returns `false` if the orchestrator is null, the label is not a valid `RoeLevel`, or the unit's **current** ROE already equals the requested level (no-op guard).
- Otherwise builds `new EffectivePolicy(roeLevel, currentPolicy.MaxSalvo)` (salvo preserved), captures a policy snapshot via `orchestrator.PolicySnapshots.Capture`, and appends a `PolicyUpdateRecord` (`field = "roe"`, old → new) to `orchestrator.DecisionLog`.
- Uses the supplied `simTime` (from the snapshot when executing, else `0`) — **no wall clock**.

### Current behavior vs target (important)

The **read** path (`RefreshDoctrineInheritance`) resolves from `orchestrator.ScenarioPolicy`; the **write** path (`DoctrineOverrideCommand`) records to the orchestrator's **policy snapshot registry + decision log**. In P0 these are distinct stores: a runtime override is **captured and audited**, but `ScenarioPolicy.UnitOverrides` is not mutated, so the panel's `SOURCE` / `OVERRIDE` lines continue to reflect scenario-policy inheritance until the unit carries a scenario-level override.

| Aspect | Today (P0) | Target (S23-03 doctrine editor sign-off) |
|--------|------------|------------------------------------------|
| Effective ROE/salvo display | Bound to `ResolvedUnitPolicy` projection | Same |
| `SOURCE` / inheritance chain | Three layers (unit override → mission → side) | Full chain per Req 13 |
| Override persistence | Snapshot + decision-log record via orchestrator | Editor sign-off + PlayMode evidence |
| Panel reflects a just-applied override | Only if it lands in `ScenarioPolicy.UnitOverrides` | Verified end-to-end in Editor |

See `production/epics/sprint-23-platform-phase-b/story-023-03-doctrine-editor.md` for the polish scope.

## Headless-first contract (ADR-010)

- `DoctrineInheritancePanelHost` reads `bridgeHost.LastDoctrineInheritance` and writes labels; it holds **no policy logic**. All decisions live in the tested `Projection`/`Binder`/`Command` types.
- The override is dispatched through the `DelegationBridgeHost` seam, never by reaching into `DelegationBridge`. Story S23-03 marks `DelegationBridge` **CRITICAL / ZERO touch** — grep + GitNexus `impact DelegationBridge --direction upstream` before any nearby edit.
- The panel UXML element names the host queries: `doctrine-root`, `unit-id-label`, `roe-label`, `salvo-label`, `source-label`, `override-label`, `roe-dropdown`, `apply-override-button`. The host is "wired" only when all five labels resolve; until then it no-ops.

## Common pitfalls

- **Override applied but panel unchanged.** Expected in P0 — the command records a snapshot/log entry; it does not write `ScenarioPolicy.UnitOverrides`, which is what the panel reads. Do not treat this as a binder bug.
- **Apply button disabled.** The unit is mission-inherited without a local override (`CanOverride == false`). This is by design — overriding a mission-assigned unit requires it to already be opted out.
- **No-op override returns `false`.** Re-applying the unit's current ROE is rejected by the equality guard; this is not an error.
- **"unavailable" everywhere.** `ScenarioPolicy` is null or no unit is selected (`SelectedUnitId` empty) → `LastDoctrineInheritance` is null → binder returns the `—` / `UNAVAILABLE` state.
- **Touching `DelegationBridge`.** Forbidden by ADR-010 / S23-03. Add behavior in the projection, binder, command, or `DelegationBridgeHost` seam instead.
- **Unsorted multi-unit output.** Use `ProjectAllUnits` (sorts by unit id with `StringComparer.Ordinal`) rather than projecting in arbitrary order — golden/PlayMode tests assume the stable order.

## Where things live

| Path | Content |
|------|---------|
| `src/ProjectAegis.Sim/Scenario/ScenarioPolicyProfile.cs` | Inheritance resolution (`ResolveUnitPolicy`) |
| `src/ProjectAegis.Sim/Policy/{ResolvedUnitPolicy,EffectivePolicy,RoeLevel}.cs` | Policy value types |
| `src/ProjectAegis.Delegation/Projection/DoctrineInheritance*.cs` | Projection, binder, panel-state records |
| `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DoctrineOverrideCommand.cs` | Headless override handler |
| `unity/ProjectAegis/Assets/Scripts/Runtime/DoctrineInheritancePanelHost.cs` | UI Toolkit view |
| `unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs` | Bridge seam for refresh + override |
| `docs/architecture/adr-010-headless-first-command-driven-ui.md` | Headless-first decision record |
| `Game-Requirements/requirements/13-Doctrine-ROE-EMCON-WRA.md` | Requirement + full inheritance model |

## Verify

```bash
# Pure projection + binder (no Unity)
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "Doctrine" -v minimal

# Override command + PlayMode smoke harness
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "Doctrine|PlayModeSmoke" -v minimal

# Confirm the panel host never touches DelegationBridge
rg "DelegationBridge\b" unity/ProjectAegis/Assets/Scripts/Runtime/DoctrineInheritancePanelHost.cs
```
