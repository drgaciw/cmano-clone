# S120 parallel kickoff — 2026-08-13

**Linear:** [DRG-155](https://linear.app/drgamtd-workspace/issue/DRG-155) — **In Progress** · **Predecessor:** S119 **COMPLETE** ([DRG-154](https://linear.app/drgamtd-workspace/issue/DRG-154) · [#485](https://github.com/drgaciw/cmano-clone/pull/485)) · **Epic:** DRG-149  
**Stage:** **Release** · **Not Launch**  
**Skill:** `dispatching-parallel-agents`  
**Plan:** [`production/sprints/sprint-120-c2-command-issuance.md`](../sprints/sprint-120-c2-command-issuance.md)

**Surfaces:** docs-only; **no C# lanes**

> **Residual-scope only — not a new command pipeline.**  
> Do **not** rebuild CMD-31 or touch `DelegationBridge` hotpath.  
> `C2CommandIssuance` / `C2PlayerCommandBridge` + toolbar + headless tests = **S108** (confirm gaps; do not re-implement).

| Lane | Surface (allowed) | Status | Does not write |
|------|-------------------|--------|----------------|
| **A Inventory** | This kickoff + residual inventory in `production/sprints/sprint-120-c2-command-issuance.md` | **Done** (#486) | Any `src/**`, `unity/**` C#, tests, `DelegationBridge*` |
| **B Hygiene** (S120-03) | Tracker/doc honesty: S119 DoD, S120 residual-scope headers, Linear comment on DRG-155 | **In flight** (this lane) | Issuance types, toolbar hosts, PlayMode harness, overlays (S121) |

**Merge:** Lane A landed in [#486](https://github.com/drgaciw/cmano-clone/pull/486). Lane B = docs-only hygiene PR. No serial C# restack.

**Invariants:** ZERO DelegationBridge · CatalogWriteGate untouched · no new command pipeline · Baltic hash `17144800277401907079` · suite floor cite ≥1638/0f (no C# → no suite rerun required).

**After S120:** S121 overlays remain residual-scope first (CMD-32 already on trunk). Do not co-dispatch overlay C#.
