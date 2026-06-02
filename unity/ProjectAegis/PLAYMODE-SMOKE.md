# Play Mode smoke checklist

## One-time setup

From repo root:

```powershell
dotnet build ProjectAegis.sln -c Release
./tools/init-unity-project.ps1
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
10. Optional: `baltic-patrol-mission` or `baltic-patrol-classify` on bridge for missions/contacts demo.
11. Enter **Play Mode** — top bar shows sim time + score; map shows ■/◆ symbols; drawer tabs work; no bridge errors.

## Automated headless gate (no Editor)

```powershell
dotnet test ProjectAegis.sln --filter "PlayModeSmokeHarnessTests|ReplayGolden"
```

Mirrors the host loop in `PlayModeSmokeHarnessTests.cs`.