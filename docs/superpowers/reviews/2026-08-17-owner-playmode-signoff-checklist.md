# Owner Play Mode signoff checklist — 2026-08-17

**Linear:** [DRG-162](https://linear.app/drgamtd-workspace/issue/DRG-162/s121-play-human-play-mode-overlay-signoff)  
**Scene:** `unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity`  
**Editor pin:** Unity **6000.3 LTS** (`6000.3.14f1` workspace / `6000.3.22f1` second clone)  
**This session Game View:** **BLOCKED** (no invented screenshots)

Related: `docs/superpowers/reviews/2026-08-17-final-integration-notes.md`, `docs/superpowers/reviews/2026-08-17-drg-162-signoff-ready.md`, `unity/ProjectAegis/PLAYMODE-SMOKE.md`.

---

## Session verdict (2026-08-18, 6000.3 batch)

| Gate | Status |
|------|--------|
| Headless / code (Phase 1 + Track B) | **PASS** — focused **144/144**, PlayModeSmokeHarness **24/24** |
| Plugin copy | **PASS** — `./tools/copy-delegation-assemblies.sh` → 14 DLLs |
| Unity **6000.3.14f1** batch console | **PASS** — `C2PlayModeSignoffBatchRunner` on `unity/ProjectAegis`, scenario `baltic-patrol-comms`, `UNITY_EXIT=0`. Log: `/tmp/unity-c2-playmode-signoff-6000.3.log` (`complete: simTicks=1 elapsed=11.77s`). `simTicks=1` is expected: demo watch auto-pauses until ACK+RESUME; batch cannot click ACK. Gate is **no Play Mode console errors**, not pixels. |
| Editor Game View pixels | **Still owner-only** — `-nographics` cannot stamp motion/rings/VFX |
| **DRG-162 overall** | **code + 6000.3 console green; visual AC still owner-only** |

Do **not** mark Linear Done until a human (or connected Unity MCP) records real Game View evidence.

---

## Session verdict (2026-08-17, agent closeout)

| Gate | Status |
|------|--------|
| Headless / code (Phase 1 + Track B) | **PASS** — focused **144/144**, PlayModeSmokeHarness **24/24** |
| Plugin copy into workspace Unity | **PASS** — `./tools/copy-delegation-assemblies.sh` → 14 DLLs @ 18:47 |
| Editor Game View pixels | **BLOCKED** — see blockers below |
| **DRG-162 overall** | **code signoff-ready; visual AC still owner-only** |

---

## AC status (this session)

| AC | Headless / code | Game View | Notes |
|----|-----------------|-----------|-------|
| **Motion** | **PASS** | **BLOCKED** | `PlayModeKinematicMover` advances inside the pause/accel loop; icons stay frozen until ACK + **RESUME**. After RESUME, `u1` / `hostile-1` should slide; Plot Course / Move draws a polyline; Hold clears it. |
| **Rings** | **PASS** | **BLOCKED** | Selected unit: blue sensor (outer) + red weapon (inner), circular on a non-square canvas. Expected HUD `ENVELOPES: 2`. |
| **Datalinks** | **PASS** | **BLOCKED** | Catalog bind in `DelegationBridgeHost.Awake` unblocks edges. Smoke pair → **1** edge. Expected HUD `DATALINKS: 1`. Default `baltic-patrol` = green; `baltic-patrol-comms` = amber then grey. |
| **Toast / compression** | **PASS** | **BLOCKED** | Demo watch auto-pauses (`TIME: PAUSED`). ACK dismisses toast; **RESUME** restarts clock. − / + walks 1x → 2x → 4x → 8x; PAUSE freezes kinematics again. |
| **VFX** | **PASS** (projection) | **BLOCKED** | Fire lines / impacts on transient layer (`map` transient), **not** `map-overlay-course-layer`. Stub `ActiveEngagementCount => 0` — live Baltic firehose may stay empty. Empty VFX is **not** a fail if the layer exists and courses stay separate. |
| **Message log** | **PASS** | **BLOCKED** | Log should keep growing after the first post-tick step (`PolicyUpdate` and other seed categories). |

**Forbidden (already honored this session):** no REQ-20 append · no `DelegationBridge.Tick` / `CatalogWriteGate` / replay-golden edits · no invented Editor shots.

---

## Why Game View is BLOCKED (this machine, this session)

Checked 2026-08-17 ~18:47 local. Do not treat any of these as pixel PASS.

| Check | Result |
|-------|--------|
| Workspace plugins | **Copied** to `/home/username01/cmano-clone/unity/ProjectAegis/Assets/Plugins/ProjectAegis` (14 DLLs, 18:47) |
| Unity 6000.3 binaries | **Installed** — `6000.3.14f1` and `6000.3.22f1` under `~/Unity/Hub/Editor/` |
| 6000.3 Editor on `unity/ProjectAegis` | **Not running** |
| Unity MCP (`ai-game-developer` / `:8080`) | **Unavailable** — MCP `needsAuth`; TCP `:8080` is an unrelated Java service, not Unity-MCP |
| Editor menus / `screenshot-game-view` | **Unavailable** — no connected Editor on the Aegis Unity project |
| Batchmode Play / scene rebuild | **PASS (2026-08-18)** — 6000.3.14f1 `C2PlayModeSignoffBatchRunner.RunBatch` without `-quit`. Console gate green. Still **not** Game View pixels (`-nographics`). |
| Open Editor process | **Wrong target** — `6000.5.1f1` with `-projectpath /home/username01/cmano-clone` (repo root, not `unity/ProjectAegis`) |
| Second clone | **Usable as a later Editor host**, not this session: `/home/username01/projects/active/cmano-clone/cmano-clone/unity/ProjectAegis` is 6000.3.22f1, plugins **stale** (15:54, smaller `Delegation*.dll`s). Running Linux64 player (`/tmp/projectaegis-player-live.log`) is a **16:08 build** — do not use as Phase1+B evidence. |
| Stale lock | `unity/ProjectAegis/Temp/UnityLockfile` exists (09:35) with **no** `lsof` holder — close/ignore before opening Editor |

---

## Copy-paste: owner Editor path

Run from **`/home/username01/cmano-clone`** unless noted. Close the mis-opened `6000.5.1f1` repo-root Editor first if it still holds Hub.

### 1. Refresh plugins (already done this session; re-run if you rebuild C#)

```bash
cd /home/username01/cmano-clone
export PATH="$HOME/.dotnet:$PATH"
./tools/copy-delegation-assemblies.sh
```

Windows/pwsh: `./tools/copy-delegation-assemblies.ps1`

If you sign off from the **second clone**, copy plugins there too (that tree is still on 15:54 DLLs):

```bash
cd /home/username01/projects/active/cmano-clone/cmano-clone
./tools/copy-delegation-assemblies.sh
```

### 2. Open the Unity project (6000.3 only)

```bash
# Workspace (pin 6000.3.14f1)
/home/username01/Unity/Hub/Editor/6000.3.14f1/Editor/Unity \
  -projectPath /home/username01/cmano-clone/unity/ProjectAegis

# Or second clone (pin 6000.3.22f1) after plugin copy
/home/username01/Unity/Hub/Editor/6000.3.22f1/Editor/Unity \
  -projectPath /home/username01/projects/active/cmano-clone/cmano-clone/unity/ProjectAegis
```

Hub: open `unity/ProjectAegis` with **6000.3.x**, not 6000.5.1f1.

### 3. Rebuild DelegationSmoke + PanelSettings

In Editor menus:

1. **Project Aegis → Build DelegationSmoke Scene (comms QA)**  
   (or **Ensure UI Maturity Hosts (open scene)** if `DelegationSmoke` is already open)
2. Confirm `AttentionToast` host exists.
3. If Game view is empty sky: **Project Aegis → Fix UIDocument PanelSettings (open scene)**
4. Open `Assets/Scenes/DelegationSmoke.unity`
5. Confirm every `UIDocument` uses `Assets/UI/C2RuntimePanelSettings.asset`

Optional batch (Linux; Editor must not already have the project):

```bash
UNITY=/home/username01/Unity/Hub/Editor/6000.3.14f1/Editor/Unity
PROJ=/home/username01/cmano-clone/unity/ProjectAegis
"$UNITY" -batchmode -nographics -quit \
  -projectPath "$PROJ" \
  -executeMethod ProjectAegis.Unity.Editor.DelegationSmokeSceneBuilder.BuildBatch \
  -logFile /tmp/unity-delegation-smoke-setup.log
```

Batch rebuild is **not** Game View signoff.

### 4. Enter Play Mode

1. `DelegationBridgeHost.scenarioPolicyId` = `baltic-patrol` (default rings/green edge). Optional: `baltic-patrol-comms` for amber/grey.
2. Press **Play**.
3. **ACK the toast first.** Demo watch auto-pauses (`TIME: PAUSED`). Motion, rings, and VFX look frozen until ACK + **RESUME**. That is Phase 1 clock authority, not a kinematics bug.
4. Click **RESUME**.

### 5. Visual AC walk (stamp PASS/FAIL here)

| Look for | Pass if | Your stamp |
|----------|---------|------------|
| **Motion** | After RESUME, `u1` / `hostile-1` icons slide. Plot Course / Move draws a course polyline; Hold clears it and stops. | _ |
| **Rings** | `u1` selected: blue sensor + red weapon, circular on non-square Game View. HUD `ENVELOPES: 2`. Click the other ■ — rings follow. | _ |
| **Datalinks** | One on-symbol edge; HUD `DATALINKS: 1`. Green on `baltic-patrol`; amber/grey on `baltic-patrol-comms`. | _ |
| **Toast / compression** | ACK dismisses toast; − / + walks 1x → 2x → 4x → 8x and motion speeds up; PAUSE freezes kinematics. | _ |
| **VFX** | Fire lines / impacts (if any) stay on the transient layer, not the course layer. Empty stub firehose is OK. | _ |
| **Message log** | Line count grows after the first post-tick step (`PolicyUpdate` and other seed categories). | _ |
| **Console** | No bridge / overlay errors. Overlay layers do not steal symbol clicks. | _ |

### 6. After pixels match

1. Attach Game View shot(s) + Console clean to [DRG-162](https://linear.app/drgamtd-workspace/issue/DRG-162).
2. Mark **Done**.
3. Do **not** append REQ-20 or change Baltic v2 goldens from this checklist.

If pixels fail: file a visual bug with a Game View shot. Do **not** rebuild CMD-32/34 or touch `DelegationBridge.Tick` / `CatalogWriteGate`.

---

## Headless evidence already in hand (not Game View)

From `docs/superpowers/reviews/2026-08-17-final-integration-notes.md` (RUN+READ):

| Filter | Result |
|--------|--------|
| `dotnet build ProjectAegis.sln` | 0 errors, 0 warnings |
| `PlayModeSmokeHarnessTests` | **24/24** |
| Focused Phase1+B matrix (MessageLog, CombatVfx, overlays, toast/clock, kinematics) | **144/144** |

Unity scripts under `#if UNITY_5_3_OR_NEWER` are **not** compiled by `ProjectAegis.sln`. Headless coverage for those hosts is source-contract tests.

---

*Closeout 2026-08-18. 6000.3.14f1 batch console PASS. Game View pixels still owner. No screenshots invented.*
