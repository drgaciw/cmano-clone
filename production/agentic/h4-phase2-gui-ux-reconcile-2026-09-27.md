# H4 Phase 2 GUI — parked UX findings reconcile (DRG-335)

| Field | Value |
| --- | --- |
| Linear | [DRG-335](https://linear.app/drgamtd-workspace/issue/DRG-335/h4-reconcile-parked-phase-2-gui-ux-findings-drg-57-pack-against) |
| Parent | DRG-327 (H4 Scenario Editor Phase 2 GUI). Scope-boundary story is DRG-333 — this file does not write that boundary. |
| Main SHA | `1902dc1299678ea17f75014406fd9c616e5576bd` (`1902dc12`, 2026-09-27) |
| Date | 2026-09-27 |
| Kind | Read-only reconciliation. No code, no ADR, no baseline, no test-floor edit. |
| Mock | Assessed against the committed artifact `docs/superpowers/reviews/scenario-editor-uiux-preview.html`. It is present. |

July line anchors have drifted. Every status below was re-located on this SHA. Citations are `path:line` on that tree.

Presentation wall used when a finding touches a view: **ADR-010 §2–3** (UI is a client), **ADR-007** (map / C2 read-only vs `DecisionLog`), **ADR-001** (snapshot in / order out). Git ADR-018 is sensor side-picture / datalink and is not the presentation boundary.

Two product surfaces exist and they are not the same UI:

- **Panel (shipped Editor window):** `unity/ProjectAegis/Assets/Editor/ScenarioMapAuthoringWindow.cs` plus `unity/ProjectAegis/Assets/UI/ScenarioEditor/ScenarioMapAuthoringPanel.uxml`. Menu `Project Aegis/Scenario Map Authoring` at `ScenarioMapAuthoringWindow.cs:115`.
- **Shell (runtime chrome, not composed):** `unity/ProjectAegis/Assets/Scripts/Runtime/ScenarioEditorShellHost.cs` plus `ScenarioEditorShell.uxml`. The script GUID `b1187aea29e54070a9c496bda01049a6` appears only in its `.meta` under `Assets/` (no scene or prefab reference found). No C# caller invokes `SetFindingRows`, `SetTopBar`, or `UndoRequested` outside the host itself.

A fix that lives only on the shell is **not** reachable from the Editor window until a composer binds the host. Those rows are `partially-fixed`, not `fixed-on-main`.

## Reconciliation table

Severity in the July pack: Blocking unless marked Backlog or advisory. Surface is mock / panel / both.

| ID | Title | Original surface / severity | Status now | Evidence on `1902dc12` | Notes |
| --- | --- | --- | --- | --- | --- |
| UX-01 | Export gate invisible / unreachable from any UI (DRG-56) | both / Blocking | **fixed-on-main** | Panel label `ScenarioMapAuthoringPanel.uxml:19-20`; paint `ScenarioMapAuthoringWindow.cs:577-617`; gate state `LiveFindingsPresenter.cs:16-17`, `ScenarioExportGateState.cs:6-11`, `CanExport` at `LiveFindingsPresenter.cs:129`. Landed in `32153a06` (#351). Contract test source (not re-run): `ScenarioMapAuthoringPanelUiTests.cs:63-64`. | Gate decision is a labeled status (`READY TO EXPORT` / `EXPORT BLOCKED`) with tooltip. There is still no Export **button** in the window. Invoking export remains `ScenarioExportCommand.Prepare` (`ScenarioExportCommand.cs:23-41`) and CLI. DRG-56’s defect was visibility of the gate, and that is on the panel. |
| UX-02 | Findings sorted by code, not ADR-008 severity tuple (DRG-55) | both / Blocking | **fixed-on-main** | Sort is `ValidationReport.cs:21-29` (Severity DESC, Code ASC, MissionId, UnitId, TargetId, Message). Presenter preserves it and does not re-sort: `LiveFindingsPresenter.cs:11-14`, `EvaluateGate` `LiveFindingsPresenter.cs:88-90` and `111-114`. Same commit `32153a06` (#351). Test source (not re-run): `LiveFindingsPresenterTests.cs:146-169`. | Panel still **displays** codes only (UX-03). Order of those codes follows the tuple. |
| UX-03 | Shipped findings list shows codes only — no severity / message / location | panel / Blocking | **partially-fixed** | Panel still emits codes: section title `ScenarioMapAuthoringPanel.uxml:41`; `BuildFindingsLines` `ScenarioMapAuthoringWindow.cs:661-674` reads `LastCodes` only. Shell row is `[SeverityTag] Code — Message`: `ScenarioEditorShellHost.cs:461-466`. Full record exists on `ValidationFinding.cs:3-10` (`Message`, `MissionId`, `UnitId`, `TargetId`, `Data`) and `LiveFindingsPresenter.LastFindings` `LiveFindingsPresenter.cs:58-59`, unused by the window. | Shell text is richer and unwired. Panel location fields are still absent. |
| UX-04 | Findings are dead ends — no click-to-entity, no `Data` (e.g. `excess_nm`), no fix path | both / Blocking | **partially-fixed** | Panel labels are not pickable: `FillScroll` `ScenarioMapAuthoringWindow.cs:695-697` (`pickingMode = Ignore`). Shell click + Enter/Space call `ActivateFinding`: `ScenarioEditorShellHost.cs:471-479`. Jump sets selection and may switch mode: `ScenarioEditorShellProjection.cs:201-208`. No UI reads `ValidationFinding.Data`. No fix / apply action on either surface. | Jump is presentation selection only (ADR-007 / ADR-010). It does not edit the document. `Data` and a fix path are still absent, and the shell jump has no composer. |
| UX-05 | No Undo/Redo UI; Rebuild / Refresh read as discard | panel / Blocking | **partially-fixed** | Disk undo sidecar still exists: `ScenarioUndoStackStore.cs:50-54` (`Count`, `ResolveStackPath` → `.undo-stack.json`). Mutate captures undo: `ScenarioEditCommandBus.cs:179-195`. CLI undo only: `ScenarioUndoCommand.cs:25-28`, `Program.cs:59-60`. No redo method on the store. Shell buttons: `ScenarioEditorShell.uxml:14-15`; host raises events `ScenarioEditorShellHost.cs:91-92`, `251-263`. Nothing outside the host subscribes. Panel still labels `Rebuild` and `Refresh Findings`: `ScenarioMapAuthoringPanel.uxml:14-16`. Shell chrome landed in `b24b7c17` (#338). | Buttons without a bus subscription and without a redo stack do not close the defect on the window authors actually open. |
| UX-06 | No keyboard model (no KeyDown, no shortcuts, no `:focus`, focusable then StopPropagation) | both / Blocking | **partially-fixed** | Focus token and measured contrast notes: `AegisTokens.uss:42-56` (`dc0db16f`, #355). Panel `:focus` rules: `ScenarioMapAuthoringPanel.uss:359-374`. Shell tab and row `:focus`: `ScenarioEditorShell.uss:133-136`, `274-277`. Shell tab keys: `ScenarioEditorShellHost.cs:328-336`. Finding-row keys: `ScenarioEditorShellHost.cs:473-479`. Panel `WireButton` still sets `focusable = true` then `StopPropagation` on `ClickEvent`: `ScenarioMapAuthoringWindow.cs:346-376`. No `KeyDownEvent` in that window. Menu items exist (`ScenarioMapAuthoringWindow.cs:115-127`) with no shortcut chords. Tooltips on the panel are the export-gate label only (`ScenarioMapAuthoringWindow.cs:598-615`). Shell Events tab has one tooltip: `ScenarioEditorShell.uxml:29`. | Focus painting improved. The panel keyboard model (shortcuts, tab order, control tooltips, KeyDown) is still missing. `StopPropagation` is on click, not on keydown. |
| UX-07 | Implementation vocabulary dominates; suggestion was “Copy as CLI command” (advisory; partially retracted) | mock / advisory | **still-open** | Mock banner names `MapAuthoringSurface`, `MissionBoardPresenter`, `LiveFindingsPresenter`, `SelectionInspector`, `ScenarioDocumentEditor`: `scenario-editor-uiux-preview.html:896-907`. Findings chip text `Validation Engine · debounce 300ms`: `scenario-editor-uiux-preview.html:1088`. No “Copy as CLI” control in that file. Panel help line is also implementation vocabulary: `ScenarioMapAuthoringPanel.uxml:26`. | Advisory. Product shell UXML does not repeat the mock’s type names. The committed mock still leads with them. |
| UX-08 | Center canvas dead space; timeline in a ~90px strip; nested scrolls | mock / Blocking | **partially-fixed** | No timeline element remains in the mock (Events tab disabled, `scenario-editor-uiux-preview.html:913`, footer `1094-1095`). Map canvas fills the center cell: `.map-canvas` `inset: 0` at `scenario-editor-uiux-preview.html:589-594`. Nested scroll regions remain: drawer `.list` `339`, mission `.board-table-wrap` `451-454`, findings list `816`. Unity panel is a form inside `scenario-map-scroll-root` plus an inner platform `ScrollView`: `ScenarioMapAuthoringPanel.uxml:24`, `56-58`. Shell center slots are placeholders: `ScenarioEditorShell.uxml:40-46`. | The 90px timeline strip is gone from the mock. Nested scrolling and the absence of a real authoring canvas on the Unity window remain. |
| UX-09 | Dirty state under-weighted; editVersion shown raw | both | **partially-fixed** | Panel still `Edit version: {n}` and `Dirty: yes/no`: `ScenarioMapAuthoringWindow.cs:558-565`, labels `ScenarioMapAuthoringPanel.uxml:32-33`. Shell dirty copy is `• unsaved` / `saved` plus a bold class: `ScenarioEditorShellProjection.cs:123`, `ScenarioEditorShellHost.cs:365-368`, `ScenarioEditorShell.uss:50-58`. Shell version is still the raw integer `v{n}`: `ScenarioEditorShellHost.cs:371-373`. Mock matches the shell pattern and still prints `editVersion: 14`: `scenario-editor-uiux-preview.html:919-920`. | Dirty weight improved on shell and mock only. The Editor window and every editVersion label are still raw. |
| UX-10 | No conflict / reconcile UI for `ScenarioEditConflictException` | both | **still-open** | Guard and exception: `ScenarioEditVersionGuard.cs:4-20`. Bus returns `ErrorCode = CONFLICT` and does not write: `ScenarioEditCommandBus.cs:182-183`, `212-221`. Window surfaces that string on place/RP failure status only: `ScenarioMapAuthoringWindow.cs:1013`, `1109`. No reload / keep-mine / discard dialog on panel, shell, or mock. | A status sentence is not a reconcile UI. |
| UX-11 | Contrast unverified; 10/11/13px flat scale; opacity-only disabled | both | **partially-fixed** | Focus-ring contrast ratios are written in `AegisTokens.uss:48-54`, with an explicit note that a colour-blind pass is still required (`AegisTokens.uss:53-54`). Panel type ramp is still 13/11/10px (`ScenarioMapAuthoringPanel.uss:43`, `52`, `95`) and disabled is opacity 0.45 (`ScenarioMapAuthoringPanel.uss:227-228`). Shell disabled opacity: `ScenarioEditorShell.uss:78-79`. Shell scale hooks exist: `ScenarioEditorShell.uss:325-331` and `ScenarioEditorShellHost.cs:510-513`. Mock still uses 10/11/13px (`scenario-editor-uiux-preview.html:102`, `111`, `119`) and `.btn:disabled { opacity: 0.45 }` at `243-245`. | Token notes are not a measured pass over the 10px ramp. Opacity remains the only disabled cue on all three surfaces. |
| UX-12 | Severity colour-only in counters / row tint | mock | **fixed-on-main** | Mock rows include the words `ERROR` / `WARN`: `scenario-editor-uiux-preview.html:1484`. Count chips include “N errors” / “N warnings”: `1086-1087` and `1498-1501`. Play chip text includes the count: `1505-1507`. Shell rows also prefix `[ERROR]`/`[WARN]`/`[INFO]`: `ScenarioEditorShellHost.cs:461` and `ScenarioEditorFindingsProjection.cs:284-290`. Panel export gate pairs colour with the words READY / BLOCKED: `ScenarioMapAuthoringPanel.uss:317-318`, `ScenarioMapAuthoringWindow.cs:607-614`. | Colour still tints rows. It is no longer the only carrier on the mock. Panel findings rows have neither a severity word nor a tint (they are codes only — UX-03). |
| UX-13 | Hover, selected, and focus share one fill (`--selected-row-bg`) | both | **partially-fixed** | Panel hover and selected still share `--selected-row-bg`: `ScenarioMapAuthoringPanel.uss:152-160`. Panel focus is now `--focus-ring`, not that fill: `ScenarioMapAuthoringPanel.uss:363-374`. Shell finding hover still uses the selected fill: `ScenarioEditorShell.uss:270-272`; focus uses the focus ring: `274-277`. Mock list hover is a lighter rgba and selected is `--aegis-selected-row-bg` (`scenario-editor-uiux-preview.html:353`, `360-361`). Mock `:focus-visible` outline uses `--aegis-selected-ring` (`238-239`), which is the selection **colour** token. | Focus is separated from the row fill on the Unity USS. Hover and selected still share a fill on the panel. Mock focus still borrows the selection ring colour. |
| UX-14 | Filter chips and count chips look identical | mock | **partially-fixed** | Filters are `.btn.filter` (UI font, 10px, padding 4px 8px): `scenario-editor-uiux-preview.html:265-274`. Counts are `.chip` (mono, uppercase, semantic err/warn borders): `781-796`, markup `1081-1087`. Unity shell uses a sentence summary plus separate filter buttons, not count chips: `ScenarioEditorShell.uxml:58-62`, `ScenarioEditorShell.uss:244-254`. Panel has neither chip. | Classes diverged. Both mock controls are still small uppercase bordered pills, so the glance-test can still fail. |
| UX-15 | Raw ticks as user-facing time | mock | **partially-fixed** | Mock top bar is the literal `T+00:00 · 2026-07-04T06:00Z`: `scenario-editor-uiux-preview.html:921`. No tick integer is rendered as time anywhere in that file. There is no timeline control left to format. Unity panel and shell have no scenario-time field. | The raw-tick defect is gone because the timeline widget is gone. Nothing formats sim time from data. |
| UX-16 | “Timeline” is a table; no density strip despite ADR-016 `peak_tick_density` | mock | **still-open** | Mock has no timeline and no density strip. Mission board is a mission table (`scenario-editor-uiux-preview.html:1051-1064`), not an event timeline. Engine emits a soft warning only: `ValidationRules.cs:498-507` (`EVENT_GRAPH_PEAK_TICK_DENSITY_HIGH`, ADR-016). `EventGraphPresenter.cs` has no density view. ADR-016 formula: `docs/architecture/adr-016-event-graph-complexity-caps.md:72`. | Removing the table did not add the strip. The warning is a findings code, which the panel shows as a bare code (UX-03). |
| UX-17 | Inspector clipped / untitled; ROE inherit vs override ambiguous | mock | **partially-fixed** | Mock inspector is titled “Selection inspector”: `scenario-editor-uiux-preview.html:1069-1070`. Inherit copy distinguishes override vs side default: `1106-1112`, rendered at `1431-1432`. Workspace `overflow: hidden` (`281`) and `.panel` has no own scroll, so a tall inspector can still clip. Shell title is “Selection” and the body is `No selection` or `Selected: {id}`: `ScenarioEditorShell.uxml:49-51`, `ScenarioEditorShellProjection.cs:137-139`. Headless summary has no ROE: `SelectionInspectorModel.cs:34`. Panel has no inspector. | Mock title and inherit chain exist. Unity chrome does not show inherit vs override. Clip risk on the mock is inferred from CSS, not a rendered viewport. |
| UX-18 | Contextual actions enabled out of context; `ShouldEnableCatalogAndFormChrome` returns true unconditionally | both | **still-open** | Policy: `ScenarioMapAuthoringHostPolicy.cs:25-31`. Window duplicate: `ScenarioMapAuthoringWindow.cs:91`, applied at `512-520`. Comment at `515-517` says the body stays enabled so buttons can show `NoSessionMessage`. Write actions are gated: `ScenarioMapAuthoringHostPolicy.cs:33-37`, `ScenarioMapAuthoringWindow.cs:524-527`. Mock enables tools with no session concept. | Behavior matches the July report. Comments now call it intentional. DRG-333 can accept that policy or keep the finding. This reconcile does not close it. |
| UX-19 | Panel USS bypasses tokens (raw rgba) | panel | **still-open** | Raw rgba still at `ScenarioMapAuthoringPanel.uss:122`, `:149`, `:232`. Shell has a further raw rgba at `ScenarioEditorShell.uss:267` (not in the July anchor, same class of gap). Tokens define surfaces in `AegisTokens.uss:9-12` but not these three fills. | July anchors still match. |
| UX-20 | No seed / determinism surface; no sample-run action | both / Backlog | **still-open** | `ScenarioMetadataDto.Seed` exists: `ScenarioMetadataDto.cs:19-20`. Sample is CLI-only: `Program.cs:23-24`, `ScenarioSimulateSampleCommand.cs:23-36` (export gate, then sample). Shell has a Sample button that only raises `SampleRequested`: `ScenarioEditorShell.uxml:17`, `ScenarioEditorShellHost.cs:97-98`, `270-276`. No subscriber. Panel has no seed label and no Sample button. | Library preview `preview-seed` (`ScenarioLibraryPanelHost.cs:300`) is a different panel, not this editor. |
| UX-21 | No dbRef / catalog binding indicator; `ResolveCatalog` silent fixture fallback | both / Backlog | **still-open** | Silent fixture fallback: `ScenarioValidateCommand.cs:33-59` (SQLite miss or unresolved `dbRef` / `tlBranch` returns `InMemoryCatalogReader.BalticPatrolFixture()`). Mock shows a static `dbRef: baltic-catalog@tl-42`: `scenario-editor-uiux-preview.html:922`. Panel and shell do not bind `ScenarioMetadataDto.DbRef` (`ScenarioMetadataDto.cs:13`). | Mock chip is a literal, not a binding indicator, and it does not say when the fixture fallback fired. |
| UX-22 | No TL branch indicator (`metadata.tlBranch`, `TL_BRANCH_*`) | both / Backlog | **still-open** | Field: `ScenarioMetadataDto.cs:25-26`. Neither panel nor shell UXML names `tlBranch`. Library preview formats `TL: {TlBranch}` in `ScenarioLibraryApplyState.cs:75` and binds `preview-tl` at `ScenarioLibraryPanelHost.cs:299` — again the library, not the editor. | Editor chrome has no TL indicator. |
| UX-23 | No provenance display (user / ai / import; ADR-015) | both / Backlog | **still-open** | Tags exist on the publish manifest: `ScenarioManifest.cs:13-18` (`Source` comment lists `user`, `ai`, `import`). ADR-015 is proposed labeling, not a shipped editor field: `docs/architecture/adr-015-agent-authored-scenario-transparency.md:21-51`. Panel and shell have no provenance line. Library `preview-provenance` is `ScenarioLibraryPanelHost.cs:302`. | Authoring UI does not show who authored the open scenario. |

### Counts

| Status | IDs |
| --- | --- |
| fixed-on-main | UX-01, UX-02, UX-12 (3) |
| partially-fixed | UX-03, UX-04, UX-05, UX-06, UX-08, UX-09, UX-11, UX-13, UX-14, UX-15, UX-17 (11) |
| still-open | UX-07, UX-10, UX-16, UX-18, UX-19, UX-20, UX-21, UX-22, UX-23 (9) |
| superseded | none |

20 parked rows remain not fully fixed (11 partially-fixed + 9 still-open; UX-03…UX-23 excluding UX-12, which is fixed-on-main). UX-01 and UX-02 match the DRG-56 / DRG-55 claim.

## Verified vs unverified

**Verified (read on this SHA):** every row’s C# / UXML / USS / HTML citation above, the absence of `SetFindingRows` / `UndoRequested` subscribers outside `ScenarioEditorShellHost`, the absence of a redo API on `ScenarioUndoStackStore`, ADR-008 sort order, the two gate policies in question (b), and the shell script GUID appearing only in its `.meta` under `Assets/`.

**Unverified / inferred:**

- No browser render of the mock and no Unity Editor play of either chrome. “Dead space”, “clipped”, and “chips look the same at a glance” are CSS readings, not screenshots.
- Contrast ratios in `AegisTokens.uss:48-54` were not re-measured.
- `dotnet test` was not run. Test files are cited as contract source only.
- Scene/prefab search was a text search of `Assets/`. It did not open the Unity Editor. A broken `unity/ProjectAegis/data/scenarios` symlink was not walked.

## Re-sized H4 waves

Fixed rows (UX-01, UX-02, UX-12) drop out. Everything else stays. Shell-only work does not count as shipped until a composer binds `ScenarioEditorShellHost` to `ScenarioAuthoringSession` without piercing ADR-010 / ADR-007 / ADR-001.

### H4-W1 — Findings the author can read and follow

Was July W1 minus UX-01 and UX-02.

| ID | Why it stays |
| --- | --- |
| UX-03 | Panel list is still codes. |
| UX-04 | No `Data`, no fix path; shell jump is unwired. |

**Surfaces:** `ScenarioMapAuthoringWindow.BuildFindingsLines` / `FillScroll`, `ScenarioMapAuthoringPanel.uxml` findings list, `LiveFindingsPresenter.LastFindings`, `ScenarioEditorFindingRow`, `ScenarioEditorShellHost.RebuildFindingsList`.

**Hard-no:** none required. Jump stays a presentation selection (ADR-007 read-only, ADR-010 client). Do not implement “show on map” by editing `MapPlaceholderPanelHost` or `GlobeMapProductHost`.

### H4-W2 — Session honesty (undo, dirty, conflict, context)

July W2 minus the keyboard slice (moved to W3 so the bus work stays separate from USS).

| ID | Why it stays |
| --- | --- |
| UX-05 | Undo button unwired; no redo store; panel still says Rebuild / Refresh. |
| UX-09 | Editor window dirty + editVersion still raw. |
| UX-10 | CONFLICT is a status string. |
| UX-18 | Catalog/form chrome still always enabled. Decision: accept the comment at `ScenarioMapAuthoringWindow.cs:515-517` or gate the body. |

**Surfaces:** `ScenarioEditorShell.uxml` Undo/Redo, `ScenarioEditorShellHost` events, `ScenarioUndoStackStore`, `ScenarioUndoCommand`, `ScenarioEditCommandBus.Mutate` conflict return, `ScenarioMapAuthoringWindow.RefreshSessionChrome`, `ScenarioMapAuthoringHostPolicy.ShouldEnableCatalogAndFormChrome`.

**Hard-no:** flag only. Undo/redo and conflict UI stay on the authoring bus and CLI. Do not route them through `CatalogWriteGate`, `DelegationBridge` Tick, or `SimulationSession`. A redo stack is new authoring state, not a replay re-bless. Do not touch `tests/regression/` or Baltic v2 hash `17144800277401907079`.

### H4-W3 — Keyboard, type, colour, canvas, inspector

July W3 plus UX-06, minus UX-12.

| ID | Why it stays |
| --- | --- |
| UX-06 | Panel has focus paint and still has no key model. |
| UX-07 | Advisory mock vocabulary; no Copy-as-CLI. |
| UX-08 | Nested scrolls; Unity center is a form, not a canvas. |
| UX-11 | 10/11/13px and opacity-only disabled. |
| UX-13 | Panel hover and selected still share one fill. |
| UX-14 | Mock pills still similar. |
| UX-15 | No data-backed time field. |
| UX-16 | No density strip; ADR-016 is a warning code only. |
| UX-17 | Unity inspector has no ROE inherit vs override. |
| UX-19 | Panel rgba at the July lines. |

**Surfaces:** `ScenarioMapAuthoringPanel.uss`, `ScenarioEditorShell.uss`, `AegisTokens.uss`, `ScenarioMapAuthoringWindow.WireButton`, `ScenarioEditorShellHost.OnTabsKeyDown`, `scenario-editor-uiux-preview.html`, `SelectionInspectorModel`, shell inspector labels. Density strip needs a per-tick density series: `EVENT_GRAPH_PEAK_TICK_DENSITY_HIGH` (`ValidationRules.cs:501-507`) only fires above the threshold and carries `peakDensity`/`threshold`, not per-tick buckets. W3 should expose the per-tick counts already computed by the validation pass as a read-only projection (or a new headless DTO), use the finding only to highlight the over-threshold tick, and not retune the ADR-016 formula in this wave.

**Hard-no:** flag only on UX-08. A map picture in the shell slot must not be built by editing `MapPlaceholderPanelHost` or `GlobeMapProductHost`, and must not write `DecisionLog` (ADR-007). CMD-31 / CMD-32 / CMD-34, S122, and Launch stay out.

### H4-Backlog — Binding, seed, provenance

Unchanged membership: UX-20, UX-21, UX-22, UX-23.

**Surfaces (read/display only):** `ScenarioMetadataDto` (`Seed`, `DbRef`, `TlBranch`), `ScenarioValidateCommand.ResolveCatalog`, `ScenarioSimulateSampleCommand`, `ManifestBuilder.ProvenanceTag`, shell Sample button. Library preview fields are a precedent for labels, not the editor surface.

**Hard-no:** flag only. A Sample button must call the existing `scenario_simulate_sample` command path (ADR-010 command façade). It must not call `SimulationSession` or `DelegationBridge.Tick`. A dbRef indicator must not open SQLite from the view (ADR-006) and must not change `CatalogWriteGate`. Changing the silent fixture fallback in `ResolveCatalog` is a CLI behavior change; do not hide it inside a GUI story.

## Carried questions

**(a) Is the ME-W1 mock the Unity panel, a separate front-end, or a design study?**

Answered for the artifact. `docs/superpowers/reviews/scenario-editor-uiux-preview.html:896-907` calls itself an HTML stand-in and a review mock. It is not `ScenarioMapAuthoringPanel.uxml` and it is not loaded by `ScenarioEditorShellHost`. ADR-017 (status **Proposed**, target decision 2026-10-01) is the topology question: in-client Unity is the proposed v1 surface, and a standalone Scenario Lab would be a later front-end on the same core (`docs/architecture/adr-017-editor-topology-client-vs-scenario-lab.md:19-21`, `67-73`). That ADR is not accepted here and this reconcile does not number or amend it. Residual: whether a second front-end is ever built stays with ADR-017’s owner.

**(b) Are the ADR-008 export gate and `EditModeController.TryEnterPlay` one gate or two policies?**

Answered by code: **two policies that match only while the export floor is Error.**

- Export gate: `ValidationReport.CanExport` against `ExportBlockSeverityFloor` (`ValidationReport.cs:18-19`), surfaced by `LiveFindingsPresenter.EvaluateGate` (`LiveFindingsPresenter.cs:129`) and the panel label. Save is never gated (`ScenarioExportGateState.cs:56-58`).
- Play entry: `EditModeController.TryEnterPlay` blocks only when `HasErrorSeverity` (`ErrorCount > 0`) and honors `forceConfirmInvalid` (`EditModeController.cs:51-62`). It does not call `CanExport`.
- Shell Play/Sample uses the same error-count rule: `ScenarioEditorShellProjection.cs:98`, `129-130`.

If `ExportBlockSeverityFloor` moves to Warning, export can block while `TryEnterPlay` still returns true. The force-confirm flag can enter Play while the export label stays blocked. DRG-333 should say whether Play must follow `CanExport` or keep the error-only rule.

## Hand-off for DRG-333

Do not treat this file as the scope boundary. Suggested cuts:

1. Ship bar for “fixed” is the **Editor window**, not the unwired shell. Shell rows in the table are capacity, not a closed defect.
2. W1 is the only blocking pair still about findings content (UX-03, UX-04). W2 is session honesty. W3 is visual and keyboard. Backlog stays backlog.
3. UX-18 is a product decision (accept always-on form chrome, or gate it). The code already documents the always-on choice.
4. Question (b) should be one sentence in the boundary: Play follows `CanExport`, or Play stays error-only with force-confirm.
5. Question (a): the HTML mock is a design study. Do not schedule it as a third runtime.
6. Hard-no flags above are flags only. This reconcile did not touch `DelegationBridge`, `CatalogWriteGate`, `SimulationSession`, map hosts, CMD-31/32/34, S122/Launch, ReplayGolden, or the Baltic v2 hash.
7. No new ADR. Presentation work in later stories cites ADR-010 §2–3, ADR-007, and ADR-001.
