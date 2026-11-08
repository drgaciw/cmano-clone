# Slice B — Engage and assess implementation plan

User signoff in this task authorizes DRG-165–170 after DRG-208. Base: `0cd321c1`.

Architecture: simulation-authored engagement metadata → replayable combat events → immutable adapter frame → map and detail presenters → thin Unity views. ADR-010 §2–3, ADR-007, ADR-001 apply. No changes to DelegationBridge.cs, CatalogWriteGate, Baltic v2 fingerprints or v3 policies. No commits or publication requested.

## Work and acceptance

- [x] DRG-165: stamp target and known weapon family at the authoritative engagement producer; project intent, authorization/refusal, fire and outcome using stable log correlation. Missing evidence remains unknown. Legacy fingerprints remain unchanged.
- [x] DRG-167: consume combat events in a bounded map presenter with missile/gun/laser glyphs, line patterns and readable labels; operational aggregation and theater declutter retain inspected engagements.
- [x] DRG-168: selected-event explanation uses the same event identity, constraints, policy reason, known firing solution and corrective advice. No selected-contact substitution for another target.
- [x] DRG-169: contact panel retains Slice A provenance/quality/targetability and adds correlated engagement and contact-specific BDA. Unknown assessment and posture remain explicit.
- [x] DRG-166/170: deterministic three-family scenario/harness, permitted and refused paths, map/log/replay identity, three zoom bands, non-color recognition and no future-event leakage.
- [x] Integrate frame at the Unity composition root, use existing map/contact surfaces, provide event inspection and zoom controls. Do not edit scene/prefab/meta YAML.
- [x] Review integrated diff and run build, full suite, ReplayGolden, PlayModeSmokeHarness, local CI parity and UCA finish checklist. Record actual results and any unavailable Editor evidence.

Implementation lanes use isolated worktrees for core contract and map presentation; coordinator owns frame/detail/Unity integration and final verification. NUnit is the actual repository test framework. TDD precedes new production logic. Existing-symbol changes require GitNexus upstream impact; LogEngagementResults is HIGH risk and was reported before edits. GitNexus refresh encountered a local database WAL assertion; existing graph impact results are available but index freshness is limited.

## Validation record

Previous baseline: build 0 warnings/errors, full suite 3,096 passing, smoke 24 passing. Current baseline contains merged C2 remediation #620. Editor MCP :8080 was unavailable at start; user signoff is recorded above, not substituted for new visual evidence.

Implementation and headless verification are complete. Last-mile Editor visual acceptance remains open because Unity-MCP :8080 is unavailable. See [verification report](../../../production/qa/slice-b-drg-165-170-implementation-2026-09-05.md) for the UCA BLOCKED verdict and outstanding visual checks.

