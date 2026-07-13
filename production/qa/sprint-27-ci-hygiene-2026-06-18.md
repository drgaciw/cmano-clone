# Sprint 27 — CI hygiene / GHA billing advisory (S27-12)

**Date:** 2026-06-18  
**Story:** S27-12 — CI Hygiene / GHA Billing Documentation  
**Verdict:** **ADVISORY** — permanent local-gate fallback; **non-blocking** for Sprint 27 closeout  
**Producer decision:** Permanent local-gate advisory; Buildkite remains merge authority

---

## Executive summary

| Question | Answer |
|----------|--------|
| What blocks merge? | **Buildkite** `buildkite/cmano-clone` — green `build` step required |
| Is GitHub Actions authoritative? | **No** — GHA is **advisory** since S16 billing failure |
| What if Buildkite is unavailable? | Run **`tools/verify-ci-local.ps1`** and attach output to the PR |
| Does this block sprint closeout? | **No** — documentation-only story |

GitHub Actions on private repo `drgaciw/cmano-clone` still aborts in ~3s with the org billing annotation (unchanged since PR #69). That is **not** a product or workflow defect. **Buildkite** runs the same product gate as local dev; **GHA** (including CodeQL) is informational until billing is restored.

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

### Sprint 27 baseline (reference)

Per [qa-plan-sprint-27-2026-06-18.md](qa-plan-sprint-27-2026-06-18.md): trunk `main` @ `ab30d35` — **≥698** solution tests, **ReplayGolden 6/6**, PlayMode smoke green in Buildkite.

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

**Producer ratification (S19-07 → S27):** Treat GHA as **permanent advisory** until billing is restored. Do **not** block merge on billing-aborted GHA checks when Buildkite is green or local gate evidence is attached.

**Billing resolution (org owner, optional):**

1. GitHub → **Settings → Billing and plans** for org/user `drgaciw`
2. Resolve failed payment or raise **Actions spending limit**
3. Re-run `.NET CI` on `main` and confirm jobs execute real steps (not ~3s abort)
4. Align branch protection in GitHub UI — required check remains **`buildkite/cmano-clone`**

No workflow changes are required for S27-12.

---

## Local gate fallback

When Buildkite is unavailable or a contributor needs pre-push parity, use the scripted local gate.

**Script:** [`tools/verify-ci-local.ps1`](../../tools/verify-ci-local.ps1)  
**Parity:** Mirrors [`tools/buildkite/dotnet-ci.sh`](../../tools/buildkite/dotnet-ci.sh) and the Buildkite `build` step.

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

**Bash equivalent:** see command block in [`sprint-19-ci-local-gate-2026-06-08.md`](sprint-19-ci-local-gate-2026-06-08.md) (same steps; baseline counts may differ by sprint).

### PR evidence (when GHA is red or Buildkite skipped)

Attach to the PR body:

1. Link to this doc or [sprint-19-ci-local-gate-2026-06-08.md](sprint-19-ci-local-gate-2026-06-08.md)
2. Commit SHA tested
3. Terminal output showing all five steps **PASS**
4. Note billing blocker if GitHub checks are red

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

## Sprint 27 closeout

| Criterion | Status |
|-----------|--------|
| Evidence doc `production/qa/sprint-27-ci-hygiene-*.md` | **DONE** — this file |
| Buildkite = merge authority documented | **DONE** |
| Local gate fallback documented | **DONE** |
| Non-blocking for closeout | **CONFIRMED** — no pipeline or workflow edits required |

---

## References

- [pr-69-ci-triage-2026-06-04.md](pr-69-ci-triage-2026-06-04.md) — S16 GHA billing root cause
- [sprint-19-ci-local-gate-2026-06-08.md](sprint-19-ci-local-gate-2026-06-08.md) — Option B local gate SOP (S19-07)
- [qa-plan-sprint-27-2026-06-18.md](qa-plan-sprint-27-2026-06-18.md) — lean review mode; S27-12 checklist
- [sprint-25-gitnexus-2026-06-18.md](sprint-25-gitnexus-2026-06-18.md) — prior CI hygiene table (Buildkite blocking)
- [buildkite-ci.md](../../docs/engineering/buildkite-ci.md) — engineering setup
- [story-027-12-ci-hygiene.md](../epics/sprint-27-closeout-devops/story-027-12-ci-hygiene.md) — story acceptance criteria