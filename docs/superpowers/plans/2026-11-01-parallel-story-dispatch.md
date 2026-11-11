# Next-story parallel dispatch — 2026-11-01

Base: `b5853f6d66d6ed73f43bd691105f68f6aa57fcc4`. User requested local agents, subagents, and Fly.io Sprites. Existing uncommitted gameplay work is excluded from this wave. No commit, push, merge, PR submission, or product acceptance is authorized by this dispatch.

## Sources and routing

- [Command experience plan](https://app.notion.com/p/409f7cb4e4df815bbccfe465326584cc): delivery order and acceptance constraints.
- [REQ-07 inventory](https://app.notion.com/p/3a7f7cb4e4df814bb381ea1b51965f81), [infrastructure spec inventory](https://app.notion.com/p/3a7f7cb4e4df81b6b4bde649724419e4): frozen Git inventories; their statuses do not establish delivery.
- [Dispatch playbook](../../../production/agentic/linear-parallel-dispatch-playbook.md) and [local/cloud matrix](../../../production/agentic/local-cloud-agent-routing.md): isolated worktrees, disjoint surfaces, cloud cap five, effective tracks four–six. This session permits three child agents plus the coordinator.

## Wave 0 — prerequisite and readiness

- [x] Inspect Linear, Notion, existing worktrees and dirty files.
- [x] Check Sprites tooling/authentication; install official Windows CLI outside the repository.
- [x] Assign isolated baseline agent. Exact SDK 8.0.400 installed in temporary tooling directory.
- [x] Read baseline build/full-suite/replay/smoke results before implementation starts: SDK 8.0.400, build 0 warnings/errors, 4058 tests/0 failures, smoke 26/26, canonical replay 6/6, hash preserved. A missing ignored plugin DLL in the fresh worktree was repaired with the documented plugin bootstrap; the full suite then passed.

## Wave 1 — disjoint implementation contracts

| Story | Owner | Worktree | Allowed new surface | Scope |
|---|---|---|---|---|
| [DRG-410](https://linear.app/drgamtd-workspace/issue/DRG-410) | `drg410_allocation` | `.worktrees/orch-drg-410-20261101` | `Delegation/Projection/AllocationPlan*.cs`, dedicated `Delegation.Tests/Projection/AllocationPlanProjectionTests.cs` | Headless allocation preview; CMANO stamp overrides older command-commit AC. UI and command submission excluded. |
| [DRG-254](https://linear.app/drgamtd-workspace/issue/DRG-254) | `drg254_aar` | `.worktrees/orch-drg-254-20261101` | `Delegation/AgentPerformance/`, corresponding new test folder | Immutable per-agent intents, denials, explicitly attributed override metrics over `IReadOnlyOrderLog`. |
| [DRG-257](https://linear.app/drgamtd-workspace/issue/DRG-257) | `drg257_experiments` | `.worktrees/orch-drg-257-20261101` | `UnityAdapter/Experiments/`, corresponding new tests, `data/experiments/`, standalone `tools/experiment-runner/`, new usage document | Validated JSON definition, existing batch-runner wrapper, stable CSV/fingerprints, standalone CLI. Opaque policy IDs remain metadata. |

Workers use Linear-generated branches. Exact story briefs and result logs live in `%TEMP%/aegis-orchestration-20261101/`; baseline evidence lives in `%TEMP%/aegis-orchestration-baseline-20261101/`. Workers return uncommitted changes in their worktrees for independent review; they do not edit the dirty root.

All three implementation workers received GO after baseline PASS. Linear states were changed to In Progress and independently read back; human assignees were preserved. Integration review uses `.worktrees/orch-review-20261101` at the same pinned base. All three tracks returned reviewable patches, passed independent review, and passed final whole-wave review. Combined gates: build 0 warnings/errors, 4119 solution tests, 26 proxy smoke, six canonical replay and nine additional CLI process tests; zero failures/skips. Source changes remain uncommitted in the worker and combined review worktrees.

GitNexus reports the root index 138 commits behind. Upstream read-anchor checks: `DecisionLog` CRITICAL (327 direct, 498 total at depth two, eight grouped affected flows); `BalticBatchRunner` LOW (one direct, 15 total); `EngageAttackOptions` LOW with zero reported dependencies. Old-index zero counts do not establish safety. Existing symbols stay read-only; existing-symbol edits require current impact evidence and a new scoped decision. No protected hubs, solution/project files, existing binders, policies, goldens, or Unity assets belong to this wave.

## Held work and next waves

- DRG-412 already has a worktree; its Surface stamp requires it to land before DRG-408. The missing DRG-408 blocked-by relation and contradictory dependency sentence were repaired and read back in Linear. Hold DRG-408 → DRG-398/269 until landing/ownership is verified; one shared alert coordinator only.
- DRG-417 already has a worktree; do not duplicate it.
- DRG-247 intersects dirty authority hubs and needs supported suspension/task-retention seams established first.
- DRG-712 has no Surface stamp and needs local Editor/MCP evidence before implementation assignment.
- DRG-348 is owned and In Progress; an independent inventory review may support it, but DRG-349 waits for the contract gate.
- Reserve authenticated Sprites lanes for independent contract/test reviews or new ready headless stories. Never run a duplicate implementation of a story already assigned locally.

## Sprites status

CLI verified at `%LOCALAPPDATA%/AegisOrchestration/sprites/cli/sprite.exe`. Follow-through: authentication succeeded for organization `drg-amtd`. Created dedicated Sprite `aegis-linux-gates-20261101`; a second creation was rejected by the trial limit of three total Sprites (two pre-existing environments were preserved). Two local agent controllers drove separate remote checkouts: full Linux gates in `/home/sprite/aegis` and CLI/focused verification in `/home/sprite/aegis-cli`. They shared SDK 8.0.400 read-only and needed no remote model credentials. Both lanes passed: Linux full 4119/0 failures/skips, build 0 warnings/errors, smoke 26/26, canonical replay 6/6; separate CLI 9/9 and focused feature tests 61/61, with actual sample/fingerprint/metadata parity verified. No billing change or existing-Sprite deletion was performed. Remote logs and reports are recorded in the handoff evidence.

Official [installation](https://docs.fly.io/sprites/cli/installation) and [authentication](https://docs.fly.io/sprites/cli/authentication) instructions were checked. Detailed inventory and resume commands: `%TEMP%/aegis-orchestration-20261101/sprites-preflight.md`.

## Review and verification

- [x] Before product writes: baseline build zero warnings/errors, full tests 4058/zero failures, canonical replay six, proxy smoke twenty-six, golden hash `17144800277401907079` preserved.
- [x] Each track: meaningful RED/GREEN tests; no source log mutation or protected-hub edits; exact SDK; full required checks read before completion claims.
- [x] Independent spec and quality review of each returned diff; allocation validation-test masking fixed and independently re-reviewed. Final whole-wave review: spec PASS, quality APPROVED, architecture PASS.
- [x] Integration in a clean isolated review worktree only; required gates passed over the combined result.
- [x] Unity/C# applicable work: `production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md` PASS verdict, ADR-010 §§2–3 / ADR-007 / ADR-001; plugin publish implications documented. No live Editor proof claimed from proxy tests.
- [x] Coordinator records actual artifacts and remaining gates in [verification and handoff](../../../production/qa/parallel-story-wave-2026-11-01.md). Linear Done requires merged work; no completion transitions during this dispatch. Sprites authentication and both remote verification lanes completed successfully within trial capacity.
