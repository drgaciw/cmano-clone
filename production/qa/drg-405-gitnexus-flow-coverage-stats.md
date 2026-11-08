# DRG-405: GitNexus process-flow coverage, before and after (rescoped)

**Scope (Jev pick C, 2026-11-01):** record the before/after coverage stats only. You cannot tune the flow limits from this repo. In gitnexus 1.6.12, `dist/core/ingestion/process-processor.js` hardcodes them in `DEFAULT_CONFIG`: `maxTraceDepth` 10, `maxBranching` 4, `minSteps` 3, and a per-entry trace budget of `maxBranching * 3`. The analyze phase overrides only `maxProcesses` (symbols / 10). No CLI flag, env var, or repo config reaches these limits or the entry-point ranking.

## Coverage stats

| Metric | Before (DRG-197, PR #681) | After (this run) |
|---|---|---|
| Flows reported | 817 | 828 |
| Candidate entry points | 2501 | 2700 |
| Candidates never ranked in | 2301 | 2500 |
| Traces cut at maxTraceDepth | 12 | 14 |
| Callees skipped at maxBranching | 1809 | 1985 |
| Walks cut by per-entry budget | 45 | 46 |
| Flows dropped at maxProcesses | n/a | 0 |
| Nodes / edges | n/a | 43,337 / 92,045 (1077 clusters) |

"After" run details:
- Commit: `main` at `ebfa2ec8`.
- Command: gitnexus 1.6.12, `GITNEXUS_SHARED_STORE=off GITNEXUS_STORAGE_PATH=<isolated> gitnexus analyze --force --skip-agents-md --skip-skills`.
- Duration: 127 s.
- Host: Fly.io Sprite my-first-sprite.
- Nothing generated was committed.

Ranked entry points stay at about 200, so coverage has not changed. The numbers moved only because the codebase grew.

## Sample impact checks (upstream)

| Symbol | Risk | processes_affected | Known runtime caller |
|---|---|---|---|
| `KillChainContactStateProjection` | LOW (10 impacted) | 0 | `SliceAContactFrame.Build` -> `DelegationBridgeHost.RunTick` |
| `RunTick` | LOW (1 impacted) | 0 | Unity host tick |

`processes_affected: 0` is still a false negative for projections that have runtime callers. Until upstream exposes the limits, blast-radius checks must cross-check with `rg` and must not treat 0 processes as "no runtime flow".

## Follow-up

An upstream GitNexus request to expose these limits is drafted on DRG-405. It has not been posted and needs owner approval.
