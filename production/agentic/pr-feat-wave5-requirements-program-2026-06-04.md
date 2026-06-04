# PR: Wave 5 + Post-MVP Requirements Program (Sprints 11–15)

**Branch:** `feat/wave5-attack-readiness-spoof` → `main`  
**Base:** `main` @ `2e0f90f`  
**Head:** `90131f3`  
**Diff:** 111 files · +3,920 / −236  
**Do not merge via this file** — description only; open/merge the PR in GitHub when ready.

---

## Summary

This PR delivers **Wave 5 gameplay** (cyber spoof track runtime, live scenario unit readiness, interactive attack menu) on the existing single engage resolver, and completes the **Post-MVP Requirements Program** (Sprints 11–15): requirements docs **01–12** matured to **FULL** with superpowers cross-links, RTM updated, and consistency gate **0 BLOCKER**.

| Track | Status |
|-------|--------|
| Wave 5 sim + delegation + Unity presentation | **Code-complete** (headless AC + replay) |
| Requirements maturity 01–12 | **Locked** per sprint (see below) |
| Unity Editor manual C2 sign-off | **PENDING** — does not block doc/program exit |

**Milestone:** [post-mvp-requirements-program.md](../milestones/post-mvp-requirements-program.md) — **COMPLETE** (2026-06-04).

### Commits (base → head)

| SHA | Message |
|-----|---------|
| `d3a8b83` | feat(wave5): interactive attack menu, policy readiness, spoof track runtime |
| `bc39480` | docs(sprint11): tracker baseline and archive delegation plan |
| `85e956b` | feat(wave5): attack menu, sprint 11 program, sprint 12 reqs 01-03 |
| `9164377` | chore(sprint12): record commit SHA 85e956b in sprint-status |
| `55ed03e` | docs(sprint12): lock requirements 01-03; close sprint 12 |
| `48e4743` | docs(sprint13): lock requirements 04-05; parallel agent tracks |
| `2c8df7d` | docs(sprint14): lock requirement 06; parallel data-layer tracks |
| `90131f3` | docs(sprint15): lock 07-08-12; RTM gate; program complete |

---

## Wave 5 (spoof, readiness, attack menu)

Epic: [wave5-engage-cyber-logistics-slice](../epics/wave5-engage-cyber-logistics-slice/EPIC.md)  
Plan: [2026-06-04-requirements-wave5-implementation.md](../../docs/superpowers/plans/2026-06-04-requirements-wave5-implementation.md)  
Requirements: **14** (engage), **16** (readiness), **19** (cyber/comms), **20** (C2 UI)

Architecture: scenario policy JSON → `ScenarioPolicyProfile` → `DelegationBridge` session hooks → `SimulationSession` / `MvpEngagementResolver`. **No second engage path.**

### 1. Cyber spoof track runtime (req 19)

| Item | Detail |
|------|--------|
| Runtime | `SpoofTrackTimelineSimulator` — `Advance(simTick)`, `IsSpoofed(contactId)` |
| Bridge | `DelegationBridge` wires `Session.IsContactSpoofed` and `TrackSpoofed` on engage context |
| Policy | `data/scenarios/baltic-patrol-spoof.policy.json` (`spoofTracks` → `ScenarioSpoofTransition[]`) |
| Abort | `EngagementAbortReason.TrackSpoofed` → manifest **`CYBER_SPOOF_TRACK`** |
| Tests | `SpoofTrackTimelineSimulatorTests`, `MvpEngagementSpoofTrackTests`, `BalticReplayHarnessSpoofTests` |

### 2. Live unit readiness (req 16)

| Item | Detail |
|------|--------|
| Loader | `ScenarioPolicyJsonLoader` — `unitReadiness` block on policy profile |
| Runtime | `UnitReadinessMap` primed on `SimulationSession` at scenario start |
| Policy | `data/scenarios/baltic-patrol-readiness.policy.json` |
| Abort | **`AIR_NOT_READY`** when `ReadyForLaunch == false` |
| Tests | `ScenarioPolicySpoofReadinessJsonTests`, `BalticReplayHarnessReadinessPolicyTests` |

### 3. Interactive attack menu (req 14 / 20)

| Item | Detail |
|------|--------|
| Projection | `EngageAttackOptions`, `EngageAttackOrderResolver`, `EngagePreviewProjection` abort preview |
| Bridge | `DelegationBridge.GetAttackMenuOptions`, `CommitAttackOption` → `PlayerEngage` order log |
| Unity | `AttackMenuPanelBinder`, `UnitDetailPanelBinder`, `RightUnitPanelHost`, `DelegationBridgeHost`, `UnitDetailPanel.uxml/.uss` |
| UX | Disabled fire when track spoofed, not ready, or comms/engage preview abort |
| Tests | `EngageAttackOptionsTests`, `EngageAttackOrderResolverTests`, `DelegationBridgeAttackOptionTests`, `AttackMenuPanelBinderTests`, `UnitDetailPanelBinderAttackMenuTests` |

### Epic acceptance (headless)

1. `baltic-patrol-spoof` and `baltic-patrol-readiness` replay **PASS** (pinned fingerprints).  
2. `CYBER_SPOOF_TRACK` and `AIR_NOT_READY` in `abort_reason_manifest.json` + order/message log.  
3. Attack option commit writes deterministic `PlayerEngage` row (replay suite).

---

## Requirements program — Sprints 11–15 (docs 01–12 locked)

Program index: [Agentic-Development-Plan.md](../../Agentic-Development-Plan.md) · [sprint-status.yaml](../sprint-status.yaml) (`requirements_program: complete`)

| Sprint | Goal | Docs locked | Evidence |
|--------|------|-------------|----------|
| **11** | Baseline + epic/story readiness + Wave 5 kickoff | Tracker baseline; epics `wave5` + `requirements-maturity` | [sprint-11-program-kickoff.md](../sprints/sprint-11-program-kickoff.md), [sprint-11-wave5-evidence](../qa/sprint-11-wave5-evidence-2026-06-04.md) |
| **12** | Requirements foundation | **01–03** (Overview, Core Loop, Simulation Modes) | [sprint-12-design-review](../qa/sprint-12-design-review-2026-06-04.md) |
| **13** | Delegation + dynamic systems agent | **04–05** | [sprint-13-design-review](../qa/sprint-13-design-review-2026-06-04.md) |
| **14** | Database intelligence maturity | **06** | [sprint-14-design-review](../qa/sprint-14-design-review-2026-06-04.md) |
| **15** | Infra + architecture + glossary + RTM gate | **07–08**, **12**; consistency **0 BLOCKER** | [sprint-15-design-review](../qa/sprint-15-design-review-2026-06-04.md), [requirements-consistency-2026-06-04.md](../../docs/reports/requirements-consistency-2026-06-04.md) |

### RTM / maturity (docs 01–12)

All rows **FULL** maturity in [requirements-traceability.md](../../docs/architecture/requirements-traceability.md). Locked superpowers specs cited for 02–04, 06; Wave 5 terms indexed in doc **12**.

| Doc | Title | Sprint lock |
|-----|-------|-------------|
| 01 | Project Overview | 12 |
| 02 | Core Gameplay Loop | 12 |
| 03 | Simulation Modes | 12 |
| 04 | Agent Delegation | 13 |
| 05 | Dynamic Systems Agent | 13 |
| 06 | Database Intelligence | 14 |
| 07 | Agentic Infrastructure | 15 |
| 08 | Agentic Architecture | 15 |
| 09–11 | Near-future / Speculative / Mission Editor | Pre-existing FULL |
| 12 | Terms Glossary | 15 |

**Out of scope (deferred):** GDD authoring for 01–12, req 05 OSINT agent, full CMO import Phase 2 → [sprint-16-backlog.md](../sprints/sprint-16-backlog.md).

---

## Test evidence

Evidence paths: [sprint-11-baseline-verify-2026-06-04.md](../qa/sprint-11-baseline-verify-2026-06-04.md), [sprint-11-wave5-evidence-2026-06-04.md](../qa/sprint-11-wave5-evidence-2026-06-04.md).

| Gate | Result | Notes |
|------|--------|-------|
| `dotnet build ProjectAegis.sln -c Release` | **PASS** | 0 errors, 0 warnings |
| `dotnet test ProjectAegis.sln -c Release` | **365/365 PASS** | +6 Delegation attack-menu tests vs Sprint 11 baseline (359) |
| PlayMode `PlayModeSmokeHarnessTests` | **7/7 PASS** | |
| Headless manual QA proxy | **18/18 PASS** | `Invoke-ManualQaHeadlessGate.ps1 -SkipBuild` |
| Wave 5 focused filter | **9/9 PASS** | spoof, readiness, engage attack |
| Replay golden suite | **15/15 PASS** | includes `baltic-patrol-spoof`, `baltic-patrol-readiness` |

```powershell
dotnet restore ProjectAegis.sln
dotnet build ProjectAegis.sln -c Release -v minimal
dotnet test ProjectAegis.sln -c Release -v minimal --no-build
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests -c Release --no-build
pwsh tools/unity/Invoke-ManualQaHeadlessGate.ps1 -SkipBuild
```

Post-merge: run `/replay-verify` if engage/order-log paths change on `main`.

---

## GitNexus notes

| Item | Detail |
|------|--------|
| Index | `npx gitnexus analyze` — **6,531 nodes** (2026-06-04, post Wave 5) |
| **`DelegationBridge`** | **CRITICAL** — ~77 upstream callers; all spoof/readiness/attack-menu wiring routes through bridge tick + engage bind |
| Other symbols | `EngageAttackOptions` LOW, `EngagePreviewProjection` LOW, `SimulationSession` HIGH (verify before edit) |
| CLI | `gitnexus impact DelegationBridge` blocked multi-repo label — fallback: [sprint-13-gitnexus-delegation-bridge-2026-06-04.md](../qa/sprint-13-gitnexus-delegation-bridge-2026-06-04.md) |
| Pre-merge | Re-run `npx gitnexus impact DelegationBridge -d upstream -r cmano-clone` and attach summary; `npx gitnexus detect-changes` after large data-layer doc moves |

**Reviewer rule:** Any follow-up PR touching `DelegationBridge` APIs or tick path requires impact report + full solution test + replay when order log changes.

---

## Manual QA still needed (C2 signoff)

Headless gates **do not** replace Unity Editor validation.

| Item | Status |
|------|--------|
| C2 manual checklist (12 checks) | **PENDING** — [c2-manual-signoff-2026-06-02.md](../qa/c2-manual-signoff-2026-06-02.md) |
| PI-006 Editor closure | **PENDING** — headless proxy PASS; Editor manual open |
| Wave 5 attack menu | Extend sign-off with attack-options / unit-detail fire button checks (Sprint 14 note) |
| Blocker for **this PR merge** | **No** — program protocol defers Editor sign-off; track in Sprint 16 backlog |

Human tester: Play Mode per `unity/ProjectAegis/PLAYMODE-SMOKE.md`, scenarios `baltic-patrol-classify`, `baltic-patrol-comms`, plus **`baltic-patrol-spoof`** / readiness policy smoke for Wave 5.

---

## Files changed (categories)

| Category | Examples |
|----------|----------|
| **Sim / engage** | `EngageContext.cs`, `MvpEngagementResolver.cs`, `EngagementAbortReason.cs`, `AbortReasonCatalog.Generated.cs` |
| **Scenario policy** | `ScenarioPolicyJsonLoader.cs`, `ScenarioPolicyJsonDto.cs`, `ScenarioSpoofTransition.cs`, `baltic-patrol-*.policy.json` |
| **Delegation / bridge** | `DelegationBridge.cs`, `SpoofTrackTimelineSimulator.cs`, `SimulationSession.cs`, `EngageAttackOrderResolver.cs`, projection binders |
| **Unity presentation** | `DelegationBridgeHost.cs`, `RightUnitPanelHost.cs`, `C2PresentationController.cs`, `UnitDetailPanel.uxml/.uss` |
| **Tests** | Sim, Delegation, UnityAdapter, Baltic replay harness spoof/readiness/attack suites |
| **Requirements docs** | `Game-Requirements/requirements/01`–`08`, `12` — maturity expansion + Wave 5 cross-links |
| **RTM / reports** | `requirements-traceability.md`, `requirements-consistency-2026-06-04.md`, `implementation-tracker-2026-06-04.md` |
| **Production / agentic** | Sprint 11–15 kickoffs, parallel tracks, protocol decisions, this PR description |
| **Epics / sprints** | `wave5-engage-cyber-logistics-slice/*`, `requirements-maturity-slice/*`, `sprint-status.yaml`, milestone |
| **QA evidence** | Sprint 11–15 design reviews, GitNexus delegation-bridge + data-layer notes |
| **Tooling / docs** | Superpowers install script, archived delegation plan, `abort_reason_manifest.json` |

---

## Reviewer checklist

- [ ] Wave 5: spoof abort, readiness abort, attack menu commit path traced in `DelegationBridge` only  
- [ ] Replay goldens green for new policy fixtures  
- [ ] Requirements 01–12 locks match RTM FULL rows  
- [ ] Consistency report: 0 BLOCKER  
- [ ] GitNexus `DelegationBridge` impact acknowledged (**CRITICAL**)  
- [ ] Schedule human C2 sign-off post-merge (non-blocking)

---

## Related

- Tracker: [implementation-tracker-2026-06-04.md](../../Game-Requirements/implementation-tracker-2026-06-04.md)  
- Next: [sprint-16-backlog.md](../sprints/sprint-16-backlog.md) (PR merge, GDD backlog, C2 sign-off)