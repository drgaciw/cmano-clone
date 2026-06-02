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