# S122-09 / DRG-183 — Slice A targetability harness

**Story:** S122-09 / [DRG-183](https://linear.app/drgamtd-workspace/issue/DRG-183)
**Sprint:** S122 Slice A (`production/sprints/sprint-122-slice-a-unity-mcp.md`)
**AC:** Scenario + AC harness documented; **not a new sim**
**Headless owner:** DRG-219 `TargetabilityAcceptProjection`
**Req:** KCX-01 composition + KCX-04 fire-control verification (`Game-Requirements/drafts/23-Kill-Chain-Explainability.md`)

QA/agents run the existing headless composition tests. Do not invent a Unity scene, UIDocument, or sim.

---

## 1. What the harness is

`TargetabilityAcceptProjection` (namespace `ProjectAegis.Delegation.TargetabilityAccept`) is the **headless, replay-stable** Slice A acceptance snapshot. It does not tick the sim, mutate orders, or own Unity.

| Item | Path |
|------|------|
| Projector | `src/ProjectAegis.Delegation/TargetabilityAccept/TargetabilityAcceptProjection.cs` |
| Types / cause codes | `src/ProjectAegis.Delegation/TargetabilityAccept/TargetabilityAcceptTypes.cs` |
| Tests (the harness) | `src/ProjectAegis.Delegation.Tests/TargetabilityAccept/TargetabilityAcceptProjectionTests.cs` |
| Fixture class | `TargetabilityAcceptProjectionTests` |

`Project(DecisionLog, currentSimTick, C2AuthorityProjectionContext, …)` builds child snapshots then composes. `Project(ContactProvenanceSnapshot, SensorToShooterSnapshot, C2AuthorityProjection)` composes already-projected children (no reproject). Null log or null authority → `TargetabilityAcceptSnapshot.Empty`.

Contacts are indexed by `ContactId` (ordinal). Each `TargetabilityAcceptContactRow` carries `Disposition` (`Permitted` / `Withheld`), a named `WithheldCauseCode`, and the three child payloads.

---

## 2. Composition (provenance + sensor-to-shooter + authority)

`ResolveDisposition` is **fail-closed**. First matching rule wins; withheld rows never use empty / `None`.

| Order | Child | Withheld when | Cause code |
|------:|-------|---------------|------------|
| 1 | Provenance | row missing (chain-only) | `MissingProvenance` |
| 2 | Provenance | `QualityState` has `CatalogMiss` | `CatalogMiss` |
| 3 | Provenance | `Freshness == Stale` | `Stale` |
| 4 | Provenance | `SilentComms` **and** `OutOfCommsUnknown` | `SilentComms` |
| 5 | Sensor-to-shooter | chain null or `!IsComplete` | mapped from `PrimaryBreakCause` |
| 6 | Authority | `Targeting.Disposition` is `Withheld` or `ApprovalRequired` | `Targeting.ReasonCode` (fallback `ApprovalRequired`) |
| — | all pass | — | `Permitted` + `None` |

S2S break-cause map (`FormatSensorToShooterCause`):

| `SensorToShooterBreakCause` | `TargetabilityAcceptCauseCodes` |
|-----------------------------|----------------------------------|
| `LostSensor` | `LostSensor` |
| `StaleTrack` | `StaleTrack` |
| `NoFireControl` | `NoFireControl` |
| `NoEligibleShooter` | `NoEligibleShooter` |
| `DegradedTrack` | `DegradedTrack` |
| other | `StaleTrack` |

Authority ROE withhold uses `C2AuthorityProjector` reason strings. `WeaponsTight` is `C2AuthorityProjector.ReasonWeaponsTight` (`nameof(FireAbortReason.WeaponsTight)`). Provenance owns the comms-display stale divisor; `SensorToShooterProjection` is not passed `commsDisplay`.

---

## 3. How to run

From repo root. `.NET 8.0.400` via `DOTNET_ROOT` (see `global.json` / `AGENTS.md`).

```bash
DOTNET_ROOT=$HOME/.dotnet PATH="$HOME/.dotnet:$PATH" \
  dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "FullyQualifiedName~TargetabilityAccept"
```

Expect the `TargetabilityAcceptProjectionTests` fixture green. No Unity Editor, no Play Mode, no `:8080`.

---

## 4. Acceptance criteria → existing tests

| AC path | Disposition | Cause | Test method |
|---------|-------------|-------|-------------|
| Permitted | `Permitted` | `None` | `Permitted_path_fresh_chain_complete_and_authority_allows_targeting` |
| Stale withheld | `Withheld` | `Stale` | `Withheld_stale_provenance_names_stale_cause` |
| No FC withheld | `Withheld` | `NoFireControl` | `Withheld_no_fire_control_names_no_fc_from_sensor_to_shooter_chain` |
| ROE withhold | `Withheld` | `WeaponsTight` | `Withheld_weapons_tight_names_roe_cause_from_authority_projection` |

Same class (not the four AC rows, but same filter): `Withheld_catalog_miss_names_catalog_miss_from_provenance`, `Withheld_rows_never_use_empty_cause_code`, `Identical_inputs_yield_identical_fingerprint`, `Empty_log_yields_empty_snapshot_and_empty_fingerprint`, `Child_snapshot_overload_composes_without_reprojecting`, `Withheld_degraded_comms_display_divisor_names_stale_when_default_would_be_fresh`, `Child_snapshot_null_provenance_with_complete_chain_fails_closed_missing_provenance`.

KCX-04 FC check is the No FC row: `fireControl: null` → incomplete chain, `PrimaryBreakCause = NoFireControl`.

---

## 5. Unity bind status (no new UIDocument)

Slice A chain **UI is already bound** (S122-02 / DRG-181):

- Host: `unity/ProjectAegis/Assets/Scripts/Runtime/SensorToShooterPanelHost.cs`
- Apply: `SensorToShooterApplyState` → `SensorToShooterPresentation` label bags (`CONTACT:`, `CHAIN:`, `CAUSE:`, four links, `EXPLAIN: engage/{id}`)
- UXML: `Assets/UI/SensorToShooter/SensorToShooterPanel.uxml`
- Play Mode evidence: `production/qa/evidence/s122-02/sensor-to-shooter-chain.png` (scene `DelegationSmoke.unity`)

**DRG-183 does not add a UIDocument, panel host, or scene.** There is no `TargetabilityAccept*` Unity type. Permitted vs withheld cause codes are proven by the headless tests above; Play Mode only shows the existing sensor-to-shooter chain, not a new acceptance HUD.

---

## 6. Non-goals

- **Not a new sim.** No scenario runner, no Baltic replay, no new `.unity`.
- **No `DelegationBridge.Tick`** and no hotpath edits to `DelegationBridge` / `DelegationBridgeHost`.
- **No Slice C** ([DRG-185](https://linear.app/drgamtd-workspace/issue/DRG-185)).
- **No CMD-32 / CMD-34 rebuild** (`TacticalOverlayProjection` / `DatalinkPictureProjection`).
- Do not re-implement `SensorToShooterPanelHost` or `SensorToShooterApplyState`.
- Do not invent a targetability panel or color-only cues.

Shot-list pointer: `production/qa/evidence/s122-visual-shotlist.md` (S122-09 row).
