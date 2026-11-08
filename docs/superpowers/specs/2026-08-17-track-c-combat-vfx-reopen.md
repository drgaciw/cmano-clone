# Track C — Combat VFX Reopen (2026-08-17)

**Status:** Owner-selected scope change — **reopened**  
**Date:** 2026-08-17  
**Parent audit:** `docs/superpowers/reviews/2026-08-17-playmode-visual-audit.md`  
**Art bible:** `design/art/art-bible.md` §7  
**ADR:** [ADR-010](../../architecture/adr-010-headless-first-command-driven-ui.md) (UI is a presentation client; no sim authority)

---

## Scope amendment

Owner selected **Track C** as a scope change. Art-bible §7 was Formal N/A for Baltic v1 (no combat particles / decorative VFX). This document **reopens §7** for a **presentation-only** CMO-style engagement picture:

| In scope (MVP) | Out of scope |
|----------------|--------------|
| Deterministic UI Toolkit **fire lines** (shooter → target) | VFX Graph / ParticleSystem / screen shake |
| Deterministic **impact markers** at victim pose | Cinematic camera chase or 3D ballistic arcs |
| New `CombatVfxProjection` from `DecisionLog` outcomes | Unity as sim authority |
| Separate transient layer (not static rings/edges) | `DelegationBridge` hotpath |
| Headless projection unit tests | `CatalogWriteGate` write paths |
| | Baltic v2 replay golden `17144800277401907079` |
| | `MessageLogProjection` PolicyUpdate / AgentDecision (Track A) |
| | Claiming Track B / CMD-38 kinematics done |
| | DRG-162 ring/edge overlay files |

**ASSET-021** (Combat Domains Hot-Tick HUD, Approved) stays HUD-only. Track C does **not** restyle, re-approve, or flip ASSET-021.

Particles remain **optional / deferred**. MVP is CMO tracks and lines first.

---

## GitNexus impact (pre-edit)

| Symbol | Direction | Risk | Action |
|--------|-----------|------|--------|
| `DecisionLog` | upstream | **HIGH** if edited | **Read-only** consumer via `ChronologicalEntries()` — no append/fingerprint change |
| `MessageLogProjection` | upstream | LOW | **Not edited** (Track A owns PolicyUpdate) |
| `MapCanvasOverlayRenderer` | not in index / sibling DRG-162 | — | **Not edited** — new transient renderer instead |
| `MapPlaceholderPanelHost` | upstream | LOW | Additive hook only |
| `DelegationBridgeHost` | upstream | LOW | Additive `LastCombatVfx` after `Tick` (not inside `DelegationBridge`) |

Blast radius accepted: new projection + new Unity layer. No HIGH/CRITICAL edits to hub symbols.

---

## Elevator intent

Play Mode engagement feedback must be more than a message-log line. When `MvpEngagementResolver` writes an `EngagementOutcome` to `DecisionLog`, the map shows a **transient fire line** and an **impact marker** at the last known symbol poses. Geometry is a pure function of `(log, symbol positions, nowSimTime)`. Wall-clock, `PkDraw`, and `Random` never participate.

---

## Acceptance criteria

1. **Art-bible reopen.** §7 records a dated Track C reopen: CMO-style lines/markers permitted; non-deterministic decorative VFX still prohibited.
2. **Projection.** `CombatVfxProjection.Project` emits fire lines + impact markers from `OrderLogEntryKind.EngagementOutcome` only. Launch-without-outcome and aborted `Engagement` rows emit nothing (no target id on `EngagementRecord`).
3. **Transient hold.** Lines live `FireLineHoldSeconds` of **sim time**; markers live `ImpactHoldSeconds`. Pause freezes VFX (evidence-safe).
4. **No RNG.** `PkDraw` is ignored. Same log + positions + sim time ⇒ identical frame.
5. **Separate layer.** `MapCanvasTransientEffectsRenderer` draws on `map-combat-vfx-*-layer` elements. `MapCanvasOverlayRenderer` remains rings/edges only.
6. **Host wire.** `DelegationBridgeHost.RunTick` sets `LastCombatVfx` **after** `Bridge.Tick` and `LastMapSymbols`. `MapPlaceholderPanelHost` syncs the transient layer. Unity does not issue orders or mutate `DecisionLog`.
7. **Headless tests.** `CombatVfxProjectionTests` cover empty, endpoints, expiry, abort/launch-only, style map, PkDraw ignore, destroyed-victim pose.

---

## MVP behavior

```text
DecisionLog.EngagementOutcome
        │  (read-only)
        v
CombatVfxProjection.Project(log, mapSymbols, simTime)
        │  CombatVfxFrame { FireLines, ImpactMarkers }
        v
DelegationBridgeHost.LastCombatVfx
        │
        v
MapCanvasTransientEffectsRenderer.Sync   ← separate from MapCanvasOverlayRenderer
```

- Fire line: shooter normalized xy → victim normalized xy (hash layout until Track B kinematics).
- Impact: small marker at victim xy; USS class by Hit / Kill / Miss / Intercept.
- Missing either endpoint ⇒ skip that engagement (no invented pose).
- Destroyed victims keep last `MapSymbolEntry` pose so kill markers still land.

---

## Non-goals / forbidden

- `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs` hotpath
- `CatalogWriteGate` write paths
- Replay golden hash changes
- `MessageLogProjection` PolicyUpdate work
- Claiming Track B (CMD-38) done
- Editing DRG-162 overlay renderer / geometry as the VFX vehicle

---

## Follow-ups

- Game View sign-off of fire lines on `DelegationSmoke`
- Optional low-risk particles (still deterministic; reduced-motion off)
- Pair pre-commit illumination vectors (**CMD-30.5**) vs post-shot tracks
- Globe host (`GlobeMapProductHost`) transient layer
- `EngagementRecord` has no target id — launch-in-flight line needs a schema extension (not this MVP)
