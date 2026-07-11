# ADR-013: CMO Scenario Import — Legal/Policy Stance

## Status

**Proposed**

## Date

2026-07-01

## Last Verified

2026-07-01

## Decision Makers

Enterprise architect (DRGAMTD); milsim architecture review. **Resolution owner: Product / Legal.** **Target decision date: 2026-09-01.** Resolves Open Question 1 of requirement [11-Agentic-Mission-Editor](../../Game-Requirements/requirements/11-Agentic-Mission-Editor.md).

**Review date:** 2026-09-01 — Product / Legal must Accept, Reject, or defer with a new dated legal review note.

## Summary

The Agentic Mission Editor requirement asks whether Project Aegis should import Command: Modern Operations (`.scen`) scenarios, and on what legal footing. This ADR proposes that **v1 ships no CMO import**; a best-effort importer is deferred to Phase 2, and — if built — must be a **clean-room mapping over observable patterns only** (no proprietary CMO data, schema dumps, or decompiled artifacts), pending explicit Product/Legal sign-off.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS / .NET 8 headless — not engine-API-dependent |
| **Domain** | Core |
| **Knowledge Risk** | LOW — policy/architecture decision, no engine API surface |
| **References Consulted** | GDD `design/gdd/agentic-mission-editor.md` §1, §2; requirement doc 11 §6, §9; ADR-011 (clean-room precedent) |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None (engine); legal review required before any importer is built |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None |
| **Enables** | Phase 2 Migration Agent (doc 11 §6) — cannot start until this is Accepted |
| **Blocks** | Any "CMO import" epic — blocked until Product/Legal accept this ADR |
| **Ordering Note** | Must be Accepted before any importer design or spike is scheduled |

## Context

### Problem Statement

CMO is the parity baseline for this editor (doc 11 "CMO Baseline"). Community demand for reusing existing `.scen` libraries is real, but importing another commercial title's scenario files raises licensing, IP, and EULA questions that engineering cannot resolve unilaterally. Deciding now prevents a Phase 2 spike from building on an unvetted legal footing.

### Current State

v1 has no import path. The GDD explicitly defers CMO import to Phase 2/3 (§1, §2), and doc 11 §9 lists "CMO import pipeline (best-effort)" as **P1**. The native `*.aegis-scenario` format is the only supported input.

### Constraints

- Legal: CMO scenario files and the underlying DB are third-party IP; no proprietary data may be copied into Aegis.
- Precedent: ADR-011 and doc 21 already commit the platform editor to CMO parity via "clean-room, observable patterns only" — this ADR keeps import consistent with that stance.
- Timeline: import is P1/Phase 2; no v1 delivery pressure.
- Compatibility: any importer must emit the canonical `*.aegis-scenario` file and pass the same Validation Engine as hand-authored scenarios (no bypass path).

### Requirements

- v1 ships without CMO import.
- Any future importer is opt-in, best-effort, and legally cleared before build.
- Imported scenarios are indistinguishable from native ones downstream (same schema, same validation gate).

## Decision

1. **v1 ships no CMO import.** The only supported input is the native `*.aegis-scenario` package.
2. **Import is a Phase 2 concern**, delivered (if at all) via the Migration Agent (doc 11 §6), and only after Product/Legal accept this ADR.
3. **Clean-room only.** Any importer maps *observable* scenario patterns (mission types, reference-point geometry, trigger/condition/action structure) to Aegis canonical objects. It must not embed, redistribute, or derive from proprietary CMO data files, the CMO database, or decompiled binaries. Unit/platform identity resolves through the Aegis Platform DB (system 4), never a copied CMO catalog.
4. **No governance bypass.** An importer stages output through the same Validation Engine and export gate as hand-authored scenarios; a partially-mappable scenario imports as an *editable-invalid* file (GDD §3.8) rather than silently dropping content.

### Implementation Guidelines

Defer implementation. When Phase 2 scopes the importer, produce a documented CMO→Aegis mapping table (doc 11 §9) reviewed by Legal, and treat unmappable constructs as explicit, logged gaps — never silent data loss.

## Alternatives Considered

### Alternative 1: Ship best-effort CMO import in v1

- **Description**: Build the importer now, alongside native save/load.
- **Pros**: Immediate access to existing scenario libraries; strong migration story.
- **Cons**: Unvetted legal exposure; large parser effort competing with v1 foundation; risks embedding proprietary data.
- **Rejection Reason**: Legal footing is unresolved and v1 must ship the trustworthy foundation first (GDD §2).

### Alternative 2: Never support CMO import

- **Description**: Permanently rule out import.
- **Pros**: Zero legal risk; simplest scope.
- **Cons**: Discards real community value and a differentiator.
- **Rejection Reason**: Too absolute; a clean-room, legally-cleared importer remains a defensible Phase 2 option.

## Consequences

### Positive

- Removes legal ambiguity from the v1 critical path.
- Keeps import consistent with the platform editor's established clean-room stance (ADR-011).
- Any future importer inherits the full determinism/validation guarantees for free.

### Negative

- No day-one migration path for existing CMO scenario authors.
- A clean-room importer is slower to build than a data-coupled one.

### Neutral

- Import becomes a discrete, legally-gated Phase 2 workstream rather than a v1 feature.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| Legal review blocks import entirely | Medium | Medium | Native format + clean-room mapping keep value even if raw `.scen` import is disallowed |
| Community expects day-one import | Medium | Low | Communicate Phase 2 sequencing and the legal rationale up front |
| Clean-room boundary erodes during implementation | Low | High | Mapping table review by Legal; no CMO data files in the repo or build |

## Performance Implications

N/A — this is a policy/scoping decision with no runtime component in v1.

## Migration Plan

No migration in v1. When import is scoped in Phase 2, deliver a reviewed mapping table and stage all output through the standard write/validation gate.

**Rollback plan**: If a Phase 2 importer proves legally or technically untenable, disable the Migration Agent import path; native authoring is unaffected.

## Validation Criteria

- [ ] v1 exposes no CMO import entry point in UI, CLI, or MCP.
- [ ] Product/Legal record an accept/reject decision by 2026-09-01.
- [ ] If accepted for build, a Legal-reviewed CMO→Aegis mapping table exists before any importer code lands.
- [ ] No proprietary CMO data files enter the repository or build artifacts.

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `design/gdd/agentic-mission-editor.md` | Agentic Mission Editor (11) | §1/§2: "CMO import … deferred (Phase 2/3)" | Confirms no v1 import and gates any future importer on legal clearance |
| `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` | Agentic Mission Editor (11) | §6 Migration Agent "where legally/technically permitted"; §9 CMO import (P1, best-effort) | Records the legal/policy stance the requirement flagged as an open question |

## Related

- Requirement [11-Agentic-Mission-Editor](../../Game-Requirements/requirements/11-Agentic-Mission-Editor.md) (Open Question 1)
- GDD [agentic-mission-editor](../../design/gdd/agentic-mission-editor.md)
- [ADR-011 Platform Editor — Excel Round-Trip](adr-011-platform-editor-excel-roundtrip.md) (clean-room, observable-patterns-only precedent)
- [ADR-008 Mission Editor Validation Engine](adr-008-mission-editor-validation-engine.md) (import must pass the same gate)
