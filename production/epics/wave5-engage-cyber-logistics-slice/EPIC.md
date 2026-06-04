# Epic: Wave 5 — Engage, Cyber Spoof, Readiness (Sprints 13–14)

> **Status:** Code-complete on `feat/wave5-attack-readiness-spoof` (Unity Editor manual sign-off still pending)  
> **Layer:** Sim + Delegation + Unity presentation  
> **Requirements:** [14](../../Game-Requirements/requirements/14-Engagement-And-Fire-Control.md), [16](../../Game-Requirements/requirements/16-Logistics-And-Magazines.md), [19](../../Game-Requirements/requirements/19-Cyber-And-Comms.md), [20](../../Game-Requirements/requirements/20-Command-And-Control-UI.md)  
> **Tracker:** Rows 14, 16, 19, 20 — [implementation-tracker-2026-06-04.md](../../Game-Requirements/implementation-tracker-2026-06-04.md)

## Goal

Ship P0 **spoof track runtime**, **live scenario readiness**, and **interactive attack menu** on the existing single engage resolver — deterministic order log + replay goldens.

## GitNexus

| Symbol | Risk |
|--------|------|
| `DelegationBridge` | CRITICAL — coordinate all bridge changes |
| `EngageAttackOptions` | LOW |
| `EngagePreviewProjection` | LOW |

Run `npx gitnexus impact <symbol> -d upstream -r cmano-clone` before edits.

## Stories

| ID | Story | Sprint | Status |
|----|-------|--------|--------|
| 001 | [story-001-spoof-track-runtime.md](story-001-spoof-track-runtime.md) | 13 | Ready |
| 002 | [story-002-spoof-c2-indicator.md](story-002-spoof-c2-indicator.md) | 13 | Ready |
| 003 | [story-003-live-readiness-json.md](story-003-live-readiness-json.md) | 13 | Ready |
| 004 | [story-004-readiness-order-log.md](story-004-readiness-order-log.md) | 13 | Ready |
| 005 | [story-005-attack-menu-projection.md](story-005-attack-menu-projection.md) | 14 | Ready |
| 006 | [story-006-attack-menu-ui.md](story-006-attack-menu-ui.md) | 14 | Ready |
| 007 | [story-007-attack-menu-smoke.md](story-007-attack-menu-smoke.md) | 14 | Ready |

## Acceptance (epic)

1. `baltic-patrol-spoof` and `baltic-patrol-readiness` replay PASS with pinned fingerprint.  
2. `CYBER_SPOOF_TRACK` and `AIR_NOT_READY` in manifest + message log.  
3. Player can pick an attack option in Unity; commit writes `PlayerEngage` order log row.  
4. Tracker rows updated with test paths.