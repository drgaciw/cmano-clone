# S42 Readiness from S41 — Parallel Execution Scaffolding (Planning Only)

**Date:** 2026-06-20  
**Status:** PLANNING / SCAFFOLDING ONLY — DO NOT DISPATCH S42 AGENTS  
**Authority (mandatory citations everywhere):**  
- [`production/sprints/sprint-42-release-kickoff-content-art-bible-w1.md`](../sprints/sprint-42-release-kickoff-content-art-bible-w1.md)  
- [`production/agentic/sprint-42-parallel-kickoff-2026-06-20.md`](./sprint-42-parallel-kickoff-2026-06-20.md)  
- [`production/release-enablement-scope-boundary-2026-06-20.md`](../release-enablement-scope-boundary-2026-06-20.md)  
- [`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §9 (S42–S48 mapping)  
- [`production/agentic/s39-s48-worktree-manifest.md`](./s39-s48-worktree-manifest.md) §S42  
- [`production/agentic/local-cloud-agent-routing.md`](./local-cloud-agent-routing.md)  
- S41 artifacts (handoff): [`production/sprints/sprint-41-polish-hardening-release-preflight.md`](../sprints/sprint-41-polish-hardening-release-preflight.md), [`production/agentic/sprint-41-parallel-kickoff-2026-06-20.md`](./sprint-41-parallel-kickoff-2026-06-20.md), [`production/agentic/s41-track-b-gap-analysis.md`](./s41-track-b-gap-analysis.md), [`production/gate-checks/scope-expansion-decision-2026-06-20.md`](../gate-checks/scope-expansion-decision-2026-06-20.md), [`production/gate-checks/scope-expansion-decision-template-2026-06-20.md`](../gate-checks/scope-expansion-decision-template-2026-06-20.md), [`production/qa/qa-plan-sprint-41-2026-06-20.md`](../qa/qa-plan-sprint-41-2026-06-20.md), [`docs/adr/s41-structural-debt-decision-telemetry-osint.md`](../../docs/adr/s41-structural-debt-decision-telemetry-osint.md), [`production/determinism/determinism-audit-2026-06-20.md`](../determinism/determinism-audit-2026-06-20.md)  
- `production/sprint-status.yaml` (S41 planned; S42 ready_to_dispatch_docs_only + scope_gate: blocked)  
- Gate: **S42 explicitly blocked per S41-06** (Closeout + scope-decision packet — Polish-exit report + gap analysis for gate; smoke; **blocks S42 dispatch**).  

> **⛔ S42 BLOCKED PER S41-06.** Scope gate APPROVED 2026-06-20 but Track A exit (S40 + S41 closeout PASS + S41 ADR + polish-exit pack + smoke-sprint-41-closeout) required before any S42 agent dispatch. All S42 work is **planning + scaffolding only**. Parallel planning safe while S41 finishes. Cite S41 closeout requirement in every artifact/story.

**Role embedding (S42 planning):** c-sharp-devops-engineer + team-qa + sprint-plan + buildkite specialists + csharpexpert + subagent-driven-development + executing-plans (S42 declarative role style adapted). Sequential-thinking discipline applied (8 thoughts: read phase, synthesis of blocks/gates, wave/ownership, GitNexus B1, skill bindings, skeletons, bootstrap, final lock). Superpowers patterns: isolated stack contexts, file ownership enforcement, GitNexus `impact()` mandatory pre-edit, no shared files across tracks.

---

## 1. Confirmed Prereqs from S41 Closeout (Handoff Verification)

**S41-06 explicitly blocks S42.** (sprint-41 plan + kickoff + qa-plan + scope-decision + release-boundary + sprint-status + roadmap §9)

Required S41 artifacts (status: most planned / partial; [PENDING S41] in decision doc):
- S41-01: Full-solution re-baseline + smoke (baseline doc exists: smoke-sprint-41-baseline-2026-06-20.md; ≥1226 tests, Replay 6/6, proxy 18/18, GitNexus tip).
- S41-02: QA plan (exists: qa-plan-sprint-41-2026-06-20.md — cites "S42 dispatch blocked until S41 closeout + scope packet"; blocks waves).
- S41-03: Structural-debt ADR (read-only; exists: docs/adr/s41-structural-debt-decision-telemetry-osint.md — GitNexus CRITICALs, cohesion 60%/67%/68%, input to B3).
- S41-04: Determinism audit + re-index (exists draft: determinism-audit-2026-06-20.md; GitNexus `node .gitnexus/run.cjs analyze` required).
- S41-05: Polish-exit evidence pack (target: production/qa/evidence/README-polish-exit-*.md).
- S41-06: Closeout + scope-decision packet (decision-2026-06-20.md APPROVED with interim gap; smoke-sprint-41-closeout + gate-checks packet **PENDING**; this is the explicit block).
- S41-07: Track B gap analysis (exists: s41-track-b-gap-analysis.md — full B1–B6 enum, 13 committed rows, GitNexus impacts, csharpexpert feasibility, creep flags, boundary citations).
- S41-08/09: Pre-flight checklist + manifest refresh (partially in decision/gap + this readiness).

**Post-S41 requirements for S42 dispatch (from release-boundary §Dispatch gate, decision, roadmap §9, sprint-status):**
- S41 COMPLETE (smoke + retro + closeout).
- S41 ADR on file.
- Polish-exit pack.
- Scope packet signed (decision updated with S41 refs; currently has [PENDING S41]).
- New boundary doc published (release-enablement-scope-boundary-2026-06-20.md — done).
- GitNexus @ HEAD (post S41-04).
- Test baseline documented (S41-01/02).
- Worktrees / routing assigned (manifest + this doc).

**Current block status (verified 2026-06-20):** S41 status=planned; S42 status=ready_to_dispatch_docs_only, scope_gate=blocked. "S42 blocked until S41 closeout." (sprint-status.yaml). Decision: "S42–S48 may proceed ... once Track A exit criteria are met." "Dispatch S42 only after S41 closeout PASS".

**Wave plan refinement (from S42 plan + kickoff + release-boundary §Sprint mapping + roadmap §9):**
- Day-1 / W0: S42-01 (baseline + expanded gate matrix; cite new boundary) → S42-02 (QA plan B1/B2; blocks waves).
- W1+: S42-03 (Catalog/Platform content), S42-04 (Scenario/data), S42-05 (art bible 1–4), S42-06 (closeout).
- Capacity: ~10 effective dev-days; 5 parallel tracks; B1 first 6 rows in wave1 (Req 02/06/12/13/16/21 projection-side).
- S42 scenario/data: Baltic policy JSON **maintenance only**; no prod hash change without golden ADR.
- Expanded gate matrix: S42-01 produces `production/qa/gate-matrix-track-b-2026-06-20.md` (or dated).

**File ownership matrix update (refined from S42 kickoff + manifest + release-boundary + local-cloud):**
- `data/scenarios/*` → Content Scenario (S42-04; replay-gated).
- `src/**/Catalog*`, `Platform*` projections (esp. read-models, surfacing) → Content Catalog/Platform (S42-03; **single local lead agent**; mandatory `impact()`).
- `design/art/art-bible.md` (sections 1–4) → Art bible 1–4 (S42-05).
- `production/sprint-status.yaml`, smoke/closeout, gate matrix → Closeout / c-sharp-devops (S42-01/06/02).
- Shared/coordinator-only: sprint-status.yaml, perf baselines, specific C2/Platform presenters (see manifest).
- Never parallel-edit; one owner per Catalog cluster.

**GitNexus impact matrix for B1 tracks (quick health; summary only; from tool calls + S41 gap-analysis + roadmap §2):**
- **CatalogWriteGate** (src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs — core to B1 Catalog/Platform): **CRITICAL**. 176 impacted (d=1:93, d=2:58, d=3:25). 7 processes (RunCatalogImportMarkdown, PlatformImportXlsxCommand.Run, ProposePlatformWeaponMounts, OnApproveSelected x2, OnProposeClicked, CatalogDiffProposalAgent). Modules hit: Import(44), Platform(37), WriteGate(19), Catalog(14), Telemetry(9), Osint(7), Runtime(5). **B1 risk: HIGH blast-radius on any Catalog symbol edit. Mandatory `impact()` + `detect_changes()` before edit. Extend-only invariant (release-boundary). Single local lead required.**
- **Platform** (projections/read-models e.g. PlatformCatalogFilterProjection, PlatformCatalogViewer patterns): High coupling via WriteGate/Import per gap (37 direct in some). Projection-side safe for B1 surfacing (Req 06/21/16/13/12). Low direct upstream in current index on some symbols → good for read-only projection work. Cohesion ~73% (roadmap).
- **Scenario** (e.g. ScenarioSimulateSampleCommand, policy JSON, Baltic replay harness): Low direct upstream in current index calls. **Replay-gated critical path (S42-04).** Must maintain ReplayGolden 6/6 + Baltic hash `17144800277401907079` immutable. Cross-cuts B1 Req 02/17/18/04 via order-log/DecisionLog (CRITICAL 261 impacted per gap).
- **Cross-B1 (DecisionLog, BalanceTelemetryAccumulator from S41 ADR/gap):** CRITICAL/HIGH (261/32 impacted). DecisionLog god-class risk (SRP/OCP). Hits Baltic/Projection/Orchestration/Bridge/Runtime. B1 content waves must avoid; defer to B3/S44. GitNexus index current @ c4d6e52 (post recent; S41-04 will confirm/reindex). Health: use summaryOnly + targeted before any B1 edit. Rebuild: `node .gitnexus/run.cjs analyze`.
- **Recommendations for B1:** `impact()` on **every** symbol edit (Catalog cluster priority). No DelegationBridge. Replay 6/6 after Scenario touches. Catalog single-owner. Cite GitNexus in all S42-03 stories.

**S41 closeout requirement cited:** All above prereqs, matrix, impacts derived from S41-06/07 artifacts + S41 ADR. S42 planning independent but **blocks on S41-06 packet + human gate**.

---

## 2. Skeletons for S42-01 / S42-02 (Baseline + QA Plan)

**Explicit block (everywhere):**  
> **⛔ BLOCKED UNTIL S41 SCOPE PACKET + HUMAN GATE + S41-06 CLOSEOUT PASS.**  
> Per sprint-41-polish-hardening-release-preflight.md S41-06, release-enablement-scope-boundary, scope-expansion-decision-2026-06-20.md, sprint-status.yaml, roadmap §9, qa-plan-sprint-41. Planning + scaffolding only. No agent dispatch. S42-01 must cite S41 closeout + new boundary. S42-02 produced post S42-01 but before any wave (S42-03+).

### Skeleton: S42-01 — Re-baseline + Expanded Gate Matrix (c-sharp-devops-engineer)

**ID:** S42-01  
**Owner/Agent:** c-sharp-devops-engineer (superpowers + buildkite + executing-plans)  
**Est:** 1 day (Day-1 / cloud-eligible but local preferred for merge)  
**Dependencies:** Scope gate APPROVED + S41 closeout PASS (S41-06 smoke + packet). **BLOCKED PER S41-06.**  
**Stack:** `stack/sprint42/baseline-qa` (or per manifest §S42)  
**Acceptance Criteria (planning draft; refine in actual story):**
- Full sln build + test ≥ post-S41 baseline (≥1226 or S41 final; monotonic; no regression).
- ReplayGolden 6/6; C2 proxy 18/18+ (matrix expand if needed for B1).
- GitNexus @ HEAD (re-index if stale; `node .gitnexus/run.cjs analyze` + status).
- **New boundary doc cited** in all artifacts (release-enablement-scope-boundary-2026-06-20.md + committed rows).
- Produce/ update expanded gate matrix: `production/qa/gate-matrix-track-b-2026-06-20.md` (headless floor, replay, proxy filters incl. new B1, Baltic hash immutable, CatalogWriteGate extend-only, impact() mandatory).
- Smoke artifact: `production/qa/smoke-sprint-42-*-baseline.md` or equivalent.
- sprint-status.yaml + kickoff refs updated with S41 closeout cite.
- 0 errors; impact() exercised on any baseline symbols touched.
- **csharpexpert note:** Verify Catalog/Platform/Scenario modules for blast (use GitNexus summary in this readiness); projection-side only for B1 content prep.
- **Subagent-driven:** Isolated baseline context only (no wave history); sequential verification of gates.

**Blocked note (embed in story):** "S42-01 execution blocked per S41-06 until S41 closeout + scope packet. This skeleton is planning readiness only."

**Handoff from S41:** Reuse S41-01/02 baseline + qa-plan patterns + S41-04 determinism + S41-06 smoke template.

### Skeleton: S42-02 — Sprint 42 QA Plan (B1/B2 scope; blocks waves) (team-qa)

**ID:** S42-02  
**Owner/Agent:** team-qa (superpowers + c-sharp-devops-engineer collab)  
**Est:** 1 day (W0 / after S42-01; cloud)  
**Dependencies:** S42-01 + S41 closeout + scope packet. **BLOCKED PER S41-06.**  
**Stack:** `stack/sprint42/baseline-qa`  
**Acceptance Criteria (planning draft):**
- `production/qa/qa-plan-sprint-42-*.md` authored per qa-plan skill.
- Classify S42-03/04/05 stories (Logic/Integration/Visual/UI; B1 content vs B2 docs).
- Automated: headless ≥ baseline, ReplayGolden 6/6, PlayMode 18/18+ (expand filters for B1 Req e.g. doctrine/platform surfacing).
- Manual: Editor evidence notes (local only per routing); playtest scope for wave1.
- Smoke / gate matrix integration (cross-ref S42-01 output).
- Hard gates listed: replay, proxy, hash, impact(), boundary citation, no hash change.
- B1/B2 scope explicit (13 rows wave1+2; art §1–4; cite release-boundary + tracker).
- Evidence paths + sign-off criteria for S42-06.
- **csharpexpert + team-data input:** Catalog/Platform surfacing tests (projection/read-model focus); Scenario replay harness.
- Blocks all waves (S42-03+); QA plan review before dispatch.

**Blocked note:** "S42-02 QA plan is planning scaffold only. Full execution + waves blocked until S41-06 closeout packet + human gate recorded. Cite S41 qa-plan-sprint-41-2026-06-20.md handoff."

**Handoff from S41:** Directly extends qa-plan-sprint-41 structure + Evidence-Pack-QA + determinism sweep.

---

## 3. Recommended Skill Bindings for S42 Tracks

Per S42 kickoff track ownership + sprint plan + skills inventory (c-sharp-devops-engineer, team-qa, team-data, team-simulation, art-director, csharpexpert, c-sharp-architect from S41 handoff, sprint-plan, buildkite-cli/preflight, subagent patterns):

- **S42-01 Baseline + gate matrix + S42-06 Closeout:** `c-sharp-devops-engineer` (lead; build/CI/smoke/replay gates + executing-plans) + `team-qa` collab. csharpexpert for module health review.
- **S42-02 QA plan:** `team-qa` (lead; qa-plan skill) + `c-sharp-devops-engineer`. Embed csharpexpert for B1 test classification.
- **S42-03 Content wave 1 — Catalog/Platform (B1 first rows):** `team-data` (local lead per routing/manifest; data + projections) + `csharpexpert` (content feasibility, impact() analysis, C# projection-side SOLID). Single agent rule for Catalog cluster.
- **S42-04 Content wave 1 — Scenario/data:** `team-simulation` (replay-gated policy/JSON/harness; determinism).
- **S42-05 Art bible sections 1–4:** `art-director` + `team-ui` (docs expansion + AegisTokens.uss + gate matrix cross-ref).
- **Coord / boundary cite (S42-07 optional):** coordinator (manifest/kickoff updates).
- **Overall S42:** c-sharp-devops-engineer + team-qa + sprint-plan orchestration. Embed `csharpexpert` + `subagent-driven-development` (isolated stacks, file ownership, sequential decomp) + `executing-plans` (wave sequencing post-gate). Handoff from S41: `c-sharp-architect` (ADR) + `determinism-engineer` patterns for later.

**Buildkite / devops relevant (if tracks hit CI):** buildkite-cli, buildkite-preflight, buildkite-pipelines, c-sharp-devops-engineer for any pipeline/gate updates in baseline/closeout.

**Subagent-driven / superpowers:** Dispatch one agent per track with **isolated context** (story list + plan excerpts + gates + boundary + this readiness; no cross-track history). Use sequential-thinking for decomp. Enforce ownership matrix. GitNexus pre-edit.

---

## 4. Worktree Bootstrap Commands Ready (from manifest §S42 + local-cloud + kickoff)

**Bootstrap pattern (coordinator, local; planning only — no dispatch):**
```bash
# S42 tracks (per s39-s48-worktree-manifest.md §Track B)
git worktree add /home/username01/cmano-clone/.worktrees/sprint42-content-catalog-platform \
  -b stack/sprint42/content-catalog-platform main
git worktree add /home/username01/cmano-clone/.worktrees/sprint42-content-scenario \
  -b stack/sprint42/content-scenario main
git worktree add /home/username01/cmano-clone/.worktrees/sprint42-art-bible-1-4 \
  -b stack/sprint42/art-bible-1-4 main
git worktree add /home/username01/cmano-clone/.worktrees/sprint42-baseline-qa \
  -b stack/sprint42/baseline-qa main
git worktree add /home/username01/cmano-clone/.worktrees/sprint42-closeout \
  -b stack/sprint42/closeout main

cd /home/username01/cmano-clone/.worktrees/sprint42-... 
# Cloud agents: same branch name, fresh VM checkout (no Editor for evidence tracks)
# After: gt create / gt submit --stack etc per AGENTS.md
```

**Rules (enforce in all S42):**
- One worktree + stack branch per parallel track.
- Never two agents on same file (ownership matrix).
- Merge order: baseline/QA → code tracks (Catalog local lead first) → closeout last.
- `gt sync` + `gt restack` after each.
- Cleanup post: git worktree remove + branch -d after main merge.
- Verify routing: Catalog local-lead; Scenario/Art/ baseline Cloud or mixed; closeout Local.

**Verification command (every track, per routing):**
```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test ... --filter ReplayGoldenSuiteTests   # 6/6
# + impact() + detect_changes() + boundary cite check
```

**S42 readiness checklist update (partial from sprint-42-48-readiness-checklist.md):**
- [ ] This doc + S42 artifacts cite S41-06 block + closeout.
- [ ] GitNexus B1 matrix reviewed.
- [ ] Worktrees prepped (planning).
- Dispatch only after full global + S42 items + S41 COMPLETE.

---

## 5. Summary of S42 Tracks + Skill Assignments + Block Verification

**S42 Tracks (from sprint plan + kickoff + roadmap §9 + release-boundary):**
- S42-01: Re-baseline + expanded gate matrix (c-sharp-devops-engineer)
- S42-02: Sprint 42 QA plan — B1/B2 (team-qa)
- S42-03: Content wave 1 Catalog/Platform (team-data local lead + csharpexpert)
- S42-04: Content wave 1 Scenario/data (team-simulation)
- S42-05: Art bible §1–4 (art-director + team-ui)
- S42-06: Closeout (c-sharp-devops-engineer)
- (S42-07 optional: boundary cite)

**Skill assignments (recommended bindings):**
- Content heavy (03): team-data + csharpexpert (content + C#)
- Scenario (04): team-simulation
- Art/B2 (05): art-director + team-ui
- DevOps/QA/Baseline/Close (01/02/06): c-sharp-devops-engineer + team-qa (+ buildkite if relevant)
- Orchestration: sprint-plan + coordinator + subagent-driven (isolated)
- Embed: csharpexpert (B1 impact/feasibility), c-sharp-architect (S41 ADR handoff), executing-plans patterns.

**Block verification (explicit, multiple sources):**
- "S42 dispatch remains blocked until S40 + S41 closeout PASS" (release-boundary).
- "S42 blocked until S41 closeout" (sprint-status.yaml + roadmap §9 table).
- "blocks S42 dispatch" (S41-06 in sprint-41 plan + qa-plan-sprint-41 + decision "PENDING S41").
- "DO NOT DISPATCH until ... Out-of-boundary until gate." (S42 kickoff + sprint-42 plan).
- "S42 agent dispatch remains blocked until S41 closeout" (gate template / decision).
- **S42 blocked per S41-06** (this doc + all planning). Planning artifacts safe + parallel while S41 completes.

**csharpexpert + superpowers / subagent-driven embedded:** GitNexus discipline, isolated stacks, sequential decomp (this readiness), ownership matrix, csharp feasibility notes, no shared files, replay/det gates, projection-side B1.

**Verification of artifact:** (post-write) Read of this file confirms structure, all cites, blocks, matrices, skeletons, cmds, skills.

*Parallel planning complete. Max independent domain. S42 execution forbidden until S41-06 human gate. Cite S41 closeout requirement in all downstream.*

---
*Produced via S42 declarative role (c-sharp-devops-engineer + team-qa + ... + csharpexpert + subagent-driven + executing-plans). Sequential-thinking used. Planning + scaffolding only.*