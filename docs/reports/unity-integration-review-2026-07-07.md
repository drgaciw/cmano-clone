# Unity Integration Review — Project Aegis

**Date:** 2026-07-07
**Reviewer:** unity-specialist (with GitNexus + direct source inspection)
**Scope:** How the project uses the Unity 6.3 LTS framework — architecture seam, presentation layer, DOTS/ECS posture, build/test pipeline — and prioritized recommendations for more effective Unity use.
**Branch:** `unity-integration-review`

---

## 1. Executive Summary

Project Aegis is **headless-first, Unity-thin**, and that is the right shape for a
deterministic, replayable military simulation. The entire game — sim, delegation,
policy, data — is built as `netstandard2.1` .NET assemblies *outside* Unity and
loaded as DLLs. Unity is a **presentation shell** (UI Toolkit panels + a bridge)
over two clean seams: `ISimWorldSnapshot` (read) and `IOrderSink` (write).

**Keep:** the pure deterministic core, the snapshot/sink seam, the `#if UNITY_5_3_OR_NEWER`
guards, the UI Toolkit choice for dense C2. These are strengths, not debt.

**Fix, in priority order:**

1. The project ships **DOTS/ECS packages that are 100% unused** and promises a "DOTS
   sim core" (ADR-005, doc 08 §4) that is architecturally incompatible with the
   shipped headless design. **Verdict: do not adopt ECS for world state; drop the packages.** (§3)
2. **No assembly definitions** — everything compiles into `Assembly-CSharp`, blocking
   automated PlayMode tests and slowing iteration. (§4.2)
3. **UI hosts poll and fully rebuild every frame** — GC churn and layout thrash that
   will not meet the 5,000-symbol @ 60 FPS NFR. (§4.3)
4. **The Unity seam has no automated test coverage** — the smoke test is a human
   checklist; `BuildPlayer.cs` is not CI-grade. (§4.4)
5. Legacy Input Manager, Mono scripting backend, and a PowerShell-only DLL bootstrap
   are friction points for a **desktop release** target. (§4.5)

---

## 2. What the Integration Actually Is

### 2.1 Assembly topology

| Layer | Where | Target | Unity dependency |
|-------|-------|--------|------------------|
| `ProjectAegis.Sim` | `src/` | `net8.0;netstandard2.1` | **None** — verified pure ("no UnityEngine") |
| `ProjectAegis.Delegation` | `src/` | `net8.0;netstandard2.1` | None |
| `ProjectAegis.Data` | `src/` | `net8.0;netstandard2.1` | None (SQLite) |
| `ProjectAegis.Delegation.UnityAdapter` | `src/` | `net8.0;netstandard2.1` | Contracts only (`ISimWorldSnapshot`, `IOrderSink`) |
| Unity presentation | `unity/ProjectAegis/Assets` | C# 10 (`csc.rsp`) | Full — 23 `.cs` files |

DLLs are published for `netstandard2.1`, copied into `Assets/Plugins/ProjectAegis/`,
and **gitignored** (rebuilt via `tools/copy-delegation-assemblies.ps1`).

### 2.2 The Unity layer

All 23 scripts are thin. The pattern is a MonoBehaviour **`*PanelHost`** per C2 panel
that reads headless projections and writes them into a UI Toolkit (`UIElements`) tree:

- `DelegationBridgeHost` — owns the `DelegationBridge`, exposes `RunTick(snapshot, sink)`
  and per-panel projections (`LastMapSymbols`, `LastOobTree`, `LastTopBar`, …).
- `SimplePlayModeSimHost` — a **zero-ECS** stub `ISimWorldSnapshot` + `IOrderSink` that
  drives `RunTick` every `Update()`. This is the play-mode harness.
- Panel hosts (`MapPlaceholderPanelHost`, `C2LeftDrawerPanelHost`, `RightUnitPanelHost`,
  `MessageLogPanelHost`, `SensorC2PanelHost`, …) — bind projections to `.uxml`/`.uss`.
- `CesiumGlobeBridge`/`CesiumGlobeHost` — optional real-globe map (Phase B, `#if CESIUM_FOR_UNITY`).
- `Assets/Editor/` — `BuildPlayer`, `App6AddressablesGroupSetup`,
  `DelegationSmokeSceneBuilder`, `C2PlayModeSignoffBatchRunner`.

**Verdict:** the seam is clean and the separation is genuinely good. The issues below
are about *how* the Unity layer is wired, built, and tested — not the architecture.

---

## 3. DOTS / ECS Posture (the central finding)

### 3.1 The gap

`Packages/manifest.json` pulls:

- `com.unity.entities` 1.4.6
- `com.unity.entities.graphics` 1.4.20
- `com.unity.burst` 1.8.29

ADR-005 ("DOTS/ECS for World State", *Accepted*), doc 08 §4 ("Burst for hot paths"),
and `VERSION.md` all frame ECS/Burst as the path to the entity-scale NFRs.

**Actual usage in `Assets/`: zero.** No `IComponentData`, `SystemBase`, `ISystem`,
`IJobEntity`, or `[BurstCompile]`. The sim is a single-threaded **managed** tick
(`SimTickRunner`/`SimTickPipeline`, fixed `1.0/60.0` dt) in a `netstandard2.1` DLL.
Doc 08 §1 already concedes this: *"Battlespace entities are `TargetRegistry` +
`UnitTarget`/`GroupTarget` … not full DOTS archetypes yet."*

### 3.2 Why ECS does not fit the shipped design

The entity-scale NFRs (doc 08 §2, doc 01, doc 03) are **two different problems**:

| NFR | Target | Runs under | Bottleneck |
|-----|--------|-----------|------------|
| Interactive C2 | 5,000+ @ 60 FPS | Unity player loop | **Rendering** 5k symbols |
| Headless AvA | 25,000 @ 1000×+ | **`dotnet` (no Unity)** | **Sim compute** |

1. **The heaviest NFR runs where Burst/Jobs/Entities do not exist.** The 25k @ 1000×
   headless batch path executes under `dotnet`, not the Unity player loop. Burst, the
   Jobs system, and Entities are Unity-runtime technologies — they cannot touch the
   path that needs raw throughput most. "Burst for hot paths" (doc 08 §4) is
   unrealizable for the headless path as designed.
2. **Moving world state into ECS breaks the project's strongest asset.** The pure,
   deterministic, headless-testable sim (1,215+ tests, golden replay, `WorldHash`
   checkpoints) depends on world truth living in plain C#. ECS world state means either
   abandoning that test/replay story, or maintaining **two bit-identical sims** (managed
   headless + ECS interactive) under a permanent determinism-audit burden. Neither is worth it.
3. **The interactive 5k target is a rendering problem, not a sim problem.** The sim tick
   is decoupled (`Refresh rate = sim tick rate`), and 5k entities with projection LOD is
   modest managed work. The hard part is *drawing* 5,000 map symbols at 60 FPS — a
   rendering concern that `entities.graphics`, `BatchRendererGroup`, GPU instancing, or
   Cesium billboards address. That is presentation, not the ECS world.

### 3.3 Recommendation

1. **Do not adopt DOTS/ECS for world state.** Remove `com.unity.entities` and
   `com.unity.entities.graphics` from the manifest (both currently 100% unused).
2. **Meet 25k @ 1000× headless with managed data-oriented parallelism** — struct-of-arrays
   hot state, order-stable parallel reductions (determinism-safe), optional
   `System.Numerics` SIMD. **Prerequisite: build the headless benchmark (INF-5.1), which
   does not exist yet** (the sim currently has zero parallelism). You cannot optimize an
   unmeasured 25k target.
3. **Solve 5k @ 60 FPS as rendering.** Commit to a symbol-render strategy — Cesium
   billboards (already integrating) or `BatchRendererGroup`/GPU instancing. Keep
   `com.unity.burst` (and possibly `entities.graphics`) **only if that render/culling path
   uses it.**
4. **Realign the docs to the real architecture** — ADR-005, doc 08 §4, `VERSION.md`:
   *"managed deterministic sim (headless-first); Unity = presentation + symbol rendering;
   ECS not used for world state."* This is the same doc/impl reconciliation the
   `platform-req21-realign` branch is already applying to the platform editor.

> **Cost of the status quo:** you pay Entities+Graphics+Burst import time, domain-reload
> overhead, and build weight — plus a misleading architecture story — for zero benefit.

---

## 4. Presentation, Build & Test Findings

### 4.1 Strengths (keep)

- Pure sim verified free of `UnityEngine` — determinism and headless tests are engine-independent.
- Clean read/write seam (`ISimWorldSnapshot` / `IOrderSink` / `EntityKey` / `TargetRegistry`).
- UI Toolkit for dense C2 UI is the correct modern choice (`com.unity.ui` 2.0.0 has runtime binding).
- `#if UNITY_5_3_OR_NEWER` / `#if CESIUM_FOR_UNITY` guards keep Unity-only code out of headless builds.

### 4.2 No assembly definitions — **HIGH leverage**

All 23 scripts land in `Assembly-CSharp`. Consequences:

- Every script edit recompiles the whole assembly (slow iteration).
- No enforced Runtime/Editor boundary beyond the magic `Editor/` folder.
- **You cannot author a real PlayMode/EditMode test assembly** — which is why the smoke
  test is still a manual checklist (§4.4).

**Fix:** add `ProjectAegis.Unity.Runtime.asmdef`, `ProjectAegis.Unity.Editor.asmdef`, and
`ProjectAegis.Unity.Tests.asmdef`, each referencing the plugin DLLs and the UI Toolkit /
Cesium modules explicitly.

### 4.3 Per-frame polling and full rebuild — **HIGH leverage**

`MapPlaceholderPanelHost.LateUpdate → Refresh()` runs unconditionally every frame, and
`RebuildSymbols()` (`MapPlaceholderPanelHost.cs:193`) does `_canvas.Clear()` then recreates
every `VisualElement` and re-registers a `ClickEvent` per symbol **every frame**. That is
per-frame allocation and layout thrash on the UI thread — and it directly blocks the
5,000-symbol NFR.

**Fix:** dirty-flag refresh (rebuild only when the tick changes the projection), pool and
reuse `VisualElement`s keyed by `SymbolId`, and use UI Toolkit **runtime data binding**
(`com.unity.ui` 2.0.0) instead of clear-and-recreate. Same pattern applies to the other
`*PanelHost.LateUpdate/Update` refresh loops.

### 4.4 Unbudgeted, untested engine seam — **HIGH leverage**

- `PLAYMODE-SMOKE.md` is a **human checklist**. The headless side has 1,215 tests; the
  Unity boundary — the thing most likely to break on a 6.3.x bump — has none automated.
- `BuildPlayer.cs` hardcodes the smoke scene, `BuildOptions.None`, no arg parsing, no
  IL2CPP, no version stamp, and does not exit non-zero on failure — not usable as a
  `game-ci` gate.

**Fix:** with the test asmdef (§4.2), convert the smoke checklist into an automated
PlayMode test (spin up `DelegationBridgeHost` + `SimplePlayModeSimHost`, tick N frames,
assert top-bar/map/log projections). Harden `BuildPlayer.cs` to read
`-buildTarget`/`-outputPath`/`-scriptingBackend`, stamp version, and `EditorApplication.Exit(code)`.

### 4.5 Desktop-release readiness (target: real desktop release)

- **Scripting backend:** standalone defaults to **Mono** (`scriptingBackend: {}`). Ship
  builds should use **IL2CPP** for performance and to satisfy platform requirements.
- **Input:** `activeInputHandler: 0` = legacy Input Manager only. Fine for mouse-driven
  C2 today, but the **Input System** package pairs better with UI Toolkit and enables the
  rebinding/accessibility work in `accessibility-requirements.md`.
- **DLL bootstrap:** plugin DLLs are gitignored and copied by **PowerShell** scripts —
  on a **Linux-primary** dev box a fresh clone won't compile in-editor until the script
  runs, and the scripts assume PowerShell. Consider a local `file:` UPM package (or a
  cross-platform `dotnet`/bash bootstrap) so the editor is self-consistent after clone.
- **`gcIncremental: 1`** is set — good; keep it and validate against the UI refresh fix (§4.3).

---

## 5. Prioritized Action List

| # | Action | Risk | NFR / Standard tie |
|---|--------|------|--------------------|
| 1 | Add Runtime/Editor/Tests `.asmdef`s | Low | Unlocks §2, faster iteration |
| 2 | Automate the PlayMode smoke test in CI | Low | coding-standards CI gate; engine-seam coverage |
| 3 | Dirty-flag + pool `MapPlaceholderPanelHost` (then other hosts) | Med | 5k-symbol @ 60 FPS NFR (doc 20) |
| 4 | Harden `BuildPlayer.cs` for `game-ci` (args, IL2CPP, exit code) | Low | Desktop release build |
| 5 | Remove `com.unity.entities` + `entities.graphics`; document DLL bootstrap | Low | §3 verdict (accepted) |
| 6 | IL2CPP backend + Input System migration | Med | Desktop release; accessibility |
| 7 | Build the headless entity-scale benchmark (INF-5.1) | Med | 25k @ 1000× NFR — *currently unmeasured* |
| 8 | Realign ADR-005 / doc 08 §4 / VERSION.md to managed-sim reality | Low | Doc/impl consistency |

---

## 6. Method Note

Findings are from direct source inspection (`Assets/**`, `Packages/manifest.json`,
`ProjectSettings/`, `src/ProjectAegis.Sim`, requirement docs 01/03/08/20, ADR-005). The
GitNexus keyword index returned degraded results during this review (FTS indexes missing —
`gitnexus analyze --repair-fts` recommended before relying on `query`), so structural
claims were verified by reading the files rather than the graph.
