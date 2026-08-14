# Campaign library & progression — developer guide

A **campaign** is a first-class artifact that stitches individual scenarios into an ordered,
completion-tracked progression (CMD-27.12). Campaigns are a *separate artifact class* from flat
scenarios — they live under [`data/campaigns/`](../../data/campaigns/) as `*.campaign.json` files and
are browsed, previewed, and advanced through a small engine-agnostic subsystem in
[`ProjectAegis.Data/Scenario/Campaign/`](../../src/ProjectAegis.Data/Scenario/Campaign/) plus a
headless presentation apply-state in
[`ProjectAegis.Delegation/Projection/`](../../src/ProjectAegis.Delegation/Projection/CampaignLibraryApplyState.cs).

This page documents that subsystem as a whole: the on-disk document + its deterministic JSON
round-trip, the pure completion API, the feasibility-gated library projection, and the read-only
presentation apply-state a Unity host binds. It is the *campaign-layer* companion to the per-scenario
document ([scenario-document-authoring.md](scenario-document-authoring.md)) and the interactive
authoring host ([scenario-authoring-host.md](scenario-authoring-host.md)); the presentation
apply-state follows the same read-only projection contract as the rest of the C2 read model
([c2-projection-layer.md](c2-projection-layer.md)). Verified against source and pinned by the tests
at the end.

- **Document:** [`CampaignDocument`](../../src/ProjectAegis.Data/Scenario/Campaign/CampaignDocument.cs)
  — `(CampaignId, Title, Synopsis, Members)` + `CampaignScenarioMember(ScenarioId, Sequence, Completed, DisplayTitle?)`.
- **JSON round-trip:** [`CampaignDocumentJsonLoader`](../../src/ProjectAegis.Data/Scenario/Campaign/CampaignDocumentJsonLoader.cs) /
  [`CampaignDocumentJsonWriter`](../../src/ProjectAegis.Data/Scenario/Campaign/CampaignDocumentJsonWriter.cs).
- **Progress API:** [`CampaignProgress`](../../src/ProjectAegis.Data/Scenario/Campaign/CampaignProgress.cs)
  — pure `MarkComplete` / `IsCampaignComplete` / `NextScenarioId`.
- **Library projection:** [`CampaignLibraryLister`](../../src/ProjectAegis.Data/Scenario/Campaign/CampaignLibraryLister.cs) +
  [`CampaignLibraryProjection`](../../src/ProjectAegis.Data/Scenario/Campaign/CampaignLibraryProjection.cs) →
  [`CampaignLibraryEntry`](../../src/ProjectAegis.Data/Scenario/Campaign/CampaignLibraryEntry.cs).
- **Presentation:** [`CampaignLibraryApplyState`](../../src/ProjectAegis.Delegation/Projection/CampaignLibraryApplyState.cs)
  — headless row/preview view-models for the library host.
- **Path seam:** [`ScenarioDataPaths.TryResolveCampaignsDirectory`](../../src/ProjectAegis.Data/Scenario/ScenarioDataPaths.cs).

---

## Design invariants — never break these

Load-bearing and enforced by tests. Preserve them when touching any piece here.

| Invariant | Rule |
|-----------|------|
| **Sequence is metadata, never filename** | Ordering + in-fiction progression live in the `sequence` field, not the file name. `CampaignIdFromPath` strips only `.campaign.json` — it never decodes a sequence number. Do not reintroduce ordinal-prefixed filenames. |
| **Deterministic round-trip** | Both the loader and the writer sort members by `(Sequence, ScenarioId)` ordinal, and the writer emits a fixed property order (`campaignId, title, synopsis, members[…]`), LF newlines, and no BOM. Load → write must be byte-stable regardless of source ordering — keep both sorts in lock-step. |
| **Never throw on one bad file** | `CampaignLibraryLister` / `CampaignLibraryProjection` catch per-file IO/parse failures and emit an **unavailable** `CampaignLibraryEntry` (`FILE_UNREADABLE` / `SCHEMA_ERROR`) rather than aborting the whole browse list. A single corrupt campaign must not blank the library. |
| **Feasibility is fail-closed** | When a scenarios index is supplied, any member whose scenario id (or a blank id) is absent marks the campaign `MEMBER_MISSING` and unavailable — "prefer unavailable (blocking) over partial available". When **no** index is available, feasibility returns `null` (available) instead of inventing a missing-member error. |
| **Progress is pure & immutable** | `CampaignProgress` does no I/O and returns **new** `CampaignDocument` values (`record with`); `MarkComplete` returns the *same* instance when nothing changed and is a no-op for unknown / already-complete ids. Empty campaigns are never "complete". Keep it side-effect-free. |
| **Presentation is read-only & engine-free** | `CampaignLibraryApplyState` only formats existing entries into row/preview view-models — no mutation, no `UnityEngine` reference. The Unity host binds `Lines` / preview fields verbatim (same contract as [c2-projection-layer.md](c2-projection-layer.md)). |

---

## The campaign document

```csharp
public sealed record CampaignDocument(
    string CampaignId, string Title, string Synopsis,
    IReadOnlyList<CampaignScenarioMember> Members);

public sealed record CampaignScenarioMember(
    string ScenarioId, int Sequence, bool Completed, string? DisplayTitle = null); // Sequence is 1-based
```

On disk it is a `*.campaign.json` artifact under `data/campaigns/`:

```jsonc
// data/campaigns/baltic-patrol-campaign.campaign.json
{
  "campaignId": "baltic-patrol-campaign",
  "title": "Baltic Patrol Campaign",
  "synopsis": "Ordered Baltic theater progression: patrol, ferry redeploy, then strike package.",
  "members": [
    { "scenarioId": "baltic-patrol",  "sequence": 1, "completed": false, "displayTitle": "Baltic Patrol" },
    { "scenarioId": "ferry-redeploy", "sequence": 2, "completed": false, "displayTitle": "Ferry Redeploy" },
    { "scenarioId": "strike-package", "sequence": 3, "completed": false, "displayTitle": "Strike Package" }
  ]
}
```

`CampaignDocumentJsonLoader` is tolerant (case-insensitive properties, `//` comments, trailing
commas), trims strings, and sorts members by `(Sequence, ScenarioId)`; `LoadFromFile` throws
`InvalidDataException` on an unparseable document while `TryLoadFromFile` swallows IO/parse failures
into a `bool`. `CampaignDocumentJsonWriter.Serialize` is the deterministic inverse — stable property
order, `displayTitle` omitted when blank, `\r\n` normalized to `\n`, UTF-8 without BOM.

---

## Progress API

`CampaignProgress` is the pure completion surface — no I/O, immutable results:

| Method | Behaviour |
|--------|-----------|
| `MarkComplete(doc, scenarioId)` | Sets the matching member `Completed = true` (ordinal-ignore-case). Returns a new document, or the **same instance** when nothing changed; blank / unknown ids are a no-op. |
| `IsCampaignComplete(doc)` | `true` only when every member is completed. An empty (or null-members) campaign is **not** complete. |
| `NextScenarioId(doc)` | The first incomplete member by `(Sequence, ScenarioId)` ordinal, or `null` when all complete / empty. |

`NextScenarioId` is what drives "resume campaign" — it is deterministic and independent of member
list order because it compares sequence first, then id.

---

## Library projection

`CampaignLibraryLister.ListFromDirectory(campaignsDir, scenariosDir?)` builds the browse list:

1. Enumerate `*.campaign.json` under `campaignsDir` (recursive; IO-tolerant).
2. Resolve a scenarios directory — the explicit arg, a sibling `scenarios/` next to `campaigns/`, or
   `ScenarioDataPaths.TryResolveScenariosDirectory()` — and index its scenario ids
   (`IndexScenarioIds`, using the same path→id rules as `ScenarioLibraryProjection` plus bare stems).
3. Project each file through `CampaignLibraryProjection.ProjectFromPath` into a `CampaignLibraryEntry`.
4. Sort by `(CampaignId, SourcePath)` ordinal.

Each row is a `CampaignLibraryEntry`:

```csharp
public sealed record CampaignLibraryEntry(
    string CampaignId, string Title, int MemberCount, int CompletedCount,
    string? NextScenarioId, bool Available, string? UnavailableReason, string SourcePath);
```

`Available` / `UnavailableReason` carry the pre-load feasibility verdict, drawn from a stable
reason-code catalog:

| `CampaignLibraryReasons` code | Meaning |
|-------------------------------|---------|
| `FILE_UNREADABLE` | The file is missing or an IO/access error occurred. |
| `SCHEMA_ERROR` | The JSON failed to parse into a `CampaignDocument`. |
| `MEMBER_MISSING` | A member scenario id (or a blank id) is absent from the scenarios index. |

`EvaluateFeasibility` is fail-closed: a supplied index that lacks any member → `MEMBER_MISSING`; a
`null` index → available (membership can't be verified, so nothing is invented). `ScenarioIdPresent`
matches both bare (`baltic-patrol`) and `.scenario`-suffixed (`baltic-patrol.scenario`) id forms so
members written in either convention resolve.

---

## Presentation apply-state

`CampaignLibraryApplyState` (in `ProjectAegis.Delegation.Projection`, the same read-model tier as the
C2 projections) turns entries into host-ready view-models — the Unity library panel binds them
without re-formatting:

- `Apply(entries)` → `CampaignLibraryPresentation(Rows, Lines, Count)`; `Empty` for a null/empty list.
- `FormatRowLine(entry)` → `"{Title}  [{completed}/{members}]  [available]"` or
  `"…  [unavailable: {REASON}]"` — the row states its own availability.
- `ApplyPreview(selected)` → `CampaignLibraryPreviewPresentation` (title / id / availability /
  progress / next-scenario / source-path lines), or the zero-state
  `"Select a campaign"` when nothing is selected.

Because the apply-state only reads `CampaignLibraryEntry` values, it is safe to call off any tick and
never mutates campaign state.

> **No CLI verb (yet).** Unlike scenario authoring, campaigns have **no** `campaign_*` verb in the
> Mission Editor CLI today ([mission-editor-cli.md](mission-editor-cli.md)) — the subsystem is
> consumed by the in-process library host (and tests) via `CampaignLibraryLister` +
> `CampaignLibraryApplyState`. Add a verb by wrapping the lister the same way the scenario library
> verbs wrap `ScenarioLibraryLister`.

---

## Producer / consumer map

| Role | Type | What it does |
|------|------|--------------|
| **Author** | `*.campaign.json` under `data/campaigns/` | The on-disk campaign artifact (sequence in metadata). |
| **Load / write** | `CampaignDocumentJsonLoader` / `CampaignDocumentJsonWriter` | Tolerant read ↔ deterministic write of `CampaignDocument`. |
| **Progress** | `CampaignProgress` | Pure completion + resume (`MarkComplete` / `NextScenarioId` / `IsCampaignComplete`). |
| **List** | `CampaignLibraryLister` → `CampaignLibraryProjection` | Enumerate + feasibility-gate into sorted `CampaignLibraryEntry` rows. |
| **Present** | `CampaignLibraryApplyState` | Read-only row/preview view-models for the Unity host. |
| **Path** | `ScenarioDataPaths.TryResolveCampaignsDirectory` | Resolves `data/campaigns` (walk-up + sibling fallback). |

---

## Runbooks

### Add a campaign

Drop a `<id>.campaign.json` into `data/campaigns/` with a unique `campaignId`, a title/synopsis, and
an ordered `members` array (1-based `sequence`, each `scenarioId` pointing at an existing scenario
document). The library picks it up automatically; if a `scenarioId` doesn't resolve, the row shows
`unavailable: MEMBER_MISSING` until the scenario is added. Prefer round-tripping through
`CampaignDocumentJsonWriter` so the file stays in canonical (sorted, LF, no-BOM) form.

### Mark progress / resume

Use `CampaignProgress.MarkComplete(doc, scenarioId)` after a scenario finishes and persist with
`CampaignDocumentJsonWriter.WriteToFile`. Use `NextScenarioId(doc)` for the "continue campaign"
target and `IsCampaignComplete(doc)` for the end-of-campaign transition. Never mutate members in
place — always thread the returned document.

### Add a feasibility reason

Add a constant to `CampaignLibraryReasons`, emit it from `CampaignLibraryProjection.EvaluateFeasibility`
(keeping the fail-closed "prefer unavailable" rule), and render it in `FormatRowLine` /
`ApplyPreview`. Add coverage to `CampaignLibraryTests`.

---

## Pinned by tests

| Test | Guards |
|------|--------|
| `CampaignLibraryTests` | Document round-trip, `CampaignProgress` completion/resume, and `CampaignLibraryLister` / `CampaignLibraryProjection` feasibility + deterministic sort. |
| `CampaignLibraryApplyStateTests` | Row/preview formatting, availability labelling, and the `"Select a campaign"` zero-state. |
