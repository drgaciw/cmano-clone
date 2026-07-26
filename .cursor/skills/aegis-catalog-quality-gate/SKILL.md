---
name: aegis-catalog-quality-gate
description: >
  Pre-merge catalog quality checklist: zero quarantine for production approve, Import and
  WriteGate tests, no tracked *.db3, optional catalog_intelligence_run / kill-chain smoke.
  Use before merging catalog fixture or import changes, or when asked to verify catalog import.
---

# Aegis Catalog Quality Gate

Pre-merge checklist for catalog fixture edits, import pipeline changes, and approve workflows. Run before opening a PR that touches `tools/cmano-db-crawler/fixtures/`, import code, or committed catalog DBs.

## When to use

- Before merging catalog fixture or import pipeline changes
- After approving batches into a scratch or production catalog DB
- User asks to "verify catalog import", "catalog quality gate", or "pre-merge catalog check"
- CI/local gate before human approves production paths

## Quick verify script

From repo root:

```powershell
./scripts/verify-catalog-import.ps1
```

The script:
1. Fails if any `*.db3` appears in `git ls-files` (CMO game DB policy)
2. Runs `dotnet test` on Import tests filtered to `CmoMarkdown`
3. Exits non-zero on any failure

## Pre-merge checklist

### 1. Quarantine zero for production approve

- Review `*-propose.json` and quarantine files under `scratch/…`
- `quarantinedCount` must be **0** before production `catalog_write_approve`
- Fix fixture order (weapons before platforms), orphan weapon names, or import floors

### 2. Import unit tests

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~Import" -v minimal
```

Focused CmoMarkdown filter (matches verify script):

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~CmoMarkdown" -v minimal
```

### 3. Write gate tests

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~WriteGate" -v minimal
```

### 4. No proprietary `*.db3` tracked

```bash
git ls-files '*.db3'
# must return empty
```

Root `.gitignore` ignores `*.db3`. Committed Aegis catalogs use `*.db` under `assets/data/catalog/`.

### 5. Optional — scenario-impacting changes

When fixtures affect Baltic/scenario engage chains:

```bash
# Catalog intelligence pass
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_intelligence_run \
  --db assets/data/catalog/baltic_patrol.db

# Kill-chain dependency report
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_kill_chain_report \
  --db assets/data/catalog/baltic_patrol.db
```

Also consider Baltic replay smoke per [dual-track-cmo-analysis-and-catalog.md](../../docs/engineering/dual-track-cmo-analysis-and-catalog.md) weekly checklist.

### 6. Policy guard

- No CMO table readers (`DataAircraft`, `DataSensor`, …) pointed at game DBs
- No `*.db3` copies under `scratch/` escaping gitignore
- Catalog mutations only through write gate (ADR-006)

## Full data-layer test baseline

```bash
dotnet build src/ProjectAegis.Data/ProjectAegis.Data.csproj
dotnet test  src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj -v minimal
```

~406 tests in `ProjectAegis.Data.Tests`; part of the solution ≥1232-test baseline.

## Failure triage

| Symptom | Likely cause |
|---------|--------------|
| CmoMarkdown test fail | Parser regression — check slug, domain, range extraction |
| WriteGate test fail | Approve guard or golden hash drift — regenerate intentionally |
| Quarantine non-zero | Weapon name mismatch, import order, TRL/confidence floor |
| `kill_chain:*` on approve | Missing platform/weapon/mount edge — run kill-chain report |
| `*.db3` in git ls-files | Policy violation — remove from index, never commit game DBs |

## Related skills

- Curator workflow: [aegis-catalog-curator](../aegis-catalog-curator/SKILL.md)
- Fixture authoring: [aegis-markdown-fixture-author](../aegis-markdown-fixture-author/SKILL.md)
- Approve gate: [aegis-write-gate-approve](../aegis-write-gate-approve/SKILL.md)

## Further reading

- [dual-track-cmo-analysis-and-catalog.md](../../docs/engineering/dual-track-cmo-analysis-and-catalog.md)
- [catalog-write-gate.md](../../docs/engineering/catalog-write-gate.md)
- [cmo-markdown-import.md](../../docs/engineering/cmo-markdown-import.md)
- [qa-gauntlet.md](../../docs/engineering/qa-gauntlet.md)
