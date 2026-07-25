# Gauntlet stack land plan — 2026-07-14

**Target:** `/home/username01/cmano-clone` @ `stack/post-editor/s93-asset-production`  
**Source:** remote `gau-src` → branch `07-10-qa_gauntlet_gauntlet-20260710-1352` (path `/home/username01/projects/active/cmano-clone/cmano-clone`)  
**Gate:** post-s93-project-release-hold-gate-2026-07-14.md Track A

## Strategy

Repos are **not** a single git worktree family (separate object histories until fetch). Land by **cherry-pick** of five linear gauntlet commits (skip merges). Do **not** merge entire gau branch tip (would pull unrelated main merges).

## Ordered cherry-picks (oldest → newest)

| Order | SHA | Subject |
|-------|-----|---------|
| 1 | `6f17f37` | docs(qa): qa-gauntlet game-playing effectiveness plan |
| 2 | `d06e8a7` | feat(qa-gauntlet): oracle fail-closed evaluator + joint catalog ORBAT |
| 3 | `fbf23bc` | fix(gauntlet): theater inject drives real CommsStateChange + PolicyUpdate ROE |
| 4 | `917e716` | feat(gauntlet): ladder inject + multi-domain with fingerprint fail-closed |
| 5 | `d927684` | qa(gauntlet): max-variance smoke gauntlet-20260713-1739 |

## Forbidden paths (abort if appear)

- `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs`
- `CatalogWriteGate` write-path rewrites under `src/ProjectAegis.Data/WriteGate/`

Pre-check on ordered range: **OK** (no forbidden paths in name-only sample for gauntlet commits).

## Classification of expected surfaces

| Class | Paths |
|-------|-------|
| Policies | `data/scenarios/gauntlet-*.policy.json` |
| Oracle | `GauntletOracleExpect.cs`, `GauntletOracleEvaluator.cs`, tests |
| Harness tests | `BalticReplayHarnessLadder*.cs`, theater/multidomain tests |
| CI | `.github/workflows/gauntlet-oracle.yml` |
| QA artifacts | `production/qa/gauntlet/**`, effectiveness plan |
| Skill | `.claude/skills/qa-gauntlet/SKILL.md`, `.grok/skills/qa-gauntlet/` if present |

## Rollback

```bash
git reset --hard <pre-land-sha>   # only if not pushed / with human ack
# or revert the land commit(s)
```

## Success

- Suite ≥ 1599/0f (or higher with new tests)
- Gauntlet oracle dry-run allPassed + strip fail-closed
- Stage remains Release
