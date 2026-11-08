# H4 Scenario Editor Phase 2 GUI — first-slice boundary

**Owner:** drg amtd. **Date:** 2026-10-01. **Maturity:** draft for repository review. **Delivery:** planning only; implementation remains gated. **Issues:** DRG-333, DRG-345, DRG-346, parent DRG-327. Stage remains Release.

The human instruction “proceed with these recommended next steps” authorizes this scope preparation and the ADR-017 topology decision. [ADR-017](../docs/architecture/adr-017-editor-topology-client-vs-scenario-lab.md) now records shared headless core, in-client v1, and an optional later Scenario Lab front-end. That decision supplies no implementation or owner visual signoff. [H4 review](agentic/h4-phase2-gui-ux-review-2026-10-01.md) pins source `1902dc12` and PR #686 input `9d653981`: 23 findings, three fixed, **20 unresolved**.

## Decisions carried from DRG-57

| Question | First-slice disposition |
| --- | --- |
| ME-W1 HTML mock | Design study for the existing client workflow. It is not an additional shipped front-end. |
| Export gate | H4 includes visible existing validation and proposed capability findings. It does not reopen M6 or change canonical packaging implicitly. |
| Undo/redo | General panel binding is later Phase 2 scope. False success, silent data loss and unsaved/conflict state must be corrected in the first workflow; reuse backend commands. |
| Keyboard/focus floor | Applies to the authoring surface: keyboard-reachable commands, visible focus, readable findings and recoverable errors need actual Editor evidence. No invented contrast measurement or acceptance claim. |
| WYSIWYG Platform Editor | Excluded. Platform authoring remains Excel-primary under ADR-011; no CatalogWriteGate write-path change. |

## In and out

**In:** one canonical scenario, one supported unit edit/place operation on the existing map, live actionable findings, explicit backend capability disclosure, draft Save/reload, existing Export artifact, and entry into Play through the approved seam. Supported, invalid, unsupported-role and conflict cases are required. Save is available for unfinished drafts.

**Later backlog:** Mission Board completion/P2.2, event graph/P2.3, general redo delivery, full parked UX remediation, density strips, replacement map hosts, standalone app, Platform WYSIWYG, global symbology modernization and remote content distribution. The first slice does not claim full P2.1/P2.2 parity.

## First vertical slice and surfaces

1. Open a scenario and select/place/edit one supported unit through `MapAuthoringSurface` and `ScenarioAuthoringSession.Bus`.
2. Refresh `LiveFindingsPresenter`: severity, message and entity location; finding selection is presentation-only. Show the actual catalog/backend identity. Never imply that silent fixture fallback resolved a requested catalog.
3. DRG-345 defines headless capability findings for offered support roles and the selected backend, projected consistently into CLI and GUI. Normal Export/Play must not advertise unsupported execution as implemented. Draft Save remains allowed. The proposed AME-6.11 amendment remains separately labeled in requirements.
4. Save, reload and verify edit-version persistence. A concurrency conflict or failure leaves a recoverable message and does not report success.
5. Export via `ScenarioExportCommand.Prepare` and existing publish/manifest contracts. The first slice uses the existing canonical document/manifest outputs; no ZIP/media packaging is added.
6. Enter Play via `EditModeController` and the approved runtime entry seam. Preserve separate existing policies: Export's severity floor versus Play's error/force-confirm behavior. Capability failures require a visible, non-bypassable unsupported-execution outcome in the proposed contract; force-confirm of an invalid document must not convert an unsupported role into supported behavior.

Allowed binding surfaces: `unity/ProjectAegis/Assets/Editor/ScenarioMapAuthoringWindow.cs`, existing `Assets/UI/ScenarioEditor` UXML/USS, and thin composition into `ScenarioEditorShellHost`. Headless authoring lives in `ProjectAegis.Data/Scenario/Authoring` and `ProjectAegis.Delegation.UnityAdapter/Authoring`. Before symbol edits, run path-qualified GitNexus upstream impact and corroborate the selected symbol locally. Reachability of the richer runtime shell is not yet demonstrated; its existing controls do not by themselves establish a delivered workflow.

## Dependencies and evidence

ADR-017 is decided in this review changeset, while its repository integration remains pending. DRG-335 reconciliation and this DRG-333 boundary require review/integration before DRG-345/346 implementation. G1/G3 and current entry/fixture evidence remain independent prerequisites. H4 does not bypass the active S122–S127 program or DRG-208 owner walk.

Use supported, invalid and unsupported-role fixtures, plus a conflicting Save. Prove command/projection and non-mutating failures headlessly first. Then run [Editor-host checklist](qa/scenario-editor-p2-1-editor-host-checklist.md) steps 2–6 with DRG-236/274: keyboard/focus, finding navigation, dirty/conflict state, Save, Export and Play. Evidence must name revision/local delta, build/plugin, scenario, catalog, backend, commands and expected/actual results. Unity `:8080` availability and actual captures must be recorded at execution time; proxy tests do not replace visual evidence or owner acceptance.

## Standing invariants and finish

Presentation follows ADR-010 §2–3, ADR-007 and ADR-001; authoring/data follow ADR-006, ADR-011 and ADR-017. Do not touch `DelegationBridge` hotpath, `SimulationSession`, existing `CatalogWriteGate` writes, `MapPlaceholderPanelHost`, `GlobeMapProductHost`, CMD-31/32/34 or S122 implementation. Preserve solution floor ≥1638, ReplayGolden 6/6, proxy smoke ≥20/20, Baltic v2 hash `17144800277401907079` and isolated v3 goldens. Stage stays Release; Launch is deferred.

**Architect verdict: PASS for the documentation boundary.** Runtime, assembly, Editor mutation and plugin API changes are N/A. Feature execution and owner acceptance are pending; review of this draft is not product acceptance.
