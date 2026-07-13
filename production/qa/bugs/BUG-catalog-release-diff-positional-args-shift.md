# Bug Report

## Summary
**Title**: `catalog_release_diff`'s documented positional invocation form (`--db <path> <from> <to>` with no `--from`/`--to` flags) misreads the `--db` value as `<fromReleaseVersion>`, shifting every subsequent positional argument by one
**ID**: BUG-catalog-release-diff-positional-args-shift
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Open
**Reported**: 2026-07-06
**Reporter**: gameplay-qa-agent (qa-r2-10-cli-integration)

## Classification
- **Category**: CLI / Tooling (Program.cs argument dispatch, `catalog_release_diff` verb)
- **System**: `ProjectAegis.MissionEditor.Cli` top-level entry point (`Program.cs`'s `RunCatalogReleaseDiff` local function)
- **Frequency**: Always (100% reproducible) whenever `catalog_release_diff` is invoked in its documented positional form (`--db <path> <from> <to>`, no `--from`/`--to` flags)
- **Regression**: No — present since `catalog_release_diff` was introduced (S32-07/S65-03); not caused by this loop's changes. Different bug class from `BUG-catalog-report-databasepath-misreport.md` (that one was report commands echoing the wrong `databasePath` on silent fallback; this one is an argument-parsing index shift in `Program.cs` itself, affecting a different command and a different code path).

## Environment
- **Build**: commit `e2e1342` (branch `qa-r2-10-cli-integration`, worktree of `cmano-clone` main)
- **Platform**: Linux sandbox, .NET 8 SDK, `dotnet test` / built `ProjectAegis.MissionEditor.Cli.dll` invoked via `dotnet <dll>` (no Unity Editor available)
- **Scene/Level**: N/A — plain .NET CLI (`ProjectAegis.MissionEditor.Cli`), not Unity PlayMode
- **Game State**: N/A

## Reproduction Steps
**Preconditions**: A valid catalog `.db` file (any path), with two release versions recorded via the normal write-gate/snapshot-binder pipeline (e.g. `fromVersion`, `toVersion`).

1. Invoke the CLI using the exact positional form documented in `CatalogReleaseDiffCommand.PrintHelp` (and printed by `catalog_release_diff --help`):
   `catalog_release_diff --db <catalog.db> <fromReleaseVersion> <toReleaseVersion>` — i.e. omit `--from`/`--to` flags.
2. Observe the process output/exit code.

**Expected Result**: Behaves identically to the flag form `catalog_release_diff --db <catalog.db> --from <fromReleaseVersion> --to <toReleaseVersion>` — a correct diff report comparing `fromReleaseVersion` to `toReleaseVersion`, exit code 0.

**Actual Result**: `Program.cs`'s `RunCatalogReleaseDiff` local function resolves the positional fallback with:
```csharp
var positional = args.Where(a => !a.StartsWith("-", StringComparison.Ordinal)).ToArray();
from ??= positional[0];
to   ??= positional[1];
```
This filter only removes tokens that *themselves* start with `-` (i.e. the flag name `--db`) — it does **not** also remove the token immediately following `--db` (the db path itself, which is a plain value and does not start with `-`). So for `args = ["--db", "<path>", "<fromVersion>", "<toVersion>"]`, `positional = ["<path>", "<fromVersion>", "<toVersion>"]`, giving:
- `from = "<path>"` (the db file path, **wrong** — should be `<fromVersion>`)
- `to = "<fromVersion>"` (**wrong** — should be `<toVersion>`)
- `<toVersion>` is silently dropped entirely.

Manually reproduced against a real (empty) db:
```
$ dotnet ProjectAegis.MissionEditor.Cli.dll catalog_release_diff --db /tmp/manualtest-2.db from-ver to-ver
Release version '/tmp/manualtest-2.db' not found in db_release. (Parameter 'releaseVersion')
```
The error cites the **db file path** as if it were a release version — proof the shift occurs. With real seeded release data (both `fromVersion`/`toVersion` present in `db_release`), the same shift instead of failing outright would silently produce a diff against the wrong `from`/`to` pair and misreport `fromReleaseVersion`/`toReleaseVersion` in the JSON payload — exactly the kind of "pipeline step silently produces wrong output" this loop was asked to hunt for.

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.MissionEditor.Cli/Program.cs` (`RunCatalogReleaseDiff`, fixed this loop)
  - `src/ProjectAegis.MissionEditor.Cli/CliArgParser.cs` (new `GetPositional` helper added this loop)
- **Related systems**: `CatalogReleaseDiffCommand.Run`/`PrintHelp` (`src/ProjectAegis.MissionEditor.Cli/CatalogReleaseDiffCommand.cs`) — the command class itself is correct and was not the bug; the bug is entirely in how `Program.cs` resolves its `from`/`to` arguments before calling `Run`.
- **Root cause**: The positional-args fallback filtered on "does the token start with `-`" without also excluding the *value* consumed by a preceding value-taking flag (`--db`). This is the exact same category of "recompute something Program.cs's args array already encodes, but with weaker logic than the primary flag parser" as the round-1 `databasePath` bug, just manifesting in argument *parsing* rather than *report output*.

## Evidence
- **New regression test**: `ProjectAegis.MissionEditor.Cli.Tests.CatalogReleaseDiffCliArgsTests.catalog_release_diff_positional_invocation_resolves_same_diff_as_flag_invocation` (`src/ProjectAegis.MissionEditor.Cli.Tests/CatalogReleaseDiffCliArgsTests.cs`)
  - This is the only test in the suite that exercises `Program.cs`'s own argument-dispatch code (all other `catalog_release_diff` tests call `CatalogReleaseDiffCommand.Run` directly with already-correct, pre-parsed `from`/`to` strings, so they never touch the buggy positional-fallback code path). It spawns the actual built `ProjectAegis.MissionEditor.Cli.dll` via `Process` (mirroring the `ProcessStartInfo` pattern already used by `BranchIntegrationPhase0SmokeTests`), seeds two real release versions, and invokes the documented positional CLI form.
  - Before fix (red): `Assert.Equal(0, exitCode)` failed — actual exit code `1`, with the process printing `Release version '<dbPath>' not found in db_release.` to stderr (the db path was consumed as `fromReleaseVersion`).
  - After fix (green): exit code `0`; `fromReleaseVersion`/`toReleaseVersion` in the JSON payload match the intended versions; `diffCount == 3` with `Added`/`Changed`/`Removed` rows, matching the equivalent flag-form test (`CatalogImport_catalog_release_diff_emits_sorted_delta_rows` in `CatalogReleaseDiffCommandTests.cs`).
- **Fix**: Added `CliArgParser.GetPositional(string[] args, params string[] valueFlags)`, which skips both a recognized flag token and the value token immediately following it (not just tokens that themselves start with `-`). `Program.cs`'s `RunCatalogReleaseDiff` now calls `CliArgParser.GetPositional(args, "--db")` instead of the naive `args.Where(a => !a.StartsWith("-"))` filter.

## Related Issues
- `production/qa/bugs/BUG-catalog-report-databasepath-misreport.md` — different bug, different command family (read-only report commands' `databasePath` field on silent-fallback), but the same underlying anti-pattern (re-deriving something from raw `args`/inputs with weaker logic than the code path that already resolved it correctly). Not a duplicate; no shared files changed.

## Notes
- **Impact/blast-radius check** (GitNexus MCP not reachable from this isolated worktree; manual Grep-based caller check performed instead):
  - `RunCatalogReleaseDiff`'s only caller is the `case "catalog_release_diff":` in `Program.cs`'s top-level switch (line ~84); no other code calls this local function.
  - The buggy `args.Where(a => !a.StartsWith("-", StringComparison.Ordinal))` pattern was grepped across all of `src/` and appears exactly once, in this one function — no other command shares this bug.
  - `CliArgParser.GetPositional` is a brand-new additive public method; grepped for existing callers (none) before adding it, so it cannot regress any other command.
  - The fix only changes behavior on the positional-fallback branch (taken only when `--from`/`--to` flags are both absent); the flag-based invocation form — which both pre-existing `CatalogReleaseDiffCommandTests` tests exercise indirectly via `CatalogReleaseDiffCommand.Run`, and which is presumably the form used by any current MCP/CI caller that already passes `--from`/`--to` — is untouched and unaffected.
  - Risk: **LOW**. Single call site, additive helper, fix only activates on a previously-broken code path.
- Regression batches run after the fix (all green, see final report for exact commands): `CatalogReleaseDiffCommandTests` + `CatalogReleaseDiffCliArgsTests` + `CatalogDependencyGraphCommandTests` + `CatalogKillChainReportCommandTests` + `CatalogLinkReportCommandTests` (10/10); `SampleCompletePipelineTests` + `McpMissionToolCliTests` + `McpToolsManifestTests` (7/7); `CatalogEntityMapCommandTests` + `CatalogImportMarkdownCommandTests` + `CatalogIntelligenceRunCommandTests` + `CatalogPlatformBrowseCommandTests` + `CatalogWriteCommandTests` (18/18); `MissionAddFerryCommandTests` + `MissionPlanSuggestCommandTests` + `ScenarioCommsStatusCommandTests` + `ScenarioCyberStatusCommandTests` + `ScenarioNearFutureSpawnCommandTests` (10/10); `ScenarioSimulateSampleCliTests` + `ScenarioUndoCliTests` + `ScenarioValidateCliTests` (21/21). `BranchIntegrationPhase0SmokeTests` was intentionally skipped in this loop (it triggers a full solution rebuild via bash script and is unrelated to the files touched — no full-solution changes were made).
