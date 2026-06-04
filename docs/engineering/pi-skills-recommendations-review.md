# Review: `pi-skills-recommendations.md` (Milsim / Project Aegis)

**Reviewer lens:** CMANO-style military simulation — deterministic sim, catalog fidelity, replay gates, headless CI.  
**Date:** 2026-06-03

## Verdict

The document is **sound for generic .NET/data agent work** and aligns with repo rules (GitNexus, xUnit, SQLite, JSON). It is **incomplete for milsim-specific work** without the Project Aegis skill stack below.

## What is correct

| Area | Assessment |
|------|------------|
| GitNexus before edit / `detect_changes` before handoff | Matches `AGENTS.md` — mandatory |
| xUnit + `dotnet test ProjectAegis.sln` | Correct; Sim/Data use xUnit, Delegation uses NUnit |
| SQLite + catalog focus | Matches `ProjectAegis.Data.Catalog` |
| Stage ordering (implement → review → architecture) | Good parallel agent boundaries |
| External P0 skills (xunit setup, sqlite-data, security) | Reasonable complements |

## Gaps for this repo (add to skill matrix)

| Priority | Project skill | Use when |
|----------|---------------|----------|
| P0 | `team-simulation` | Sim tick, engage, logistics, policy, replay |
| P0 | `replay-verify` / `determinism-audit` | Any change to `DecisionLog`, tick pipeline, RNG |
| P0 | `gitnexus-impact-analysis` + `gitnexus-exploring` | Symbol edits (MCP or CLI) |
| P0 | `hindsight-gitnexus` | Multi-session implementation |
| P0 | `c-sharp-test-engineer` | Delegation NUnit + Sim xUnit |
| P1 | `database-layer-architecture` / `sqlite-schema-management` | Catalog migrations, provenance |
| P1 | `deterministic-data-access` | Catalog read ordering, snapshot hashes |
| P1 | `military-database-research` | `basePd`, platform specs (already listed) |
| P1 | `mission-editor` path: ADR-008 validation engine | `ProjectAegis.Data.Validation` |
| P2 | `team-unity` | Editor/Cesium only — not headless CI |

## Framework nuance

- **Do not assume xUnit everywhere.** `ProjectAegis.Delegation.Tests` and `UnityAdapter.Tests` use **NUnit**; `ProjectAegis.Sim.Tests` and `ProjectAegis.Data.Tests` use **xUnit**.
- Prefer **`c-sharp-reviewer`** + **`replay-verify`** on PRs touching `SimulationSession`, `MvpEngagementResolver`, or replay goldens.

## Recommended change to `pi-skills-recommendations.md`

Add a **"Project Aegis (milsim)"** section after "Recommended Installed Skills" listing the table above, and extend the decision tree:

- **Sim / engage / fuel / order log** → `team-simulation` + `replay-verify`
- **Replay golden / fingerprint** → `replay-verify` + `determinism-audit`
- **Scenario validation** → ADR-008 + `ProjectAegis.Data.Validation` tests
- **Unity C2 / map** → `team-ui` / `team-unity` (manual QA gate)

## External skills — keep / drop

| Skill | Recommendation |
|-------|----------------|
| `dotnet-testing-xunit-project-setup` | Keep for Sim/Data test projects |
| `sqlite-data` | Keep for catalog |
| `smithery.ai@security` | Keep |
| `flutter-implement-json-serialization` | Keep as *contract checklist* only (patterns transfer; not Flutter-specific APIs) |
| `text-to-sql` | Use sparingly — catalog SQL is hand-written, small surface |
| `exploring-data-catalog` (AWS) | Optional; GitNexus + `military-database-research` usually enough |

## Bottom line (milsim)

Default stack for **gameplay/sim PRs**:

1. GitNexus impact/context  
2. `team-simulation` (scope the layer)  
3. `c-sharp-engineer` + `c-sharp-test-engineer`  
4. `replay-verify` before merge  
5. `sqlite-data` / `deterministic-data-access` if touching catalog  

Default stack for **data/catalog-only PRs**:

1. GitNexus  
2. `database-layer-architecture`  
3. `sqlite-data` + `test-helpers`  
4. `smithery.ai@security` on import paths