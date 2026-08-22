# DRG-196 proof vs origin/main

**Branch:** `feat/DRG-196-agent-c2-skill-contract`  
**Worktree:** `.worktrees/drg-196-agc-skill-contract`  
**Not merged.**

Re-run after checkout:

```
powershell -NoProfile -File production/docs/skills/agent-c2-skill-contract/verify-contract.ps1
```

Expected: `VERDICT=PASS`, merge-base `84684958`, behind `0`.

## Isolation

| Ref | SHA |
| --- | --- |
| `origin/main` | `84684958ea6d0328d4abcfff547f1b48a3c9c22c` |
| Merge-base | `84684958ea6d0328d4abcfff547f1b48a3c9c22c` |

Committed paths vs `origin/main` are under `production/docs/skills/` and `.claude/skills/agent-c2-skill-contract/` only. No `src/`, no Unity host, no `DelegationBridge.cs`, no `CatalogWriteGate`, no `SimulationSession`, no `BalticReplayHarness`, no classifier, no DRG-179 projection types, no gauntlet skills, no t2 policy.

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
