# Play entry and authored coordination fixture — delivery preflight

**Date:** 2026-10-01. **Owner:** drg amtd. **Source base:** `1902dc1299678ea17f75014406fd9c616e5576bd`. **Status:** read-only delivery preflight; implementation and visual acceptance pending.

Fresh Linear reads: [DRG-243](https://linear.app/drgamtd-workspace/issue/DRG-243) and [DRG-239](https://linear.app/drgamtd-workspace/issue/DRG-239) are Backlog. DRG-243 is blocked by [DRG-197](https://linear.app/drgamtd-workspace/issue/DRG-197), which remains In Progress. Both feed owner-only DRG-208; enabling its walk does not complete it. User authorization permits advancing this delivery path; existing status does not establish delivery.

GitNexus query used the absolute repository path and was corroborated against this worktree's source because DRG-323/324 record graph limitations. No symbols were edited; editing impact analysis is N/A. Unity-MCP `http://localhost:8080` was unreachable on this review (HTTP `000`, connection failed). No Editor inspection or mutation occurred.

## Existing seams and missing delivery

| Area | Existing source seam | Remaining work |
| --- | --- | --- |
| Library | `ScenarioLibraryPanelHost.cs`, `ScenarioLibraryPanel.uxml`, Data `ScenarioLibraryLister`/`Projection`, Delegation `ScenarioLibraryApplyState` | Browse/preview exists; selection does not load a session. No load/commit control or policy/ORBAT handoff was found on this path. |
| Feasibility | `ScenarioLibraryProjection.EvaluateFeasibility` accepts catalog and validation dependencies | Host `ReloadFromDisk()` passes neither by default; even its optional catalog path supplies no validation engine. Metadata-only availability cannot stand for validated load eligibility. |
| Package | `src/ProjectAegis.Data/Scenario/ScenarioPackageLoader.cs` and `ScenarioPackage.cs` | Loader derives policy/catalog/seed/edit metadata; the package does not carry the complete authored ORBAT. Compose from the canonical document; do not infer JSON loading means ZIP/media mounting. |
| Campaign | Data campaign lister/projection and separate campaign preview | Bind selected scenario membership into briefing using campaign document members. Separate list/progress UI does not establish this association. Location/year are reserved placeholders and mode hints are absent from the scenario row contract. |
| Runtime | `DelegationBridgeHost.Awake`, `BeginExecution`, `RunTick`; `SimplePlayModeSimHost` | Awake composes a serialized default policy. Smoke startup defaults to automatic execution; Update advances smoke time and seeded log without a Planning phase check. Loading must not accidentally use this behavior. |
| Coordination | `CoordinationBridge`, `ICoordinationFacts`, `CoverageFact`, `CommandReviewView` → `SubmitGroupDecision` | Runtime smoke snapshot does not implement authored facts; smoke seeder registers units, not a task group. Existing disposable `CoordinationScenario` is headless evidence, not the requested live fixture. |

Full runtime paths above are under `unity/ProjectAegis/Assets/Scripts/Runtime/`; library UXML is under `unity/ProjectAegis/Assets/UI/ScenarioLibrary/`. Coordination contracts live under `src/ProjectAegis.Delegation.UnityAdapter/CommandReview/`.

## Bounded implementation order

1. Resolve DRG-197's path-specific freshness and graph-gap evidence before claiming its delivery gate closed. Run upstream impact before changing existing symbols; inspect local callers where graph results are incomplete.
2. Establish a headless validated load/commit contract for DRG-243: selected canonical document, resolved catalog and policy, ORBAT, seed, and campaign membership. Failure preserves the existing session. No silent fixture fallback may convert a failed explicit binding into successful load.
3. Bind the existing library to that contract. Successful commit initializes Planning; keep automatic smoke execution and synthetic log advancement outside this product entry path. Use the existing Begin Execution façade once after deliberate confirmation.
4. Author DRG-239 data and a live snapshot/composition provider: one registered task group with linked members, package roles, explicit group/package assignment, mission intent, and source-labelled coverage geometry. Reuse command and projection façades; do not invent range rings or roles in the view.
5. Prove integration headlessly, then use the approved Unity scene-builder/MCP workflow when Editor tooling is available. Save scene/prefab changes through Editor APIs, inspect Console, and capture Game View. Link evidence to DRG-175/185/192/235/244 without marking them accepted.

## Concrete acceptance cases

- Supported load: preview exposes genuine feasibility, theater/mode information and campaign membership; commit loads the selected policy and ORBAT into Planning. Time, authoritative state and logs remain unchanged until Begin Execution. Repeated Begin actions do not start duplicate sessions.
- Invalid load: missing/corrupt document, unresolved explicit dbRef, and blocking validation each show an actionable reason and leave the prior session unchanged with no simulation tick. Revalidate at commit if the file changed after preview.
- Positive fixture: the live provider yields the registered group, authored package association and visible coverage polygon. Hold and Withdraw are reachable through `CoordinationCommandBridge`; verify the resulting permitted member scope and order effects.
- Negative fixture: missing roles/assignment/coverage stay explicitly unknown. Invalid geometry is suppressed: at least three finite normalized points in `[0,1]` and nonempty `SourceRef` are required. Lost/detached contributors produce gap presentation rather than fabricated coverage.

Record exact revision, SDK/build/plugin, scenario/catalog identities, commands, expected/actual outcomes, Console output, screenshots and owner criteria separately. No tests were executed by this preflight. Architecture checklist: **PASS for this documentation review**, ADR-010 §2–3/ADR-007/ADR-001; executable, assembly, plugin and Editor-mutation gates N/A. Preserve `DelegationBridge` hotpath, catalog writes, replay goldens and Baltic v2 hash `17144800277401907079`. Owner Play Mode signoff remains pending.
