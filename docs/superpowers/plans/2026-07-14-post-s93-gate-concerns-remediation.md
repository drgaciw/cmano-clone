# Implementation Plan: Post-S93 Gate CONCERNS Remediation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Address every actionable issue and next-step from `production/gate-checks/post-s93-project-release-hold-gate-2026-07-14.md` without falsely advancing Launch: land gauntlet QA into the Release branch safely, continue S93 residual assets, refresh architecture review, and document CRITICAL-hub merge discipline.

**Architecture:** Multi-track remediation. Track A (P0) stacks the sibling gauntlet worktree into the Release program branch with GitNexus impact gates on CRITICAL symbols. Track B (P1) continues deferred assets (ASSET-036/037/040/041) without Addressables bulk import. Track C (P1) produces a post-editor architecture-review report. Track D (P2, optional) only opens when human explicitly authorizes Launch. No `DelegationBridge` hotpath edits; CatalogWriteGate remain extend-only.

**Tech Stack:** .NET 8 / Project Aegis solution, GitNexus CLI+MCP, Graphite (`gt`), Demo batch + `gauntlet_oracle_eval`, design asset pipeline under `production/assets/` + `design/assets/`.

## Superpowers review of the gate (source truth)

| Gate finding | Class | Plan response |
|--------------|-------|---------------|
| Release hold PASS; Launch FAIL/deferred | Policy | **Do not** change `stage.txt` unless human Launch ack (Track D only) |
| CRITICAL hubs (ScenarioDocumentEditor 233, CatalogWriteGate 186, DelegationBridge 145, Patrol 113, BalticReplayHarness 54) | Process + merge risk | Task A0 playbook + mandatory impact before any code touch |
| Dual worktree: gauntlet on `07-10-qa_…` not on `s93-asset-production` | Integration debt | Track A: Graphite stack / cherry-merge with re-verify |
| Assets: 4 Needed + umbrellas incomplete | Content | Track B: produce ASSET-036/037/040/041; nudge umbrellas |
| Architecture CONCERNS unrefreshed | Quality | Track C: `/architecture-review` report |
| Launch checklist / store package missing | Commercial | Track D: stub remains until human scope expand |
| Editor PNG pack deferred (no host) | Residual | Document deferral; optional protocol only — no host invent |

## Global Constraints

- Repo primary for Release program: `/home/username01/cmano-clone` on `stack/post-editor/s93-asset-production` (HEAD was `9ea9657` at gate).
- Gauntlet source worktree: `/home/username01/projects/active/cmano-clone/cmano-clone` on `07-10-qa_gauntlet_gauntlet-20260710-1352` (HEAD historically `d927684`).
- Standing floors: build **0e/0w**, suite **≥1599/0f**, ReplayGolden **6/6**, hash **`17144800277401907079`** preserved, DelegationBridge **ZERO** hotpath.
- GitNexus: `node .gitnexus/run.cjs status` must be fresh @ HEAD before commits; use absolute repo path for MCP (`/home/username01/cmano-clone` or worktree path) to avoid dual-name ambiguity.
- **Never** rewrite `CatalogWriteGate` write paths; **never** edit `DelegationBridge` hotpath without golden ADR.
- Stage remains **Release** unless Track D human ack.
- Cite gate file + S92/S93 closeouts on all commits.

## File map

| Path | Role |
|------|------|
| `production/gate-checks/post-s93-project-release-hold-gate-2026-07-14.md` | Source requirements |
| `production/qa/gauntlet/**` (from gauntlet worktree) | Max-variance smoke artifacts to land |
| `data/scenarios/gauntlet-*.policy.json` | Policies + expects |
| `src/ProjectAegis.Data/Catalog/GauntletOracle*.cs` | Fingerprint fail-closed |
| `.github/workflows/gauntlet-oracle.yml` | CI fixtures |
| `design/assets/asset-manifest.md` | Asset status |
| `design/assets/specs/**` + `production/assets/**` | New asset binaries/specs |
| `docs/architecture/**` or `production/qa/*architecture-review*` | Track C output |
| `production/gate-checks/commercial-launch-execution-gate-TBD.md` | Track D only |
| `production/qa/evidence/gates-*.log` | Re-verification evidence |

## Out of scope (default execution)

- Advancing `stage.txt` to Launch
- Steam/store upload, tax, revenue
- Addressables bulk import
- Live Unity Editor PNG pack (no host)
- Mass CMO-DB scrape
- Rewriting CRITICAL hubs for convenience

---

### Task 0: CRITICAL-hub merge playbook (process gate)

**Files:**
- Create: `production/agentic/critical-hub-merge-playbook-2026-07-14.md`
- Modify: none (docs only)

**Interfaces:**
- Consumes: gate §1 watchlist impact counts
- Produces: mandatory checklist for Tracks A–C code edits

- [ ] **Step 1: Write playbook** listing the five CRITICAL symbols, command templates:

```bash
# From the checkout about to change:
node .gitnexus/run.cjs status
# MCP or CLI equivalents:
# impact BalticReplayHarness upstream summaryOnly
# impact GauntletOracleEvaluator upstream summaryOnly
# impact CatalogWriteGate upstream summaryOnly
# detect_changes scope=all  (or compare vs main before stack land)
dotnet test ProjectAegis.sln -c Release --nologo
```

- [ ] **Step 2: State rule** — any edit whose `detect_changes` touches those symbols requires impact output pasted into PR/commit body; `DelegationBridge` = refuse unless golden ADR.

- [ ] **Step 3: Commit** `docs(agentic): critical hub merge playbook for post-S93 gate`

---

### Task A1: Inventory gauntlet delta (read-only)

**Files:**
- Create: `production/qa/gauntlet-stack-land-plan-2026-07-14.md` (file list + commit SHAs)

- [ ] **Step 1: Diff worktrees**

```bash
# Gauntlet worktree
cd /home/username01/projects/active/cmano-clone/cmano-clone
git log --oneline stack/post-editor/s93-asset-production..HEAD | head -40
git diff --stat stack/post-editor/s93-asset-production...HEAD | tail -40
```

- [ ] **Step 2: Classify files** into: policies, oracle evaluator/tests, CI workflow, production/qa gauntlet runs, harness tests, unrelated noise.

- [ ] **Step 3: Write land plan** with ordered commits and rollback note.

- [ ] **Step 4: Commit** land plan on Release branch (docs only).

---

### Task A2: GitNexus pre-land impact on CRITICAL + new surfaces

**Files:** none (tooling only) → evidence log

- [ ] **Step 1: Ensure index fresh** on both checkouts if needed:

```bash
cd /home/username01/cmano-clone && node .gitnexus/run.cjs status
cd /home/username01/projects/active/cmano-clone/cmano-clone && node .gitnexus/run.cjs status
# If stale: node .gitnexus/run.cjs analyze
```

- [ ] **Step 2: Run impacts** (absolute repo paths):

| Target | Direction | Gate threshold |
|--------|-----------|----------------|
| `BalticReplayHarness` | upstream | CRITICAL expected; confirm no unexpected modules beyond Baltic/tests |
| `GauntletOracleEvaluator` | upstream | Must exist after land; document consumers |
| `CatalogWriteGate` | upstream | Must stay extend-only (no write-path file edits in land set) |
| `DelegationBridge` | upstream | Land set must show **zero** edits to Bridge.cs hotpath |

- [ ] **Step 3: Save** outputs to `production/qa/evidence/gitnexus-gauntlet-land-pre-2026-07-14.log`.

- [ ] **Step 4: Abort Track A code land** if land set includes `DelegationBridge.cs` production edits or CatalogWriteGate write-path changes.

---

### Task A3: Land gauntlet commits onto Release program branch

**Files (expected; confirm via A1):**
- `data/scenarios/gauntlet-*.policy.json`
- `src/ProjectAegis.Data/Catalog/GauntletOracleExpect.cs`
- `src/ProjectAegis.Data/Catalog/GauntletOracleEvaluator.cs`
- `src/ProjectAegis.Data.Tests/Catalog/GauntletOracleEvaluatorTests.cs`
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessLadder*.cs`
- `.github/workflows/gauntlet-oracle.yml`
- `production/qa/gauntlet/**` (select artifacts, not junk)

**Interfaces:**
- Consumes: A1 file list, A2 impact greenlight
- Produces: Release branch contains gauntlet hard-gate + policies

- [ ] **Step 1: Prefer Graphite stack** from gauntlet branch onto base, or cherry-pick ordered commits (`917e716`, `d927684`, and any theater inject deps if missing). Avoid force-push.

- [ ] **Step 2: Resolve conflicts** favoring: fingerprint gates + tier-budget expects; preserve Baltic production hash; no bridge edits.

- [ ] **Step 3: Build**

```bash
dotnet build ProjectAegis.sln -c Release --nologo
# Expected: 0 Error(s)
```

- [ ] **Step 4: Targeted tests**

```bash
dotnet test src/ProjectAegis.Data.Tests --filter "FullyQualifiedName~GauntletOracleEvaluatorTests" -c Release --nologo
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "FullyQualifiedName~LadderInject|FullyQualifiedName~LadderMultiDomain|FullyQualifiedName~LadderNegative|FullyQualifiedName~ReplayGolden" -c Release --nologo
```

Expected: all Passed.

- [ ] **Step 5: Full suite floor**

```bash
dotnet test ProjectAegis.sln -c Release --nologo
# Expected: total passed ≥ 1599, failed = 0 (or document Phase0-only residual with quarantine)
```

- [ ] **Step 6: Local CI dry-run** mirror `gauntlet-oracle.yml` (batch + oracle allPassed + strip fail-closed).

- [ ] **Step 7: Commit/stack** with message citing gate + impact summary; `detect_changes` before finalize.

---

### Task A4: Post-land GitNexus + gate evidence refresh

- [ ] **Step 1:** `node .gitnexus/run.cjs analyze` on landed HEAD; confirm status up-to-date.

- [ ] **Step 2:** Re-run impact on the five CRITICAL hubs; append to `production/qa/evidence/gitnexus-gauntlet-land-post-2026-07-14.log`.

- [ ] **Step 3:** Append **Track A closed** section to gate file or write `production/gate-checks/post-s93-track-a-closeout-2026-07-14.md` with suite counts + SHAs.

---

### Task B1: Spec residual Needed assets (if specs missing)

**Files:**
- Create/modify: `design/assets/specs/` for ASSET-036, 037, 040, 041 as needed
- Modify: `design/assets/asset-manifest.md`

| ID | Name | Category |
|----|------|----------|
| ASSET-036 | Main Menu shell | UI |
| ASSET-037 | Scenario Select | UI |
| ASSET-040 | Policy denial SFX | Audio |
| ASSET-041 | ROE change SFX | Audio |

- [ ] **Step 1:** Read S93 scope boundary + existing C2/store/baltic specs for style.

- [ ] **Step 2:** Author lean specs (paths, dimensions/format, acceptance) — no Addressables bulk strategy expansion.

- [ ] **Step 3:** Bump manifest: Needed → Specced for the four IDs.

- [ ] **Step 4:** Commit `docs(assets): spec ASSET-036/037/040/041 residual Needed`

---

### Task B2: Produce minimal binaries / placeholders for residual assets

**Files:**
- Create: under `production/assets/` (e.g. `ui/`, `audio/`) following S93 layout
- Modify: `design/assets/asset-manifest.md` (Specced → Done or In Production)

- [ ] **Step 1:** UI shells — produce USS/PNG stubs consistent with AegisTokens / C2 style (match S93 quality bar).

- [ ] **Step 2:** Audio — produce short placeholder WAV/OGG or document deferred binary with **Specced** if audio tooling blocked; prefer real short SFX if pipeline exists under `production/assets/`.

- [ ] **Step 3:** Update manifest counts; note umbrellas ASSET-001–003 progress if children complete.

- [ ] **Step 4:** Commit `feat(assets): residual Needed assets wave post-S93 gate`

- [ ] **Step 5:** Standing gates smoke: build + full suite still green (assets-only should not break).

---

### Task C1: Post-editor architecture review refresh

**Files:**
- Create: `docs/architecture/architecture-review-post-s93-2026-07-14.md` (or under `production/qa/`)
- Read: `docs/architecture/architecture.md`, ADRs 013–017, editor gates, control-manifest if present

- [ ] **Step 1:** Run architecture-review workflow (skill `/architecture-review` or structured review): map GDD TR coverage, ADR conflicts, editor surfaces, gauntlet/oracle seams.

- [ ] **Step 2:** Verdict PASS / CONCERNS / FAIL with explicit residual list.

- [ ] **Step 3:** If CONCERNS only on Launch-blocking items already known, document **cleared for Release hold** vs **still blocking Launch**.

- [ ] **Step 4:** Commit `docs(architecture): post-S93 architecture review refresh`

---

### Task D (optional — human gate only): Launch prep unlock

**Do not start unless user explicitly says Launch is in scope.**

**Files:**
- Modify: `production/gate-checks/commercial-launch-execution-gate-TBD.md` → executable checklist with dates
- Possibly: `production/stage.txt` only after human ack

- [ ] **Step 1:** Confirm human ack phrase for Launch.
- [ ] **Step 2:** Walk commercial-launch prerequisites (stage, assets threshold, i18n, store accounts).
- [ ] **Step 3:** Produce Launch gate PASS/FAIL with evidence; only then change stage.

---

### Task E: Gate closeout + dashboard snapshot

**Files:**
- Create: `production/gate-checks/post-s93-concerns-remediation-closeout-2026-07-14.md`
- Optionally update: `docs/reports/dashboard-snapshots/YYYY-MM-DD-pm.md` + HTML

- [ ] **Step 1:** Matrix of gate §7–8 items → Open / Closed / Deferred with evidence paths.

- [ ] **Step 2:** Re-run standing floors; record suite counts ≥ pre-land.

- [ ] **Step 3:** Commit closeout; do **not** claim Launch.

---

## Suggested execution order

```
Task 0 (playbook)
   → A1 inventory → A2 impact → A3 land → A4 post-land
   → B1 specs → B2 produce   (can parallelize with C1 after A3 if no file overlap)
   → C1 architecture review
   → E closeout
   → D only if human Launch ack
```

**Parallelism:** After A3 lands, B1/B2 and C1 are independent (different directories). Use parallel subagents with non-overlapping paths.

## Default success criteria (without Launch)

- [ ] Gauntlet hard-gate + policies present on Release program branch; suite ≥1599/0f; CI dry-run allPassed + strip fail-closed
- [ ] GitNexus fresh @ landed HEAD; CRITICAL impacts documented; zero DelegationBridge hotpath edits
- [ ] ASSET-036/037/040/041 no longer pure Needed without specs (Specced or Done)
- [ ] Architecture review report published (PASS or CONCERNS with Launch-only residuals)
- [ ] Closeout matrix maps every gate §7–8 item
- [ ] `stage.txt` still **Release**

## Verification plan (before claiming done)

1. Full suite + build 0e/0w on landed branch  
2. Gauntlet oracle dry-run (pass + fail-closed strip)  
3. GitNexus status up-to-date; detect_changes risk understood  
4. Asset manifest counts updated  
5. Closeout doc complete  

## Risks

- Cherry-pick conflicts between s93 docs branch and gauntlet branch — resolve with A1 inventory first  
- Suite count may exceed 1599 after gauntlet tests land (monotonic OK; failed must stay 0)  
- Dual GitNexus indexes — always pass absolute repo path  
- Asset audio tooling may block Done — accept Specced + tracked deferral rather than fake binaries  

---

## Self-review

1. **Spec coverage:** All gate §7 blockers and §8 recommendations map to Tracks A–E (Launch = D optional).  
2. **No placeholders:** Paths, commands, asset IDs, and CRITICAL symbols named.  
3. **Consistency:** Release hold preserved; Launch not automatic.
