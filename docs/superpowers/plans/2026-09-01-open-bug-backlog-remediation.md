# Open Bug Backlog Remediation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring `production/qa/bugs/` back to truth — verify and close the 16 reports whose fixes already landed on `main`, get a human decision on the one genuine open item, close the residual-risk gaps the fixed reports themselves recommended, and add a guard so the ledger cannot silently rot again.

**Architecture:** This is a ledger-reconciliation plan, not a bug-fixing plan. A read-only audit of `main` @ `81831e76` (2026-09-01) found that 14 of the 15 reports marked `Open` already have their fix and named regression test on `main`, the 2 `Fixed (pending review)` reports are merged, and the 3 header-less gauntlet reports are `CLOSED` with their content on disk. Only `BUG-scoring-penalises-roe-correct-refusals` is genuinely open, and it is a design decision. Work is therefore: (1) verify + flip statuses per the `bug-report` skill's Verify/Close protocol, (2) a decision gate, (3) three small TDD follow-ups the reports asked for, (4) a `bug-ledger-check.sh` guard wired into the bash CI parity script.

**Tech Stack:** .NET 8.0.400 (`global.json`), xUnit (`ProjectAegis.Data.Tests`, `ProjectAegis.Sim.Tests`, `ProjectAegis.MissionEditor.Cli.Tests`), NUnit 4 (`ProjectAegis.Delegation.Tests`, `ProjectAegis.Delegation.UnityAdapter.Tests`), bash + `rg`, Python 3 (gauntlet tooling).

## Global Constraints

- Replay golden hash `17144800277401907079` must remain unchanged (`grep -r "17144800277401907079" tests/ data/`).
- `DelegationBridge.cs` is zero-touch; `CatalogWriteGate` is extend-only; `baltic-v3-*` goldens are never touched.
- Determinism: no `Random.Shared`, `DateTime.UtcNow`, or unordered `Dictionary` enumeration on any path that feeds `OrderLog` / replay fingerprints.
- Test floor is monotonic: the suite is **3,062 tests / 0 failures** on `main` today (AGENTS.md still says ≥1638; never regress below the current count).
- Test naming: `[SystemUnderTest]Tests.cs`, methods `Scenario_expected` in snake_case; test project frameworks differ (see Tech Stack) — do not mix `[Fact]` and `[Test]`.
- Conventional Commits, one commit per logical change, story/bug ID in the body.
- **Before every planned commit:** run GitNexus `detect_changes()` for the files about to be staged, record affected symbols/processes in the PR evidence, and stop to reconcile any unexpected scope before `git add`. This applies to every commit block below, including docs-only tasks.
- `production/qa/bugs/*.md` status transitions follow `.claude/skills/bug-report/SKILL.md` Phase 2C (Verify) and 2D (Close). Setting `Status: Closed` requires human approval; `Verified Fixed` does not.
- Cloud VM verification prerequisite: `export PATH="$HOME/.dotnet:$PATH"` and `bash tools/copy-delegation-assemblies.sh` before `dotnet test ProjectAegis.sln`, otherwise `UnityPluginEpicATypesTests` fails on a fresh checkout (plugin DLLs are gitignored). The PowerShell gate `tools/verify-ci-local.ps1` is not runnable on cloud (no `pwsh`); use `bash tools/buildkite/dotnet-ci.sh`.

---

## 1. Triage results (verified against `main` @ `81831e76`, 2026-09-01)

### 1.1 Headline

| Metric | Value |
|--------|-------|
| Bug report files | 31 |
| Header says `Open` | 15 |
| …of which fix + named regression test already on `main` | **14** |
| …of which genuinely open | **1** (design question) |
| Header says `Fixed (pending review)` — actually merged | 2 |
| No standard header, body says `CLOSED`, content on `main` | 3 |
| Already `Fixed` / `Verified Fixed` / `FIXED` in header | 11 |
| Full suite on `main` after plugin copy step | 3,062 passed / 0 failed (build 21 s, tests 28 s) |

The backlog is a **ledger-integrity problem**, not an engineering backlog. Every stale report was filed from an isolated QA worktree (`qa-loop-*`, `qa-r2-*`) with `Status: Open (fix in this commit…)`; the fix merged, the worktree branch was deleted, and nobody ran `/bug-report verify`. None of the named branches exist locally or on the remote any more.

### 1.2 Table A — reports marked `Open` whose fix is on `main` (14)

Every regression test below was located in the tree and is part of the 3,062 passing tests.

| # | ID | Sev | Fix surface | Regression test (file → method) | Fix commit |
|---|----|-----|-------------|----------------------------------|-----------|
| 1 | `BUG-undo-snapshot-drops-untouched-canonical-sections` | S1 | `ScenarioUndoStackStore.CloneDocument` | `src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioUndoCliTests.cs` → `scenario_undo_preserves_sides_and_support_mission_fields_not_touched_by_the_mutation` | `8407a1a0` |
| 2 | `BUG-phantom-undo-snapshot-on-rejected-mission-mutation` | S2 | `ScenarioDocumentEditor.CaptureUndoSnapshot`/`PersistUndoSnapshot` + 8 mission CLI commands | `ScenarioUndoCliTests.cs` → `scenario_undo_does_not_push_snapshot_on_mission_not_found_rejected_delete` | `c3484047` |
| 3 | `BUG-roe-holdfire-silently-overridden` | S2 | `MvpEngagementResolver` (`ResolvedPolicySnapshotMarker = 1`) | `src/ProjectAegis.Sim.Tests/Engage/MvpEngagementResolverTests.cs` → `Resolver_resolve_policy_denies_hold_fire_even_when_evaluator_default_allows` | `5c254e9b` |
| 4 | `BUG-kill-transition-order-nondeterminism` | S2 | `PdDetectionContactSimulator.ApplyTargetKill` | `src/ProjectAegis.Sim.Tests/Sensors/PdContactKillTests.cs` → `Kill_with_multiple_contacts_on_same_target_emits_transitions_in_ordinal_contact_order` | `b59c931b` |
| 5 | `BUG-stale-loss-order-nondeterminism` | S2 | `PdDetectionContactSimulator.EmitStaleLosses` | `src/ProjectAegis.Sim.Tests/Sensors/PdContactStaleTests.cs` → `Stale_loss_with_multiple_simultaneous_contacts_emits_transitions_in_ordinal_contact_order` | `27a3e404` |
| 6 | `BUG-targetregistry-duplicate-target-key` | S2 | `TargetRegistry.Register` (`_byTarget` guard) | `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/TargetRegistryTests.cs` → `RegisterUnit_with_duplicate_target_key_throws_instead_of_corrupting_registry` (+2 siblings) | `7c3b68d8` |
| 7 | `BUG-primary-blueforce-target-stale-not-recomputed` | S2 | `PdDetectionContactSimulator.EmitStaleLosses`/`RecomputePrimary` | `src/ProjectAegis.Sim.Tests/Sensors/PdContactPrimaryBlueForceStaleTests.cs` → `Stale_loss_of_only_blue_force_contact_clears_primary_blue_force_target` | `a21c39d8` |
| 8 | `BUG-fingerprint-negative-zero` | S2 | `FingerprintFloat.NormalizeNegativeZero` | `src/ProjectAegis.Delegation.Tests/Decision/FingerprintFloatNegativeZeroTests.cs` → `FuelBurn_fingerprint_treats_negative_zero_delta_same_as_positive_zero` | `473a0079` |
| 9 | `BUG-scenario-contacts-shadowed-by-detection` | S2 | `BalticReplayHarness.RunCore` (detection and `ContactSeeds` now both run) | `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessContactTests.cs` → `Detection_plus_contacts_emits_scripted_appearAtTick_contact_change` | `40aa1b93` (#572) |
| 10 | `BUG-forge-scorecard-filename-vs-policy-id` | S2 | `tools/qa-gauntlet/forge_scorecard.py` (`sid = policy.get("id") or …`) | none (Python tooling) — verify by grep | `8422a34b` |
| 11 | `BUG-catalog-release-diff-positional-args-shift` | S2 | `Program.RunCatalogReleaseDiff` via `CliArgParser.GetPositional` | `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogReleaseDiffCliArgsTests.cs` → `catalog_release_diff_positional_invocation_resolves_same_diff_as_flag_invocation` | `671d3577` |
| 12 | `BUG-c2-graph-highlight-stale-selection` | S3 | `C2PresentationController.SelectFriendlyUnit`/`SelectHostileContact` → `ClearGraphSurfacing()` | `src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/C2PresentationControllerTests.cs` → `SelectHostileContact_clears_stale_graph_highlights_from_previous_unit` | `7c043e28` |
| 13 | `BUG-catalog-emcon-tables-empty` | S3 | data: `assets/data/catalog/baltic_patrol.db` `platform_emcon` / `catalog_staging_emcon` now 1,611 rows each | none (data) — verify by row count | `84684958` (#571) |
| 14 | `BUG-catalog-report-databasepath-misreport` | S3 | `CatalogKillChainReportCommand`, `CatalogLinkReportCommand`, `CatalogDependencyGraphCommand` (`out resolvedDatabasePath`) | `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogKillChainReportCommandTests.cs` → `KillChain_reported_database_path_reflects_actual_source_when_input_path_missing` | `0b88597c` |

### 1.3 Table B — genuinely open (1)

| ID | Sev | Nature | Decision needed |
|----|-----|--------|-----------------|
| `BUG-scoring-penalises-roe-correct-refusals` | S3 / P3 | `LossesScoringProjection.Project` applies a flat `-5` per `PolicyDenialRecord` regardless of `FireAbortReason`, so restraint-by-design scenarios score as failures (`gauntlet-t2-escort-passive` = −200 for doing what its name says) | Pick one of the report's options 1–4 (Task 3) |

### 1.4 Table C — other states

| State | IDs | Action |
|-------|-----|--------|
| `Fixed (pending review)` — merged on `main` | `BUG-magazine-cumulative-overcapacity` (`64462c65`, test `Cumulative_over_capacity_across_weapon_types_in_same_mount_is_flagged_as_error`), `BUG-magazine-negative-quantity-masks-overcapacity` (`5aad6627`, test `Negative_magazine_quantity_is_rejected_and_cannot_mask_cumulative_overcapacity`) — both in `src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookValidatorTests.cs` | Verify + close in Task 1 |
| No standard header, body `CLOSED` 2026-07-31 | `BUG-gauntlet-emcon-dimension-not-exercised`, `BUG-oracle-blindspot-03-salvo-off-by-one`, `BUG-oracle-blindspot-05-contact-lifecycle-skip` | Leave content; add the standard header block in Task 1 so `bug-ledger-check.sh` can parse them |
| Already `Fixed` / `Verified Fixed` / `FIXED` | `BUG-double-take-control-drops-queued-orders`, `BUG-emcon-posture-case-sensitive-default`, `BUG-engagement-resolver-shooter-liveness`, `BUG-fuelledger-negative-delta-overfill`, `BUG-logistics-fuel-fraction-validation-crash-no-burn-model`, `BUG-losses-scoring-side-unaware`, `BUG-mine-domain-legacy-gate-blocks-all-launches`, `BUG-missioncontacttargetclass-domain-filter-broken`, `BUG-t2-escort-passive-emcon-claim-unimplemented`, `BUG-wra-range-abort-reason-mismapped`, `BUG-zero-scored-candidate-still-selectable` | No change (optionally close via `/bug-report close` in a later sweep) |

### 1.5 Systemic findings (bug-triage deviation check)

- **Hot spot — `PdDetectionContactSimulator` (3 S2 bugs, one root-cause class).** `ApplyTargetBdaLost`, `ApplyTargetKill`, and `EmitStaleLosses` each emitted transitions in `Dictionary` insertion order until fixed one at a time. All three now use `.OrderBy(id => id, StringComparer.Ordinal)`. Two reports explicitly ask for an audit of other lifecycle emitters → Task 4a.
- **Hot spot — scenario undo (S1 + S2, hand-maintained clone).** `CloneDocument` silently dropped every DTO field added after it was written. It now copies all 10 `ScenarioDocumentDto` and 10 `ScenarioMissionDto` properties, but nothing stops the next new field from being dropped → Task 4b (tripwire test).
- **Process defect — ledger rot.** 16 of 31 reports carry a status that has been false for 4–8 weeks. Root cause: QA-loop worktrees file "fix in this commit" reports; the merge PR never flips them; the `bug-report verify` step is manual and skipped → Task 2 (guard) + Task 1 (reconcile).
- **Verification trap on fresh checkouts.** The AGENTS.md gate omits `tools/copy-delegation-assemblies.sh`, so "0 failures" is unattainable on a cloud VM without an undocumented step. Folded into every task's verification steps here; a permanent fix (add to `.cursor/cloud-install.sh` or make the test skip) belongs to the separate repo-hygiene PR, not this plan.
- **Stale skill text.** `.claude/skills/qa-gauntlet/SKILL.md` lines 180 and 194 still tell scenario authors to consult a `CatalogEmcon` "profile" (the emcon-tables report's option 2); the tables are now populated → Task 4c.

---

## 2. Execution routing (cost)

| Task | Nature | Recommended model tier | Why |
|------|--------|------------------------|-----|
| 1 Reconcile ledger | Run named tests, edit 19 markdown headers, one triage report | Sonnet 5 / Composer | Mechanical; zero design judgement |
| 2 Ledger guard script | ~60-line bash + one CI hook + one AGENTS.md line | Sonnet 5 | Small, fully specified below |
| 3 Scoring decision | Human decision, then (options 1/2 only) a scoring change + envelope regen | Human, then Opus 5 / Fable 5.1 | Touches gauntlet oracle semantics; needs impact analysis |
| 4a Ordering audit | Read-heavy determinism review of transition emitters | Opus 5 / Fable 5.1 | Replay-hash-adjacent; reasoning over call graph |
| 4b Clone tripwire test | One xUnit test, fully specified | Sonnet 5 | Code given verbatim below |
| 4c Skill text fix | Two-line doc edit | Haiku / Sonnet | Trivial |

Run Task 1 and Task 2 in one session (shared context); Tasks 4a–4c are independent and can be dispatched in parallel worktrees. Task 3 blocks on a human reply and should not be started until the decision is in.

---

## 3. Tasks

### Task 1: Verify and reconcile the 16 stale ledger entries

**Files:**
- Modify: `production/qa/bugs/<each ID in Table A and Table C row 1>.md` (16 files) — `**Status**:` line + appended `## Closure Record`
- Modify: `production/qa/bugs/BUG-gauntlet-emcon-dimension-not-exercised.md`, `production/qa/bugs/BUG-oracle-blindspot-03-salvo-off-by-one.md`, `production/qa/bugs/BUG-oracle-blindspot-05-contact-lifecycle-skip.md` — prepend standard header
- Create: `production/qa/bug-triage-2026-09-01.md`

**Interfaces:**
- Consumes: nothing.
- Produces: every file under `production/qa/bugs/` has a parseable `**Status**:` line; `bug-triage-2026-09-01.md` is the triage-of-record that Task 2's script is validated against.

- [ ] **Step 1: Prepare the environment and confirm the baseline gate is green**

```bash
export PATH="$HOME/.dotnet:$PATH"
cd "$(git rev-parse --show-toplevel)"
bash tools/copy-delegation-assemblies.sh
dotnet build ProjectAegis.sln --nologo 2>&1 | tail -n 3
dotnet test ProjectAegis.sln -v minimal --no-build --nologo 2>&1 | rg "^(Passed!|Failed!)"
```

Expected: `0 Error(s)`; six `Passed!` lines; zero `Failed!` lines; totals sum to 3062.

- [ ] **Step 2: Run each named regression test in isolation (Table A rows 1–9, 11, 12, 14 and Table C row 1)**

```bash
export PATH="$HOME/.dotnet:$PATH"
T=src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj
dotnet test $T --no-build --nologo -v minimal --filter "FullyQualifiedName~ScenarioUndoCliTests|FullyQualifiedName~CatalogReleaseDiffCliArgsTests|FullyQualifiedName~CatalogKillChainReportCommandTests" | rg "^(Passed!|Failed!)"

T=src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj
dotnet test $T --no-build --nologo -v minimal --filter "FullyQualifiedName~MvpEngagementResolverTests|FullyQualifiedName~PdContactKillTests|FullyQualifiedName~PdContactStaleTests|FullyQualifiedName~PdContactPrimaryBlueForceStaleTests" | rg "^(Passed!|Failed!)"

T=src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj
dotnet test $T --no-build --nologo -v minimal --filter "FullyQualifiedName~TargetRegistryTests|FullyQualifiedName~BalticReplayHarnessContactTests|FullyQualifiedName~C2PresentationControllerTests" | rg "^(Passed!|Failed!)"

T=src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj
dotnet test $T --no-build --nologo -v minimal --filter "FullyQualifiedName~FingerprintFloatNegativeZeroTests" | rg "^(Passed!|Failed!)"

T=src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj
dotnet test $T --no-build --nologo -v minimal --filter "FullyQualifiedName~PlatformWorkbookValidatorTests" | rg "^(Passed!|Failed!)"
```

Expected: five `Passed!` lines, `Failed: 0` in each. Any `Failed!` means the corresponding report is `STILL PRESENT` — stop, leave that file `Open`, and record the failure in the triage report.

- [ ] **Step 3: Verify the two non-C# fixes (Table A rows 10 and 13)**

```bash
rg -n 'sid = policy.get\("id"\)' tools/qa-gauntlet/forge_scorecard.py
python3 - <<'EOF'
import sqlite3
c = sqlite3.connect("assets/data/catalog/baltic_patrol.db")
for t in ("platform_emcon", "catalog_staging_emcon"):
    print(t, c.execute(f"select count(*) from {t}").fetchone()[0])
EOF
```

Expected: one match on the `sid = policy.get("id") or policy_path.name…` line; `platform_emcon 1611` and `catalog_staging_emcon 1611` (any value > 0 verifies the report).

- [ ] **Step 4: Flip each verified report to `Verified Fixed`**

For each of the 16 files, replace the whole `**Status**:` line with exactly this shape (fill the commit and test from Table A / Table C):

```markdown
**Status**: Verified Fixed — on `main` @ `81831e76` (fix `8407a1a0`; `ScenarioUndoCliTests.scenario_undo_preserves_sides_and_support_mission_fields_not_touched_by_the_mutation` green 2026-09-01)
```

For the two non-C# rows use the evidence from Step 3 in place of the test name, e.g. `` (fix `84684958`; `platform_emcon` = 1611 rows 2026-09-01) ``.

- [ ] **Step 5: Give the three header-less reports a standard header**

Insert at the top of each of the three files, immediately after the first `#` title line, replacing nothing:

```markdown
## Summary
**Title**: <copy the file's existing H1 text>
**ID**: <file name without .md>
**Severity**: S3-Minor (oracle / ladder coverage gap; no runtime defect)
**Priority**: P3-Backlog
**Status**: Closed — content landed on `main` (see body, CLOSED 2026-07-31)
**Reported**: 2026-07-27
**Reporter**: QA Gauntlet
```

- [ ] **Step 6: Write the triage report of record**

Create `production/qa/bug-triage-2026-09-01.md` with:

```markdown
# Bug Triage Report

> **Date**: 2026-09-01
> **Mode**: full
> **Generated by**: /bug-triage (manual, audit against `main` @ `81831e76`)
> **Open bugs processed**: 15
> **Sprint in scope**: N/A (S120/S121 residual-scope docs sprints; no engineering sprint open)

## Triage Summary

| Priority | Count | Notes |
|----------|-------|-------|
| P1 — Fix this sprint | 0 | The S1 undo bug is fixed on `main` (`8407a1a0`); status was stale |
| P2 — Fix soon | 0 | All 10 S2 reports fixed on `main`; statuses were stale |
| P3 — Backlog | 1 | `BUG-scoring-penalises-roe-correct-refusals` — design decision, not a code defect |
| P4 — Won't fix | 0 | |

**Critical (S1/S2) unfixed count**: 0

## Verified Fixed this triage (16)

<paste Table A and Table C row 1 from docs/superpowers/plans/2026-09-01-open-bug-backlog-remediation.md>

## Systemic Issues Flagged

1. `PdDetectionContactSimulator` — three S2 ordering bugs from one root-cause class → ordering audit (plan Task 4a).
2. Scenario undo — hand-maintained `CloneDocument` dropped new DTO fields → tripwire test (plan Task 4b).
3. Ledger rot — 16/31 reports carried false statuses for 4–8 weeks → `tools/qa/bug-ledger-check.sh` (plan Task 2).

## Recommended Actions

1. Human decision on `BUG-scoring-penalises-roe-correct-refusals` (options 1–4 in the report).
2. Land plan Tasks 2 and 4a–4c.
3. Fix the fresh-checkout verification trap (`tools/copy-delegation-assemblies.sh` missing from the AGENTS.md gate) in the repo-hygiene PR.
```

- [ ] **Step 7: Commit the verification pass**

```bash
GitNexus `detect_changes()` (expected write surface: `production/qa/bugs/**`, `production/qa/bug-triage-2026-09-01.md`); record the result in the PR evidence, then:

git add production/qa/bugs/ production/qa/bug-triage-2026-09-01.md
git commit -m "docs(qa): verify 16 stale bug reports against main and file 2026-09-01 triage

All 14 Open + 2 pending-review reports have fixes and green regression tests on main @ 81831e76.
Statuses flipped to Verified Fixed per bug-report skill Phase 2C. Header block added to the three
header-less gauntlet reports. Refs: docs/superpowers/plans/2026-09-01-open-bug-backlog-remediation.md"
```

- [ ] **Step 8: Human gate — closure approval**

Post in the PR: "16 reports are `Verified Fixed` with test evidence. May I set them to `Closed` and append Closure Records?" Do not proceed to Step 9 without an explicit yes (a short ack such as "acknowledged" counts per repo convention).

- [ ] **Step 9: Append Closure Records and set `Closed`**

For each of the 16 files, append (values from Table A / Table C):

```markdown

## Closure Record
**Closed**: 2026-09-01
**Resolution**: Fixed — <one line from the report's "Fix" bullet>
**Fix commit / PR**: `8407a1a0`
**Verified by**: qa-tester (regression test green on `main` @ `81831e76`)
**Closed by**: <user>
**Regression test**: `src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioUndoCliTests.cs`
**Status**: Closed
```

and change the top-level line to `**Status**: Closed`.

```bash
git add production/qa/bugs/
git commit -m "docs(qa): close 16 verified bug reports with closure records

Human closure approval: <PR comment link>."
```

---

### Task 2: Ledger-rot guard (`tools/qa/bug-ledger-check.sh`)

**Files:**
- Create: `tools/qa/bug-ledger-check.sh`
- Modify: `tools/buildkite/dotnet-ci.sh` (append one informational call before `echo "=== PASS ==="`)
- Modify: `AGENTS.md` — one bullet under "Workflow & Collaboration Rules"

**Interfaces:**
- Consumes: `production/qa/bugs/*.md` with a `**Status**:` and `**Reported**:` line (guaranteed by Task 1 Steps 4–5).
- Produces: exit 0 and a table by default; exit 1 under `--strict` when any report is stale.

- [ ] **Step 1: Write the script**

Create `tools/qa/bug-ledger-check.sh`:

```bash
#!/usr/bin/env bash
# Lists bug reports whose Status is transient ("Open", "pending review", "fix in this
# commit", "pending merge") and older than MAX_AGE_DAYS. Transient statuses are only
# legitimate while a QA worktree is in flight; after merge they must become
# "Verified Fixed" or "Closed" via /bug-report verify|close.
#
# Usage: tools/qa/bug-ledger-check.sh [--strict] [--max-age-days N]
#   --strict           exit 1 if any stale report is found (default: report only)
#   --max-age-days N   staleness threshold (default 14)
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
bugs_dir="$repo_root/production/qa/bugs"
strict=0
max_age_days=14
while [[ $# -gt 0 ]]; do
  case "$1" in
    --strict) strict=1; shift ;;
    --max-age-days) max_age_days="$2"; shift 2 ;;
    *) echo "unknown arg: $1" >&2; exit 2 ;;
  esac
done

today_epoch=$(date -u +%s)
stale=0
printf '%-62s | %-10s | %-4s | %s\n' "ID" "Reported" "Age" "Status"
for f in "$bugs_dir"/*.md; do
  id="$(basename "$f" .md)"
  status="$(sed -nE 's/^\*\*Status\*\*: ?(.*)$/\1/p' "$f" | head -n1)"
  reported="$(sed -nE 's/^\*\*Reported\*\*: ?([0-9]{4}-[0-9]{2}-[0-9]{2}).*$/\1/p' "$f" | head -n1)"
  if [[ -z "$status" || -z "$reported" ]]; then
    printf '%-62s | %-10s | %-4s | %s\n' "$id" "${reported:-????-??-??}" "?" "NO HEADER — add **Status**/**Reported** lines"
    stale=$((stale + 1))
    continue
  fi
  reported_epoch="$(python3 - "$reported" <<'PY'
import datetime
import sys
print(int(datetime.datetime.strptime(sys.argv[1], "%Y-%m-%d").replace(tzinfo=datetime.timezone.utc).timestamp()))
PY
)"
  age_days=$(( (today_epoch - reported_epoch) / 86400 ))
  if echo "$status" | rg -qi '^open|pending review|pending merge|fix in this commit|awaiting'; then
    if (( age_days > max_age_days )); then
      printf '%-62s | %-10s | %-4s | STALE: %s\n' "$id" "$reported" "$age_days" "$status"
      stale=$((stale + 1))
    else
      printf '%-62s | %-10s | %-4s | in flight: %s\n' "$id" "$reported" "$age_days" "$status"
    fi
  fi
done

echo
echo "stale transient bug reports: $stale (threshold ${max_age_days}d)"
if (( strict == 1 && stale > 0 )); then
  echo "FAIL: run /bug-report verify <ID> for each STALE row, then /bug-report close." >&2
  exit 1
fi
```

```bash
chmod +x tools/qa/bug-ledger-check.sh
```

- [ ] **Step 2: Run it before Task 1 has landed (RED) and after (GREEN)**

```bash
git stash    # only if Task 1 edits are uncommitted in this worktree; otherwise check out main
bash tools/qa/bug-ledger-check.sh; echo "exit=$?"
```

Expected on `main` @ `81831e76` (dry-run confirmed 2026-09-01): 15 `STALE: Open…` rows (Table A plus the design question), 2 `STALE: Fixed (pending review)` rows for the magazine reports, 3 `NO HEADER` rows, `stale transient bug reports: 20 (threshold 14d)`, `exit=0`. With `--strict`: `exit=1`.

```bash
git stash pop   # or return to the Task 1 branch
bash tools/qa/bug-ledger-check.sh --strict; echo "exit=$?"
```

Expected after Task 1: only `BUG-scoring-penalises-roe-correct-refusals` listed (as `STALE:` until Task 3 closes it — see note in Task 3 Step 4), `exit=1` under `--strict`. After Task 3: `stale transient bug reports: 0`, `exit=0`.

- [ ] **Step 3: Wire it into the bash CI parity script (informational)**

In `tools/buildkite/dotnet-ci.sh`, insert immediately before the line `echo "=== PASS ==="`:

```bash
echo "=== bug ledger check (informational; run with --strict locally before closeout) ==="
bash "$repo_root/tools/qa/bug-ledger-check.sh" || true
```

- [ ] **Step 4: Document the rule**

In `AGENTS.md`, under `## Workflow & Collaboration Rules`, after the "Files to NEVER commit" list, add:

```markdown
**Bug ledger discipline:** a report filed as `Status: Open (fix in this commit…)` must be flipped to `Verified Fixed` by the PR that merges the fix (`/bug-report verify <ID>`). `bash tools/qa/bug-ledger-check.sh --strict` must be clean before any sprint closeout.
```

- [ ] **Step 5: Commit**

```bash
git add tools/qa/bug-ledger-check.sh tools/buildkite/dotnet-ci.sh AGENTS.md
git commit -m "chore(qa): add bug-ledger-check.sh guard against stale bug report statuses

Informational in dotnet-ci.sh; --strict for closeout. Motivated by 16/31 reports carrying
false Open/pending statuses for 4-8 weeks (bug-triage-2026-09-01)."
```

---

### Task 3: Decision gate — `BUG-scoring-penalises-roe-correct-refusals`

**Files:**
- Modify (options 1/2 only): `src/ProjectAegis.Delegation/Projection/LossesScoringProjection.cs`
- Test (options 1/2 only): `src/ProjectAegis.Delegation.Tests/Projection/LossesScoringProjectionTests.cs` (NUnit)
- Modify (all options): `production/qa/bugs/BUG-scoring-penalises-roe-correct-refusals.md`
- Regenerate (options 1/2 only): gauntlet `expect` envelopes per `tools/qa-gauntlet/README-expect-regen.md`

**Interfaces:**
- Consumes: `PolicyDenialRecord.Reason : FireAbortReason` (`src/ProjectAegis.Sim/Policy/FireAbortReason.cs`: `None, RoeHoldFire, WeaponsTight, WraRange, WraSalvo, EmconOff, NoFireControlTrack, CommsDenied, AirAspectBlock, SurfaceAspectBlock, SubsurfaceAspectBlock, LandAspectBlock, MineAspectBlock`).
- Produces: `LossesScoringSnapshot(Score, HostileKills, MissilesFired, PolicyDenials)` — shape unchanged; `PolicyDenials` stays the total count.

- [ ] **Step 1: Present the decision (human gate)**

Ask the game designer to choose, quoting the report's four options and this default proposal:

> Default proposal = **Option 1**: denials whose `Reason` is `RoeHoldFire`, `WeaponsTight`, or `EmconOff` are *restraint* and cost 0; every other reason (`WraRange`, `WraSalvo`, `NoFireControlTrack`, `CommsDenied`, all `*AspectBlock`) is a *failure* and keeps the −5. `PolicyDenials` in the snapshot remains the total so gauntlet `minDenials` floors still prove gating fired. Alternative with zero code cost = **Option 4** (accept; envelopes are the oracle).

Do not write code until the answer is in.

- [ ] **Step 2 (Options 1/2 chosen): GitNexus upstream impact analysis, then write the failing test**

Run GitNexus MCP `impact` on `LossesScoringProjection.Project` **before editing** and attach its upstream callers, affected processes, and risk level to the PR. If the result is **HIGH** or **CRITICAL**, stop for an explicit human go/no-go before changing score semantics. Then cross-check the result with:

```bash
rg -n "LossesScoringProjection\.Project\(" src --glob '!**/obj/**'
```

Report every caller in the PR body (expected: gauntlet/scorecard projection paths and tests only; it is not on the `OrderLog`/fingerprint path, so `17144800277401907079` is unaffected — confirm with the ReplayGolden run in Step 5).

Append to `LossesScoringProjectionTests`:

```csharp
    /// <summary>
    /// BUG-scoring-penalises-roe-correct-refusals: a HoldFire / WeaponsTight / EMCON refusal is
    /// the system working as designed and must not be scored like a failed engagement.
    /// </summary>
    [Test]
    public void Project_does_not_penalise_restraint_denials_but_still_counts_them()
    {
        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            1, 1.0, 1, new AgentId("a1"), new TargetId("hostile-1"), 0,
            FireAbortReason.RoeHoldFire, OrderKind.Engage));
        log.AppendPolicyDenial(new PolicyDenialRecord(
            2, 2.0, 2, new AgentId("a1"), new TargetId("hostile-1"), 0,
            FireAbortReason.EmconOff, OrderKind.Engage));
        log.AppendPolicyDenial(new PolicyDenialRecord(
            3, 3.0, 3, new AgentId("a1"), new TargetId("hostile-1"), 0,
            FireAbortReason.NoFireControlTrack, OrderKind.Engage));

        var tally = LossesScoringProjection.Project(log);

        Assert.That(tally.PolicyDenials, Is.EqualTo(3), "total denial count is unchanged");
        Assert.That(tally.Score, Is.EqualTo(-5), "only the NoFireControlTrack failure is penalised");
    }
```

Also change the existing `Project_counts_kills_and_denials_in_score` test's reason from `FireAbortReason.RoeHoldFire` to `FireAbortReason.NoFireControlTrack` so it keeps asserting `100 - 5` for a genuine failure.

- [ ] **Step 3 (Option 1): Run to verify RED**

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --nologo -v minimal --filter "FullyQualifiedName~LossesScoringProjectionTests" | rg "^(Passed!|Failed!)"
```

Expected: `Failed: 1` (`Project_does_not_penalise_restraint_denials_but_still_counts_them`, score −15 vs expected −5).

- [ ] **Step 4 (Option 1): Minimal implementation**

In `LossesScoringProjection.cs`, replace

```csharp
        var denials = log.PolicyDenials.Count;
        var score = baseScore + (kills * DefaultPointsPerKill) - (denials * DefaultPenaltyDenial);
```

with

```csharp
        var denials = log.PolicyDenials.Count;
        var penalisedDenials = log.PolicyDenials.Count(d => !IsRestraintDenial(d.Reason));
        var score = baseScore + (kills * DefaultPointsPerKill) - (penalisedDenials * DefaultPenaltyDenial);
```

and add to the class:

```csharp
    /// <summary>
    /// Denials that represent correct restraint under the unit's ROE / EMCON posture
    /// (BUG-scoring-penalises-roe-correct-refusals). These are the system working as
    /// designed and are counted but not penalised.
    /// </summary>
    public static bool IsRestraintDenial(FireAbortReason reason) => reason is
        FireAbortReason.RoeHoldFire or
        FireAbortReason.WeaponsTight or
        FireAbortReason.EmconOff;
```

Add `using ProjectAegis.Sim.Policy;` to the file's using block (it currently imports only `ProjectAegis.Delegation.Decision`, `ProjectAegis.Sim.Engage`, `ProjectAegis.Sim.Scenario`), otherwise `FireAbortReason` does not resolve.

- [ ] **Step 5 (Option 1): GREEN + full gate + envelope regen**

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --nologo -v minimal --filter "FullyQualifiedName~LossesScoringProjectionTests" | rg "^(Passed!|Failed!)"
dotnet build ProjectAegis.sln --nologo 2>&1 | tail -n 3
bash tools/copy-delegation-assemblies.sh
dotnet test ProjectAegis.sln -v minimal --no-build --nologo 2>&1 | rg "^(Passed!|Failed!)"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~ReplayGoldenSuiteTests" | rg "^(Passed!|Failed!)"
grep -r "17144800277401907079" tests/ data/ | wc -l
```

Expected: `Failed: 0` everywhere; total ≥ 3063; ReplayGolden `Passed: 6`; hash grep count > 0 and unchanged from `main`. Then regenerate the gauntlet corpus envelopes exactly as `tools/qa-gauntlet/README-expect-regen.md` describes and commit the regenerated `expect` files in the same PR, citing this bug ID.

- [ ] **Step 5b (Option 2 chosen): Weighted-denial implementation and evidence**

Keep `PolicyDenials` as the total denial count. Add a single private `PenaltyFor(FireAbortReason reason)` mapping in `LossesScoringProjection`: `RoeHoldFire`, `WeaponsTight`, and `EmconOff` map to `0`; `WraRange`, `WraSalvo`, `NoFireControlTrack`, and `CommsDenied` map to the explicitly chosen weights; all other reasons fall back to `DefaultPenaltyDenial` until the design decision lists a different weight. Compute `penalty = log.PolicyDenials.Sum(d => PenaltyFor(d.Reason))` and subtract that from `baseScore + kills * DefaultPointsPerKill`. Add table-driven NUnit coverage for every mapped reason, a fallback-reason case, total-denial count preservation, and deterministic repeated projection. Run RED/GREEN plus the full gate in Step 5, regenerate gauntlet envelopes, then commit the projection, test, bug report, and regenerated expectation files with `feat(scoring): weight policy-denial penalties by reason`.

- [ ] **Step 5c (Option 3 chosen): Restraint-objective delivery slice**

Do not silently change `LossesScoringProjection` for this option. First create a design-approved `ScoringObjective` contract that names the scenario, positive objective, required evidence, points, and whether the objective is mutually exclusive with a kill objective. Implement it in a separate bounded slice with: schema/data fixture, deterministic projection of the objective result from `DecisionLog`/snapshot facts, unit tests for success/failure and repeated-run stability, and regenerated gauntlet envelopes. Use `minDenials` (or a new explicit restraint assertion) to prove the policy gate actually fired. Run GitNexus `impact` on every new score/projection surface, apply the HIGH/CRITICAL stop rule, run the full gate, and only then close this bug with the linked implementation PR. The implementation plan must be reviewed before code work; it is not bundled into the ledger task.

- [ ] **Step 6 (all options): Close the report**

Append to `BUG-scoring-penalises-roe-correct-refusals.md`:

```markdown

## Closure Record
**Closed**: <date>
**Resolution**: Design decision — Option <N>: <one line, e.g. "restraint denials (RoeHoldFire/WeaponsTight/EmconOff) counted but not penalised">
**Fix commit / PR**: <sha or "n/a (Option 4 accepted as designed)">
**Verified by**: qa-tester
**Closed by**: <user>
**Regression test**: `src/ProjectAegis.Delegation.Tests/Projection/LossesScoringProjectionTests.cs` (Option 1/2) or "n/a — design acceptance"
**Status**: Closed
```

and set the top-level line to `**Status**: Closed — design decision recorded (Option <N>)`.

- [ ] **Step 7: Commit**

```bash
git add src/ProjectAegis.Delegation/Projection/LossesScoringProjection.cs src/ProjectAegis.Delegation.Tests/Projection/LossesScoringProjectionTests.cs production/qa/bugs/BUG-scoring-penalises-roe-correct-refusals.md tools/qa-gauntlet/
git commit -m "feat(scoring): stop penalising ROE/EMCON-correct refusals in LossesScoringProjection

Design decision Option 1 for BUG-scoring-penalises-roe-correct-refusals; gauntlet expect
envelopes regenerated per README-expect-regen.md. PolicyDenials total unchanged."
```

(For Option 4, the commit is `docs(qa): close BUG-scoring-penalises-roe-correct-refusals as accepted design (Option 4)` touching only the bug file.)

---

### Task 4a: Ordinal-ordering audit of transition emitters

**Files:**
- Create: `production/qa/determinism-ordering-audit-2026-09.md`
- Modify (only if a hit is a real defect): the offending emitter + a new `*Tests.cs` sibling, following the pattern of `PdContactStaleTests.cs`

**Interfaces:**
- Consumes: `.claude/skills/determinism-audit/SKILL.md` (method), the three fixed reports as the pattern definition.
- Produces: an audit note listing every enumeration of a `Dictionary`/`HashSet` that feeds `OrderLog`, `DecisionLog`, or a `ContactTransition` list, each marked `SORTED`, `NOT REACHABLE FROM ORDERLOG`, or `DEFECT → BUG-<id>`.

- [ ] **Step 1: Enumerate candidate sites**

```bash
rg -n "\.Keys\b|\.Values\b|foreach \(var [a-zA-Z]+ in _[a-zA-Z]+\)" \
  src/ProjectAegis.Sim/Sensors src/ProjectAegis.Sim/Engage src/ProjectAegis.Sim/Logistics \
  src/ProjectAegis.Delegation/Orchestration src/ProjectAegis.Delegation/Decision src/ProjectAegis.Delegation/Mission \
  src/ProjectAegis.Delegation.UnityAdapter/Baltic --glob '!**/obj/**' \
  | rg -v "OrderBy|SortedSet|SortedDictionary|GetSorted"
```

Expected on `main` today: `PdDetectionContactSimulator.cs` shows zero unsorted `_tracks` hits (all three emitters sort). Every other hit is a row in the audit note.

- [ ] **Step 2: Classify each hit**

For each line: open the file, follow the enumerated collection to its consumer. Mark `SORTED` if the collection is a `SortedSet`/`SortedDictionary`/pre-sorted array; `NOT REACHABLE FROM ORDERLOG` if the result never reaches `OrderLog.Append*`, `DecisionLog.Append*`, a returned `ContactTransition` list, or a fingerprinted projection; otherwise `DEFECT`.

- [ ] **Step 3: For any DEFECT, fix TDD-style using the fixed reports as the template**

Write a two-contact test that inserts in non-ordinal order (as in `PdContactStaleTests.Stale_loss_with_multiple_simultaneous_contacts_emits_transitions_in_ordinal_contact_order`), run RED, add `.OrderBy(x => x, StringComparer.Ordinal)`, run GREEN, file `production/qa/bugs/BUG-<system>-order-nondeterminism.md` with the standard header and `Status: Verified Fixed` in the same commit.

- [ ] **Step 4: Write the audit note and run the gate**

`production/qa/determinism-ordering-audit-2026-09.md`: date, command from Step 1, table `file:line | collection | consumer | verdict`, and the list of any bugs filed. Then run the full gate from Task 1 Step 1 plus ReplayGolden and the hash grep from Task 3 Step 5.

- [ ] **Step 5: Commit**

```bash
GitNexus `detect_changes()` and then stage the audit **plus every discovered defect's emitter, regression test, and `production/qa/bugs/BUG-<system>-order-nondeterminism.md`** in the same logical commit:

git add production/qa/determinism-ordering-audit-2026-09.md src/ProjectAegis.Sim/ src/ProjectAegis.Delegation/ production/qa/bugs/
git commit -m "docs(qa): ordinal-ordering audit of OrderLog-visible transition emitters

Follow-up requested by BUG-kill-transition-order-nondeterminism and
BUG-stale-loss-order-nondeterminism."
```

---

### Task 4b: `CloneDocument` drift tripwire

**Files:**
- Create: `src/ProjectAegis.Data.Tests/Scenario/ScenarioUndoStackStoreCloneCoverageTests.cs` (xUnit)

**Interfaces:**
- Consumes: `ProjectAegis.Data.Scenario.Authoring.ScenarioDocumentDto` (10 public properties) and `ScenarioMissionDto` (10 public properties) from `src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentDto.cs`.
- Produces: a test that fails the moment a property is added to either DTO, with a message directing the author to `ScenarioUndoStackStore.CloneDocument`.

- [ ] **Step 1: Write the test**

```csharp
using System.Reflection;
using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// BUG-undo-snapshot-drops-untouched-canonical-sections: ScenarioUndoStackStore.CloneDocument is a
/// hand-maintained field-by-field copy. It silently dropped every DTO property added after it was
/// written. This tripwire pins the property sets so adding a field forces the author to update
/// CloneDocument (and this list) in the same change.
/// </summary>
public sealed class ScenarioUndoStackStoreCloneCoverageTests
{
    private static readonly string[] ExpectedDocumentProperties =
    {
        "Metadata", "Features", "Sides", "Orbat", "ReferencePoints",
        "Missions", "OperationsTimeline", "Events", "Variables", "EditorState",
    };

    private static readonly string[] ExpectedMissionProperties =
    {
        "Id", "Type", "AssignedUnitIds", "TargetIds", "FerryDestinationBaseId",
        "PatrolZone", "StationGeometry", "RoeOverride", "SupportRole", "EmconOverride",
    };

    [Fact]
    public void ScenarioDocumentDto_public_properties_are_all_known_to_CloneDocument()
    {
        var actual = PublicPropertyNames(typeof(ScenarioDocumentDto));
        Assert.True(
            ExpectedDocumentProperties.OrderBy(n => n, StringComparer.Ordinal).SequenceEqual(actual),
            $"ScenarioDocumentDto properties changed: [{string.Join(", ", actual)}]. " +
            "Update ScenarioUndoStackStore.CloneDocument to copy the new field(s), then update " +
            "ExpectedDocumentProperties in this test.");
    }

    [Fact]
    public void ScenarioMissionDto_public_properties_are_all_known_to_CloneDocument()
    {
        var actual = PublicPropertyNames(typeof(ScenarioMissionDto));
        Assert.True(
            ExpectedMissionProperties.OrderBy(n => n, StringComparer.Ordinal).SequenceEqual(actual),
            $"ScenarioMissionDto properties changed: [{string.Join(", ", actual)}]. " +
            "Update ScenarioUndoStackStore.CloneDocument to copy the new field(s), then update " +
            "ExpectedMissionProperties in this test.");
    }

    private static string[] PublicPropertyNames(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
}
```

- [ ] **Step 2: Run to verify GREEN on today's DTOs, then prove it trips**

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --nologo -v minimal --filter "FullyQualifiedName~ScenarioUndoStackStoreCloneCoverageTests" | rg "^(Passed!|Failed!)"
```

Expected: `Passed: 2`. Then temporarily add `public string? Tripwire { get; init; }` to `ScenarioMissionDto`, re-run, expect `Failed: 1` with the `ScenarioMissionDto properties changed` message, then revert the temporary property (`git checkout -- src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentDto.cs`) and re-run to `Passed: 2`.

- [ ] **Step 3: Commit**

```bash
git add src/ProjectAegis.Data.Tests/Scenario/ScenarioUndoStackStoreCloneCoverageTests.cs
git commit -m "test(data): tripwire pinning ScenarioDocumentDto/ScenarioMissionDto property sets for CloneDocument

Follow-up to BUG-undo-snapshot-drops-untouched-canonical-sections: adding a DTO field now fails
the suite until ScenarioUndoStackStore.CloneDocument copies it."
```

---

### Task 4c: Remove the stale `CatalogEmcon` wording from the gauntlet skill

**Files:**
- Modify: `.claude/skills/qa-gauntlet/SKILL.md` lines 180 and 194

**Interfaces:** none.

- [ ] **Step 1: Edit both lines**

Line 180 — replace

```
  including each platform's `CatalogEmcon` emissions profile and archetype bindings.
```

with

```
  including each platform's emissions profile from the `platform_emcon` table (populated since #571) and archetype bindings.
```

Line 194 — replace

```
with EMCON postures consistent with each platform's `CatalogEmcon` profile —
```

with

```
with EMCON postures consistent with each platform's `platform_emcon` rows —
```

- [ ] **Step 2: Verify and commit**

```bash
rg -n "CatalogEmcon" .claude/skills/qa-gauntlet/SKILL.md; echo "matches above must be 0"
git add .claude/skills/qa-gauntlet/SKILL.md
git commit -m "docs(skills): point qa-gauntlet at populated platform_emcon tables

Closes the option-2 follow-up from BUG-catalog-emcon-tables-empty."
```

---

## 4. Verification gate (every task, before marking done)

```bash
export PATH="$HOME/.dotnet:$PATH"
cd /workspace
bash tools/copy-delegation-assemblies.sh
dotnet build ProjectAegis.sln --nologo 2>&1 | tail -n 3                       # 0 Error(s)
dotnet test ProjectAegis.sln -v minimal --no-build --nologo 2>&1 | rg "^(Passed!|Failed!)"   # 0 Failed, total >= 3062
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~ReplayGoldenSuiteTests" | rg "^(Passed!|Failed!)"   # Passed: 6
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~PlayModeSmokeHarnessTests" | rg "^(Passed!|Failed!)"   # >= 20 passed
grep -r "17144800277401907079" tests/ data/ | wc -l                              # > 0, unchanged
git diff --stat main -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs   # empty
bash tools/qa/bug-ledger-check.sh --strict                                        # after Tasks 1-3: exit 0
```

Pipe outputs to `/tmp` and grep summary lines as shown; do not paste raw logs into agent context.

## 5. Not in scope (tracked elsewhere)

- The five watched residuals in `production/qa/gauntlet-defect-registry.json` (`GAUNTLET-RES-*`) are a QA-gauntlet ledger, not `production/qa/bugs/` reports.
- Repo hygiene surfaced while auditing (stale `production/qa/AGENTS.md`/`CLAUDE.md` copies, 71 duplicated skill directories, broken `docs/reports/future-sprint-roadpmap.md` symlink, 55 LFS-pattern binaries showing as permanently modified, AGENTS.md test-floor numbers) — separate hygiene PR.
- Closing the 11 reports already marked `Fixed`/`Verified Fixed` via `/bug-report close` — optional sweep once Task 2's guard is in place.

## 6. Self-review

- Spec coverage: every report in `production/qa/bugs/` appears in §1 with an action (Task 1, Task 3, or "no change").
- Placeholder scan: the only deferred content is the human decision in Task 3 Step 1 and `<user>`/`<date>` fields that must come from the approver.
- Type consistency: `PolicyDenialRecord` positional order `(SequenceId, SimTime, SimTick, AgentId, TargetId, PolicySnapshotId, Reason, AttemptedKind)` matches `src/ProjectAegis.Delegation/Decision/PolicyDenialRecord.cs`; `LossesScoringSnapshot(Score, HostileKills, MissilesFired, PolicyDenials)` unchanged; DTO property lists in Task 4b match `ScenarioDocumentDto.cs` on `main` @ `81831e76`.
