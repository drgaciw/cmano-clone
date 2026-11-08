# Sprint 25 — S24 stretch branch integration (Graphite)

**Date:** 2026-06-17  
**Runbook:** `docs/superpowers/plans/sprint-25-graphite-stack.md`  
**Kickoff:** `production/agentic/sprint-25-parallel-kickoff-2026-06-17.md`  
**Trunk:** `main` @ `9ecbf2c` (592/592 baseline)  
**S25-01 PR:** [#213](https://github.com/drgaciw/cmano-clone/pull/213) — `stack/sprint25/full-sln-gate`

---

## Recommendation

| Decision | Verdict |
|----------|---------|
| Rename `stack/sprint24/*` → `stack/sprint25/*`? | **Yes** — canonical names already in `sprint-status.yaml` and graphite runbook |
| Cherry-pick onto stack after S25-01? | **No** — stack parent is **S25-06** (`damage-validator`), not S25-01 |
| Integration method | **Rename + rebase + `gt track`** — preserve single-commit history; cherry-pick only as fallback |

**Why not cherry-pick onto S25-01:** Sprint 24 retro documented cherry-pick conflicts on doctrine signoff files when folding S24-10 into `cesium-polish`. S25-02..06 will add Platforms damage columns; S24-11 binary golden (`PlatformWorkbookBinaryGoldenTests`) may need refresh after damage export lands. Stacking stretch slices after `damage-validator` keeps the damage critical path clean and avoids premature golden churn.

**Why rename (not new branches):** Each stretch branch is one clean commit rebased on `9ecbf2c`. Rename preserves SHA history, remote tracking, and DONE evidence paths.

---

## Stretch branch contents

### `stack/sprint24/closedxml-phase-b-ux` → `stack/sprint25/closedxml-phase-b-ux` (S25-07 / S24-11)

| Field | Value |
|-------|-------|
| Commit | `a9db122` — `feat(data): ClosedXML Phase B enum dropdowns [S24-11]` |
| Rebased on | `9ecbf2c` ✓ |
| Tests | `ProjectAegis.Data.Excel.Tests` **5/5** |
| Files | 7 (+428 / −6 lines) |

**Touches:**
- `src/ProjectAegis.Data.Excel/ClosedXmlPlatformWorkbookIo.cs` — Emcon Condition/Posture list validation
- `src/ProjectAegis.Data.Excel/PlatformEmconEnums.cs` — migration-008 allowed values
- `src/ProjectAegis.Data.Excel.Tests/*` — validation metadata + Phase B binary golden harness
- `ProjectAegis.sln` — adds `Data.Excel.Tests` project
- `production/agentic/stacks/sprint24/S24-11-DONE.md`

**Scope lock:** Emcon/Mobility/Signatures sheets only — no damage-model columns (may need golden update after S25-03..06).

### `stack/sprint24/doctrine-emcon-readonly` → `stack/sprint25/doctrine-emcon-readonly` (S25-10 / S24-10)

| Field | Value |
|-------|-------|
| Commit | `96b63b0` — `feat(unity): doctrine EMCON read-only panel field [S24-10]` |
| Rebased on | `9ecbf2c` ✓ |
| Tests | Doctrine **9/9**; PlayModeSmoke\|Doctrine **16/16** (18/18 per branch DONE) |
| Files | 10 (+104 / −61 lines) |

**Touches:**
- `DoctrineInheritanceProjection` / `PanelBinder` / `PanelState` — read-only EMCON line
- `DoctrineInheritancePanelHost.cs` + UXML/USS
- Doctrine + PlayMode smoke tests (refactored fixtures)
- `production/agentic/stacks/sprint24/S24-10-DONE.md`

**Invariant:** ZERO touch `DelegationBridge.cs`.

**File overlap between stretch branches:** none — safe linear stack (S25-07 → S25-10).

---

## Target stack position

```
… → stack/sprint25/damage-validator          (S25-06)
      └── stack/sprint25/closedxml-phase-b-ux   (S25-07)  ← rename + track here
           └── stack/sprint25/doctrine-emcon-readonly (S25-10)
                └── stack/sprint25/app6-atlas-phase-c (S25-08)
                     └── …
```

**Parallel development:** After PR #213 merges, stretch work can continue in dedicated worktrees on renamed branches rebased on `stack/sprint25/full-sln-gate`. **Graphite submit** for S25-07/S25-10 waits until `stack/sprint25/damage-validator` exists in the stack.

---

## Integration steps

### Phase A — After PR #213 merges (S25-01 on `main`)

Run from primary repo (`/home/username01/cmano-clone/cmano-clone`):

```bash
export PATH="/home/username01/.dotnet:$PATH"
unset npm_config_prefix
GT() { npx --yes @withgraphite/graphite-cli@stable "$@"; }

cd /home/username01/cmano-clone/cmano-clone
GT sync
GT checkout main

# Rename local + remote (preserve commits)
git branch -m stack/sprint24/closedxml-phase-b-ux stack/sprint25/closedxml-phase-b-ux
git branch -m stack/sprint24/doctrine-emcon-readonly stack/sprint25/doctrine-emcon-readonly
git push origin :stack/sprint24/closedxml-phase-b-ux   # delete old remote name
git push origin :stack/sprint24/doctrine-emcon-readonly
git push -u origin stack/sprint25/closedxml-phase-b-ux
git push -u origin stack/sprint25/doctrine-emcon-readonly

# Rebase stretch onto merged S25-01 tip (parallel dev baseline)
GT checkout stack/sprint25/closedxml-phase-b-ux
git rebase stack/sprint25/full-sln-gate   # or origin/main if S25-01 merged to main
dotnet test src/ProjectAegis.Data.Excel.Tests/ProjectAegis.Data.Excel.Tests.csproj -v minimal

GT checkout stack/sprint25/doctrine-emcon-readonly
git rebase stack/sprint25/full-sln-gate
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "DoctrineInheritance" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|Doctrine" -v minimal
```

### Phase B — After S25-06 branch exists (`stack/sprint25/damage-validator`)

```bash
GT checkout stack/sprint25/closedxml-phase-b-ux
git rebase stack/sprint25/damage-validator
GT track stack/sprint25/closedxml-phase-b-ux --parent stack/sprint25/damage-validator

GT checkout stack/sprint25/doctrine-emcon-readonly
git rebase stack/sprint25/closedxml-phase-b-ux
GT track stack/sprint25/doctrine-emcon-readonly --parent stack/sprint25/closedxml-phase-b-ux

# Full gate before submit
dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal   # expect ≥592 + stretch deltas

GT restack --no-interactive
GT submit --no-interactive   # S25-07 first
GT submit --no-interactive   # S25-10 second (or GT submit --stack when full stack ready)
```

### Phase C — Post-damage golden refresh (if S25-03..06 shift Platforms columns)

On `stack/sprint25/closedxml-phase-b-ux` after rebasing on `damage-validator`:

```bash
dotnet test src/ProjectAegis.Data.Excel.Tests/ProjectAegis.Data.Excel.Tests.csproj -v minimal
# If golden fails: update PlatformWorkbookBinaryGoldenTests pinned hash per S25-05 empty-diff evidence
GT modify -am "test(data): refresh Phase B binary golden after damage columns [S25-07]"
```

---

## Fallback — cherry-pick (only if rename/rebase blocked)

Sprint 24 retro: cherry-pick S24-10 onto `cesium-polish` conflicted on doctrine signoff tests. Use only when rename is impossible (e.g. remote branch owned by another worktree):

```bash
GT checkout stack/sprint25/damage-validator
GT create -m "feat(data): ClosedXML Phase B enum dropdowns [S25-07]" stack/sprint25/closedxml-phase-b-ux
git cherry-pick a9db122

GT checkout stack/sprint25/closedxml-phase-b-ux
GT create -m "feat(unity): doctrine EMCON read-only panel field [S25-10]" stack/sprint25/doctrine-emcon-readonly
git cherry-pick 96b63b0
```

Resolve conflicts in doctrine test fixtures manually; re-run filtered test suites above.

---

## `gt` command checklist (post-#213)

| When | Command |
|------|---------|
| Trunk sync | `GT sync` |
| S25-02..06 bootstrap | `GT checkout stack/sprint25/full-sln-gate` then `GT create -m "…" stack/sprint25/damage-schema-009` (per runbook) |
| Stretch rename | `git branch -m` + `git push` (Phase A) |
| Stack parenting | `GT track stack/sprint25/closedxml-phase-b-ux --parent stack/sprint25/damage-validator` |
| Stack parenting | `GT track stack/sprint25/doctrine-emcon-readonly --parent stack/sprint25/closedxml-phase-b-ux` |
| Pre-submit | `GT restack --no-interactive` |
| Open PRs | `GT submit --stack --no-interactive` (when damage + stretch segments connected) |
| After bottom merge | `GT sync` → `GT restack --no-interactive` |

**Do not** double-prefix: use `GT submit`, not `GT gt submit`.

---

## Worktree map (optional parallel agents)

| Worktree | Branch | Story |
|----------|--------|-------|
| primary | `stack/sprint25/damage-schema-009` … `damage-validator` | S25-02..06 serial |
| `.worktrees/sprint24-cesium` (or new) | `stack/sprint25/closedxml-phase-b-ux` | S25-07 |
| dedicated unity worktree | `stack/sprint25/doctrine-emcon-readonly` | S25-10 |

---

*Prepared for Sprint 25 Graphite stack integration — 2026-06-17.*