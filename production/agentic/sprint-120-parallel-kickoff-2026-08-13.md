# S120 parallel kickoff — 2026-08-13

**Linear:** [DRG-155](https://linear.app/drgamtd-workspace/issue/DRG-155) · **Predecessor:** S119 · **Epic:** DRG-149  
**Stage:** **Release** · **Not Launch**  
**Skill:** `dispatching-parallel-agents`  
**Plan:** [`production/sprints/sprint-120-c2-command-issuance.md`](../sprints/sprint-120-c2-command-issuance.md)

**Surfaces:** docs-only; **no C# lanes**

> **Do not rebuild CMD-31. Do not touch DelegationBridge hotpath.**  
> Do **not** rebuild `C2CommandIssuance` / `C2PlayerCommandBridge`. CMD-31…37 core = S108.

| Lane | Surface (allowed) | Does not write |
|------|-------------------|----------------|
| **A Inventory** | This kickoff + residual inventory in `production/sprints/sprint-120-c2-command-issuance.md` | Any `src/**`, `unity/**` C#, tests, `DelegationBridge*` |
| **B Hygiene** (optional, tiny) | Tracker/doc honesty under `production/**` or Linear/Hub wording only | Issuance types, toolbar hosts, PlayMode harness, overlays (S121) |

**Merge:** single docs PR if B fires; A may land as the open-sprint commit. No serial C# restack.

**Invariants:** ZERO DelegationBridge · CatalogWriteGate untouched · no new command pipeline · Baltic hash `17144800277401907079` · suite floor cite ≥1638/0f (no C# → no suite rerun required).

**After S120:** S121 overlays remain residual-scope first (CMD-32 already on trunk). Do not co-dispatch overlay C#.
