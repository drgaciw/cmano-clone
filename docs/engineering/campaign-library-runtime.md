# Campaign library runtime — ordered scenario progression, browse list & preview

> **Scope.** The pure, headless **campaign** artifact class (CMD-27.12): the first-class
> `*.campaign.json` document (ordered scenario membership + completion state), the pure
> completion API, the deterministic loader/writer, and the browse-list / preview projection —
> all in [`src/ProjectAegis.Data/Scenario/Campaign/`](../../src/ProjectAegis.Data/Scenario/Campaign/)
> (`CampaignDocument`, `CampaignProgress`, `CampaignDocumentJsonLoader`,
> `CampaignDocumentJsonWriter`, `CampaignLibraryLister`, `CampaignLibraryProjection`,
> `CampaignLibraryEntry`) plus the presentation apply-state in
> [`src/ProjectAegis.Delegation/Projection/`](../../src/ProjectAegis.Delegation/Projection/)
> (`CampaignLibraryApplyState` → `CampaignLibraryPresentation` / `CampaignLibraryPreviewPresentation`).
> A campaign is a **separate artifact class** from a flat scenario: it is an *ordered list of
> existing scenarios* with per-member completion, not a folder of scenarios and not filename-encoded
> sequence. This is the direct sibling of the flat [scenario browse](scenario-library-runtime.md) —
> both are surfaced by the same Unity host.
>
> Boundary rationale: [ADR-010 (headless-first, command-driven UI)](../architecture/adr-010-headless-first-command-driven-ui.md).
> Everything here is **presentation-only** and pure — no sim state, no `DecisionLog`, no RNG, no
> wall-clock — so it runs under plain `dotnet test` and cannot perturb the replay hash
> (`17144800277401907079`).

---

## Where it lives

| Type | Assembly / folder | Kind | Role |
|------|-------------------|------|------|
| `CampaignDocument` / `CampaignScenarioMember` | `Data/Scenario/Campaign/` | `sealed record` | The artifact: id/title/synopsis + ordered members with completion. |
| `CampaignProgress` | `Data/Scenario/Campaign/` | `static` | Pure completion API (`MarkComplete`, `IsCampaignComplete`, `NextScenarioId`). |
| `CampaignDocumentJsonLoader` | `Data/Scenario/Campaign/` | `static` | Tolerant `*.campaign.json` reader → `CampaignDocument`. |
| `CampaignDocumentJsonWriter` | `Data/Scenario/Campaign/` | `static` | Deterministic canonical writer (stable order, LF, no BOM). |
| `CampaignLibraryEntry` | `Data/Scenario/Campaign/` | `sealed record` | One browse row: membership summary + `Available` + `UnavailableReason`. |
| `CampaignLibraryReasons` | `Data/Scenario/Campaign/` | `static` consts | The stable pre-load feasibility reason codes. |
| `CampaignLibraryLister` | `Data/Scenario/Campaign/` | `static` | Enumerate a directory → sorted `CampaignLibraryEntry` list. |
| `CampaignLibraryProjection` | `Data/Scenario/Campaign/` | `static` | Project one document/path → `CampaignLibraryEntry` (+ feasibility). |
| `CampaignLibraryApplyState` | `Delegation/Projection/` | `static` | Fold entries → bindable list + preview presentation. |
| `CampaignLibraryDisplayRow` / `CampaignLibraryPresentation` | `Delegation/Projection/` | `sealed record` | List-side bind bundle. |
| `CampaignLibraryPreviewPresentation` | `Delegation/Projection/` | `sealed record` | Preview-pane fields (or zero-state). |
| `ScenarioLibraryPanelHost` | `unity/…/Runtime/` | `MonoBehaviour` | Shared UI-Toolkit consumer (binds scenario **and** campaign lists). |

The data layer (`ProjectAegis.Data`) does the IO + feasibility; the presentation layer
(`ProjectAegis.Delegation`) does the string formatting. Neither references `UnityEngine`.

---

## The document model — `CampaignDocument`

```csharp
public sealed record CampaignDocument(
    string CampaignId, string Title, string Synopsis,
    IReadOnlyList<CampaignScenarioMember> Members);

public sealed record CampaignScenarioMember(
    string ScenarioId, int Sequence, bool Completed, string? DisplayTitle = null);
```

- **`Sequence` is 1-based** metadata that carries the in-fiction progression order — it is **not**
  encoded in the filename. The loader and writer both sort members by `(Sequence, ScenarioId)`
  (`Ordinal`), so the on-disk / in-memory order is always canonical regardless of authoring order.
- **`ScenarioId`** references a scenario **by id**, matched against the scenario library later (see
  feasibility below); a campaign owns *references*, not scenario copies.
- **`DisplayTitle`** is an optional per-leg label; blank/whitespace is normalized to `null`.

### On-disk shape — `*.campaign.json`

Campaigns live under `data/campaigns/` (the sibling of `data/scenarios/`). Example
([`baltic-patrol-campaign.campaign.json`](../../data/campaigns/baltic-patrol-campaign.campaign.json)):

```json
{
  "campaignId": "baltic-patrol-campaign",
  "title": "Baltic Patrol Campaign",
  "synopsis": "Ordered Baltic theater progression: patrol posture, ferry redeploy, then strike package.",
  "members": [
    { "scenarioId": "baltic-patrol",  "sequence": 1, "completed": false, "displayTitle": "Baltic Patrol" },
    { "scenarioId": "ferry-redeploy", "sequence": 2, "completed": false, "displayTitle": "Ferry Redeploy" },
    { "scenarioId": "strike-package", "sequence": 3, "completed": false, "displayTitle": "Strike Package" }
  ]
}
```

---

## Loader & writer

- **`CampaignDocumentJsonLoader`** — camelCase, case-insensitive, comment-skipping,
  trailing-comma-tolerant `System.Text.Json`. `LoadFromFile` throws `InvalidDataException` on a null
  document; `LoadFromJson` returns `null`; `TryLoadFromFile` swallows `Json` / `InvalidData` / `IO` /
  `Unauthorized` into `false`. Members are trimmed and sorted `(Sequence, ScenarioId Ordinal)` at load.
- **`CampaignDocumentJsonWriter`** — deterministic canonical form: fixed property order
  `campaignId, title, synopsis, members[{ scenarioId, sequence, completed, displayTitle? }]`,
  members re-sorted `(Sequence, ScenarioId)`, `displayTitle` omitted when blank, `LF` line endings,
  no BOM. This is what makes round-trips diff-stable (`JsonWriter_round_trips_with_stable_property_order`).

---

## The completion API — `CampaignProgress`

Pure, no-IO, returns **new** documents (the input is never mutated):

| Method | Behaviour |
|--------|-----------|
| `MarkComplete(doc, scenarioId)` | Sets the matching member's `Completed = true` (ordinal-ignore-case match). Unknown id / already-complete / blank id → returns the **same instance** (`ReferenceEquals`), so callers can cheaply detect no-ops. |
| `IsCampaignComplete(doc)` | `true` only when every member is complete; an **empty campaign is never complete**. |
| `NextScenarioId(doc)` | First incomplete member by `(Sequence, ScenarioId Ordinal)`, or `null` when all complete / empty. This is the "resume here" pointer. |

---

## The row model — `CampaignLibraryEntry`

```csharp
public sealed record CampaignLibraryEntry(
    string CampaignId, string Title,
    int MemberCount, int CompletedCount, string? NextScenarioId,
    bool Available, string? UnavailableReason, string SourcePath);
```

- **`CampaignId`** — from `metadata.campaignId`, else derived from the file name by stripping
  `.campaign.json` (or the first extension) — **never** encodes sequence (`CampaignIdFromPath`).
- **`Title`** falls back to `CampaignId` when the document title is blank.
- **`MemberCount` / `CompletedCount`** — the progress summary (`n` members, `k` completed).
- **`NextScenarioId`** — the resume pointer from `CampaignProgress.NextScenarioId`.
- **`Available` / `UnavailableReason`** — the pre-load feasibility verdict (below);
  a row is **available** exactly when `UnavailableReason is null`.

### Feasibility reason codes — `CampaignLibraryReasons`

| Code | Raised when |
|------|-------------|
| `FILE_UNREADABLE` | Path missing / IO / access failure loading the document. |
| `SCHEMA_ERROR` | JSON parse / `InvalidDataException` / null document. |
| `MEMBER_MISSING` | At least one member `scenarioId` is blank, or is absent from the scenarios index. |

---

## Projection & feasibility — `CampaignLibraryProjection`

Three entry points, all pure and **fail-soft**:

| Method | Use |
|--------|-----|
| `Project(document, sourcePath, availableScenarioIds?)` | Project an already-loaded `CampaignDocument` (tests / in-memory). |
| `ProjectFromPath(path, availableScenarioIds?)` | Load from disk then project; catches `Json` / `InvalidData` / `IO` / `Unauthorized` as the matching unavailable code. |
| `ProjectUnavailable(campaignId, sourcePath, reason)` | Build an unavailable row directly (counts → `0`, `NextScenarioId → null`). |

`EvaluateFeasibility(document, availableScenarioIds)` returns the reason (or `null`):

1. **Null document →** `SCHEMA_ERROR`.
2. **No scenarios index (`availableScenarioIds is null`) →** `null`. Membership **cannot be verified
   without a scenarios index, so it is not invented** — the campaign is treated as available rather
   than falsely blocked.
3. **Membership check.** With an index, any member whose `ScenarioId` is blank **or** not present
   (see id-matching below) → `MEMBER_MISSING`. The policy is **prefer unavailable (blocking) over
   partial available**: one missing leg blocks the whole campaign.

### Scenario-id matching — `ScenarioIdPresent`

Membership tolerates the two id forms the scenario library can emit
(see [scenario-library-runtime.md](scenario-library-runtime.md)): a member id matches when the index
contains it directly, its `"<id>.scenario"` form, or (for a `*.scenario` member) its bare stem. The
index itself is built by `CampaignLibraryLister.IndexScenarioIds`, which records every scenario's
derived id **and** its bare basename, using the same document patterns as `ScenarioLibraryLister`
(`*.scenario.json`, `*.aegis-scenario`, plus `examples/` / `validation/` / `golden_*` fixtures;
`*.policy.json` / `*.schema.json` / `*.campaign.json` excluded).

---

## Enumeration — `CampaignLibraryLister.ListFromDirectory`

```csharp
IReadOnlyList<CampaignLibraryEntry> ListFromDirectory(
    string campaignsDir,
    string? scenariosDir = null);
```

- **Pattern.** Recursively (`AllDirectories`) collects `*.campaign.json` only.
- **Scenarios index resolution.** When `scenariosDir` is omitted the lister first tries the
  **sibling** `../scenarios` of the campaigns dir, then falls back to
  `ScenarioDataPaths.TryResolveScenariosDirectory()`. If no scenarios dir resolves, the index is
  `null` and membership is **not** checked (rows stay available — see feasibility rule 2).
- **IO-tolerant.** `IOException` / `UnauthorizedAccessException` while enumerating is swallowed; the
  lister continues with whatever it has.
- **Deterministic order.** Entries are sorted by `CampaignId` then `SourcePath`, both
  `StringComparer.Ordinal` — never filesystem/enumeration order. Same directory → same list.
- **Never throws on one bad file.** Each path goes through `ProjectFromPath`, which converts any
  per-file failure into an *unavailable* row rather than aborting the whole listing.
- An empty/whitespace/nonexistent directory returns `Array.Empty<…>()`.

The directories are resolved by
[`ScenarioDataPaths.TryResolveCampaignsDirectory()`](../../src/ProjectAegis.Data/Scenario/ScenarioDataPaths.cs)
(a walk-up search for `data/campaigns`, then the `data/scenarios` sibling), so hosts and tests do not
hard-code paths.

---

## Presentation — `CampaignLibraryApplyState`

The headless apply path a UI host binds verbatim (no re-formatting in the view):

- **`Apply(entries)` → `CampaignLibraryPresentation`** (`Rows`, `Lines`, `Count`; `Empty` for a
  null/empty list). Each row's `FormatRowLine` folds the progress counter into the label:
  - available → `"{Title}  [{done}/{total}]  [available]"`
  - unavailable → `"{Title}  [{done}/{total}]  [unavailable: {reason}]"` (reason stated **on the row**).
- **`ApplyPreview(selected?)` → `CampaignLibraryPreviewPresentation`** — the selected campaign's
  fields (`Title`, `ID:`, availability, `Progress: {done}/{total}`, `Next: {id}`, source path). A
  `null` selection returns the **zero-state** instruction `"Select a campaign"` (CMD-27.12), exposed
  as `CampaignLibraryApplyState.ZeroStateInstruction`.

```text
Alpha Campaign  [1/3]  [available]
Beta Campaign   [0/2]  [unavailable: MEMBER_MISSING]
```

---

## Consumer — `ScenarioLibraryPanelHost` (Unity)

[`ScenarioLibraryPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/ScenarioLibraryPanelHost.cs)
is a UI-Toolkit `MonoBehaviour` (`#if UNITY_5_3_OR_NEWER`) that binds **both** this campaign library
and the sibling flat scenario library in one panel. On enable / reload it resolves
`data/scenarios` and `data/campaigns` via `ScenarioDataPaths`, lists both (passing the scenarios dir
into `CampaignLibraryLister.ListFromDirectory(campaignsDir, dir)` so membership is checked against the
same tree), and folds each via its `ApplyState`.

The panel has **one shared preview pane** driven by a `PreviewMode` state
(`ScenarioZero` / `ScenarioSelected` / `CampaignZero` / `CampaignSelected`): selecting a campaign row
(`SelectCampaignIndex`) clears any scenario selection and vice-versa (`SelectIndex`), so the two lists
are mutually exclusive in the preview. The host holds only presentation state (last entries /
presentation / preview per list) and never touches the sim — selection is a pure `ApplyPreview`
recompute.

---

## Determinism & invariants

- **Presentation-only.** Inputs are a directory of documents (+ optional scenarios index); outputs
  are records/strings. Nothing reads or writes the `DecisionLog` or sim state → it cannot move the
  replay hash `17144800277401907079`. *(Boundary cites ADR-010 — not ADR-018.)*
- **Deterministic.** Ordinal sort by `(CampaignId, SourcePath)` for the list and `(Sequence,
  ScenarioId)` for members; canonical writer output; no RNG, no wall-clock; identical inputs →
  identical bytes/list.
- **Pure completion.** `CampaignProgress` returns new documents and never mutates its input; unknown
  ids are reference-equal no-ops.
- **Fail-soft, never-throw.** One unreadable/invalid file becomes an unavailable row, not an
  exception; unreadable subtrees are skipped.
- **Membership is not invented.** Without a scenarios index, `MEMBER_MISSING` is never raised — an
  unverifiable campaign is shown as available rather than falsely blocked.
- **No engine dependency.** The data + presentation types have no `UnityEngine` reference, so they
  are CI-safe and headless-testable; only `ScenarioLibraryPanelHost` is Unity-gated.

---

## Extend it

| You want to… | Do this |
|--------------|---------|
| Add a campaign metadata field | Add it to `CampaignDocument` + the loader DTO + `CampaignDocumentJsonWriter` (keep the property order stable), then surface it in `CampaignLibraryEntry` / preview. |
| Add a new feasibility reason | Add a const to `CampaignLibraryReasons` and raise it from `EvaluateFeasibility`; the row line + preview render it automatically. |
| Change a row / preview string | Edit `CampaignLibraryApplyState` only — formatting lives there, not in the host or the data layer. |
| Persist completion | Load → `CampaignProgress.MarkComplete` → `CampaignDocumentJsonWriter.WriteToFile`; the deterministic writer keeps the diff clean. |
| Loosen/tighten id matching | Edit `CampaignLibraryProjection.ScenarioIdPresent` (and keep it aligned with `ScenarioLibraryProjection.ScenarioIdFromPath`). |

---

## See also

| Doc | For |
|-----|-----|
| [scenario-library-runtime.md](scenario-library-runtime.md) | The flat scenario browse this campaign list sits beside; the id-derivation rules its membership matches. |
| [scenario-document-authoring.md](scenario-document-authoring.md) | Authoring the `*.scenario.json` documents a campaign references. |
| [mission-editor-cli.md](mission-editor-cli.md) | The headless CLI that validates/simulates the same scenario documents. |
| [determinism-and-replay.md](determinism-and-replay.md) | Why the read path must stay pure. |

## Tests

| Test | Assembly (framework) | Pins |
|------|----------------------|------|
| `CampaignLibraryTests` | `ProjectAegis.Data.Tests/Scenario/` (xUnit) | Fixture load preserves `(sequence)` order; canonical writer stable property + member order + round-trip; `MarkComplete` / `IsCampaignComplete` / `NextScenarioId` semantics; unknown-id `MarkComplete` no-op (`ReferenceEquals`); lister finds the Baltic fixture available; missing member → `MEMBER_MISSING`; all-present → available with correct counts; `SCHEMA_ERROR` on bad JSON; `CampaignIdFromPath` strips `.campaign.json` without encoding sequence. |
| `CampaignLibraryApplyStateTests` | `ProjectAegis.Delegation.Tests/Projection/` (NUnit) | `[n/m]  [available]` / `[unavailable: MEMBER_MISSING]` row lines; null preview → `"Select a campaign"` zero-state; selected preview fills id/availability/progress/next/source fields; null entries → empty presentation. |
