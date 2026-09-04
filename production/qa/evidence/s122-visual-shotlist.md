# S122 visual shot-list (Unity-MCP serial lane)

**Sprint:** S122 Slice A (`production/sprints/sprint-122-slice-a-unity-mcp.md`)
**Stories:** S122-01 / DRG-180 · S122-02 / DRG-181 · S122-03 overlay counts · S122-04 / DRG-208
**When to run:** After `http://localhost:8080` returns HTTP 2xx. Do **not** start the Editor in this recon. Do **not** mutate C#, UXML, USS, scenes, prefabs, or `DelegationBridge`.
**Companion stub:** [`s122-04-playmode-signoff.md`](s122-04-playmode-signoff.md) (status BLOCKED until these PNGs + console dump exist).
**Play Mode doc:** [`unity/ProjectAegis/PLAYMODE-SMOKE.md`](../../../unity/ProjectAegis/PLAYMODE-SMOKE.md)

---

## Scene to `scene-open`

| Field | Value |
|-------|--------|
| Asset path | `Assets/Scenes/DelegationSmoke.unity` |
| Repo path | `unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity` |
| GUID | `348674c3a3394e7418d5698111f4fa42` |
| MCP `sceneRef` | `{ "assetPath": "Assets/Scenes/DelegationSmoke.unity", "assetGuid": "348674c3a3394e7418d5698111f4fa42", "instanceID": 0 }` |
| Mode | `Single` |
| Last open scene | `Library/LastSceneManagerSetup.txt` already points here |
| Policy on GO `DelegationSmoke` | `scenarioPolicyId: baltic-patrol-classify` (hostile ◆ on map; keep it) |
| Seed | `globalSeed: 42`, `SimplePlayModeSimHost` auto-begins, selects **`u1`** (friendly), **not** a contact |
| Smoke IDs | friendly `u1` · hostile unit `hostile-1` · contact `c1` |

This is the only C2 Play Mode `.unity` under `unity/ProjectAegis/Assets` (`CesiumSpike.unity` is Phase B globe, not this pack).

---

## Host inventory (committed scene YAML)

Read-only recon of `DelegationSmoke.unity` GameObject names:

| GameObject | Host type | In committed scene? |
|------------|-----------|---------------------|
| `DelegationSmoke` | `DelegationBridgeHost` + `SimplePlayModeSimHost` | Yes |
| `MapPlaceholder` | `MapPlaceholderPanelHost` | **Yes** — `m_EditorClassIdentifier: Assembly-CSharp::ProjectAegis.Unity.Runtime.MapPlaceholderPanelHost` |
| `C2TopBar` / `C2LeftDrawer` / `RightUnitDetail` / `MessageLog` / `DoctrineInheritance` / catalogs | other C2 hosts | Yes |
| `ContactDetail` | `ContactDetailPanelHost` | **No** — not in scene YAML. Builder *can* add it: menu **Project Aegis → Ensure UI Maturity Hosts (open scene)** (`DelegationSmokeSceneBuilder.EnsurePanelHostIfMissing<ContactDetailPanelHost>("ContactDetail", …)`). Full rebuild also creates it. |
| *(none)* | `SensorToShooterPanelHost` | **No — not in any `.unity` / `.prefab`.** Not in `DelegationSmokeSceneBuilder` at all. |

**UXML / host label names (already in tree):**

- Contact quality — `Assets/UI/ContactDetail/ContactDetailPanel.uxml` + `ContactDetailPanelHost`: `source-line`, `comms-line`, `last-known-line`, `explain-link-line` (also `contact-id-line`, `confidence-line`, `staleness-line`).
- Sensor→shooter — `Assets/UI/SensorToShooter/SensorToShooterPanel.uxml` + `SensorToShooterPanelHost`: `contact-id-line`, `complete-line`, `primary-cause-line`, `sensor-link-line`, `track-link-line`, `targetability-link-line`, `shooter-link-line`, `explain-link-line`.
- Overlay counts — `Assets/UI/MapPlaceholder/MapPlaceholderPanel.uxml` + `MapPlaceholderPanelHost`: `envelope-ring-count` / `datalink-edge-count` bound to `ENVELOPES: n` / `DATALINKS: n`.

---

## Hard rules for the serial lane

1. **ZERO `DelegationBridge` mutation.** No `script-execute`, no reflection, no `script-update-or-create` on `DelegationBridge` / hotpath. Do not tick the bridge from MCP.
2. **Do not `gameobject-component-add` `SensorToShooterPanelHost` in this evidence pass** unless a *user-facing serial lane* has already been told to add the GO. Recon: host is **not** in the scene; adding it is a scene mutation owned by that later lane.
3. Do not start Unity if `:8080` is down (S122-00). Do not `gt` / commit from this list.
4. `screenshot-game-view` returns a PNG to the agent. Persist bytes to the exact paths below (create dirs if missing). Tool itself has no `outputPath` arg.

---

## Ordered MCP steps

Run in this order. Stop on first hard blocker (see below).

### 0. Preflight (shell, already assumed live)

```bash
curl -sS -o /dev/null -w "%{http_code}\n" --max-time 3 http://localhost:8080
# expect 2xx; if this clone was never pinned: ./tools/pin-unity-mcp-8080.sh
```

### 1. `ping`

Echo a token, e.g. `s122-visual`. Expect the same string (or `pong` if omitted).

### 2. `unity-tool-list`

Confirm at least: `scene-open`, `editor-application-get-state`, `editor-application-set-state`, `screenshot-game-view`, `console-get-logs`, `gameobject-find`. Do not invent tools.

### 3. `scene-open`

- `sceneRef.assetPath` = `Assets/Scenes/DelegationSmoke.unity`
- `loadSceneMode` = `Single`

### 3b. Read-only host check (before Play)

`gameobject-find` by name:

| `name` | Expect |
|--------|--------|
| `MapPlaceholder` | present; component `ProjectAegis.Unity.Runtime.MapPlaceholderPanelHost` |
| `ContactDetail` | **may be missing** — see blockers |
| `SensorToShooter` (or any GO with `SensorToShooterPanelHost`) | **missing** — see blockers |

Do **not** add components here.

### 4. Wait compile (Edit Mode)

Loop `editor-application-get-state` until `IsCompiling == false` and `IsUpdating == false`. If `scriptCompilationFailed`, **stop** (set-state Play will throw).

### 5. `editor-application-set-state` Play

`{ "isPlaying": true, "isPaused": false }`

### 6. Wait compile / domain settle (Play Mode)

Loop `editor-application-get-state` until `IsPlaying == true`, `IsCompiling == false`, `IsPlayingOrWillChangePlaymode` stable. Wait ~1–2 s of ticks so `SimplePlayModeSimHost` seeds ORBAT (`u1` selected).

### 6b. Select a **contact** (required for shots 1–2)

Default selection is **unit `u1`**. `ContactDetailPanelHost` and `SensorToShooterPanelHost` set `display: none` unless `SelectedContactId` is set (or S2S has an injected snapshot).

**Allowed:** Game View click — left drawer **CONTACTS** tab (`tab-contacts`) then a contact row, **or** map hostile ◆ (resolver → `SelectContact`). Smoke contact id is **`c1`**.

**Forbidden:** `script-execute` / reflection on `DelegationBridge`. MCP has no pointer-click tool in the registered set — a human (or the user-facing serial lane) must click in Game View.

If no contact is selected, shots 1–2 are empty chrome and **FAIL**.

### 7. Three `screenshot-game-view` captures

Save each returned PNG to the destination in the table. Game View must be open.

### 8. `console-get-logs`

Suggested: `maxEntries` ≥ 200, `includeStackTrace: true`, no type filter. Persist full dump. Pass only if **0** `Error` / `Exception` (ignore pre-existing Edit Mode noise only if clearly dated before Play).

### 9. Stop Play

`editor-application-set-state` `{ "isPlaying": false, "isPaused": false }`

Do **not** `scene-save` unless a later user-facing lane added hosts and was told to persist.

---

## PNG / log destinations

| Shot | Story | Destination | What must be readable |
|------|-------|-------------|------------------------|
| 1 | S122-01 | `production/qa/evidence/s122-01/contact-quality-cues.png` | Title `CONTACT DETAIL`. Lines with prefixes **`SOURCE:`**, **`COMMS:`**, **`LAST KNOWN:`**, **`EXPLAIN:`**. After `c1` selected: `CONTACT: c1`; `SOURCE: observer` (or non-dash source); `LAST KNOWN:` not `—`; `EXPLAIN: engage/c1`. **`COMMS: UNKNOWN`** is the DRG-180 out-of-comms cue (`COMMS: UNKNOWN (out-of-comms)`). Live `ContactDetailPanelHost.Refresh()` currently calls `ProjectAndApply` with `outOfComms: false`, so Play may show **`COMMS: up`**. If so: still capture; do not fake text; flag bind gap vs S122-01 AC. Empty `COMMS: —` means **no selected contact** (shot FAIL). Non-color cues: comms line is **bold** (`contact-detail-line--comms`); explain is **italic**. |
| 2 | S122-02 | `production/qa/evidence/s122-02/sensor-to-shooter-chain.png` | Title `SENSOR TO SHOOTER`. `CONTACT:`, `CHAIN:` (`COMPLETE` or `BROKEN`), `CAUSE:`, `SENSOR:`, `TRACK:`, `TARGETABILITY:`, `SHOOTER:`, `EXPLAIN: engage/{id}` (deep-link to DRG-180). Host **not in scene** → this shot is **BLOCKED** until user-facing serial lane adds `SensorToShooterPanelHost` (then `scene-save`). Do not add it from this list. |
| 3 | S122-03 | `production/qa/evidence/s122-03/overlay-counts.png` | Map HUD labels `envelope-ring-count` / `datalink-edge-count`. Visible text **`ENVELOPES:`** and **`DATALINKS:`**. Smoke + `u1` selected (S121 / PLAYMODE-SMOKE): **`ENVELOPES: 2`**, **`DATALINKS: 1`**. `ENVELOPES: 0` / `DATALINKS: 0` is UXML default — wait a tick / confirm catalog bind, do not rewrite overlay projections. |
| 4 | S122-04 | `production/qa/evidence/s122-04/console-get-logs.txt` | Raw MCP `console-get-logs` JSON/text. **0 errors** to PASS DRG-208. |
| — | S122-09 / DRG-183 | [`s122-09/targetability-harness.md`](s122-09/targetability-harness.md) | **Not a Game View shot.** Headless DRG-219 `TargetabilityAcceptProjection` AC (`FullyQualifiedName~TargetabilityAccept`). Chain UI is S122-02 (shot 2); no new UIDocument. |
| 5 | S122-12 | `production/qa/evidence/s122-12/unit-roe-auth.png` | After `SelectUnit(u1)`: `ROE:` not `—`, `AUTH: Permitted` or `AUTH: Withheld`. Probe `LastUnitDetail` non-null. |
| 6 | S122-13 | `production/qa/evidence/s122-13/contacts-list-c1.png` | Left CONTACTS lists `c1` while Contact Detail shows `CONTACT: c1`. |
| — | S123 | `production/sprints/sprint-123-slice-b-mcp.md` | Slice B MCP follow-up. CombatDomains missing from scene (`slice-rec-2`). |

PNG dirs `s122-02/`, `s122-03/`, `s122-04/` may not exist yet; create only when writing real files.

---

## Blockers (fail-closed)

| Blocker | Effect | What the serial lane must **not** do |
|---------|--------|--------------------------------------|
| Editor / `:8080` down | Entire pack BLOCKED (same as `s122-04-playmode-signoff.md`) | Do not start Unity from a parallel C# lane; do not invent MCP tools |
| Compile errors | Cannot enter Play | Do not ignore `scriptCompilationFailed` |
| `ContactDetail` GO missing from open scene | Shot 1 empty | Do not hand-edit YAML. User-facing serial lane may run **Ensure UI Maturity Hosts** (adds `ContactDetail` only) then `scene-save`. This recon does not. |
| `SensorToShooterPanelHost` **not in any scene** | Shot 2 BLOCKED | **`gameobject-component-add` only after user-facing serial lane.** Type: `ProjectAegis.Unity.Runtime.SensorToShooterPanelHost` + `UIDocument`; UXML `Assets/UI/SensorToShooter/SensorToShooterPanel.uxml`; USS sibling; wire `bridgeHost` to `DelegationSmoke`; PanelSettings `Assets/UI/C2RuntimePanelSettings.asset`. USS parks panel at `right: 304px; top: 56px` (left of contact detail). |
| No selected contact (`u1` only) | Shots 1–2 hide roots | Click CONTACTS / hostile ◆. Do not `script-execute` `DelegationBridge` |
| Game View not open / empty sky | Screenshots useless | Confirm each `UIDocument` has PanelSettings (`Fix UIDocument PanelSettings` menu if empty; that *is* a scene fix — user-facing only) |
| Overlay counts stuck at 0 | Shot 3 weak | Do not rebuild `TacticalOverlayProjection` / `DatalinkPictureProjection`. Catalog bind is on `DelegationBridgeHost.Awake` — do not edit it for this pack |

---

## Pass / fail (S122-04)

Mark [`s122-04-playmode-signoff.md`](s122-04-playmode-signoff.md) **PASS** only when all four destinations exist, shots 1–3 show the required prefixes, shot 2 is not skipped unless the S2S host was explicitly deferred, and console dump has 0 Play Mode errors.

Until then S122-04 stays **not done**.
)
