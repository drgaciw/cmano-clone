# Cesium ion visual gate — tile streaming (2026-08-01)

**Wave:** UI Maturity Wave 4 residual — Lane C  
**Scope:** Productize tile-streaming **configuration contracts** + honest ion **presence** gate.  
**Secrets:** **NEVER commit** a Cesium ion access token to the repo, scene YAML, Prefabs, PlayerPrefs dumps, screenshots with Inspector open on the token field, or CI logs.

Related:

- Package pin: [cesium-unity-package-pin.md](./cesium-unity-package-pin.md)
- Phase B checklist: [cesium-phase-b-spike-checklist.md](./cesium-phase-b-spike-checklist.md)
- Scene notes: `unity/ProjectAegis/Assets/Scenes/CESIUM-SPIKE-SETUP.md`
- Headless contracts: `GlobeTileStreamingConfig`, `GlobeIonGateProjection`, `GlobeTileStreamingApplyState`

---

## Status lines (honest inactive)

| Condition | `StatusLine` | `StreamingActive` |
|-----------|--------------|-------------------|
| Token present + package present | `GLOBE TILES · ACTIVE` | true |
| Package missing | `GLOBE TILES · INACTIVE · PACKAGE_MISSING` | false |
| Package present, no token | `GLOBE TILES · INACTIVE · NO_ION_TOKEN` | false |

Gate math (headless): `StreamingActive = HasTokenConfigured && PackageAvailable`.  
Token **values** are never stored on `GlobeIonGateState` — only a boolean presence flag.

---

## Token sources (never commit)

Set **one** of:

1. **Environment variable** (preferred for local + CI):
   - `CESIUM_ION_TOKEN`
   - or `ProjectAegis_CesiumIonToken`
2. **Inspector** on `CesiumGlobeHost.ionAccessToken` (Editor user secret only).  
   Do **not** save the scene after pasting a real token if your git hygiene is weak — prefer env.

`DelegationSmokeSceneBuilder.BuildCesiumSpikeScene` **never** writes `ionAccessToken`.

---

## Runnable steps (local Editor visual gate)

1. Open Unity 6.3 LTS project: `unity/ProjectAegis`.
2. Ensure Cesium for Unity is pinned/resolved per [cesium-unity-package-pin.md](./cesium-unity-package-pin.md).
3. Set token via env **or** leave empty to verify inactive path:
   ```bash
   export CESIUM_ION_TOKEN='<your-private-token>'   # NEVER commit this value
   ```
4. Build spike scene (menu or batch):
   - Menu: **Project Aegis → Build CesiumSpike Scene**
   - Or batch `-executeMethod ProjectAegis.Unity.Editor.DelegationSmokeSceneBuilder.BuildCesiumSpikeSceneBatch`
5. Open `Assets/Scenes/CesiumSpike.unity`.
6. Confirm hierarchy includes:
   - `DelegationBridgeHost` with `useGlobeMap = true`
   - `GlobeMapProductHost`
   - `GlobeTileStreamingHost` (Toolkit gate status; works without package)
   - `CesiumGlobeBridge` / `CesiumGlobeHost` when package types resolve
7. Enter **Play Mode**.
8. **Verify tiles / gate:**
   - **No token:** console warning once; status `GLOBE TILES · INACTIVE · NO_ION_TOKEN` (or `PACKAGE_MISSING` if package absent); `StreamingActive=false`.
   - **Token + package:** log contains `tile streaming gate OPEN`; terrain tiles stream; status `GLOBE TILES · ACTIVE`.
9. **Screenshot path** (attach evidence under QA, no token UI):
   - `production/qa/cesium-ion-visual-gate-2026-08-01-globe.png` (or dated variant under `production/qa/`)
   - Optional FPS overlay: `production/qa/cesium-ion-visual-gate-2026-08-01-fps.png`
10. Exit Play. If Inspector held a pasted token, clear it before saving the scene.

---

## Checklist — tile streaming + FPS

- [ ] Token set via env or Inspector only (not in git)
- [ ] `BuildCesiumSpikeScene` does not serialize `ionAccessToken`
- [ ] Play Mode: gate status line matches table above
- [ ] With token + package: World Terrain (ion asset id **1**) tiles visible over Baltic overview
- [ ] Billboard markers still appear (bridge / product path)
- [ ] FPS note: target ~60 FPS empty / light markers in Editor; record measured FPS in evidence
- [ ] Screenshot saved under `production/qa/` **without** token field visible
- [ ] `dotnet test` filter `GlobeIonGate|GlobeTileStreaming|CesiumGlobeHostSource` green (headless)

---

## Headless / CI

- Default `DelegationSmoke` keeps `useGlobeMap=false` and does **not** require ion.
- Headless projections + apply-state tests cover ACTIVE / NO_ION_TOKEN / PACKAGE_MISSING without Unity Editor.
- Source contract test asserts `CesiumGlobeHost.cs` mentions `CESIUM_ION_TOKEN` / `ProjectAegis_CesiumIonToken` and does not embed JWT-like secrets.

---

## Rollback

Remove package pin + `CesiumSpike.unity`; leave Toolkit map default. Headless `dotnet test` unchanged. Tile-streaming contracts remain pure C# (no package dep).
