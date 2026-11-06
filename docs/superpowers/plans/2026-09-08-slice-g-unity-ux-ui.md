# Slice G: Unity UX/UI Rebaseline and Acceptance Plan

> For agentic workers: use `superpowers:subagent-driven-development` or `superpowers:executing-plans` for the implementation tasks after their evidence and contract prerequisites are satisfied. No commits without user instruction.

**Goal:** Establish a truthful, testable UX/UI baseline across C2, Mission Editor and Platform Editor, then close verified presentation gaps through existing delivery owners.

**Architecture:** Unity reads immutable projections and submits intent through approved command facades. ADR-010 sections 2-3, ADR-007 and ADR-001 govern presentation. Editor authoring also follows ADR-011 and ADR-017.

**Tech stack:** .NET 8, Unity 6000.3.22f1, UI Toolkit, existing headless and Unity test assemblies.

**Authorization:** User approved proceeding with the proposed Slice G on 2026-09-08. This package establishes scope, tickets and acceptance criteria; it does not record completed UI implementation or owner acceptance.

**Delivery:** [DRG-233 epic](https://linear.app/drgamtd-workspace/issue/DRG-233), [G1 / DRG-234](https://linear.app/drgamtd-workspace/issue/DRG-234), [G2 / DRG-235](https://linear.app/drgamtd-workspace/issue/DRG-235), [G3 / DRG-236](https://linear.app/drgamtd-workspace/issue/DRG-236). All created in Backlog; G2/G3 depend on G1. [Notion scope and acceptance](https://app.notion.com/p/3d5f7cb4e4df812186c2db1a397b17d1) links the same records.

**Program links:** [Dated roadmap](../../reports/future-sprint-roadmap-09082026.md) and [dashboard snapshot](../../reports/dashboard-snapshots/2026-09-08-slice-g.md).

## Scope and Authority

The September 2 audit uses D, E and F as **finding categories**, not delivery slice definitions. Live Notion and Linear define combat delivery Slices A-C. This plan introduces G as discussed with the owner; it does not silently redefine D/E/F or assign new sprint numbers.

| Workstream | Responsibility | Relationship to G |
| --- | --- | --- |
| Audit D | Correct document defects | G supplies evidence and proposed amendments for D-10 and D-15; other defects stay outside G |
| Audit E | Traceability and status reconciliation | Reuse DRG-187 and DRG-188; one requirement-to-story-to-evidence mapping |
| Audit F | GitNexus reliability and graph coverage | Reuse DRG-197; freshness and usable impact results precede symbol edits |
| Slice G | Unity UX/UI baseline and acceptance | C2, Mission Editor and Platform Editor presentation, usability, evidence and bounded defect follow-up |

Notion owns requirements and design decisions. Linear owns delivery status and dependencies. Git owns implementation, reviewed requirement amendments and evidence. GitNexus supports code navigation and impact analysis; it does not prove delivery or human acceptance.

## Evidence Baseline

Evidence inspected on 2026-09-08 at HEAD `37e6ba6d382e736b694ed19bc27ca7240d92d372`:

- [Slice C plan](2026-09-07-slice-c-command-learn-adapt.md) and [implementation review](../reviews/2026-09-08-slice-c-implementation.md).
- [Slice C Play Mode acceptance](../reviews/2026-09-08-slice-c-playmode-acceptance.md): technical smoke PASS; owner acceptance pending. Six actual Unity tests passed. Screenshots cover 1600x979 and 1280x779; callback checks are not physical input tests.
- That package reports 3,216 headless tests, 24 proxy smoke tests and six canonical replay goldens. These are recorded prior evidence until rerun, not new measurements from this planning task.
- The smoke scene has no configured task group or authored live coverage geometry. Positive group/BDA retasking has headless proof; the coverage screenshots use a labeled presentation fixture. Neither proves the complete positive live Unity scenario.
- The layout correction, generated metadata and acceptance package were already uncommitted when this task began. Preserve them and retain their working-tree provenance.
- Live Linear still lists DRG-185 as Backlog. Code evidence, tracker status and owner acceptance are separate facts; do not automatically mark the epic Done.
- [S36 frame baseline](../../../production/perf/unity-c2-frame-baseline-s36-2026-08-01.md) proves headless bind timing, not Unity frame timing. BL-C2-01/02/03 remain unverified by that report.

## Global Constraints

- Keep `DelegationBridge.cs` zero-touch and CatalogWriteGate existing write paths unchanged.
- Preserve Baltic v2 hash `17144800277401907079`; isolate Baltic v3 policies and goldens.
- Preserve Excel-primary Platform Editor authoring (ADR-011); no second catalog authoring authority.
- No scene, prefab or metadata YAML hand-editing. Use the Editor workflow for serialized assets.
- Do not infer approved requirement IDs or statuses from an audit recommendation. Reconcile CMD-31 through CMD-39 with their actual source records; CMD-40 through CMD-43 remain proposals until their definitions and approval are verified.
- Release v1 / Spirit 1 remains closed; this is bounded presentation work, with no commercial launch scope.
- Every source-symbol edit requires fresh GitNexus upstream impact and an explicit report of HIGH/CRITICAL results.

## Delivery Tasks

### G1: Reconcile the Unity Requirement and Evidence Baseline

**Output:** `docs/superpowers/reviews/2026-09-08-slice-g-baseline.md`, produced during G1 execution; linked amendments to requirements 11, 20 and 21 where the evidence warrants them.

**Read:** `Game-Requirements/requirements/11-Agentic-Mission-Editor.md`, `20-Command-And-Control-UI.md`, `21-Platform-Editor.md`, and `Game-Requirements/reviews/audit-findings-2026-09-02.md`. Inspect existing Unity hosts and their headless consumers before proposing implementation.

- [ ] Build rows with requirement ID, canonical source, acceptance criterion, Unity surface, projection/command boundary, test, scenario, screenshot/log, commit plus local delta, Linear owner and owner acceptance.
- [ ] Keep delivery state separate from evidence class: source-only, headless-tested, Editor-tested, owner-accepted, or explicitly unverified. Each evidence label needs a dated source.
- [ ] Reconcile audit B-10, presentation portions of B-15, D-10 and D-15 against current evidence. Never copy September 2 statuses forward without checking.
- [ ] Resolve CMD-31 through CMD-39 ownership and duplicate definitions; propose Alerting and Interruption, approval UX and NFR amendments. Preserve any pending owner decision for CMD-38/39 and proposed CMD-40 through CMD-43.
- [ ] Compare findings with existing Linear stories before creating defects. Reference DRG-187/188 for cross-system linking; do not duplicate their acceptance matrix.

**Acceptance:** Every reviewed row has evidence or an explicit missing-evidence reason. No source-only or headless-only row is labeled visually accepted. Proposed requirement changes remain distinguishable from accepted requirements.

### G2: Validate Integrated C2 Workflows

**Depends on:** G1. **Read:** `unity/ProjectAegis/Assets/Scripts/Runtime/CommandReviewView.cs`, `CombatMapView.cs`, `CoverageMapView.cs`, and the Slice C evidence package. Route implementation defects to existing A/B/C stories where ownership already exists.

- [ ] Trace selection across map, OOB, contact details, own-unit details and command review. Inspect stale, unknown, empty and lost-comms states using grounded evidence.
- [ ] Validate command availability, refusal explanation, approval-required state and feedback without bypassing command facades.
- [ ] Exercise history inspection and return to live. Clearly distinguish historical and current-state controls; assert history cannot accidentally authorize a current action.
- [ ] Test alert priority, toast/auto-pause behavior and return to work against the accepted requirements. Record missing specifications rather than inventing behavior.
- [ ] Reuse DRG-175/192 and DRG-185 for the positive group/BDA scenario gap. Provide an authored live scenario or retain the explicit unverified acceptance row; do not use a presentation fixture as live proof.
- [ ] Reuse DRG-170/177/205 for keyboard focus, non-color recognition, accessible text, declutter and scenario-based usability checks. Capture failures as linked defects.

**Acceptance:** Each interaction has a named scenario and expected result, including denied/stale/unknown paths. All controls remain reachable at the baseline view sizes and supported UI scale extremes. Record callback automation separately from pointer/keyboard interaction. Owner acceptance stays explicit.

### G3: Validate Mission and Platform Editor Workflows

**Depends on:** G1. **Read:** `unity/ProjectAegis/Assets/Scripts/Runtime/ScenarioEditorShellHost.cs`, `ScenarioLibraryPanelHost.cs`, `LiveEditPanelHost.cs`, `PlatformEditorShellHost.cs`; existing authoring and platform tests in `src/ProjectAegis.Delegation.UnityAdapter.Tests/`.

- [ ] Walk scenario library load, authoring navigation, map selection, validation, save/export and return-to-play. Verify rejected export/publish states and recovery using existing authoring contracts.
- [ ] Walk catalog browse, selection, import preview, validation feedback, proposal status and return navigation; preserve Excel-primary editing and production approval gates.
- [ ] Record dirty-state handling, empty/error states, keyboard reachability, dense layouts and scale behavior. Compare with existing editor tickets before filing new ones.
- [ ] Reconcile D-10's claimed missing Unity surfaces and P0 AME gaps against source and evidence. Record genuine product gaps separately from stale documentation.
- [ ] Produce an HTML review under `docs/superpowers/reviews/` with actual captured screenshots and issue links, and open it in the available browser. Label any mockup explicitly; it cannot substitute for Editor evidence.

**Acceptance:** Each critical authoring workflow has a reproducible success path and a validation/recovery path. Proposed UI changes preserve authoring/runtime boundaries; production write approval remains human-gated.

### Shared Gates: Reuse Existing Owners

| Existing story | Contribution to G | Closure evidence |
| --- | --- | --- |
| DRG-197 | Audit F tooling prerequisite | Current path-specific graph; no WAL/FTS failure; query/context/impact can resolve current consumers |
| DRG-187 / DRG-188 | Audit E traceability | Requirement, issue, code and evidence links with one status authority |
| DRG-176 / DRG-26 | Bind and frame budgets | Actual Unity capture alongside headless measurements, with hardware, resolution, entity counts, sample window, mean/p95 and GC allocations |
| DRG-170 / DRG-177 / DRG-205 | Accessibility, replay, usability | Existing checks reused, evidence linked, failures owned |
| DRG-185 / DRG-175 / DRG-192 | Slice C acceptance and positive scenario | Owner decision and explicit disposition of live group/coverage gaps |

Do not reparent these stories or close them merely because G exists. G's exit requires the relevant acceptance evidence, not blanket completion of unrelated work in their parent epics.

## Execution Order

1. G1 baseline and audit E link reconciliation can start immediately. Recover audit F through DRG-197 before code edits requiring graph impact.
2. After G1, run G2 and G3 independently. Use isolated worktrees for implementation changes and one local Editor coordinator for captures. Shared composition roots remain serial integration work.
3. Route confirmed defects to their existing owners, implement under a concrete contract, then capture updated evidence. Headless tests precede Editor checks.
4. Merge requirement amendments through the audit D owner after G evidence is available. Refresh DRG-187/188 links and roadmap/dashboard together.
5. Present the evidence package for explicit owner acceptance. Planning approval and automated PASS results do not constitute UX signoff.

## Verification Contract

Run and read every command before closing implementation:

```powershell
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter ReplayGoldenSuiteTests
rg -n "17144800277401907079" tests data
git diff --check
```

Expected: zero build warnings/errors, zero test failures, no reduction below the observed 3,216-test baseline, at least 24 smoke tests and six canonical replay tests. Run `tools/verify-ci-local.ps1` before submission. Apply `production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md` and paste its verdict in the PR. Detect GitNexus changes before committing; commits and Graphite submission require user instruction.

For Editor work load `.claude/skills/team-unity/SKILL.md`, verify the endpoint and MCP ping, and use available tools. The prior package observed a session-related HTTP 400 with successful MCP ping, so a non-2xx root response alone is not proof of an outage. A refused connection means stay headless and mark visual checks unverified. Record the actual Editor/package versions: the prior package observed MCP 0.90.0 against the documented 0.86.0 pin; do not silently upgrade or downgrade.

## Exit Gate

- [ ] G1-G3 acceptance rows are verified or explicitly dispositioned by the owner with linked rationale.
- [ ] Requirement amendments, issue ownership and evidence links agree; no proposed ID is presented as approved.
- [ ] Headless, Editor, performance and owner acceptance results are separately reported.
- [ ] No new high-severity usability defect remains without an owner-approved disposition.
- [ ] Technical gates pass and protected source/golden boundaries remain intact.
- [ ] Owner accepts the scoped Unity UX/UI package; Release/Spirit 1 and launch decisions remain unchanged.

## Planning Review

**UCA verdict: PASS for the planning boundary only.** ADR-010/007/001 constraints and the authoring boundary are explicit. No production code, command path, assembly, plugin, scene or prefab is changed by this plan; implementation checklist sections 2.1-2.6 are N/A to this documentation diff. Section 2.7 citations and future verification requirements are included. This is not a completed Slice G implementation verdict.

**Fresh planning-task verification (2026-09-08):** SDK 8.0.424 selected by `global.json` (requested 8.0.400 with `rollForward: latestMajor`). Build 0 warnings / 0 errors; full suite 3,216 passed / 0 failed / 0 skipped (Data 771, Excel 24, Sim 583, Delegation 1,102, UnityAdapter 621, CLI 115); PlayModeSmokeHarness 24/24; ReplayGoldenSuiteTests 6/6. The v2 hash remains present. No new Editor run was performed; previous captures retain their original provenance.

**GitNexus preflight:** MCP reported this checkout three commits behind. `npx gitnexus analyze --index-only` detected an incomplete prior run, forced a rebuild, emitted Ladybug WAL assertions and reported FTS unavailable. The refresh was interrupted after approximately nine minutes without completion. DRG-197 carries the diagnostic and recovery remains open. No source-symbol edits were made, no index was deleted, and no graph freshness or successful impact result is claimed.
