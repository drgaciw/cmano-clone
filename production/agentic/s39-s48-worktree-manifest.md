# S39–S48 Worktree Manifest — 10-Sprint Agent Program

**Date:** 2026-06-20  
**Parent repo:** `/home/username01/cmano-clone/cmano-clone`  
**Worktree root:** `/home/username01/cmano-clone/.worktrees/` (per `AGENTS.md`)  
**Stack workflow:** Graphite — `gt create`, `gt submit --stack --no-interactive`  
**Authority:** 10-sprint agent program plan + per-sprint parallel kickoffs

---

## Bootstrap pattern (coordinator, local)

```bash
# Example — one track per sprint
git worktree add /home/username01/cmano-clone/.worktrees/sprintNN-<track-slug> \
  -b stack/sprintNN/<track-slug> main
cd /home/username01/cmano-clone/.worktrees/sprintNN-<track-slug>
# Cloud agents: same branch name, fresh VM checkout
```

**Rules:**
- One worktree + stack branch per parallel track
- Never two agents on the same file (see kickoff file-ownership matrix)
- Merge order: baseline/QA → code tracks by dependency → closeout last
- `gt sync` + `gt restack` after each sprint merge

---

## Track A — Polish (S39–S41)

| Sprint | Worktree dir | Stack branch | Track | Agent env |
|--------|--------------|--------------|-------|-----------|
| S39 | `sprint39-c2-platform` | `stack/sprint39/c2-platform-deeper` | C2/Platform polish | Local + Cloud |
| S39 | `sprint39-hygiene-ci` | `stack/sprint39/hygiene-ci` | Hygiene / CI / docs | Cloud |
| S39 | `sprint39-perf-replay` | `stack/sprint39/perf-replay` | Perf / Replay | Cloud |
| S39 | `sprint39-evidence-playtest` | `stack/sprint39/evidence-playtest` | Evidence / Playtest | **Local** |
| S39 | `sprint39-dispatch-ux` | `stack/sprint39/dispatch-ux` | Dispatch + Art/UX | Local |
| S39 | `sprint39-closeout` | `stack/sprint39/closeout` | Closeout | **Local** |
| S40 | `sprint40-catalog-import` | `stack/sprint40/catalog-import-projection` | Catalog/Import (**1 agent**) | **Local lead** |
| S40 | `sprint40-perf-p1` | `stack/sprint40/perf-p1` | Perf P1 | Cloud |
| S40 | `sprint40-replay-maint` | `stack/sprint40/replay-maint` | Replay maint | Cloud |
| S40 | `sprint40-evidence` | `stack/sprint40/evidence` | Evidence / playtest 12 | **Local** |
| S40 | `sprint40-hygiene` | `stack/sprint40/hygiene` | Hygiene / coord | Cloud |
| S40 | `sprint40-closeout` | `stack/sprint40/closeout` | Closeout | **Local** |
| S41 | `sprint41-adr-decision-telemetry` | `stack/sprint41/adr-decision-telemetry` | Structural-debt ADR (read-only) | Cloud |
| S41 | `sprint41-determinism-audit` | `stack/sprint41/determinism-audit` | Determinism audit | Cloud |
| S41 | `sprint41-evidence-pack` | `stack/sprint41/evidence-pack` | Polish-exit evidence | Local + Cloud |
| S41 | `sprint41-gap-analysis` | `stack/sprint41/gap-analysis` | Track B gap analysis | Cloud |
| S41 | `sprint41-closeout` | `stack/sprint41/closeout` | Closeout + scope packet | **Local** |

---

## Track B — Release Enablement (S42–S48)

> **⛔ BLOCKED until scope-expansion decision** — worktrees may be pre-created for planning only; no feature dispatch.

| Sprint | Worktree dir | Stack branch | Track | Agent env |
|--------|--------------|--------------|-------|-----------|
| S42 | `sprint42-content-catalog-platform` | `stack/sprint42/content-catalog-platform` | B1 wave 1 Catalog/Platform | Local lead |
| S42 | `sprint42-content-scenario` | `stack/sprint42/content-scenario` | B1 scenario/data | Cloud |
| S42 | `sprint42-art-bible-1-4` | `stack/sprint42/art-bible-1-4` | B2 sections 1–4 | Cloud |
| S42 | `sprint42-baseline-qa` | `stack/sprint42/baseline-qa` | QA + expanded gates | Cloud |
| S42 | `sprint42-closeout` | `stack/sprint42/closeout` | Closeout | **Local** |
| S43 | `sprint43-content-engage` | `stack/sprint43/content-engage` | B1 Engage batch | Cloud |
| S43 | `sprint43-content-remainder` | `stack/sprint43/content-remainder` | B1 remainder | Cloud |
| S43 | `sprint43-art-bible-complete` | `stack/sprint43/art-bible-complete` | B2 sections 5–9 | Cloud |
| S43 | `sprint43-evidence` | `stack/sprint43/evidence` | Evidence / playtest | **Local** |
| S43 | `sprint43-closeout` | `stack/sprint43/closeout` | Closeout | **Local** |
| S44 | `sprint44-decision-refactor` | `stack/sprint44/decision-refactor` | B3 Decision | Local lead |
| S44 | `sprint44-telemetry-refactor` | `stack/sprint44/telemetry-refactor` | B3 Telemetry | Cloud (if zero shared files) |
| S44 | `sprint44-osint-audit` | `stack/sprint44/osint-audit` | B3 Osint audit | Cloud |
| S44 | `sprint44-replay-gate` | `stack/sprint44/replay-gate` | Replay regression | Cloud |
| S44 | `sprint44-closeout` | `stack/sprint44/closeout` | Closeout | **Local** |
| S45 | `sprint45-runtime-sensors` | `stack/sprint45/runtime-sensors` | B4 Runtime/Sensors | Local lead |
| S45 | `sprint45-engage-scale` | `stack/sprint45/engage-scale` | B4 Engage scale | Cloud + determinism review |
| S45 | `sprint45-perf-profile` | `stack/sprint45/perf-profile` | Profiling / budgets | Cloud |
| S45 | `sprint45-replay` | `stack/sprint45/replay` | Replay / determinism | Cloud |
| S45 | `sprint45-closeout` | `stack/sprint45/closeout` | Closeout | **Local** |
| S46 | `sprint46-release-checklist` | `stack/sprint46/release-checklist` | B5 checklist | Cloud |
| S46 | `sprint46-store-pages` | `stack/sprint46/store-pages` | Store pages | Cloud |
| S46 | `sprint46-i18n-pipeline` | `stack/sprint46/i18n-pipeline` | Localization | Cloud |
| S46 | `sprint46-launch-docs` | `stack/sprint46/launch-docs` | Launch docs | Cloud |
| S46 | `sprint46-closeout` | `stack/sprint46/closeout` | Closeout | **Local** |
| S47 | `sprint47-ci-gate-check` | `stack/sprint47/ci-gate-check` | CI + gate-check draft | Cloud + Local coord |
| S47 | `sprint47-evidence` | `stack/sprint47/evidence` | Evidence consolidation | **Local** |
| S47 | `sprint47-closeout` | `stack/sprint47/closeout` | Go/No-Go checklist | **Local** |
| S48 | `sprint48-gate-check` | `stack/sprint48/gate-check` | Release gate B6 | **Local** (1–2 agents max) |
| S48 | `sprint48-closeout` | `stack/sprint48/closeout` | Ship train closeout | **Local** |

---

## Shared files — coordinator merge only

Never parallel-edit across tracks:

- `production/sprint-status.yaml`
- `production/perf/perf-profile-polish-baseline-*.md`
- `src/ProjectAegis.Delegation.UnityAdapter/Presentation/C2PresentationController.cs`
- `src/ProjectAegis.Delegation/Projection/PlatformCatalogFilterProjection.cs`

---

## Cleanup after sprint closeout

```bash
git worktree remove /home/username01/cmano-clone/.worktrees/sprintNN-<track-slug>
git branch -d stack/sprintNN/<track-slug>  # after merge to main
```

---

*Manifest for S39–S48 program. Update when kickoff file-ownership tables change.*
