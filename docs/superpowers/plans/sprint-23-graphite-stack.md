# Sprint 23 — Graphite Stack Runbook

> **Slice-specific topology only.** Canonical `gt` commands and agent rules: [graphite-github-substitute-plan.md](../../engineering/graphite-github-substitute-plan.md)

**Sprint:** `production/sprints/sprint-23-platform-phase-b-doctrine-polish.md`  
**Implementation plan:** `docs/superpowers/plans/sprint-23-implementation.md`  
**Parallel kickoff:** `production/agentic/sprint-23-parallel-kickoff-2026-06-17.md`  
**Data track:** `production/agentic/sprint-23-plan-data-2026-06-17.md`  
**Unity track:** `production/agentic/sprint-23-plan-unity-2026-06-17.md`  
**Trunk:** `main` @ `f81f1f9` (parallel to other open stacks, not stacked on them)  
**Teams:** `team-data`, `team-unity`, `team-csharp`, `.claude/teams/database-intelligence-team.yaml`

---

## Stack topology (bottom → top)

All seven program stories **S23-01** through **S23-07** map to a single linear Graphite stack. Parallel worktrees (see [Parallel worktree table](#parallel-worktree-table)) implement slices concurrently; stack parenting encodes merge dependencies.

```
main  @ f81f1f9
 └── stack/sprint23/full-sln-gate              (S23-02)  day-1 baseline gate
      └── stack/sprint23/closedxml-xlsx-io     (S23-01)  ClosedXML `.xlsx` I/O + D02/D06 harness
           └── stack/sprint23/doctrine-editor-visual  (S23-03)  Doctrine panel Editor sign-off + U03 regression
                └── stack/sprint23/approve-batch-multi   (S23-04)  ApproveBatch multi-entity commit
                     └── stack/sprint23/phase-b-schema        (S23-05)  Phase B schema spike — **depends on S23-01**
                          └── stack/sprint23/canonical-determinism  (S23-06)  Catalog* sort-key golden — **depends on S23-04**
                               └── stack/sprint23/closeout-gitnexus     (S23-07)  GitNexus re-index + sprint closeout evidence
```

### Linear stack order + dependency notes

| # | Branch | Program ID | Tier | Parent / depends on | Notes |
|---|--------|------------|------|---------------------|-------|
| 1 | `stack/sprint23/full-sln-gate` | S23-02 | must-have | `main` | Day 1 — blocks all feature slices until baseline recorded |
| 2 | `stack/sprint23/closedxml-xlsx-io` | S23-01 | must-have | after S23-02 | ClosedXML-first per integrated kickoff; bundles S23-D02 + S23-D06 harness |
| 3 | `stack/sprint23/doctrine-editor-visual` | S23-03 | must-have | after S23-02 | Parallel with S23-01 in worktrees; stacked after ClosedXML in Graphite |
| 4 | `stack/sprint23/approve-batch-multi` | S23-04 | should-have | after S23-02 | Extend-only `CatalogWriteGate`; defer to Sprint 24 if must-haves consume buffer |
| 5 | `stack/sprint23/phase-b-schema` | S23-05 | should-have | **after S23-01** | Export-only Phase B sheets; needs stable `IPlatformWorkbookIo` from S23-01 |
| 6 | `stack/sprint23/canonical-determinism` | S23-06 | nice-to-have | **after S23-04** | Golden sort-key tests on committed `Catalog*` types |
| 7 | `stack/sprint23/closeout-gitnexus` | S23-07 | nice-to-have | stack top | Re-index @ `f81f1f9` + `detect_changes` baseline; full sln re-run |

**Critical path:** `S23-02 → S23-01 → closeout` (data I/O) **parallel with** `S23-02 → S23-03 → closeout` (Unity polish).

---

## Track ID → program branch map

### Data track (S23-D01–D08)

| Data ID | Program branch | Program ID | Bundled in slice |
|---------|----------------|------------|------------------|
| S23-D08 | `stack/sprint23/full-sln-gate` | S23-02 | Full-solution `dotnet build/test` baseline |
| S23-D02 | `stack/sprint23/closedxml-xlsx-io` | S23-01 | ClosedXML adapter + CLI wiring |
| S23-D06 | `stack/sprint23/closedxml-xlsx-io` | S23-01 | Binary verification harness (same branch) |
| S23-D01 | `stack/sprint23/approve-batch-multi` | S23-04 | `ApproveBatch` multi-entity commit |
| S23-D04 | `stack/sprint23/phase-b-schema` | S23-05 | Migration `008` + exporter sheet stubs |
| S23-D03 | `stack/sprint23/canonical-determinism` | S23-06 | `CatalogSortKeyDeterminismTests` golden |
| S23-D05 | — | — | **Out of stack** — advisory telemetry; defer if behind |
| S23-D07 | — | — | **Out of stack** — Excel UX nice-to-have |

### Unity track (S23-U01–U07)

| Unity ID | Program branch | Program ID | Bundled in slice |
|----------|----------------|------------|------------------|
| S23-U01 | `stack/sprint23/doctrine-editor-visual` | S23-03 | UXML/USS + `DelegationSmoke` wiring + Editor sign-off |
| S23-U03 | `stack/sprint23/doctrine-editor-visual` | S23-03 | Post-S22 PlayMode + C2 batch regression |
| S23-U02 | — | — | **Out of stack** — Cesium polish (buffer) |
| S23-U04 | — | — | **Out of stack** — APP-6 symbology spike |
| S23-U05 | — | — | **Out of stack** — C2 manual sign-off refresh |
| S23-U06 | — | — | **Out of stack** — `useGlobeMap` variant |
| S23-U07 | — | — | **Out of stack** — doctrine keyboard/motion prefs |

---

## Branch bootstrap

**Graphite auth:** If `gt sync` fails with invalid token, refresh at https://app.graphite.com/activate then `gt auth`.

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

gt checkout main
git pull origin main   # confirm @ f81f1f9
gt sync

# Day 1 — baseline gate (bottom of stack)
gt create -am "feat(sprint23): full-solution test gate baseline [S23-02]" stack/sprint23/full-sln-gate

# Must-have data I/O
gt create -am "feat(data): ClosedXML xlsx adapter wiring [S23-01]" stack/sprint23/closedxml-xlsx-io

# Must-have Unity polish (parallel worktree; stacked after ClosedXML)
gt create -am "feat(unity): doctrine inheritance panel Editor visual [S23-03]" stack/sprint23/doctrine-editor-visual

# Should-have write-gate
gt create -am "feat(data): ApproveBatch multi-entity commit [S23-04]" stack/sprint23/approve-batch-multi

# Should-have Phase B — after S23-01 lands
gt create -am "feat(data): Phase B schema export spike [S23-05]" stack/sprint23/phase-b-schema

# Nice-to-have determinism — after S23-04
gt create -am "test(data): Catalog sort-key determinism golden [S23-06]" stack/sprint23/canonical-determinism

# Closeout — top of stack
gt create -am "chore(qa): GitNexus re-index sprint 23 closeout [S23-07]" stack/sprint23/closeout-gitnexus
```

Without Graphite, use plain git stacked branches and open PRs with base branch = parent slice.

**Pre-bootstrap (coordinator, day 1):**

```bash
npx gitnexus analyze .
npx gitnexus impact CatalogWriteGate --direction upstream
npx gitnexus impact IPlatformWorkbookIo --direction upstream
npx gitnexus impact DelegationBridge --direction upstream
```

---

## PR matrix (Cursor agent delegation)

| ID | Branch | PR title | Owner agent | Verify commands | Story / plan path |
|----|--------|----------|-------------|-----------------|-------------------|
| S23-02 | `stack/sprint23/full-sln-gate` | chore(ci): full-solution test gate baseline [S23-02] | `c-sharp-devops-engineer` | `dotnet build ProjectAegis.sln -v minimal`; `dotnet test ProjectAegis.sln -v minimal` | `production/sprints/sprint-23-platform-phase-b-doctrine-polish.md` §S23-02; `production/agentic/sprint-23-plan-data-2026-06-17.md` §S23-D08 |
| S23-01 | `stack/sprint23/closedxml-xlsx-io` | feat(data): ClosedXML xlsx adapter + binary harness [S23-01] | `team-data` → `c-sharp-engineer` | `dotnet restore src/ProjectAegis.Data.Excel/ProjectAegis.Data.Excel.csproj`; `dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "Platform\|ClosedXml" -v minimal`; `dotnet test src/ProjectAegis.Data.Excel.Tests/ProjectAegis.Data.Excel.Tests.csproj -v minimal`; `dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter "Mcp\|Platform" -v minimal` | `docs/superpowers/plans/sprint-23-implementation.md` §S23-01; `production/agentic/sprint-23-plan-data-2026-06-17.md` §S23-D02, §S23-D06 |
| S23-03 | `stack/sprint23/doctrine-editor-visual` | feat(unity): doctrine panel Editor visual sign-off [S23-03] | `team-unity` → `c-sharp-engineer` | `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "Doctrine\|PlayModeSmoke" -v minimal`; `dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "Doctrine" -v minimal`; `rg "DelegationBridge" unity/ProjectAegis/Assets/Scripts/Runtime/DoctrineInheritancePanelHost.cs` | `docs/superpowers/plans/sprint-23-implementation.md` §S23-03; `production/agentic/sprint-23-plan-unity-2026-06-17.md` §S23-U01, §S23-U03 |
| S23-04 | `stack/sprint23/approve-batch-multi` | feat(data): ApproveBatch multi-entity commit [S23-04] | `team-data` + `gitnexus impact` | `dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "WriteGate\|Platform\|CmoMarkdown" -v minimal`; `npx gitnexus impact CatalogWriteGate --direction upstream` | `docs/superpowers/plans/sprint-23-implementation.md` §S23-04; `production/agentic/sprint-23-plan-data-2026-06-17.md` §S23-D01 |
| S23-05 | `stack/sprint23/phase-b-schema` | feat(data): Phase B schema export spike [S23-05] | `team-data` + `sqlite-schema-management` | `dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "Platform" -v minimal` | `docs/superpowers/plans/sprint-23-implementation.md` §S23-05; `production/agentic/sprint-23-plan-data-2026-06-17.md` §S23-D04 |
| S23-06 | `stack/sprint23/canonical-determinism` | test(data): Catalog sort-key determinism golden [S23-06] | `team-data` + `deterministic-data-access` | `dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "CatalogSortKey" -v minimal` | `docs/superpowers/plans/sprint-23-implementation.md` §S23-06; `production/agentic/sprint-23-plan-data-2026-06-17.md` §S23-D03 |
| S23-07 | `stack/sprint23/closeout-gitnexus` | chore(qa): GitNexus re-index + sprint closeout [S23-07] | `c-sharp-devops-engineer` + `/team-qa sprint` | `npx gitnexus analyze --force`; `npx gitnexus detect_changes --repo cmano-clone`; `dotnet test ProjectAegis.sln -v minimal` | `docs/superpowers/plans/sprint-23-implementation.md` §S23-07; evidence `production/qa/sprint-23-gitnexus-*.md` |

---

## Agent dispatch prompts (per slice)

### S23-02 — `full-sln-gate`

```text
Implement S23-02 from docs/superpowers/plans/sprint-23-implementation.md.
Run dotnet build + dotnet test ProjectAegis.sln @ main f81f1f9; triage failures.
Record evidence: production/qa/smoke-sprint-23-*.md + sprint-status.yaml baseline count.
Branch: stack/sprint23/full-sln-gate. Day-1 gate — do NOT open feature worktrees until baseline recorded.
Skills: c-sharp-devops-engineer, smoke-check.
```

### S23-01 — `closedxml-xlsx-io`

```text
Implement S23-01 (+ S23-D02, S23-D06 harness) from sprint-23-implementation.md.
Design: docs/architecture/adr-011-platform-editor-excel-roundtrip.md.
Before edits: npx gitnexus impact IPlatformWorkbookIo --direction upstream --repo cmano-clone
After tests: npx gitnexus detect_changes --repo cmano-clone
Branch: stack/sprint23/closedxml-xlsx-io. Add ProjectAegis.Data.Excel to sln; prove PLE-2.1 empty-diff on binary .xlsx.
Skills: team-data, deterministic-data-access.
```

### S23-03 — `doctrine-editor-visual`

```text
Implement S23-03 (+ S23-U01, S23-U03 regression) from sprint-23-implementation.md.
ADR-010: ZERO touch DelegationBridge.cs — all writes via DelegationBridgeHost seam.
Before edits: npx gitnexus impact DoctrineInheritancePanelHost --direction upstream --repo cmano-clone
After tests: npx gitnexus detect_changes --repo cmano-clone
Branch: stack/sprint23/doctrine-editor-visual.
Manual evidence: production/qa/sprint-23-doctrine-editor-signoff-*.md
Skills: team-unity, hindsight-gitnexus.
```

### S23-04 — `approve-batch-multi`

```text
Implement S23-04 (= S23-D01) from sprint-23-implementation.md.
CRITICAL extend-only on CatalogWriteGate — preserve sensor ApproveBatch behavior.
Before ANY edit: npx gitnexus impact CatalogWriteGate --direction upstream --repo cmano-clone
Document blast radius in PR. After tests: npx gitnexus detect_changes --repo cmano-clone
Branch: stack/sprint23/approve-batch-multi.
Skills: team-data, provenance-audit-modeling.
```

### S23-05 — `phase-b-schema`

```text
Implement S23-05 (= S23-D04) from sprint-23-implementation.md.
Blocker: S23-01 I/O port stable. Export-only Phase B sheets; import deferred.
Before edits: npx gitnexus impact PlatformWorkbookExporter --direction upstream --repo cmano-clone
Branch: stack/sprint23/phase-b-schema. Migration: 008_platform_editor_phase_b.sql.
Skills: team-data, sqlite-schema-management.
```

### S23-06 — `canonical-determinism`

```text
Implement S23-06 (= S23-D03) from sprint-23-implementation.md.
Blocker: S23-04 committed Catalog* types. Add CatalogSortKeyDeterminismTests golden.
Before edits: npx gitnexus impact CatalogWriteGate --direction upstream --repo cmano-clone
Branch: stack/sprint23/canonical-determinism.
Skills: team-data, deterministic-data-access.
```

### S23-07 — `closeout-gitnexus`

```text
Implement S23-07 closeout from sprint-23-implementation.md.
Re-index: npx gitnexus analyze --force @ f81f1f9
Baseline: npx gitnexus detect_changes --repo cmano-clone
Full sln: dotnet test ProjectAegis.sln -v minimal (0 failures)
Run /smoke-check sprint and /team-qa sprint.
Evidence: production/qa/sprint-23-gitnexus-*.md, production/qa/sprint-23-signoff-*.md
Branch: stack/sprint23/closeout-gitnexus.
Skills: c-sharp-devops-engineer, hindsight-gitnexus.
```

---

## Pre-ship gates (per slice)

| Slice | Gate commands | Evidence |
|-------|---------------|----------|
| **S23-02** | `dotnet build ProjectAegis.sln`; `dotnet test ProjectAegis.sln -v minimal` | `production/qa/smoke-sprint-23-*.md` |
| **S23-01** | Scoped Data + Excel + CLI filters (see PR matrix); CLI smoke `platform_export_xlsx --out /tmp/s23-smoke.xlsx` | `ClosedXmlPlatformWorkbookIoTests.cs`; `ClosedXmlRoundTripTests.cs` |
| **S23-03** | Delegation UnityAdapter + Delegation filters `Doctrine\|PlayModeSmoke` | `production/qa/doctrine-inheritance-s23-editor-evidence.md` |
| **S23-04** | `WriteGate\|Platform\|CmoMarkdown` filter; GitNexus CRITICAL impact doc | `CatalogWriteGatePlatformApproveTests.cs` |
| **S23-05** | `Platform` filter; migration applies cleanly | `PlatformWorkbookPhaseBSheetTests.cs` |
| **S23-06** | `CatalogSortKey` filter | `CatalogSortKeyDeterminismTests.cs` |
| **S23-07** | `npx gitnexus analyze --force`; full sln 0 failures; `/smoke-check sprint` | `production/qa/sprint-23-gitnexus-*.md` |

**All slices (before submit):**

```bash
npx gitnexus detect_changes --repo cmano-clone
gt checkout stack/sprint23/full-sln-gate
gt submit --stack --no-interactive
```

Or full gate: `.\tools\verify-ci-local.ps1`

---

## Submit + merge

### Submit (whole stack)

```bash
gt sync
gt checkout stack/sprint23/full-sln-gate
gt submit --stack --no-interactive
```

Review on [Graphite dashboard](https://app.graphite.com). Merge **bottom PR first**; Graphite rebases dependents. After each merge to `main`: `gt sync`.

### Merge order (implementation phase)

From `production/agentic/sprint-23-parallel-kickoff-2026-06-17.md`:

1. `stack/sprint23/full-sln-gate` → `main` (day 1 — baseline evidence only; triage fixes if needed)
2. `stack/sprint23/closedxml-xlsx-io` + `stack/sprint23/doctrine-editor-visual` → `main` (parallel, days 2–5)
3. `stack/sprint23/approve-batch-multi` → `main` (if capacity, days 5–8)
4. `stack/sprint23/canonical-determinism` + `stack/sprint23/phase-b-schema` → `main` (should-have, last)
5. `stack/sprint23/closeout-gitnexus` → `main` (closeout: full sln + `/smoke-check sprint` + `/team-qa sprint`)

---

## Parallel worktree table

From `production/agentic/sprint-23-parallel-kickoff-2026-06-17.md` — use for concurrent implementation; stack parenting above encodes merge order.

| Worktree path | Branch | Owner track | Blocks |
|---------------|--------|-------------|--------|
| `.worktrees/sprint23-full-sln-gate` | `stack/sprint23/full-sln-gate` | devops / coordinator | All feature branches until baseline recorded |
| `.worktrees/sprint23-closedxml` | `stack/sprint23/closedxml-xlsx-io` | team-data | S23-05 Phase B spike |
| `.worktrees/sprint23-doctrine` | `stack/sprint23/doctrine-editor-visual` | team-unity | — (parallel with ClosedXML after S23-02) |
| `.worktrees/sprint23-approve-batch` | `stack/sprint23/approve-batch-multi` | team-data | S23-D05 telemetry wiring (out of stack) |
| `.worktrees/sprint23-determinism` | `stack/sprint23/canonical-determinism` | team-data | — (parallel) |
| `.worktrees/sprint23-phase-b` | `stack/sprint23/phase-b-schema` | team-data | — (after S23-01 lands) |
| `.worktrees/sprint23-closeout` | `stack/sprint23/closeout-gitnexus` | devops / coordinator | — (final 0.5–1d) |

**Worktree bootstrap (after S23-02 green):**

```bash
git worktree add .worktrees/sprint23-closedxml stack/sprint23/closedxml-xlsx-io
git worktree add .worktrees/sprint23-doctrine stack/sprint23/doctrine-editor-visual
git worktree add .worktrees/sprint23-approve-batch stack/sprint23/approve-batch-multi
git worktree add .worktrees/sprint23-determinism stack/sprint23/canonical-determinism
git worktree add .worktrees/sprint23-phase-b stack/sprint23/phase-b-schema
```

**Day-1 gate:** Feature worktrees (S23-01, S23-03) open only after S23-02 baseline is **recorded** (green or triaged with owner assignments).

---

## GitNexus mandatory rules

| Symbol | Risk | Rule |
|--------|------|------|
| `CatalogWriteGate` | **CRITICAL** | Extend-only on S23-04; impact before edit |
| `DelegationBridge` | **CRITICAL** | **ZERO touch** — S23-03 via `DelegationBridgeHost` only |
| `IPlatformWorkbookIo` | HIGH | Impact before S23-01 wiring |
| `PlatformWorkbookImporter` / `Exporter` | HIGH | Impact before sheet changes |
| `DoctrineInheritancePanelHost` | LOW | Impact before UXML wiring |

After every slice: `npx gitnexus detect_changes --repo cmano-clone` before commit.

---

## Delegation / file ownership

| Path | Owner slice |
|------|-------------|
| `src/ProjectAegis.Data/**` | `closedxml-xlsx-io`, `approve-batch-multi`, `phase-b-schema`, `canonical-determinism` |
| `src/ProjectAegis.Data.Excel/**` | `closedxml-xlsx-io` |
| `unity/ProjectAegis/**` | `doctrine-editor-visual` |
| `src/ProjectAegis.Delegation.UnityAdapter/**` | `doctrine-editor-visual` |
| `production/qa/**` | any slice (evidence only; coordinator merges closeout) |

Avoid parallel PRs touching the same paths. Rebase `phase-b-schema` after `closedxml-xlsx-io` merges if exporter contracts change.

---

## Related

- ADR-010: `docs/architecture/adr-010-headless-first-command-driven-ui.md`
- ADR-011: `docs/architecture/adr-011-platform-editor-excel-roundtrip.md`
- QA plan: `production/qa/qa-plan-sprint-23-2026-07-08.md`
- Sprint 22 retro: `production/retrospectives/retro-sprint-22-2026-06-17.md`