# Sprint 32 — CI hygiene / GHA billing advisory (S32-12)

**Date:** 2026-06-19  
**Story:** S32-12 — CI/Local Gate Refresh (S31-12 carryover)  
**Verdict:** **ADVISORY** — permanent local-gate fallback; **non-blocking** for Sprint 32 closeout  
**Producer decision:** Permanent local-gate advisory; Buildkite remains merge authority (carried from S27-12 → S28-12 → S29-12 → S30-12)

---

## Executive summary

| Question | Answer |
|----------|--------|
| What blocks merge? | **Buildkite** `buildkite/cmano-clone` — green `build` step required |
| Is GitHub Actions authoritative? | **No** — GHA is **advisory** since S16 billing failure |
| What if Buildkite is unavailable? | Run **`tools/verify-ci-local.ps1`** (or bash parity `tools/buildkite/dotnet-ci.sh`) and attach output to the PR |
| Does this block sprint closeout? | **No** — documentation-only story |

GitHub Actions on private repo `drgaciw/cmano-clone` still aborts in ~3s with the org billing annotation (unchanged since PR #69). That is **not** a product or workflow defect. **Buildkite** runs the same product gate as local dev; **GHA** (including CodeQL) is informational until billing is restored.

---

## S31-12 carryover — third deferral rationale

| Sprint | Story | Status | Notes |
|--------|-------|--------|-------|
| S31 | S31-12 | **DEFERRED** | Nice-to-have; 12/13 stories landed; QA sign-off `production/qa/qa-signoff-sprint-31-2026-06-18.md` |
| S32 | S32-12 | **COMPLETE** (this doc) | Explicit carryover from S31-12; refreshes stale S30-12 policy comments in local gate scripts |
| S33 | S33-12 | Backlog | 4th deferral acceptable per sprint cut line if S33 capacity tight |

**Why S31-12 deferred:** Sprint 31 closeout prioritized S31-13 hygiene (1006/1006), replay 6/6, and GitNexus indexing. S31-12 was `nice-to-have` doc-only work; `tools/verify-ci-local.ps1` still pointed at S30-12 thresholds (≥878/≥918).

**S32-12 resolution:** Updates local gate policy to **≥1006 day-1** / **≥1046 closeout** per [qa-plan-sprint-32-2026-11-13.md](qa-plan-sprint-32-2026-11-13.md) and [sprint-32-release-train-combat-phase6-platform-phase-f.md](../sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md). Does **not** gate S32-13 closeout hygiene.

---

## Merge authority — Buildkite

**Primary blocking CI:** Buildkite pipeline `buildkite/cmano-clone`.

| Artifact | Role |
|----------|------|
| [`.buildkite/pipeline.yml`](../../.buildkite/pipeline.yml) | Pipeline definition — Graphite optimizer, build/test, Gitleaks, Baltic replay (main), GitNexus PR/reindex |
| [`tools/buildkite/dotnet-ci.sh`](../../tools/buildkite/dotnet-ci.sh) | Core product gate invoked by `agent-dotnet-ci.sh` on the **`:hammer: Build and test`** step |
| [`docs/engineering/buildkite-ci.md`](../../docs/engineering/buildkite-ci.md) | Human setup, branch protection, secrets |

### Buildkite product gate (blocking)

The `build` step runs [`tools/buildkite/dotnet-ci.sh`](../../tools/buildkite/dotnet-ci.sh):

1. `dotnet restore ProjectAegis.sln`
2. `dotnet build ProjectAegis.sln -c Release --no-restore`
3. `dotnet test ProjectAegis.sln -c Release --no-build -v minimal` — full solution
4. `dotnet test` — filter `FullyQualifiedName~ReplayGoldenSuiteTests`
5. `dotnet test` — filter `FullyQualifiedName~PlayModeSmokeHarnessTests`

Other pipeline steps are **soft-fail** or branch-scoped (Gitleaks, Graphite optimizer, GitNexus PR analysis, Baltic replay on `main`). A red **`build`** step blocks merge.

### Sprint 32 baseline (reference)

Per [qa-plan-sprint-32-2026-11-13.md](qa-plan-sprint-32-2026-11-13.md) and S32-01 day-1 @ `d3db76db`:

| Gate | Expected |
|------|----------|
| Full solution (default `dotnet test ProjectAegis.sln`) | **≥1006** PASS — **1006/1006** @ `d3db76db` (S32-01 day-1) |
| Full solution (Release CI parity) | **≥1006** PASS — **1073/1073** @ S32-12 verify (working tree; feature merges landed) |
| ReplayGolden | **6/6** PASS (`FullyQualifiedName~ReplayGoldenSuiteTests`) |
| PlayMode smoke | **17/17** PASS (`FullyQualifiedName~PlayModeSmokeHarnessTests`) |
| **Closeout target (S32-13)** | **≥1046/1046** — +40 tests from S32 feature merges |

Sprint 32 day-1 baseline was **1006/1006** @ `d3db76db` (S32-01). Closeout hygiene (S32-13) raises the floor to **≥1046** after should-have and must-have feature merges land. S32-12 verification run shows **1073/1073** — above both thresholds.

---

## GitHub Actions — advisory (billing open since S16)

**Root triage:** [pr-69-ci-triage-2026-06-04.md](pr-69-ci-triage-2026-06-04.md) (S16; Sprint 19 status update appended 2026-06-08).

| Symptom | Interpretation |
|---------|----------------|
| All GHA jobs fail in ~3s | **Billing / spending limit** — jobs never reach checkout |
| Annotation: *recent account payments have failed or your spending limit needs to be increased* | Org/account issue on private repo — **not** a code fail |
| `.NET CI` → `build_test` red | **Advisory** — superseded by Buildkite for merge decisions |
| `CodeQL (C#)` / `CodeQL (JS/TS)` red | **Advisory** — `continue-on-error: true` when running; billing-blocked when not |
| Graphite / GitNexus / Gitleaks GHA workflows red | Same billing gate — **do not** chase as product defects |

**Producer ratification (S19-07 → S27 → S28 → S29 → S30 → S32):** Treat GHA as **permanent advisory** until billing is restored. Do **not** block merge on billing-aborted GHA checks when Buildkite is green or local gate evidence is attached.

**Billing resolution (org owner, optional):**

1. GitHub → **Settings → Billing and plans** for org/user `drgaciw`
2. Resolve failed payment or raise **Actions spending limit**
3. Re-run `.NET CI` on `main` and confirm jobs execute real steps (not ~3s abort)
4. Align branch protection in GitHub UI — required check remains **`buildkite/cmano-clone`**

No workflow changes are required for S32-12.

---

## Local gate fallback

When Buildkite is unavailable or a contributor needs pre-push parity, use the scripted local gate.

**Script:** [`tools/verify-ci-local.ps1`](../../tools/verify-ci-local.ps1)  
**Bash parity:** [`tools/buildkite/dotnet-ci.sh`](../../tools/buildkite/dotnet-ci.sh)  
**Parity:** Mirrors Buildkite `build` step (Release configuration).

### Steps (in order)

| Step | Command |
|------|---------|
| 1. Restore | `dotnet restore ProjectAegis.sln` |
| 2. Release build | `dotnet build ProjectAegis.sln -c Release --no-restore` |
| 3. Full solution test | `dotnet test ProjectAegis.sln -c Release --no-build -v minimal` |
| 4. Replay golden | `dotnet test` … `--filter FullyQualifiedName~ReplayGoldenSuiteTests` |
| 5. PlayMode smoke | `dotnet test` … `--filter FullyQualifiedName~PlayModeSmokeHarnessTests` |

**One-liner (PowerShell, repo root):**

```powershell
.\tools\verify-ci-local.ps1
```

**Bash equivalent (repo root) — use when `pwsh` unavailable:**

```bash
export PATH="/home/username01/.dotnet:$PATH"
bash tools/buildkite/dotnet-ci.sh
```

**Story verify fallback:**

```bash
pwsh -File tools/verify-ci-local.ps1 2>/dev/null || bash tools/buildkite/dotnet-ci.sh
```

**PATH:** ensure `dotnet` is on PATH, e.g. `export PATH="/home/username01/.dotnet:$PATH"`.

### PR evidence (when GHA is red or Buildkite skipped)

Attach to the PR body:

1. Link to this doc or [sprint-30-ci-hygiene-2026-06-18.md](sprint-30-ci-hygiene-2026-06-18.md)
2. Commit SHA tested
3. Terminal output showing all five steps **PASS**
4. Note billing blocker if GitHub checks are red

---

## Local gate evidence — S32-12 verification @ `d3db76db`

**Commit:** `d3db76db` — S32-01 day-1 trunk (1006/1006 sln @ baseline smoke)  
**Host:** Linux agent; `pwsh` unavailable — evidence from bash parity script (same steps as `verify-ci-local.ps1`).

### Default solution test (story verify command)

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test ProjectAegis.sln -v minimal
```

```
Passed!  - Failed:     0, Passed:   264, Skipped:     0, Total:   264 - ProjectAegis.Sim.Tests.dll
Passed!  - Failed:     0, Passed:   232, Skipped:     0, Total:   232 - ProjectAegis.Delegation.Tests.dll
Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5 - ProjectAegis.Data.Excel.Tests.dll
Passed!  - Failed:     0, Passed:    36, Skipped:     0, Total:    36 - ProjectAegis.MissionEditor.Cli.Tests.dll
Passed!  - Failed:     0, Passed:   201, Skipped:     0, Total:   201 - ProjectAegis.Delegation.UnityAdapter.Tests.dll
Passed!  - Failed:     0, Passed:   335, Skipped:     0, Total:   335 - ProjectAegis.Data.Tests.dll
```

**Total:** **1073/1073 PASS** (0 failed). Exceeds day-1 floor **≥1006** and closeout target **≥1046**.

### Release CI parity (`tools/buildkite/dotnet-ci.sh`)

```
Passed!  - Failed:     0, Passed:   264 - ProjectAegis.Sim.Tests.dll
Passed!  - Failed:     0, Passed:    36 - ProjectAegis.MissionEditor.Cli.Tests.dll
Passed!  - Failed:     0, Passed:   232 - ProjectAegis.Delegation.Tests.dll
Passed!  - Failed:     0, Passed:   201 - ProjectAegis.Delegation.UnityAdapter.Tests.dll
Passed!  - Failed:     0, Passed:     5 - ProjectAegis.Data.Excel.Tests.dll
Passed!  - Failed:     0, Passed:   335 - ProjectAegis.Data.Tests.dll
Passed!  - Failed:     0, Passed:     6 - ReplayGoldenSuiteTests filter
Passed!  - Failed:     0, Passed:    17 - PlayModeSmokeHarnessTests filter
=== PASS ===
```

**Release full solution:** **1073/1073 PASS**; **ReplayGolden 6/6**; **PlayMode smoke 17/17**.

---

## CI layer matrix

| Layer | Status | Blocks merge? | Notes |
|-------|--------|---------------|-------|
| **Buildkite** `buildkite/cmano-clone` | **BLOCKING** | **Yes** | [`dotnet-ci.sh`](../../tools/buildkite/dotnet-ci.sh) via [`.buildkite/pipeline.yml`](../../.buildkite/pipeline.yml) |
| **Local gate** `verify-ci-local.ps1` | **ACTIVE FALLBACK** | **Yes** (evidence-based) | When Buildkite cannot run; same commands as Buildkite `build` |
| **GitHub Actions** `.NET CI` | **ADVISORY** | **No** | Billing abort since S16 — [pr-69-ci-triage](pr-69-ci-triage-2026-06-04.md) |
| **GitHub Actions** CodeQL (C#/JS) | **ADVISORY** | **No** | Soft-fail when running; red when billing blocks |
| **GitHub Actions** Graphite / GitNexus / Gitleaks | **ADVISORY** | **No** | Superseded by Buildkite steps where applicable |

---

## Agent rule

Do **not** treat skipped or billing-aborted GitHub checks as product failures. **Buildkite green** or **local gate PASS** with attached evidence is the merge-quality signal.

---

## Sprint 32 closeout (S32-12)

| Criterion | Status |
|-----------|--------|
| Evidence doc `production/qa/sprint-32-ci-hygiene-*.md` | **DONE** — this file |
| Buildkite = merge authority documented | **DONE** |
| Local gate fallback documented (day-1 ≥1006; closeout target ≥1046; ReplayGolden step) | **DONE** |
| S31-12 carryover / deferral rationale documented | **DONE** — §S31-12 carryover |
| Bash fallback when `pwsh` unavailable | **DONE** — §Local gate fallback |
| `verify-ci-local.ps1` policy reference refreshed | **DONE** — S32-12 policy pointer |
| Non-blocking for closeout | **CONFIRMED** — no pipeline or workflow edits required |
| ZERO touch `DelegationBridge.cs` | **CONFIRMED** — doc-only story |

---

## References

- [sprint-30-ci-hygiene-2026-06-18.md](sprint-30-ci-hygiene-2026-06-18.md) — prior sprint CI hygiene (S30-12)
- [sprint-29-ci-hygiene-2026-06-18.md](sprint-29-ci-hygiene-2026-06-18.md) — prior sprint CI hygiene (S29-12)
- [pr-69-ci-triage-2026-06-04.md](pr-69-ci-triage-2026-06-04.md) — S16 GHA billing root cause
- [sprint-19-ci-local-gate-2026-06-08.md](sprint-19-ci-local-gate-2026-06-08.md) — Option B local gate SOP (S19-07)
- [qa-plan-sprint-32-2026-11-13.md](qa-plan-sprint-32-2026-11-13.md) — lean review mode; S32-12 checklist; closeout ≥1046
- [smoke-sprint-32-baseline-2026-06-18.md](smoke-sprint-32-baseline-2026-06-18.md) — S32-01 day-1 1006/1006 baseline
- [story-031-12-ci-hygiene.md](../epics/sprint-31-closeout-devops/story-031-12-ci-hygiene.md) — deferred S31-12 carryover source
- [buildkite-ci.md](../../docs/engineering/buildkite-ci.md) — engineering setup
- [story-032-12-ci-hygiene.md](../epics/sprint-32-closeout-devops/story-032-12-ci-hygiene.md) — story acceptance criteria