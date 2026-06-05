# Wave 5 Requirements Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` or `executing-plans`. Steps use checkbox (`- [ ]`) syntax.  
> **GitNexus:** `npx gitnexus impact <symbol> -d upstream -r cmano-clone` before edits; `npx gitnexus detect-changes` before PR.

**Goal:** Implement tracker Wave-5 rows (req 14, 16, 19, 20): spoof track, live readiness, interactive attack menu.

**Architecture:** Extend existing scenario policy JSON → `ScenarioPolicyProfile` → `DelegationBridge` session hooks → `SimulationSession` engage resolver. No second engage path.

**Tech Stack:** .NET 8, `ProjectAegis.Sim`, `ProjectAegis.Delegation`, Unity UI Toolkit adapters.

**Sprints:** 13 (spoof + readiness), 14 (attack menu)

---

## Task 1: Spoof track runtime (req 19)

**Files:**
- Create/Modify: `src/ProjectAegis.Delegation/Comms/SpoofTrackTimelineSimulator.cs`
- Modify: `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs` (**CRITICAL** impact)
- Modify: `src/ProjectAegis.Sim/Scenario/ScenarioPolicyJsonLoader.cs`
- Test: `src/ProjectAegis.Delegation.Tests/Comms/SpoofTrackTimelineSimulatorTests.cs`
- Test: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessSpoofTests.cs`
- Data: `data/scenarios/baltic-patrol-spoof.policy.json`

- [ ] **Step 1:** Run `gitnexus impact DelegationBridge -d upstream -r cmano-clone` — attach summary to PR
- [ ] **Step 2:** Write failing test `Spoof_policy_aborts_engage_with_CYBER_SPOOF_TRACK` (if not green)
- [ ] **Step 3:** Implement `SpoofTrackTimelineSimulator.Advance` + `IsSpoofed`
- [ ] **Step 4:** Wire `Session.IsContactSpoofed` in bridge; pass `TrackSpoofed` to engage context
- [ ] **Step 5:** Assert `EngagementAbortReason.TrackSpoofed` → `CYBER_SPOOF_TRACK` in `EngagePreviewProjection`
- [ ] **Step 6:** Run `dotnet test` + `/replay-verify` for `baltic-patrol-spoof`
- [ ] **Step 7:** `gitnexus detect-changes`

---

## Task 2: Live unit readiness (req 16)

**Files:**
- Modify: `src/ProjectAegis.Sim/Scenario/ScenarioPolicyJsonLoader.cs` (`unitReadiness` block)
- Modify: `src/ProjectAegis.Delegation/Sim/UnitReadinessMap.cs`
- Modify: `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs`
- Test: `src/ProjectAegis.Sim.Tests/Scenario/ScenarioPolicySpoofReadinessJsonTests.cs`
- Test: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessReadinessPolicyTests.cs`
- Data: `data/scenarios/baltic-patrol-readiness.policy.json`

- [ ] **Step 1:** Impact `SimulationSession` before readiness hook changes
- [ ] **Step 2:** Failing test: readiness from policy JSON without harness-only `unitReadiness` override
- [ ] **Step 3:** Load `UnitReadiness` dictionary in loader → profile
- [ ] **Step 4:** Apply `UnitReadinessMap` on session at scenario start (bridge/harness)
- [ ] **Step 5:** Engage abort `AIR_NOT_READY` when `ReadyForLaunch == false`
- [ ] **Step 6:** Optional: `ReadinessStateChange` order-log row on transition (should-have)
- [ ] **Step 7:** Replay golden + detect-changes

---

## Task 3: Attack options projection (req 14)

**Files:**
- Modify: `src/ProjectAegis.Delegation/Projection/EngageAttackOptions.cs`
- Modify: `src/ProjectAegis.Delegation/Projection/EngageAttackOrderResolver.cs`
- Test: `src/ProjectAegis.Delegation.Tests/Projection/EngageAttackOptionsTests.cs`

- [ ] **Step 1:** Impact `EngageAttackOptions` (LOW expected)
- [ ] **Step 2:** Tests for disabled options when abort (spoof, readiness, comms, ROE)
- [ ] **Step 3:** Add salvo count + weapon id labels per CMO §4.1.1 minimum set
- [ ] **Step 4:** `EngageAttackOrderResolver.Resolve` maps selection → `PlayerEngage` payload

---

## Task 4: Unity attack menu UI (req 20)

**Files:**
- Create: `unity/ProjectAegis/Assets/Scripts/Runtime/AttackOptionsPanelHost.cs` (or extend unit detail host)
- Modify: `src/ProjectAegis.Delegation/Projection/UnitDetailPanelBinder.cs`
- Test: PlayMode or EditMode binder test

- [ ] **Step 1:** UX: confirm control pattern (dropdown vs context menu) — user approval
- [ ] **Step 2:** Failing binder test: N options rendered from projection
- [ ] **Step 3:** UI Toolkit host binds `EngageAttackOptions.Build` list
- [ ] **Step 4:** Selection calls `TryEnqueueHumanOrder` with resolved attack id
- [ ] **Step 5:** PlayMode smoke + manual sign-off checklist item
- [ ] **Step 6:** Avoid `DelegationBridge` tick changes — presentation-only diff preferred

---

## Handoff checklist

- [ ] `Game-Requirements/implementation-tracker-2026-06-04.md` rows 14, 16, 19, 20 updated  
- [ ] `production/sprint-status.yaml` story statuses  
- [ ] Hindsight `retain` with `[OUTCOME:]` and symbol names