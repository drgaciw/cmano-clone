# Wave 4 — Corpus Gate (Consistency + RTM + Design-Review)

> **For agentic workers:** REQUIRED SUB-SKILLS: `dispatching-parallel-agents` (file-disjoint tracks) + `subagent-driven-development` + `using-git-worktrees`. Docs/reports only — **no production `.cs`**, no goldens, no `DelegationBridge`. Rely on existing honesty pins (hub FR, Wave3 docs).

**Goal:** Close epic story **005** — prove corpus **01–21** is consistent and reviewable with **0 BLOCKER** consistency report; refresh RTM/index; stamp program complete while **req 11 editor train remains active**.

**Architecture:** One story **005**, serial scaffold + **parallel W4 tracks** (file-disjoint) + serial closeout (consistency report + design-review + epic complete).

**Tech Stack:** Markdown requirements, architecture RTM, master indexes, implementation tracker, QA design-review memo. Existing tests only (no new adversarial suite this wave).

---

## Context

| Milestone | State on `main` |
|-----------|-----------------|
| Waves 0–3 | Complete (docs honesty 01–10, 12–21; story 004 Complete) |
| Adversarial W2/W3 pins | Merged (`f41df91` tip family) |
| Epic | `requirements-corpus-maturity` — story **005** Pending plan |
| Design | [`docs/superpowers/specs/2026-07-08-requirements-corpus-maturity-design.md`](docs/superpowers/specs/2026-07-08-requirements-corpus-maturity-design.md) §5 Wave 4 (W4-1…W4-6) |
| Prior consistency | [`docs/reports/requirements-consistency-2026-06-04.md`](docs/reports/requirements-consistency-2026-06-04.md) — **0 BLOCKER** but pre–W0–W3 honesty |

**Why Wave 4:** Honesty waves landed; gate artifacts (fresh consistency report, RTM/index stamp, full design-review, tracker program note) still required for epic exit.

### Pre-audit findings (parallel explore 2026-07-08)

| Severity | Finding | Wave 4 action |
|----------|---------|---------------|
| **BLOCKER candidate** | Doc **11** Related → `17-Replay-And-Order-Log.md` **404** (real: `17-Replay-AAR-And-Order-Log.md`) | **Mechanical fix** |
| **BLOCKER candidate** | Doc **11** mapping row schema/fixtures still **New** though artifacts exist | **New → Shipped/Partial+** |
| NOTE | Doc **11** lacks FR-09 reverse-ref | Add reverse-ref only |
| NOTE | Doc **11** AC phantom paths / unchecked ACs | Document residual; **no rewrite** |
| NOTE | Root `00-Master-Index.md` stale (345 tests, no doc 21, old tracker) | W4-6 catch-up |
| NOTE | RTM body still quotes historical **403/7** | Annotate historical; header floors already current |
| NOTE | RTM has maturity row 21 but no FR-19 TR evidence block | Add short platform-editor block |
| OK | GR `Game-Requirements-Index.md` already has corpus program note | Final “W0–W4 complete” stamp |
| OK | Research RTM 01/09/10 fine; 21 absent expected | **W4-3 skip** (user) |
| Allowed CONCERN | Commercial product name Open (C-03) | Keep as CONCERN only |

---

## Locked decisions (user 2026-07-08)

| Decision | Choice |
|----------|--------|
| Doc 11 | **Mechanical fix only** (link, FR-09, mapping status) — no AME rewrite |
| Packaging | **Design-complete W4-1…6 Standard** |
| Research RTM W4-3 | **Skip** (optional not required for exit) |
| Adversarial TDD | **Docs-only + existing pins** (no new test wave) |
| S56 MVP grades | **Freeze** — tracker additive notes only |
| Code | **Zero** production `.cs` / goldens / hash / DelegationBridge / CatalogWriteGate write paths |

---

## Superpowers + agent team map

```
Orchestrator
├── Preflight: worktree from main; confirm clean; no S54 types (sanity)
├── Scaffold: story-005 + wave4 plan + EPIC In progress
├── PARALLEL (file-disjoint)
│   ├── W4-a  Doc 11 mechanical + optional FR polish notes only in 11
│   ├── W4-b  Architecture RTM + architecture-traceability-index
│   ├── W4-c  Master indexes (GR index + 00-Master-Index)
│   └── W4-d  Implementation tracker program-close notes (01 next-stack; epic stamp)
└── SERIAL closeout
    ├── W4-1  Consistency report 0 BLOCKER
    ├── W4-4  Design-review memo (01 + Template A sample + Template B sample + 21)
    ├── Story 005 Complete + EPIC waves complete
    └── Verify + STOP for user commit/merge
```

| Track | Files owned | Parallel |
|-------|-------------|----------|
| W4-a | `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` only | Yes |
| W4-b | `docs/architecture/requirements-traceability.md`, `docs/architecture/architecture-traceability-index.md` | Yes |
| W4-c | `Game-Requirements/Game-Requirements-Index.md`, `00-Master-Index.md` | Yes |
| W4-d | `Game-Requirements/implementation-tracker-2026-07-04.md` (additive notes only) | Yes |
| Closeout | `docs/reports/requirements-consistency-2026-07-08.md` (or gate date), `production/qa/requirements-corpus-w4-design-review-….md`, story-005, EPIC | Serial |

**Conflict rule:** Only one track per file. Consistency report author must re-read W4-a…d results before declaring 0 BLOCKER.

### Skills / agents

| Role | Skill / agent |
|------|----------------|
| Orchestrator | writing-plans, dispatching-parallel-agents |
| Isolation | using-git-worktrees → `.worktrees/req-corpus-w4` branch `docs/req-corpus-maturity-w4` |
| Implementers | general-purpose docs writers (file-scoped) |
| Consistency | design-review / qa-tester style memo (W4-1 + W4-4) |
| Optional memory | Hindsight retain after closeout |

---

## Deliverables (W4-1…W4-6)

### W4-1 — Consistency report (exit criterion)

**Create:** `docs/reports/requirements-consistency-2026-07-08.md`  
**Clone structure from:** `docs/reports/requirements-consistency-2026-06-04.md`

Must include:

- Verdict: **0 BLOCKER**
- Scope: docs **01–21** after W0–W3 honesty + W4 mechanical fixes
- Check table: FR map hub, Related Index, Implementation Mapping coverage, re-honesty footers, link integrity, tracker 10b demotion, S56 grades frozen, doc 11 residual owned by editor train
- Close prior C-05 (req 21 now in hub + RTM) with evidence
- CONCERNS allowed: **C-03** commercial name; doc 11 AC residual; GDD STUB backlog; known UA 2 engage failures (if still true — note only)
- Explicit: broken doc 11→17 link **fixed in W4** (if W4-a done)

### W4-2 — Architecture RTM

**Modify:**

1. `docs/architecture/requirements-traceability.md`
   - Header: full baseline refresh **corpus W4 complete** (not only W0 hub note)
   - Keep gates ≥1232 / 6/6 / 18/18 / hash
   - Annotate Sprint 11–15 closeout **403/403 · 7/7** as **historical only**
   - Add short **Platform editor (req 21 / FR-19)** block: ADR-011, symbols (`IPlatformWorkbookIo`, `PlatformWorkbookExporter`/`Importer`/`Diff`, write-gate consumer), tests (Data.Excel / Platform* tests), status **PARTIAL**
   - Maturity table: note W1–W3 honesty dates where useful (light)

2. `docs/architecture/architecture-traceability-index.md`
   - Last Updated → wave date
   - Replace live **403/7** with current floors or “see RTM header”
   - Optional one-line platform editor / ADR-011 Partial

**Out:** `tr-registry.yaml` new TR-platform IDs (unless free and zero-risk).

### W4-3 — Research RTM

**Skip** per user. Optional one-line “req 21 not research-derived” deferred.

### W4-4 — Design-review memo

**Create:** `production/qa/requirements-corpus-w4-design-review-2026-07-08.md`

Sample set (fixed):

| Role | Doc |
|------|-----|
| Hub | **01** |
| Locked Template A | **04** Agent Delegation (core product pillar) |
| Template B | **14** Engagement (high overclaim risk historically) |
| Authoring | **21** Platform Editor |

Verdict: **APPROVED** or **APPROVED WITH NOTES** against honesty rubric (mapping, FR links, no inventing shipped DEW, editor residual owned by S81–S88).

### W4-5 — Tracker

**Modify:** `Game-Requirements/implementation-tracker-2026-07-04.md` only additive:

- Row **01** next-stack: change “corpus W1+ pending” → **corpus maturity W0–W4 complete 2026-07-08** (editor train still active)
- Optional footer/program note: Waves 0–4 complete; no MVP regrade
- Do **not** change S56 status cells except already-correct 10b Phase N

### W4-6 — Master indexes

1. `Game-Requirements/Game-Requirements-Index.md`  
   - Last Updated → gate date  
   - Program note: **Corpus maturity W0–W4 complete**; **scenario editor (req 11) remains active code train (S81–S88)**  
   - Next steps: remove pending wave language

2. `00-Master-Index.md`  
   - Last Updated → gate date  
   - Program note + redirect honesty to GR index  
   - Add **doc 21** (or Authoring pointer)  
   - Tracker → `implementation-tracker-2026-07-04.md`  
   - Replace **345 tests** with **≥1232** (+ optional smoke/hash one-liner)

### W4-a — Doc 11 mechanical (parallel track)

**File:** `11-Agentic-Mission-Editor.md` only

1. Related: `17-Replay-And-Order-Log.md` → `17-Replay-AAR-And-Order-Log.md`  
2. Header: **FR reverse-ref: FR-09** → hub 01  
3. Implementation Mapping: schema/fixtures row **New** → **Shipped** (or Partial+ with evidence path) if files exist  
4. Last Updated → wave date  
5. One-line residual: AC checkboxes / phantom `tests/unit/editor/` paths owned by **S81–S88** — not claiming green without evidence  
6. **Do not** mass-check ACs; **do not** rewrite AME-* design body

---

## Story 005 acceptance (testable)

1. Consistency report exists with verdict **0 BLOCKER**.  
2. Architecture RTM includes req **21 / FR-19** evidence block; historical 403/7 not presented as current gate.  
3. Design-review memo covers **01, 04, 14, 21** — APPROVED or WITH NOTES.  
4. Doc **11** Related link to doc 17 resolves; FR-09 present.  
5. GR index + root index stamped corpus complete; editor train still active.  
6. Tracker additive notes; S56 grades not re-litigated (10b stays Phase N).  
7. `git diff` has **no** `.cs` / goldens / DelegationBridge.  
8. Existing pins still relevant: `RequirementsHubContractTests`, Wave3 honesty contracts.  
9. Story 005 Complete; epic waves W0–W4 complete (epic may stay “Complete” or “Closed”).  
10. Research RTM change not required (skipped).

---

## Execution tasks (post-approval)

### Task 0: Isolation

- [ ] Worktree: `git worktree add .worktrees/req-corpus-w4 -b docs/req-corpus-maturity-w4 main`  
- [ ] Confirm main tip includes W3 + adversarial pins  
- [ ] All edits under worktree only  

### Task 1: Scaffold

- [ ] Create `production/epics/requirements-corpus-maturity/story-005-corpus-gate.md`  
- [ ] Update `EPIC.md` — Wave 4 in progress; story 005 In progress; plan link  
- [ ] Write `docs/superpowers/plans/2026-07-08-requirements-corpus-maturity-wave4-plan.md` (checkbox copy of this plan)  

### Task 2: Parallel tracks

- [ ] Dispatch **W4-a, W4-b, W4-c, W4-d** concurrently with file ownership above  
- [ ] Review diffs for file-disjointness  

### Task 3: Serial closeout

- [ ] Write consistency report (W4-1) after reading W4-a…d  
- [ ] Write design-review memo (W4-4)  
- [ ] Mark story 005 Complete; EPIC complete  
- [ ] Verification block (below)  
- [ ] **STOP** — commit/merge only on user instruction  

---

## Verification

```bash
# Doc count
ls Game-Requirements/requirements/*.md | wc -l   # 21

# Doc 11 link fixed
rg -n '17-Replay-AAR-And-Order-Log' Game-Requirements/requirements/11-Agentic-Mission-Editor.md
test ! -f Game-Requirements/requirements/17-Replay-And-Order-Log.md

# FR-09
rg -n 'FR-09|FR reverse-ref' Game-Requirements/requirements/11-Agentic-Mission-Editor.md

# Consistency + design-review exist
test -f docs/reports/requirements-consistency-2026-07-08.md
rg -n '0 BLOCKER' docs/reports/requirements-consistency-2026-07-08.md
test -f production/qa/requirements-corpus-w4-design-review-2026-07-08.md

# RTM floors + 21
rg -n '≥1232|17144800277401907079|FR-19|Platform Editor' docs/architecture/requirements-traceability.md

# Index stamp
rg -n 'W0–W4|corpus maturity complete|≥1232' \
  Game-Requirements/Game-Requirements-Index.md 00-Master-Index.md

# Docs-only
git diff --name-only | rg '\.cs$|tests/regression|DelegationBridge' || true

# Existing pins (optional smoke)
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~RequirementsHub|FullyQualifiedName~Wave3RequirementsHonesty" -v minimal
```

---

## Out of scope

| Item | Owner |
|------|--------|
| Full AME rewrite / AC mass-check | S81–S88 editor train |
| Research RTM expansion | Skipped W4-3 |
| New adversarial test suite | Separate if desired later |
| S56 MVP regrade | Never |
| Production code / goldens | Never |
| Doc 11 phantom test path inventory rewrite | Residual NOTE only |

---

## Success definition

Wave 4 exit: **0 BLOCKER** consistency report; RTM + indexes trustworthy at current floors; design-review APPROVED; story 005 Complete; epic W0–W4 closed; docs-only; editor code train explicitly still active.

---

## Next step after plan approval

1. Task 0 worktree  
2. Task 1 scaffold  
3. Task 2 fan-out W4-a ∥ W4-b ∥ W4-c ∥ W4-d  
4. Task 3 consistency + design-review + verify  
5. User-instructed commit / merge to `main`
