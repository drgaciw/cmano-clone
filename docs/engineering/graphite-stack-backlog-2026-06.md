# Graphite PR Backlog — Project Aegis (2026-06)

**Trunk:** `main` · **Graphite:** `gt submit --stack` from bottom branch of each stack.

## Stack A — Delegation (submit first)

| ID | Branch | PR title | Status |
|----|--------|----------|--------|
| DELEG-1 | `stack/delegation/sim-core` | feat(sim): policy, engage MVP, scenario JSON | Merged / in stack |
| DELEG-2 | `stack/delegation/orchestrator` | feat(delegation): ROE adapter, session, order log union | In stack |
| DELEG-3 | `stack/delegation/phase-gate` | feat(delegation): planning/execution phase gate | In stack |
| DELEG-4 | `stack/delegation/player-info` | feat(delegation): player info filter | In stack |
| DELEG-5 | `stack/delegation/bridge-engage` | feat(delegation): bridge MVP engage wiring | **Merged #13** |

```powershell
gt checkout stack/delegation/sim-core
gt submit --stack --no-interactive
```

**Gate per slice:** `dotnet test ProjectAegis.sln`

---

## Stack B — Sim engage (2 PRs, after A on main)

| ID | Branch | PR title | Status |
|----|--------|----------|--------|
| SIM-1 | `stack/sim/engage-scenario` | feat(sim): scenario-driven engage priming | **Merged #14** |
| SIM-2 | `stack/sim/engage-log` | feat(sim): engagement order-log contract | **Merged #15** |

```powershell
git checkout main && git pull
gt checkout main
gt create -m "feat(sim): scenario-driven engage priming [SIM-1]" stack/sim/engage-scenario
# commit SIM-1
gt create -m "feat(sim): engagement order-log contract [SIM-2]" stack/sim/engage-log
gt checkout stack/sim/engage-scenario
gt submit --stack --no-interactive
```

**Deferred:** `SIM-FUTURE` — `IPolicyEvaluator` inside `MvpEngagementResolver` (delegation gate covers MVP).

---

## Stack C — Unity (after SIM-1 or DELEG-5)

| ID | Branch | PR title | Status |
|----|--------|----------|--------|
| UNT-2 | `stack/unity/playmode-smoke` | feat(unity): play-mode smoke with engage | **Merged #16** |

Parent: `stack/sim/engage-scenario` or `stack/delegation/bridge-engage` tip.

---

## Stack D — Design (parallel 1-PR stacks from main)

| ID | Branch | PR title | Status |
|----|--------|----------|--------|
| DES-1 | `design/logistics-gdd` | docs(gdd): logistics and magazines | **Draft** on stack/sim/engage-scenario |
| DES-2 | `design/mission-editor-rereview` | docs(gdd): mission editor re-review | **APPROVED** (2026-06-01 follow-up on SIM-2 stack) |

---

## Stack E — Production (after DES-1)

| ID | Branch | PR title | Status |
|----|--------|----------|--------|
| PROD-1 | `chore/epics-mvp` | chore(prod): MVP epics | **Done** — `production/epics/baltic-headless-slice/` |
| PROD-2 | `chore/stories-sim-engage` | chore(prod): stories for sim engage epic | Not started |

---

## File ownership (avoid parallel PR conflicts)

| Path | Owner stack |
|------|-------------|
| `src/ProjectAegis.Delegation.UnityAdapter/**` | A (DELEG-5) |
| `unity/ProjectAegis/**` | A / C |
| `src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs` | B |
| `src/ProjectAegis.Sim/Engage/**` | B |
| `design/gdd/logistics-magazines.md` | D |

---

## Changelog

| Date | Note |
|------|------|
| 2026-06-01 | Backlog created; DELEG-5 bridge uses `SimulationSession.BindMvpEngagement` |
| 2026-06-01 | Integration pass: #13 merged main; #14–#16 restacked; logistics GDD Approved; Baltic epic scaffold |
| 2026-06-01 | Baltic slice **complete** on main (#17–#19); 105 tests; gate PASS — see `docs/reports/baltic-headless-slice-gate-2026-06-01.md` |
| 2026-06-01 | Next epic: `sensor-headless-slice` (ContactChange + scenario contacts); sensor GDD still In Review |
| 2026-06-02 | `sensor-headless-slice` implemented on branch `stack/sensor-contact-change` — 110 tests |