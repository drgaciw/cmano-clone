# S115 parallel kickoff (2026-08-11)

**Skill:** dispatching-parallel-agents  
**Decision:** Architecture/producer — Attention+auto-pause spine over asset wave 4 / Phase N / hold-only.

| Lane | Surface only | Owner theme |
|------|--------------|-------------|
| A | Pause-class event model + emit hooks (prefer pure + thin sim fact source) | S115-01 |
| B | `WatchAttention*` projection (new files under Projection/) | S115-02 |
| C | Session resume/override + tests + closeout | S115-03…05 |

**Do not:** edit DelegationBridge hotpath; touch CatalogWriteGate; change Baltic golden; invent Approved; open Phase N.

**Merge order:** A → B → C (or A∥B if pure types land first in shared model package without conflict).
