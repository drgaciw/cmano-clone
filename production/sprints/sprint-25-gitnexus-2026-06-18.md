# Sprint 25 GitNexus Re-index + Closeout Hygiene — 2026-06-18

**Repo:** `/home/username01/cmano-clone/cmano-clone`  
**Branch:** `stack/sprint25/closeout-gitnexus` (evidence prepared @ trunk `main`)  
**Trunk:** `main` @ `7a13b5a` (closeout stack tip; S25-01..11 merged)
**Command:** `npx gitnexus analyze . --force`  
**Duration:** 29.9s  
**Indexed commit:** `bd225ae` (trunk tip; S25-01..09 merged)

## Graph stats

| Metric | Value |
|--------|-------|
| Nodes | 10,171 |
| Edges | 21,083 |
| Clusters | 266 |
| Flows | 300 |

**Prior baseline (Sprint 24 closeout @ `9ecbf2c`):** 9,761 nodes / 20,194 edges — **+410 nodes / +889 edges** after Sprint 25 damage + presentation stack.

## Status

```
Repository: cmano-clone
Indexed: 2026-06-18
Status: up-to-date @ bd225ae
```

## detect_changes — Sprint 25 delta vs `9ecbf2c` (scope=compare)

| Metric | Value |
|--------|-------|
| Changed symbols | 466 |
| Changed files | 85 |
| Affected processes | 100 |
| Risk level | **CRITICAL** |

### Sprint touch-set blast radius (impact upstream)

| Symbol | Risk | Direct deps | Processes | Notes |
|--------|------|-------------|-----------|-------|
| `CatalogWriteGate` | **CRITICAL** | 49 | 13 | Extend-only `ProposePlatformDamageBatch` + Phase B `Propose*/ApproveBatch` (S25-04); no bypass |
| `PlatformWorkbookImporter` | **LOW** | 1 | 1 | Damage column import + Mobility/Signatures/Emcon (S25-05) |
| `PlatformWorkbookValidator` | **MEDIUM** | 14 | 0 | Phase B + damage rule pack (S25-06) |
| `ICatalogReader` | **LOW** | 4 | 0 | `TryGetPlatformDamage` + Phase B reads (S25-03) |
| `DelegationBridge` | **N/A** | — | — | **ZERO** file touches vs `main` (ADR-010 compliance) |

### Affected process families (S25 vs S24 baseline)

- `Run → PlatformWorkbook` — export/import/diff CLI verbs + damage columns
- `Run → StageFromFile → Propose*Batch` — platform workbook import staging (Phase B + damage)
- `OnApproveSelected → ApproveBatch` — multi-entity commit path
- `ShouldSkipMigration → TableExists` — migration `009_platform_editor_phase_b_damage.sql`

## Verification gates (trunk @ `bd225ae`)

| Gate | Result |
|------|--------|
| `ReplayGoldenSuiteTests` | **6/6 PASS** (264 ms) |
| Full solution `dotnet test ProjectAegis.sln` | **641/641 PASS** |
| Replay golden drift | **NONE** — no silent golden updates |
| `CatalogWriteGate` extend-only | **HELD** — damage path additive only |
| `DelegationBridge.cs` ZERO touch | **PASS** — empty diff vs `main` |

### Replay catalog (6/6)

| Golden file | Policy | Ticks | Verdict |
|-------------|--------|-------|---------|
| `replay-golden-baltic-engage-2026-06-02.txt` | `baltic-patrol` | 4 | PASS |
| `replay-golden-baltic-comms-2026-06-02.txt` | `baltic-patrol-comms` | 6 | PASS |
| `replay-golden-baltic-classify-2026-06-02.txt` | `baltic-patrol-classify` | 4 | PASS |
| `replay-golden-baltic-stale-2026-06-04.txt` | `baltic-patrol-stale` | 3 | PASS |
| `replay-golden-baltic-spoof-2026-06-04.txt` | `baltic-patrol-spoof` | 5 | PASS |
| `replay-golden-baltic-readiness-2026-06-04.txt` | `baltic-patrol-readiness` | 5 | PASS |

### Per-project test counts (closeout re-run)

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 93 |
| ProjectAegis.MissionEditor.Cli.Tests | 21 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 99 |
| ProjectAegis.Delegation.Tests | 176 |
| ProjectAegis.Data.Excel.Tests | 5 |
| ProjectAegis.Data.Tests | 245 |
| **Total** | **641** |

## DelegationBridge ZERO-touch check

```bash
git diff main -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty — ZERO touch)
```

## Stale `stack/sprint24/*` branch cleanup

Sprint 24 landed via squash-merge PRs **#203–#211**; local branch tips are not ancestors of `main` (expected). All work is on trunk @ `9ecbf2c`+.

| Local branch | Disposition |
|--------------|-------------|
| `stack/sprint24/full-sln-gate` | **PRUNED** — superseded by #203 |
| `stack/sprint24/phase-b-reader` | **PRUNED** — superseded by #204 |
| `stack/sprint24/phase-b-write-gate` | **PRUNED** — superseded by #205 |
| `stack/sprint24/phase-b-importer` | **PRUNED** — superseded by #206 |
| `stack/sprint24/phase-b-validator` | **PRUNED** — superseded by #207 |
| `stack/sprint24/c2-app6-spike` | **PRUNED** — superseded by #209 |
| `stack/sprint24/cesium-polish` | **PRUNED** — superseded by #210 |
| `stack/sprint24/closeout-replay-gitnexus` | **PRUNED** — superseded by #211 |
| `stack/sprint24/closeout-replay-gitnexus-clean` | **PRUNED** — cherry-pick variant; merged via #211 |

**Remote:** No `origin/stack/sprint24/*` refs observed (already absent).  
**Command used:** `git branch -D stack/sprint24/<name>` for each local ref above.

## CI hygiene — GitHub Actions CodeQL advisory

**Verdict:** **ADVISORY** — Buildkite remains merge authority.

| Layer | Status | Notes |
|-------|--------|-------|
| **Buildkite** `buildkite/cmano-clone` | **BLOCKING** | `tools/buildkite/dotnet-ci.sh` — replay + full sln gate |
| **GitHub Actions** `.NET CI` | **ADVISORY** | Billing abort ~3s — org spending limit (unchanged since PR #69) |
| **GitHub Actions** `CodeQL (C#)` | **ADVISORY** | `continue-on-error: true` in `gitnexus-security.yml`; red when billing blocks |
| **GitHub Actions** `CodeQL (JS/TS)` | **ADVISORY** | Same billing gate |
| **Local gate SOP** | **ACTIVE** | `production/qa/sprint-19-ci-local-gate-2026-06-08.md` |

**Producer decision (S25 kickoff):** Document permanent advisory status until billing restored. Do **not** treat skipped/billing-aborted GHA checks as product failures. Re-run `npx gitnexus analyze` after S25-10/11 merge before final sprint closeout merge.

## Tracker row 21

Updated in `Game-Requirements/implementation-tracker-2026-06-04.md`: **Phase B complete** — import→validate→write-gate→damage round-trip (S24-02..06 + S25-02..06).

## Notes

- FTS extension unavailable (load-only policy); semantic search features limited.
- 6 large reference markdown files skipped (>512KB).
- S25-10 (EMCON panel) and S25-11 (tri-batch) remain on parallel branches — re-run closeout gates before final stack merge.
- No replay golden files modified in this closeout commit.

## Next

Before further `CatalogWriteGate` edits: `npx gitnexus impact CatalogWriteGate --repo cmano-clone`.  
After S25-10/11 merge: re-index @ post-merge trunk and refresh this evidence.