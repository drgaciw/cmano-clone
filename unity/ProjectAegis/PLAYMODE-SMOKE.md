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
10. Optional scenarios on bridge: `baltic-patrol-mission`, `baltic-patrol-classify`, `baltic-patrol-comms` (COMMS top bar + denials).
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