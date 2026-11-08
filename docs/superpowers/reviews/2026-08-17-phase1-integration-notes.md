# Phase 1 integration notes — 2026-08-17

**Role:** Phase 1 integration verifier (post four parallel landings)  
**Workspace:** `/home/username01/cmano-clone` (`main`, uncommitted working tree)  
**Forbidden this pass:** `DelegationBridge.Tick` redesign · Track B kinematics · `CatalogWriteGate` · commits

---

## Verdict

**Phase 1 integrates cleanly.** All four capabilities coexist in source. No merge-conflict markers, no duplicate methods, no missing braces, no compile breaks. **No code fixes applied.**

`dotnet build ProjectAegis.sln` — **0 errors, 0 warnings.** Discoverable Phase 1 filters — **all green** (matrix below).

Editor Game View was **not** run (headless). Pixel ACs stay **UNKNOWN**.

---

## Overlapping files

Tracks that landed on the same hosts:

| File | DRG-162 | Track A telemetry | Track A UI | Track C |
|------|:-------:|:-----------------:|:----------:|:-------:|
| `unity/.../DelegationBridgeHost.cs` | Catalog bind in `Awake` | — | Clock/toast command façade | `LastCombatVfx` after `RunTick` |
| `unity/.../SimplePlayModeSimHost.cs` | — | `AdvanceDecisionLog` after tick | Pause skip + accel 1–8× loop | — |
| `unity/.../MapPlaceholderPanelHost.cs` | Overlay HUD + `CatalogReader` | — | — | `ApplyTransientCombatVfx` |
| `src/.../PlayModeSmokeOrbatSeeder.cs` | — | Richer seed + time-gated feed | — | — |
| `src/.../PlayModeSmokeHarnessTests.cs` | — | Feed-growth assertions | — | — |
| `unity/.../DelegationSmokeSceneBuilder.cs` | — | — | AttentionToast host | — |
| `unity/.../C2TopBarPanelHost.cs` | — | — | − / + / PAUSE·RESUME | — |

Unrelated dirty tree (gauntlet skills, `AGENTS.md`, art bible, etc.) was **not** treated as Phase 1 payload.

`git diff --stat` on the Phase 1 hosts is additive only. No `<<<<<<<` / `=======` / `>>>>>>>` in `*.cs` / `*.uxml` / `*.uss`.

---

## Capability coexistence (source)

| Track | Required surface | Status | Where |
|-------|------------------|--------|-------|
| **A telemetry** | `PolicyUpdate` (plus AgentDecision / Mission / Event / Damage / Controller) | **Present** | `MessageLogProjection.TryProject` switch arms; USS `--policy` / `--mission` / `--kill` reuse |
| **DRG-162** | Catalog bind so datalink edges exist | **Present** | `DelegationBridgeHost.Awake`: `CatalogReader ??= Session?.CatalogReader ?? CatalogReaderFactory.TryCreateBalticPatrolReader() ?? InMemoryCatalogReader.BalticPatrolFixture()` |
| **A UI** | Toast + compression APIs | **Present** | `C2ClockCommand`; host `TrySetTimeAcceleration` / `TryPauseSim` / `TryResumeSim` / `RefreshAttentionToast` / `TrySeedDemoWatchAttention`; `AttentionToastPanelHost`; top-bar −/+ / PAUSE; scene builder `AttentionToast` |
| **C** | Combat VFX projection + transient apply | **Present** | `CombatVfxProjection.Project` → `LastCombatVfx` in `RunTick`; `MapPlaceholderPanelHost.ApplyTransientCombatVfx` → `MapCanvasTransientEffectsRenderer` |

`RunTick` still calls `Bridge.Tick` then **presentation-only** refresh (`LastMessageLog`, `LastCombatVfx`, top bar, comms, roster). No Tick body rewrite. `DelegationBridge.cs` and `CatalogWriteGate` have **zero** Phase 1 diff.

Replay golden hash `17144800277401907079` remains in `tests/regression/`.

---

## GitNexus

| Call | Result |
|------|--------|
| `impact(DelegationBridgeHost, upstream, summaryOnly)` | **LOW** — 0 direct callers, 0 processes (index likely stale; Unity hosts often report empty upstream) |
| `impact(SimplePlayModeSimHost, upstream, summaryOnly)` | **LOW** — same |
| `impact(MessageLogProjection, upstream, summaryOnly)` | **LOW** — same |
| `detect_changes({scope:"all"})` | **LOW** — 23 changed files, 0 affected processes |

No HIGH / CRITICAL. Do not treat empty upstream as proof of no Unity dependents — hosts are presentation clients.

---

## Fixes applied

**None.** Parallel edits composed without compile or structural damage:

- `DelegationBridgeHost` keeps catalog bind, clock/toast façade, and `LastCombatVfx` as adjacent additive members.
- `SimplePlayModeSimHost.Update` composes pause → accel loop → `RunTick` → `AdvanceDecisionLog` (telemetry after tick, not inside `DelegationBridge`).
- Map host applies static overlay (DRG-162) then transient VFX (Track C) on separate renderers / dirty refs.

---

## Test matrix (RUN+READ)

| Filter / fixture | Assembly | Result |
|------------------|----------|--------|
| `dotnet build ProjectAegis.sln` | sln | **0/0** errors/warnings |
| `FullyQualifiedName~PlayModeSmokeHarnessTests` | UnityAdapter.Tests | **24/24** |
| `FullyQualifiedName~MessageLog` | Delegation.Tests | **33/33** |
| `FullyQualifiedName~MessageLog` | UnityAdapter.Tests | **13/13** |
| `FullyQualifiedName~CombatVfx` | Delegation.Tests | **8/8** |
| `CombatVfx` / `TransientEffects` / `MapCanvasTransient` | UnityAdapter.Tests | **2/2** |
| `Drg162OverlaySignoff` + `TacticalOverlayProjection` + `MapCanvasOverlayGeometry` | Delegation.Tests | **16/16** |
| `Drg162OverlaySignoffProjectionTests` only | Delegation.Tests | **2/2** |
| `CatalogBind` + `MapPlaceholderPanelHostContract` + `MapCanvasOverlay` | UnityAdapter.Tests | **3/3** |
| Broad `~Drg162\|~Overlay` | Delegation.Tests | **34/34** (includes non-DRG-162 overlay tests) |
| Broad `~Drg162\|~CatalogBind\|~Overlay\|~MapPlaceholderPanelHostContract` | UnityAdapter.Tests | **4/4** |
| `FullyQualifiedName~AttentionToast` | Delegation.Tests | **7/7** (`AttentionToastApplyStateTests`) |
| `AttentionToast` / `C2Clock` | UnityAdapter.Tests | **11/11** |

Commands:

```bash
dotnet build ProjectAegis.sln
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter FullyQualifiedName~PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter FullyQualifiedName~MessageLog
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter FullyQualifiedName~MessageLog
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter FullyQualifiedName~CombatVfx
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~CombatVfx|FullyQualifiedName~TransientEffects|FullyQualifiedName~MapCanvasTransient"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "FullyQualifiedName~Drg162OverlaySignoffProjectionTests|FullyQualifiedName~TacticalOverlayProjectionTests|FullyQualifiedName~MapCanvasOverlayGeometryTests"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~DelegationBridgeHostCatalogBindContractTests|FullyQualifiedName~MapPlaceholderPanelHostContractTests|FullyQualifiedName~MapCanvasOverlay"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter FullyQualifiedName~AttentionToast
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~AttentionToast|FullyQualifiedName~C2Clock"
```

Unity scripts under `#if UNITY_5_3_OR_NEWER` are **not** compiled by `ProjectAegis.sln`. Headless coverage for those hosts is source-contract tests (catalog bind, overlay HUD strings, transient renderer, toast UXML / scene builder).

---

## Risks for Track B (kinematics sibling)

1. **Do not replace `SimplePlayModeSimHost.Update`.** Pause early-return and the 1–8× accel loop now own the tick cadence. Pose integration must run **inside** that loop (or honor `Session.IsSimPaused` / `TimeAccelerationFactor`) so toast pause and compression stay authoritative (ADR-010).
2. **Demo watch auto-pauses Play Mode.** `AttentionToastPanelHost` calls `TrySeedDemoWatchAttention()` → `TIME: PAUSED` until ACK + RESUME. Kinematic motion will look frozen on first enter unless Track B documents that, or the owner ACKs.
3. **`AdvanceDecisionLog` is after `RunTick`.** Newly appended seed rows appear on the **next** host step in `LastMessageLog` / `LastCombatVfx`. Do not move that call into `DelegationBridge.Tick`.
4. **Keep `CatalogReader`.** Overlay edges are `if (catalog is not null)`. Clearing or delaying the Awake bind regresses DRG-162 (`DATALINKS: 0`).
5. **Shared map canvas.** Rings/edges (`MapCanvasOverlayRenderer`) and fire lines/impacts (`MapCanvasTransientEffectsRenderer`) both index unit positions from `LastMapSymbols`. Track B world/normalized poses should update `MapSymbolEntry` (or the snapshot the picture bridge reads) — do not invent a second position authority.
6. **VFX dirty flag is reference equality.** `LastCombatVfx = CombatVfxProjection.Project(...)` allocates a new frame whenever outcomes are live, so the map rebinds every tick during VFX. Empty frames reuse `CombatVfxFrame.Empty`. Pose-only motion already dirties via `LastMapSymbols` reference. Avoid extra per-frame catalog/VFX rebuilds on the kinematic path.
7. **Still forbidden:** `DelegationBridge` Tick hotpath, `CatalogWriteGate`, Baltic v2 golden hash.

---

## Residual (not this verifier)

- Owner Game View signoff for rings/edges/toasts/VFX (DRG-162 visual AC).
- Live Baltic classify/engage firehose in Editor (stub `ActiveEngagementCount => 0`).
- CMD-39 REQ-20 append (owner-gated).
- Stale sentence in `2026-08-17-playmode-visual-audit.md` §1 still says `PolicyUpdate` falls through — **superseded** by Track A telemetry.
