# Sprint 7 Definition of Done Rubric (`sprint/s7-c2-interrupt-deeplink`)

**Authorization:** Owner delegated decision to Typesafe-AI Jev (`use jev to make these decisions and proceed`):
- `gen-dec-1795465081-eG3fo2QUC5Aty4EJpDGD`: `submit_draft_stack_now_then_sprint7` (`0.91`), `keep_h6_honestly_pending_on_pr` (`1.0`). Draft PRs #907–#913 created cleanly on Graphite.
- `gen-dec-1795465106-JCrlUfufH4ONrxCV7Y1M`: `drg269_and_drg268_c2_pair` (`0.81`) — releases `DRG-269` (High) + `DRG-268` (Medium) for Sprint 7 while keeping `DRG-236`, `DRG-231`, and `DRG-347` held.

**Scope:**
- **`DRG-269` (`[N-IRQ-01] Pending-approval interrupt: force 1× and DLZ countdown`, High):**
  - One approval event issues at most one `1x` drop request through the shared `DRG-408`/`C2ClockCommand` path (never spams across ticks; suppressed in replay/observer mode).
  - DLZ countdown driven strictly by authoritative sim-time DLZ window data (halts while paused; displays `DLZ WINDOW: UNKNOWN` when authoritative data is absent — never inferred from animation/wall clock).
  - Confirm/cancel executes through existing authority command boundary, preserves ordering, revalidates stale/expired approvals, disables replay/observer commands with explicit reason, and is verified under `DRG-348`/`DRG-349` H6 save/resume replay parity.
- **`DRG-268` (`[C2-13] Message log click → unit + explain deep-link`, Medium):**
  - Clicking a message-log row selects the referenced unit (fog-safe: unobserved hostiles are never disclosed or selected) and opens/binds the existing `EngageExplainPanelHost` explain surface for the referenced `SequenceId` without introducing a new explain DTO.

## Hard Gates (`H1`–`H8`)
- `H1`/`C1`: `dotnet build` (`0` errors, `0` warnings) & `dotnet test` (`>= 5,083` passed, `0` failed, `0` skipped); `netstandard2.1` synced (`15` DLLs).
- `H2`: Linear evidence comments on `DRG-269` and `DRG-268` only (kept `Backlog` until PR merge); `DRG-236`, `DRG-231`, `DRG-347` untouched.
- `H3`/`C7`: `0` diff vs `6e406bec` on `DelegationBridge.cs`, `SimulationSession.cs`, `CatalogWriteGate`, `MapPlaceholderPanelHost.cs`, `GlobeMapProductHost.cs`, `tests/regression/`, `DelegationSmoke.unity`, `assets/data/catalog/*.db`.
- `H4`/`C2`: `ReplayGolden` `17/17`, hash `17144800277401907079` preserved.
- `H5`/`C8`: Verbatim `RED -> GREEN` evidence in `production/qa/evidence/sprint-7/s7-c8-red-evidence.md`.
- `H8`/`C4`: `PlayModeSmokeHarnessTests >= 27/27`, live Unity `:8081` PlayMode `> 66` passed (`0` Console errors, clean Edit Mode).

## Round Log
- **Round 0 (Kickoff)**: `gen-dec-1795465081-eG3fo2QUC5Aty4EJpDGD` + `gen-dec-1795465106-JCrlUfufH4ONrxCV7Y1M` (`drg269_and_drg268_c2_pair` = `0.81`).
- **Round 1 (DoD Verification)**: `gen-dec-1795466126-020HOZN7sSz95yITj2Yh` — **`sprint7_verdict = pass_submit_draft_pr` (`P = 0.99`, confidence `0.98`)**, **`more_rounds = 0` (`score = 0.19`, `P(0) = 0.84`)**, `h1 = 0.94`, `h2 = 0.94`, `h3 = 0.80`, `h4 = 0.93`, `h8 = 0.83`.
- **Linear Comments (`H2`: Kept `Backlog`)**: `DRG-269` (`ec3e3a6f-6409-4cac-9fa7-c2efbb435a84`), `DRG-268` (`e1610364-1530-499c-8dff-cfc25791813d`). Held untouched: `DRG-236`, `DRG-231`, `DRG-347`.
- **Implementation Commit**: `3ccf20c0` (`5,088/5,088` headless, `17/17` ReplayGolden, `27/27` PlayModeSmokeHarnessTests, `70/70` live Unity `:8081` PlayMode with `0` Console errors, `0` hard-no diffs).
