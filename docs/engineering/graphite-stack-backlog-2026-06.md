# Graphite PR Backlog — Project Aegis (2026-06)

**Trunk:** `main` · **Graphite:** `gt submit --stack` from bottom branch of each stack.

## Stack A — Delegation (submit first)

| ID | Branch | PR title | Status |
|----|--------|----------|--------|
| DELEG-1 | `stack/delegation/sim-core` | feat(sim): policy, engage MVP, scenario JSON | Merged / in stack |
| DELEG-2 | `stack/delegation/orchestrator` | feat(delegation): ROE adapter, session, order log union | In stack |
| DELEG-3 | `stack/delegation/phase-gate` | feat(delegation): planning/execution phase gate | In stack |
| DELEG-4 | `stack/delegation/player-info` | feat(delegation): player info filter | In stack |
| DELEG-5 | `stack/delegation/bridge-engage` | feat(delegation): bridge MVP engage wiring | **Open** |

```powershell
gt checkout stack/delegation/sim-core
gt submit --stack --no-interactive
```

**Gate per slice:** `dotnet test ProjectAegis.sln`

---

## Stack B — Sim engage (2 PRs, after A on main)

| ID | Branch | PR title | Status |
|----|--------|----------|--------|
| SIM-1 | `stack/sim/engage-scenario` | feat(sim): scenario-driven engage priming | **In progress** (branch pushed) |
| SIM-2 | `stack/sim/engage-log` | feat(sim): engagement order-log contract | Not started |

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
| UNT-2 | `stack/unity/playmode-smoke` | feat(unity): play-mode smoke with engage | Not started |

Parent: `stack/sim/engage-scenario` or `stack/delegation/bridge-engage` tip.

---

## Stack D — Design (parallel 1-PR stacks from main)

| ID | Branch | PR title | Status |
|----|--------|----------|--------|
| DES-1 | `design/logistics-gdd` | docs(gdd): logistics and magazines | **Draft** on stack/sim/engage-scenario |
| DES-2 | `design/mission-editor-rereview` | docs(gdd): mission editor re-review | **NEEDS REVISION** logged 2026-06-01 |

---

## Stack E — Production (after DES-1)

| ID | Branch | PR title | Status |
|----|--------|----------|--------|
| PROD-1 | `chore/epics-mvp` | chore(prod): MVP epics | Not started |
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