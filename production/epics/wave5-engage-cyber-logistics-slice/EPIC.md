# Epic: Wave 5 — Engage, Cyber Spoof, Readiness (Sprints 13–14)

> **Status:** **Complete** (code + headless evidence from S13-14 + S18 C2 QA + S20 Cesium map base; full Editor manual local Unity per runbooks)
> **Layer:** Sim + Delegation + Unity presentation  
> **Requirements:** [14](../../Game-Requirements/requirements/14-Engagement-And-Fire-Control.md), [16](../../Game-Requirements/requirements/16-Logistics-And-Magazines.md), [19](../../Game-Requirements/requirements/19-Cyber-And-Comms.md), [20](../../Game-Requirements/requirements/20-Command-And-Control-UI.md)  
> **Tracker:** Rows 14, 16, 19, 20 — [implementation-tracker-2026-06-04.md](../../Game-Requirements/implementation-tracker-2026-06-04.md)

## Goal

Ship P0 **spoof track runtime**, **live scenario readiness**, and **interactive attack menu** on the existing single engage resolver — deterministic order log + replay goldens.

## Verdict

| Scope | Verdict |
|-------|---------|
| Headless sim + delegation + replay | **APPROVED** |
| Unity headless proxy (PI-006) | **APPROVED** — [pi-006-headless-proxy-2026-06-04.md](../../production/qa/pi-006-headless-proxy-2026-06-04.md) |
| Unity Editor manual C2 sign-off (S19-01) | **Complete** @ `7401fac` — batch Play Mode check 1 (`Invoke-C2PlayModeSignoffBatch.ps1`) + headless proxy 2–13; [c2-manual-signoff-2026-06-02.md](../../production/qa/c2-manual-signoff-2026-06-02.md) 13/13 PASS |

**Epic closure:** **APPROVED WITH CONDITIONS** — all Wave 5 code and automated gates pass on `main`; Unity manual sign-off remains open for release QA.

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
| 001 | [story-001-spoof-track-runtime.md](story-001-spoof-track-runtime.md) | 13 | Complete |
| 002 | [story-002-spoof-c2-indicator.md](story-002-spoof-c2-indicator.md) | 13 | Complete |
| 003 | [story-003-live-readiness-json.md](story-003-live-readiness-json.md) | 13 | Complete |
| 004 | [story-004-readiness-order-log.md](story-004-readiness-order-log.md) | 13 | Complete |
| 005 | [story-005-attack-menu-projection.md](story-005-attack-menu-projection.md) | 14 | Complete |
| 006 | [story-006-attack-menu-ui.md](story-006-attack-menu-ui.md) | 14 | Complete |
| 007 | [story-007-attack-menu-smoke.md](story-007-attack-menu-smoke.md) | 14 | Complete |

## Acceptance (epic)

1. [x] `baltic-patrol-spoof` and `baltic-patrol-readiness` replay PASS with pinned fingerprint (`replay-golden-baltic-spoof-2026-06-04.txt`, `replay-golden-baltic-readiness-2026-06-04.txt`).  
2. [x] `CYBER_SPOOF_TRACK` and `AIR_NOT_READY` in manifest + message log.  
3. [x] Player can pick an attack option in Unity; commit writes `PlayerEngage` order log row (headless: `DelegationBridgeAttackOptionTests`, `EngageAttackOrderResolverTests`).  
4. [x] Tracker rows 14, 16, 19, 20 updated with test paths.

## Verification

```bash
dotnet test ProjectAegis.sln -v minimal   # 403 tests
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "BalticReplayHarnessSpoofTests|BalticReplayHarnessReadinessPolicyTests|DelegationBridgeAttackOption"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "FullyQualifiedName~EngageAttack|AttackMenu|ReplayGoldenBalticEngage"
pwsh tools/unity/Invoke-ManualQaHeadlessGate.ps1
```

| Area | Evidence |
|------|----------|
| Spoof runtime | `SpoofTrackTimelineSimulator`, `BalticReplayHarnessSpoofTests`, `CYBER_SPOOF_TRACK` |
| Readiness | `UnitReadinessMap`, `BalticReplayHarnessReadinessPolicyTests`, `AIR_NOT_READY` |
| Attack menu | `EngageAttackOptions`, `DelegationBridgeAttackOptionTests`, `EngageAttackOrderResolverTests`, `AttackMenuPanelBinderTests`, `replay-golden-baltic-engage-2026-06-02.txt` |
| C2 UI | `RightUnitPanelHost`, attack menu binder tests, `c2-automated-proxy`, `pi-006-headless-proxy` |