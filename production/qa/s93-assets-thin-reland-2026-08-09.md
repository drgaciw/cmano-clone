# QA note — S93 assets thin re-land (2026-08-09)

**PR:** https://github.com/drgaciw/cmano-clone/pull/442  
**Branch:** `drgamtd/s93-assets-thin-reland-2026-08-09`  
**Work package:** WP-S93-assets-thin (`production/qa/pr-324-residual-2026-08-09.md`)  
**Source tip:** `b8e174b8` (closed PR #324)

## Scope

Four binary/USS paths only; extracted from closed PR tip. No C#, no Game-Requirements, no ADRs, no tests.

| Path | Bytes |
|------|------:|
| `production/assets/audio/sfx_policy_denial.wav` | 11564 |
| `production/assets/audio/sfx_roe_change.wav` | 15404 |
| `production/assets/ui/MainMenuShell.uss` | 591 |
| `production/assets/ui/ScenarioSelect.uss` | 697 |

## Checks

- [x] Branch from `origin/main` only
- [x] Files match `git show b8e174b8:<path>` blobs
- [x] `.gitignore` does not block `*.wav` (no allowlist needed)
- [x] Diff is four files only
- [ ] CI green / merge CLEAN

## Notes

Smoke: USS stubs only (ASSET-036/037). No `dotnet test` required for this surface.
