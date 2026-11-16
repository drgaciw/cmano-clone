# AI evidence & artifact contracts — `ProjectAegis.Ai.Contracts` (schema v2)

> **ADR:** these DTOs cross the **process boundary to Unity / CLI clients**, so they sit under the
> [`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3
> presentation-client seam and the [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md)
> adapter boundary. The host boundary, egress rule, and artifact envelope are to be ratified as
> **ADR-030** before Wave 1 merges (see the [AI feature plan](../superpowers/plans/2026-11-12-dotnet-11-ai-feature-integration.md)).
> Do **not** cite Git ADR-018 here (that is sensor side-picture / datalink).

`ProjectAegis.Ai.Contracts` is the **Wave 0, track A (W0-A)** deliverable of the
[.NET 11 AI feature integration plan](../superpowers/plans/2026-11-12-dotnet-11-ai-feature-integration.md) —
the **frozen, zero-dependency shapes** that every later AI slice reads and writes: the evidence a
completed run exports, the envelope that identifies a generated AI artifact, and the transport record
a Unity or CLI client receives. It is a pure contract assembly. It holds **no logic beyond two tiny
helpers**, references **no packages**, and depends on **none** of the simulation assemblies, so Unity
and headless hosts can read the same types without pulling in `Microsoft.Extensions.AI`, Delegation,
Sim, or Data.

Everything lives in the single namespace `ProjectAegis.Ai.Contracts` under
[`src/ProjectAegis.Ai.Contracts/`](../../src/ProjectAegis.Ai.Contracts/).

---

## Project shape & build

[`ProjectAegis.Ai.Contracts.csproj`](../../src/ProjectAegis.Ai.Contracts/ProjectAegis.Ai.Contracts.csproj):

- **Targets** `netstandard2.1;net11.0`. The `netstandard2.1` target is what the Unity plug-in folder
  consumes; `net11.0` is for the host and tests. Keep both — `netstandard2.1` is why the records need
  the [`IsExternalInit`](../../src/ProjectAegis.Ai.Contracts/Polyfills/IsExternalInit.cs) polyfill
  (compiled only under `#if NETSTANDARD2_1`, so it never collides with the runtime type on `net11.0`).
- **`LangVersion` `12.0`** — the repo's C# 12 pin ([`Directory.Build.props`](../../Directory.Build.props)
  says "Do not raise"). Records and enums only; no C# 15 unions.
- **Zero `PackageReference` items, by design** (DRG-730). `System.Text.Json` consumers live downstream
  in `Ai.Host` and the tests, never here. A Wave 0 architecture gate (W0-D) exists to keep AI packages
  and DLLs out of the four `netstandard2.1` shared projects and `Assets/Plugins/ProjectAegis`; this
  assembly is the base case for that gate — it must stay package-free.
- `IsPackable=false`.

It is **not yet in `ProjectAegis.sln`** (joins at DRG-735). Build and test it directly:

```bash
dotnet build src/ProjectAegis.Ai.Contracts/ProjectAegis.Ai.Contracts.csproj
dotnet test  src/ProjectAegis.Ai.Contracts/test/ProjectAegis.Ai.Contracts.Tests.csproj
```

The test project ([`test/ProjectAegis.Ai.Contracts.Tests.csproj`](../../src/ProjectAegis.Ai.Contracts/test/ProjectAegis.Ai.Contracts.Tests.csproj))
targets `net11.0` and references xUnit; the main csproj excludes `test/**` from compilation. See the
project [`README.md`](../../src/ProjectAegis.Ai.Contracts/README.md) for the terse freeze summary.

---

## Schema identity & the freeze rule

[`AiContractSchema`](../../src/ProjectAegis.Ai.Contracts/AiContractSchema.cs) is the single source of
truth for the version:

| Member | Value | Use |
|--------|-------|-----|
| `AiContractSchema.Version` | `2` (int) | Stamped into `EvidenceHeader.SchemaVersion` and `ArtifactEnvelope.SchemaVersion` (EvidenceProjector / JsonlEvidenceEnvelopePublisher / AarAssistantPipeline / AdviceExplanationPipeline). Wave 0 froze at `1`; DRG-749 additive `AdviceExplanationContent` bumped to `2`. |
| `AiContractSchema.VersionString` | `"2"` | String form for envelopes and transport headers. |

**Freeze rule** (effective at end of Wave 0):

- Changes are **additive only**, and each additive change **bumps `Version`**.
- Public members are **never renamed or removed** without a new *major* schema version **and** a
  coordinated consumer update — the on-disk and on-wire shape is a contract with Unity and the CLI.
- **Property declaration order is part of the contract** (see [Determinism](#determinism--invariants)).

---

## Evidence model

A completed run's order log is projected (by slice 1, not in this assembly) into an
`EvidenceDocument`: a header plus a list of side-filtered rows.

### `EvidenceId` — stable row identity

[`EvidenceId.From(fingerprintPrefix, sequenceId)`](../../src/ProjectAegis.Ai.Contracts/EvidenceId.cs)
returns `"{fingerprintPrefix}:{sequenceId}"` — e.g. `EvidenceId.From("abc123", 42)` → `"abc123:42"`.
The `sequenceId` is formatted with `CultureInfo.InvariantCulture`; a blank/whitespace prefix throws
`ArgumentException`. The prefix comes from the order-log fingerprint and the sequence from
`OrderLogEntry.SequenceId`, so an evidence ID points back to exactly one logged event.

### `EvidenceHeader`

[`EvidenceHeader`](../../src/ProjectAegis.Ai.Contracts/EvidenceHeader.cs) — export header (property
order is frozen):

| # | Property | Type | Notes |
|---|----------|------|-------|
| 1 | `SchemaVersion` | `int` | `AiContractSchema.Version`. |
| 2 | `BuildIdentity` | `string` | Build that produced the evidence. |
| 3 | `ScenarioId` | `string` | e.g. `baltic-v3`. |
| 4 | `CatalogRevision` | `string` | Catalog revision the run used. |
| 5 | `OrderLogFingerprint` | `string` | Fingerprint of the source order log. |
| 6 | `ViewingSide` | `ViewingSide` | Side this document is filtered to. |
| 7 | `DebriefMode` | `bool` | `true` only for explicit post-run full-truth debrief. |
| 8 | `EvidenceIds` | `IReadOnlyList<string>` | The IDs included in this document. |

### `EvidenceEntry`

[`EvidenceEntry`](../../src/ProjectAegis.Ai.Contracts/EvidenceEntry.cs) — one row projected from an
order-log entry (property order is frozen):

| # | Property | Type | Notes |
|---|----------|------|-------|
| 1 | `EvidenceId` | `string` | `{fingerprint-prefix}:{SequenceId}`. |
| 2 | `SequenceId` | `ulong` | Source order-log sequence number. |
| 3 | `Kind` | `EvidenceEntryKind` | See the mapping table below. |
| 4 | `SimTime` | `double` | Simulation time of the event. |
| 5 | `ViewingSide` | `ViewingSide` | Side this row belongs to. |
| 6 | `UnitId` | `string?` | Optional unit reference. |
| 7 | `Summary` | `string?` | Optional human-readable summary. |
| 8 | `Attributes` | `IReadOnlyDictionary<string,string>?` | Optional key/value detail. |

**`Summary`, `UnitId`, and `Attributes` are untrusted data.** They originate from scenario/unit text
and must be treated as data in every consumer — delimited, never concatenated into a model
instruction (per the plan's untrusted-input rule).

### `EvidenceDocument`

[`EvidenceDocument(Header, Entries)`](../../src/ProjectAegis.Ai.Contracts/EvidenceDocument.cs) — the
complete side-filtered document: one `EvidenceHeader` plus `IReadOnlyList<EvidenceEntry>`.

### `EvidenceEntryKind` — mirrors order-log kinds **by name, not by value**

[`EvidenceEntryKind`](../../src/ProjectAegis.Ai.Contracts/EvidenceEntryKind.cs) mirrors the canonical
[`OrderLogEntryKind`](../../src/ProjectAegis.Delegation/Decision/OrderLogEntryKind.cs) **by member
name**, so this assembly needs no reference to the Delegation layer. It adds a `Header = 0` sentinel
with no order-log counterpart, which shifts every other value **+1** relative to `OrderLogEntryKind`:

| Name | `EvidenceEntryKind` | `OrderLogEntryKind` |
|------|:-------------------:|:-------------------:|
| `Header` | 0 | *(none)* |
| `AgentDecision` | 1 | 0 |
| `PolicyDenial` | 2 | 1 |
| `Engagement` | 3 | 2 |
| `ControllerChange` | 4 | 3 |
| `GroupMemberDetach` | 5 | 4 |
| `GroupMemberRejoin` | 6 | 5 |
| `MagazineChange` | 7 | 6 |
| `ContactChange` | 8 | 7 |
| `MissionTransition` | 9 | 8 |
| `EventFired` | 10 | 9 |
| `EngagementOutcome` | 11 | 10 |
| `PlayerOrder` | 12 | 11 |
| `PolicyUpdate` | 13 | 12 |
| `ModeChange` | 14 | 13 |
| `CommsStateChange` | 15 | 14 |
| `FuelStateChange` | 16 | 15 |
| `FuelBurn` | 17 | 16 |
| `PlatformDamageChange` | 18 | 17 |
| `OrdnanceStateChange` | 19 | 18 |

> **Pitfall.** `System.Text.Json` serializes enums as **integers** by default (the round-trip tests use
> default options). Because the two enums share names but not values, the slice-1 exporter must map
> `OrderLogEntryKind` → `EvidenceEntryKind` **by name**, never by casting the raw integer — a raw cast
> would silently shift every kind by one. On the wire, `Kind` is the `EvidenceEntryKind` value
> (`1` = `AgentDecision`), not the order-log value.

---

## AI artifact model

### `ArtifactEnvelope`

[`ArtifactEnvelope`](../../src/ProjectAegis.Ai.Contracts/ArtifactEnvelope.cs) is the versioned identity
of one generated AI artifact. **Cache identity is the hash of all of its fields** — any change to
build, scenario, catalog, order log, provider, model, prompt, evidence set, side, debrief flag, or
status produces a different artifact (property order is frozen):

| # | Property | Type | Notes |
|---|----------|------|-------|
| 1 | `SchemaVersion` | `int` | `AiContractSchema.Version`. |
| 2 | `BuildIdentity` | `string` | Build that generated the artifact. |
| 3 | `ScenarioId` | `string` | |
| 4 | `CatalogRevision` | `string` | |
| 5 | `OrderLogFingerprint` | `string` | Ties the artifact to its evidence source. |
| 6 | `ProviderId` | `string` | e.g. `fake` for the offline client. |
| 7 | `ModelId` | `string` | |
| 8 | `PromptVersion` | `string` | e.g. `aar-explain-1`. |
| 9 | `EvidenceIds` | `IReadOnlyList<string>` | Evidence rows the artifact cites. |
| 10 | `ViewingSide` | `ViewingSide` | Side the artifact was generated for. |
| 11 | `DebriefMode` | `bool` | Full-truth debrief flag. |
| 12 | `Status` | `GenerationStatus` | Generation outcome (below). |
| 13 | `ContentHash` | `string` | Hash of the artifact content, e.g. `sha256:…`. |

Authored content additionally carries a `ProvenanceTag` with `Source = "ai"` **outside** this
assembly — that record lives in `ProjectAegis.Data` and is reused rather than duplicated here.

### `GenerationStatus`

[`GenerationStatus`](../../src/ProjectAegis.Ai.Contracts/GenerationStatus.cs) — explicit outcomes are
mandatory; a failure is a status, never a thrown-away result:

| Value | Member | Meaning |
|:-----:|--------|---------|
| 0 | `Succeeded` | Generation completed and passed validation. |
| 1 | `TimedOut` | Request exceeded its deadline. |
| 2 | `Cancelled` | Caller cancelled. |
| 3 | `MalformedOutput` | Model output failed to parse/validate. |
| 4 | `ProviderUnavailable` | Provider/model could not be reached. |
| 5 | `RejectedCitation` | A cited evidence ID was absent from the evidence set. |
| 6 | `RejectedNumber` | A number in prose disagreed with the authoritative report. |

`RejectedCitation` and `RejectedNumber` encode the plan's anti-fabrication checks: the host copies
quantitative values from the source report and validates every cited ID, rejecting the artifact on
mismatch rather than showing invented facts.

### `ViewingSide`

[`ViewingSide`](../../src/ProjectAegis.Ai.Contracts/ViewingSide.cs): `Unknown=0`, `Blue=1`, `Red=2`,
`Neutral=3`, `Debrief=4`. The side filter is applied **before** prompt construction, not after
generation. `Debrief` is an explicit post-run full-truth view and is **not** the default player view;
full truth requires `DebriefMode` recorded in the header/envelope.

---

## Transport to clients

[`AiArtifactTransport`](../../src/ProjectAegis.Ai.Contracts/AiArtifactTransport.cs) is the
process-boundary DTO a Unity or CLI client receives (an ADR-010 presentation client). It carries only
envelope metadata plus **opaque** content — **no provider SDK types**:

| Property | Type | Notes |
|----------|------|-------|
| `Envelope` | `ArtifactEnvelope` | Identity + status of the artifact. |
| `ContentMediaType` | `string` | e.g. `application/json`. |
| `ContentText` | `string?` | The opaque content, or `null`. |
| `Warnings` | `IReadOnlyList<string>?` | Optional non-fatal warnings. |

Because the content is opaque text with a media type, a failed generation still crosses the boundary
as a well-formed transport whose `Envelope.Status` names the failure (e.g. `TimedOut`) and whose
`Warnings` explain it, letting the client fall back to the deterministic report.

---

## Determinism & invariants

- **Property order = declaration order, and it is frozen.** The round-trip tests assert the serialized
  JSON key order equals the record's declaration order for `EvidenceHeader`, `EvidenceEntry`,
  `ArtifactEnvelope`, and `AiArtifactTransport`. Reordering or inserting a property mid-record is a
  breaking change even though the type still compiles.
- **Deterministic serialization.** Tests serialize with `WriteIndented = false` and
  `PropertyNamingPolicy = null` (PascalCase as declared), then re-serialize the deserialized value and
  assert byte-for-byte equality — including when an `IReadOnlyList` round-trips as an array vs a list.
- **Enum-by-name mapping.** See the [`EvidenceEntryKind` pitfall](#evidenceentrykind--mirrors-order-log-kinds-by-name-not-by-value): the default integer encoding does **not** line up with `OrderLogEntryKind`.
- **Side isolation is upstream.** Evidence is filtered to `ViewingSide` before it reaches a prompt;
  this assembly only records which side a document/artifact belongs to.
- **Text fields are untrusted.** `Summary`, `UnitId`, and `Attributes` are data, not instructions.
- **Zero dependencies is an invariant, not a convenience.** No `PackageReference`, and no reference to
  Delegation / Sim / Data / `UnityEngine` / `Microsoft.Extensions.AI`. The W0-D architecture gate
  enforces that AI packages never leak into shared projects or the Unity plug-in folder.

---

## Tests (pins)

All in [`ContractRoundTripTests`](../../src/ProjectAegis.Ai.Contracts/test/ContractRoundTripTests.cs)
(xUnit, `net11.0`, **15** tests = 8 facts + a 7-case `GenerationStatus` theory):

| Test | Covers |
|------|--------|
| `Schema_version_is_current` | `AiContractSchema.Version == 2`, `VersionString == "2"`. |
| `AdviceExplanationContent_round_trip_preserves_property_order` | DRG-749 additive explanation content JSON key order == declaration order. |
| `EvidenceId_format_is_prefix_colon_sequence` | `From("abc123", 42) == "abc123:42"`. |
| `EvidenceHeader_round_trip_preserves_property_order` | Header JSON key order == declaration order. |
| `EvidenceEntry_round_trip_preserves_property_order` | Entry JSON key order == declaration order. |
| `EvidenceDocument_round_trip` | Document re-serializes byte-identically. |
| `ArtifactEnvelope_round_trip_preserves_property_order` | Envelope JSON key order == declaration order. |
| `AiArtifactTransport_round_trip` | Transport (with failure status + warnings) re-serializes identically. |
| `GenerationStatus_round_trips` (theory) | Each of the 7 statuses round-trips through JSON. |

---

## See also

- [AI feature integration plan](../superpowers/plans/2026-11-12-dotnet-11-ai-feature-integration.md) —
  the Wave 0 → slice 5 roadmap this assembly anchors (host boundary, egress rule, envelope,
  pending ADR-030).
- [order-log-runtime.md](order-log-runtime.md) — the `IReadOnlyOrderLog` and `OrderLogEntry.SequenceId`
  that evidence IDs and kinds derive from.
- [after-action-ledger-projection.md](after-action-ledger-projection.md) — the existing read-model that
  also mirrors the combat-event/order-log kinds by name without a hard Delegation dependency.
- [determinism-and-replay.md](determinism-and-replay.md) — why AI artifacts are versioned separately
  and never enter the Baltic v2 `SimWorldHash`.
- [delegation-plugin-dll-copy.md](delegation-plugin-dll-copy.md) — the Unity plug-in copy path the
  zero-dependency rule protects.
