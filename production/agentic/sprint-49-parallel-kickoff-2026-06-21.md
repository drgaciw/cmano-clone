# Sprint 49 Parallel Kickoff — Agentic Kickoff (E2 Lead: MCP/OSINT + Infra)

**Date:** 2026-06-21 (planning artifact)  
**Trunk:** `main` @ post-S48 Release  
**Sprint plan:** `production/sprints/sprint-49-agentic-kickoff-mcp-osint-infra.md`  
**QA plan:** `production/qa/qa-plan-sprint-49-2026-06-21.md` (TBD @ S49-02)  
**Authority:** [`production/post-release-scope-boundary-2026-06-21.md`](../post-release-scope-boundary-2026-06-21.md)

> **READY TO DISPATCH** — post-release boundary published. E2 lead; Req **05** + **07** foundations only.

## Sprint Goal (recap)

Production-harden MCP/OSINT (Req 05) and agentic infra foundations (Req 07). Post-Release gate matrix + QA baseline. All v1.0 invariants held.

## Wave plan

| Wave | Stories | Track | Est. | Agent env | Notes |
|------|---------|-------|------|-----------|-------|
| Day-1 | S49-01 | Baseline + gate matrix | 1d | Cloud | Floor 1227 tests |
| W0 | S49-02 | QA plan | 1d | Cloud | Blocks feature waves |
| W1 | S49-03 | MCP production | 2.5d | Cloud | CLI + mcp-tools.json |
| W2 | S49-04, S49-07 | OSINT production + digest stub | 3.5d | **Local lead** | Osint/Catalog cluster |
| W3 | S49-05 | Agentic infra foundations | 2.5d | Cloud | Scenario validate + batch schema |
| W4 | S49-06, S49-08 | Closeout + tracker | 1d | **Local** | Smoke + status |

## Track ownership

| Track | Owner | Stories | Stack prefix | Agent env |
|-------|-------|---------|--------------|-----------|
| MCP production | c-sharp-engineer | S49-03 | `stack/sprint49/mcp-production` | Cloud |
| OSINT production | team-data | S49-04, S49-07 | `stack/sprint49/osint-production` | **Local lead** |
| Agentic infra | c-sharp-engineer + team-simulation | S49-05 | `stack/sprint49/agentic-infra` | Cloud |
| Baseline + QA | c-sharp-devops + team-qa | S49-01, S49-02 | `stack/sprint49/baseline-qa` | Cloud |
| Closeout | c-sharp-devops-engineer | S49-06, S49-08 | `stack/sprint49/closeout` | Local |

## File ownership matrix

| File / path | Owner track |
|-------------|-------------|
| `src/ProjectAegis.MissionEditor.Cli/**` | MCP production |
| `tools/mission-editor/mcp-tools.json` | MCP production |
| `src/ProjectAegis.Data/Osint/**` | OSINT production |
| `src/ProjectAegis.Data/Import/OsintCatalogMapper*` | OSINT production |
| `src/ProjectAegis.MissionEditor.Cli/**` (scenario_* verbs) | Agentic infra |
| `src/ProjectAegis.Delegation/**` (BalticBatchRunner if touched) | Agentic infra |
| `production/qa/gate-matrix-post-release-*.md` | Baseline + QA |
| `production/sprint-status.yaml` | Closeout |

## Hard gates

| Gate | Policy |
|------|--------|
| Headless tests | ≥ **1227**; monotonic |
| ReplayGolden | **6/6** |
| C2 proxy | **18/18+** |
| Baltic hash | **`17144800277401907079`** immutable |
| DelegationBridge | **ZERO touch** |
| CatalogWriteGate | **extend-only** |
| GitNexus | `impact()` + `detect_changes()` |
| Boundary cite | **`post-release-scope-boundary-2026-06-21.md`** on every artifact |

## GitNexus pre-flight (run before S49-03/04/05 edits)

| Symbol / area | Expected risk | Track |
|---------------|---------------|-------|
| `CatalogWriteGate` | CRITICAL — extend-only | OSINT |
| `OsintDigestRunner` | HIGH | OSINT, MCP |
| `OsintCatalogMapper` | MED–HIGH | OSINT |
| `Program` (CLI MCP) | LOW–MED | MCP |
| `ScenarioPackage` / validate commands | MED | Agentic infra |
| `BalticBatchRunner` | HIGH if sim bind | Agentic infra |

Re-index if stale: `node .gitnexus/run.cjs analyze`

## Worktree bootstrap

```
.worktrees/stack/sprint49/mcp-production
.worktrees/stack/sprint49/osint-production      ← Local lead
.worktrees/stack/sprint49/agentic-infra
.worktrees/stack/sprint49/baseline-qa
.worktrees/stack/sprint49/closeout               ← Local coordinator
```

Parent path: `/home/username01/cmano-clone/.worktrees/` (per AGENTS.md).

## Dispatch sequence

1. **S49-01** — baseline + `gate-matrix-post-release-2026-06-21.md`
2. **S49-02** — QA plan (blocks W1–W3)
3. **Parallel W1–W3** — S49-03, S49-04+07, S49-05 (after S49-02)
4. **S49-06** — closeout smoke; update sprint-status → S50 ready

## S50 prep (handoff)

- Req **07** scenario generation workers + Req **11** NL planner
- Plan TBD: `sprint-50-agentic-scenario-gen-mission-editor.md`
- Cite S49 MCP/OSINT + infra foundation artifacts

---

*S49 parallel kickoff 2026-06-21. Cites post-release-scope-boundary + future-sprint-roadpmap §9.*
