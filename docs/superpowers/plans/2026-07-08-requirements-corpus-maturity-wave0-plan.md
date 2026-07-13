# Requirements Corpus Maturity — Wave 0 (Hub Re-baseline) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `01-Project-Overview.md` a trustworthy corpus hub for Release-stage Project Aegis (FR-19/doc 21, scope/success honesty, standing invariants, Related Index 02–21) and align master index + light RTM/tracker notes — docs-only.

**Architecture:** Wave 0 of [2026-07-08-requirements-corpus-maturity-design.md](../specs/2026-07-08-requirements-corpus-maturity-design.md). Docs-only; no `.cs`, schema, or golden edits. S81–S88 scenario editor remains the code lead. Does **not** reopen completed epic `requirements-maturity-slice`.

**Tech Stack:** Markdown requirements corpus, implementation tracker, RTM, production QA design-review memo; bash verification (path/link checks). Collaborative protocol: path-level approval before Write if user requires it mid-execution.

**Spec:** [docs/superpowers/specs/2026-07-08-requirements-corpus-maturity-design.md](../specs/2026-07-08-requirements-corpus-maturity-design.md) (user-approved 2026-07-08)

**Non-goals:** Waves 1–4; rewriting Vision/Goals pillars; commercial rename; MVP regrade; doc 11 feature rewrite; any DelegationBridge / CatalogWriteGate / replay-hash changes.

---

## File map

| File | Action | Responsibility |
|------|--------|----------------|
| `production/epics/requirements-corpus-maturity/EPIC.md` | Create | New epic for W0–W4 |
| `production/epics/requirements-corpus-maturity/story-001-hub-rebaseline.md` | Create | Wave 0 story with testable ACs |
| `Game-Requirements/requirements/01-Project-Overview.md` | Modify | Hub charter re-baseline (primary deliverable) |
| `Game-Requirements/Game-Requirements-Index.md` | Modify | Program note, reading order includes 21, Last Updated |
| `Game-Requirements/implementation-tracker-2026-07-04.md` | Modify | Req 01 Post-S56 note only (additive) |
| `docs/architecture/requirements-traceability.md` | Modify | Scope + maturity row for doc 21; baseline note |
| `production/qa/requirements-corpus-w0-design-review-2026-07-08.md` | Create | Full-mode hub design-review memo |
| `docs/superpowers/specs/2026-07-08-requirements-corpus-maturity-design.md` | Modify | Status → Approved |

**Do not touch:** any `src/**/*.cs`, `tests/regression/**`, `data/scenarios/**` goldens, `DelegationBridge.cs`, doc 11 body (except if a relative link from 01 needs no change — 01 links to `11-Agentic-Mission-Editor.md` already).

---

### Task 1: Mark design spec Approved + scaffold epic/story

**Files:**
- Modify: `docs/superpowers/specs/2026-07-08-requirements-corpus-maturity-design.md` (header Status line)
- Create: `production/epics/requirements-corpus-maturity/EPIC.md`
- Create: `production/epics/requirements-corpus-maturity/story-001-hub-rebaseline.md`

- [ ] **Step 1: Update design spec status**

In `docs/superpowers/specs/2026-07-08-requirements-corpus-maturity-design.md`, change:

```markdown
**Status:** Draft — awaiting user review of written spec
```

to:

```markdown
**Status:** Approved — 2026-07-08 (user); Wave 0 plan ready
```

- [ ] **Step 2: Create epic file**

Write `production/epics/requirements-corpus-maturity/EPIC.md` with exactly:

```markdown
# Epic: Requirements Corpus Maturity (docs 01–21)

> **Status:** In progress (Wave 0)
> **Layer:** Documentation
> **Design:** [2026-07-08-requirements-corpus-maturity-design.md](../../../docs/superpowers/specs/2026-07-08-requirements-corpus-maturity-design.md)
> **Plan (Wave 0):** [2026-07-08-requirements-corpus-maturity-wave0-plan.md](../../../docs/superpowers/plans/2026-07-08-requirements-corpus-maturity-wave0-plan.md)
> **Does not reopen:** [requirements-maturity-slice](../requirements-maturity-slice/EPIC.md) (S12–15 Complete)

## Goal

Bring requirement docs **01–21** to trustworthy maturity (shipped vs phased honesty, hub/index/RTM consistency) without reopening locked product pillars or blocking S81–S88 scenario editor.

## Stories

| ID | Story | Wave | Status |
|----|-------|------|--------|
| 001 | [story-001-hub-rebaseline.md](story-001-hub-rebaseline.md) | 0 | Ready |
| 002 | Locked Template A honesty (02–08, 12) | 1 | Pending plan |
| 003 | Template B drafts (13–20) | 2 | Pending plan |
| 004 | Content + platform (09, 10, 21) | 3 | Pending plan |
| 005 | Consistency gate + RTM + design-review | 4 | Pending plan |

## Acceptance (epic)

1. [ ] Wave exits W0–W4 per design spec §5 / §10
2. [ ] Consistency report 0 BLOCKER at W4
3. [ ] S56 MVP grades unchanged (additive tracker notes only)
4. [ ] No DelegationBridge / production hash / CatalogWriteGate path edits from this epic
```

- [ ] **Step 3: Create story-001**

Write `production/epics/requirements-corpus-maturity/story-001-hub-rebaseline.md`:

```markdown
# Story 001 — Hub re-baseline (req 01 + index)

**Epic:** requirements-corpus-maturity  
**Wave:** 0  
**Status:** Ready  
**Type:** Documentation + traceability  
**Design:** docs/superpowers/specs/2026-07-08-requirements-corpus-maturity-design.md §5 Wave 0  
**Plan:** docs/superpowers/plans/2026-07-08-requirements-corpus-maturity-wave0-plan.md

## Context

S12 story-001 completed Template A for doc 01. By 2026-07 the hub is stale: missing doc 21, Related Index statuses frozen, success criteria over-claim vs Release/Baltic gates, no standing invariants, no scenario-editor forward program. This story re-baselines the hub only.

## Acceptance

1. `01-Project-Overview.md` has FR-19 → 21 and Related Index rows for docs **02–21** with resolving relative links.
2. Success criteria are split into gate-backed vs north-star; each original bullet tagged Measured | Proxy | Deferred with evidence path.
3. NFR or Technical Considerations includes standing invariants (test floor ≥1232, ReplayGolden/hash, C2 18/18, DelegationBridge zero-touch, CatalogWriteGate extend-only, v3 isolation).
4. Scope has explicit tiers: shipped vertical slice / active program (req 11) / ambition.
5. Future Extensibility leads with scenario editor program (req 11).
6. FR-09 wording is not “NL-only”; reflects schema/MCP/validation + optional NL as deferred.
7. Master index program note + reading order mention doc 21; Last Updated ≥ 2026-07-08.
8. Tracker req 01 Post-S56 note records re-baseline date without MVP regrade.
9. RTM scope notes doc 21 present in corpus; maturity table has row 21.
10. Design-review memo under `production/qa/` with full-mode hub checks (not lean 4-row only).
11. Verification script/commands in plan all PASS.
12. Collaborative protocol: no secret commits; user-instructed commits only.

## Verify commands

```bash
test -f Game-Requirements/requirements/21-Platform-Editor.md
rg -n 'FR-19' Game-Requirements/requirements/01-Project-Overview.md
rg -n '21-Platform-Editor' Game-Requirements/requirements/01-Project-Overview.md
rg -n '17144800277401907079' Game-Requirements/requirements/01-Project-Overview.md
# Related targets: see Task 10 verification loop
```

## Completion Notes

- **Completed:** (fill on close)
- **Per-AC evidence:** (path + section per AC)
- **Code Review:** N/A (docs) — design-review memo is the review artifact
```

- [ ] **Step 4: Verify scaffold exists**

Run:

```bash
test -f production/epics/requirements-corpus-maturity/EPIC.md && \
test -f production/epics/requirements-corpus-maturity/story-001-hub-rebaseline.md && \
rg -n 'Approved' docs/superpowers/specs/2026-07-08-requirements-corpus-maturity-design.md
```

Expected: both files exist; `Approved` appears in design spec header.

- [ ] **Step 5: Commit (only if user instructed)**

```bash
git add docs/superpowers/specs/2026-07-08-requirements-corpus-maturity-design.md \
  production/epics/requirements-corpus-maturity/EPIC.md \
  production/epics/requirements-corpus-maturity/story-001-hub-rebaseline.md
git commit -m "$(cat <<'EOF'
docs: approve corpus maturity design and scaffold Wave 0 epic

Story: requirements-corpus-maturity / 001
EOF
)"
```

If user has not instructed commits, **skip** and leave staged changes for later.

---

### Task 2: Doc 01 header + program snapshot + Purpose one-liner

**Files:**
- Modify: `Game-Requirements/requirements/01-Project-Overview.md` (lines 1–9 area)

- [ ] **Step 1: Replace header + Purpose**

Replace the top of the file through `## Purpose` block with:

```markdown
# 01 - Project Overview

**Last Updated:** 2026-07-08  
**Related:** [Game-Requirements-Index](../Game-Requirements-Index.md), [implementation tracker 2026-07-04](../implementation-tracker-2026-07-04.md), corpus docs 02–21 — see [Related Requirements Index](#related-requirements-index)  
**Status:** Locked vision — charter re-baselined 2026-07-08; commercial name still open; implementation grades live in the tracker  
**Research basis:** [Agentic CMO Research](../../docs/research/agentic-cmano-research.md)  
**Stage:** Release (`production/stage.txt`); S56 MVP closed; S73–S80 Baltic v3 content-complete; active forward program = scenario editor (req 11 / S81–S88)

## Purpose

Charter for product vision, scope tiers, NFR spine, and **corpus index** (FR map + Related docs). This file is not the live implementation grade sheet — use the [implementation tracker](../implementation-tracker-2026-07-04.md) for Partial / Partial+ / next-stack status.

## Program / stage snapshot

| Item | Value |
|------|--------|
| Production stage | **Release** (no Launch advance without explicit decision) |
| MVP (S56) | 21/21 rows MVP-done or documented Partial+ — **grades frozen** |
| Content (S57–S80) | Baltic v2 + v3 content programs complete; v3 isolated (`baltic-v3-*`) |
| Active engineering | **Scenario editor (req 11)** — headless-first; see roadmap `docs/reports/future-sprint-roadpmap-07042026.md` |
| Working title | Project Aegis (commercial name **Open**) |
```

Keep existing `## Vision` and `## Core Project Goals` **unchanged** after this insert (do not rewrite pillars).

- [ ] **Step 2: Spot-check Vision still present**

Run:

```bash
rg -n '^## Vision$|^## Core Project Goals$|^## Program / stage snapshot$' Game-Requirements/requirements/01-Project-Overview.md
```

Expected: all three headings present.

---

### Task 3: Scope Boundaries → three tiers

**Files:**
- Modify: `Game-Requirements/requirements/01-Project-Overview.md` section `## Scope Boundaries (First Release)`

- [ ] **Step 1: Replace Scope section**

Replace from `## Scope Boundaries (First Release)` through the line before `## Success Criteria` with:

```markdown
## Scope Boundaries

### A — Shipped vertical slice (Release through S80)

Evidence of the shippable product spine (not multi-theater parity):

- Baltic theater content (v2 production baseline + v3 isolated corpus)
- Headless .NET simulation + delegation (`ProjectAegis.Sim`, `ProjectAegis.Delegation`)
- Deterministic replay goldens and CI gates (see Standing invariants under NFR)
- C2 proxy / PlayMode smoke harness (headless-first UI contract, [ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md))
- Clean-room catalog/data layer with extend-only write gate ([06](06-Database-Intelligence.md))

### B — Active program (post-S80)

- **Scenario / mission editor (req 11)** — schema-backed scenario documents, validation engine, CLI/MCP authoring; Unity edit-mode Phase 2 per editor program plan
- Platform/catalog authoring (req 21) Excel round-trip on the write gate — Partial+; screenshots/live Editor polish remaining

### C — Product ambition (not equal-depth at vertical slice)

- Additional regional theaters (Black Sea, South China Sea, etc.) as **data extensibility**, not claimed equal content depth today
- Air, naval, and aggregate-level ground (brigade+) for air/naval interaction
- Drone swarms / autonomous systems depth beyond current OOB
- Full multi-theater 5,000+ entity interactive performance (north-star — see Success Criteria)

### Out of scope (later releases — unchanged pillars)

- Global/strategic-level campaigns (initially)
- All multiplayer — hotseat and async planned later; live networked play unscheduled
- Land warfare at battalion scale or below
```

- [ ] **Step 2: Confirm Out of scope pillars intact**

Run:

```bash
rg -n 'battalion scale|hotseat|Global/strategic' Game-Requirements/requirements/01-Project-Overview.md
```

Expected: all three phrases present under Out of scope.

---

### Task 4: Success Criteria honesty + overview ACs

**Files:**
- Modify: `Game-Requirements/requirements/01-Project-Overview.md` section `## Success Criteria`

- [ ] **Step 1: Replace Success Criteria section**

Replace from `## Success Criteria` through the process-goal italic line (inclusive) with:

```markdown
## Success Criteria

### Gate-backed (current Release bar)

These are the **operational** success criteria for the vertical slice. Evidence paths are CI / AGENTS.md gates:

| ID | Criterion | Tag | Evidence |
|----|-----------|-----|----------|
| OV-SC-G1 | Full solution tests meet monotonic floor **≥1232** with 0 gate failures (known UA exclusions documented in AGENTS.md) | **Measured** | `dotnet test ProjectAegis.sln`; AGENTS.md |
| OV-SC-G2 | Baltic v2 ReplayGolden **6/6**; production hash **`17144800277401907079`** preserved | **Measured** | `tests/regression/`; AGENTS.md hash grep |
| OV-SC-G3 | PlayModeSmokeHarness **18/18** | **Measured** | UnityAdapter filter `PlayModeSmokeHarnessTests` |
| OV-SC-G4 | Clean-room: no proprietary CMO DB/scenarios/code committed | **Proxy** | Process + review; Goal 5 |
| OV-SC-G5 | Capability map FR-01…FR-19 each has a primary requirement doc on disk | **Measured** | Related Index + `ls Game-Requirements/requirements/` |

### North-star (aspirational — not current gate)

Original charter bars retained as product north-stars. **Do not treat as S80 acceptance.**

| ID | Criterion | Tag | Evidence / owner |
|----|-----------|-----|------------------|
| OV-SC-N1 | Realistic 2030-era Baltic with **5,000+ entities** at **60+ FPS** Human Mode on recommended hardware; **≥256×** headless AvA effective speed | **Deferred** | Scale target; [03](03-Simulation-Modes.md); not the S80 content gate |
| OV-SC-N2 | Agent personality presets distinguishable in blind AAR of AvA batch runs; decisions rated plausible human command | **Deferred** | [04](04-Agent-Delegation.md), [17](17-Replay-AAR-And-Order-Log.md); needs AAR protocol |
| OV-SC-N3 | Headless API sufficient for external researcher batch study without engine code changes | **Proxy** | Partial via demo/CLI/tests; full researcher package → [03](03-Simulation-Modes.md), [07](07-Agentic-Infrastructure.md) |

*(Agentic development workflow quality is a process goal tracked in [07](07-Agentic-Infrastructure.md), not a product success criterion.)*

### Overview acceptance hooks (charter verification)

| ID | Hook | How to verify |
|----|------|---------------|
| OV-AC-1 | Gate-backed table present with G1–G5 | Section above |
| OV-AC-2 | North-star table present with N1–N3 tagged Deferred/Proxy | Section above |
| OV-AC-3 | Standing invariants listed under NFR | Task 6 |
| OV-AC-4 | Related Index 02–21 complete | Task 7 |
```

- [ ] **Step 2: Confirm tags present**

Run:

```bash
rg -n 'OV-SC-G1|OV-SC-N1|Deferred|Measured' Game-Requirements/requirements/01-Project-Overview.md | head -20
```

Expected: G1, N1, Measured, Deferred all hit.

---

### Task 5: FR table — FR-09 accuracy + FR-19

**Files:**
- Modify: `Game-Requirements/requirements/01-Project-Overview.md` Functional Requirements table

- [ ] **Step 1: Update FR-09 row and append FR-19**

Change FR-09 capability cell from:

```markdown
| FR-09 | Natural-language mission editor | [11](11-Agentic-Mission-Editor.md) |
```

to:

```markdown
| FR-09 | Scenario/mission editor (schema, validation, CLI/MCP; NL authoring Phase N) | [11](11-Agentic-Mission-Editor.md) |
```

Append after FR-18:

```markdown
| FR-19 | Platform/catalog editor (Excel write-gate round-trip) | [21](21-Platform-Editor.md) |
```

- [ ] **Step 2: Verify FR-19 file exists and row present**

Run:

```bash
test -f Game-Requirements/requirements/21-Platform-Editor.md
rg -n 'FR-19|FR-09' Game-Requirements/requirements/01-Project-Overview.md
```

Expected: file exists; FR-09 and FR-19 lines present; FR-09 does not say only “Natural-language”.

---

### Task 6: NFR standing invariants + Technical Considerations

**Files:**
- Modify: `Game-Requirements/requirements/01-Project-Overview.md` NFR + Technical sections

- [ ] **Step 1: After Agent safety bullet, add Standing invariants**

Immediately after the `**Agent safety:**` bullet under NFR, insert:

```markdown

### Standing engineering invariants (Release law)

These are load-bearing production constraints (see AGENTS.md). Charter-level; do not weaken without ADR + user decision:

| Invariant | Rule |
|-----------|------|
| Test floor | Solution tests **≥1232**, 0 gate failures (monotonic; known UA exclusions documented) |
| Replay hash | Production Baltic v2 hash **`17144800277401907079`** preserved unless golden ADR |
| ReplayGolden | **6/6** v2 suite green |
| C2 smoke | PlayModeSmokeHarness **18/18** |
| DelegationBridge | **Zero-touch** on hotpath through Release v1 |
| CatalogWriteGate | **Extend-only** write paths |
| Baltic v3 isolation | `baltic-v3-*` policies/goldens independent of v2 |

**Performance scale targets** (5k@60 FPS, ≥256× AvA) remain north-star under Success Criteria OV-SC-N1 — not the current CI gate.
```

- [ ] **Step 2: Expand Technical Considerations**

Replace the Technical Considerations bullets with:

```markdown
## Technical Considerations

- **Engine:** Unity 6.3 LTS presentation; simulation and delegation in headless .NET assemblies ([ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md))
- **Data:** `ProjectAegis.Data` SQLite catalog with provenance gates ([06](06-Database-Intelligence.md)); CatalogWriteGate extend-only
- **Bridge:** `DelegationBridge` / `UnitDetailBridge` — CRITICAL upstream; GitNexus impact required before edits; zero-touch hotpath policy
- **Authoring surface (active program):** scenario documents + Validation Engine + CLI/MCP ([11](11-Agentic-Mission-Editor.md)); platform Excel path ([21](21-Platform-Editor.md), [ADR-011](../../docs/architecture/adr-011-platform-editor-excel-roundtrip.md))
- **Verification:** `dotnet test`, PlayMode smoke harness, replay golden suite, hash grep — see Standing invariants
```

- [ ] **Step 3: Future Extensibility lead with editor**

Replace `## Future Extensibility` body with:

```markdown
## Future Extensibility

1. **Active:** Scenario editor program (req 11) — complete headless ACs, then Unity edit-mode Phase 2 ([roadmap 2026-07-04](../../docs/reports/future-sprint-roadpmap-07042026.md))
2. **Active / Partial+:** Platform editor (req 21) polish (live Editor evidence)
3. **Later:** Global/strategic campaigns and live multiplayer (out of first commercial release)
4. **Later:** External RL/co-simulation agents on the headless farm
5. **Data-extensible:** Additional theaters and classification tiers without engine fork
```

- [ ] **Step 4: Agentic Capabilities — clarify product vs process**

Replace Agentic Capabilities bullets with:

```markdown
## Agentic Capabilities

- **In-simulation (product):** personality-bound agents, autonomy tiers, order-log reviewability ([04](04-Agent-Delegation.md), [08](08-Agentic-Architecture.md)) — shipped Partial+ at MVP; C2 badge/UI polish remains
- **Authoring agents (product program):** MCP/CLI scenario tools + validation ([11](11-Agentic-Mission-Editor.md)); NL freeform authoring remains Phase N where reserved
- **Development (process):** Claude/Cursor + Superpowers skills + GitNexus/Hindsight per [07](07-Agentic-Infrastructure.md) — not a player-facing success criterion
```

- [ ] **Step 5: Confirm hash in file**

Run:

```bash
rg -n '17144800277401907079|Zero-touch|Future Extensibility' Game-Requirements/requirements/01-Project-Overview.md
```

Expected: hash present; Zero-touch present; Future Extensibility heading present.

---

### Task 7: Related Requirements Index 02–21

**Files:**
- Modify: `Game-Requirements/requirements/01-Project-Overview.md` Related Requirements Index

- [ ] **Step 1: Replace entire Related Requirements Index section**

Replace from `## Related Requirements Index` through end of file with:

```markdown
## Related Requirements Index

Design status = per-doc header. Implementation grades = [implementation tracker](../implementation-tracker-2026-07-04.md). This table is a navigation map, not the grade sheet.

| Doc | Title | Design status (header) | Notes |
|-----|-------|------------------------|-------|
| [02](02-Core-Gameplay-Loop.md) | Core Gameplay Loop | Locked | Tracker: Partial |
| [03](03-Simulation-Modes.md) | Simulation Modes | Locked | Tracker: Partial+ |
| [04](04-Agent-Delegation.md) | Agent Delegation | Locked | Tracker: Partial+ |
| [05](05-Dynamic-Systems-Agent.md) | Dynamic Systems Agent | Locked | Tracker: Partial+ |
| [06](06-Database-Intelligence.md) | Database Intelligence | Locked | Tracker: Partial |
| [07](07-Agentic-Infrastructure.md) | Agentic Infrastructure | Locked | Tracker: Partial |
| [08](08-Agentic-Architecture.md) | Agentic Architecture | Locked | Tracker: Partial |
| [09](09-Near-Future-Technologies.md) | Near-Future Technologies | Research-integrated | Tracker: Partial |
| [10](10-Speculative-Systems.md) | Speculative Systems | Research-integrated | Tracker: Partial+ |
| [11](11-Agentic-Mission-Editor.md) | Agentic Mission Editor | **Revised** (2026-07-01) | Active program S81–S88; `AME-*` |
| [12](12-Terms-Glossary.md) | Terms Glossary | Locked | Tracker: Partial |
| [13](13-Doctrine-ROE-EMCON-WRA.md) | Doctrine / ROE / EMCON / WRA | Draft | Tracker: Partial |
| [14](14-Engagement-And-Fire-Control.md) | Engagement & Fire Control | Draft | Tracker: Partial+ |
| [15](15-Sensor-Detection-And-EW.md) | Sensor, Detection & EW | Draft | Tracker: Partial (MVP COVERED) |
| [16](16-Logistics-And-Magazines.md) | Logistics & Magazines | Draft | Tracker: Partial |
| [17](17-Replay-AAR-And-Order-Log.md) | Replay, AAR & Order Log | Draft | Tracker: Partial |
| [18](18-Combat-Domains.md) | Combat Domains | Draft | Tracker: Partial+ |
| [19](19-Cyber-And-Comms.md) | Cyber & Comms | Draft | Tracker: Partial |
| [20](20-Command-And-Control-UI.md) | Command & Control UI | Draft | Tracker: Partial |
| [21](21-Platform-Editor.md) | Platform Editor | Draft | Tracker: MVP-done / Partial+; FR-19 |

Status snapshot **2026-07-08** (Wave 0 hub re-baseline). Per-doc headers remain authoritative for design Status; tracker for implementation.
```

- [ ] **Step 2: Link resolution loop**

Run:

```bash
cd Game-Requirements/requirements
while read -r f; do
  test -f "$f" || echo "MISSING $f"
done <<'EOF'
02-Core-Gameplay-Loop.md
03-Simulation-Modes.md
04-Agent-Delegation.md
05-Dynamic-Systems-Agent.md
06-Database-Intelligence.md
07-Agentic-Infrastructure.md
08-Agentic-Architecture.md
09-Near-Future-Technologies.md
10-Speculative-Systems.md
11-Agentic-Mission-Editor.md
12-Terms-Glossary.md
13-Doctrine-ROE-EMCON-WRA.md
14-Engagement-And-Fire-Control.md
15-Sensor-Detection-And-EW.md
16-Logistics-And-Magazines.md
17-Replay-AAR-And-Order-Log.md
18-Combat-Domains.md
19-Cyber-And-Comms.md
20-Command-And-Control-UI.md
21-Platform-Editor.md
EOF
```

Expected: **no** `MISSING` lines.

- [ ] **Step 3: Count Related rows**

Run:

```bash
rg -c '^\| \[' Game-Requirements/requirements/01-Project-Overview.md
# Or count 21-Platform row:
rg -n '21-Platform-Editor' Game-Requirements/requirements/01-Project-Overview.md
```

Expected: `21-Platform-Editor` appears in Related Index (and FR-19).

---

### Task 8: Master index hygiene

**Files:**
- Modify: `Game-Requirements/Game-Requirements-Index.md`

- [ ] **Step 1: Update header program note**

Replace lines 1–5 with:

```markdown
# Game Requirements - Master Index

**Last Updated:** 2026-07-08

**Program note:** Post-MVP Requirements Program (Sprints 11–15) complete for docs 01–12 Template A. **Corpus maturity program (2026-07)** re-baselines hub + drafts 13–21 honesty — design: `docs/superpowers/specs/2026-07-08-requirements-corpus-maturity-design.md`. Scenario editor (req 11) is the **active code train** (S81–S88). Doc 11 revised 2026-07-01 (`AME-*`). Doc 21 Platform Editor is first-class under Authoring.
```

- [ ] **Step 2: Update reading order to include 21**

Replace the Reading order section with:

```markdown
## Reading order (for design review)

1. **12** Glossary → **13** Policy → **15** Sensors → **14** Engagement  
2. **16** Logistics → **18** Combat → **19** Cyber  
3. **20** UI → **17** Replay (ties systems to player evidence)  
4. **11** Mission editor (authoring) with **06** Database  
5. **21** Platform editor (catalog) with **06** Database — after or parallel to 11  
6. **01** Project Overview last as hub re-check (FR map + NFR)
```

- [ ] **Step 3: Refresh Next workflow steps**

Replace Next workflow steps with:

```markdown
## Next workflow steps

- Execute scenario editor rows (req 11) per [implementation-tracker-2026-07-04.md](implementation-tracker-2026-07-04.md) and S81–S88 roadmap
- Run corpus maturity Waves 1–4 per design spec (locked Template A honesty → drafts 13–20 → 09/10/21 → consistency gate)
- Run `/design-review` when mechanics change on **01, 04, 06–11, 13–21**
- Run `/military-requirements-impact` before DB schema for JADC2, C-UAS, hypersonic defense
```

- [ ] **Step 4: Verify index date**

Run:

```bash
rg -n 'Last Updated|21 Platform|corpus maturity' Game-Requirements/Game-Requirements-Index.md | head -15
```

Expected: 2026-07-08; mentions of 21 and corpus maturity.

---

### Task 9: Tracker + RTM light updates

**Files:**
- Modify: `Game-Requirements/implementation-tracker-2026-07-04.md` (req 01 row only)
- Modify: `docs/architecture/requirements-traceability.md` (header scope + maturity table)

- [ ] **Step 1: Tracker req 01 Post-S56 note**

Change the req 01 table row Post-S56 cell from:

```markdown
| 01 | Project Overview | **MVP-done (S56)** | S72 commercial launch prep complete | (complete @ S56) |
```

to:

```markdown
| 01 | Project Overview | **MVP-done (S56)** | S72 commercial launch prep complete; **doc charter re-baseline 2026-07-08** (hub FR-19/index/invariants — corpus maturity W0; MVP grade unchanged) | (complete @ S56); corpus W1+ pending |
```

Do **not** change the MVP status column.

- [ ] **Step 2: RTM header scope**

In `docs/architecture/requirements-traceability.md`, replace the scope blockquote lines:

```markdown
> **Last updated:** 2026-06-08
> **Scope:** Requirements docs **01–12** (maturity program) + MVP slice **13–20** (GDD → ADR → code → test)  
> **Coverage:** Headless chain GDD → ADR → code → test (see [architecture-review-2026-06-02.md](architecture-review-2026-06-02.md))
```

with:

```markdown
> **Last updated:** 2026-07-08 (Wave 0 hub note; full baseline refresh at corpus W4)
> **Scope:** Requirements docs **01–21** on disk (hub + Template A 01–12 + MVP slice 13–20 + **21 Platform Editor**); implementation grades in Game-Requirements tracker  
> **Coverage:** Headless chain GDD → ADR → code → test (see [architecture-review-2026-06-02.md](architecture-review-2026-06-02.md))  
> **Gates (current):** solution tests ≥1232; ReplayGolden 6/6; PlayModeSmoke 18/18; hash `17144800277401907079` — supersedes historical 403/7 baselines in older closeout notes below
```

- [ ] **Step 3: Add maturity row for doc 21**

In the “Requirements maturity (docs 01–12)” table, after the last data row (before any closing notes), ensure doc 01 notes re-baseline and add:

```markdown
| 21 | Platform Editor | **PARTIAL** (doc Draft; impl Partial+) | [ADR-011](adr-011-platform-editor-excel-roundtrip.md); hub FR-19 as of 2026-07-08 | — |
```

Also update doc 01 row Notes cell to mention hub re-baseline 2026-07-08 if a Notes column exists; if the table only has “Locked spec / notes”, append: `hub re-baseline 2026-07-08`.

Rename section heading if needed for honesty:

```markdown
## Requirements maturity (docs 01–12 + 21 note) — Sprint 11–15 + corpus W0
```

- [ ] **Step 4: Verify tracker/RTM**

Run:

```bash
rg -n 're-baseline 2026-07-08|Platform Editor|01–21' \
  Game-Requirements/implementation-tracker-2026-07-04.md \
  docs/architecture/requirements-traceability.md
```

Expected: re-baseline on tracker 01; Platform Editor / 01–21 on RTM.

---

### Task 10: Design-review memo + full verification

**Files:**
- Create: `production/qa/requirements-corpus-w0-design-review-2026-07-08.md`

- [ ] **Step 1: Write design-review memo**

```markdown
# Requirements Corpus Maturity — Wave 0 Design Review (Hub)

**Date:** 2026-07-08  
**Reviewer:** (agent or human)  
**Scope:** `Game-Requirements/requirements/01-Project-Overview.md` + index/tracker/RTM W0 touchpoints  
**Verdict:** APPROVED | APPROVED WITH NOTES | REVISIONS REQUIRED

## Checks

| Check | Result | Notes |
|-------|--------|-------|
| Template A sections retained | | Purpose, Vision, Goals, Scope, Success, FR, NFR, Technical, Agentic, Future, Open Q, Related |
| Vision / Goals pillars not reopened | | |
| Scope tiers A/B/C present | | |
| Success criteria gate vs north-star | | OV-SC-G* / OV-SC-N* |
| Standing invariants + hash | | `17144800277401907079` |
| FR-09 not NL-only | | |
| FR-19 → 21 | | |
| Related Index 02–21 links resolve | | Task 7 loop |
| Commercial name still Open | | |
| Tracker MVP grade unchanged | | |
| No src/golden edits in W0 | | `git diff --stat` |

## Follow-ups (non-blocking unless marked)

- Wave 1: locked Template A honesty (02–08, 12)
- Wave 2: drafts 13–20
- Full consistency report at Wave 4

## Sign-off

- Reviewer:  
- Date:  
```

Fill Result column with PASS after running Step 2.

- [ ] **Step 2: Run verification battery**

```bash
# 1. File counts
ls Game-Requirements/requirements/*.md | wc -l
# expect 21

# 2. Hub greps
rg -n 'FR-19|OV-SC-G1|OV-SC-N1|17144800277401907079|Revised' Game-Requirements/requirements/01-Project-Overview.md

# 3. Related targets
cd Game-Requirements/requirements && for f in \
  02-Core-Gameplay-Loop.md 03-Simulation-Modes.md 04-Agent-Delegation.md \
  05-Dynamic-Systems-Agent.md 06-Database-Intelligence.md 07-Agentic-Infrastructure.md \
  08-Agentic-Architecture.md 09-Near-Future-Technologies.md 10-Speculative-Systems.md \
  11-Agentic-Mission-Editor.md 12-Terms-Glossary.md 13-Doctrine-ROE-EMCON-WRA.md \
  14-Engagement-And-Fire-Control.md 15-Sensor-Detection-And-EW.md 16-Logistics-And-Magazines.md \
  17-Replay-AAR-And-Order-Log.md 18-Combat-Domains.md 19-Cyber-And-Comms.md \
  20-Command-And-Control-UI.md 21-Platform-Editor.md; do
  test -f "$f" || echo "MISSING $f"
done

# 4. No code dirty from this work (expect only docs paths)
git status --short

# 5. Optional: ensure no DelegationBridge.cs in diff
git diff --name-only | rg 'DelegationBridge|\.cs$' || true
# expect empty for .cs
```

Expected: 21 files; greps hit; no MISSING; git status docs-only; no `.cs` in diff.

- [ ] **Step 3: Mark story Complete**

Update `production/epics/requirements-corpus-maturity/story-001-hub-rebaseline.md`:

- Status → Complete  
- Completion Notes with date and per-AC evidence paths  
- Epic story table Status → Complete for 001  

- [ ] **Step 4: Commit Wave 0 (only if user instructed)**

```bash
git add \
  Game-Requirements/requirements/01-Project-Overview.md \
  Game-Requirements/Game-Requirements-Index.md \
  Game-Requirements/implementation-tracker-2026-07-04.md \
  docs/architecture/requirements-traceability.md \
  production/qa/requirements-corpus-w0-design-review-2026-07-08.md \
  production/epics/requirements-corpus-maturity/story-001-hub-rebaseline.md \
  production/epics/requirements-corpus-maturity/EPIC.md \
  docs/superpowers/specs/2026-07-08-requirements-corpus-maturity-design.md \
  docs/superpowers/plans/2026-07-08-requirements-corpus-maturity-wave0-plan.md
git commit -m "$(cat <<'EOF'
docs: Wave 0 hub re-baseline for requirements corpus maturity

Re-baseline req 01 (FR-19, index 02-21, gate vs north-star success criteria,
standing invariants). Align master index, tracker note, RTM scope. Epic
requirements-corpus-maturity story 001.

EOF
)"
```

---

## Spec coverage checklist (self-review)

| Spec W0 deliverable | Task |
|---------------------|------|
| W0-1 FR-19 | Task 5 |
| W0-2 Related Index 02–21 | Task 7 |
| W0-3 Status refresh (11 Revised) | Task 7 |
| W0-4 Scope tiers | Task 3 |
| W0-5 Success criteria tags | Task 4 |
| W0-6 Standing invariants | Task 6 |
| W0-7 Future Extensibility editor-first | Task 6 |
| W0-8 Header / commercial Open | Task 2 |
| W0-9 Modern story file | Task 1 |
| Master index | Task 8 |
| Tracker additive note | Task 9 |
| RTM C-05 / doc 21 | Task 9 |
| Design-review full mode | Task 10 |
| No vision pillar rewrite | Tasks 2–3 keep Vision/Goals |
| Docs-only | File map + Task 10 verify |

**Placeholder scan:** none intentional.  
**Commit steps:** gated on explicit user instruction (project collaborative protocol).

---

## Execution handoff

Plan complete and saved to `docs/superpowers/plans/2026-07-08-requirements-corpus-maturity-wave0-plan.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** — fresh subagent per task, review between tasks  
2. **Inline Execution** — execute tasks in this session with checkpoints  

**Which approach?**
