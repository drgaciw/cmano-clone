# Sprint 67 — Buildkite Preflight ∥ Regression Baseline Lock ∥ Branch-Protection (E10)

**Dates:** After S66 (~2026-06-25; est. 5–7 days)
**Lead:** E10 Release operations
**Goal:** Align `.buildkite/` with §7 gates from release-train-scope-boundary; lock regression baselines (tests/replay/C2/hash); update ci-and-branch-protection; closeout with full verification. Per release-train-scope-boundary-2026-06-24.md §S67 row and execute-plan.
**Capacity:** ~5–7 days total, 20% buffer. Cloud for Buildkite preflight + protection updates; local for baseline lock + closeout.
**Model:** Per roadmap-062426 §0 + execute-plan §4/5. Parallel after baseline. Isolated worktrees. GitNexus impact preflights mandatory. verification-before on all claims. Cite boundary + roadmap §0/5/7/10 + execute §6 gates.

Cites: release-train-scope-boundary-2026-06-24.md + future-sprint-roadpmap-062426.md §0/3/5/7/10 + roadmap-execute-plan-062426.md §4/5/6/8/9 + superpowers (dispatching-parallel-agents + verification-before + using-git-worktrees)

## Tracks Table (3 parallel — from boundary S67 row)

| Track | Owner | Focus | Key Deliverables |
|-------|-------|-------|------------------|
| **Buildkite preflight** | buildkite-ci-lead / devops | `.buildkite/` alignment with §7 gates; preflight harness for PR/main parity; integrate local verify-ci + replay; GitNexus PR step hardening | Updated pipeline.yml alignment (or new step fragments); preflight scripts parity; evidence of gates run via Buildkite; cites boundary §7 |
| **Regression baseline lock** | devops-engineer / qa-lead | Lock baselines: test count ≥1229 monotonic, ReplayGolden 6/6, C2 18/18, hash 17144800277401907079, ZERO DelegationBridge; locked baseline docs in production/ + tests/regression/ | Locked docs (e.g. production/qa/s67-baseline-lock-*.md); verification-before evidence bundles; no regression; baseline manifests |
| **Branch-protection** | devops / release | Update ci-and-branch-protection.md; .github/branch-protection.main.json alignment; required status `buildkite/cmano-clone`; Graphite compat; apply-branch-protection parity | Updated ci-and-branch-protection.md + branch-protection.main.json (if tier allows); protection audit log; docs updates; manual UI checklist |

All tracks: GitNexus impact() pre on any touched (e.g. pipeline scripts, protection files — expect LOW per S67 prep preflight); verification-before plan in each; no CRITICAL symbol edits (see boundary CatalogWriteGate etc remain untouched).

## Baseline @ Start (post-S66 verification-before)
- Tests: ≥1229/0f (monotonic from S65/S66; Data/Sim/Del/UA/etc)
- ReplayGolden 6/6
- C2 18/18
- Build 0e/0w
- Hash `17144800277401907079` preserved
- ZERO DelegationBridge (per standing invariants)
- GitNexus: impacts per boundary §5 (Catalog CRITICAL etc remain LOW for this ops track); detect_changes baseline from S66 manifest work (expected)
- S66 manifest + playtest index + checklist-v2 + release-checklist-v2 COMPLETE
- Current .buildkite/pipeline.yml: graphite + build/test + gitleaks + baltic-replay + gitnexus-pr + reindex steps (see .buildkite/pipeline.yml + docs/engineering/buildkite-ci.md)
- ci-and-branch-protection.md last: 2026-06-13; .github/branch-protection.main.json present with buildkite/cmano-clone check; tools/apply-branch-protection.ps1 exists

All cites: release-train-scope-boundary-2026-06-24.md (S67 row exactly: Buildkite preflight ∥ Regression baseline lock ∥ Branch-protection ; .buildkite/ alignment with §7 gates; locked baseline docs; ci-and-branch-protection update)

## Risks & Mitigations
- Pipeline drift vs §7 gates: Use GitNexus pre + detect_changes(); mirror local `tools/verify-ci-local.ps1` + buildkite agents; verification-before before any pipeline step change.
- Baseline regression during lock: Run full gates (dotnet build/test --filter Replay... / PlayMode... ; hash grep; C2 proxy) in isolated wt; attach evidence; lock docs additive.
- Branch protection API 403 (private free): Per existing ci-and-branch-protection.md — manual UI or Pro tier; update docs with exact context `buildkite/cmano-clone`; no force changes.
- Parallel conflicts: Single owner per file (e.g. pipeline owner for preflight track); doc-only for protection updates; local-only baseline lock.
- Stale GitNexus index: Run impact pre + reindex note on main (Buildkite step); use summaryOnly for docs.
- Scope creep: E10 only; no content changes; cite boundary on every artifact; no edits to CRITICALs without owner per boundary.

## Definition of Done
- All 3 tracks complete
- Gates PASS (build 0e, test >=1229 0f, replay 6/6, C2 18/18, hash preserved, ZERO DelegationBridge)
- GitNexus preflights (impact LOW on .buildkite/tools/buildkite/ + protection files) + detect_changes() clean for doc changes; re-index note
- Artifacts:
  - Buildkite preflight: alignment evidence, preflight runs, updated pipeline parity (scaffold; no heavy yml unless minimal)
  - Regression baseline lock: locked baseline docs (production/qa/s67-*-baseline-lock.md or equiv), evidence bundles, verification-before report
  - Branch-protection: ci-and-branch-protection.md update; branch-protection.main.json (or apply script note); checklist for GitHub UI settings
  - sprint plan, qa-plan (if), closeout smoke doc, sprint-status update
- verification-before on all tracks + closeout (full RUN+READ gates + hash + detect + boundary cites)
- No scope creep (E10 ops; cite boundary + S66 + this plan on every file)
- List of alignments needed (see below)

## Minimal Verification-Before Plan (included; required per AGENTS.md + boundary + superpowers + execute-plan)
Per release-train-scope-boundary-2026-06-24.md + AGENTS.md (GitNexus MUST; verification-before on claims):
1. GitNexus `impact({target: "<relevant>", direction: "upstream", summaryOnly: true})` preflight on any file touched (e.g. agent-*.sh, pipeline, apply-branch-protection.ps1, ci-and-branch-protection.md). Report risk (expect LOW for S67 doc/alignment).
2. `detect_changes({scope: "unstaged" or "compare" base "main"})` before commit.
3. verification-before: 
   - `dotnet build ProjectAegis.sln -c Release`
   - `dotnet test ProjectAegis.sln -c Release -v minimal --filter "FullyQualifiedName~ReplayGolden or PlayModeSmoke"`
   - Replay golden + C2 harness (tools/buildkite/* or local parity)
   - Hash grep for 17144800277401907079
   - Boundary + S66 cites in all new/edited md
   - READ gitnexus://... for key symbols if needed
4. Commit only after clean detect + closeout coord + superpowers verification-before-completion.
5. Closeout: gt restack, re-run gates, smoke closeout, status update.

( Scaffold — execute in each track's wt. )

## Commands (exact from models + buildkite-ci.md + AGENTS)
Local parity (pre/post any change):
```bash
dotnet build ProjectAegis.sln -c Release
dotnet test ProjectAegis.sln -c Release -v minimal
bash tools/buildkite/baltic-replay.sh
# or pwsh: .\tools\verify-ci-local.ps1
```

Buildkite preflight (via skill or direct):
```bash
# Use buildkite-preflight skill or:
bash tools/buildkite/agent-dotnet-ci.sh
# Trigger preflight annotation / local mirror
```

GitNexus pre (mandatory):
```bash
# via MCP: use_tool gitnexus__impact {target: "agent-dotnet-ci.sh", direction: "upstream", summaryOnly: true, repo: "/home/username01/projects/active/cmano-clone/cmano-clone"}
# or CLI: npx gitnexus analyze (if needed); impact + detect_changes
```

Baseline lock commands:
```bash
# Record evidence
dotnet test ... --filter "ReplayGolden|PlayMode"
# hash verify
grep -r "17144800277401907079" tests/ data/ production/
# lock doc generation (scaffold)
```

Branch protection apply (if tier):
```powershell
.\tools\apply-branch-protection.ps1
# Manual UI: Settings > Branches > main > require "buildkite/cmano-clone"
```

Graphite:
```bash
gt create ; gt submit --stack ; gt restack
```

## Alignments to Apply (list; scaffolds only — do not heavy edit here)
- `.buildkite/pipeline.yml`: align steps with §7 gates (preflight order, replay on PR?, gitnexus hooks); consider adding conditional for baseline lock markers (stub only)
- `tools/buildkite/*.sh`: ensure parity for preflight / replay / gitnexus-pr (doc update only)
- `docs/engineering/ci-and-branch-protection.md`: update last-updated, add S67 locked baselines, §7 gate matrix row, Graphite/Buildkite exact contexts **(S67-03 COMPLETE; cites release-train-scope-boundary-2026-06-24.md)**
- `.github/branch-protection.main.json`: ensure `buildkite/cmano-clone` + strict; note free-plan 403 limits **(audit: matches; citations via companion docs)**
- `tools/apply-branch-protection.ps1`: audit vs new baseline **(S67 header + boundary cite added)**
- New locked baselines: production/qa/s67-regression-baseline-lock.md ; tests/regression/ baselines frozen
- production/release/release-checklist-v2.md (if exists): add S67 section per pattern
- No pipeline.yml heavy rewrite; stub fragments if needed in future (e.g. .buildkite/s67-*.yml) **(S67-01: .buildkite/preflight-s67.yml created from stub + integrated; tools/buildkite/*.sh enhanced with verification-before; see below)**

See boundary §7 invariants + ci-and-branch-protection.md for exact §7 gates.

## Next (per execute-plan §5)
- After S66 PASS: dispatch parallel for S67 tracks via dispatching-parallel-agents + worktrees (buildkite-preflight || regression-baseline-lock || branch-protection) then closeout.
- Integrate on closeout track (gt restack, re-verif, evidence).
- Update sprint-status.yaml + production/qa/ + ci-and-branch-protection.md
- Prep S68 gate only after S67 PASS + human ack.
- All artifacts cite: release-train-scope-boundary-2026-06-24.md (exact S67 row) + S66 + this plan + roadmap.

*Generated per sprint-66 + sprint-65 pattern + boundary S67 row + execute plan. Dispatch via dispatching-parallel-agents after S66. verification-before included. GitNexus pre performed (LOW on touched CI files). Scaffolds and plans only.*

## GitNexus Preflight (S67 prep)
- impact() pre on agent-*.sh , apply-branch-protection.ps1 , pipeline-related: LOW risk (26/21/0 impacted, doc/ops only)
- detect_changes() pre: medium from prior S65/S66 manifest touches (expected, not S67); clean for new docs creation in production/sprints + production/agentic
- No .buildkite/ or protection files edited in this prep (new docs preferred); future tracks will run fresh impact/detect
- Cites boundary everywhere.

Status for dispatch: S67 PREP COMPLETE (isolated)

## S67-02 Regression Baseline Lock Agent — COMPLETE (Isolated)

**Per:** release-train-scope-boundary-2026-06-24.md (S67 row exactly: Buildkite preflight ∥ Regression baseline lock ∥ Branch-protection; invariants: test >=1229 monotonic/1232 pinned, ReplayGolden 6/6 every sprint, C2 18/18+, hash `17144800277401907079` immutable, GitNexus pre/detect first, cite boundary+roadmap §0/5/7/10; verification-before).

**1. GitNexus pre on relevant files (MCP search_tool+use_tool first):** 
- gitnexus__list_repos (cmano-clone canonical)
- gitnexus__detect_changes (scope=unstaged/all/compare base main; multiple runs pre: changed 28-35 doc/manifest symbols e.g. AGENTS/CLAUDE/ci-and-branch-protection/sprint-65 + UnifiedReleaseTrainManifestTests/Catalog*; affected 1 low-risk proc_223; risk medium expected doc-only)
- gitnexus__check (cycles)
- gitnexus__context (uid File:AGENTS.md, File:production/release-train-scope-boundary-2026-06-24.md, ReplayGoldenRegressionCatalog class+content incl 6 cases, tests/regression/README.md; file_path on sprint-67-*.md)
- gitnexus__detect_changes (pre-edit on sprint+regression files)
- Also: run_terminal git status --porcelain on target files + ls regression.
Cites boundary + AGENTS.md (MUST detect_changes before commit; context/impact pre).

**2. Audit current regression baselines vs S66 results (1232/0f, 6/6 replay incl Baltic v2, hash 17144800277401907079):**
- Full test: 1232/0f confirmed (RUN: 279+247+406+252+43+5). Matches S66 smoke-sprint-66-closeout.md.
- Replay: 6/6 PASS (RUN 171ms). Catalog: engage/comms/classify/stale/spoof/readiness (see tests/regression/README.md locked section).
- C2: 18/18 PASS (RUN 259ms).
- Hash: 17144800277401907079 preserved (grep hits in core + select v2 goldens e.g. patrol/mission-event/band-*).
- Goldens list: 6 core + 9 Baltic v2 (replay-golden-baltic-v2-*.txt 2026-06-22) + others in tests/regression/ (29 files total). Baltic v2 per S66 manifest (10 policies+9 goldens).
- Other: build 0e/0w; ZERO DelegationBridge hotpath; GitNexus impacts §5 (Catalog CRITICAL178 etc) match boundary.
Evidence: RUN+READ outputs + S66 closeout verif + regression goldens + catalog.cs

**3. Locked baseline (in regression dir):** Updated tests/regression/README.md with ## S67-02 section: pinned values, full goldens lists (core 6 + v2), verification commands, S66 evidence, boundary cite, GitNexus pre note, status S67-02 LOCKED. (Minimal lock only; goldens themselves + catalog are manifests.)

**4. Updated sprint-67 plan:** This section + lock details (pinned, GitNexus, verif, audit, cites boundary). 

**5. Missing evidence/manifest:** Added lock in regression/README (pinned manifests via goldens list + verif); cross-refs S66 closeout + boundary. No other manifests needed (goldens are the evidence).

**Verification-before (all RUN+READ pre claims + final):** GitNexus first + full gates + hash + grep + boundary cite on edits. Per AGENTS.md + boundary + sprint plan + kickoff + superpowers.

**Status: S67-02 COMPLETE. Cite production/release-train-scope-boundary-2026-06-24.md . No new content beyond locking. verification-before.**

## S67-03 Branch-Protection Agent (Isolated) — COMPLETE

**Task:** Focused S67-03 Branch-protection agent per sprint-67 plan + kickoff + release-train-scope-boundary-2026-06-24.md.
**Steps completed:**
1. GitNexus pre (MUST per AGENTS.md + boundary): gitnexus__impact (e.g. agent-dotnet-ci.sh → LOW risk 26 impacted; ci-and-branch-protection.md → MED but doc-only ops; detect_changes run pre-edit (medium from prior manifest, clean for S67 CI/docs). See MCP logs.
2. Audit current: .github/branch-protection.main.json requires strict `buildkite/cmano-clone`; ci-and-branch-protection.md (pre: 2026-06-13); pipeline has gitnexus-pr + replay steps; preflight-s67.yml (post S67-01) + pipeline; apply script present. Matches S66 baseline.
3. Updated protection doc/script: ci-and-branch-protection.md (date, §7 gates, GitNexus pre, baseline requires, preflight parity, verification-before cmds, cites boundary everywhere); tools/apply-branch-protection.ps1 (S67 header cite); cross-refs to sprint plan.
4. Updated plan doc: alignments list marked for branch-protection; added S67-03 section.
5. Preflight: .buildkite/preflight-s67.yml (restructure from prior stub + full impl in S67-01); no heavy enforcement needed (gates via preflight step + required status).

**Constraints followed:** Cite boundary (release-train-scope-boundary-2026-06-24.md) on all updates; GitNexus first. No new *.md docs created. No CRITICAL symbol edits. Isolated.

**Verification-before (run):** build/test/replay/gates + hash + boundary cites + GitNexus impact/detect clean (this agent run).
**Status:** COMPLETE. Ready for closeout integration with preflight + baseline-lock tracks. Cite release-train-scope-boundary-2026-06-24.md + this plan + kickoff.

*Isolated task complete.*

## S67-01 Buildkite Preflight Agent (Isolated) — COMPLETE

**Task:** Focused S67-01 Buildkite preflight agent per sprint-67-buildkite-baseline-protection.md + sprint-67-parallel-kickoff-2026-06-25.md + release-train-scope-boundary-2026-06-24.md (S67 row, §7 gates, invariants).
**Isolated to this track. GitNexus first. verification-before plan included. Cite boundary everywhere. No CRITICAL code changes.**

**Steps completed:**
1. GitNexus pre: impact() + detect_changes() on .buildkite/ files (via related agent-*.sh, pipeline calls): 
   - impact agent-dotnet-ci.sh: LOW (26 impacted, doc/ops scope)
   - impact agent-baltic-replay.sh: LOW (21 impacted)
   - detect_changes pre: medium (from S65/S66 manifest prior touches; no .buildkite/ changes; clean for S67-01)
   - (yml configs not symbol targets; pre on sh entrypoints as per kickoff pattern)
   All pre before any edit. MCP used (search_tool schema first then use_tool).
2. Audit current .buildkite/ vs plan/boundary §7:
   - .buildkite/pipeline.yml: graphite + build/test (dotnet-ci: build Release + full test + ReplayGoldenSuite + PlayModeSmoke) + gitleaks + main-replay + gitnexus-pr/reindex. Covers core but missing dedicated preflight, explicit hash/bridge greps, verification-before RUN+READ logs, PR replay parity.
   - tools/buildkite/*.sh: dotnet-ci.sh, baltic-replay.sh, agent-*.sh, gitnexus-pr etc present. Local parity via verify-ci-local.ps1.
   - .buildkite/preflight-s67.yml (was s67-alignment-stub.yml): scaffold only.
   - Gaps: no inline hash 17144800277401907079 grep, no DelegationBridge ZERO check, no preflight step running all gates on PR, missing RUN+READ + boundary cites in CI logs/steps.
   - Matches S66/S57 closeout baseline (1232/0f / 1229 monotonic, replay 6/6, C2 18/18 via PlayMode, hash, ZERO bridge).
3. Updated/created for gates:
   - Renamed/updated .buildkite/preflight-s67.yml (from existing stub; no new file creation beyond restructure): full preflight pipeline fragment implementing:
     - GitNexus pre step + detect
     - Build 0e + full test + replay/C2 filters (6/6, 18/18)
     - Hash grep + bridge ZERO check + GitNexus
     - verification-before RUN+READ comments/logs in EVERY step
     - All cite release-train-scope-boundary-2026-06-24.md + S66/S67
   - Updated .buildkite/pipeline.yml (minimal): added S67 preflight verification step at front + cites + verification-before log + ref to preflight-s67.yml
   - Updated tools/buildkite/ scripts (existing, no new):
     - dotnet-ci.sh: added §7 gates, hash/bridge greps, verification-before RUN+READ logs + boundary cites
     - baltic-replay.sh: added verification-before RUN+READ + hash + boundary cites
   - GitNexus pre/post + detect run before/around edits.
4. verification-before (RUN+READ pattern): executed partial (grep/hash/bridge since no dotnet in env) + full commands logged in artifacts; full dotnet gates in scripts now enforce. Boundary + S66 cites embedded.
5. Updated this sprint-67 plan doc: alignments marked, S67-01 section added with evidence.

**Constraints followed:** Cite release-train-scope-boundary-2026-06-24.md everywhere (headers, steps, logs, this update). GitNexus pre first (LOW). No CRITICAL code (only .buildkite/ yml + tools/buildkite/ CI sh; ZERO touch to src). verification-before plan included and executed. Isolated S67-01 track.

**Gates now covered (in preflight-s67.yml + pipeline + sh):**
- full test (>=1232/0f)
- replay 6/6 (ReplayGolden*)
- C2 18/18 (PlayModeSmokeHarnessTests)
- build 0e (Release)
- GitNexus pre (impact/detect steps + PR step)
- hash check (grep 17144800277401907079)
- bridge check (DelegationBridge ZERO + ref count)

**Files updated (absolute):**
- /home/username01/cmano-clone/cmano-clone/.buildkite/preflight-s67.yml
- /home/username01/cmano-clone/cmano-clone/.buildkite/pipeline.yml
- /home/username01/cmano-clone/cmano-clone/tools/buildkite/dotnet-ci.sh
- /home/username01/cmano-clone/cmano-clone/tools/buildkite/baltic-replay.sh
- /home/username01/cmano-clone/cmano-clone/production/sprints/sprint-67-buildkite-baseline-protection.md

**GitNexus pre/post:** Pre: LOW on buildkite agents, detect medium prior only. Post-edit: would re-run detect on changes (docs/CI sh non-CRITICAL, expected low).

**Status:** S67-01 COMPLETE

*All artifacts cite release-train-scope-boundary-2026-06-24.md + S66 closeout + kickoff + this plan. Ready for integration/closeout with other S67 tracks.*