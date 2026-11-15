# ADR-030: AI Host Boundary — Separate Assemblies, Egress, Artifact Envelope

## Status

**Proposed** — conditional owner ACCEPT recorded via Projects Manager + Decision (2026-11-15) is **not** applied as Accepted on this draft. Status stays **Proposed** until Projects Manager confirms acceptance with the owner and an authorized follow-up lands. This draft does **not** invent or flip owner acceptance.

## Date

2026-11-15

## Last Verified

2026-11-15 — gates under DRG-733 (`AiHostBoundaryGateTests`); Baltic hash / `DelegationBridge` hotpath untouched. Owner sign-off: **conditional ACCEPT pending Projects Manager confirmation** — document remains Proposed on draft PR #853.

## Decision Makers

Owner / Technical Director (drg amtd) — sole accept/waive authority. Linear: [DRG-733](https://linear.app/drgamtd-workspace/issue/DRG-733). Parent epic [DRG-723](https://linear.app/drgamtd-workspace/issue/DRG-723). Conditional ACCEPT channel: Projects Manager + Decision bot (confirmation later).

## Summary

Project Aegis will host Microsoft.Extensions.AI and related packages only in dedicated `net11.0` AI assemblies (`ProjectAegis.Ai.Host` and siblings). Unity-bound `netstandard2.1` projects and `Assets/Plugins/ProjectAegis` must never reference or receive those packages. Evidence leaves the host only through a versioned artifact envelope with side filtering before prompt construction; evidence text is untrusted data. Egress respects the dual-track CMO firewall (ADR-013).

**Decision (tick / network boundary):** `ProjectAegis.Sim` and `ProjectAegis.Delegation` make **no** network or LLM calls on the simulation path. AI output reaches the sim **only** as validated, human-approved artifacts, **never** on the tick path. Optional `Delegation/Hindsight/` memory sidecar HTTP (non-tick, non-LLM) is a documented exception outside that path; AI provider calls stay in `Ai.Host` only.

## Engine Compatibility

| Field | Value |
|-------|-------|
| Engine | Unity 6.3 LTS client + .NET 11 headless |
| Unity APIs | None on the AI host path |
| Targets | AI host: `net11.0` only; Contracts: `netstandard2.1;net11.0` with **zero** PackageReference |
| Risk | MEDIUM product risk if packages leak into Unity Plugins via `copy-delegation-assemblies.sh` |

## ADR Dependencies

| Relationship | ADR / artifact |
|--------------|----------------|
| **Depends on** | ADR-001 (assembly boundary), ADR-006 (data read/write split), ADR-013 (dual-track CMO import firewall), ADR-014 (no Lua / no arbitrary execution on authoring path), ADR-015 (agent-authored scenario transparency — referenced for envelope provenance / agent-authored label; **do not block** Wave 0 on ADR-015 status), ADR-019 (read-only order log for AAR) |
| **Enables** | Wave 1+ AI slices (provider adapters, retrieval, evaluation lanes) |
| **Conflicts with** | None — additive. Does not authorize paid provider calls in CI |

## Decision

### 1. Separate AI assemblies

| Assembly | TFM | May reference Microsoft.Extensions.AI* |
|----------|-----|----------------------------------------|
| `ProjectAegis.Ai.Contracts` | `netstandard2.1;net11.0` | **No** — zero PackageReference |
| `ProjectAegis.Ai.Host` (and future AI-only hosts) | `net11.0` | Yes |
| Data / Sim / Delegation / UnityAdapter | `net11.0;netstandard2.1` | **No** on either TFM for AI/provider packages |

`tools/copy-delegation-assemblies.sh` copies every DLL from the UnityAdapter `netstandard2.1` publish into Unity. Any AI package on a shared project would ship into the plug-in folder. Architecture tests under `AiHostBoundaryGateTests` enforce the package and Plugins DLL fences, plus Sim / non-Hindsight Delegation source scans that reject `System.Net.Http`, `HttpClient`, and `Socket` usage.

### 2. Egress rule (dual-track firewall)

- No proprietary CMO `*.db3` content may enter prompts, fixtures, or evidence artifacts (ADR-013 Track A stays outside this repo).
- AI fixtures and evidence use repository-public synthetic content only.
- Network egress to paid providers is out of CI and out of agent default runs unless the owner explicitly authorizes a live lane.

### 3. Artifact envelope

Every AI generation result is an envelope with:

| Field | Role |
|-------|------|
| Schema version | Contracts freeze (`AiContractSchema.Version`) |
| Generation status | Success / failure / denial / empty / malformed lanes |
| Viewing side | Side filter applied before prompt construction |
| Evidence IDs | `{fingerprint-prefix}:{SequenceId}` |
| Structured facts | Machine-readable facts separate from prose |
| **Prompt version** | Provenance — which prompt/template produced the artifact (ADR-015 agent-authored labeling can derive later) |
| **Rationale** | Provenance — short structured reason / model summary bound to the artifact |
| **Diff / content hash** | Provenance — hash of proposed content or diff so stale applies are refused |
| **Approving id** | Provenance — human (or gated) approver identity when the artifact is accepted |

Contracts freeze the DTO shape (DRG-730); Host validates and persists. ADR-015 remains the labeling policy reference — Wave 0 does not block on ADR-015 being Accepted.

### 4. Side filtering before prompt construction

Two-sided order logs must filter to the viewing side before any prompt or retrieval construction. Hidden-side entries must not appear in evidence text shown to the other side (fixtures under DRG-732 encode the expected cut).

### 5. Evidence text is untrusted data

Order-log fields, scenario names, briefs, and unit names may embed instructions. Treat all evidence text as untrusted data for prompt construction; never execute content from evidence as code or shell. Authoring/validation paths remain free of `Process.Start` (existing gate) and of .NET 11 `Process.Run*` / `RunAndCapture` / `StartAndForget` / `SafeProcessHandle.Start` (this ADR's extended gate).

## Alternatives considered (rejected)

| Option | Why rejected |
|--------|----------------|
| An AI folder inside the Mission Editor CLI | Couples provider SDKs and credentials to the authoring CLI; conflicts with credential-free CI and shared `netstandard2.1` publish. Prefer thin CLI verbs calling a separate `Ai.Host`. |
| AI packages on shared or Unity-bound projects | `copy-delegation-assemblies.sh` would ship AI DLLs into `Assets/Plugins/ProjectAegis`. Forbidden by the package-leak gate. |
| `.NET 11` `Process.Run*` helpers for validation | Shelling out from authoring/validation contradicts ADR-014 / requirement 11 (no arbitrary code execution on that path). Validators stay in-process; Process helpers rejected except Unity-to-host process boundary outside this ADR's authoring scan. |

## Consequences

- Draft PR may land gates + this **Proposed** ADR; Wave 0 exit still needs confirmed owner Accept or explicit Waive (Projects Manager confirms with owner — not claimed here).
- Slice hosts land later; Contracts may merge before Host.
- Traceability index update is owned by the integration issue (DRG-735), not this ADR PR.

## Owner action required

Status remains **Proposed** on draft #853. Projects Manager will confirm acceptance with the owner later. Do not treat conditional ACCEPT chat as Status = Accepted until that confirmation and an authorized status flip.

## References

- Plan: `docs/superpowers/plans/2026-11-12-dotnet-11-ai-feature-integration.md`
- Gate: `src/ProjectAegis.Data.Tests/Architecture/AiHostBoundaryGateTests.cs`
- Related: ADR-001, ADR-006, ADR-013, ADR-014, ADR-015, ADR-019; DRG-730 / DRG-732 / DRG-733
