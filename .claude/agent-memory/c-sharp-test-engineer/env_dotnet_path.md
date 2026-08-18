---
name: env-dotnet-path
description: dotnet CLI is not on default PATH in this sandbox; must add ~/.dotnet before running dotnet test/build
metadata:
  type: project
---

`dotnet` is not on the default `PATH` in this execution environment. Bare `dotnet test`/`dotnet build` fail with
"command not found" even though the SDK is installed.

**Fix:** prefix commands with `export PATH="$HOME/.dotnet:$PATH";` (or use the full path
`/home/username01/.dotnet/dotnet`). Confirmed working SDK reports `8.0.422` (repo pins `8.0.400` via
`global.json` with `rollForward: latestMajor`, so 8.0.422 building is expected/correct, not a version
mismatch to flag).

**Why:** Saves a failed-command round trip every session when running `src/` `.NET` test suites
(`ProjectAegis.Sim.Tests`, etc.) for this project.

**How to apply:** Before any `dotnet` invocation in this repo's sandbox, export the PATH addition first.
