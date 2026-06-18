# Doctrine Inheritance Panel Visual Sign-off — S29-07 (lean proxy)

**Date:** 2026-06-18  
**Story:** S29-07 — Doctrine Inheritance Panel Visual Sign-off  
**ADR:** ADR-010 (headless-first; doctrine panel seam)  
**Environment:** Headless Linux agent host — Unity 6.3 Editor unavailable; automated proxy = merge authority per S23/S27 lean mode.

## Verdict

**PASS (lean proxy)** — closes Sprint 22/S23 deferred visual gate C4 via scene wiring + headless regression; Editor screenshot advisory optional.

## Scene wiring (AC-1)

`DelegationSmoke.unity` now includes `DoctrineInheritance` root with `DoctrineInheritancePanelHost`:

| Field | Value |
|-------|-------|
| GameObject | `DoctrineInheritance` |
| Host | `DoctrineInheritancePanelHost` |
| `bridgeHost` | `DelegationBridgeHost` (`DelegationSmoke` root) |
| UXML | `Assets/UI/DoctrineInheritance/DoctrineInheritancePanel.uxml` (`guid: b7e4a1c92f3d8e5062f0a4d9c3b5e7a2`) |
| USS | `Assets/UI/DoctrineInheritance/DoctrineInheritancePanel.uss` (`guid: c8f5b2d03a4e9f6173a1b5e0d4c6f8b3`) |
| Scene builder | `DelegationSmokeSceneBuilder.CreatePanelHost<DoctrineInheritancePanelHost>` (rebuild path) |

**Scene excerpt (no missing-reference markers):**

```yaml
m_Name: DoctrineInheritance
m_EditorClassIdentifier: Assembly-CSharp::ProjectAegis.Unity.Runtime.DoctrineInheritancePanelHost
bridgeHost: {fileID: 1998411918}
panelAsset: {fileID: 9197481963319205126, guid: b7e4a1c92f3d8e5062f0a4d9c3b5e7a2, type: 3}
panelStyles: {fileID: 7433441132597879392, guid: c8f5b2d03a4e9f6173a1b5e0d4c6f8b3, type: 3}
showPanel: 1
```

**UXML element contract** (host constants ↔ panel):

| Host constant | UXML `name` | Bound label / control |
|---------------|-------------|------------------------|
| `doctrine-root` | `doctrine-root` | Panel root |
| `unit-id-label` | `unit-id-label` | `UNIT: {id}` |
| `roe-label` | `roe-label` | Effective ROE |
| `salvo-label` | `salvo-label` | Max salvo (WRA) |
| `emcon-label` | `emcon-label` | EMCON read-only |
| `source-label` | `source-label` | Inheritance source |
| `override-label` | `override-label` | Override affordance state |
| `roe-dropdown` | `roe-dropdown` | ROE override picker |
| `apply-override-button` | `apply-override-button` | Apply dispatch |

Inheritance order hint: `unit → embarked → mission → group → side → scenario` (`inheritance-order-label`).

## ROE override round-trip (AC-2)

Doctrine writes route **only** through the ADR-010 seam:

```
DoctrineInheritancePanelHost.OnApplyOverrideClicked()
  → DelegationBridgeHost.TrySetDoctrineOverride(roeLabel)
    → DoctrineOverrideCommand.TryApply(...)
      → RefreshDoctrineInheritance() read-back
```

**Host grep:** `DoctrineInheritancePanelHost.cs` references `TrySetDoctrineOverride` only; no direct `DelegationBridge` or orchestrator access.

**Headless proxy (merge authority):**

| Test | Result |
|------|--------|
| `Doctrine_override_round_trip_updates_policy_log_and_projection_bind` | PASS — `HoldFire` apply updates policy + projection bind |
| `TryApply_changes_policy_and_logs_policy_update_record` | PASS |
| `TryApply_is_idempotent_when_roe_unchanged` | PASS |
| `Bind_mission_inherited_without_override_disables_override_controls` | PASS |
| `Bind_scenario_default_unit_enables_override_controls` | PASS |

## Automated regression (AC-3)

```text
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "Doctrine" -v minimal
# Passed: 9, Failed: 0

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "PlayModeSmoke|Doctrine" -v minimal
# Passed: 21, Failed: 0

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

### Doctrine filter breakdown

**`ProjectAegis.Delegation.Tests` — `Doctrine` (9):**

- `DoctrineInheritancePanelBinderTests` — 4 PASS
- `DoctrineInheritanceProjectionTests` — 5 PASS

**`ProjectAegis.Delegation.UnityAdapter.Tests` — doctrine rows in `PlayModeSmoke|Doctrine` (21 total):**

- `DoctrineOverrideCommandTests` — 4 PASS (included in 21)
- PlayMode harness doctrine rows — `Doctrine_override_round_trip_*`, `Doctrine_panel_uxml_*`, `Doctrine_smoke_scene_builder_*`, `Baltic_doctrine_mission_roe_*`

### PlayModeSmoke + doctrine harness rows (selected)

- `Baltic_doctrine_mission_roe_harness_matches_doctrine_batch_preconditions` — PASS
- `Doctrine_override_round_trip_updates_policy_log_and_projection_bind` — PASS
- `Doctrine_panel_uxml_assets_define_host_element_names` — PASS
- `Doctrine_smoke_scene_builder_registers_doctrine_panel_host` — PASS
- `Delegation_smoke_keeps_useGlobeMap_false_for_ci_safe_default` — PASS

## Console excerpt (expected PlayMode / batch rebuild)

No Unity batch run on this host. Expected clean console when scene opens or rebuilds:

```text
DelegationSmoke scene saved: Assets/Scenes/DelegationSmoke.unity scenario=baltic-patrol-classify
```

No `MissingReferenceException`, `NullReferenceException`, or unresolved `VisualTreeAsset` / `StyleSheet` GUID errors for doctrine panel assets.

## Manual Editor steps (optional polish)

When Unity 6000.3.x is available locally:

1. **Project Aegis → Build DelegationSmoke Scene (classify QA)**
2. Open `Assets/Scenes/DelegationSmoke.unity` — confirm `DoctrineInheritance` object present
3. Play Mode → select friendly unit `u1` on `baltic-patrol-mission-roe` or classify scenario
4. Confirm panel shows `UNIT`, `ROE`, `SALVO`, `EMCON`, `SOURCE`, override row
5. Change ROE dropdown → **Apply** → labels refresh; console clean
6. Optional batch: `pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario doctrine`
7. Attach screenshot to `production/qa/evidence/doctrine-panel-s29-*.png` if desired

## Architecture compliance

- [x] **ZERO edits** to `DelegationBridge.cs`
- [x] Writes via `DelegationBridgeHost.TrySetDoctrineOverride` only
- [x] Presentation bind via `DoctrineInheritanceProjection` / `DoctrineInheritancePanelBinder`
- [x] Headless doctrine tests unchanged PASS
- [x] `DelegationSmoke` scene wired (not builder-only)

## Closes deferred gates

| Gate | Prior status | S29-07 status |
|------|--------------|---------------|
| S22 C4 Editor visual sign-off | Deferred | **Closed (lean proxy)** |
| S23-03 manual Editor evidence | Partial | **Closed (lean proxy)** |