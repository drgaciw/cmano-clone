# Wave 3 — Content + Platform Editor Honesty (docs 09, 10, 21)

> **For agentic workers:** REQUIRED SUB-SKILLS: `dispatching-parallel-agents` (3 implementer tracks) + `subagent-driven-development` (task loop) + `using-git-worktrees` (isolated `docs/req-corpus-maturity-w3`). Optional later: `design-review` closeout. Docs-only — **no TDD red/green on product code**.

**Goal:** Make docs **09 / 10 / 21** implementation-honest against trunk (`main@355c917` or later docs tip) and correct tracker **10b** overclaim, without re-landing S54 code or reopening S56 MVP grades for the base rows.

**Architecture:** One story **004**, three **file-disjoint** parallel tracks (W3-a / W3-b / W3-c), serial scaffold + serial closeout. Same Standard honesty pattern as Waves 0–2.

**Tech Stack:** Markdown requirements corpus, implementation tracker, epic/story files, design-review QA memo. Zero `.cs` / goldens / `DelegationBridge`.

---

## Context

| Milestone | State |
|-----------|--------|
| Wave 0 hub | Complete on `main` |
| Wave 1 Template A (02–08, 12) | Complete |
| Wave 2 Template B (13–20) | Complete (`355c917`) |
| Epic | `production/epics/requirements-corpus-maturity/` — stories 001–003 Complete; **004 pending** |
| Design | [`docs/superpowers/specs/2026-07-08-requirements-corpus-maturity-design.md`](docs/superpowers/specs/2026-07-08-requirements-corpus-maturity-design.md) §5 Wave 3 |
| Adversarial W2 tests | **Parked parallel** on `.worktrees/adv-w2-tdd` (`test/adversarial-wave2-tdd`) — **do not block or mix into W3** |

**Why Wave 3:** Docs **09 / 10 / 21** lag Wave 0–2 honesty.

| Doc | Design posture | Tracker | Critical honesty issue |
|-----|----------------|---------|------------------------|
| **09** Near-Future | Research-integrated May 29 | **Partial** (empty evidence) | Headless spine **exists** but invisible; most § categories design-only |
| **10** Speculative | Research-integrated May 29 | **Partial+** “S54 orbital DEW + Kessler”; **10b Implemented** | **S54 DEW/Kessler/ladder types not on trunk** — only TL/`BLACK_PROJECT` gate + catalog metadata |
| **21** Platform Editor | Draft Jun 17; PLE-* unchecked; mapping “New” | **MVP-done / Partial+** | Code **far ahead** of doc; **no FR-19 reverse-link** |

**Not this wave:** Re-land S54 code; full DOTS NF spawn; new content systems; doc 11 AME rewrite; S56 MVP regrade of base grades; sim hotpath; ADR-011; hub Related Index rewrite; adversarial test merge.

---

## Locked decisions (user 2026-07-08)

| Decision | Choice |
|----------|--------|
| Honesty depth | **Standard** (W0–W2 pattern) |
| Packaging | **Story 004** + **3 parallel tracks** + design-review closeout |
| Tracker 10b | **Hard demote** status cell: `Implemented (S54)` → **`Phase N / not on main`** (fact fix, not S56 regrade). Row **10** stays **Partial+** with corrected Post-S56 evidence text |
| Packaging extras | **Docs 09/10/21 + tracker only** — no ADR-011, no hub Related Index churn |
| Adversarial W2 | **Parallel separate worktree** — leave uncommitted; never stage into W3 branch; merge each only on user instruction |

---

## Superpowers + agent team orchestration

### Skill chain

| Phase | Skill / team | Role |
|-------|--------------|------|
| Plan (this doc) | `writing-plans` + `dispatching-parallel-agents` | Orchestrator |
| Isolation | `using-git-worktrees` | Worktree `docs/req-corpus-maturity-w3` from clean `main` |
| Execute | `subagent-driven-development` | Fresh subagent per task; orchestrator reviews between tasks |
| Domain agents | `requirements-analyst` / explore (read-only preflight); implementer general-purpose for docs | Map claim→path; write honesty |
| Closeout QA | `design-review` skill + `qa-tester` style memo | `production/qa/requirements-corpus-w3-design-review-2026-07-08.md` |
| Memory (optional) | Hindsight `dev-cmano-clone` retain after closeout | Session outcome only — not in sim paths |
| Git | Project `git-commit` skill **only when user says commit** | Graphite-first if PR; direct merge only if user requests |

### Agent team map (Wave 3)

```
Orchestrator (this session)
├── Preflight (serial, read-only)
│   └── Confirm zero OrbitalDewPlatform|KesslerRiskMeter|EscalationTier in src/
│   └── Confirm main clean; adv-w2-tdd stays isolated
├── Scaffold (serial)
│   └── worktree + story-004 + wave3 checkbox plan + EPIC row
├── PARALLEL fan-out (file-disjoint)
│   ├── W3-a implementer → 09-Near-Future-Technologies.md only
│   ├── W3-b implementer → 10-Speculative-Systems.md + tracker 10/10b only
│   └── W3-c implementer → 21-Platform-Editor.md only
└── Closeout (serial)
    ├── design-review memo + story AC check
    ├── git diff docs-only gate
    └── STOP for user commit/merge instruction
```

**Conflict rule:** Only W3-b touches `implementation-tracker-2026-07-04.md`. No track edits another track’s primary doc.

### Parallel agent prompts (ready to dispatch post-approval)

**W3-a (09 only):**
```
Wave 3 Standard honesty for Game-Requirements/requirements/09-Near-Future-Technologies.md ONLY.
Worktree: <W3 path>. Pattern: Wave 1/2 (header FR reverse-ref, honesty overlay, Implementation Mapping Status|Evidence, light ACs, footer grade).
- Reverse-ref hub FR-08 (near-future half).
- Map SHIPPED spine only with paths: NearFutureArchetypeCatalog, data/catalog/near_future_archetypes.json (4), CatalogArchetypeGate, SwarmTierLimits, NearFutureArchetypeRuntime.PlanSpawns, MaxTechnologyLevel/nearFutureUnits schema, ScenarioNearFutureSpawnCommand, Baltic NF_SPAWN, HypersonicEngageGate, CatalogPlatformBinding.GameTechnologyLevel.
- Tag CCA/swarm runtime, AUV, full DEW thermal, quantum, JADC2, CEW, MASS, full DOTS spawn → Phase N.
- No PlatformTechnologyLevel type — use GameTechnologyLevel + archetype TL.
- ScenarioSpeculativeSettings → cross-link doc 10 only.
- Footer: Partial + Wave 3 re-honesty date.
- Do NOT edit tracker, 10, 21, or any .cs.
Return: summary + Mapping row count + FR-08 present yes/no.
```

**W3-b (10 + tracker — highest risk):**
```
Wave 3 Standard honesty for 10-Speculative-Systems.md + implementation-tracker rows 10 and 10b ONLY.
PRECONDITION: re-run `rg -n 'OrbitalDewPlatform|KesslerRiskMeter|EscalationTier' src/` — expect ZERO.
- Reverse-ref FR-08 (speculative half).
- Map SHIPPED: ScenarioSpeculativeSettings, SpeculativeEngageGate→MvpEngagementResolver, speculative_platforms.json metadata, baltic-patrol-black-project + speculative-gated policies, ScenarioSpeculativeGateTests.
- DEMOTE (not on trunk): OrbitalDewPlatform, KesslerRiskMeter, EscalationTier 5-tier runtime, first-fire ladder events → Phase N / not on main.
- Tracker HARD DEMOTE: row 10b status cell from "Implemented (S54)" → "Phase N / not on main".
- Tracker row 10: keep grade Partial+; rewrite evidence from "S54 orbital DEW + Kessler" to TL/black-project gate + catalog metadata honesty note Wave 3.
- Do NOT re-land S54 code. Do NOT change other tracker rows. Do NOT edit 09/21.
Return: rg exit evidence, before/after 10b cell, Mapping rows.
```

**W3-c (21 only):**
```
Wave 3 Standard honesty for 21-Platform-Editor.md ONLY.
- Header reverse-ref FR-19 + tracker MVP-done/Partial+ (S56) freeze.
- Rewrite Implementation Mapping: New → Shipped/Partial+ with evidence (IPlatformWorkbookIo, PlatformWorkbook export/import/diff, CatalogWriteGate Propose* consumer, Phase A/B catalog rows, platform_*_xlsx CLI/MCP, PlatformCatalogViewerHost / PlatformImportPanelHost Partial+).
- PLE ACs: check only with cited tests; Partial/deferred stay unchecked with notes.
- Residual live Editor screenshots → Phase N.
- Footer grade + Wave 3 re-honesty.
- Do NOT edit tracker, 09, 10, or .cs.
Return: Mapping row count, FR-19 present, residual Phase N list.
```

---

## Critical files

| Path | Action |
|------|--------|
| `Game-Requirements/requirements/09-Near-Future-Technologies.md` | W3-a: FR-08, honesty, mapping, Phase N tags |
| `Game-Requirements/requirements/10-Speculative-Systems.md` | W3-b: FR-08, demote S54 claims, map gate spine |
| `Game-Requirements/requirements/21-Platform-Editor.md` | W3-c: FR-19, rewrite mapping New→Shipped/Partial |
| `Game-Requirements/implementation-tracker-2026-07-04.md` | W3-b only: hard demote **10b**; honesty note **10** |
| `production/epics/requirements-corpus-maturity/story-004-content-platform.md` | Create (scaffold) |
| `production/epics/requirements-corpus-maturity/EPIC.md` | Add story 004 In progress → Complete at closeout |
| `docs/superpowers/plans/2026-07-08-requirements-corpus-maturity-wave3-plan.md` | Checkbox plan at execute (copy of this plan) |
| `production/qa/requirements-corpus-w3-design-review-2026-07-08.md` | Closeout APPROVED / WITH NOTES |

**Out:** ADR-011, hub Related Index rewrite, `.cs`, goldens, `DelegationBridge`, mixing `adv-w2-tdd`.

---

## Shared honesty template (all three docs)

```markdown
---
**Implementation grade:** <Partial|Partial+|MVP-done / Partial+> — see [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row NN.
Design Status: <Research-integrated|Draft>. Charter re-honesty: Wave 3 2026-07-08.
```

| Doc | Prefix | Hub FR | Tracker grade (frozen except 10b fact) |
|-----|--------|--------|------------------------------------------|
| 09 | `NF-*` major only | FR-08 | Partial |
| 10 | `SPEC-*` major only | FR-08 | Partial+ (evidence rewritten) |
| 21 | existing `PLE-*` | FR-19 | MVP-done / Partial+ (S56) |

---

## Track detail

### W3-a — Near-Future (09)

**Shipped spine (map with evidence):**

| Area | Path | Status |
|------|------|--------|
| Archetypes | `data/catalog/near_future_archetypes.json`, `NearFutureArchetypeCatalog` | Shipped (4 rows) |
| TL/swarm gates | `CatalogArchetypeGate`, `SwarmTierLimits` | Shipped |
| Spawn plan | `NearFutureArchetypeRuntime.PlanSpawns` | Shipped (plan only) |
| Scenario metadata | `MaxTechnologyLevel`, `nearFutureUnits` | Shipped schema |
| CLI | `ScenarioNearFutureSpawnCommand` | Shipped |
| Harness | Baltic `NF_SPAWN` | Shipped log/register |
| Hypersonic preview | `HypersonicEngageGate` | Shipped preview |
| Platform GTL | `CatalogPlatformBinding.GameTechnologyLevel` | Shipped catalog |

**Phase N:** CCA/swarm behaviors, AUV, full DEW thermal, quantum, JADC2, CEW, MASS runtime, full DOTS spawn. No Baltic near-future content pack.

### W3-b — Speculative (10) + tracker — highest factual risk

**Shipped:** `ScenarioSpeculativeSettings`, `SpeculativeEngageGate` → `MvpEngagementResolver`, `speculative_platforms.json` (metadata), black-project / speculative-gated policies, `ScenarioSpeculativeGateTests`.

**Not on main (demote):** `OrbitalDewPlatform`, `KesslerRiskMeter`, 5-tier `EscalationTier` runtime, first-fire ladder on session path.

**Tracker edits (W3-b exclusive):**

| Row | Status cell | Evidence / notes |
|-----|-------------|------------------|
| **10** | Keep **Partial+** | Replace “S54 orbital DEW + Kessler” with: Wave 3 honesty — TL/black-project engage gate + catalog metadata; S54 DEW/Kessler **not on main** |
| **10b** | **`Phase N / not on main`** (was Implemented S54) | Note: types absent from `src/` as of Wave 3 re-honesty |

### W3-c — Platform Editor (21)

**Header:** FR-19 + freeze MVP-done/Partial+ (S56).

**Mapping rewrite examples:**

| Area | Status | Evidence |
|------|--------|----------|
| `IPlatformWorkbookIo` + ClosedXML | Shipped | Data.Excel |
| Export / Import / Diff | Shipped | PlatformWorkbook* |
| Write-gate Propose* consumer | Shipped consumer | CatalogWriteGate batches |
| Catalog Phase A/B rows | Shipped schema | Mount, Loadout, Signature, … |
| CLI/MCP `platform_*_xlsx` | Shipped | Platform*XlsxCommand |
| Unity viewer + import panel | Partial+ | *Host types |
| Live Editor screenshots | Phase N residual | Tracker next stack |

---

## Story 004 acceptance (testable)

1. Docs **09, 10, 21** have Last Updated ≥ wave date + tracker grade footer.  
2. Each has Implementation Mapping (≥4 evidence rows).  
3. **09/10** reverse-ref **FR-08**; **21** reverse-refs **FR-19**.  
4. Doc **09** does not claim full tech matrix as shipped; spine mapped.  
5. Doc **10** does not claim orbital DEW/Kessler/ladder as on-main shipped.  
6. Doc **21** mapping not all “New”; FR-19 present.  
7. Tracker **10b** status = **Phase N / not on main** (or equivalent honest wording); row **10** evidence no longer claims S54 DEW/Kessler on trunk.  
8. `git diff` has **no** `.cs` / goldens / DelegationBridge; no files from `adv-w2-tdd`.  
9. S56 grades for 09 / 10 / 21 base rows not re-litigated (10b demotion is fact fix only).  
10. Design-review memo APPROVED or APPROVED WITH NOTES.

---

## Execution tasks (post-approval)

### Task 0: Preflight + isolation

- [ ] **Step 0.1:** Confirm `main` clean at repo root; note tip (expect `355c917` or later).
- [ ] **Step 0.2:** Confirm adversarial worktree separate: `git -C .worktrees/adv-w2-tdd status -sb` — do not pull those files into W3.
- [ ] **Step 0.3:** `rg -n 'class (OrbitalDewPlatform|KesslerRiskMeter)|enum EscalationTier' src/` → expect empty.
- [ ] **Step 0.4:** Create worktree via skill pattern:
  ```bash
  git worktree add .worktrees/req-corpus-w3 -b docs/req-corpus-maturity-w3 main
  ```
- [ ] **Step 0.5:** All subsequent edits only under that worktree.

### Task 1: Scaffold (serial)

- [ ] **Step 1.1:** Create `story-004-content-platform.md` (Goal, ACs 1–10 above, tracks W3-a/b/c, out of scope).
- [ ] **Step 1.2:** Update `EPIC.md` — status Wave 3 in progress; story 004 row.
- [ ] **Step 1.3:** Write `docs/superpowers/plans/2026-07-08-requirements-corpus-maturity-wave3-plan.md` (checkbox copy).

### Task 2: Parallel implementers

- [ ] **Step 2.1:** Dispatch **W3-a**, **W3-b**, **W3-c** concurrently with prompts above (`background: true` / parallel Task tools).
- [ ] **Step 2.2:** On return: verify file-disjoint diffs; re-run symbol `rg` if W3-b claims demotion.
- [ ] **Step 2.3:** Spot-check each doc for FR reverse-link + Mapping + footer.

### Task 3: Closeout (serial)

- [ ] **Step 3.1:** Write design-review memo; verdict APPROVED or APPROVED WITH NOTES.
- [ ] **Step 3.2:** Mark story 004 ACs; EPIC story 004 Complete when ACs pass.
- [ ] **Step 3.3:** Verification block (below) — RUN+READ.
- [ ] **Step 3.4:** **STOP** — present summary; commit/merge **only** on user instruction. Adversarial branch remains separate.

---

## Verification

```bash
# From W3 worktree
rg -n 'class (OrbitalDewPlatform|KesslerRiskMeter)|enum EscalationTier' src/ || true

rg -n 'Implementation Mapping|FR-08|FR-19|re-honesty: Wave 3' \
  Game-Requirements/requirements/09-Near-Future-Technologies.md \
  Game-Requirements/requirements/10-Speculative-Systems.md \
  Game-Requirements/requirements/21-Platform-Editor.md

rg -n '10b|Phase N / not on main|S54' Game-Requirements/implementation-tracker-2026-07-04.md

git diff --name-only
# Fail closeout if any:
git diff --name-only | rg '\.cs$|tests/regression|DelegationBridge' || true
```

Existing `RequirementsHubContractTests` already pins FR-01…19 existence (no new C# required).

---

## Out of scope / defer

| Item | Defer |
|------|--------|
| Re-merge S54 DEW/Kessler code | Separate code story |
| Commit/merge adversarial W2 tests | User instruction on `adv-w2-tdd` only |
| ADR-011 Context note | Explicitly out this wave |
| Hub Related Index rewrite | Out this wave |
| Full DOTS NF spawn / MASS | Tracker residual |
| Wave 4 corpus consistency gate | After W3 |
| Doc 11 AME residual | S81–S88 |

---

## Success definition

Wave 3 exit: docs **09, 10, 21** are research/design-reviewable and **implementation-honest** against trunk; **10b hard-demoted**; FR-08/FR-19 links complete; story 004 Complete; docs-only diff; adversarial worktree untouched by this wave.

---

## Next step after plan approval

1. Task 0 worktree + preflight  
2. Task 1 scaffold  
3. Task 2 fan-out W3-a ∥ W3-b ∥ W3-c  
4. Task 3 design-review + verify  
5. Await user: commit / merge W3 (and separately adversarial if desired)
