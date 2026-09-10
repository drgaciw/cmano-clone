# ReSharper command-line analysis

Run the configured solution inspection from the repository root:

```powershell
.\tools\resharper\inspect.ps1
```

The default run reports warning-or-higher findings to
`artifacts/resharper/inspectcode.sarif`. To include suggestions:

```powershell
.\tools\resharper\inspect.ps1 -Severity SUGGESTION
```

The script locates `inspectcode.exe` through the per-user
`JETBRAINS_RESHARPER_CLT_HOME` environment variable and falls back to `PATH`.
Generated reports and caches are local artifacts and are ignored by Git.

`cleanupcode.exe` is installed as well, but it is intentionally not automated:
cleanup can rewrite many source files and should only be run on a clean, reviewed
branch with an agreed ReSharper settings profile.
