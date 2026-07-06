# C2 manual QA sign-off — addendum 2026-07-06 (gameplay-qa-agent 10-loop TDD batch)

**Sprint / task:** Ad hoc gameplay-QA batch — 10 parallel exploratory-QA + TDD loops (`qa-loop-01` .. `qa-loop-10`), orchestrated across isolated worktrees, merged in two waves.
**Build:** `main` @ `d6893b9` (all 10 loops merged; full suite green post-merge)
**Parent checklist:** `production/qa/c2-manual-signoff-2026-06-02.md` (checks 1–18) — this addendum adds checks 19–20 only; it does not re-run or supersede 1–18.
**Scope of this addendum:** two of the ten fixes touch player-facing control/presentation flow in a way that is easy to get subtly wrong in ways only visible in an actual PlayMode session — `qa-loop-03` (direct-control handoff) and `qa-loop-08` (C2 dependency-graph presentation desync). The other eight are pure simulation/data/CLI logic fixes with no PlayMode-observable surface and are not covered here.

**Lean mode:** no Unity Editor host available in this session (Linux sandbox; the project's own `tools/unity/Invoke-C2PlayModeSignoffBatch.ps1` requires a Windows `Unity.exe` path). Per ADR-010 / PI-006, headless proxy stands in for merge authority; the Editor walk below is logged PENDING, not run.

**Headless proxy (this session):** `dotnet test --filter "FullyQualifiedName~OrchestratorOverride|FullyQualifiedName~C2PresentationController|FullyQualifiedName~DelegationBridge"` → **24/24 PASS** (3 `ProjectAegis.Delegation.Tests`, 21 `ProjectAegis.Delegation.UnityAdapter.Tests`).

**Reference:** `unity/ProjectAegis/PLAYMODE-SMOKE.md`; scene `DelegationSmoke.unity`.

| # | Check | Pass | Tester | Notes |
|---|-------|------|--------|-------|
| 19 | Select unit A, click **Take Direct Control**, issue an order, click **Take Direct Control** again on the same (already-controlled) unit → the previously queued order is still present, no silent `HumanController` replacement | ☐ PENDING | — | Repro for `production/qa/bugs/BUG-double-take-control-drops-queued-orders.md`; proxy: `OrchestratorOverrideTests.TryTakeDirectControl_calledAgainOnAlreadyDetachedUnit_preservesQueuedPlayerOrders` (green) |
| 20 | Select a friendly unit so its dependency graph highlights/link-chain are shown, then click a hostile contact → graph highlights and link-chain clear immediately; nothing from the previously selected unit lingers in the C2 panel | ☐ PENDING | — | Repro for `production/qa/bugs/BUG-c2-graph-highlight-stale-selection.md`; proxy: `C2PresentationControllerTests.SelectHostileContact_clears_stale_graph_highlights_from_previous_unit` (green) |

**Verdict:** ☐ **PENDING HUMAN WALK** (0/2) — headless proxy PASS (24/24) is sufficient for lean-mode merge authority per ADR-010/PI-006, consistent with checks 1–18 in the parent checklist. Checks 19–20 are advisory, non-blocking follow-up: run `Invoke-C2PlayModeSignoffBatch.ps1` (or open `DelegationSmoke.unity` directly) on a local Windows/macOS Unity 6000.3.14f1 host, walk the two steps above, and update this table.

**Blockers:** None. Both fixes are already merged to `main` and covered by the full automated suite (see `production/qa/bugs/BUG-double-take-control-drops-queued-orders.md` and `BUG-c2-graph-highlight-stale-selection.md` for full TDD evidence: red→green test, impact analysis, before/after counts).
