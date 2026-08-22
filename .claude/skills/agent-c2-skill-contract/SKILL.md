---
name: agent-c2-skill-contract
description: Use when defining, implementing, or reviewing agent-callable C2 skills (AGC-01 through AGC-04), track assessment, data-link reasoning, sensor-to-shooter pairing, C2 explanation, skill envelopes, or the split between projection reads, bounded proposals, and approved command submission.
argument-hint: "[read|propose|submit|review]"
user-invocable: true
allowed-tools: Read, Grep, Glob
---

# Agent-callable C2 skill contract (implementer)

**Product contract (source of truth):** `production/docs/skills/agent-c2-skill-contract/CONTRACT.md`

This skill is for implementers and reviewers. The four in-sim skills live under `production/docs/skills/c2-*/SKILL.md`. Do not copy their field lists here.

## Phase 1: Load the contract

1. Read `production/docs/skills/agent-c2-skill-contract/CONTRACT.md`.
2. Read `production/docs/skills/agent-c2-skill-contract/catalog.json`.
3. Open `production/docs/skills/agent-c2-skill-contract/envelopes/skill-envelope.schema.json` when checking a payload.

If the task is a runtime C# story, stop and treat those files as the API. Do not invent a fifth Slice A skill.

## Phase 2: Classify the change

| If the change | Then |
| --- | --- |
| Adds a named skill | New catalog row + SKILL.md under `production/docs/skills/` |
| Reads tracks / links / pairs / abort text | Lane `read` only. Existing `*Projection` types. |
| Recommends a command | Lane `propose`. Envelope must include authority, approval, override, provenance, `ttlTicks`. |
| Enqueues an order | Host verb `c2.skill.submit` after approval. `C2CommandIssuance` then `C2PlayerCommandBridge.TryIssue`. |
| Touches `DelegationBridge.cs`, `CatalogWriteGate`, `SimulationSession`, gauntlet skills, t2 policy, `BalticReplayHarness`, `MissionContactTargetClass`, or new DRG-179 projection types | **BLOCKED**. Wrong ticket. |

## Phase 3: Review a payload or PR

Fail the change when any of these hold:

- `read` appends to the order log
- `propose` sets `engagementAuthorizationImplied: true`
- `engage` proposed on `datalinkShared` / `fusedWithoutOrganicFc`
- `submit` without `proposalId` in state `approved`
- new command id that `C2CommandIssuance.TryResolve` does not know
- Unity host edit presented as "just glue" for this contract

Verdict: **PASS** / **FAIL** / **BLOCKED**.

This skill does not write product code. If a later story needs files, ask "May I write" in that story's implementer skill (`/c-sharp-engineer`), not here.

## Next step

- New skill text: edit the production/docs/skills files on `feat/DRG-196-agent-c2-skill-contract`.
- Runtime: `/c-sharp-architect` then `/c-sharp-engineer`, headless tests first (ADR-010).
- Static check of this file: `/skill-test static agent-c2-skill-contract`.
