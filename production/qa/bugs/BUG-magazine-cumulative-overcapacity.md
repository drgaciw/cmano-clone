# Bug Report

## Summary
**Title**: Mixed-weapon-type magazine loadouts can exceed mount capacity without validation error
**ID**: BUG-magazine-cumulative-overcapacity
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Fixed (pending review)
**Reported**: 2026-07-06
**Reporter**: gameplay-qa-agent (qa-loop-06, automated TDD bug hunt)

## Classification
- **Category**: Gameplay / Data Integrity
- **System**: Platform catalog fitting validation (Req-21 / ADR-011 PLE-4.2), Excel platform-editor import pipeline
- **Frequency**: Always, for any mount loaded with 2+ magazine rows (weapon types) whose individual quantities each stay under capacity but whose sum exceeds it
- **Regression**: No — appears to be present since the validator was introduced; no prior test exercised more than one magazine row per mount

## Environment
- **Build**: branch `qa-loop-06-catalog-data`, HEAD `5eab203` (pre-fix)
- **Platform**: .NET 8 / xUnit, ProjectAegis.Data / ProjectAegis.Data.Excel (Unity-independent simulation/data layer)
- **Scene/Level**: N/A — data layer / platform editor workbook validation
- **Game State**: Any platform-editor workbook (Excel-based fitting editor) with a mount carrying more than one weapon type in the same loadout

## Reproduction Steps
**Preconditions**: A `PlatformWorkbook` (as produced by `PlatformWorkbookExporter` / edited via the Excel platform editor) with:
- One mount, e.g. `vls-fwd` on platform `u1`, `Capacity = 32`.
- One loadout `asuw-default` for `u1`.
- Two `Magazines` rows for the *same* platform/loadout/mount: `weapon-a` Quantity 20, `weapon-b` Quantity 20 (sum = 40, capacity = 32).

1. Build/export the workbook with the two magazine rows above.
2. Call `PlatformWorkbookValidator.Validate(workbook)`.

**Expected Result**: A `PLE-MAG-CAPACITY` (`MagazineOverCapacity`) error finding, since the mount is asked to hold 40 rounds in a 32-cell capacity (a realistic mixed VLS-cell loadout, e.g. ESSM + Tomahawk sharing a strike-length cell bank).

**Actual Result (pre-fix)**: `Validate` returns an **empty** findings collection. `PlatformWorkbookValidator.Validate` checked each magazine row's `Quantity` against the mount's `Capacity` **independently**, never summing quantities that share the same (PlatformId, LoadoutId, MountId). Since 20 ≤ 32 for each row individually, no error was raised even though the mount is overloaded by 8 rounds in aggregate.

Because `PlatformWorkbookImporter.Plan(...)` gates staging on `PlatformImportPlan.Blocked` (`Findings.Any(Severity == Error)`), this meant an over-capacity mixed-weapon fitting produced via the Excel platform editor would be **silently accepted and staged** into the write-gate instead of being rejected for human review.

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.Data/Platform/PlatformWorkbookValidator.cs` (root cause — `MagazineOverCapacity` check, previously per-row)
  - `src/ProjectAegis.Data/Platform/PlatformWorkbookImporter.cs` (sole caller of `PlatformWorkbookValidator.Validate`; consumes findings to block staging)
- **Related systems**: `CatalogMagazineEntry` / `CatalogMount` (Data layer catalog model), `CatalogWriteGate.ProposeMagazineBatch` (downstream committer — has no independent capacity check of its own, so the workbook validator was the only capacity gate in this path)
- **Possible root cause**: The original implementation validated `MagazineOverCapacity` inline inside the per-row loop (`else if (quantity > capacity)`), which is correct only when a mount holds a single weapon type per loadout. It does not match how VLS/mixed-load mounts are typically modeled (multiple weapon types sharing a single mount's cell capacity).

## Impact Analysis (manual — GitNexus not reachable in isolated worktree)
GitNexus MCP tools/CLI (`.gitnexus/run.cjs`) were not reachable from this worktree, so impact was assessed via manual `grep` for all callers of `PlatformWorkbookValidator` across `src/`:
- **Production caller**: exactly one — `PlatformWorkbookImporter.Plan` (line ~72), which flows into `Stage()` blocking logic via `PlatformImportPlan.Blocked`.
- **Test callers**: `PlatformWorkbookValidatorTests`, `PlatformWorkbookImporterTests`, `CatalogPhaseBValidationTests`, `CatalogPhaseBDamageValidationTests`, `PlatformWorkbookPhaseBImportTests` — all reviewed; none constructs more than one `CatalogMagazineEntry` sharing a (PlatformId, LoadoutId, MountId) triple, so none was at risk of a false-positive regression from the cumulative check.
- **Risk assessment**: LOW. Single, well-contained call site; fix only tightens (never loosens) an existing Error-severity check, and the full `ProjectAegis.Data.Tests` suite (477 tests) plus `ProjectAegis.Data.Excel.Tests` (5 tests) pass unchanged aside from the one new test.

## Evidence
- **New test (red → green)**: `ProjectAegis.Data.Tests.Platform.PlatformWorkbookValidatorTests.Cumulative_over_capacity_across_weapon_types_in_same_mount_is_flagged_as_error`
  - Path: `src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookValidatorTests.cs`
  - Before fix: **FAILED** — `Assert.Contains() Failure: Filter not matched in collection / Collection: []` (confirms no finding was produced for the over-capacity mixed-weapon fitting).
  - After fix: **PASSED**.
- **Full suite before/after** (only production-code before this fix was applied — test-only "before" run showed the new test red; the suites below are the actual regression run to confirm no unrelated tests broke):
  - `ProjectAegis.Data.Tests`: 477/477 passed (baseline was 476/476; +1 is the new test)
  - `ProjectAegis.Data.Excel.Tests`: 5/5 passed (unchanged from baseline)

## Related Issues
- Req-21 / ADR-011 PLE-4.2 (cross-sheet fitting validation)
- DBI-2.4 (human-approval threshold for large batches) — unaffected by this fix

## Notes
Fix implemented in `PlatformWorkbookValidator.Validate`: magazine rows are now grouped by `(PlatformId, LoadoutId, MountId)` and their `Quantity` values summed; the `MagazineOverCapacity` finding is now emitted once per group when the *cumulative* quantity exceeds the mount's `Capacity`, rather than once per row compared against the full capacity in isolation. The `MagazineUnknownMount` / `MagazineUnknownLoadout` per-row referential checks are unchanged. Message text was updated from `"Magazine '{weaponId}' loads {quantity}..."` to `"Loadout '{loadoutId}' loads {totalQuantity} total round(s)..."` to reflect the cumulative nature of the check; no test asserted on the previous message string (only on the finding `Code`), so this is not a breaking change to the public contract.
