# Fly.io Sprite trial results

Status: DRG-704. Measured on Sprite `my-first-sprite` (org `drg-amtd`),
2026-11-01. Times are CT. Sprite: x86_64, 8 vCPU, 15 GiB RAM.

These are author-reported trial observations. Raw per-job logs were not
committed with this report, so the timings, resource measurements, and test
counts below have not been independently reproduced from archived logs.
The linked PRs provide the associated changes and their separate CI evidence.

| Run | Jobs | PRs | Wall time | Notes |
| --- | --- | --- | --- | --- |
| Trial 1 | 1 docs (DRG-693) | [#783](https://github.com/drgaciw/cmano-clone/pull/783) | ~8 min incl. setup | .NET 10 SDK + ReSharper CLI install; `dotnet` needed `libicu-dev`. Full solution 4,053/4,054 (missing plugin DLL). |
| Trial 2 | 1 code (DRG-694 strict JSON) | [#784](https://github.com/drgaciw/cmano-clone/pull/784) | ~6 min | DLL copy fixed the test; full solution 4,059/4,059 in 72 s; inspect ~3 min. |
| Trial 3 | 1 code (DRG-695 async ZIP) + 3 docs (DRG-696/697/698), parallel worktrees | [#788](https://github.com/drgaciw/cmano-clone/pull/788), [#785](https://github.com/drgaciw/cmano-clone/pull/785), [#786](https://github.com/drgaciw/cmano-clone/pull/786), [#787](https://github.com/drgaciw/cmano-clone/pull/787) | 4 min 16 s | Docs jobs ~4 s each. Code job: DLL copy 5 s, tests 79 s, inspect 2 min 52 s. Peak load 7.89, min free RAM ~4.1 GB. |
| Batch 4 | 3 docs (DRG-702/703/704), parallel | [#791](https://github.com/drgaciw/cmano-clone/pull/791), [#789](https://github.com/drgaciw/cmano-clone/pull/789), [#790](https://github.com/drgaciw/cmano-clone/pull/790) | seconds | WIP cap at 4/4 product PRs, so no code jobs. |

## Trial interpretation

- `jb inspectcode` (solution load) dominates code-job time; tests take ~75 s.
- One code job nearly saturates 8 cores. Run at most 2 code jobs at once.
- Docs jobs are effectively free; the WIP cap, not the Sprite, limits throughput.
- Installed tools persist across sleep/wake; running processes do not, so
  launch jobs and `wait` for them inside one `sprite exec` session.
- Base-image gaps: `libicu`, `ripgrep`, `pwsh`, GitNexus.
- Cost: reported acceptable by the owner after trial 2. The Sprite CLI shows
  status only, not usage; check the Fly dashboard.
