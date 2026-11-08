# Sprint 35 — CI hygiene / local gate refresh (S35-15)

**Date:** 2026-06-19  
**Story:** S35-15 — CI/Local Gate Refresh + Test Layout Audit (S34-12 carryover)  
**Verdict:** **ADVISORY** — permanent local-gate fallback; **non-blocking** for Sprint 35 closeout  
**Producer decision:** Permanent local-gate advisory; Buildkite remains merge authority (carried from S27-12 → S28-12 → S29-12 → S30-12 → S32-12 → S33-12)

---

## Executive summary

| Question | Answer |
|----------|--------|
| What blocks merge? | **Buildkite** `buildkite/cmano-clone` — green `build` step required |
| Is GitHub Actions authoritative? | **No** — GHA is **advisory** since S16 billing failure |
| What if Buildkite is unavailable? | Run **`tools/verify-ci-local.ps1`** (or bash parity `tools/buildkite/dotnet-ci.sh`) and attach output to the PR |
| Does this block sprint closeout? | **No** — documentation-only story |
| `tests/unit/` studio layout? | **DEFERRED** (6th deferral) — see §Test layout audit |

---

## S34-12 carryover — sixth deferral rationale

| Sprint | Story | Status | Notes |
|--------|-------|--------|-------|
| S33 | S33-12 | **COMPLETE** | Evidence `production/qa/sprint-33-ci-hygiene-2026-06-19.md` |
| S34 | S34-12 | **DEFERRED** | 5th deferral — capacity cut line; no `sprint-34-ci-hygiene-*.md` |
| S35 | S35-15 | **COMPLETE** (this doc) | 6th deferral on `tests/unit/` migration OK; gate script thresholds refreshed |

**Why S35-15 was nice-to-have:** Sprint 35 closeout prioritized S35-01 baseline (1193/1193), feature waves, and gate r2 CONCERNS uplift. S35-15 refreshes stale S33-12 policy comments (≥1046/≥1086) to Sprint 35 floors and documents the studio test-layout gap without blocking closeout.

**S35-15 resolution:** Updates local gate policy to **≥1204 day-1** / **≥1204 closeout** (current solution count). Does **not** migrate `src/*.Tests` to `tests/unit/` — disposition is explicit **deferral** per gate r2 residual #7 and `production/polish-scope-boundary-2026-06-19.md` §Out of Scope.

---

## Sprint 35 baseline (reference)

Per [smoke-sprint-35-baseline-2026-06-19.md](smoke-sprint-35-baseline-2026-06-19.md) (S35-01 @ `8de98b1`) and S35-15 verify:

| Gate | Expected |
|------|----------|
| Full solution (default `dotnet test ProjectAegis.sln`) | **≥1204** PASS — policy floor (current sln count) |
| Full solution (S35-01 day-1 @ trunk) | **≥1193** PASS — **1193/1193** @ `8de98b1` |
| Full solution (S35-15 verify @ working tree) | **≥1204** PASS — **1204/1204** (feature merges since day-1) |
| ReplayGolden | **6/6** PASS (`FullyQualifiedName~ReplayGoldenSuiteTests`) |
| PlayMode smoke | **17/17** PASS (`FullyQualifiedName~PlayModeSmokeHarnessTests`) |
| **Closeout target (S35-15)** | **≥1204/1204** |

---

## Test layout audit — gate r2 residual #7

**Authority:** [production-to-polish-2026-06-19-r2.md](../gate-checks/production-to-polish-2026-06-19-r2.md) residual #7; [polish-scope-boundary-2026-06-19.md](../polish-scope-boundary-2026-06-19.md) §Out of Scope (studio template gaps).

### Studio template (expected)

Per `.claude/docs/coding-standards.md` and `/gate-check`:

| Path | Purpose |
|------|---------|
| `tests/unit/[system]/` | Logic/unit tests — **BLOCKING** evidence path |
| `tests/integration/[system]/` | Multi-system integration tests — **BLOCKING** evidence path |
| `tests/regression/` | Golden replay artifacts (already present) |

### Current state (actual)

| Location | Contents |
|----------|----------|
| `src/ProjectAegis.Sim.Tests/` | 279 unit tests (co-located assembly) |
| `src/ProjectAegis.Delegation.Tests/` | 235 unit tests |
| `src/ProjectAegis.Data.Tests/` | 398 unit tests |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/` | 245 tests (adapter + ReplayGolden + PlayMode smoke) |
| `src/ProjectAegis.MissionEditor.Cli.Tests/` | 42 unit tests |
| `src/ProjectAegis.Data.Excel.Tests/` | 5 unit tests |
| `tests/regression/` | Replay golden baselines (`replay-golden-baltic-*.txt`) |
| `tests/unit/` | **ABSENT** |
| `tests/integration/` | **ABSENT** |

**Total automated tests:** **1204/1204 PASS** — all in `src/*.Tests` projects referenced by `ProjectAegis.sln`. Engineering coverage is strong; **audit path** does not match studio folder convention.

### Gap analysis

| Dimension | Studio template | Project Aegis (brownfield) |
|-----------|-----------------|---------------------------|
| Folder layout | `tests/unit/`, `tests/integration/` | `src/*.Tests/` co-located with production assemblies |
| Solution discovery | Convention-based paths | Explicit `.csproj` entries in `ProjectAegis.sln` |
| Regression goldens | `tests/regression/` | **Aligned** — goldens already under `tests/regression/` |
| Gate-check checklist | `tests/unit/` exists | **FAIL** on path existence; **PASS** on test execution |
| Migration cost | N/A | High — 6 assemblies, ~1200 tests, CI filters, Unity adapter paths |

### Recommended disposition: **DEFER** (6th deferral)

| Option | Assessment |
|--------|------------|
| **Alias** (README/symlink pointers) | Low effort; does not satisfy gate-check path existence; cosmetic only |
| **Migrate** (move csproj + sources to `tests/unit/`) | Correct long-term; **out of scope** for S35 per polish-scope-boundary; risks CI/Unity path churn |
| **Defer** | **SELECTED** — document gap; carry to S36+ dedicated hygiene story; non-blocking for Production→Polish |

**Carry-forward:** Schedule `tests/unit/` scaffold + incremental migration (or ADR accepting co-located .NET test layout) in Sprint 36 closeout hygiene. Do **not** claim studio layout complete until paths exist.

---

## Local gate fallback

**Script:** [`tools/verify-ci-local.ps1`](../../tools/verify-ci-local.ps1)  
**Bash parity:** [`tools/buildkite/dotnet-ci.sh`](../../tools/buildkite/dotnet-ci.sh) — **use when `pwsh` unavailable on Linux host**  
**Parity:** Mirrors Buildkite `build` step (Release configuration).

### Steps (in order)

| Step | Command |
|------|---------|
| 1. Restore | `dotnet restore ProjectAegis.sln` |
| 2. Release build | `dotnet build ProjectAegis.sln -c Release --no-restore` |
| 3. Full solution test | `dotnet test ProjectAegis.sln -c Release --no-build -v minimal` |
| 4. Replay golden | `dotnet test` … `--filter FullyQualifiedName~ReplayGoldenSuiteTests` |
| 5. PlayMode smoke | `dotnet test` … `--filter FullyQualifiedName~PlayModeSmokeHarnessTests` |

**Bash equivalent (repo root) — Linux host without `pwsh`:**

```bash
export PATH="/home/username01/.dotnet:$PATH"
bash tools/buildkite/dotnet-ci.sh
```

**Story verify fallback:**

```bash
pwsh -File tools/verify-ci-local.ps1 2>/dev/null || bash tools/buildkite/dotnet-ci.sh
```

---

## Local gate evidence — S35-15 verification

**Commit:** `8de98b1` (S35-01 indexed trunk; working tree verify)  
**Host:** Linux agent; `pwsh` unavailable — bash parity noted; evidence from story verify command.

### Default solution test (story verify command)

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test ProjectAegis.sln -v minimal
```

```
Passed!  - Failed:     0, Passed:   279, Skipped:     0, Total:   279 - ProjectAegis.Sim.Tests.dll
Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5 - ProjectAegis.Data.Excel.Tests.dll
Passed!  - Failed:     0, Passed:   235, Skipped:     0, Total:   235 - ProjectAegis.Delegation.Tests.dll
Passed!  - Failed:     0, Passed:    42, Skipped:     0, Total:    42 - ProjectAegis.MissionEditor.Cli.Tests.dll
Passed!  - Failed:     0, Passed:   245, Skipped:     0, Total:   245 - ProjectAegis.Delegation.UnityAdapter.Tests.dll
Passed!  - Failed:     0, Passed:   398, Skipped:     0, Total:   398 - ProjectAegis.Data.Tests.dll
```

**Total:** **1204/1204 PASS** (0 failed). Meets day-1 floor **≥1204** and closeout target **≥1204**.

| Assembly | Passed | Δ vs S35-01 |
|----------|--------|-------------|
| ProjectAegis.Sim.Tests | 279 | +3 |
| ProjectAegis.Delegation.Tests | 235 | — |
| ProjectAegis.MissionEditor.Cli.Tests | 42 | — |
| ProjectAegis.Data.Excel.Tests | 5 | — |
| ProjectAegis.Delegation.UnityAdapter.Tests | 245 | +5 |
| ProjectAegis.Data.Tests | 398 | +3 |
| **Total** | **1204** | **+11** |

---

## CI layer matrix

| Layer | Status | Blocks merge? | Notes |
|-------|--------|---------------|-------|
| **Buildkite** `buildkite/cmano-clone` | **BLOCKING** | **Yes** | [`dotnet-ci.sh`](../../tools/buildkite/dotnet-ci.sh) via [`.buildkite/pipeline.yml`](../../.buildkite/pipeline.yml) |
| **Local gate** `verify-ci-local.ps1` | **ACTIVE FALLBACK** | **Yes** (evidence-based) | Bash parity via `tools/buildkite/dotnet-ci.sh` |
| **GitHub Actions** `.NET CI` | **ADVISORY** | **No** | Billing abort since S16 |
| **Studio `tests/unit/` path** | **DEFERRED** | **No** | 6th deferral; engineering gate green @ 1204/1204 |

---

## Sprint 35 closeout (S35-15)

| Criterion | Status |
|-----------|--------|
| Evidence doc `production/qa/sprint-35-ci-hygiene-*.md` | **DONE** — this file |
| `verify-ci-local.ps1` policy pointer updated (day-1 ≥1204; closeout ≥1204) | **DONE** |
| `dotnet-ci.sh` bash parity policy pointer updated | **DONE** |
| Bash fallback when `pwsh` unavailable documented | **DONE** |
| `tests/unit/` layout audit with unambiguous disposition | **DONE** — **DEFER** (6th deferral) |
| S34-12 carryover / deferral rationale documented | **DONE** |
| Non-blocking for closeout | **CONFIRMED** — no pipeline or workflow edits |
| ZERO touch `DelegationBridge.cs` | **CONFIRMED** — doc-only story |

---

## References

- [sprint-33-ci-hygiene-2026-06-19.md](sprint-33-ci-hygiene-2026-06-19.md) — prior sprint CI hygiene (S33-12)
- [smoke-sprint-35-baseline-2026-06-19.md](smoke-sprint-35-baseline-2026-06-19.md) — S35-01 day-1 1193/1193 baseline
- [production-to-polish-2026-06-19-r2.md](../gate-checks/production-to-polish-2026-06-19-r2.md) — gate r2 residual #7
- [polish-scope-boundary-2026-06-19.md](../polish-scope-boundary-2026-06-19.md) — `tests/unit/` out of scope
- [story-035-15-ci-hygiene.md](../epics/sprint-35-closeout-devops/story-035-15-ci-hygiene.md) — story acceptance criteria
- [buildkite-ci.md](../../docs/engineering/buildkite-ci.md) — engineering setup