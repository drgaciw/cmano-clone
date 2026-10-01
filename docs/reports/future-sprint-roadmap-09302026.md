# Future sprint roadmap — requirements and delivery reconciliation

**Date:** 2026-09-30. **Stage:** Release; Launch remains deferred. **Authority:** user authorized the review recommendations and artifact updates. This planning revision supersedes [2026-09-15](future-sprint-roadmap-09152026.md); it does not accept undelivered product features or the owner's Play Mode gate.

**Live cross-system record:** [Notion requirements/backlog/roadmap reconciliation](https://app.notion.com/p/3ebf7cb4e4df81cd80b7ff559888a0bd). Linear issues below carry scoped delivery criteria; this snapshot and Notion preserve planning maturity separately from implementation and owner acceptance.

## Current program and truth model

S122.0–S127 remains the numbered execution program. The [index](../../production/sprints/sprint-122-127-index.md), [machine status](../../production/sprint-status-s122-s127.yaml), [dashboard](project-dashboard.md) and [shared QA plan](../../production/qa/qa-plan-sprint-122-127-rebaseline-2026-09-30.md) are reconciled together. Original calendar windows below are historical forecasts, not evidence of completion or newly promised dates. Outstanding predecessor criteria gate downstream execution; independent documentation and feasibility work may proceed in parallel.

Track three independent dimensions: **document maturity** (draft/reviewed/approved), **delivery** (planned/in progress/implemented/verified), and **acceptance** (pending/accepted by named owner). A merged implementation, live backlog status, a passing headless test and an owner acceptance describe different facts. Frozen MVP grades and completed Release v1/Spirit 1 milestones remain historical.

| Sprint | Original forecast | Scope and current gate | Must budget |
| --- | --- | --- | --- |
| S122.0 | 09-15–09-16 | Hygiene evidence, cycle/cap state and row ownership must be checked; no administrative closeout inferred. | 2.0d hygiene |
| S122 | 09-15–09-19 | Tracker/G1 matrix, requirement links and usable graph evidence remain prerequisites. DRG-234/242 Backlog; DRG-197 In Progress. | 4.0d |
| S123 | 09-22–09-26 | Entry work DRG-243/246 Backlog; Slice A harness DRG-183 In Progress; DRG-244 Backlog. Retain interim owner walk. | **4.0d**: 3.5d agent + 0.5d owner |
| S124 | 09-29–10-03 | Commit/delegation criteria depend on verified entry path. Calendar arrival does not bypass S123 evidence. | 2.75d |
| S125 | 10-06–10-10 | G3/editor honesty DRG-236/274 Backlog; Excel-primary ADR-011 retained. | 3.5d |
| S126 | 10-13–10-17 | Thin naval symbology, explicit CIV decision or MIL-only fallback; no certification claim. | 3.0d |
| S127 | 10-20–10-24 | Residual G2/G3 + regression/QA evidence + final owner Play Mode decision. DRG-208 Todo. | 3–4d |

S123-02's optional 0.25d briefing fixture enrichment moves to Should. The mandatory briefing panel still displays existing package content or an explicit missing-content state; its dependencies no longer require optional enrichment. Smoke, evidence indexing and the owner walk remain Must. This restores the 4.0d ceiling instead of moving the excess to hidden owner work. All sprints require the shared QA plan and evidence; the missing-plan warning waiver is removed. The plan is authored, execution and sign-off remain pending.

## Current evidence and backlog reconciliation

September 30 board reads before reconciliation: DRG-234/242/275/243/246/244/239/235/236/274 **Backlog**; DRG-183/197/169/258 **In Progress**; DRG-165/166/168/170 and DRG-208 **Todo**; DRG-181/182 **Done**. Therefore Slice B's September 15 blanket In Review narrative is historical, and remaining acceptance criteria need reconciliation per story rather than a mass Done transition. Preserve implementation evidence and close only criteria actually proven. Reuse DRG-188 for workflow consistency, DRG-275 for Map/C2 design completion, DRG-197/323/324 for graph correctness, DRG-201/234/244 for evidence quality.

After verified artifact delivery, DRG-342/343/344 and existing DRG-275 are **In Review** for the locally authored checker and published Core/Weapons/Map design drafts. The Notion designs remain Draft / owner review pending. DRG-345–349 remain gated Backlog feature work; DRG-208 stays Todo. See the [closeout](../superpowers/reviews/2026-09-30-planning-reconciliation-closeout.md).

Core/combat project descriptions report main `1902dc1299678ea17f75014406fd9c616e5576bd` on September 27; this is a remote planning/acceptance reference. Local reviewed checkout is `4190fb1b49cd15775b6b995b7c245d73828cd7a2` with existing uncommitted changes; it is not proof of the target revision. Align DRG-208 and the Notion evidence index to `1902dc12`, identifying local delta on every acceptance row. Older `7a097f9` and `da6c5c64` records remain revision-labeled history. Final acceptance is pending and owner-only.

The preceding review reported 3,352 solution / 26 proxy smoke / six canonical replay tests and zero-warning single-worker build using SDK 8.0.425 on that local dirty checkout. These are historical review results, not new execution of this plan, exact-8.0.400 compliance, Editor coverage or owner acceptance. GitNexus overload/index defects and same-name checkout ambiguity require qualified source and path evidence before implementation. No simulation symbol is edited by this planning revision.

Fresh September 30 local verification subsequently passed the final full solution **3,352 / zero failed or skipped**, smoke **26/26**, canonical `ReplayGoldenSuiteTests` **6/6**, and single-worker build **zero warnings/errors**. See the [dashboard snapshot](dashboard-snapshots/2026-09-30-planning-reconciliation.md) for exact local source-input identity and rerun provenance. This does not execute every planned sprint criterion or accept `1902dc12`; toolchain remains SDK 8.0.425 with exact 8.0.400 unverified.

## Follow-on milestones — scoped, unscheduled

H4–H6 intake is already tracked and is extended rather than duplicated. No sprint number, date, accepted ADR or estimate is invented. Design pages use eight substantive sections, named role ownership, source/evidence links and explicit maturity. Complete Simulation and Weapons design alongside delivery-relevant Map/C2 work.

Exact proposed clauses are defined in the [requirements reconciliation specification](../superpowers/specs/2026-09-30-requirements-planning-reconciliation.md): **AME-6.11** (H4 capability disclosure), **LIB-05.1…3** (H5 content failure isolation), **RPL-29…31** (H6 deterministic save/resume), and **VER-07.3…6** (revision-bound acceptance evidence across the program). These are proposed amendments, not approved or delivered requirements.

| Milestone | Scope / acceptance design | Dependencies and release entry |
| --- | --- | --- |
| **H4 bounded editor slice** (intake DRG-335; delivery parent DRG-327; DRG-345/346) | Existing-map place/edit → validate → save → export/play for a bounded scenario/map workflow; success and recovery paths. Every offered role discloses runtime support; unsupported behavior is an explicit Export/Play finding, with equivalent CLI/GUI diagnosis. Save unfinished work remains allowed by its contract. | UX reconciliation DRG-335 → scope boundary DRG-333, plus topology ADR-017 decision DRG-334, G1/G3 evidence and live fixtures. WYSIWYG Platform Editor scope remains undecided; Excel-primary ADR-011 unchanged. |
| **H5 local presentation-content pilot** (intake DRG-332; delivery parent DRG-326; DRG-347) | One agreed presentation content class delivered locally; compatibility identity, agreed measured loading/performance budgets, missing/delayed/mismatched fallback, no changes to sim/order/RNG/replay state. | Inventory before H5 ADR DRG-331; accepted architecture and budgets before implementation scheduling. No remote distribution assumed. |
| **H6 deterministic save/resume first** (intake DRG-330; delivery parent DRG-325; DRG-348/349) | Compare uninterrupted and restored runs under identical build/scenario/catalog and later inputs; state and order fingerprints equal. Corrupt/incompatible save rejected without partial active-session replacement. | Feasibility DRG-330 + product scope DRG-328 → ADR DRG-329 → complete contract DRG-348 → vertical slice DRG-349 → estimate/schedule. Replay checkpoints provide hash verification, not session restoration. |
| **Multiplayer — separate later decision** | Define authority, transport, determinism/recovery and multiplayer acceptance independently from save/resume. | Separate product scope and feasibility/architecture decision; no implementation or date committed by H6's save/resume priority. |

## Backlog improvements and sequencing

Scoped children are now filed: [DRG-345](https://linear.app/drgamtd-workspace/issue/DRG-345) capability disclosure; [DRG-346](https://linear.app/drgamtd-workspace/issue/DRG-346) bounded existing-map workflow; [DRG-347](https://linear.app/drgamtd-workspace/issue/DRG-347) loading/failure isolation; [DRG-348](https://linear.app/drgamtd-workspace/issue/DRG-348) complete save contract and [DRG-349](https://linear.app/drgamtd-workspace/issue/DRG-349) restoration vertical slice, blocked by the contract. [DRG-343](https://linear.app/drgamtd-workspace/issue/DRG-343) Core and [DRG-344](https://linear.app/drgamtd-workspace/issue/DRG-344) Weapons design completion complement existing DRG-275 Map/C2.

[DRG-342](https://linear.app/drgamtd-workspace/issue/DRG-342), child of DRG-188, owns the artifact consistency check for contradictory current pointers, missing requirement links, nonexistent tracker sections and unowned acceptance criteria. Its [reconciliation manifest](../../production/planning-reconciliation-2026-09-30.json) and `tools/Test-PlanningArtifacts.ps1` provide reviewable consistency evidence. Prerequisite relations retain evidence/owner gates rather than auto-closing work from merge state.

Parallel-safe order: repository/Notion design and traceability completion + Linear reconciliation + artifact checks can run independently. Implementation follows current-graph impact and accepted seam/scope decisions. Entry and commit Editor hosts remain single-owner collision surfaces; tests and documentation do not manufacture owner approval. DRG-208 is the final product acceptance gate. Launch, Phase N, full APP-6 LOD, proprietary CMO `.db3`, v2 golden changes and DelegationBridge hotpath changes remain outside this program.

## Closeout criteria

Planning updates complete when repository, Linear and Notion references agree and link checks pass. Delivery complete requires sprint-specific implementation proof, common regression gates, Editor evidence where applicable, QA sign-off and named owner acceptance. Store the current result separately for each dimension; blocked acceptance keeps the sprint incomplete. See the shared QA plan for exact commands, measurable criteria and evidence identity.
