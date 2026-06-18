# Sprint 22 — Git Commit Plan

**Date:** 2026-06-17  
**Repo:** `cmano-clone` (`/home/username01/cmano-clone/cmano-clone`)  
**Base:** `main` @ `34d4e6f`  
**Sync state:** `main...origin/main [behind 8]` — **ff-only pull NOT safe until working tree committed or stashed**

---

## Git status summary

| Category | Count |
|----------|-------|
| Modified (staged + unstaged) | **15** |
| Untracked (project files) | **14** |
| Untracked (exclude from commit) | **3** (`.omnigent/`, `.cursor/hooks/`) |
| **Total working-tree entries** | **31** (`git status --short`) |
| Diff stat (tracked only) | **15 files, +954 / −69 lines** |

### Modified (tracked)

```
assets/data/catalog/migrations/007_platform_editor_phase_a.sql
production/sprint-status.yaml
production/sprints/sprint-22-platform-editor-db-doctrine.md
src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookImporterTests.cs
src/ProjectAegis.Data/Import/CmoMarkdownImportProposer.cs
src/ProjectAegis.Data/Import/CmoMarkdownImporter.cs
src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs
src/ProjectAegis.Data/WriteGate/IWriteGate.cs
src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs
src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridgeHost.Doctrine.cs  (deleted)
src/ProjectAegis.Delegation.UnityAdapter/Bridge/DoctrineOverrideCommand.cs
src/ProjectAegis.MissionEditor.Cli.Tests/McpToolsManifestTests.cs
src/ProjectAegis.MissionEditor.Cli/Program.cs
tools/mission-editor/mcp-tools.json
unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs
```

### Untracked (include in commits)

```
docs/superpowers/plans/sprint-22-implementation.md
production/qa/smoke-2026-06-17.md
production/qa/sprint-22-signoff-2026-06-17.md
production/retrospectives/retro-sprint-22-2026-06-17.md
production/agentic/sprint-22-commit-plan-2026-06-17.md
src/ProjectAegis.Data.Tests/Import/CmoMarkdownImporterTests.cs
src/ProjectAegis.Data/Catalog/CatalogPlatformBinding.cs
src/ProjectAegis.Data/Catalog/CatalogWeaponRecord.cs
src/ProjectAegis.Delegation.Tests/Projection/DoctrineInheritancePanelBinderTests.cs
src/ProjectAegis.Delegation.Tests/Projection/DoctrineInheritanceProjectionTests.cs
src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DoctrineOverrideCommandTests.cs
src/ProjectAegis.MissionEditor.Cli/PlatformDiffXlsxCommand.cs
src/ProjectAegis.MissionEditor.Cli/PlatformExportXlsxCommand.cs
src/ProjectAegis.MissionEditor.Cli/PlatformImportXlsxCommand.cs
tools/cmano-db-crawler/fixtures/baltic-platform-mini.md
tools/cmano-db-crawler/fixtures/weapon-mini.md
```

### Do NOT commit (unless user explicitly requests)

```
.omnigent/
.cursor/hooks/
```

**Note:** `docs/architecture/adr-011-platform-editor-excel-roundtrip.md` is already on `main` (committed prior to this dirty tree). No re-commit needed.

---

## Pre-commit test command (run once before any commit)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet build ProjectAegis.sln -v q

dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~Platform|FullyQualifiedName~CmoMarkdown|FullyQualifiedName~WriteGate" -v q

dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj -v q

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~Doctrine|FullyQualifiedName~PlayModeSmoke" -v q

dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "FullyQualifiedName~Doctrine" -v q
```

**Closeout agent result (2026-06-17):** 49 + 21 + 13 + 8 = **91 tests PASS** (build assumed green from test runs).

Optional CLI smoke after commit 2:

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_export_xlsx --out /tmp/smoke-export.platform.txt
```

---

## `git pull --ff-only` safety check

| Check | Result |
|-------|--------|
| Local ahead of origin | **0** commits |
| Local behind origin | **8** commits |
| Diverged | **No** — fast-forward possible **after** clean working tree |
| Dirty working tree | **Yes** — blocks clean ff-only pull |
| Conflict risk on rebase | **Medium** — `origin/main` @ `a95e06f` already fixed doctrine host wiring; overlaps Commit 4 files |

**Recommendation:** Commit locally (sequence below) → `git pull --rebase origin main` → resolve doctrine conflicts if any → re-run pre-commit tests → push (requires network approval).

`git pull --ff-only` is **not** recommended until commits land; even then prefer **rebase** given overlapping doctrine fix on origin.

### Upstream commits to absorb (origin/main)

```
9cf9432 docs: mark GitNexus Buildkite migration complete in progress
de9a982 feat(ci): migrate GitNexus workflows to Buildkite
...
a95e06f fix(ci): repair doctrine override build on main   ← overlaps Commit 4
```

---

## Suggested commit sequence (5 commits)

### Commit 1 — `feat(data): extend platform write-gate for mounts, loadouts, magazines, comms`

**Stories:** 22-1  
**Scope:** Write-gate + migration + platform importer test extensions

```
assets/data/catalog/migrations/007_platform_editor_phase_a.sql
src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs
src/ProjectAegis.Data/WriteGate/IWriteGate.cs
src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookImporterTests.cs
```

---

### Commit 2 — `feat(cli): add platform_export/import/diff_xlsx MCP verbs`

**Stories:** 22-2  
**Scope:** CLI commands, Program wiring, MCP manifest

```
src/ProjectAegis.MissionEditor.Cli/PlatformExportXlsxCommand.cs
src/ProjectAegis.MissionEditor.Cli/PlatformImportXlsxCommand.cs
src/ProjectAegis.MissionEditor.Cli/PlatformDiffXlsxCommand.cs
src/ProjectAegis.MissionEditor.Cli/Program.cs
src/ProjectAegis.MissionEditor.Cli.Tests/McpToolsManifestTests.cs
tools/mission-editor/mcp-tools.json
```

---

### Commit 3 — `feat(data): CmoMarkdown platform and weapon import staging`

**Stories:** 22-4  
**Scope:** Importer, proposer, catalog types, fixtures, tests

```
src/ProjectAegis.Data/Import/CmoMarkdownImporter.cs
src/ProjectAegis.Data/Import/CmoMarkdownImportProposer.cs
src/ProjectAegis.Data/Catalog/CatalogPlatformBinding.cs
src/ProjectAegis.Data/Catalog/CatalogWeaponRecord.cs
src/ProjectAegis.Data.Tests/Import/CmoMarkdownImporterTests.cs
tools/cmano-db-crawler/fixtures/baltic-platform-mini.md
tools/cmano-db-crawler/fixtures/weapon-mini.md
```

*(Write-gate platform/weapon batch methods land in Commit 1 if not already split — if `CatalogWriteGate.cs` changes are only mount/loadout/mag/comms, platform batches may already be in same file from 22-4; keep Commit 1+3 sequential to avoid partial gate state.)*

**Adjustment:** If `CatalogWriteGate.cs` / `IWriteGate.cs` include `ProposePlatformBatch` / `ProposeWeaponBatch` from 22-4, move those hunks to Commit 3 or squash Commits 1+3 into one `feat(data): platform write-gate and markdown import` commit. Prefer **5-commit plan** by staging 22-4 gate overloads with Commit 3 files only (use `git add -p` if needed).

---

### Commit 4 — `feat(doctrine): inheritance panel headless round-trip and tests`

**Stories:** 22-5  
**Scope:** Doctrine command, Unity host wiring, tests; zero `DelegationBridge` touch

```
src/ProjectAegis.Delegation.UnityAdapter/Bridge/DoctrineOverrideCommand.cs
src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridgeHost.Doctrine.cs
unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs
src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DoctrineOverrideCommandTests.cs
src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs
src/ProjectAegis.Delegation.Tests/Projection/DoctrineInheritanceProjectionTests.cs
src/ProjectAegis.Delegation.Tests/Projection/DoctrineInheritancePanelBinderTests.cs
```

**Rebase note:** Compare with `origin/main` `a95e06f` during conflict resolution — prefer upstream host pattern if equivalent.

---

### Commit 5 — `chore(sprint): Sprint 22 closeout evidence, retro, and status`

**Stories:** meta (22-3 evidence refs, QA, retro)  
**Scope:** Production docs only

```
production/sprint-status.yaml
production/sprints/sprint-22-platform-editor-db-doctrine.md
production/qa/smoke-2026-06-17.md
production/qa/sprint-22-signoff-2026-06-17.md
production/retrospectives/retro-sprint-22-2026-06-17.md
production/agentic/sprint-22-commit-plan-2026-06-17.md
docs/superpowers/plans/sprint-22-implementation.md
```

---

## Example commands (prepare only — do not push without approval)

```bash
cd /home/username01/cmano-clone/cmano-clone
# Run pre-commit tests first (see above)

git add assets/data/catalog/migrations/007_platform_editor_phase_a.sql \
  src/ProjectAegis.Data/WriteGate/ \
  src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookImporterTests.cs
git commit -m "feat(data): extend platform write-gate for mounts, loadouts, magazines, comms"

git add src/ProjectAegis.MissionEditor.Cli/Platform*.cs \
  src/ProjectAegis.MissionEditor.Cli/Program.cs \
  src/ProjectAegis.MissionEditor.Cli.Tests/McpToolsManifestTests.cs \
  tools/mission-editor/mcp-tools.json
git commit -m "feat(cli): add platform_export/import/diff_xlsx MCP verbs"

# ... commits 3–5 similarly

git pull --rebase origin main
# resolve conflicts, re-run tests
# git push origin main   # requires network approval
```

---

## Recommended next step

1. Run pre-commit test block (verified PASS 2026-06-17).  
2. Apply 5-commit sequence locally.  
3. `git pull --rebase origin main` (not bare ff-only on dirty tree).  
4. Re-run tests post-rebase.  
5. Open PR or push with network approval.

*Generated by Sprint 22 closeout agent — 2026-06-17. No commits or pushes performed.*