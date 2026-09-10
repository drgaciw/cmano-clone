# ReSharper command-line analysis

Run the configured solution inspection from the repository root with SDK 8.0.400
and ReSharper Command Line Tools 2026.2.1:

```powershell
.\tools\resharper\inspect.ps1 -Check
```

The wrapper builds first and stops on build or analyzer failure. It explicitly
enables solution-wide analysis, analyzes all target frameworks, disables personal
and global settings layers, and retains repository-shared settings. It writes a
fresh SARIF plus invocation metadata under ignored `artifacts/resharper/`.

If the system SDK rolls forward, select an isolated SDK installation:

```powershell
.\tools\resharper\inspect.ps1 -DotnetPath "$HOME/.dotnet-8.0.400/dotnet.exe" -Check
```

`JETBRAINS_RESHARPER_CLT_HOME` (process, then user environment) or `PATH` locates
the portable `inspectcode.exe`. `-ToolPath` explicitly selects a portable binary
or the `jb.exe` shim from the pinned .NET tool package used in CI:

```powershell
dotnet tool install JetBrains.ReSharper.GlobalTools --version 2026.2.1 --tool-path artifacts/resharper/cli
.\tools\resharper\inspect.ps1 -ToolPath ./artifacts/resharper/cli/jb.exe -Check
```

Do not run the wrapper concurrently with builds/tests in the same worktree:
Windows testhost can lock files needed by MSBuild. For an exploratory report
including suggestions (not a regression comparison):

```powershell
.\tools\resharper\inspect.ps1 -Severity SUGGESTION
```

## Disposition ledger and CI

`ledger.json` preserves the original 2,326 findings and their individual review
state. The source report SHA-256, SDK, tool, and analysis settings are recorded
in its baseline metadata. The initial CRLF-to-LF fingerprint migration was
reviewed against identical normalized source in all 751 files; each entry keeps
its original fingerprint. This migration is not an automatic matching fallback.

`.github/workflows/resharper.yml` installs the pinned SDK and CLI and runs this
wrapper with `-Check`. It never initializes or refreshes the baseline.

```powershell
python -m unittest discover -s tools/resharper -p 'test_*.py' -v
python tools/resharper/sarif_gate.py check --report artifacts/resharper/inspectcode.sarif --ledger tools/resharper/ledger.json
```

The gate matches rule, normalized path, and context fingerprint, disambiguating
by message and framework. It preserves multiplicity and rejects ambiguous partial
collision groups with conflicting dispositions. Location fallback is used only
when the analyzer supplies no context fingerprint. Thus moving a line does not
create a new finding, while fixing one warning cannot conceal a different new one.

Reports distinguish fixed, accepted-exception, deferred, open, new, reintroduced,
and ambiguous findings. Existing open debt remains visible and passes the
regression comparison; that is **not** remediation completion. New/reintroduced
warnings and ambiguous matches fail. Missing/malformed reports and explicit
analyzer failures also fail. Exit codes: 0 clean, 1 regression, 2 invalid input.

Ledger maintenance is a reviewed action: each fixed entry needs verification and
a resolution revision; each exception needs an owner, contract rationale,
verification, and review condition; each deferral needs an owner, rationale, and
review condition. Keep source-compatible runtime guards, DTO contracts, exact
replay identity checks, and framework polyfill namespaces when justified.
Do not demote whole inspection families to erase existing debt.

`sourceExceptions` records any reviewed, single-symbol source suppression exposed
by remediation separately from the 2,326 original entries. Currently it contains
only `CecNodeRegistration.IsSwarm`: removing its unused private copy exposed an
unread-property warning, but the public positional component remains part of
construction, deconstruction, and record equality. The source uses one
`disable once` comment with that rationale. This section does not whitelist
findings in the comparison algorithm; removing the comment without adding a real
consumer will surface an unaccepted warning and fail CI.

`sarif_gate.py init --report <sarif> --ledger <new-path>` exists only for an
explicit new baseline and refuses to overwrite an existing ledger. Ordinary
inspection and CI use `check`, never `init`.

`cleanupcode.exe` is installed as well, but it is intentionally not automated:
cleanup can rewrite many source files and should only be run with an explicit,
reviewed profile and include list. Preserve `DelegationBridge.cs`, existing
`CatalogWriteGate` paths, and Baltic v2 goldens. See the
[remediation closeout](../../docs/superpowers/reviews/2026-09-10-resharper-closeout.md)
for verified counts, batch evidence, and the seven scoped integration follow-ups.
