# Bug Report

## Summary
**Title**: Negative magazine Quantity is never validated and can mask a genuine cumulative over-capacity error
**ID**: BUG-magazine-negative-quantity-masks-overcapacity
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Fixed (pending review)
**Reported**: 2026-07-06
**Reporter**: gameplay-qa-agent (qa-r2-06, automated TDD bug hunt, round 2)

## Classification
- **Category**: Gameplay / Data Integrity
- **System**: Platform catalog fitting validation (Req-21 / ADR-011 PLE-4.2), Excel platform-editor import pipeline
- **Frequency**: Always, for any `Magazines` row with a negative `Quantity` value (typo, bad manual edit, or malformed external tool output)
- **Regression**: No — a distinct, previously-uncaught gap. Related to, but not the same bug class as, `BUG-magazine-cumulative-overcapacity` (round 1): that bug was about summing quantities per-row instead of cumulatively; this bug is about the cumulative sum itself having no floor, so a negative row can silently discount the very total the round-1 fix introduced.

## Environment
- **Build**: branch `qa-r2-06-catalog-data`, HEAD (pre-fix) `e2e1342` + local uncommitted change
- **Platform**: .NET 8 / xUnit, `ProjectAegis.Data` / `ProjectAegis.Data.Excel` (Unity-independent simulation/data layer)
- **Scene/Level**: N/A — data layer / platform editor workbook validation
- **Game State**: Any platform-editor workbook (Excel-based fitting editor) with a `Magazines` row whose `Quantity` cell is negative

## Reproduction Steps
**Preconditions**: A `PlatformWorkbook` with:
- One mount, e.g. `vls-fwd` on platform `u1`, `Capacity = 32`.
- One loadout `asuw-default` for `u1`.
- Two `Magazines` rows for the *same* platform/loadout/mount: `weapon-a` Quantity `50` (already over the 32-cell capacity on its own), `weapon-b` Quantity `-20`.

1. Build/export the workbook with the two magazine rows above.
2. Call `PlatformWorkbookValidator.Validate(workbook)`.

**Expected Result**: Two findings — a `PLE-MAG-CAPACITY` (`MagazineOverCapacity`) error, because `weapon-a` alone requests 50 rounds into a 32-cell mount, and ideally a distinct error flagging that `weapon-b`'s `Quantity` of `-20` is not physically valid data.

**Actual Result (pre-fix)**: `Validate` returns an **empty** findings collection. The (correct, round-1-fixed) cumulative sum for the mount/loadout group was `50 + (-20) = 30`, which is `<= 32`, so no `MagazineOverCapacity` finding fired — the negative row silently "paid down" the genuine over-capacity condition caused by `weapon-a`. Separately, no check anywhere in the validator (or in `PlatformWorkbookImporter`, `CatalogWriteGate`, or `CatalogMagazineEntry`) ever rejects a negative `Quantity` on its own merits, so a lone negative-quantity row (with no other row in the group) also passes silently.

Because `PlatformWorkbookImporter.Plan(...)` gates staging on `PlatformImportPlan.Blocked` (`Findings.Any(Severity == Error)`), a workbook edit that introduces a negative-quantity row — whether by itself or as a mask for a real over-capacity fitting — would be **silently accepted and staged** through `CatalogWriteGate.ProposeMagazineBatch` for a mount that is, in reality, overloaded.

Notably, the sim-side consumer of the same catalog rows, `CatalogMagazineResolver.EvaluateInitialMagazine` (`src/ProjectAegis.Sim/Catalog/CatalogMagazineResolver.cs:46`), already defensively clamps with `Math.Max(0, row.Quantity)` when summing rounds for engage readiness — implicit acknowledgement elsewhere in the codebase that negative `Quantity` is untrusted input — but the data-layer workbook validator had no equivalent floor and no error for the negative value itself.

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.Data/Platform/PlatformWorkbookValidator.cs` (root cause — magazine `Quantity` accumulation had no floor at 0, and no standalone check for negative `Quantity`)
  - `src/ProjectAegis.Data/Platform/PlatformWorkbookImporter.cs` (sole caller of `PlatformWorkbookValidator.Validate`; `BuildChangedMagazineRows` parses `Quantity` with plain `ParseInt` and passes it straight through to `gate.ProposeMagazineBatch` once `Plan` reports not-`Blocked`)
- **Related systems**: `CatalogMagazineEntry` (Data layer catalog model, `Quantity` is a plain `int` with no non-negative invariant), `CatalogMagazineResolver.EvaluateInitialMagazine` (Sim layer — already clamps negative quantities defensively when computing engage-readiness totals, confirming this is untrusted/unvalidated input upstream), `CatalogWriteGate.ProposeMagazineBatch` (downstream committer — has no independent quantity check of its own, so the workbook validator is the only gate in this path)
- **Possible root cause**: The round-1 fix correctly changed the over-capacity check from per-row to a cumulative sum grouped by `(PlatformId, LoadoutId, MountId)`, but the summation itself (`loadedQuantity[...] += quantity`) never validated that each row's contribution was non-negative, so the cumulative total can be pushed arbitrarily low by a negative row, defeating the very capacity gate the sum exists to enforce. No prior test exercised a negative `Quantity` value.

## Impact Analysis (manual — GitNexus not reachable in isolated worktree)
GitNexus MCP tools/CLI were not reachable from this worktree (consistent with round 1), so impact was assessed via manual `grep` for all references to `PlatformWorkbookValidator.Validate` / `PlatformWorkbookValidator.Magazine*` across `src/`:
- **Production caller**: exactly one — `PlatformWorkbookImporter.Plan` (line ~72), same single call site as round 1, flowing into `Stage()` blocking logic via `PlatformImportPlan.Blocked`.
- **Test callers**: `PlatformWorkbookValidatorTests`, `PlatformWorkbookImporterTests`, `CatalogPhaseBValidationTests`, `CatalogPhaseBDamageValidationTests` — all reviewed; grepped specifically for any `CatalogMagazineEntry` construction with a negative `Quantity` literal (`Quantity: -`, or negative positional third-numeric-argument patterns) — **no matches** in any test file, so none was at risk of a false-positive regression from either the new `MagazineNegativeQuantity` finding or the `Math.Max(0, quantity)` floor on the cumulative sum.
- **Risk assessment**: LOW. Same single, well-contained call site as the round-1 fix; the change only tightens an existing Error-severity check (adds a new Error-severity finding, and floors an internal accumulator that previously had no floor) — it never loosens any existing check, and no existing fixture exercises negative quantities.

## Evidence
- **New test (red → green)**: `ProjectAegis.Data.Tests.Platform.PlatformWorkbookValidatorTests.Negative_magazine_quantity_is_rejected_and_cannot_mask_cumulative_overcapacity`
  - Path: `src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookValidatorTests.cs`
  - Before fix: **did not compile** (`CS0117: 'PlatformWorkbookValidator' does not contain a definition for 'MagazineNegativeQuantity'`) — the constant did not exist, which is the correct "red" state proving the check was entirely absent (confirmed manually first that, with the constant temporarily stubbed out, the `MagazineOverCapacity` assertion alone failed with `Assert.Contains() Failure: Filter not matched` against an empty/absent finding, i.e. the masking behavior reproduced as described).
  - After fix: **PASSED** (both the new `MagazineNegativeQuantity` finding and the `MagazineOverCapacity` finding are now present).
- **Full suite before/after**:
  - `ProjectAegis.Data.Tests`: 478/478 passed (baseline was 477/477; +1 is the new test)
  - `ProjectAegis.Data.Excel.Tests`: 5/5 passed (unchanged from baseline)
  - `ProjectAegis.Sim.Tests`: 285/285 passed (unchanged from baseline) — run as an additional sanity check because `ProjectAegis.Sim.Catalog.CatalogMagazineResolver` shares the `CatalogMagazineEntry.Quantity` field and already has its own independent negative-quantity clamp; confirmed no interaction/regression.

## Related Issues
- `BUG-magazine-cumulative-overcapacity` (round 1) — introduced the cumulative-sum mechanism that this bug's negative-quantity gap could silently defeat.
- Req-21 / ADR-011 PLE-4.2 (cross-sheet fitting validation)

## Notes
Fix implemented in `PlatformWorkbookValidator.Validate`:
1. Added a new finding code `MagazineNegativeQuantity` (`PLE-MAG-QTY-NEGATIVE`), Error severity, raised per-row whenever a `Magazines` row's `Quantity` is negative.
2. Changed the cumulative capacity accumulator from `loadedQuantity[key] += quantity` to `loadedQuantity[key] += Math.Max(0, quantity)`, so a negative row can never reduce the group's total below what its non-negative rows alone would produce — aligning the workbook validator's treatment of negative quantities with the pre-existing defensive clamp already used at runtime in `CatalogMagazineResolver.EvaluateInitialMagazine`.

No existing finding codes, message formats, or public signatures were changed; this is purely additive (new code) plus an internal-only accumulation fix, so no existing test assertions were affected.
