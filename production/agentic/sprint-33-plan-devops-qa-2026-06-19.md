# Sprint 33 — DevOps / QA Track Plan

**Date:** 2026-06-19  
**Kickoff:** `production/sprints/sprint-33-kill-chain-intelligence-comms-integration.md`  
**Trunk:** `main` @ `d3db76db` — **1073/1073**; ReplayGolden **6/6** (S32 APPROVED)  
**QA plan (authoritative):** `production/qa/qa-plan-sprint-33-2026-11-27.md`  
**Review mode:** **Lean** — Buildkite = merge authority; headless proxy = merge authority for Unity; GHA advisory (billing)

## Goal

Gate Sprint 33 on **≥1073** day-1 floor, **≥1086** closeout, ReplayGolden 6/6, fresh GitNexus @ trunk, C2 Check 17 upgrade, and `/team-qa sprint` sign-off after S33-13. **No S32 carryover in QA scope** — S32 closed 13/13; S33-09 is Path A (Phase 6 smoke) only.

## DevOps / QA story map

| ID | Owner | Gate | Deliverable |
|----|-------|------|-------------|
| **S33-01** | c-sharp-devops-engineer | **Day-1 blocker** | `smoke-sprint-33-baseline-*.md`; `tests_passed_sprint33_baseline` ≥1073; GitNexus @ HEAD |
| S33-02..10 | track owners | Per merge | See QA plan — filtered `dotnet test`, `/replay-verify` on sim merges |
| **S33-11** | team-qa | Wave 5 | C2 Check 17 + refresh 14–16; `sprint-33-c2-signoff-*.md` |
| **S33-12** | c-sharp-devops-engineer | Nice-to-have | `sprint-33-ci-hygiene-*.md` — S32-12 pattern; **non-blocking** |
| **S33-13** | c-sharp-devops-engineer | **Closeout** | `smoke-sprint-33-closeout-*.md` ≥1086; `sprint-33-gitnexus-*.md`; prune `stack/sprint32/*` |
| Sign-off | team-qa | Post S33-13 | `qa-signoff-sprint-33-*.md` — run `/team-qa sprint` |

---

## S33-01 — Day-1 baseline (must-have)

**Blocks S33-02+** until green. Confirms S32 closeout trunk is healthy before kill-chain / comms work lands.

| Gate | Expected |
|------|----------|
| `dotnet build ProjectAegis.sln` | 0 errors |
| `dotnet test ProjectAegis.sln` | **≥1073/1073** PASS |
| `ReplayGoldenSuiteTests` | **6/6** PASS |
| `DelegationBridge.cs` diff | **ZERO touch** |
| Production Baltic world hash | `17144800277401907079` unchanged |
| GitNexus | `npx gitnexus analyze . --force` @ trunk HEAD |

**Evidence:** `production/qa/smoke-sprint-33-baseline-*.md` + `production/agentic/sprint-33-gitnexus-*.md` (day-1 index row) + `sprint-status.yaml` counter update.

**Per-project reference (S32 closeout @ 1073):**

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 264 |
| ProjectAegis.Delegation.Tests | 232 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 201 |
| ProjectAegis.MissionEditor.Cli.Tests | 36 |
| ProjectAegis.Data.Tests | 335 |
| ProjectAegis.Data.Excel.Tests | 5 |

### Day-1 dispatch command

```bash
/dev-story dispatch S33-01
```

### Day-1 verify (bash)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
git checkout main && git pull --ff-only

dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
git diff HEAD -- src/ProjectAegis.Delegation/DelegationBridge.cs  # expect empty

npx gitnexus analyze . --force
```

---

## S33-11 — C2 manual sign-off upgrade (should-have)

**Depends:** S33-06 (Phase G comms Unity), S33-10 (presentation evidence).  
**Baseline:** `production/qa/c2-manual-signoff-2026-06-02.md` @ S32 **16/16 PASS WITH NOTES**; `production/qa/sprint-32-c2-signoff-2026-06-19.md`.

### Expectations

| Check | S33-11 action | Evidence |
|-------|---------------|----------|
| 1–13 | Re-confirm PASS (no regression) | Existing S31 headless proxy filters |
| 14 | **Upgrade** — add comms columns (`LinkId`, `Role`, `SatcomCapable`) alongside S32 damage | `PlatformCatalogViewer` + `PlatformImport` filters; S33-10 PNGs |
| 15 | Re-confirm PASS | `Doctrine` filter; S31 PNG fallback acceptable (lean) |
| 16 | Re-confirm PASS | `C2TopBar` filter; S31 PNG fallback acceptable (lean) |
| **17** | **New** — platform comms/datalink fittings visible in catalog viewer | Headless `PlatformComms` tests + S33-10 comms PNG |

**Verdict target:** **PASS WITH NOTES** acceptable under lean mode (headless Linux; no live Unity Editor walkthrough required). Live Editor capture remains advisory per S32-10/S32-11 precedent.

**Headless verify:**

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|Doctrine|C2TopBar|PlatformCatalogViewer|PlatformComms" -v minimal
```

**Artifacts:** updated `c2-manual-signoff-*.md`; `production/qa/sprint-33-c2-signoff-*.md`.

---

## S33-12 — CI / local gate refresh (nice-to-have)

**Pattern:** Carry **S32-12** doc-only hygiene — does **not** block S33-13 closeout. Fourth deferral acceptable per sprint cut line.

| Item | S32-12 (resolved) | S33-12 (refresh) |
|------|-------------------|------------------|
| Merge authority | Buildkite `buildkite/cmano-clone` | Unchanged |
| GHA | Advisory (billing) | Unchanged |
| Local fallback | `tools/verify-ci-local.ps1` / `tools/buildkite/dotnet-ci.sh` | Refresh threshold comments |
| Day-1 threshold | ≥1006 | **≥1073** |
| Closeout threshold | ≥1046 | **≥1086** |

**Reference:** `production/qa/sprint-32-ci-hygiene-2026-06-19.md`, `docs/engineering/buildkite-ci.md`.

**Acceptance:** `production/qa/sprint-33-ci-hygiene-*.md` documenting Buildkite blocking gate + local advisory parity at **≥1073/≥1086**. Script edits only if comments still cite S32 thresholds.

**Dispatch (if capacity):** `/dev-story dispatch S33-12` after S33-01; may run parallel with Wave 4–5.

---

## S33-13 — Closeout hygiene (should-have)

**Depends:** S33-02+ must-have landed. Runs before `/team-qa sprint` sign-off.

| Gate | Expected |
|------|----------|
| `dotnet test ProjectAegis.sln` | **≥1086/1086** PASS (+13 feature tests budget) |
| `ReplayGoldenSuiteTests` | **6/6** PASS |
| GitNexus | Fresh `npx gitnexus analyze . --force` @ tip |
| `DelegationBridge.cs` | ZERO touch |
| Production Baltic hash | Unchanged unless isolated fixture approved |
| Stack prune | `stack/sprint32/*` — 0 local refs documented |
| Tracker | Rows **06** (DBI-1.5 + DBI-3.5), **18**, **20**, **21** updated |

**Evidence:**

- `production/qa/smoke-sprint-33-closeout-*.md`
- `production/agentic/sprint-33-gitnexus-*.md` (closeout row; delta vs S32 15,064/30,605)
- `production/agentic/stacks/sprint33/S33-*-DONE.md` (13/13 target)

**Dispatch:** `/dev-story dispatch S33-13` after S33-11 (S33-12 optional).

---

## Lean QA gate (reference only)

**Do not duplicate** story-level test matrices here. All classification, hard gates, filtered commands, manual checklists, and Definition of Done live in:

→ **`production/qa/qa-plan-sprint-33-2026-11-27.md`**

### Merge policy (every PR)

| Rule | Action |
|------|--------|
| **Required CI** | `buildkite/cmano-clone` green `build` step |
| **Full sln** | `dotnet test ProjectAegis.sln` ≥1073 (every merge); ≥1086 at closeout |
| **Sim/delegation merges** | `ReplayGoldenSuiteTests` 6/6 |
| **Data merges** | `npx gitnexus impact CatalogWriteGate` before edit; WriteGate filter on PR |
| **DelegationBridge** | Reject PRs with diffs unless ADR waiver |
| **S33-09** | Phase 6 smoke only — S32 sim stories already landed; no carryforward QA path |

### Sign-off sequence

1. S33-13 closeout smoke **PASS**
2. `/smoke-check sprint` → `production/qa/smoke-sprint-33-closeout-*.md`
3. `/team-qa sprint` → `production/qa/qa-signoff-sprint-33-*.md`

**Sprint gates (QA plan):** S33-02 dependency graph, S33-03 kill-chain rules, S33-04 datalink comms share gate, S33-06 Phase G comms Unity.

---

*DevOps + QA planning — Sprint 33 kickoff 2026-06-19. S32 sign-off: `production/qa/qa-signoff-sprint-32-2026-06-19.md`.*