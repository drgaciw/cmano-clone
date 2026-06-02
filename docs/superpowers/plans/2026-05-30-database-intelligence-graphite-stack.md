# Database Intelligence P0 — Graphite Stack Runbook

**Design:** `docs/superpowers/specs/2026-05-30-database-intelligence-p0-design.md`  
**Implementation plan:** `docs/superpowers/plans/2026-05-30-database-intelligence-p0.md`  
**Trunk:** `main` (parallel to `stack/delegation/*`, not stacked on it)  
**Team:** `.claude/teams/database-intelligence-team.yaml`

---

## Stack topology (bottom → top)

```
main
 └── stack/data/p0-spec          (DATA-0)  docs + ADR-006
      └── stack/data/assembly     (DATA-1)  ProjectAegis.Data scaffold
           └── stack/data/schema  (DATA-2)  entities + migrations + seed
                └── stack/data/scenario-bind (DATA-3)  snapshots + package + policy move
                     └── stack/data/validation (DATA-4)  validators + write gate
                          └── stack/data/import-smoke (DATA-5)  CMO subset importer
```

---

## Branch bootstrap

**Graphite auth:** If `gt sync` fails with invalid token, refresh at https://app.graphite.com/activate then:

```powershell
cd d:\MyCode\cmano-clone
git checkout main
gt sync
gt track stack/data/p0-spec
gt create stack/data/assembly -m "feat(data): scaffold ProjectAegis.Data assembly"
# ... repeat for schema, scenario-bind, validation, import-smoke
```

Without Graphite, use plain git stacked branches and open PRs with base branch = parent slice.

---

## PR matrix (Cursor agent delegation)

| ID | Branch | PR title | Assign agent | Skills / tools |
|----|--------|----------|--------------|----------------|
| DATA-0 | `stack/data/p0-spec` | docs(data): P0 database intelligence design | `database-intelligence-lead` | Spec + this runbook |
| DATA-1 | `stack/data/assembly` | feat(data): add ProjectAegis.Data | `database-engineer` | `sqlite-schema-management`, gitnexus |
| DATA-2 | `stack/data/schema` | feat(data): catalog schema v1 | `database-modeler` | `database-layer-architecture` |
| DATA-3 | `stack/data/scenario-bind` | feat(data): snapshots + scenario bind | `database-engineer` | `deterministic-data-access` |
| DATA-4 | `stack/data/validation` | feat(data): validators + write gate | `database-engineer` | `provenance-audit-modeling` |
| DATA-5 | `stack/data/import-smoke` | feat(data): CMO subset import | `database-engineer` | `tools/cmano-db-crawler` |

**Agent prompt (paste into Cursor Task):**

```text
Implement DATA-{N} from docs/superpowers/plans/2026-05-30-database-intelligence-p0.md.
Design: docs/superpowers/specs/2026-05-30-database-intelligence-p0-design.md.
Before edits: npx gitnexus impact <Symbol> --repo cmano-clone
After tests: npx gitnexus detect_changes --repo cmano-clone
Branch: stack/data/<slice>. Do not touch stack/delegation/*.
```

---

## Pre-ship (each slice)

1. `dotnet test ProjectAegis.sln`
2. `npx gitnexus detect_changes --repo cmano-clone`
3. `gt submit --stack --no-interactive` (when Graphite auth OK)

---

## Delegation stack coordination

Rebase DATA-3 after delegation merges that touch `ScenarioPolicyRepository`. Stash unrelated WIP before `git checkout main` for this stack.
