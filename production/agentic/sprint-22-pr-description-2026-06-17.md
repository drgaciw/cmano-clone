# feat(sprint-22): Platform Editor Phase A, DB Intelligence P1, doctrine panel, telemetry & OSINT TL

**Base:** `main` @ `34d4e6f` (local; **8 commits behind** `origin/main`)  
**Target:** `main` (after `git pull --rebase origin main`)  
**Indexed commit (GitNexus):** `34d4e6f` — re-index after commits land  
**QA verdict:** **APPROVED WITH CONDITIONS** (`production/qa/sprint-22-signoff-2026-06-17.md`)  
**Commit plan:** `production/agentic/sprint-22-commit-plan-2026-06-17.md` (5 local commits, not yet applied)

---

## Summary

Sprint 22 delivers Platform Editor Phase A: write-gate staging for mounts, loadouts, magazines, and comms; MCP/CLI verbs for platform workbook export, import, and diff; and ADR-011 documenting the canonical-text interim I/O boundary. Database Intelligence P1 extends `CmoMarkdownImporter` for platform and weapon entries, and the Unity Doctrine Inheritance Panel ships as a headless round-trip via ADR-010 with zero `DelegationBridge.cs` changes.

Nice-to-have stories add an advisory `IBalanceTelemetrySink` accumulator (±8% win-rate flag, default off) and OSINT `OsintCatalogMapper` TL routing (`proposedTL` / `targetDoc` → `TrlLevel` + branch metadata). All seven stories are complete; implementation remains **uncommitted locally** and must be rebased onto upstream doctrine host fix `a95e06f` before push.

---

## Stories

| ID | Story | Priority | Test evidence | Result |
|----|-------|----------|---------------|--------|
| **22-1** | Extend `PlatformWorkbookImporter` write-gate to Mounts/Loadouts/Magazines/Comms | must-have | `dotnet test src/ProjectAegis.Data.Tests --filter "FullyQualifiedName~Platform"` → **25/25 PASS**; migration `007_platform_editor_phase_a.sql`; `ProposeMountBatch` / `Loadout` / `Magazine` / `Comms` wired in `CatalogWriteGate` | **PASS** |
| **22-2** | CLI verbs `platform_export_xlsx` / `platform_import_xlsx` / `platform_diff_xlsx` | must-have | `dotnet test src/ProjectAegis.MissionEditor.Cli.Tests` → **21/21 PASS**; `McpToolsManifestTests` registers all three verbs; CLI smoke `platform_export_xlsx --out /tmp/smoke-export.platform.txt` → exit 0, `ok: true` | **PASS** |
| **22-3** | ADR-011 platform-editor-excel-roundtrip | must-have | `docs/architecture/adr-011-platform-editor-excel-roundtrip.md` exists; referenced from Req 21; status = **Proposed** (not Accepted) | **PASS WITH NOTE** |
| **22-4** | Extend `CmoMarkdownImporter` to platform + weapon/mount entries | should-have | `CmoMarkdownImporterTests.cs` → **22/22 PASS**; Baltic + `weapon-mini.md` fixtures; orphan guard (DBI-1.4); `ProposePlatformBatch` / `ProposeWeaponBatch` staging verified; `ApproveBatch` commit path **deferred** | **PASS** |
| **22-5** | Unity Doctrine Inheritance Panel (Req 13) | should-have | `DoctrineOverrideCommandTests` (4) + `DoctrineInheritanceProjectionTests` + `DoctrineInheritancePanelBinderTests` (8) + `PlayModeSmokeHarnessTests` doctrine row (1) → **17 doctrine-scoped PASS**; UnityAdapter filter **13/13**; Delegation filter **8/8**; **zero touch** `DelegationBridge.cs` | **PASS** (headless proxy) |
| **22-6** | `IBalanceTelemetrySink` real accumulator + win-rate flag | nice-to-have | `BalanceTelemetryAccumulatorTests` (7) + `BalanceTelemetrySinkFactoryTests` (2) + `BalanceTelemetryGoldenTests` (1) → **10/10 PASS**; `enableBalanceDrift` default false; golden hash pinned; no `IWriteGate` bypass | **PASS** |
| **22-7** | OSINT `OsintCatalogMapper` TL routing | nice-to-have | `OsintCatalogMapperTests` → TL routing (3 theory cases), clamp, determinism, no-bypass, quarantine, write-gate staging → **8/8 PASS**; extend-only verified on `CatalogWriteGate` sensor path | **PASS** |

---

## Test commands & counts

**Pre-commit gate** (run once before any commit; verified 2026-06-17):

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet build ProjectAegis.sln -v q

dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~Platform|FullyQualifiedName~CmoMarkdown|FullyQualifiedName~WriteGate" -v q
# → 49/49 PASS (25 Platform + 22 CmoMarkdown + 2 WriteGate overlap)

dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj -v q
# → 21/21 PASS

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~Doctrine|FullyQualifiedName~PlayModeSmoke" -v q
# → 13/13 PASS

dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "FullyQualifiedName~Doctrine" -v q
# → 8/8 PASS

dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~Telemetry" -v q
# → 10/10 PASS (22-6)

dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~OsintCatalogMapper" -v q
# → 8/8 PASS (22-7)
```

| Suite | Filter / scope | Count | Status |
|-------|----------------|-------|--------|
| Data — Platform | `FullyQualifiedName~Platform` | 25 | PASS |
| Data — CmoMarkdown | `FullyQualifiedName~CmoMarkdown` | 22 | PASS |
| Data — WriteGate (scoped) | `FullyQualifiedName~WriteGate` | (in 49 aggregate) | PASS |
| Data — Telemetry | `FullyQualifiedName~Telemetry` | 10 | PASS |
| Data — OsintCatalogMapper | `FullyQualifiedName~OsintCatalogMapper` | 8 | PASS |
| MissionEditor.Cli | full | 21 | PASS |
| Delegation.UnityAdapter | `Doctrine\|PlayModeSmoke` | 13 | PASS |
| Delegation | `Doctrine` | 8 | PASS |
| CLI smoke | `platform_export_xlsx` | 1 | PASS |
| **Closeout aggregate (22-1–22-5)** | pre-commit block | **91** | PASS |
| **Full sprint aggregate (22-1–22-7)** | all scoped suites above | **~109** | PASS |

**Not executed:** full-solution `dotnet test ProjectAegis.sln`, Unity Editor PlayMode batch, replay golden harness.

**Must-have smoke (earlier gate):** `production/qa/smoke-2026-06-17.md` — **46/46 PASS** @ `34d4e6f` (22-1–22-3 only).

---

## GitNexus blast-radius summary

**Re-index:** `npx gitnexus analyze --force` @ `34d4e6f` (17.6s)  
**Graph:** 8,671 nodes / 17,891 edges (+2,140 vs Sprint 11 baseline)  
**Evidence:** `production/qa/sprint-22-gitnexus-2026-06-17.md`

### `detect_changes` (uncommitted, scope=all)

| Metric | Value |
|--------|-------|
| Changed symbols | **75** |
| Changed files | **24** |
| Affected processes | **29** |
| Risk level | **CRITICAL** |

### CRITICAL symbols (extend-only; regression-tested)

| Symbol | Sprint touch | Regression note |
|--------|--------------|-----------------|
| `CatalogWriteGate` | `ProposePlatformBatch`, `ProposeWeaponBatch`, `ProposeMountBatch`, `Loadout`, `Magazine`, `Comms`, `ApproveBatch`, `DeleteStagingRows` | Sensor staging path unchanged; Platform/CmoMarkdown/WriteGate filters green |
| `CmoMarkdownImporter` / `CmoMarkdownImportProposer` | platform + weapon + mount parsing | `RunCatalogImportMarkdown → *` sensor path — 22 dedicated tests |
| `OsintCatalogMapper` | `ResolveTrlLevel`, `ResolveBranchTag`, `ToSensorBinding(s)` | TL routing + determinism + no direct gate mutation |
| `DoctrineOverrideCommand` / `DelegationBridgeHost.TrySetDoctrineOverride` | headless doctrine round-trip | Zero `DelegationBridge.cs` edits; conflict risk with upstream `a95e06f` |

### Affected process families

- `RunCatalogImportMarkdown → *` — sensor path regression risk
- `ProposePlatformWeaponMounts → *` — new S22-04 path
- `OnApproveSelected → ApproveBatch` — staging commit path (approve deferred for platform/weapon)

**Post-merge:** `npx gitnexus analyze` to sync index; `gitnexus impact CatalogWriteGate direction=upstream` on further gate edits.

---

## Conditions & known deferrals

| ID | Condition / deferral | Owner hint | Blocks merge? |
|----|----------------------|------------|---------------|
| C1 | **ADR-011 status = Proposed** — promote to Accepted or record waiver before Production → Polish architecture gate | writer / lead-programmer | No (documented condition) |
| C2 | **ClosedXML `.xlsx` adapter (Phase B)** — Phase A uses `CanonicalTextWorkbookIo`; export smoke returns canonical text workbook, not binary `.xlsx` | team-data | No |
| C3 | **`ApproveBatch` commit path** for staged platform/weapon rows — staging-only verified in S22-04 | team-data | No |
| C4 | **Unity Editor PlayMode** — `DoctrineInheritancePanelHost` visual sign-off deferred; headless proxy (projection/binder/command tests) sufficient for sprint closeout | team-unity | No |
| C5 | **Full `ProjectAegis.sln` test gate** — kickoff DoD item; run post-rebase before merge | c-sharp-devops-engineer | Recommended |
| C6 | **Git sync** — local `main` **8 commits behind** `origin/main`; doctrine host overlap with `a95e06f`; dirty tree blocks `git pull --ff-only` | implementer | **Yes** (pre-push) |
| C7 | **CanonicalId determinism** on new `Catalog*` types — qa-plan note; track in Sprint 23 | team-data | No |

---

## Reviewer checklist

- [ ] **Write-gate extend-only:** no direct catalog mutation outside `IWriteGate`; new overloads follow existing `ProposeSensorBatch` pattern
- [ ] **Platform importer:** mounts/loadouts/magazines/comms round-trip and validator tests cover happy + reject paths
- [ ] **CLI/MCP parity:** `platform_export_xlsx`, `platform_import_xlsx`, `platform_diff_xlsx` registered in `mcp-tools.json` and `McpToolsManifestTests`
- [ ] **ADR-011 alignment:** canonical text I/O boundary documented; Phase B `.xlsx` scope clearly deferred
- [ ] **CmoMarkdown:** platform/weapon fixtures parse correctly; orphan guard fires; no bypass of import gate
- [ ] **Doctrine panel:** `SetDoctrineOverride` round-trip via `DoctrineOverrideCommand`; **no edits** to `DelegationBridge.cs`
- [ ] **Doctrine rebase:** compare local `DelegationBridgeHost.cs` / deleted `DelegationBridgeHost.Doctrine.cs` with upstream `a95e06f`
- [ ] **Telemetry:** `enableBalanceDrift` default false; advisory-only; golden hash stable; no write-gate side effects
- [ ] **OSINT TL routing:** `proposedTL` clamped 1–9; `targetDoc` → branch tag; deterministic ordering preserved
- [ ] **GitNexus CRITICAL symbols:** extend-only verified; re-run `detect_changes` if gate symbols change post-review
- [ ] **Production docs:** `production/sprint-status.yaml` reflects all 7 stories done; retro + sign-off paths valid

---

## Suggested merge strategy

### Recommended: single PR after local commit + rebase

1. Run pre-commit test block (see above).
2. Apply **5-commit sequence** from `production/agentic/sprint-22-commit-plan-2026-06-17.md`:
   - `feat(data): extend platform write-gate for mounts, loadouts, magazines, comms` (22-1)
   - `feat(cli): add platform_export/import/diff_xlsx MCP verbs` (22-2)
   - `feat(data): CmoMarkdown platform and weapon import staging` (22-4)
   - `feat(doctrine): inheritance panel headless round-trip and tests` (22-5)
   - `chore(sprint): Sprint 22 closeout evidence, retro, and status` (meta + 22-3 refs)
   - *(22-6 telemetry + 22-7 OSINT: fold into commit 3 or add `feat(data): balance telemetry and OSINT TL routing` before chore)*
3. `git pull --rebase origin main` — resolve doctrine host conflicts preferring upstream `a95e06f` pattern where equivalent.
4. Re-run scoped test block post-rebase.
5. Open **one PR** targeting `main` with this description.

### Why single PR (not stack)

- All stories share `CatalogWriteGate` blast radius; splitting increases rebase/conflict surface.
- Commit plan already provides logical atomic commits within one PR for reviewability.
- Upstream is linear (8 commits behind, not diverged); rebase-then-single-PR is lowest friction.

### Do **not** use bare `git pull --ff-only` on dirty tree

Working tree must be committed or stashed first. Prefer **rebase** over ff-only given doctrine file overlap.

---

## Diff scope (estimated)

| Metric | Value |
|--------|-------|
| Working-tree entries (`git status --short`, excl. `.omnigent/`, `.cursor/hooks/`) | **~45** |
| Sprint-22 commit-plan scope (modified + untracked project files) | **29** (+ telemetry/osint nice-have, QA/gitnexus docs) |
| Tracked diff stat | **25 files, +1,154 / −171** |
| New source files (untracked) | CLI commands (3), CmoMarkdown tests, catalog types (2), doctrine tests (3), telemetry module, OsintCatalogMapper tests, fixtures (2) |
| Deleted | `DelegationBridgeHost.Doctrine.cs` (consolidated into `DelegationBridgeHost.cs`) |

---

## References

- Kickoff: `production/sprints/sprint-22-platform-editor-db-doctrine.md`
- Retro: `production/retrospectives/retro-sprint-22-2026-06-17.md`
- Smoke: `production/qa/smoke-2026-06-17.md`
- Sign-off: `production/qa/sprint-22-signoff-2026-06-17.md`
- GitNexus: `production/qa/sprint-22-gitnexus-2026-06-17.md`
- Commit plan: `production/agentic/sprint-22-commit-plan-2026-06-17.md`
- ADR-011: `docs/architecture/adr-011-platform-editor-excel-roundtrip.md`

*Generated by Sprint 22 PR Package agent — 2026-06-17. Documentation only; no git commit or push.*