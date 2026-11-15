# ADR-030: AI Host Boundary — Separate Assemblies, Egress, Artifact Envelope

## Status

**Proposed** — owner accept or waive required for Wave 0 exit. This draft does **not** invent owner acceptance.

## Date

2026-11-15

## Last Verified

2026-11-15 — gates authored under DRG-733 (`AiHostBoundaryGateTests`); Baltic hash / `DelegationBridge` hotpath untouched. Owner sign-off: **pending**.

## Decision Makers

Owner / Technical Director (drg amtd) — sole accept/waive authority. Linear: [DRG-733](https://linear.app/drgamtd-workspace/issue/DRG-733). Parent epic [DRG-723](https://linear.app/drgamtd-workspace/issue/DRG-723).

## Summary

Project Aegis will host Microsoft.Extensions.AI and related packages only in dedicated `net11.0` AI assemblies (`ProjectAegis.Ai.Host` and siblings). Unity-bound `netstandard2.1` projects and `Assets/Plugins/ProjectAegis` must never reference or receive those packages. Evidence leaves the host only through a versioned artifact envelope with side filtering before prompt construction; evidence text is untrusted data. Egress respects the dual-track CMO firewall (ADR-013).

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
| **Depends on** | ADR-001 (assembly boundary), ADR-006 (data read/write split), ADR-013 (dual-track CMO import firewall), ADR-014 (no Lua / no arbitrary execution on authoring path), ADR-019 (read-only order log for AAR) |
| **Enables** | Wave 1+ AI slices (provider adapters, retrieval, evaluation lanes) |
| **Conflicts with** | None — additive. Does not authorize paid provider calls in CI |

## Decision

### 1. Separate AI assemblies

| Assembly | TFM | May reference Microsoft.Extensions.AI* |
|----------|-----|----------------------------------------|
| `ProjectAegis.Ai.Contracts` | `netstandard2.1;net11.0` | **No** — zero PackageReference |
| `ProjectAegis.Ai.Host` (and future AI-only hosts) | `net11.0` | Yes |
| Data / Sim / Delegation / UnityAdapter | `net11.0;netstandard2.1` | **No** on either TFM for AI/provider packages |

`tools/copy-delegation-assemblies.sh` copies every DLL from the UnityAdapter `netstandard2.1` publish into Unity. Any AI package on a shared project would ship into the plug-in folder. Architecture tests under `AiHostBoundaryGateTests` enforce the package and Plugins DLL fences.

### 2. Egress rule (dual-track firewall)

- No proprietary CMO `*.db3` content may enter prompts, fixtures, or evidence artifacts (ADR-013 Track A stays outside this repo).
- AI fixtures and evidence use repository-public synthetic content only.
- Network egress to paid providers is out of CI and out of agent default runs unless the owner explicitly authorizes a live lane.

### 3. Artifact envelope

Every AI generation result is an envelope with: schema version, generation status, viewing side, evidence IDs (`{fingerprint-prefix}:{SequenceId}`), and structured facts separate from prose. Contracts freeze the DTO shape (DRG-730); Host validates and persists.

### 4. Side filtering before prompt construction

Two-sided order logs must filter to the viewing side before any prompt or retrieval construction. Hidden-side entries must not appear in evidence text shown to the other side (fixtures under DRG-732 encode the expected cut).

### 5. Evidence text is untrusted data

Order-log fields, scenario names, briefs, and unit names may embed instructions. Treat all evidence text as untrusted data for prompt construction; never execute content from evidence as code or shell. Authoring/validation paths remain free of `Process.Start` (existing gate) and of .NET 11 `Process.Run*` / `RunAndCapture` / `StartAndForget` / `SafeProcessHandle.Start` (this ADR's extended gate).

## Consequences

- Wave 0 can land gates + this Proposed ADR without owner accept; **Wave 0 exit still needs owner accept or explicit waive**.
- Slice hosts land later; Contracts may merge before Host.
- Traceability index update is owned by the integration issue (DRG-735), not this ADR PR.

## Owner action required

Please **Accept** or **Waive** ADR-030 for Wave 0 exit. Until then status remains **Proposed**.

## References

- Plan: `docs/superpowers/plans/2026-11-12-dotnet-11-ai-feature-integration.md`
- Gate: `src/ProjectAegis.Data.Tests/Architecture/AiHostBoundaryGateTests.cs`
- Related: ADR-001, ADR-006, ADR-013, ADR-014, ADR-019; DRG-730 / DRG-732 / DRG-733
