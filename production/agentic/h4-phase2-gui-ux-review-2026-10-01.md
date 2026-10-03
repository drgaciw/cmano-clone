# H4 Phase 2 GUI — review and bounded first slice

**Date:** 2026-10-01. **Review owner:** drg amtd. **Status:** source-backed review; proposed delivery scope, visual acceptance pending.

**Inputs:** DRG-335 brief at `origin/cursor/drg-335-h4-ux-reconcile-0d36:production/agentic/h4-phase2-gui-ux-reconcile-2026-09-27.md`, head `9d65398130a8172ae048d0557e7c92c496e84993` (PR #686); source base `1902dc1299678ea17f75014406fd9c616e5576bd`; live [DRG-333](https://linear.app/drgamtd-workspace/issue/DRG-333), [DRG-345](https://linear.app/drgamtd-workspace/issue/DRG-345), and [DRG-346](https://linear.app/drgamtd-workspace/issue/DRG-346).

## Review verdict

**PASS for the reconciliation, with one count correction.** The inventory contains 23 findings: 3 fixed (UX-01/02/12), 11 partially fixed, and 9 open. Therefore **20 remain unresolved**. The parked range UX-03–UX-23 contains 21 IDs, but includes fixed UX-12; it cannot be described as 21 unresolved findings.

Load-bearing claims were rechecked against this source base. GitNexus query used the absolute repository path; local reads corroborated results because DRG-323/324 identify index and overload limitations. This review does not claim a fresh inspection of every historical visual finding.

| Finding | Corroborated evidence | First-slice consequence |
| --- | --- | --- |
| UX-03/04 | `ScenarioMapAuthoringWindow.BuildFindingsLines` reads `LastCodes`; richer shell findings have no external composer calls. | Bind severity, message, entity location, and useful finding data; selection jump remains presentation-only. |
| UX-05/09/10 | Shell undo/redo events have no outside subscriber; window Save exists; conflict returns a status. | Show unsaved state and failed/conflicting edits honestly. Do not label unwired redo as usable. |
| Export versus Play | `ValidationReport.CanExport` uses the configured severity floor; `EditModeController.TryEnterPlay` uses errors and permits force-confirm. | Specify both policies; avoid claiming they are already one gate. |
| Reachability | Shell GUID appears only in its `.meta` under Assets; no scene/prefab match or external `SetFindingRows`/`SetTopBar` call was found. | Existing shell capacity is not a delivered workflow. |
| UX-21 | `ScenarioValidateCommand.ResolveCatalog` can silently return the Baltic fixture. | Identify the actual catalog used in evidence; a visible indicator must not imply resolution succeeded. |

## Proposed boundary for DRG-333

Use the shared headless core and in-client v1 direction authorized on 2026-10-01; ADR-017 acceptance is recorded separately by the coordinator. Optional Scenario Lab remains a later front-end. The HTML mock is a design study, not another shipped application.

H4 covers scenario authoring. **Platform editing remains Excel-primary under accepted ADR-011**; no WYSIWYG platform editor or catalog write-path change enters this slice. Presentation follows ADR-010 §2–3, ADR-007, and ADR-001. Event-graph editing, full Mission Board completion, density strips, redo-stack delivery, alternate front-ends, and general globe/symbology modernization remain later backlog.

## Proposed first slice — DRG-345 and DRG-346

1. Open a canonical scenario, select/place or edit one supported unit through `MapAuthoringSurface` and `ScenarioAuthoringSession.Bus`, then refresh live findings.
2. Display actionable validation and backend capability findings. `MissionAddSupportCommand` accepts Tanker/AEW/EW vocabulary; that does not establish execution support. DRG-345 must evaluate role plus selected backend and disclose unsupported execution before Export/Play. Draft Save remains available.
3. Save and reload the draft, confirming the mutation and edit version persisted. No export gate disables Save. A conflict leaves an explicit recoverable status and never claims success.
4. Export using existing `ScenarioExportCommand.Prepare` and publish/manifest contracts. Choose the current canonical document/manifest representation; do not introduce ZIP/media packaging implicitly. Confirm the exact output artifact contract before implementation.
5. Enter Play through the existing approved entry seam. Proposed capability failures block normal execution; any invalid-document force-confirm behavior needs explicit policy and visible disclosure. It cannot silently advertise an unsupported role as implemented.

Reuse `ScenarioMapAuthoringWindow`, its UXML/USS, and existing headless presenters. Composition into the in-client shell needs an explicit thin binding plan; no replacement host is authorized by this brief.

**Concrete surfaces:** `unity/ProjectAegis/Assets/Editor/ScenarioMapAuthoringWindow.cs`; `unity/ProjectAegis/Assets/UI/ScenarioEditor/ScenarioMapAuthoringPanel.uxml` / `.uss`; `unity/ProjectAegis/Assets/Scripts/Runtime/ScenarioEditorShellHost.cs`; `src/ProjectAegis.Delegation.UnityAdapter/Authoring/{MapAuthoringSurface,LiveFindingsPresenter,EditModeController}.cs`; `src/ProjectAegis.Data/Scenario/Authoring/{ScenarioAuthoringSession,ScenarioEditCommandBus,ScenarioExportCommand}.cs`. Capability rules belong in headless validation; chrome consumes their findings rather than duplicating policy.

## Evidence and finish conditions

Use one supported fixture, one invalid fixture, and one unsupported-role fixture. Record revision, build/plugin identity, scenario/catalog/backend identity, commands, expected/actual results, and evidence links. Prove command/projection contracts headlessly, then capture the actual authoring surface through checklist steps 2–6 in `production/qa/scenario-editor-p2-1-editor-host-checklist.md`, coordinated with DRG-236/274. Keyboard reachability, readable findings, dirty/conflict state, Save, Export, and Play need visual evidence; source presence and proxy tests do not supply it.

No code or tests were changed or executed by this review. Unity Editor screenshots, contrast measurement, and owner Play Mode signoff remain pending. No `DelegationBridge` hotpath, `SimulationSession`, `CatalogWriteGate` writes, map runtime hosts, CMD-31/32/34, replay goldens, test floor, or Release stage changes are included. Preserve Baltic v2 hash `17144800277401907079`.

**Architect finish:** PASS for this documentation review under `production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md`; executable, assembly, Editor mutation, plugin refresh, and changed-code testing gates are N/A. Product delivery and owner acceptance remain separate.
