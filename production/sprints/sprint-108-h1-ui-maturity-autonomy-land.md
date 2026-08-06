# Sprint 108 — H1 Land + UI Maturity + Autonomy

**Dates:** 2026-08-03 → 2026-08-08 (est.)  
**Stage:** **Release** · **Not Launch**  
**Program:** Serial land of open C2 / UI Maturity / autonomy stack (product progress, not residual paper)  
**Milestone:** Linear **H1 — C2 Runtime Depth**  
**Trackers:** [DRG-68](https://linear.app/drgamtd-workspace/issue/DRG-68) · [DRG-69](https://linear.app/drgamtd-workspace/issue/DRG-69) · [DRG-66](https://linear.app/drgamtd-workspace/issue/DRG-66)

## Goal

Land the open **C2 / UI Maturity / autonomy** stack onto `main` with serial merge discipline:
1. Orders no longer drop on `QueueForApproval` (**DRG-66** / #388)
2. CMD-31…37 command surface + Waves 2–6 on trunk
3. H1 moves from ~19% toward a real close trigger

## Must Have

| ID | Task | Acceptance |
|----|------|------------|
| S108-01 | Merge order + restack | One UI/Projection PR at a time; restack after each land |
| S108-02 | Land DRG-66 (#388) | CI green; human merge; QueueForApproval no longer drops |
| S108-03 | Land UI Maturity base (#382 + #390) | CMD-31…37 on main; suite floor held |
| S108-04 | Land Waves 2→6 | #383 → #384 → #385 → #386 → #387 serial |
| S108-05 | Suite floor | ≥1638/0f; Replay 6/6; preserve Baltic production hash `17144800277401907079`; ZERO DelegationBridge Tick rewrite |
| S108-06 | Closeout | smoke-sprint-108 + Linear H1 / DRG-66 update |

## Should Have

| ID | Task |
|----|------|
| S108-07 | #392 after #388 (CMD-11/26 feeds) |
| S108-08 | Wave 7 residual #393–#395 |
| S108-09 | DRG-50 fuel 60× Play Mode |
| S108-10 | CI lint #317 |

## PR merge order (Day-1 triage 2026-08-02)

```text
0. Prep restack on main tip a2c4c49
1. #390 → stack tip of #382 (WireClickHandlers + NormalizeStatus)  [APPROVED]
2. #393 → #394 → #395  Wave 7 docs/qa  [CI green; docs-only]
3. #382 (+ #390) → main  CMD-31…37 base
4. #383 → #384 → #385 → #386 → #387  Waves 2–6 (serial)
5. #388  DRG-66 PendingApprovalQueue (depends on Wave 6 branch)
6. #392  CMD-11/26 feeds (depends on #388)
```

**Hazard:** #382–#387 all target `main` and share Unity/projection surface — never parallel-merge.

## Day-1 checklist

1. [x] Plan ack (Notion + Linear DRG-68/69)
2. [ ] Merge #390 into #382 stack branch (Graphite/stack merge — not REST squash)
3. [ ] Land Wave 7 #393→#395 (or defer if review gate blocks)
4. [ ] Land #382 to main after #390 absorbed
5. [ ] Restack remaining UI waves; serial land
6. [ ] Land #388; verify queue
7. [ ] Suite floor + closeout

## Non-goals

Launch · invent Approved · DelegationBridge Tick hotpath · H3 store · H4 SE Phase 2 GUI · residual-only paperwork sprint

---
*S108 lean plan — 2026-08-02. Stage Release. Not Launch.*
