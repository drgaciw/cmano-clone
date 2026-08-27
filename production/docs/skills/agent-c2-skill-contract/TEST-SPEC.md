# Skill test spec: agent-callable C2 contract

## Skill summary

Contract plus four discoverable C2 skills for Slice A (DRG-196 / AGC-01..AGC-04). Primary artifacts: `CONTRACT.md`, `catalog.json`, `envelopes/skill-envelope.schema.json`, and `production/docs/skills/c2-*/SKILL.md`. Verdicts: PASS / FAIL / BLOCKED.

---

## Static assertions (structural)

- [x] Catalog lists exactly four Slice A skills: `c2.track.assess`, `c2.datalink.reason`, `c2.pairing.recommend`, `c2.explain`
- [x] No Slice A skill lists lane `submit`
- [x] Submit is documented as host verb `c2.skill.submit`
- [x] Envelope schema requires `authorityBasis`, `playerOverride`, `replayProvenance` definitions
- [x] Propose if-then forces `engagementAuthorizationImplied: false`
- [x] CONTRACT.md names `C2CommandIssuance`, `C2PlayerCommandBridge`, `IPolicyEvaluator`, ADR-018

---

## Test cases

### Case 1: Happy path. Track read

**Fixture:** `ContactPictureEntry` for `c-12` with organic observer; `HasFireControlTrackOnPrimaryContact` true.

**Input:** `c2.track.assess` lane `read`

**Expected behavior:**

1. Skill reads contact picture + sensor C2 snapshot
2. Output names `trackSource: organic` and `fireControlSatisfied: true`
3. `replayProvenance.submitted` is false

**Assertions:**

- [x] SKILL.md Phase 1 lists `ContactPictureProjection` and `SensorC2Projection`
- [x] Skill does not list `engage` as a read output command
- [x] Skill forbids order-log append on read

### Case 2: Failure path. Shared track as fire

**Fixture:** Contact exists only on a `DatalinkEdgeEntry`; `HasFireControlTrackOnPrimaryContact` false.

**Input:** `c2.pairing.recommend` lane `propose` with `commandId: engage`

**Expected behavior:**

1. Pairing skill FAIL
2. CONTRACT.md ADR-018 rule: shared track cannot take `weaponsRelease`

**Assertions:**

- [x] Pairing SKILL.md says `engage` requires `trackSource: organic` and `fireControlSatisfied: true`
- [x] Data-link skill says never `engage`
- [x] Schema/contract keep `engagementAuthorizationImplied` false on propose

### Case 3: Edge case. Submit without approval

**Fixture:** Proposal still `staged`, not `approved`. Replay viewer off. Unit is `HumanController`.

**Input:** `c2.skill.submit`

**Expected behavior:**

1. Host refuses with `PROPOSAL_NOT_APPROVED`
2. No `PlayerOrder` append

**Assertions:**

- [x] CONTRACT.md submit preconditions require state `approved`
- [x] CONTRACT.md lists `PROPOSAL_NOT_APPROVED` as a structured reason
- [x] No Slice A SKILL.md describes a direct enqueue

### Case 4: Retrieval. Explanation cites abort code

**Fixture:** `EngageExplainProjection` returns `NO_FIRE_CONTROL`.

**Input:** `c2.explain`

**Expected behavior:**

1. Output `reasonPlain` matches `EngageExplainProjection.ExplainCode`
2. At least one `sequenceId` or projection citation

**Assertions:**

- [x] Explanation skill says do not paraphrase a known abort into a different cause
- [x] Lane is `read` only

---

## Protocol compliance

- [x] Implementer skill (`.claude/skills/agent-c2-skill-contract/SKILL.md`) is read-only (`allowed-tools` without Write)
- [x] Implementer skill names BLOCKED files (DelegationBridge, CatalogWriteGate, SimulationSession, gauntlet, t2, harness, MissionContactTargetClass, DRG-179 types)
- [x] Next-step handoff present

Re-run: `powershell -NoProfile -File production/docs/skills/agent-c2-skill-contract/verify-contract.ps1`
