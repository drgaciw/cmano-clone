# Requirements and planning reconciliation — 2026-09-30

**Status:** Planning update authorized; proposed product clauses await design and owner acceptance. Implementation is backlog work.

This reconciliation connects the live [requirements index](../../../Game-Requirements/Game-Requirements-Index.md), [implementation tracker](../../../Game-Requirements/implementation-tracker.md), and [RTM](../../architecture/requirements-traceability.md). Frozen S56 grades and dated snapshots remain historical evidence. A complete draft, a landed implementation, a passing test run, and product acceptance are four different facts.

## Evidence and acceptance vocabulary

| Field | Required meaning |
| --- | --- |
| Document maturity | Draft, reviewed, or approved, with reviewer/date/reference. Complete draft text does not imply approval. |
| Implementation evidence | Specific source path, symbol, commit/build identity, and supported subset; state residual behavior explicitly. |
| Test evidence | Criterion, test layer, command/filter, actual result, artifact location, and revision/input identity. Test source presence is a candidate mapping until a run is recorded. |
| Owner acceptance | Pending, accepted, or rejected, with owner/date/reference and accepted revision. Engineering delivery cannot populate this field automatically. |

Drafts 23–27 are visible in the index, tracker, and RTM without promoting them to approved requirements. Existing code/test mappings identify implemented subsets; they do not accept an entire document or a newer build. Product acceptance for the Combat UX entry path remains bound to [DRG-208](https://linear.app/drgamtd-workspace/issue/DRG-208).

## Proposed requirement amendments

| ID | Parent | Normative behavior | Acceptance evidence needed | Implementation/acceptance |
| --- | --- | --- | --- | --- |
| AME-6.11 | [Req 11](../../../Game-Requirements/requirements/11-Agentic-Mission-Editor.md), H4 / [DRG-345](https://linear.app/drgamtd-workspace/issue/DRG-345) / [DRG-346](https://linear.app/drgamtd-workspace/issue/DRG-346) / DRG-333 / G3 | Every offered mission role exposes its runtime support for the selected build and scenario. Unsupported roles produce a stable finding on Export/Play; Save preserves unfinished work. CLI and GUI use the same domain assessment. | Table-driven supported/unsupported roles, persisted Save round-trip, rejected Export/Play, same code/severity across CLI and GUI, no false claim that accepting a role executes it. | Proposed; no implementation or product acceptance claimed. |
| RPL-29…31 | [Req 17](../../../Game-Requirements/requirements/17-Replay-AAR-And-Order-Log.md), H6 / DRG-328–330 / [DRG-348](https://linear.app/drgamtd-workspace/issue/DRG-348) contract → [DRG-349](https://linear.app/drgamtd-workspace/issue/DRG-349) slice | Same-build, same-scenario/catalog/policy save/resume preserves authoritative state and future deterministic execution. Corrupt/incompatible input fails before mutating the active session. | Uninterrupted versus resumed differential runs with identical subsequent inputs; checkpoints after save compare authoritative state and order-log fingerprints; negative loads leave active state/log unchanged. | Proposed; feasibility/scope/ADR gates open. Replay checkpoints currently retain hashes, not restorable sessions. |
| LIB-05.1…3 | [Draft 27](../../../Game-Requirements/drafts/27-Scenario-Library-Campaigns.md), H5 / DRG-331 / [DRG-347](https://linear.app/drgamtd-workspace/issue/DRG-347) | Missing, delayed, corrupt, or incompatible presentation assets select a documented fallback and preserve simulation truth, orders, RNG, and replay fingerprints. | Fault-injected content loads against a baseline with the same simulation inputs; documented fallback/diagnostic; identical authoritative state/log fingerprints; measured performance against an agreed pilot budget. | Proposed; presentation content pilot and ADR pending. |
| VER-07.3…6 | [Draft 26](../../../Game-Requirements/drafts/26-Verification-CI-Gauntlet.md), DRG-201/234/244 | Acceptance evidence identifies criterion, revision/build, local delta, scenario/catalog/policy input hashes, test layer/result, artifacts, and accepting owner. Different revisions require explicit reassessment. | Required-field validation; mismatch/stale evidence rejected; historical evidence retained with its own identity; acceptance only records the owner's explicit action. | Proposed extension; existing oracle provenance is an implemented subset. |

## Implementation entry and delivery order

1. Resolve the current acceptance revision and local delta before collecting fresh acceptance evidence. Refresh scheduled, delivered, and accepted planning fields together; do not infer them from dates.
2. H4: reconcile editor topology and UX boundaries, then deliver a bounded place/edit → validate → save → export/play slice with AME-6.11. Platform Editor Excel-primary ownership remains [ADR-011](../../architecture/adr-011-platform-editor-excel-roundtrip.md); topology follows the decision process in [ADR-017](../../architecture/adr-017-editor-topology-client-vs-scenario-lab.md).
3. H5: finish content inventory and ADR before a local presentation-content pilot. Select one content class and record compatibility, fallback, and measured budgets before broader delivery.
4. H6: finish product scope and feasibility before the save/resume ADR and contract tests. Plan save/resume first; multiplayer receives a separate later scope/architecture decision. No multiplayer behavior is implied by RPL-29…31.

Presentation remains a client of headless state under [ADR-010 §2–3](../../architecture/adr-010-headless-first-command-driven-ui.md), [ADR-007](../../architecture/adr-007-c2-map-presentation.md), and [ADR-001](../../architecture/adr-001-sim-assembly-boundary.md). No simulation authority moves into content assets or UI hosts. `DelegationBridge` stays zero-touch and catalog write paths stay extend-only.

## Planning maintenance acceptance

- The requirements index links existing drafts 23–27 and all proposed amendments.
- Tracker and RTM separate document maturity, implementation mapping, test evidence, and owner acceptance.
- No S56 grade or dated snapshot is regraded; Phase N deferrals remain owner-gated.
- Relative Markdown links resolve, including draft links that previously assumed the `requirements/` directory.
- Linear children and roadmap entries reference these exact clause IDs. Artifact consistency is [DRG-342](https://linear.app/drgamtd-workspace/issue/DRG-342) under DRG-188; Core and Weapons design documentation are [DRG-343](https://linear.app/drgamtd-workspace/issue/DRG-343) and [DRG-344](https://linear.app/drgamtd-workspace/issue/DRG-344), alongside the existing DRG-275 Map/C2 work. Delivery tasks may enter implementation only after their stated scope/ADR/dependency gates.
- Required verification runs and results are recorded by the coordinating review; documentation changes alone do not establish product acceptance.
