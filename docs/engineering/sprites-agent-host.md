# Fly.io Sprites as a headless agent host

Status: trial (DRG-693). First used on Sprite `my-first-sprite` (org `drg-amtd`).

A Sprite is a persistent, isolated Linux sandbox that sleeps when idle and
wakes on demand. We use it to run **headless** cmano-clone work for agents
without tying up a developer machine.

## What fits on a Sprite

| Good fit | Not a fit |
| --- | --- |
| Docs and ADR edits | Anything under `unity/` |
| Headless C# in `src/` with `dotnet test` | Unity Editor, Play Mode, scene or prefab work |
| ReSharper `jb inspectcode` fixes | Unity licensing / GameCI secrets |
| npm / NuGet dependency bumps | ReplayGolden re-bless (never silent, owner only) |
| GitNexus index rebuilds | Any hard-no area in `AGENTS.md` |

## One-time setup

Run from the host that has the Sprite CLI logged in:

```bash
sprite exec -s <sprite> -- bash -lc '
  sudo apt-get install -y libicu-dev          # .NET needs ICU
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir ~/.dotnet
  echo "export DOTNET_ROOT=\$HOME/.dotnet; export PATH=\$HOME/.dotnet:\$HOME/.dotnet/tools:\$PATH" >> ~/.bashrc
  dotnet tool install -g JetBrains.ReSharper.GlobalTools
  gh auth setup-git
  gh repo clone drgaciw/cmano-clone ~/cmano-clone'
```

### GitHub auth

- Use a **fine-grained PAT** scoped to `drgaciw/cmano-clone` only, with
  Contents and Pull requests set to read/write and nothing else.
- The owner loads it himself:
  `sprite exec -s <sprite> --stdin -- gh auth login --with-token`.
  Agents never see, print, or copy the token.
- Never put broader tokens or Unity credentials on a Sprite.

## Story to draft PR

1. Pick one story that is docs-only or headless, has no open PR, and is not
   owned by the Slice bots, SprintSync, or HerdrGrok.
2. Branch `sprite/drg-NNN-<slug>` from `main`.
3. Make the change, then run `dotnet test ProjectAegis.sln` and
   `jb inspectcode` scoped to changed projects.
4. Push and open **one draft PR** with `gh pr create --draft`. The body has
   `Closes DRG-NNN`, `Surface: <area>, no hard-no`, and a note that it was
   built on a Sprite.
5. Move the Linear issue to In Progress. The owner reviews and merges.

Not allowed from a Sprite: merges, `--admin`, force-push, CI / workflow /
ruleset changes, creating Sprites, or Fly billing changes.

## Gotchas

- **Missing ICU:** the base image lacks `libicu`; `dotnet` aborts until it is
  installed.
- **Sleep/wake:** files on disk persist, running processes do not. Re-run
  long jobs after a wake; keep work committed and pushed.
- **Cost:** billed for active compute and storage; idle Sprites sleep. Check
  usage in the Fly dashboard.
- **Network:** outbound only by default. Use the Sprite CLI (or the owner's
  official Tailscale tailnet) to reach it; do not expose ports publicly.
