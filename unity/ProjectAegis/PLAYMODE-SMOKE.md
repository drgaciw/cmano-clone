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
4. **Top:** `C2TopBarPanelHost` — `Assets/UI/TopBar/C2TopBarPanel.uxml` / `.uss`
5. **Left:** `C2LeftDrawerPanelHost` — `Assets/UI/C2LeftDrawer/C2LeftDrawerPanel.uxml` / `.uss`
6. **Center:** `MapPlaceholderPanelHost` — `Assets/UI/MapPlaceholder/MapPlaceholderPanel.uxml` / `.uss`
7. **Right:** `RightUnitPanelHost` — `Assets/UI/UnitDetail/UnitDetailPanel.uxml` / `.uss`
8. **Bottom:** `MessageLogPanelHost` — `Assets/UI/MessageLog/MessageLogPanel.uxml` / `.uss`
9. Wire each host's `bridgeHost` to the same **DelegationBridgeHost**.
10. Optional scenarios on bridge: `baltic-patrol-mission`, `baltic-patrol-classify`, `baltic-patrol-comms` (COMMS top bar + denials), `baltic-patrol-mission-roe` (doctrine inheritance panel).
11. Enter **Play Mode** — top bar shows sim time + score; map shows ■/◆ symbols; drawer tabs work; no bridge errors.

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
```

Opens `DelegationSmoke.unity`, enters Play Mode in batchmode, and fails on game console errors. Evidence: `unity-c2-playmode-signoff.log`. Full checklist: `production/qa/c2-manual-signoff-2026-06-02.md`.

| Scenario | `-executeMethod` | Policy id |
|----------|------------------|-----------|
| `comms` | `C2PlayModeSignoffBatchRunner.RunBatch` | `baltic-patrol-comms` |
| `classify` | `C2PlayModeSignoffBatchRunner.RunClassifyBatch` | `baltic-patrol-classify` |
| `doctrine` | `C2PlayModeSignoffBatchRunner.RunDoctrineBatch` | `baltic-patrol-mission-roe` |