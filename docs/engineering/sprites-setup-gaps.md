# Sprite base-image setup gaps

Status: DRG-696. Found during Fly.io Sprite trials 1-3 on `my-first-sprite`.
Companion to `sprites-agent-host.md`.

The default Sprite image (Ubuntu, x86_64, 8 vCPU / 15 GiB in the trial) has
`git`, `gh`, `curl`, and Node, but is missing several things cmano-clone needs.

| Gap | Symptom | Fix |
| --- | --- | --- |
| `libicu` | `dotnet` aborts: "Couldn't find a valid ICU package" | `sudo apt-get install -y libicu-dev` |
| .NET 10 SDK | not installed | `dotnet-install.sh --channel 10.0 --install-dir ~/.dotnet`, add `~/.dotnet` and `~/.dotnet/tools` to `PATH` |
| ReSharper CLI | `jb` not found | `dotnet tool install -g JetBrains.ReSharper.GlobalTools` |
| `ripgrep` | `rg: command not found` | `sudo apt-get install -y ripgrep` |
| `pwsh` | `tools/*.ps1` cannot run | not installed; use the bash equivalent in `delegation-plugin-dll-copy.md` |
| Delegation plugin DLLs | `Unity_plugin_Delegation_dll_exports_Epic_A_types_used_by_Runtime_hosts` fails: "Missing plugin DLL" | run the DLL copy step once per clone or worktree |
| GitNexus | not installed | inspect callers with `rg` instead |

## Notes

- The Sprite clock is UTC. Convert to CT when reporting.
- Installed packages and `~/.dotnet` persist across sleep/wake; running
  processes do not.
- Quoting nested commands in `sprite exec -- bash -lc '...'` is fragile. Pipe a
  script instead: `sprite exec -s <sprite> --stdin -- bash -l < job.sh`.
