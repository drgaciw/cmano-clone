# DRG-196 proof vs origin/main

**Branch:** `feat/DRG-196-agent-c2-skill-contract`  
**Worktree:** `.worktrees/drg-196-agc-skill-contract`  
**Not merged.**

Re-run after checkout:

```
pwsh -NoProfile -File production/docs/skills/agent-c2-skill-contract/verify-contract.ps1
```

Expected: `VERDICT=PASS`, merge-base matches `origin/main`, behind `0`.

## Isolation

Committed paths vs `origin/main` are limited to:

- `production/docs/skills/` (contract, catalog, four Slice A SKILL.md files, envelopes)
- `.claude/skills/agent-c2-skill-contract/` (implementer discovery)
- `src/ProjectAegis.Delegation/Skills/` (headless contract types)
- `src/ProjectAegis.Delegation.Tests/Skills/` (validator and catalog tests)

No `DelegationBridge.cs`, no `CatalogWriteGate`, no `SimulationSession`, no `BalticReplayHarness`, no KillChain / DRG-179 projection types, no gauntlet skills, no t2 policy.

## Contract files

| Path | Role |
| --- | --- |
| `production/docs/skills/agent-c2-skill-contract/CONTRACT.md` | Lanes, authority, override, provenance |
| `production/docs/skills/agent-c2-skill-contract/catalog.json` | Discoverable skill list (AGC-01) |
| `production/docs/skills/agent-c2-skill-contract/envelopes/skill-envelope.schema.json` | Envelope (AGC-03, AGC-04) |
| `production/docs/skills/agent-c2-skill-contract/envelopes/examples/read-track.json` | Lane `read` |
| `production/docs/skills/agent-c2-skill-contract/envelopes/examples/propose-pairing.json` | Lane `propose` |
| `production/docs/skills/agent-c2-skill-contract/envelopes/examples/submit-engage.json` | Lane `submit` |
| `production/docs/skills/agent-c2-skill-contract/envelopes/examples/fail-shared-track-engage.json` | ADR-018 reject fixture |
| `production/docs/skills/c2-track-assessment/SKILL.md` | `c2.track.assess` |
| `production/docs/skills/c2-datalink-reasoning/SKILL.md` | `c2.datalink.reason` |
| `production/docs/skills/c2-sensor-to-shooter-pairing/SKILL.md` | `c2.pairing.recommend` |
| `production/docs/skills/c2-explanation/SKILL.md` | `c2.explain` |
| `.claude/skills/agent-c2-skill-contract/SKILL.md` | Implementer discovery |

## Headless C# types (AGC-01..04)

| Path | Role |
| --- | --- |
| `src/ProjectAegis.Delegation/Skills/SkillIds.cs` | Stable skill ids and enums |
| `src/ProjectAegis.Delegation/Skills/SkillCatalog.cs` | Slice A discovery + command allowlists |
| `src/ProjectAegis.Delegation/Skills/SkillEnvelope.cs` | Envelope DTOs |
| `src/ProjectAegis.Delegation/Skills/SkillEnvelopeValidator.cs` | Pure gate (no enqueue) |
| `src/ProjectAegis.Delegation.Tests/Skills/SkillCatalogTests.cs` | Catalog parity |
| `src/ProjectAegis.Delegation.Tests/Skills/SkillEnvelopeValidatorTests.cs` | Lane and engage guard tests |

Filter: `FullyQualifiedName~ProjectAegis.Delegation.Tests.Skills`

## Reads vs proposals vs submit

| Lane | Owner | Effect |
| --- | --- | --- |
| `read` | four Slice A skills | Projection / snapshot fold. No order-log append. |
| `propose` | track, datalink, pairing | Session-local staging. `ttlTicks`. Authority, override, provenance. `engagementAuthorizationImplied` false. Dismiss leaves no mutation. |
| `submit` | host verb `c2.skill.submit` | After `approved`, `C2CommandIssuance` then `C2PlayerCommandBridge.TryIssue`. `IPolicyEvaluator` still gates. Shared tracks cannot `engage`. |

## Skill-test static (`agent-c2-skill-contract`)

| Check | Result |
| --- | --- |
| 1 Frontmatter | PASS (`name`, `description`, `argument-hint`, `user-invocable`, `allowed-tools`) |
| 2 Phases | PASS (Phase 1, 2, 3) |
| 3 Verdicts | PASS (`PASS`, `FAIL`, `BLOCKED`) |
| 4 Collaborative protocol | PASS (`May I write` named; tools are read-only) |
| 5 Handoff | PASS |
| 6 Fork context | PASS (not set) |
| 7 Argument hint | PASS (`[read\|propose\|submit\|review]`) |

Aggregate: COMPLIANT.
