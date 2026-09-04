# 27 - Scenario Library, Campaigns & Package Loading

**Last Updated:** 2026-09-02
**Status:** Draft — remediation baseline (remedies B-13, addresses D-10 / AME-2.1 package loader residual & CMD-27 / CMD-27.12)
**FR reverse-ref:** [FR-09](01-Project-Overview.md) — Scenario/mission editor & [FR-02](01-Project-Overview.md) — Core loop / scenario selection
**Author basis:** Codebase review of `ProjectAegis.Data.Scenario` (`ScenarioPackageLoader`, `ScenarioPackage`, `ScenarioLibraryProjection`, `ScenarioLibraryLister`), `ProjectAegis.Data.Scenario.Campaign` (`CampaignDocument`, `CampaignScenarioMember`, `CampaignLibraryEntry`, `CampaignLibraryReasons`, `CampaignProgress`, `CampaignDocumentJsonLoader`, `CampaignDocumentJsonWriter`, `CampaignLibraryProjection`, `CampaignLibraryLister`), `ProjectAegis.Delegation.Projection` (`ScenarioLibraryApplyState`, `CampaignLibraryApplyState`), and Unity runtime host `ScenarioLibraryPanelHost.cs` (`CMD-27.12` / `CMD-27`); ADR-008, ADR-011, ADR-013, ADR-015, ADR-017; requirements 01, 02, 06, 11, 20.
**Related:** [01-Project-Overview.md](01-Project-Overview.md) · [02-Core-Gameplay-Loop.md](02-Core-Gameplay-Loop.md) · [06-Database-Intelligence.md](06-Database-Intelligence.md) · [11-Agentic-Mission-Editor.md](11-Agentic-Mission-Editor.md) · [20-Command-And-Control-UI.md](20-Command-And-Control-UI.md) · [21-Platform-Editor.md](21-Platform-Editor.md)
**Decision record:** [ADR-008 Mission-Editor Validation Engine (Accepted)](../../docs/architecture/adr-008-mission-editor-validation-engine.md) · [ADR-011 Scenario Package Format & Database Binding (Accepted)](../../docs/architecture/adr-011-scenario-package-format-and-db-binding.md) · [ADR-013 CMO Scenario Import Policy (Proposed)](../../docs/architecture/adr-013-cmo-scenario-import-policy.md) · [ADR-015 Agent-Authored Scenario Transparency (Proposed)](../../docs/architecture/adr-015-agent-authored-scenario-transparency.md) · [ADR-017 Editor Topology: Client vs Scenario Lab (Proposed)](../../docs/architecture/adr-017-editor-topology-client-vs-scenario-lab.md)

---

## Purpose

Define functional requirements, data contracts, and presentation projection pipelines for **Scenario & Campaign Discovery, Pre-load Validation Feasibility, and Package Ingestion**.

In Project Aegis, scenario discovery and campaign progression are first-class domain capabilities decoupled from any single user interface. Scenarios and campaigns are stored as canonical, version-controlled JSON artifacts (and zip-based packages). The scenario and campaign libraries provide deterministic listing, pre-load feasibility evaluation (such as catalog snapshot resolution and member scenario existence), zero-allocation/headless presentation projections, and resilient loading without failing or throwing on corrupt files.

---

## Architecture & Codebase Anchors

The implementation spans three layers:
1. **Data Layer (`ProjectAegis.Data.Scenario` & `ProjectAegis.Data.Scenario.Campaign`)**:
   - `ScenarioPackageLoader`: Loads canonical `*.scenario.json` and package manifests into runtime `ScenarioPackage` representations.
   - `ScenarioPackage`: Holds scenario identifier, bound policy ID, catalog snapshot binding (`DbSnapshotId`, `DbRef`, `TlBranch`), RNG seed, and edit version.
   - `ScenarioLibraryLister`: Enumerates scenario documents (`*.scenario.json`, `*.aegis-scenario`, and test fixtures) recursively; sorts deterministically by `ScenarioId` and `SourcePath`.
   - `ScenarioLibraryProjection`: Pure projector mapping scenario documents/paths to `ScenarioLibraryEntry` records with pre-load feasibility reasons (`FILE_UNREADABLE`, `SCHEMA_ERROR`, `DB_UNRESOLVED`, `VALIDATION_FAILED`).
   - `CampaignDocument`: Ordered scenario membership (`CampaignScenarioMember`) with sequence, completion state, and synopsis.
   - `CampaignLibraryLister`: Enumerates `*.campaign.json` files and evaluates membership feasibility against available scenario IDs.
   - `CampaignLibraryProjection`: Projects campaign documents/paths to `CampaignLibraryEntry` with feasibility reasons (`FILE_UNREADABLE`, `SCHEMA_ERROR`, `MEMBER_MISSING`).
   - `CampaignProgress`: Pure state transition functions (`MarkComplete`, `IsCampaignComplete`, `NextScenarioId`).
2. **Delegation / Headless Presentation Layer (`ProjectAegis.Delegation.Projection`)**:
   - `ScenarioLibraryApplyState`: Projects `ScenarioLibraryEntry` collections into UI-ready `ScenarioLibraryPresentation` and `ScenarioLibraryPreviewPresentation` records.
   - `CampaignLibraryApplyState`: Projects `CampaignLibraryEntry` collections into `CampaignLibraryPresentation` and `CampaignLibraryPreviewPresentation` records.
3. **Unity Presentation Layer (`ProjectAegis.Unity.Runtime`)**:
   - `ScenarioLibraryPanelHost`: UI Toolkit MonoBehaviour host binding scenario and campaign list views, zero-state previews, availability badges, and load dispatchers.

---

## Functional Requirements

### LIB-01: Scenario Library & Pre-Load Feasibility Discovery

- **LIB-01.1 (P0)** — **Deterministic Enumeration:** The system shall enumerate scenario artifacts matching `*.scenario.json` and `*.aegis-scenario` under configured directories (e.g., `data/scenarios/`), sorting entries deterministically by `ScenarioId` (ordinal) then `SourcePath`.
- **LIB-01.2 (P0)** — **Pre-Load Feasibility Evaluation:** The library projection shall evaluate scenario availability *before* full scenario load or simulation initialization:
  - Validates document readability and schema conformance.
  - Resolves database snapshot binding (`dbSnapshotId`, `dbRef`, `tlBranch`) against the active catalog reader.
  - Optionally runs fast pre-flight rule validation via `IScenarioValidationEngine`.
- **LIB-01.3 (P0)** — **Resilient Failure Isolation:** Corrupted, malformed, or unreadable scenario files shall not throw exceptions or abort library enumeration. Instead, they shall be projected as unavailable entries with explicit `UnavailableReason` codes (`FILE_UNREADABLE`, `SCHEMA_ERROR`, `DB_UNRESOLVED`, `VALIDATION_FAILED`).
- **LIB-01.4 (P0)** — **Library Metadata Projection:** Each `ScenarioLibraryEntry` shall expose `ScenarioId`, `Title`, `PolicyId`, `TlBranch`, `Seed`, `Location`, `Year`, `ProvenanceLabel` (e.g. `authored`, `agent-scaffolded`, `imported` per ADR-015), `Difficulty`, `Complexity`, `Available`, `UnavailableReason`, and `SourcePath`.

### LIB-02: Scenario Package Loading & Database Binding

- **LIB-02.1 (P0)** — **Canonical JSON & Package Ingestion:** `ScenarioPackageLoader` shall load scenario packages from canonical JSON (`*.scenario.json`) or package archives (`*.aegis-scenario`).
- **LIB-02.2 (P0)** — **Database Snapshot Resolution:** The loader shall resolve database bindings according to strict precedence rules:
  1. Explicit `metadata.dbSnapshotId` if specified.
  2. Named `metadata.dbRef` resolved against `ICatalogReader` (falling back to known public corpus / Baltic defaults).
  3. Technology level branch (`metadata.tlBranch` normalizing `TL-0`…`TL-5`) mapped to catalog snapshots.
  4. Default Baltic catalog snapshot fallback.
- **LIB-02.3 (P0)** — **Optimistic Concurrency & Determinism:** The loaded `ScenarioPackage` shall preserve `Seed` (ulong root seed) and `EditVersion` (monotonic integer edit counter) to guarantee reproducible simulation runs and detect concurrent edit conflicts.
- **LIB-02.4 (P1 / GAP Pointer)** — **AME-2.1 Zip Archive Ingestion (`*.aegis-scenario`):** Full decompression and mounting of standalone zip-packaged scenarios containing embedded database deltas or media bundles is specified under AME-2.1 and remains a Phase 2 residual / GAP for advanced distribution packages.

### LIB-03: Campaign Document Schema & Progression

- **LIB-03.1 (P0)** — **Campaign Artifact Separation:** Campaigns shall be distinct first-class artifacts (`*.campaign.json` stored in `data/campaigns/`), not simple directory hierarchies or naming conventions (satisfying CMD-27.12).
- **LIB-03.2 (P0)** — **Campaign Data Model:** A `CampaignDocument` shall contain:
  - `CampaignId`: Unique campaign identifier derived from filename/stem.
  - `Title`: Display title of the campaign.
  - `Synopsis`: Narrative overview and background description.
  - `Members`: Ordered list of `CampaignScenarioMember` records containing `ScenarioId`, `Sequence` (1-based index), `Completed` (boolean flag), and optional `DisplayTitle`.
- **LIB-03.3 (P0)** — **Pure Progression Logic:** Campaign completion and progression shall be managed through pure, immutable operations (`CampaignProgress`):
  - `MarkComplete(CampaignDocument, string scenarioId)`: Returns a new `CampaignDocument` with the target member flagged as completed.
  - `IsCampaignComplete(CampaignDocument)`: Returns true if and only if all members are marked completed.
  - `NextScenarioId(CampaignDocument)`: Returns the first incomplete scenario ID ordered by `Sequence` then `ScenarioId`.
- **LIB-03.4 (P0)** — **Serialization & Round-trip Conformance:** `CampaignDocumentJsonWriter` and `CampaignDocumentJsonLoader` shall serialize and deserialize campaign documents with deterministic formatting, supporting lossless save/load of player campaign progress.

### LIB-04: Campaign Library Projection & Host Binding

- **LIB-04.1 (P0)** — **Campaign Pre-Load Feasibility:** `CampaignLibraryProjection` and `CampaignLibraryLister` shall evaluate campaign availability:
  - Validates document readability and schema syntax.
  - Verifies that all referenced member scenario IDs exist in the indexed scenario directory. Missing member scenarios flag the campaign with `UnavailableReason = "MEMBER_MISSING"`.
- **LIB-04.2 (P0)** — **Campaign Entry Projection:** `CampaignLibraryEntry` shall project `CampaignId`, `Title`, `MemberCount`, `CompletedCount`, `NextScenarioId`, `Available`, `UnavailableReason`, and `SourcePath`.
- **LIB-04.3 (P0)** — **Headless Presentation Projection:** `ScenarioLibraryApplyState` and `CampaignLibraryApplyState` shall transform library entries into formatted presentation objects (`ScenarioLibraryPresentation`, `CampaignLibraryPresentation`) and preview objects (`ScenarioLibraryPreviewPresentation`, `CampaignLibraryPreviewPresentation`), including standard zero-state instructions and row status strings (`[available]` vs `[unavailable: <REASON>]`).
- **LIB-04.4 (P0)** — **UI Toolkit Presentation Binding:** `ScenarioLibraryPanelHost` binds scenario and campaign presentations into Unity UI Toolkit list views and preview labels without altering sim or domain state.

---

## Data Contracts & Schemas

### Campaign Document JSON (`*.campaign.json`)

```json
{
  "campaignId": "baltic-patrol-campaign",
  "title": "Baltic Patrol Campaign",
  "synopsis": "A multi-phase naval surveillance and escalation response campaign in the Baltic Sea.",
  "members": [
    {
      "scenarioId": "baltic-patrol",
      "sequence": 1,
      "completed": true,
      "displayTitle": "Phase 1: Initial Reconnaissance"
    },
    {
      "scenarioId": "strike-package",
      "sequence": 2,
      "completed": false,
      "displayTitle": "Phase 2: Strike Coordination"
    }
  ]
}
```

### Pre-Load Feasibility Reason Codes

| Reason Code | Domain | Description |
|-------------|--------|-------------|
| `FILE_UNREADABLE` | Scenario & Campaign | File does not exist, IO failure, or access permission denied. |
| `SCHEMA_ERROR` | Scenario & Campaign | Malformed JSON or required structural metadata missing. |
| `DB_UNRESOLVED` | Scenario | Specified `dbSnapshotId` or `dbRef` cannot be resolved in active catalog. |
| `VALIDATION_FAILED` | Scenario | Fast pre-flight scenario validation engine detected critical errors. |
| `MEMBER_MISSING` | Campaign | One or more member `scenarioId` entries cannot be located in the scenario catalog. |

---

## Verification & Test Traceability

| Requirement | Test Fixture / Contract |
|-------------|-------------------------|
| LIB-01.1, LIB-01.2, LIB-01.3, LIB-01.4 | `ProjectAegis.Data.Tests/Scenario/ScenarioLibraryProjectionTests.cs` |
| LIB-02.1, LIB-02.2, LIB-02.3 | `ProjectAegis.Data.Tests/Scenario/ScenarioPackageTests.cs` |
| LIB-03.1, LIB-03.2, LIB-03.3, LIB-03.4, LIB-04.1, LIB-04.2 | `ProjectAegis.Data.Tests/Scenario/CampaignLibraryTests.cs` |
| LIB-04.3 | `ProjectAegis.Delegation.Tests/Projection/CampaignLibraryApplyStateTests.cs` |
| LIB-04.4 | `ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs` (`ScenarioLibraryPanelHost`) |
