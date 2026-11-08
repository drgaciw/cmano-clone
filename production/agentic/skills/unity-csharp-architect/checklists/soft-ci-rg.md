# Soft CI / optional `rg` lint patterns

**Skill:** `unity-csharp-architect` · **Parent:** [`../SKILL.md`](../SKILL.md)  
**Program:** UCA-M4 · **Lane:** D · **Implements:** DRG-122 (optional soft gate)  
**Audience:** agents and reviewers doing a **local** smell pass before or during architecture review

> **Law (one line):** These patterns are **advisory only** — checklist-first ([`pr-finish.md`](pr-finish.md)); hard lint is **optional** and must **not** block product suite floors (gauntlet, oracle, required `dotnet test`).

**Related:** [`pr-finish.md`](pr-finish.md) · [`review-gates.md`](review-gates.md) · [`../references/mono-anti-patterns.md`](../references/mono-anti-patterns.md) · [`../references/asmdefs-and-layers.md`](../references/asmdefs-and-layers.md)

---

## 1. Policy (non-negotiable)

| Rule | Detail |
| --- | --- |
| **Soft, not hard** | Hits are signals for humans/agents — not automatic PR red |
| **No product floor breakage** | Do **not** wire these as required checks that fail Release / gauntlet / Buildkite product suites |
| **Not a substitute for review** | Green soft lint ≠ **PASS** / **APPROVED**; red soft lint ≠ automatic **FAIL** |
| **False positives expected** | Tests, stubs, Editor tooling, bootstrap hosts, comments — triage with gate context |
| **Checklist first** | Authoritative finish remains [`pr-finish.md`](pr-finish.md) |

**Future hard lint (optional, post-decision):** if product later enables CI `rg` gates, document them here and keep them **out** of product suite floors until an explicit program decision.

---

## 2. Scope paths (default)

Prefer scanning **production Unity + headless C#** — not third-party, not generated:

```text
unity/ProjectAegis/Assets/Scripts/
unity/ProjectAegis/Assets/Editor/
src/ProjectAegis.Delegation.UnityAdapter/
src/ProjectAegis.Delegation/
src/ProjectAegis.Data/
src/ProjectAegis.Sim/
```

**Usually exclude / treat carefully:**

| Path | Why |
| --- | --- |
| `**/Tests/**`, `**/*Tests.cs` | Stubs, intentional Find in fixtures |
| `unity/ProjectAegis/Assets/Plugins/` | Precompiled DLLs / meta only |
| `**/Library/`, `**/obj/`, `**/bin/` | Build artifacts |
| `node_modules/`, `.git/` | Noise |

---

## 3. Pattern catalog (advisory)

Run with `rg` if available, else `grep -R`. Adjust flags for your shell.

### 3.1 Presentation / authority smells

| Smell | Suggested pattern | Gate |
| --- | --- | --- |
| Direct sink apply from UI-ish names | `ApplyOrder\s*\(` under `Assets/Scripts` / panel hosts | G2 |
| DecisionLog write from presentation | `DecisionLog` + `\.Append` under `Assets/Scripts` | G3 |
| Live session cache on hosts | `SimulationSession` under `Assets/Scripts/Runtime` | G1 |

```bash
# Examples (from repo root) — advisory
rg -n 'ApplyOrder\s*\(' unity/ProjectAegis/Assets/Scripts || true
rg -n 'DecisionLog' unity/ProjectAegis/Assets/Scripts || true
rg -n 'SimulationSession' unity/ProjectAegis/Assets/Scripts/Runtime || true
```

### 3.2 MonoBehaviour hygiene

| Smell | Suggested pattern | Gate |
| --- | --- | --- |
| Find* sprawl | `FindObjectOfType|FindObjectsOfType|FindObjectsByType|GameObject\.Find` | G6 |
| Resources.Load in production scripts | `Resources\.Load` under `Assets/Scripts` | G6 |

```bash
rg -n 'FindObjectOfType|FindObjectsOfType|FindObjectsByType|GameObject\.Find' \
  unity/ProjectAegis/Assets/Scripts || true
rg -n 'Resources\.Load' unity/ProjectAegis/Assets/Scripts || true
```

**Known false positive:** `UiDocumentPanelSettingsBootstrap` may use `FindObjectsOfType<UIDocument>` for one-shot panel settings repair — triage against PR note / waiver; prefer composition long-term.

### 3.3 Headless purity

| Smell | Suggested pattern | Gate |
| --- | --- | --- |
| UnityEngine in headless | `using UnityEngine` under `src/ProjectAegis.{Data,Sim,Delegation,Delegation.UnityAdapter}` | G5 |
| UnityEditor in Runtime | `using UnityEditor` under `Assets/Scripts/Runtime` | G5 / G7 |

```bash
rg -n 'using UnityEngine' \
  src/ProjectAegis.Data src/ProjectAegis.Sim \
  src/ProjectAegis.Delegation src/ProjectAegis.Delegation.UnityAdapter || true
rg -n 'using UnityEditor' unity/ProjectAegis/Assets/Scripts/Runtime || true
```

### 3.4 Zero-touch / hotpath (manual + path check)

| Smell | How | Gate |
| --- | --- | --- |
| `DelegationBridge` hotpath edits | `git diff` / PR files include `Bridge/DelegationBridge.cs` | G4 |

```bash
# On a PR branch — path presence is the signal; still need human waiver check
git diff main --name-only | rg 'DelegationBridge\.cs' || true
```

### 3.5 Hot-path alloc smells (weak signal)

| Smell | Suggested pattern | Gate |
| --- | --- | --- |
| LINQ in Runtime hosts | `\.Where\(|\.Select\(|\.ToList\(` under `Assets/Scripts/Runtime` | G9 |

```bash
rg -n '\.Where\(|\.Select\(|\.ToList\(' unity/ProjectAegis/Assets/Scripts/Runtime || true
```

High false-positive rate (one-shot setup is fine). Prefer Profiler / `C2PanelPerfBenchTests` for real budgets.

### 3.6 Wrong ADR label (docs / comments)

| Smell | Suggested pattern | Gate |
| --- | --- | --- |
| Presentation called ADR-018 | `ADR-018` near `presentation` / `UI` / `MonoBehaviour` in skill/docs PRs | G13 |

```bash
rg -n 'ADR-018' production/agentic/skills/unity-csharp-architect docs/architecture || true
```

Remember: **Git ADR-018** is valid for **datalink** docs — do not auto-fail every hit.

---

## 4. Suggested local “soft pass” script shape

Not checked in as required CI. Agents may paste into a worklog:

```bash
#!/usr/bin/env bash
# soft-uca-rg.sh — advisory only; exit 0 always unless you opt into fail mode
set -u
root="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$root"

run() { echo "==> $*"; rg -n "$@" || true; }

run 'FindObjectOfType|FindObjectsOfType|FindObjectsByType' unity/ProjectAegis/Assets/Scripts
run 'Resources\.Load' unity/ProjectAegis/Assets/Scripts
run 'using UnityEngine' src/ProjectAegis.Data src/ProjectAegis.Sim \
  src/ProjectAegis.Delegation src/ProjectAegis.Delegation.UnityAdapter
run 'ApplyOrder\s*\(' unity/ProjectAegis/Assets/Scripts

echo "Soft pass complete — triage hits with checklists/review-gates.md (not a hard gate)."
```

---

## 5. How to report soft-lint in PR

```markdown
### Soft CI (optional)
- Ran soft-ci-rg patterns: yes | no
- Hits triaged: none | list with gate ids + disposition (false positive / fixed / waived)
- Did **not** block product suite floors
```

---

## 6. See also

| Doc | Use |
| --- | --- |
| [`pr-finish.md`](pr-finish.md) | Authoritative agent finish checklist |
| [`review-gates.md`](review-gates.md) | Reviewer severity + gate ids |
| [`../ROADMAP.md`](../ROADMAP.md) | UCA-M4: checklist-first; hard lint optional |
| `production/agentic/critical-hub-merge-playbook-2026-07-14.md` | CRITICAL hub / zero-touch pre-land |
| `docs/engineering/buildkite-ci.md` | Real product CI floors |

**UCA-M4 note:** Document only — no workflow YAML changes in this program unless a separate product issue enables hard lint.

---

## UCA-A3 posture decision (2026-08-12)

**Decision:** Soft CI remains **advisory only**.

| Rule | Detail |
| --- | --- |
| Finish gate | [`pr-finish.md`](pr-finish.md) checklist-first — architecture **PASS / FAIL / BLOCKED** |
| Soft `rg` | Informational triage for agents/humans |
| Product floors | Buildkite / required `dotnet test` / gauntlet / oracle **unchanged** by these patterns |
| Hard lint | **Not enabled** by UCA-A. Requires a **separate product decision issue** + explicit workflow change before any required status check |

Program: [UCA Adoption](https://linear.app/drgamtd-workspace/project/uca-adoption-uca-a-085666c9d310) · [DRG-139](https://linear.app/drgamtd-workspace/issue/DRG-139) · kickoff `production/agentic/uca-a-adoption-train-2026-08-12.md`
