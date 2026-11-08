# AC-8 Unity host load evidence pack (SE-W2)

**Date:** 2026-07-08  
**Program:** `scenario-editor-completion` SE-W2  
**Scope:** Headless-authored `.scenario.json` → host load path with intact ORBAT/missions/events + `editorState` defaults (camera / layers). **No map-first placement UI.**

## Automated evidence (RUN+READ)

| Test | Assembly | Result |
|------|----------|--------|
| `AC8_Unity_host_roundtrip_loads_headless_scenario_json_intact_ORBAT_missions_events_editorState` | `ProjectAegis.Delegation.UnityAdapter.Tests` | **PASS** (PlayModeSmoke filter) |
| `AC8_strike_package_fixture_loads_with_missions_and_editorState_defaults` | same | **PASS** (SE-W2 productionize second fixture) |
| Full `PlayModeSmokeHarnessTests` | same | **19/19 PASS** |

**Command:**

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter PlayModeSmokeHarnessTests -v minimal
```

## Fixtures

- `data/scenarios/examples/baltic-patrol.scenario.json`
- `data/scenarios/examples/strike-package.scenario.json`

## Contract

- Load via `ScenarioDocumentJsonLoader` / `ScenarioPackageLoader` (Data) — thin host path  
- `editorState` is **derived-only** (AC-9); never written back to canonical examples  
- Defaults: `camera` (theater lat/lon/zoom), `layers` (all/orbat/events on)  
- **Not claimed:** Unity Edit Mode map drawing, Mission Board GUI, live Editor screenshots (Phase 2 / optional manual)

## Honesty

S87 delivered the primary AC-8 proxy test. SE-W2 adds a second fixture assertion and this evidence pack so the completion epic can treat **AC-8 host load contract** as **Met (headless UnityAdapter path)** without claiming full GUI editor.
