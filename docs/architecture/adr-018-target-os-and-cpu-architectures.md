# ADR-018: Target Operating Systems and CPU Architectures (v1)

## Status

**Accepted**

## Date

2026-07-25

## Last Verified

2026-07-25 (codifies product NFR decided 2026-06-09; aligns with headless-first CI practice)

## Decision Makers

Product NFR owners (2026-06-09 platform decision in [01-Project-Overview](../../Game-Requirements/requirements/01-Project-Overview.md)); architecture codification by Technical Director / enterprise architect review. **Change requires ADR amendment + explicit product re-decision.**

## Summary

Project Aegis v1 ships an **interactive client on Windows 10/11 x64** (Steam primary) and runs **headless sim / batch / CI on Linux x64**. The managed core (`ProjectAegis.Data`, `.Sim`, `.Delegation`) is **host-agnostic** and must not grow OS-specific sim logic. **x64 only** for v1 ship and gate hosts. **macOS, ARM64, console, and Steam Deck** are out of scope for v1. Cross-OS **determinism contracts** (culture, paths, golden parity) are architecture law.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.14f1) player builds; .NET SDK **8.0.400** headless (`global.json`) |
| **Domain** | Core / Platform / Packaging / CI |
| **Knowledge Risk** | LOW — OS/RID matrix and portability rules; no post-cutoff Unity API dependency |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `docs/engine-reference/dotnet/README.md`, ADR-005 (superseded → managed headless-first), ADR-008, ADR-010, ADR-011, req 01 NFR Platforms, `docs/engineering/buildkite-ci.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Replay goldens and solution tests green on primary CI (Linux x64); validation / golden text fixtures culture-invariant; player packaging targets StandaloneWindows64 only for v1 commercial path |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | [ADR-001](adr-001-sim-assembly-boundary.md) (engine-free Sim); [ADR-006](adr-006-data-layer-boundary.md) (engine-free Data); [ADR-010](adr-010-headless-first-command-driven-ui.md) (headless-first host split); [ADR-008](adr-008-mission-editor-validation-engine.md) (cross-host validation determinism) |
| **Enables** | Steam Windows player packaging; Linux headless AvA / batch farm RIDs; CI agent sizing; release-checklist platform gates |
| **Blocks** | macOS player SKU; ARM64 CI as a required gate; Steam Deck / console targets — until this ADR is amended |
| **Ordering Note** | Product NFR in req 01 (2026-06-09) is the product authority; this ADR is the **architecture enforcement** of that matrix. Packaging/store work must cite this ADR. |

## Context

### Problem Statement

The product NFR already states Windows primary + Linux headless and excludes macOS / Deck / console for v1, but no architecture ADR bound packaging, CI, RID choice, or cross-OS determinism. Without that record:

- agents and contributors re-litigate "do we support Linux players / ARM / macOS?";
- OS `#if` and path/locale hazards can leak into sim, catalog, and golden paths;
- Steam / release packaging has no architecture-level non-goals to gate scope.

### Current State

| Surface | Practice today |
|---------|----------------|
| Product NFR | Windows 10/11 x64 primary (Steam); Linux x64 headless only; no macOS / console / Deck ([req 01](../../Game-Requirements/requirements/01-Project-Overview.md)) |
| CI | Buildkite **hosted Linux** agents ([buildkite-ci.md](../engineering/buildkite-ci.md)) |
| Headless core | `dotnet test` / CLI / batch on .NET 8 without Unity (ADR-010, ADR-005 supersession) |
| Player presentation | Unity 6.3 LTS over managed sim (ADR-007, ADR-010) |
| Cross-host checks | ADR-008 requires byte-identical validation reports on Linux and Windows CI where both run; ADR-011 requires `InvariantCulture` Excel parsing |

### Constraints

- Determinism: same seed + scenario + catalog snapshot → same order-log / world hash regardless of host OS (within supported matrix).
- Headless-first: sim scale and goldens must not depend on Unity player loop (ADR-005 superseded, ADR-010).
- Assemblies `ProjectAegis.Data` / `.Sim` / `.Delegation` remain free of `UnityEngine` and free of sim-affecting OS branches.
- Commercial path targets Steam PC; headless AvA is a Linux container workload.
- Reference hardware for player and AvA node is defined in req 01 NFR (not repeated as architecture alternatives here).

### Requirements

- One authoritative OS/CPU matrix for v1 ship + CI.
- Clear split: **player host** vs **headless host** vs **dev host**.
- Portable core code; OS-specific code only at presentation/build/packaging seams.
- Explicit non-goals so Launch/store scope cannot silently expand.

## Decision

**Accepted.** Adopt the following v1 platform matrix and architecture rules.

### 1. Supported host matrix (v1)

| Role | OS | CPU | Notes |
|------|----|-----|--------|
| **Interactive player (ship)** | Windows 10 / 11 | **x64** | Primary commercial SKU (Steam). Unity player target: **StandaloneWindows64**. |
| **Headless farm / AvA / batch** | Linux | **x64** | Primary scale path; container-friendly; no Unity Editor required. |
| **CI gate (authoritative)** | Linux | **x64** | Buildkite (or successor) Linux agents run build + full test + ReplayGolden + C2 smoke. |
| **Local dev / optional local gate** | Windows or Linux | **x64** | Developers may use either; `tools/verify-ci-local.ps1` is the Windows-oriented full gate. Dev hosts are not additional ship SKUs. |

### 2. Explicit non-goals (v1)

| Target | Status |
|--------|--------|
| macOS player or headless gate | **Out of scope** |
| Windows ARM64 / Linux ARM64 / Apple Silicon as ship or required CI | **Out of scope** |
| Steam Deck | **Out of scope** |
| Consoles | **Out of scope** |
| Linux interactive Steam player | **Out of scope** (Linux is headless/batch only) |
| Multi-arch fat binaries / universal builds | **Out of scope** |

Reopening any non-goal requires a **new ADR or amendment** plus product re-decision in req 01.

### 3. Artifact and RID guidance

| Artifact | Host | Guidance |
|----------|------|----------|
| Unity player build | Windows x64 | Ship StandaloneWindows64 only for v1 commercial. |
| Headless `dotnet` apps (CLI, demo, batch runners, tests) | Linux x64 primary; Windows x64 for local | Prefer portable TFMs (`net8.0` / `netstandard2.1` as today). Self-contained publish, if used, is **`linux-x64`** for farm and optional **`win-x64`** for Windows tooling — not a second product SKU. |
| SQLite catalog / scenario packages | All supported hosts | Files are portable; no host-specific schema. |

Do **not** introduce OS-specific NuGet packages into `ProjectAegis.Sim` / `.Delegation` / `.Data` without an architecture review.

### 4. Host-agnostic core (mandatory)

```
  +---------------------------+     +------------------------------+
  |  Ship: Windows x64 player |     |  Farm/CI: Linux x64 headless |
  |  Unity presentation + C2  |     |  dotnet CLI / batch / tests  |
  +-------------+-------------+     +--------------+---------------+
                |                                  |
                v                                  v
        +-------+----------------------------------+-------+
        |  Host-agnostic managed core                         |
        |  ProjectAegis.Data · Sim · Delegation               |
        |  (no UnityEngine; no OS-dependent sim rules)        |
        +-----------------------------------------------------+
```

1. **No OS-conditioned sim rules.** `#if WINDOWS` / `#if LINUX` (or runtime OS checks) must not change engagement, policy, sensors, logistics, RNG, or order-log semantics.
2. **Presentation and packaging** may be OS-specific (Unity player APIs, installers, Steam depots, shell scripts vs PowerShell wrappers).
3. **Adapter layer** (`UnityAdapter`) may use Unity APIs; it must not redefine world truth.

### 5. Cross-OS determinism contract

These rules apply on every supported host:

| Concern | Rule |
|---------|------|
| Culture | Numeric and date formatting for goldens, fingerprints, Excel, and scenario JSON uses **invariant culture** (see ADR-011 for workbook path). |
| Paths | Use BCL path APIs (`Path.Combine`, `Path.GetFullPath`, etc.). No hardcoded `\` or `/` in core logic. |
| Line endings | Golden and report comparisons normalize or generate with stable conventions; do not fail solely on CRLF vs LF when content is otherwise equal (prefer generating LF in headless tools where practical). |
| Filesystem enumeration | Ordered, deterministic iteration (stable sort keys) — never rely on directory order. |
| Time | No wall-clock in sim paths; injectable clocks only (existing determinism law). |
| Floats in fingerprints | Prefer quantized / invariant-formatted values over raw `double.ToString()` host defaults. |
| Validation parity | Per ADR-008: same fixture → same `ValidationReport` semantics across Linux and Windows when both are exercised. |
| Replay hash | Production Baltic hash invariant unchanged; host OS is not a free variable that may diverge hash without a golden ADR. |

### 6. CI and verification placement

| Check | Primary host | Notes |
|-------|--------------|--------|
| `dotnet build` / `dotnet test` (solution floor) | Linux CI | Authoritative green gate |
| ReplayGolden / Baltic hash | Linux CI | Same binaries/logic as headless farm |
| PlayMode smoke (headless harness) | Linux CI | Per project filter; Unity Editor not required for the .NET harness |
| Optional Windows local full gate | Windows x64 | Developer convenience; not a second product matrix |
| Player packaging smoke | Windows x64 (when packaging epic runs) | Outside headless suite until store program scopes it |

### 7. Dev tooling note (not ship scope)

Agent hooks, bash scripts, and editor watcher limits may differ by OS (e.g. Linux `inotify`, Windows Git Bash for hooks). Those are **developer environment** concerns documented under engineering runbooks — they do **not** expand the ship matrix.

## Alternatives Considered

### Alternative 1: Full cross-platform player (Windows + Linux + macOS)

- **Description**: Ship interactive clients on all three desktop OSes.
- **Pros**: Larger addressable market; single community.
- **Cons**: Multiplies Unity certification, input, GPU, store, and QA cost; weak milsim Steam PC focus for v1.
- **Rejection Reason**: Explicitly out of product NFR for v1; does not change headless architecture benefit.

### Alternative 2: Linux as a second interactive Steam SKU in v1

- **Description**: Support Linux players alongside Windows.
- **Pros**: Overlap with headless Linux investment.
- **Cons**: Player graphics/input/store QA is not free; headless Linux ≠ desktop Linux player support.
- **Rejection Reason**: Product locks Linux to headless/batch only for v1.

### Alternative 3: ARM64-first or multi-arch CI now

- **Description**: Require arm64 (Graviton, Apple Silicon, Windows ARM) in the gate matrix.
- **Pros**: Future-proofing cloud cost / laptop diversity.
- **Cons**: Extra RID matrix, native dependency risk, float/packaging edge cases before commercial Windows x64 is solid.
- **Rejection Reason**: Premature; reopen only with product demand and a dedicated amendment.

### Alternative 4: Windows-only everything (no Linux)

- **Description**: CI and farm only on Windows.
- **Pros**: Matches player OS.
- **Cons**: Conflicts with existing Linux CI and container AvA economics; weaker agentic/cloud workflow.
- **Rejection Reason**: Headless Linux is already the primary CI and scale path; keeping it is load-bearing.

### Alternative 5: Defer ADR; keep NFR-only

- **Description**: Leave matrix only in req 01.
- **Pros**: Fewer documents.
- **Cons**: Packaging and code review lack architecture enforcement; agents re-litigate.
- **Rejection Reason**: Standing platform law belongs in the ADR set next to headless-first and determinism.

## Consequences

### Positive

- Single matrix for product, packaging, CI, and agent guidance.
- Preserves host-agnostic core and Linux-cheap headless scale.
- Stops silent scope creep into macOS / ARM / Deck without a formal decision.
- Gives release and store checklists a citeable architecture authority.

### Negative

- Linux desktop players and ARM users are unsupported in v1 (by design).
- Developers on unsupported hosts (e.g. macOS-only machines) have no official player path — use Linux/Windows x64 VMs or remote agents.
- Windows-only packaging skills remain on the critical path for Steam while day-to-day CI stays Linux.

### Neutral

- PowerShell vs bash tooling asymmetry remains an engineering convenience issue, not a sim architecture issue.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Locale/path drift breaks goldens on one OS | Medium | High | InvariantCulture; Path APIs; ADR-008/011 tests; prefer Linux CI as source of truth |
| `#if` or native deps enter core assemblies | Low | High | Code review + assembly boundary tests; GitNexus impact before native deps |
| Packaging epic assumes multi-OS without amendment | Medium | Medium | Release checklist cites this ADR; store work blocked on non-goals |
| Future ARM cloud agents cheaper / demanded | Medium | Medium | Amend ADR with explicit RID + golden re-verify plan; do not silently enable |
| Windows player-only bugs not caught on Linux CI | Medium | Medium | Presentation/UI bugs need Windows Editor or player smoke when packaging; sim/logic stays host-agnostic and covered headless |

## Performance Implications

| Area | Impact |
|------|--------|
| **CPU** | No change to tick budgets; reference hardware remains req 01 NFR. Linux AvA node remains the headless scale target. |
| **Memory** | No change. |
| **Load time** | Player packaging on Windows may have different asset pipeline times; not a sim determinism concern. |
| **Network** | N/A for offline single-player v1. |

## Migration Plan

No code migration required — this ADR **records and enforces** existing practice.

1. Cite this ADR from req 01 Platforms NFR and from packaging / release checklists when those epics run.
2. Reject PRs that add ship support for non-goal OSes/arch without amendment.
3. When Steam packaging starts: document StandaloneWindows64 (and depot layout) under engineering release docs, referencing this ADR — do not expand matrix in ad hoc scripts.

**Rollback plan**: Only via superseding ADR if product expands platforms; do not silently drop Linux CI (determinism gate depends on it).

## Validation Criteria

- [x] Product NFR platforms text matches this matrix (Windows x64 player; Linux x64 headless; no macOS/console/Deck).
- [x] Primary CI is Linux x64 (`docs/engineering/buildkite-ci.md`).
- [ ] Packaging / store checklist (when authored) cites ADR-018 and StandaloneWindows64 only for v1 player.
- [ ] No `ProjectAegis.Data` / `.Sim` / `.Delegation` production code gates behavior on `OperatingSystem` / `#if` for sim outcomes (spot-check / review gate).
- [ ] Validation and workbook paths retain invariant culture / portable path usage (ADR-008, ADR-011).
- [ ] Production Baltic hash and ReplayGolden remain green on Linux CI without OS-specific golden forks.

## GDD Requirements Addressed

| Document | System | Requirement | How this ADR satisfies it |
|----------|--------|-------------|---------------------------|
| [01-Project-Overview.md](../../Game-Requirements/requirements/01-Project-Overview.md) | Program NFR | Platforms: Windows 10/11 x64 primary (Steam); Linux x64 headless only; no macOS/console/Deck | Architecture enforcement of the decided matrix |
| [01-Project-Overview.md](../../Game-Requirements/requirements/01-Project-Overview.md) | Program NFR | Headless execution; reference hardware (PC + Linux AvA node) | Splits player vs farm hosts; keeps AvA on Linux x64 |
| [03-Simulation-Modes.md](../../Game-Requirements/requirements/03-Simulation-Modes.md) | Simulation modes | Headless / AvA performance floors | Headless host = Linux x64 farm/CI |
| ADR-010 / headless-first | Core | UI is not authority; core runnable without Unity | Affirms host split without second sim |
| ADR-008 | Mission validation | Cross Linux/Windows CI parity | Extended into full host determinism contract |

## Related

- [01-Project-Overview.md](../../Game-Requirements/requirements/01-Project-Overview.md) — NFR Platforms & reference hardware (product decision 2026-06-09)
- [ADR-001 Sim Assembly Boundary](adr-001-sim-assembly-boundary.md)
- [ADR-005 DOTS/ECS (superseded — managed headless-first)](adr-005-dots-sim-core.md)
- [ADR-006 Data Layer Boundary](adr-006-data-layer-boundary.md)
- [ADR-008 Mission Editor Validation Engine](adr-008-mission-editor-validation-engine.md)
- [ADR-010 Headless-First Command-Driven UI](adr-010-headless-first-command-driven-ui.md)
- [ADR-011 Platform Editor Excel Round-Trip](adr-011-platform-editor-excel-roundtrip.md)
- [buildkite-ci.md](../engineering/buildkite-ci.md)
- [local-dev-environment.md](../engineering/local-dev-environment.md)
- [unity VERSION.md](../engine-reference/unity/VERSION.md), [dotnet README](../engine-reference/dotnet/README.md)
