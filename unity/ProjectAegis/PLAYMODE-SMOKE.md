# Play Mode smoke checklist

## One-time setup

From repo root:

```powershell
./tools/Test-UnityPluginAssemblies.ps1   # fails fast if plugins missing
./tools/copy-delegation-assemblies.ps1   # if guardrail fails
./tools/unity/Invoke-DelegationSmokeSceneSetup.ps1   # optional: batchmode compile + DelegationSmoke.unity
```

Open `unity/ProjectAegis` in **Unity Hub 6.3 LTS** (6000.3.x).

## Scene setup (recommended stack)

1. Create empty GameObject `DelegationSmoke`.
2. Add **DelegationBridgeHost** (`globalSeed` = 42, `scenarioPolicyId` = `baltic-patrol`).
3. Add **SimplePlayModeSimHost** on the same object.
4. **Top:** `C2TopBarPanelHost` — `Assets/UI/TopBar/C2TopBarPanel.uxml` / `.uss` (TIME − / + / PAUSE)
4b. **Toast:** `AttentionToastPanelHost` — `Assets/UI/AttentionToast/AttentionToastPanel.uxml` / `.uss`
5. **Left:** `C2LeftDrawerPanelHost` — `Assets/UI/C2LeftDrawer/C2LeftDrawerPanel.uxml` / `.uss`
6. **Center:** `MapPlaceholderPanelHost` — `Assets/UI/MapPlaceholder/MapPlaceholderPanel.uxml` / `.uss`
7. **Right:** `RightUnitPanelHost` — `Assets/UI/UnitDetail/UnitDetailPanel.uxml` / `.uss`
8. **Bottom:** `MessageLogPanelHost` — `Assets/UI/MessageLog/MessageLogPanel.uxml` / `.uss`
9. Wire each host's `bridgeHost` to the same **DelegationBridgeHost**.
10. **Panel Settings (required):** every `UIDocument` must reference a `PanelSettings` asset (shared `Assets/UI/C2RuntimePanelSettings.asset`). Without it, `rootVisualElement` stays null and Game view is empty sky only. Runtime fallback: `UiDocumentPanelSettingsBootstrap`. Editor fix: menu **Project Aegis → Fix UIDocument PanelSettings** or batch `-executeMethod ProjectAegis.Unity.Editor.DelegationSmokeSceneBuilder.FixPanelSettingsBatch`.
11. Optional scenarios on bridge: `baltic-patrol-mission`, `baltic-patrol-classify`, `baltic-patrol-comms` (COMMS top bar + denials), `baltic-patrol-mission-roe` (doctrine inheritance panel).
12. Enter **Play Mode** — top bar shows sim time + score; map shows ■/◆ symbols; drawer tabs work; no bridge errors.

## S121 / DRG-162 — overlay signoff (owner Game View)

Use default `scenarioPolicyId` = `baltic-patrol` (or `baltic-patrol-comms` to exercise amber/grey edges). Smoke ORBAT seeds `u1` + `hostile-1`; first friendly (`u1`) is selected on Start.

| Step | Expected |
|------|----------|
| Play starts, `u1` selected | Map HUD `ENVELOPES: 2` and `DATALINKS: 1` (Wave 2 counts) |
| Selected unit rings | Blue **sensor** ring (outer) + red **weapon** ring (inner), circular on a non-square Game View |
| Datalink | One edge between the two OOB symbols — green (Nominal), amber (`baltic-patrol-comms` Degraded), grey (Denied) |
| Click the other ■ | Rings move to the new selection; `ENVELOPES` stays `2`; `DATALINKS` stays `1` |
| No console errors | Bridge tick continues; overlay layers do not steal symbol clicks |

Headless gate (no Editor): `dotnet test … --filter PlayModeSmokeHarnessTests` plus `MapCanvasOverlayGeometryTests` / `Drg162OverlaySignoffProjectionTests`. Visual pixels remain owner-only.

## Sprint 7–9 — COMMS + fuel QA (manual)

Use `scenarioPolicyId` = `baltic-patrol-comms`.

| Step | Expected |
|------|----------|
| ~2 s Play | Top bar `COMMS: DEGRADED` (amber); hostile ◆ faded + italic ghost offset |
| ~4 s Play | `COMMS: DENIED` (red); all map symbols dimmer |
| Message log | Purple-bold `COMMS` lines for state transitions |
| After DENIED | Policy/CommsDenied lines; no new launches |
| ~100 s sim (burn model) | Unit detail `FUEL: JOKER` with kg readout |
| Speed fuel check | Set `SimplePlayModeSimHost.simTimeStep` = `1.0` |

Full checklist: `production/qa/c2-manual-signoff-2026-06-02.md`  
Headless gate first: `tools/unity/Invoke-ManualQaHeadlessGate.ps1`

## Sprint 22–25 — doctrine QA (manual)

Use `scenarioPolicyId` = `baltic-patrol-mission-roe`.

| Step | Expected |
|------|----------|
| Play starts | First friendly unit selected; doctrine panel shows `WeaponsTight` ROE from mission |
| Doctrine panel | `EMCON:` line populated; `SOURCE:` includes `Mission`; inheritance hint visible |
| Override (when enabled) | ROE dropdown + apply updates policy log; duplicate apply rejected |
| No console errors | Bridge tick continues; map/OOB selection still syncs |

## Sprint 6 — selection QA (manual)

Use `scenarioPolicyId` = `baltic-patrol-classify` for hostile contacts on map.

| Step | Expected |
|------|----------|
| Play starts | First alive friendly unit selected; OOB row highlighted; map ■ has gold ring |
| Click another ■ on map | OOB highlight moves; right panel `UNIT:` matches clicked id |
| Click OOB row `u2` | Map selection ring moves; unit detail updates |
| Click ◆ hostile symbol | Right panel shows `CONTACT:` line; unit lines show `—` or contact-only |
| Click CONTACTS tab row | Same as hostile map click |
| No console errors | Bridge tick continues; message log still appends |

## CMD-39 Track A — attention toast + interactive clock (2026-08-17)

Rebuild the smoke scene so the toast host is present: menu **Project Aegis → Build DelegationSmoke Scene (comms QA)** or **Ensure UI Maturity Hosts (open scene)**.

| Step | Expected |
|------|----------|
| Play starts | Top-right toast: `WATCH · PAUSE` (demo hostile contact); sim clock pauses (`TIME: PAUSED`) |
| ACK | Toast dismisses; **RESUME** on the top bar becomes enabled |
| RESUME | Clock label returns to `TIME: 1x`; stub host ticks again |
| − / + | Compression walks 1x → 2x → 4x → 8x (session `SetTimeAccelerationFactor`; label is not a static `TIME: 1x`) |
| Unacked pause-class | RESUME stays disabled (`WatchAutoPauseGate.CanResume`) |

Headless: `dotnet test … --filter "AttentionToastApplyStateTests|C2ClockCommandTests|AttentionToastHostContractTests"`.

## Automated headless gate (no Editor)

```powershell
dotnet test ProjectAegis.sln --filter "PlayModeSmokeHarnessTests|ReplayGolden"
```

Mirrors the host loop in `PlayModeSmokeHarnessTests.cs`.

## S19-01 check 1 — batch Play Mode console gate (Unity Editor)

When Unity **6000.3.14f1** is installed locally:

```powershell
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario comms
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario classify -SkipBuild
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario doctrine -SkipBuild
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario import -SkipBuild
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario begin-execution -SkipBuild
```

Opens `DelegationSmoke.unity`, enters Play Mode in batchmode, and fails on game console errors. Evidence: `unity-c2-playmode-signoff.log`. Full checklist: `production/qa/c2-manual-signoff-2026-06-02.md`. S30-06 presentation evidence README: `production/qa/evidence/README-presentation-evidence-s30.md`.

| Scenario | `-executeMethod` | Policy id |
|----------|------------------|-----------|
| `comms` | `C2PlayModeSignoffBatchRunner.RunBatch` | `baltic-patrol-comms` |
| `classify` | `C2PlayModeSignoffBatchRunner.RunClassifyBatch` | `baltic-patrol-classify` |
| `doctrine` | `C2PlayModeSignoffBatchRunner.RunDoctrineBatch` | `baltic-patrol-mission-roe` |
| `import` | `C2PlayModeSignoffBatchRunner.RunImportBatch` | `baltic-patrol-classify` |
| `begin-execution` | `C2PlayModeSignoffBatchRunner.RunBeginExecutionBatch` | `baltic-patrol-classify` |