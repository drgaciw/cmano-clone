# Slice C implementation plan

**Goal:** Complete DRG-171–175, DRG-191, DRG-192 and DRG-194 against the existing merged headless foundations.

**Architecture:** Immutable read models compose the current authoritative log and explicitly supplied evidence. Pure presenters provide a unified command-review surface. Player decisions alone submit bounded intents through existing command facades; advisory skills never issue fire. ADR-010 §2–3, ADR-007 and ADR-001 apply.

**Tech stack:** .NET 8 / netstandard2.1, NUnit, Unity 6000.3.22f1 UI Toolkit. User authorization: current request, following prior DRG-208 signoff. No commits or board mutations requested.

## Sources and scope

- [Linear Slice C epic](https://linear.app/drgamtd-workspace/issue/DRG-185): review → recommendation → human decision → delegated group response → assessed retasking.
- [Notion requirements](https://app.notion.com/p/3c1f7cb4e4df810c96b0ea6e32cc6933), plus its Agentic, ASBS and IBCS child requirements. Linear remains delivery status authority.
- Existing foundations: AfterAction, ThreatAssessment, ResourceRank, EmconPosture, PlatformDegrade, TaskGroupCoord, MissionIntent and Skills.

## Tracks and verification

- [x] Timeline: add `Presentation/CommandReviewTimeline.cs` and tests. Map existing CombatEvent rows into AfterAction ledger, conjunctively filter platform/target/family/outcome, inspect exact event time/key, and return to current state. Test interleaved same-correlation events, empty results, replay cutoff and no command-selection mutation.
- [x] Recommendations/skills: new adapter `CommandReview/` files and tests compose existing threat/rank projections, show evidence/assumptions/confidence/alternatives, expose discoverable capability-scoped advisory functions with unavailable/stale/authority-withheld outcomes. No fabricated contexts or automatic order submission.
- [x] Coordination: new adapter `CommandReview/` files and tests compose group membership, mission intent, known role/coverage facts and gaps; submit explicit player group intents through existing facades. Test unknown authority, split group, lost member, incompatible commitments, withdrawal and replay refusal.
- [x] Sensors/damage: new adapter `CommandReview/` files and tests expose existing provenance, emissions, EW and platform degradation with per-component UNKNOWN when unmodeled. Include clutter toggles, text semantics and timeline references.
- [x] Integration: extend the existing Unity composition root after GitNexus impact; add a thin pooled `CommandReviewView` to the map surface. Runtime reads pass through an adapter bridge; all decisions remain command-driven. No scene/prefab/meta edits.
- [x] Acceptance: deterministic normal, stale/partition, withheld authority, resource-conflict, unavailable-model and damage/retask cases. Review specs then code quality. Run solution build/tests, ReplayGolden, PlayModeSmokeHarness, C2/combat bind budgets and CI parity; compile Unity runtime/tests offline if Editor remains unavailable.

For each track, write behavioral tests before implementation and run the narrow filter RED then GREEN. Inspect GitNexus impact before changing existing symbols; new files use source-reviewed consumer seams. Worktree isolation separates parallel tracks. Final coordinator integration preserves `DelegationBridge.cs`, CatalogWriteGate and all golden fixtures.

## QA baseline and gate

Baseline b92b0b3d: prior 3,149 passing tests, 24 smoke, 6 replay goldens. Fresh baseline passed 3,149/3,149 before implementation. New tests must grow that count without regressions. Requirement-specific tests must prove consumer wiring, not only disconnected DTO shape. Unity MCP :8080 connection refused; offline compilation cannot substitute for new visual evidence. GitNexus reanalysis encounters Ladybug WAL errors; retain limitation in final report.

## Completion evidence

See [Slice C implementation and verification](../reviews/2026-09-08-slice-c-implementation.md). All eight implementation stories and headless acceptance paths are integrated in `37e6ba6d`. The [Play Mode acceptance package](../reviews/2026-09-08-slice-c-playmode-acceptance.md) now includes actual Editor smoke, six passing Unity tests and screenshots. Technical smoke passed; owner visual signoff remains pending. Positive group/BDA retasking is headless-tested; the Unity smoke scene has no configured group, as documented in that package.
