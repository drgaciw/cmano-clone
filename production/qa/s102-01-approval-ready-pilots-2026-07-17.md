# S102-01 Approval-Ready Pilots (Next Wave) — 2026-07-17

**Sprint:** S102 · **Stage:** Release  
**Path A:** Ready for human phrase only — **do not invent** `asset approved:`

## Prior Approved (S101-05)

| ID | Status |
|----|--------|
| ASSET-006 Message Log Panel | **Approved** |
| ASSET-021 Combat Domains Hot-Tick | **Approved** |

## Next pilots (pick 1–2)

| ID | Name | Path | Manifest | A7 |
|----|------|------|----------|-----|
| ASSET-004 | APP-6 Frame Atlas | `production/assets/c2/App6FrameAtlas.png` | **Approved** | Applied 2026-07-17 |
| ASSET-005 | C2 Top Bar Panel | `production/assets/c2/C2TopBarPanel.uss` | Done | Need `asset approved: ASSET-005` |

**Secondary queue (if capacity):** ASSET-014 (AegisTokens.uss), ASSET-018 (Baltic framing PNG), ASSET-019 (Band B contact overlay MD).

## Human phrase template (not claimed)

```
asset approved: ASSET-004
asset approved: ASSET-005
```

## Apply recipe (agent, after real phrase)

1. Record phrase in `production/qa/s102-*-human-asset-approval-*.md`  
2. Manifest Done→**Approved** for that ID only  
3. `/smoke-check` + commit + `gt submit`  
4. Stage remains **Release**

**Approved count today:** **3** (004 applied; 005 still waiting).

---
*S102-01 package — 2026-07-17. No auto-flip.*
