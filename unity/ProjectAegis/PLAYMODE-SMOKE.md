# Play Mode smoke checklist

## One-time setup

From repo root:

```powershell
dotnet build ProjectAegis.sln -c Release
./tools/init-unity-project.ps1
```

Open `unity/ProjectAegis` in **Unity Hub 6.3 LTS** (6000.3.x).

## Scene setup

1. Create empty GameObject `DelegationSmoke`.
2. Add **DelegationBridgeHost** (`globalSeed` = 42).
3. Add **SimplePlayModeSimHost** on the same object (`bridgeHost` auto-wires in Inspector Reset).
4. Add **SensorC2PanelHost** (UI Toolkit):
   - Assign `Assets/UI/SensorC2/SensorC2Panel.uxml` to **Panel Asset**
   - Assign `Assets/UI/SensorC2/SensorC2Panel.uss` to **Panel Styles**
   - `UIDocument` is added automatically; leave **Panel Settings** as default or create one under `Assets/UI/`
5. Enter **Play Mode** — sensor C2 panel shows EMCON, track, and contact rows; no bridge errors.

## Automated headless gate (no Editor)

```powershell
dotnet test ProjectAegis.sln --filter PlayModeSmokeHarnessTests
```

Mirrors the host loop in `PlayModeSmokeHarnessTests.cs`.
