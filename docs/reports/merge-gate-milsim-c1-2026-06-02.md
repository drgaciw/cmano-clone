# Merge Gate — milsim C1 + Sprint 2

**Original date:** 2026-06-02  
**Last refreshed:** 2026-06-19  
**Source branch:** `stack/milsim-c1-combat-data-replay`  
**Target:** `main`  
**Merge:** PR **#36** @ `1f7423e` (2026-06-02)  
**Verdict:** **MERGED** (original gate **READY**); lineage **HEALTHY** (2026-06-19 refresh)

---

## Summary

This gate authorized merge of the **C1 order-log union** (`AgentDecisionPayload`), CMO catalog export bridge, combat outcomes/checkpoints, and Sprint 2 production scaffolding. The stack landed on `main` via **PR #36** the same day. Design-review blocker **C1** (DecisionLog vs full order log union) is **closed** per [architecture-review-2026-06-02.md](../architecture/architecture-review-2026-06-02.md).

**Related gates:** [Baltic headless slice gate](baltic-headless-slice-gate-2026-06-01.md) · [vertical-slice gate](../production/vertical-slice/gate-2026-06-02.md) **PROCEED**

---

## Original gate evidence (2026-06-02)

| Gate | Result |
|------|--------|
| `dotnet test ProjectAegis.sln` | **181 / 181** pass |
| PlayMode + ReplayGolden | **12 / 12** pass |
| `/replay-verify` report | [replay-2026-06-02.md](../production/determinism/replay-2026-06-02.md) **PASS** |
| `/smoke-check` | [smoke-2026-06-02.md](../production/qa/smoke-2026-06-02.md) **PASS** |
| Sensor GDD | **Approved** (headless MVP) |
| GitNexus detect-changes | **Medium** — expected C1 payload touch (`DecisionLog`, `PlayerInfoFilter`) |

### PR scope (merged)

**Engineering**

- C1 `AgentDecisionPayload` — typed order-log union completion ([story-001](../production/epics/order-log-replay-slice/story-001-agent-decision-payload.md))
- Catalog CMO export pipeline + quarantine path
- Combat outcomes, checkpoints, message log bridge

**Production / infra**

- `sprint-status.yaml`, `stage.txt` (**Production**), smoke + replay gates
- Superpowers global setup (`tools/install-superpowers.ps1`)

### Post-merge checklist (Sprint 2 continuation)

| Item | Status (2026-06-19) |
|------|---------------------|
| Contact Classify/Identify FSM (`TR-sensor-001`) | **Complete** — `sensor-classify-slice` epic |
| Unity sensor C2 vertical slice | **Complete** — `sensor-c2-ui-slice` epic |
| `npx gitnexus analyze` refresh | **Ongoing** — 14,424 nodes @ `d3db76d` |

---

## C1 blocker resolution

Source: [requirements-13-20-design-review-2026-05-29.md](../../Game-Requirements/reviews/requirements-13-20-design-review-2026-05-29.md)

| Blocker | Requirement | Resolution | Status |
|---------|-------------|------------|--------|
| **C1** | Order log + replay (doc 17) | `AgentDecisionPayload`, `IOrderLog` / ADR-003, `ReplayGoldenSuiteTests` | **Closed** |

**GDD:** [order-log-replay.md](../design/gdd/order-log-replay.md) — C1 typed payload landed 2026-06-02.

**Key symbols (HIGH blast radius — impact before edit):**

- `AgentDecisionPayload` — canonical typed agent decision payload
- `DecisionLog` — append + fingerprint; accepts legacy `DecisionRecord` migration path
- `OrderLogEntry.FromDecisionRecord` — maps legacy records into typed payload
- `PlayerInfoFilter` — projection filter for agent decision entries

---

## Lineage refresh (2026-06-19)

Evidence that C1 deliverables remain stable through Sprints 1–31.

| Check | Result |
|-------|--------|
| `dotnet test ProjectAegis.sln` | **1,006/1,006 PASS** @ `d3db76d` |
| `ReplayGoldenSuiteTests` | **6/6 PASS** — fingerprints include agent + engagement rows |
| `AgentDecisionPayloadTests` + `IOrderLogContractTests` | Green in `ProjectAegis.Delegation.Tests` |
| `HindsightOrderLogHook` | Typed + legacy `DecisionRecord` migration path intact |
| Checkpoints + scrub | `order-log-replay-checkpoints-slice` **MVP Complete** |
| Sprint 2 closeout | **430** tests documented; classify golden PASS |

### Successor epics building on C1

| Epic | Extends C1 with |
|------|-----------------|
| [order-log-replay-checkpoints-slice](../production/epics/order-log-replay-checkpoints-slice/EPIC.md) | Checkpoint store + scrub-to-tick |
| [combat-outcomes-mvp-slice](../production/epics/combat-outcomes-mvp-slice/EPIC.md) | Hit/miss/kill engagement outcomes in log |
| [platform-db-basepd-slice](../production/epics/platform-db-basepd-slice/EPIC.md) | CMO catalog export → detection loop |
| [wave5-engage-cyber-logistics-slice](../production/epics/wave5-engage-cyber-logistics-slice/EPIC.md) | Attack menu, spoof, readiness order-log rows |

### Regression commands

```bash
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "FullyQualifiedName~AgentDecisionPayload|IOrderLogContract"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter FullyQualifiedName~ReplayGoldenSuiteTests
```

---

## Current CONCERNS (non-blocking)

1. **`DecisionLog` HIGH risk** — any schema or fingerprint change requires `gitnexus impact` + golden refresh.
2. **GDD status** — `order-log-replay.md` still **In Progress** at doc level; MVP slice shipped, scrub UI and AAR agent deferred (req 17).
3. **Architecture TR gaps** — 12 gap TRs persist from 2026-06-02 review; refresh recommended post-S30/S31.

None of these reopen the C1 merge gate or block continued stacked delivery.

---

## Recommended next

1. **`gitnexus impact DecisionLog`** before any order-log schema or fingerprint edit.
2. **`/replay-verify`** when touching `AgentDecisionPayload`, `PlayerInfoFilter`, or golden fixtures.
3. **Req 17 backlog** — scrub UI, AAR agent projection (beyond C1 typed payload).
4. **Defer** — human-in-the-loop override UX (blocker **C5**, deferred per ADR-001).

---

*Original gate: 2026-06-02 (READY → merged PR #36). Refreshed: 2026-06-19 from sprint-status, architecture review, and live test baseline.*
