# DRG Slice Author/Review Split Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the DRG headless-slice stream to a two-model pipeline — a cheaper model authors each `feat(DRG-2xx)` slice under a written contract, a mechanical preflight rejects the checkable failures for free, and Claude Fable 5.1 reviews only the judgement calls against ADR-010 §2–3 / ADR-007 / ADR-001 and the `DelegationBridge` zero-touch invariant — so review is input-heavy and output-light, and the stream stops merging unreviewed.

**Architecture:** Three artifacts do the work. (1) An **authoring contract** that makes every slice PR body machine-parseable (write-surface lock, hard-nos, self-check, full-suite evidence). (2) `tools/qa/slice-preflight.sh`, which enforces everything a grep can enforce (surface lock, frozen files, replay hash, authority calls, determinism smells, engine-free, tests present, full suite claimed) and emits ADVISORY rows for the model reviewer. (3) A **slice review gate set** (S1–S8, layered on the existing UCA `review-gates.md` G4/G5/G10/G13) with a fixed reviewer prompt, fixed output format, and a token budget, run on Fable in batches so the stable prefix (contract + gates + ADR excerpts) is a cache read. A retro-review of the 25 landed slices proves value before the pilot.

**Tech Stack:** .NET 8.0.400, xUnit/NUnit test projects, bash + `rg` + `gh` (read-only), Graphite (`gt`) for stack/merge, Linear (`Combat Interaction UX` project) as story SoT, Cursor cloud agents with per-`Task` `model` selection.

## Global Constraints

- Replay golden hash `17144800277401907079` unchanged; `tests/regression/replay-golden-*.txt` never re-blessed by a slice.
- `DelegationBridge.cs`, `SimulationSession.cs`, `CatalogWriteGate.cs` zero-touch for slices (waiver = program decision, architect verdict **BLOCKED**).
- `OrderLogReplayFingerprint.cs` is the replay-golden fingerprint — **excluded** from any slice-fingerprint harmonisation; changing it changes the hash.
- Headless assemblies stay engine-free (no `UnityEngine`/`UnityEditor`), per ADR-010/ADR-001.
- Presentation-boundary citations are **ADR-010 §2–3, ADR-007, ADR-001** — never Git **ADR-018** (sensor side-picture / datalink).
- Reviewer language: **APPROVED / CHANGES REQUIRED**; architect finish language **PASS / FAIL / BLOCKED**; severities **BLOCKER / SHOULD-FIX / NIT** (`production/agentic/skills/unity-csharp-architect/checklists/review-gates.md` §4–6). Do not invent a fourth dialect.
- Test floor monotonic: 3,062 / 0 failed on `main` @ `81831e76` (after `bash tools/copy-delegation-assemblies.sh`).
- Conventional Commits; one logical change per commit; Linear ID in body.
- Model tier names follow `.claude/docs/coordination-rules.md` §Model Tier Assignment; this plan adds one tier (Task 3 Step 4).

---

## 1. Why (evidence from the stream, `main` @ `81831e76`)

| Fact | Evidence |
|------|----------|
| 25 headless slices merged 2026-08-27 → 08-29 (DRG-179, 206–230), +300…+1,600 lines each, 3–9 files, all new folders under `src/ProjectAegis.Delegation/<Feature>/` + tests | `git log`, `gh pr list` (#575–#603) |
| **Zero GitHub reviews** recorded on any slice PR; the only reviewer comment is `chatgpt-codex-connector: You have reached your Codex usage limits` on #597 | `gh pr view 597 --json reviews,comments` |
| PR bodies routinely skip the mandated full gate: `- [ ] Full solution test suite (delegation-only change; not run in this PR slice)` | #597 body |
| Codex P2 lessons from #596 (length-prefixed fingerprint fields; `Array.AsReadOnly` fresh-copy rows; contract parity with `CombatEventProjection.FindPolicyDenial`) were applied to **1 of 14** `*Fingerprint.cs` files and **1** slice's row collection | `rg` over `src/ProjectAegis.Delegation/**/*Fingerprint.cs`; `AsReadOnly` only in `CdeAssess/CdeAssessTypes.cs` |
| The repo already has the review vocabulary (UCA `review-gates.md` G1–G13, `pr-finish.md` PASS/FAIL/BLOCKED) but slice PRs never paste the finish block; G1–G3/G6–G9 are Unity-presentation gates that mostly don't apply to headless DTO slices | `pr-finish.md`, #597 body |
| Preflight dry run (Task 2 script) against two landed slices: #597 **FAIL** (full suite skipped) + 2 advisories (raw-delimiter fingerprint, cast-back-mutable rows); #592 **FAIL** (no `## Write surface` section) | run 2026-09-01, see Task 2 Step 2 |

Conclusion: the stream is high-volume, pattern-regular, and unreviewed. That is the ideal shape for a cheap author + expensive reviewer split: the contract makes authoring mechanical, the preflight makes half the review free, and the reviewer's judgement is spent on ~6 questions per PR.

---

## 2. Roles and cost model

| Role | Who | Input per slice | Output per slice | Est. cost per slice |
|------|-----|-----------------|------------------|---------------------|
| **Author** | Sonnet 5 / Composer 2.5 cloud agent (per `coordination-rules.md` Sonnet tier) | Linear issue + `authoring-contract.md` + 1 sibling slice as template (~30–60K tokens over the session) | ~500 LOC + tests + PR body (~15–40K output incl. thinking) | ≈ $0.30–0.60 on Sonnet 5 ($2/$10 per MTok) |
| **Preflight** | `tools/qa/slice-preflight.sh` (no model) | PR body + diff | PASS/FAIL table | $0 |
| **Reviewer** | Claude Fable 5.1 (`claude-fable-5-1-thinking-high`), batched 3–6 PRs per session | Stable prefix ≈ 12K tokens (contract + S-gates + ADR excerpts, cached at $0.25/MTok after first PR) + per-PR diff ≈ 6–10K + Linear AC ≈ 1K | ≤ 600 tokens on APPROVED; ≤ 1,500 on CHANGES REQUIRED | ≈ $0.10–0.20 per PR in a batch ($10 in / $50 out; first PR of a session pays the ≈ $0.15 prefix write) |

Same slice authored **and** self-reviewed on Fable today: ≈ $5–15 (150–300K tokens incl. thinking at $10/$50). Target after this plan: ≈ $0.50–0.80 per slice all-in, with a review that actually happens.

Effort: reviewer stays on `high`; `xhigh` only when the diff touches `Sim/Sensors`, `Sim/Engage`, or anything the preflight flags on S4 determinism.

---

## 3. Tasks

### Task 1: Authoring contract (`authoring-contract.md`)

**Files:**
- Create: `production/agentic/skills/drg-slice-review/authoring-contract.md`

**Interfaces:**
- Consumes: Linear issue fields (`Surface:`, AC, Out of scope) as used by `production/agentic/linear-parallel-dispatch-playbook.md`.
- Produces: the PR body section names that Task 2's script parses verbatim: `## Write surface`, `## Hard nos`, `## Slice self-check`, `## Verification`.

- [ ] **Step 1: Write the contract**

```markdown
# DRG headless slice — authoring contract

**Applies to:** any `feat(DRG-NNN): headless …` change under `src/ProjectAegis.Delegation/<Feature>/` (+ tests). Author model tier: Sonnet (see `.claude/docs/coordination-rules.md`). Reviewer: `production/agentic/skills/drg-slice-review/SKILL.md`.

## 1. Inputs you must read (and nothing else unless the issue says so)
1. The Linear issue (`DRG-NNN`): title, `Surface:`, Acceptance Criteria, Out of scope.
2. This file.
3. One sibling slice as the template — default `src/ProjectAegis.Delegation/CdeAssess/` (post-P2, canonical). Copy its shape: `<Feature>Types.cs`, `<Feature>Projection.cs`, `<Feature>Fingerprint.cs`, tests in `src/ProjectAegis.Delegation.Tests/<Feature>/`.
4. `docs/architecture/adr-010-headless-first-command-driven-ui.md` §2–3, `adr-007-c2-map-presentation.md`, `adr-001-sim-assembly-boundary.md` (skim; cite them, never ADR-018).

## 2. Write-surface lock
- Create **only** `src/ProjectAegis.Delegation/<Feature>/` and `src/ProjectAegis.Delegation.Tests/<Feature>/` (SDK glob auto-includes; **no `.csproj` edits**).
- Never touch: `DelegationBridge.cs`, `SimulationSession.cs`, `CatalogWriteGate.cs`, any `*.asmdef`, `tests/regression/replay-golden-*.txt`, `OrderLogReplayFingerprint.cs`, sibling slice folders, Unity `Assets/**`.
- If the AC cannot be met inside the surface, stop and report **BLOCKED** with the missing contract — do not widen the surface.

## 3. Slice conventions (each one is a reviewer gate S1–S8)
- **S3 advisory-only.** Every snapshot and row carries `IsOrder = false` / `IsFireOrder = false` (name per issue) as an `init`-only or computed property; the projection never constructs `Order`, never touches `IOrderSink`, never calls `DecisionLog.Append*`. It reads `DecisionLog` / `ISimWorldSnapshot`-derived inputs only (ADR-001: Delegation consumes snapshots, emits orders only through the orchestrator).
- **S4 determinism.** No `DateTime`, `Random`, `Guid.NewGuid`, `Environment.TickCount`, `Stopwatch`. Every `Dictionary<string,…>` / `HashSet<string>` / `OrderBy` on strings uses `StringComparer.Ordinal`. Iterate sorted, never insertion order, before emitting rows. Floats format via `FingerprintFloat.Format` (invariant culture, negative-zero normalised).
- **S5 fingerprint.** `<Feature>Fingerprint.Compute(snapshot)` returns a replay-stable string: a fixed prefix (`cde:`, `el:` …), an `…:empty` sentinel, row count, the advisory flag, and **every** row field. String fields are **length-prefixed** (`{value.Length}:{value}`) so ids containing `,`/`|` cannot collide (CdeAssess P2 lesson). Excluding a field from the fingerprint requires a `///` remark saying why.
- **S6 immutability.** Snapshot `Rows` is `IReadOnlyList<Row>` backed by `Array.AsReadOnly(freshCopy)`; nested collections on rows are copied in the row constructor. Callers must not be able to mutate via cast-back.
- **S7 contract parity.** When binding to `DecisionLog` records that a sibling already binds (e.g. policy denials → `CombatEventProjection.FindPolicyDenial`: shooter-scoped, `SimTick >= input.SimTick`, last-wins), reuse the same semantics and say so in a `<remarks>`; do not invent a second contract for the same log shape.
- **S8 tests.** Minimum: empty input → sentinel; threshold/boundary case from the AC; feasible/happy path; fingerprint stability (same input twice → same string); **delimiter collision** (ids containing `,` and `|` produce distinct fingerprints); cast-back mutation guard; `Is*Order` false on every row and snapshot.

## 4. PR body (mandatory sections, exact headings — the preflight parses them)

```markdown
## Summary
Linear: [DRG-NNN](https://linear.app/drgamtd-workspace/issue/DRG-NNN)
<3–6 lines: what the projection emits, which slice (A/B/C), what it is distinct from>

## Write surface
- `src/ProjectAegis.Delegation/<Feature>/`
- `src/ProjectAegis.Delegation.Tests/<Feature>/`

## Hard nos
- `DelegationBridge` Tick / hotpath; `SimulationSession`; `CatalogWriteGate`; `*.asmdef`; `*.csproj`
- ReplayGolden re-bless (`tests/regression/replay-golden-*.txt` unchanged)
- Sibling slice folders: <list>

## Slice self-check
- S3 advisory-only: `IsOrder=false` on <types> — no IOrderSink / DecisionLog.Append
- S4 determinism: Ordinal comparers at <file:line>; no wall clock / RNG
- S5 fingerprint: length-prefixed; fields: <list>; excluded: none | <field + reason>
- S6 immutability: `Array.AsReadOnly` at <file:line>
- S7 parity: <sibling contract reused> | N/A
- ADRs: ADR-010 §2–3, ADR-007, ADR-001

## Verification
- [x] `bash tools/copy-delegation-assemblies.sh && dotnet build ProjectAegis.sln` — 0 errors, 0 warnings
- [x] `<Feature>ProjectionTests` — N/N
- [x] `dotnet test ProjectAegis.sln -v minimal` — <total>/<total>, 0 failed
- [x] `ReplayGoldenSuiteTests` — 6/6, goldens unchanged
- [x] `bash tools/qa/slice-preflight.sh --pr <N>` — PREFLIGHT: PASS (advisories: <list or none>)
```

The full-suite line is **not optional**. "Not run in this PR slice" is a preflight FAIL.

## 5. Hand-off
1. Push, `gt submit --no-interactive`, then run the preflight on the PR number and paste its table under `## Verification`.
2. Set the Linear issue to *In Review* with the PR link.
3. Do not self-merge. The reviewer (Fable) posts APPROVED / CHANGES REQUIRED; a human merges via Graphite.
```

- [ ] **Step 2: Commit**

```bash
git add production/agentic/skills/drg-slice-review/authoring-contract.md
git commit -m "docs(agentic): DRG headless slice authoring contract

Machine-parseable PR body sections (Write surface / Hard nos / Slice self-check / Verification)
and slice conventions S3-S8 distilled from the CdeAssess Codex P2 follow-up (#596)."
```

---

### Task 2: Mechanical preflight (`tools/qa/slice-preflight.sh`)

**Files:**
- Create: `tools/qa/slice-preflight.sh`

**Interfaces:**
- Consumes: PR body sections from Task 1 (`## Write surface`, `## Verification`), `gh` (read-only), the checked-out PR head.
- Produces: exit 0 = `PREFLIGHT: PASS` (reviewer may start), exit 1 = `PREFLIGHT: FAIL` (author fixes first). ADVISORY rows are the reviewer's S5/S6 starting points.

- [ ] **Step 1: Write the script**

```bash
#!/usr/bin/env bash
# Mechanical pre-review for DRG headless slice PRs. Runs BEFORE the model reviewer so the
# reviewer only spends tokens on judgement calls. Exit 1 on any HARD failure; ADVISORY
# rows never fail the run but are echoed for the reviewer.
#
# Usage:
#   tools/qa/slice-preflight.sh --pr <number>            # uses gh to read body + diff range
#   tools/qa/slice-preflight.sh --range <base>..<head> --body <file>   # offline / CI
# Run on the PR head checkout: content checks read the working tree.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

pr=""; range=""; body_file=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --pr) pr="$2"; shift 2 ;;
    --range) range="$2"; shift 2 ;;
    --body) body_file="$2"; shift 2 ;;
    *) echo "unknown arg: $1" >&2; exit 2 ;;
  esac
done

tmp="$(mktemp -d)"
if [[ -n "$pr" ]]; then
  gh pr view "$pr" --json body --template '{{.body}}' > "$tmp/body.md"
  base="$(gh pr view "$pr" --json baseRefOid --template '{{.baseRefOid}}')"
  head="$(gh pr view "$pr" --json headRefOid --template '{{.headRefOid}}')"
  range="$base..$head"
  body_file="$tmp/body.md"
fi
[[ -n "$range" && -n "$body_file" ]] || { echo "need --pr or --range+--body" >&2; exit 2; }

mapfile -t changed < <(git diff --name-only "$range")
hard_fail=0
row() { printf '%-9s | %-28s | %s\n' "$1" "$2" "$3"; }
hard()  { row "FAIL" "$1" "$2"; hard_fail=1; }
pass()  { row "PASS" "$1" "$2"; }
adv()   { row "ADVISORY" "$1" "$2"; }

echo "range: $range  files: ${#changed[@]}"
printf '%-9s | %-28s | %s\n' "result" "gate" "detail"

# S1 — write-surface lock: every changed path must sit under a path declared in the PR body's
# "## Write surface" section (backticked, ending in '/').
mapfile -t surface < <(awk '/^## Write surface/{f=1;next} /^## /{f=0} f' "$body_file" \
  | grep -oE '`[A-Za-z0-9_./-]+/`' | tr -d '`' | sort -u)
if (( ${#surface[@]} == 0 )); then
  hard "S1 write-surface" "PR body has no '## Write surface' section with backticked dirs"
else
  outside=()
  for f in "${changed[@]}"; do
    ok=0; for s in "${surface[@]}"; do [[ "$f" == "$s"* ]] && ok=1 && break; done
    (( ok )) || outside+=("$f")
  done
  if (( ${#outside[@]} )); then hard "S1 write-surface" "outside declared surface: ${outside[*]}"
  else pass "S1 write-surface" "${#changed[@]} files within: ${surface[*]}"; fi
fi

# S2 — zero-touch + goldens
frozen='src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs|src/ProjectAegis.Sim/.*SimulationSession.cs|src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs|src/ProjectAegis.Delegation/Replay/OrderLogReplayFingerprint.cs|tests/regression/replay-golden-.*\.txt|\.csproj$|\.asmdef$'
hits="$(printf '%s\n' "${changed[@]}" | rg -n "$frozen" || true)"
if [[ -n "$hits" ]]; then hard "S2 zero-touch" "frozen file touched: $(echo "$hits" | tr '\n' ' ')"
else pass "S2 zero-touch" "DelegationBridge / SimulationSession / CatalogWriteGate / OrderLogReplayFingerprint / goldens / csproj / asmdef untouched"; fi
if grep -rq "17144800277401907079" tests/ data/; then pass "S2 replay-hash" "17144800277401907079 present"
else hard "S2 replay-hash" "Baltic v2 golden hash missing from tests/ data/"; fi

# S3 — advisory-only: no order emission / log writes from a projection slice
cs_changed=(); for f in "${changed[@]}"; do [[ "$f" == *.cs && -f "$f" ]] && cs_changed+=("$f"); done
if (( ${#cs_changed[@]} )); then
  auth="$(rg -n "IOrderSink|\.ApplyOrder\(|DecisionLog\.Append|new Order\(|TryEnqueueHumanOrder" "${cs_changed[@]}" || true)"
  if [[ -n "$auth" ]]; then hard "S3 advisory-only" "authority call in slice: $(echo "$auth" | head -3 | tr '\n' ' ')"
  else pass "S3 advisory-only" "no IOrderSink / ApplyOrder / DecisionLog.Append / new Order"; fi
  prod_cs=(); for f in "${cs_changed[@]}"; do [[ "$f" != *Tests* ]] && prod_cs+=("$f"); done
  if (( ${#prod_cs[@]} )) && ! rg -q "IsOrder|IsFireOrder|IsAdvisory" "${prod_cs[@]}"; then
    adv "S3 advisory-flag" "no IsOrder/IsFireOrder/IsAdvisory flag found on snapshot/rows — reviewer confirms"
  fi

  # S4 — determinism smells
  det="$(rg -n "DateTime\.(Now|UtcNow)|Random\.Shared|new Random\(|Guid\.NewGuid|Environment\.TickCount|Stopwatch" "${prod_cs[@]:-/dev/null}" || true)"
  if [[ -n "$det" ]]; then hard "S4 determinism" "$(echo "$det" | head -3 | tr '\n' ' ')"
  else pass "S4 determinism" "no wall-clock / unseeded RNG / GUID in production files"; fi
  dict="$(rg -n "new Dictionary<string|new HashSet<string|new SortedSet<string|OrderBy\(" "${prod_cs[@]:-/dev/null}" | rg -v "StringComparer\.Ordinal" || true)"
  if [[ -n "$dict" ]]; then adv "S4 ordinal-comparer" "string keyed collection/sort without StringComparer.Ordinal: $(echo "$dict" | head -2 | tr '\n' ' ')"; fi

  # S5 — fingerprint contract (advisory; reviewer judges)
  fp=(); for f in "${prod_cs[@]:-}"; do [[ "$f" == *Fingerprint.cs ]] && fp+=("$f"); done
  if (( ${#fp[@]} )); then
    if rg -q "len:|\.Length\)\.Append\(':'\)|\{[A-Za-z]+\.Length\}:" "${fp[@]}"; then pass "S5 fingerprint" "length-prefixed field encoding present"
    else adv "S5 fingerprint" "raw delimiter join in ${fp[*]} — CdeAssess P2 lesson: prefix string fields with length (len:value)"; fi
  else
    adv "S5 fingerprint" "no *Fingerprint.cs in diff — reviewer confirms replay-stable fingerprint exists or is N/A"
  fi

  # S6 — immutability (advisory)
  if (( ${#prod_cs[@]} )) && rg -q "IReadOnlyList<[A-Za-z]+Row> Rows" "${prod_cs[@]}" && ! rg -q "AsReadOnly\(|ToImmutable|ImmutableArray" "${prod_cs[@]}"; then
    adv "S6 immutability" "Rows exposed as IReadOnlyList without AsReadOnly/Immutable copy — cast-back can mutate (CdeAssess P2 lesson)"
  fi

  # G5 — engine-free headless
  ue="$(rg -n "using UnityEngine|using UnityEditor" "${cs_changed[@]}" || true)"
  if [[ -n "$ue" ]]; then hard "G5 engine-free" "$ue"; else pass "G5 engine-free" "no UnityEngine/UnityEditor in headless diff"; fi

  # S8 — tests present
  t=0; for f in "${cs_changed[@]}"; do [[ "$f" == *Tests* ]] && t=$((t+1)); done
  if (( t == 0 )); then hard "S8 tests" "no test files in diff"; else pass "S8 tests" "$t test file(s) in diff"; fi
fi

# S8 — evidence: full suite must not be skipped
if rg -qi "\[ \] Full solution test suite|not run in this PR" "$body_file"; then
  hard "S8 full-suite" "PR body says full suite was not run — run it (see AGENTS.md gate)"
elif rg -qi "\[x\].*(dotnet test ProjectAegis.sln|Full solution test suite)" "$body_file"; then
  pass "S8 full-suite" "PR body claims full suite run"
else
  adv "S8 full-suite" "PR body does not state whether the full suite ran"
fi

# G13 — ADR hygiene
if rg -q "ADR-018" "$body_file" && ! rg -qi "datalink|side-picture" "$body_file"; then
  hard "G13 adr-citation" "ADR-018 cited without datalink context — presentation must cite ADR-010/007/001"
fi

echo
if (( hard_fail )); then echo "PREFLIGHT: FAIL — fix HARD rows before requesting model review"; exit 1; fi
echo "PREFLIGHT: PASS — hand ADVISORY rows to the reviewer"
```

```bash
chmod +x tools/qa/slice-preflight.sh
```

- [ ] **Step 2: Verify against two landed slices (expected results recorded from the 2026-09-01 dry run)**

```bash
gh pr view 597 --json body --template '{{.body}}' > /tmp/body597.md
bash tools/qa/slice-preflight.sh --range 8f95b0f0^..8f95b0f0 --body /tmp/body597.md; echo "exit=$?"
```

Expected: `S1 write-surface PASS` (4 files within `EmploymentLedger/` dirs), `S2` PASS ×2, `S3` PASS, `S4` PASS, `S5 fingerprint ADVISORY` (raw delimiter join in `EmploymentLedgerFingerprint.cs`), `S6 immutability ADVISORY`, `G5` PASS, `S8 tests` PASS, `S8 full-suite FAIL` ("not run in this PR slice"), `PREFLIGHT: FAIL`, `exit=1`.

```bash
gh pr view 592 --json body --template '{{.body}}' > /tmp/body592.md
bash tools/qa/slice-preflight.sh --range 32e3c927^..32e3c927 --body /tmp/body592.md; echo "exit=$?"
```

Expected: `S1 write-surface FAIL` (no `## Write surface` section), `S8 full-suite ADVISORY`, `PREFLIGHT: FAIL`, `exit=1`. (Content gates read the working tree, so S5 reports the post-#596 length-prefixed encoding — correct for a real PR-head checkout.)

- [ ] **Step 3: Commit**

```bash
git add tools/qa/slice-preflight.sh
git commit -m "chore(qa): slice-preflight.sh — mechanical pre-review gate for DRG headless slices

Enforces write-surface lock, zero-touch files, replay hash, advisory-only, determinism smells,
engine-free, tests present and full-suite evidence; emits S5/S6 advisories for the model reviewer.
Dry run: #597 FAIL (full suite skipped), #592 FAIL (no write-surface section)."
```

---

### Task 3: Reviewer skill (`drg-slice-review`) and model tier

**Files:**
- Create: `production/agentic/skills/drg-slice-review/SKILL.md`
- Create: `production/agentic/skills/drg-slice-review/review-gates-slice.md`
- Modify: `.claude/docs/coordination-rules.md` — Model Tier Assignment table (add one row)
- Modify: `AGENTS.md` — one bullet under "Unity / C# architecture skill (required)"

**Interfaces:**
- Consumes: preflight table (Task 2), authoring contract (Task 1), UCA `review-gates.md` §0, §4–6 (severity + verdict mapping, not re-hosted).
- Produces: a PR review comment in the fixed format below; the human merges on APPROVED.

- [ ] **Step 1: Write `review-gates-slice.md`**

```markdown
# Slice review gates (S1–S8) — headless DRG projections

**Skill:** `drg-slice-review` · **Layered on:** `../unity-csharp-architect/checklists/review-gates.md` (use its §0 ADR table, §4 severity, §6 verdict mapping — do not re-host) · **Preflight:** `tools/qa/slice-preflight.sh` must be PASS before this review starts.

> **Law (one line):** A slice is a **read-only, deterministic, replay-stable projection** of `DecisionLog` / snapshot facts that can never become an order. Review the seam, not the compile.

Answer each gate Yes / No / N/A with `path:line`. A **No** on a BLOCKER gate → **CHANGES REQUIRED** (architect **FAIL**).

| Gate | Question | Severity if No |
|------|----------|----------------|
| **S1 surface** | Preflight S1 PASS and the surface matches the Linear `Surface:` field? Any file the issue did not authorise? | BLOCKER |
| **S2 zero-touch** | Preflight S2 PASS; no indirect reach into `DelegationBridge` (new `Get*` wiring, reflection, `InternalsVisibleTo`)? Goldens unchanged? | BLOCKER |
| **S3 advisory-only** | Every snapshot and row carries the false advisory flag; no path can turn a row into an `Order`; the projection reads facts only (ADR-001 snapshot-in/order-out; ADR-010 §2–3 UI is a client) | BLOCKER |
| **S4 determinism** | Sorted iteration before emission; `StringComparer.Ordinal` on every string-keyed collection/sort; `FingerprintFloat` for floats; no hidden insertion-order dependence (the `PdDetectionContactSimulator` bug class) | BLOCKER |
| **S5 fingerprint** | Every row field participates or is excluded with a written reason; string fields length-prefixed; sentinel for empty; a test proves `,`/`|` ids do not collide and same-input stability | SHOULD-FIX (BLOCKER if a field that affects the UI is omitted) |
| **S6 immutability** | `Rows` = `Array.AsReadOnly(freshCopy)`; nested collections copied; cast-back test present | SHOULD-FIX |
| **S7 parity** | Same `DecisionLog` binding semantics as the sibling that already binds that record (e.g. `FindPolicyDenial` shooter-scoped / `>=` tick / last-wins); no second contract for the same log shape | SHOULD-FIX (BLOCKER if it silently diverges) |
| **S8 evidence** | Full suite run and stated; ReplayGolden 6/6; tests cover the AC's boundary case; PR body cites ADR-010 §2–3 / 007 / 001 and never ADR-018 | SHOULD-FIX |
| **G4 / G5 / G10 / G13** | Apply from UCA `review-gates.md` verbatim | per that file |

## Output format (post as one PR review comment)

```markdown
## drg-slice-review — <DRG-NNN> · PR #<N>

**Preflight:** PASS (advisories: S5, S6) | FAIL (stop — author fixes)
**Gates:** S1 ✓ · S2 ✓ · S3 ✓ · S4 ✓ · S5 ✗ · S6 ✓ · S7 N/A · S8 ✓ · G4 ✓ · G5 ✓ · G10 ✓ · G13 ✓

**Findings**
- [SHOULD-FIX · S5] `src/…/<Feature>Fingerprint.cs:36` — raw `,` join of `ShooterId`/`WeaponFamilyId`; length-prefix per CdeAssess. Add collision test.
- [NIT · S8] PR body: add `ReplayGoldenSuiteTests` line.

**Verdict:** APPROVED | CHANGES REQUIRED   (architect: PASS | FAIL | BLOCKED)
**ADRs checked:** ADR-010 §2–3, ADR-007, ADR-001
```

Budget: APPROVED ≤ 600 output tokens; CHANGES REQUIRED ≤ 1,500. No prose beyond the template. Do not restate the diff.
```

- [ ] **Step 2: Write `SKILL.md` with the reviewer prompt**

```markdown
---
name: drg-slice-review
description: "Fable-tier review of DRG headless slice PRs against the slice gates S1–S8 and UCA G4/G5/G10/G13 (ADR-010 §2–3 / ADR-007 / ADR-001; DelegationBridge zero-touch). Run after tools/qa/slice-preflight.sh passes. Batch 3–6 PRs per session."
model: fable
allowed-tools: Read, Grep, Glob, Bash(gh pr view *), Bash(gh pr diff *), Bash(bash tools/qa/slice-preflight.sh *), Bash(git diff *)
---

# drg-slice-review

Read-only review. You never edit code, never merge, never run the test suite yourself (the author's
Verification section plus the preflight are the evidence; if they are missing that is a finding).

## Inputs (read in this order, nothing else)
1. `production/agentic/skills/drg-slice-review/review-gates-slice.md`
2. `production/agentic/skills/drg-slice-review/authoring-contract.md` §3
3. `production/agentic/skills/unity-csharp-architect/checklists/review-gates.md` §0, §4, §6
4. For each PR in the batch:
   - `bash tools/qa/slice-preflight.sh --pr <N>` — if FAIL, post the table with verdict CHANGES REQUIRED and move on
   - `gh pr view <N> --json title,body` and `gh pr diff <N>`
   - The Linear issue AC (Linear MCP `get_issue`) — compare surface and AC to the diff
   - The sibling slice named in the PR's S7 line, only the file(s) needed to check parity

## Procedure
1. Classify: projection | fingerprint | types | tests. Note the preflight advisories.
2. Walk S1–S8 then G4/G5/G10/G13. Each gate gets ✓ / ✗ / N/A with `path:line` for every ✗.
3. Severity per `review-gates.md` §4. Verdict per §6. Never APPROVED with an open BLOCKER or an unwaived SHOULD-FIX.
4. Post the output template as a single PR review comment. Set the Linear issue comment to the verdict line only.
5. Batch: keep this skill file, the gates, and the contract as the first thing in context and review 3–6 PRs before ending the session (stable prefix → cache reads).

## Escalate (architect BLOCKED)
- Any `DelegationBridge` / `SimulationSession` / `CatalogWriteGate` touch without a written program waiver.
- AC requires a `DecisionLog` record or snapshot field that does not exist (missing contract).
- Fingerprint change to `OrderLogReplayFingerprint.cs` or any golden.
```

- [ ] **Step 3: Verify the skill is well-formed**

```bash
rg -n "^name:|^model:|^allowed-tools:" production/agentic/skills/drg-slice-review/SKILL.md
rg -c "^\| \*\*S[1-8]" production/agentic/skills/drg-slice-review/review-gates-slice.md
```

Expected: three front-matter lines; `8` gate rows.

- [ ] **Step 4: Add the model tier and the AGENTS.md pointer**

In `.claude/docs/coordination-rules.md`, add a row to the Model Tier Assignment table after the Opus row:

```markdown
| **Fable** | `claude-fable-5-1` (Cursor Task slug `claude-fable-5-1-thinking-high`) | Review-only gates where the input is large and the output is a short verdict: DRG slice review (`drg-slice-review`), retro audits, plan review. Never for authoring slices or docs. Batch 3–6 items per session to amortise the prefix. |
```

In `AGENTS.md`, under "Unity / C# architecture skill (required)", after item 5 add:

```markdown
6. **DRG headless slices** (`feat(DRG-NNN): headless …` under `src/ProjectAegis.Delegation/<Feature>/`): author under `production/agentic/skills/drg-slice-review/authoring-contract.md` (Sonnet tier), run `bash tools/qa/slice-preflight.sh --pr <N>`, then request `drg-slice-review` (Fable tier). No slice merges without the review comment.
```

- [ ] **Step 5: Commit**

```bash
git add production/agentic/skills/drg-slice-review/ .claude/docs/coordination-rules.md AGENTS.md
git commit -m "docs(agentic): drg-slice-review skill — Fable-tier S1-S8 review gates for headless slices

Adds Fable model tier to coordination-rules and the slice author/preflight/review rule to AGENTS.md.
Layered on unity-csharp-architect review-gates (ADR-010/007/001; never ADR-018)."
```

---

### Task 4: Retro-review the 25 landed slices (one Fable session)

**Files:**
- Create: `production/qa/slice-review-retro-2026-09.md`
- Create (Linear, not git): one follow-up issue per SHOULD-FIX/BLOCKER cluster in project *Combat Interaction UX*

**Interfaces:**
- Consumes: Task 2 script, Task 3 gates; PR set below.
- Produces: the first measurement of findings-per-slice (feeds the Task 5 decision gate) and the remediation backlog.

PR set (all merged, `main` @ `81831e76`): #575 (DRG-179), #579 (206), #580 (207), #581 (209), #582 (212), #583 (211), #584 (217), #585 (215), #586 (213), #587 (218), #588 (214), #589 (216), #590 (221), #591 (219), #592+#596 (220), #593 (223), #594 (222), #597 (224), #598 (227), #599 (226), #600 (225), #601 (228), #602 (230), #603 (229).

- [ ] **Step 1: Run the preflight over the whole set (no model)**

```bash
for n in 575 579 580 581 582 583 584 585 586 587 588 589 590 591 592 593 594 597 598 599 600 601 602 603; do
  echo "=== #$n"; bash tools/qa/slice-preflight.sh --pr "$n" 2>&1 | rg "^(FAIL|ADVISORY|PREFLIGHT)" || true
done > /tmp/retro-preflight.txt
rg -c "^FAIL" /tmp/retro-preflight.txt; rg -c "S5 fingerprint" /tmp/retro-preflight.txt
```

Expected (from the family scan): S5 advisory on ~13 of the fingerprint-bearing PRs (all except #592/#596), S6 advisory on most, S8 full-suite FAIL on the PRs whose body says "not run in this PR slice". Note: historical PRs are reviewed against current `main` content, so the table describes today's code, which is what the remediation issues should target.

- [ ] **Step 2: Fable session — review the set with `drg-slice-review`**

Dispatch one cloud agent with `model: claude-fable-5-1-thinking-high` and this prompt (verbatim):

```text
Load production/agentic/skills/drg-slice-review/SKILL.md and follow it in retro mode: do not post PR
comments (PRs are merged). For each PR number in [575 579 580 581 582 583 584 585 586 587 588 589 590
591 592 593 594 597 598 599 600 601 602 603] use the preflight output in /tmp/retro-preflight.txt, read
`gh pr diff <N>` once, and produce the output template. Then write
production/qa/slice-review-retro-2026-09.md containing: a table PR | DRG | preflight | verdict |
BLOCKER n | SHOULD-FIX n | NIT n; a "Clusters" section grouping identical findings across PRs with the
file list per cluster; and a "Proposed Linear issues" section with one title + 3-line body per cluster.
Exclude src/ProjectAegis.Delegation/Replay/OrderLogReplayFingerprint.cs from every cluster (replay-golden
fingerprint; hash 17144800277401907079 must not change). Keep the whole document under 2,500 words.
```

Expected clusters (hypotheses to confirm, not to assume): (a) raw-delimiter fingerprints in 13 files → one issue "Harmonise slice fingerprints to length-prefixed encoding (excl. OrderLogReplayFingerprint)" with a note that slice tests asserting exact fingerprint strings must be updated in the same change; (b) cast-back-mutable `Rows` → one issue; (c) missing full-suite evidence → no code issue, covered by preflight.

- [ ] **Step 3: File the Linear issues and commit the retro**

Create the issues from the "Proposed Linear issues" section (Linear MCP `save_issue`, project *Combat Interaction UX*, label `type:tech-debt`), paste their IDs back into the retro file, then:

```bash
git add production/qa/slice-review-retro-2026-09.md
git commit -m "docs(qa): retro drg-slice-review of 24 landed headless slice PRs (DRG-179, 206-230)

First Fable-tier review pass over the stream; clusters filed as Linear follow-ups."
```

---

### Task 5: Pilot on the next three slices, measure, decide

**Files:**
- Create: `production/agentic/drg-slice-split-pilot-2026-09.md`
- Modify (on ADOPT): `production/agentic/linear-parallel-dispatch-playbook.md` — dispatch prompt template gains the authoring-contract path and the preflight line

**Interfaces:**
- Consumes: Tasks 1–3 landed; three headless `[Slice A|B|C]` issues in Todo in *Combat Interaction UX* (pick the next three whose `Surface:` is a new `src/ProjectAegis.Delegation/<Feature>/` folder — not UI chrome, not verification-only).
- Produces: a go/no-go with numbers.

- [ ] **Step 1: Dispatch three authors (Sonnet tier) with the contract**

Per issue, one cloud agent, `model: claude-sonnet-5-thinking-high`, prompt:

```text
Implement <DRG-NNN> exactly per production/agentic/skills/drg-slice-review/authoring-contract.md. Read the
Linear issue (Linear MCP get_issue), the contract, and src/ProjectAegis.Delegation/CdeAssess/ as the shape
template. Stay inside the declared write surface. Run the Verification block including the full suite
(`bash tools/copy-delegation-assemblies.sh && dotnet test ProjectAegis.sln -v minimal`). Open a draft PR
with the mandatory body sections, run `bash tools/qa/slice-preflight.sh --pr <N>` and paste its table.
Stop when the preflight prints PREFLIGHT: PASS. Do not merge.
```

- [ ] **Step 2: One Fable session reviews all three**

Dispatch `drg-slice-review` (Task 3) with the three PR numbers. Record per PR: preflight result, verdict, findings by severity, review output tokens, and rework loops (author fix → re-review) until APPROVED.

- [ ] **Step 3: Record the measurement**

`production/agentic/drg-slice-split-pilot-2026-09.md`:

```markdown
# DRG slice author/review split — pilot 2026-09

| PR | DRG | Author model | Author session tokens (in/out) | Preflight | Review verdict (1st pass) | BLOCKER / SHOULD-FIX / NIT | Rework loops | Review out-tokens | Est. cost author / review |
|----|-----|--------------|-------------------------------|-----------|---------------------------|----------------------------|--------------|-------------------|---------------------------|
| #  | DRG- | claude-sonnet-5-thinking-high | / | PASS | | / / | | | $ / $ |

## Decision
- ADOPT if: all three APPROVED within ≤ 2 rework loops, zero BLOCKERs missed by preflight that the reviewer caught (or, if caught, added as a preflight rule), and review cost ≤ 25% of author cost.
- ADJUST if: ≥ 3 rework loops on any slice → tighten the contract (add the failure as a §3 convention + preflight rule) and re-pilot one slice.
- STOP if: a reviewer-caught BLOCKER would have merged under the old flow AND the author model could not fix it in two loops → author tier goes to Opus 5 for that slice class.
```

- [ ] **Step 4 (ADOPT): Wire into the dispatch playbook**

In `production/agentic/linear-parallel-dispatch-playbook.md`, in the dispatch prompt template, add two lines to the gates list:

```markdown
- Author under `production/agentic/skills/drg-slice-review/authoring-contract.md` (headless slices); model tier Sonnet.
- `bash tools/qa/slice-preflight.sh --pr <N>` = PASS, then `drg-slice-review` (Fable tier) APPROVED before Graphite merge.
```

- [ ] **Step 5: Commit**

```bash
git add production/agentic/drg-slice-split-pilot-2026-09.md production/agentic/linear-parallel-dispatch-playbook.md
git commit -m "docs(agentic): DRG slice author/review split pilot results and dispatch wiring"
```

---

## 4. Verification gate (per task)

Tasks 1–3 and 5 are docs + one bash script; the only code-touching path is Task 4's follow-up issues (executed later under their own PRs). Before marking any task done:

```bash
bash -n tools/qa/slice-preflight.sh                                       # syntax
bash tools/qa/slice-preflight.sh --range 8f95b0f0^..8f95b0f0 --body /tmp/body597.md; echo "exit=$?"   # expect FAIL (S8 full-suite), exit=1
rg -n "ADR-018" production/agentic/skills/drg-slice-review/ | rg -v "never|not|datalink|side-picture"   # expect no output
grep -r "17144800277401907079" tests/ data/ | wc -l                      # > 0
git diff --stat main -- src/                                              # empty for Tasks 1-3, 5
```

## 5. Not in scope

- Re-enabling `.github/workflows/gitnexus-pr-analysis.yml` or adding a Buildkite step for the preflight (needs `gh` auth on agents; revisit after ADOPT).
- Replacing the Codex connector or configuring Cursor Bugbot (`.cursor/BUGBOT.md` does not exist); this plan makes the Fable review the required gate regardless of which bot comments.
- UI-chrome slices (`unity/ProjectAegis/Assets/**`, DRG-165–170): they stay under the full UCA `pr-finish.md` / `review-gates.md` G1–G13 flow.
- Changing `OrderLogReplayFingerprint.cs` or any golden.

## 6. Self-review

- Spec coverage: author on cheaper model (Task 1 + Task 5 Step 1), Fable reviews (Task 3), ADR-010/007/001 + zero-touch as the gate law (S2/S3/G13, ADR excerpts in inputs), input-heavy/output-light (batching, token budget, cached prefix), measured (Task 5).
- Placeholder scan: PR/DRG numbers in Task 5 are chosen at dispatch time from Linear by the stated criteria; everything else is concrete.
- Consistency: gate names S1–S8 identical across contract §3, preflight rows, and `review-gates-slice.md`; severity/verdict words match UCA `review-gates.md` §4–6; the preflight script in Task 2 is the exact script dry-run on 2026-09-01 (results in Task 2 Step 2), plus `OrderLogReplayFingerprint.cs` added to the frozen list after the family scan.
