# Stack Land Runbook — UI Maturity PRs #382–#385 — 2026-08-01

**Do not merge mid-stack alone.** Land the full ordered stack (or a single pre-integrated tip) so `main` never sits on a half-wired maturity wave.

## Stack map

| Order | PR | Branch | Scope |
|------:|----|--------|-------|
| 1 | [#382](https://github.com/drgaciw/cmano-clone/pull/382) | `stack/ui-maturity/cmd-31-37-parallel` | Wave 1 — CMD-31…37 command / picture / contact / agent |
| 2 | [#383](https://github.com/drgaciw/cmano-clone/pull/383) | `stack/ui-maturity/wave2-cmd-24-27-33-36` | Wave 2 — Air Ops A, scenario library, live edit, perf, scene hosts |
| 3 | [#384](https://github.com/drgaciw/cmano-clone/pull/384) | `stack/ui-maturity/wave3-log08-campaign-layers-globe` | Wave 3 — LOG-08 N, campaigns, basemap layers, product globe |
| 4 | [#385](https://github.com/drgaciw/cmano-clone/pull/385) | `stack/ui-maturity/wave4-cesium-deck-boat-lod` | Wave 4 — boat ops, magazine/deck, APP-6 LOD, Cesium ion gate |
| 5 | (Wave 5 tip) | `stack/ui-maturity/wave5-tick-chrome-signoff` | FSM tick + CMD-22/23 chrome + menu host + signoff (optional same land) |

**Base of stack:** `main` @ pre-#382 (historically `a2c4c49` era).  
**Kickoff Wave 5:** `production/agentic/sprint-ui-maturity-wave5-recommendations-kickoff-2026-08-01.md`.

## Strategy A — Ordered PR merge (preferred for review history)

```
main
  ← merge #382 (cmd-31-37-parallel)
  ← merge #383 (wave2-…)   base = #382 tip
  ← merge #384 (wave3-…)   base = #383 tip
  ← merge #385 (wave4-…)   base = #384 tip
  ← merge Wave5 tip        base = #385 tip   (if ready)
```

Rules:

1. **Never** merge #383 alone onto `main` without #382.
2. **Never** merge #384 / #385 without their lower parents.
3. After each merge: run **Gate commands** below; fix CI before the next PR.
4. Prefer squash-or-merge-commit per repo convention; keep OrderKind append-only history readable.
5. If a mid-PR fails CI, **revert that land** or fix-forward on the same tip — do not open a parallel “skip layer” PR onto main.

## Strategy B — Single tip branch (fast land)

When Graphite / local integration already contains 382→385 (and optionally Wave5):

1. Open **one** PR from tip branch → `main` (e.g. `stack/ui-maturity/wave4-cesium-deck-boat-lod` or `wave5-tick-chrome-signoff`).
2. PR body must list contained PR numbers and wave closeouts.
3. Run full gate once on the tip.
4. Still **do not** cherry-pick a middle wave commit onto main without parents.

## Gate commands (run on land tip)

From repo root (Release configuration):

```bash
# Build
dotnet build ProjectAegis.sln -c Release --nologo

# Core suites (failed must be 0)
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj -c Release --nologo
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -c Release --nologo
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj -c Release --nologo
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj -c Release --nologo

# Unity plugin mirror (after pure-C# / UnityAdapter changes)
tools/copy-delegation-assemblies.sh
# Optional: tools/Test-UnityPluginAssemblies.ps1 when pwsh available

# Optional Editor batch (Windows/macOS Unity host)
# tools/unity/Invoke-C2PlayModeSignoffBatch.ps1
```

Human Editor residual (not required for CI green):

- `production/qa/playmode-signoff-checklist-wave5-2026-08-01.md`
- Cesium visual gate: `docs/engineering/cesium-ion-visual-gate-2026-08-01.md` (personal token only)

## Invariant checklist

| Invariant | Check |
|-----------|-------|
| No `DelegationBridge.Tick` body rewrite | `git diff main...HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs` empty or comment-only |
| No `SimulationSession` tick ownership from Host lane | Host PR must not own FSM-tick wiring (Lane F) |
| CatalogWriteGate extend-only | No write-path rewrite under `src/ProjectAegis.Data/WriteGate/` |
| OrderKind append-only | New kinds only at end of enum; no renumber |
| No ion tokens committed | `git grep -iE 'eyJ|ion.?token.*=.*[A-Za-z0-9]{20}'` clean on tip; CesiumSpike never serializes token |
| Plugin DLLs mirrored | `copy-delegation-assemblies` after adapter changes |
| Suite green | All gate test projects failed=0 |
| Mid-stack alone | **Forbidden** — see Strategy A rule 1–2 |

## Pre-merge operator steps

1. Confirm PR base chain: 382 → 383 → 384 → 385 (→ wave5).
2. Paste gate command output (or CI links) into the land PR.
3. Confirm invariants table above.
4. Schedule human play-mode checklist when Editor available (non-blocking for CI if batch check 1 green).
5. After full stack on `main`, close Graphite children / mark wave closeouts landed.

## Rollback

```bash
# If land not pushed widely:
git reset --hard <pre-land-sha>

# If already on main:
# Prefer revert of the merge commit(s) in reverse order (wave5 → 385 → 384 → 383 → 382)
```

## Related docs

| Doc | Role |
|-----|------|
| `production/agentic/sprint-ui-maturity-wave5-recommendations-kickoff-2026-08-01.md` | Wave 5 lanes |
| `production/qa/playmode-signoff-checklist-wave5-2026-08-01.md` | Editor pass/fail |
| `production/agentic/critical-hub-merge-playbook-2026-07-14.md` | CRITICAL symbol impact |
| Wave 1–4 closeouts under `production/agentic/sprint-ui-maturity-*-closeout-2026-08-01.md` | Per-wave evidence |

---
*Stack land notes — UI Maturity PRs 382–385 (+ optional Wave 5 tip). Do not merge mid-stack alone.*
