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
4. Enter **Play Mode** — Console should show no bridge errors; opposing agent issues orders each frame.

## Automated headless gate (no Editor)

```powershell
dotnet test ProjectAegis.sln --filter PlayModeSmokeHarnessTests
```

Mirrors the host loop in `PlayModeSmokeHarnessTests.cs`.
