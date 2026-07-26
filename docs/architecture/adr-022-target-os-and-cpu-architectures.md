# ADR-022: Target Operating Systems and CPU Architectures (v1)

## Status

**Accepted** (2026-07-25)

> **Renumbered 2026-07-26.** Issued as ADR-018. That number was already taken by
> [adr-018-sensor-side-picture-datalink.md](adr-018-sensor-side-picture-datalink.md), committed
> `2abea6f` on 2026-07-25. Resolved per this repo's own rule — *git is source of truth; Linear and
> Notion are mirrors* — so the ADR present in git kept 018 and this one moved to **022**.
> Tracked as Linear DRG-51.

## Date

2026-07-25

## Last Verified

2026-07-26 (product NFR text confirmed at `01-Project-Overview.md:166`; all five cited ADR filenames verified present)

## Decision Makers

Product platform decision 2026-06-09 (`Game-Requirements/requirements/01-Project-Overview.md`); architecture codification 2026-07-25

## Summary

Project Aegis v1 ships an **interactive client on Windows 10/11 x64** (Steam primary) and runs
**headless sim, batch, and CI on Linux x64**. The managed core — `ProjectAegis.Data`,
`ProjectAegis.Sim`, `ProjectAegis.Delegation` — is **host-agnostic**. **x64 only** for v1.

This ADR codifies existing practice; it introduces no new capability. Its purpose is to make the
supported matrix, the non-goals, and the cross-OS determinism rules enforceable rather than assumed.

## Engine Compatibility

| Field | Value |
|-------|-------|
| Engine | Unity 6.3 LTS (6000.3.14f1) + .NET 8 |
| Player target | **StandaloneWindows64** only for the v1 commercial path |
| Headless RID | **linux-x64** farm; optional **win-x64** for local tooling |
| Risk | **LOW** — documents the shipped matrix; no code change |

## ADR Dependencies

| Relationship | ADR / artifact |
|--------------|----------------|
| **Depends on** | [ADR-001](adr-001-sim-assembly-boundary.md) (engine-free Sim), [ADR-006](adr-006-data-layer-boundary.md) (engine-free Data) |
| **Constrains** | [ADR-008](adr-008-mission-editor-validation-engine.md) (cross-host validation parity), [ADR-011](adr-011-platform-editor-excel-roundtrip.md) (InvariantCulture round-trip) |
| **Related** | [ADR-010](adr-010-headless-first-command-driven-ui.md) (headless-first UI) |
| **Product source** | `Game-Requirements/requirements/01-Project-Overview.md` §NFR (2026-06-09) |

## Supported host matrix

| Role | OS | Arch |
|---|---|---|
| Interactive player (Steam) | Windows 10/11 | x64 |
| Headless farm / AvA / CI | Linux | x64 |
| Local dev (optional) | Windows or Linux | x64 |

Matches the product NFR verbatim: *"Windows 10/11 x64 (primary, Steam); Linux x64 for the headless
server/batch farm only. No macOS, console, or Steam Deck support in v1."*

## Decision

### 1. Ship matrix is as above; x64 only for v1

### 2. The managed core is host-agnostic

**No OS-conditioned simulation rules** in `Data`, `Sim`, or `Delegation`. `#if WINDOWS` / `#if LINUX`
and runtime OS checks must never change engagement, policy, sensor, logistics, RNG, or order-log
semantics. Presentation and packaging may be OS-specific; the adapter layer may use Unity APIs but
must not redefine world truth.

### 3. Cross-OS determinism is architecture law

| Rule | Requirement |
|---|---|
| Culture | `InvariantCulture` for goldens, fingerprints, Excel, scenario JSON (ADR-011) |
| Paths | BCL path APIs only — no hardcoded `\` or `/` in core logic |
| Filesystem | Ordered, deterministic enumeration; never rely on directory order |
| Time | No wall-clock in sim paths; injectable clocks only |
| Floats | Quantized in fingerprints (`FingerprintFloat`) |
| Validation | ADR-008 parity — same fixture, same `ValidationReport` semantics on either host |
| Replay | Baltic hash `17144800277401907079` is **host-independent**; an OS must not diverge it without a golden ADR |

### 4. Authoritative CI host is Linux x64 (Buildkite)

### 5. Dev-tooling OS quirks are not ship-matrix expansions

Running the Editor or a script on another OS locally does not add that OS to the supported matrix.

### 6. Reopening a non-goal requires an ADR amendment plus product re-decision

## Explicit non-goals (v1)

- macOS player or headless gate
- ARM64 (Windows, Linux, or Apple Silicon) as ship target or required CI
- Consoles, Steam Deck
- Linux **interactive** Steam player — Linux is headless/batch only
- Multi-arch fat binaries / universal builds

## Alternatives Considered

| Option | Why rejected for v1 |
|--------|---------------------|
| Add macOS player | No product demand recorded in the NFR; doubles the packaging and cert surface |
| Add ARM64 as a required CI host | No ship target needs it; would double gate cost for a matrix nobody ships |
| Linux interactive player | Presentation stack is validated only on StandaloneWindows64; Linux value is the headless farm |
| Leave the matrix implicit | The status quo before this ADR — the constraint was real but unenforceable, and nothing prevented an OS-conditioned sim rule from landing |

## Consequences

### Positive

- The supported matrix is now checkable rather than folkloric
- The host-agnostic rule gives reviewers explicit grounds to reject an OS-conditioned sim change
- Cross-OS determinism rules are collected in one place instead of spread across ADR-008 and ADR-011

### Negative

- Any future platform expansion needs an ADR amendment, adding process weight to a product decision
- Windows-only player packaging means the Linux-authoritative CI never exercises the shipped player binary

## Validation Criteria

- [x] Product NFR platform text matches this matrix — `01-Project-Overview.md:166`
- [x] Authoritative CI host is Linux x64 — `.buildkite/pipeline.yml`, `docs/engineering/buildkite-ci.md`
- [x] Cited ADR dependencies all resolve — ADR-001, 006, 008, 010, 011 verified present
- [x] **Decision 2 verified clean** — spot-check run 2026-07-26 across all **489** `.cs` files in `Data`, `Sim`, and `Delegation`: **zero** OS preprocessor directives (`#if WINDOWS|LINUX|OSX|…`), **zero** runtime OS checks (`RuntimeInformation.IsOSPlatform`, `OperatingSystem.Is*`, `Environment.OSVersion`). Wall-clock reads and unseeded RNG also zero. Hardcoded separators: none harmful — repo-relative `/` constants resolved through BCL path APIs
- [ ] **OPEN:** packaging checklist cites ADR-022 + StandaloneWindows64 when the store epic runs (Linear DRG-39)
- [ ] **OPEN (Linear DRG-54):** one **Decision 3** exception found by the same spot-check — `ScenarioPolicyJsonIndex.cs:26` and `ScenarioPolicyJsonLoader.cs:32` call `Directory.EnumerateFiles` unordered and then `map[dto.Id] = dto`, so a duplicate `Id` resolves last-writer-wins and **the survivor differs between Windows and Linux**. Latent today (76 policy files, zero duplicate ids), but neither site guards the overwrite

## Migration Plan

1. Commit this ADR and add it to the traceability index ADR inventory — done with this change.
2. Run the open spot-check above; if any OS-gated sim rule exists, it is a defect under Decision 2.
3. Cite ADR-022 in the packaging checklist when the store epic (Launch criterion 2, Linear DRG-39) runs.
