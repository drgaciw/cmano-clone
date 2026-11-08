# UAT remediation: startup, selection, and command feedback

**Goal:** Repair the three failures observed in the DelegationSmoke / baltic-patrol-classify Computer Use session and document a live retest.

**Architecture:** Keep Unity hosts as presentation clients of the existing projection and command facade (ADR-010 §§2–3, ADR-007, ADR-001). Lifecycle readiness belongs in the consumer; list selection is presentation state; command feedback must describe the actual facade result.

**Scope authorization:** User requested implementation, agent delegation, Unity-MCP, Computer Use, and a documented retest. No commit/push requested.

## Constraints and baseline

- Unity 6000.3.22f1; .NET SDK 8.0.400. Base commit: 46485600.
- Preserve DelegationBridge.cs, CatalogWriteGate write paths, Baltic v2 hash 17144800277401907079, and v3 isolation.
- Preserve all pre-existing working changes. In particular, the live project already has Unity-MCP 0.90.0 versus tracked 0.86.0, generated skills, and import metadata changes. These are not remediation payload.
- Main workspace owns the live Editor. Agents work in independent detached worktrees under .worktrees/uat-{startup,selection,command}; no commits and no shared Editor input.
- Build baseline: zero warnings/errors. Solution baseline: 3,095 passed, zero failed/skipped.
- GitNexus index refresh precedes upstream impact and symbol edits. Hindsight unavailable at localhost:8888; no memory recall claimed.

## Work packages

- [x] Startup agent: reproduce the top-bar OnEnable → Refresh → LiveCompressionLabel → Session failure; guard presentation refresh until the bridge is initialized. Scope C2TopBarPanelHost.cs and focused tests. Preserve functional clock controls once initialized.
- [x] Selection agent: test the rebuilding/virtualized OOB row event lifecycle; route ListView selection through the existing SelectUnit presentation facade. Scope C2LeftDrawerPanelHost.cs and focused tests. Verify both unit rows and switching back from contact selection.
- [x] Command agent: trace Fire/Hold input through the existing facade; show truthful queued/accepted or denied feedback and reason. Scope RightUnitPanelHost.cs and focused tests; extend only if evidence requires it.
- [x] Each agent: upstream impact, failing regression, minimal fix, passing targeted tests, self-review, uncommitted patch.
- [x] Coordinator: inspect and integrate only scoped patches; independent agent review for spec compliance and code quality.
- [x] Coordinator: full build, solution tests, PlayModeSmokeHarness, ReplayGolden, hash and zero-touch checks.
- [x] Coordinator: refresh Unity through MCP, run applicable Unity tests, then retest player input through Computer Use.
- [x] Coordinator: save dated QA report with exact evidence, limitations, verdict, and pre-existing changes separated from remediation.

## Live acceptance criteria

| ID | Steps | Required result |
|---|---|---|
| UAT-START | Enter and re-enter DelegationSmoke Play Mode; inspect new Console exceptions; exercise pause/resume and 1x/2x | No startup session null reference; map/top bar usable; clock responds |
| UAT-SELECT | Pause; select each unit from OOB; select contact on map then friendly via OOB; select unit on map | OOB highlight, map ring, and detail panel agree after each selection |
| UAT-COMMAND | Issue a valid available command; issue an unavailable/invalid command or choose an unavailable target | Visible feedback reflects actual queued/accepted/denied result, with a reason for denial; no silent no-op or invented success |

The original session's tiny text, clipping, and empty mission list are outside this focused repair unless they prevent verification of these criteria.

Completion evidence: [dated UAT report](../../../production/qa/uat-remediation-2026-09-05.md). All three criteria passed with the documented automated-denial qualification. Debug and Release suites each passed 3,096 tests; four focused Unity tests passed. No commit made.
