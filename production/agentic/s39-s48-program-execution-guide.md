# S39–S48 Program Execution Guide — Local + Cloud Agents

**Date:** 2026-06-20  
**Program:** 10-sprint release train (Polish S39–S41 → scope gate → Release Enablement S42–S48)  
**Authority:** [`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §9 + 10-sprint agent program plan

> **Footnote:** Spirit 1 (Vertical Slice MVP) gap remediation runs **orthogonal** to S40–S48 — parallel kickoff [`spirit1-gap-parallel-kickoff-2026-06-20.md`](spirit1-gap-parallel-kickoff-2026-06-20.md); does not block or replace sprint closeout order.

---

## Track B (S42–S48) — scope gate APPROVED

> **Scope gate APPROVED** 2026-06-20 — [`scope-expansion-decision-2026-06-20.md`](../gate-checks/scope-expansion-decision-2026-06-20.md). Track B boundary: [`release-enablement-scope-boundary-2026-06-20.md`](../release-enablement-scope-boundary-2026-06-20.md).  
> **Do not dispatch S42 agents** until **S40 + S41 closeout PASS** (Track A exit + S41 ADR + polish-exit pack).

---

## Sequential sprint order (never parallel full sprints)

```
S39 ──► S40 ──► S41 ──► [SCOPE GATE] ──► S42 ──► S43 ──► S44 ──► S45 ──► S46 ──► S47 ──► S48
```

| Sprint | Status (2026-06-20) | Next action |
|--------|---------------------|-------------|
| **S39** | **COMPLETE** — smoke PASS 1215/1215, Replay 6/6, proxy 18/18 | — |
| **S40** | Planned — in-boundary | `/qa-plan sprint 40` → parallel kickoff → dispatch |
| **S41** | Planned — in-boundary | After S40 closeout; produces scope-decision packet |
| **S42–S48** | **READY TO DISPATCH (docs only)** | Scope gate APPROVED; S42 blocked until S41 closeout |

---

## Per-sprint coordinator loop

1. Confirm sprint plan + parallel kickoff exist under `production/sprints/` and `production/agentic/`.
2. **Serial prereqs:** baseline (S*-01) + QA plan (S*-02) — blocks feature dispatch.
3. Bootstrap worktrees per [`s39-s48-worktree-manifest.md`](s39-s48-worktree-manifest.md).
4. Assign tracks per [`local-cloud-agent-routing.md`](local-cloud-agent-routing.md) (local: evidence/closeout/Editor; cloud: code/test/hygiene).
5. Dispatch one agent per track — isolated context, boundary cite, hard gates.
6. Per-track verify: `dotnet build`, `dotnet test ProjectAegis.sln`, PlayModeSmokeHarnessTests, ReplayGolden 6/6.
7. GitNexus: `impact()` before edits; `detect_changes()` before commit.
8. Merge stacks: baseline → code tracks → closeout; `gt sync` + `gt restack`.
9. Closeout: smoke, retro, `sprint-status.yaml`, session-state.
10. Hindsight retain in `dev-cmano-clone` bank with `[OUTCOME:]`.

---

## Track B dispatch order (after scope gate)

| Order | Sprint | Epic | Prereq |
|-------|--------|------|--------|
| 1 | S42 | B1 wave 1 + B2 start | Scope gate APPROVED + S41 closeout PASS |
| 2 | S43 | B1 wave 2 + B2 complete | S42 closeout |
| 3 | S44 | B3 structural debt | S41 ADR + scope decision |
| 4 | S45 | B4 perf scale-out | B1 scope locked (S43) |
| 5 | S46 | B5 launch artifacts | B1 + B2 complete |
| 6 | S47 | B6 dry run | S46 closeout |
| 7 | S48 | B6 release gate | S47 Go + human verdict |

Pre-flight each Track B sprint with [`sprint-42-48-readiness-checklist.md`](sprint-42-48-readiness-checklist.md).

---

## Standing gates (all sprints)

| Gate | S39–S41 | S42–S48 |
|------|---------|---------|
| Baltic hash `17144800277401907079` | Immutable | ADR for any bump |
| ReplayGolden | 6/6 | 6/6 |
| C2 proxy | 18/18+ | 18/18+ (expand matrix if new UI) |
| Test baseline | ≥1213, no regression | Monotonic growth |
| DelegationBridge | ZERO touch | ADR if touched |
| CatalogWriteGate | Extend-only | Per post-gate policy |
| Boundary cite | `polish-scope-boundary-2026-06-19.md` | `release-enablement-scope-boundary-2026-06-20.md` |

---

## Related artifacts

| Artifact | Path |
|----------|------|
| Roadmap §9 decomposition | `docs/reports/future-sprint-roadpmap.md` |
| Worktree manifest | `production/agentic/s39-s48-worktree-manifest.md` |
| Local/cloud routing | `production/agentic/local-cloud-agent-routing.md` |
| Scope gate template | `production/gate-checks/scope-expansion-decision-template-2026-06-20.md` |
| Track B readiness | `production/agentic/sprint-42-48-readiness-checklist.md` |
| Spirit1 gap remediation (orthogonal) | `production/agentic/spirit1-gap-parallel-kickoff-2026-06-20.md` |
| Coordination map | `.claude/docs/agent-coordination-map.md` |

---

*Created per 10-sprint agent program plan. S39 COMPLETE; S40–S41 executable in-boundary; S42–S48 blocked pending scope gate.*
