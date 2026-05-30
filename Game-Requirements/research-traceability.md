# Research → Requirements Traceability

**Last Updated:** May 29, 2026

Maps research supplements under `docs/research/` to Game Requirements documents and downstream design artifacts.

## Research Documents

| Document | Scope | Primary requirements |
|----------|-------|---------------------|
| [agentic-cmano-research.md](../docs/research/agentic-cmano-research.md) | CMO product architecture, DB workflow, agentic opportunities, clean-room roadmap | 01, 04, 06, 07, 08, 11 |
| [near-future-tech-research.md](../docs/research/near-future-tech-research.md) | 2025–2035 TRL 5–9 systems, parameters, gap analysis | 09, 15, 14, 18 |
| [speculative-systems-research.md](../docs/research/speculative-systems-research.md) | 2030–2045+ speculative systems, TRL, escalation | 10, 13, 19 |

---

## agentic-cmano-research.md

| Research section | Requirement updates | Status |
|------------------|---------------------|--------|
| Five-layer architecture (sim, DB, scenario, UX, automation) | [08-Agentic-Architecture](../requirements/08-Agentic-Architecture.md) § Five-Layer | Integrated May 29 |
| Dual-track DB pipeline (public + internal) | [06-Database-Intelligence](../requirements/06-Database-Intelligence.md) § Dual-Track | Integrated May 29 |
| Provenance per field, release trains | [06](../requirements/06-Database-Intelligence.md) § Provenance, Release trains | Integrated May 29 |
| Database agents (retrieval → human review) | [06](../requirements/06-Database-Intelligence.md) § DB Research Agent; [07](../requirements/07-Agentic-Infrastructure.md) § DB Research Assistants | Integrated May 29 |
| Scenario agents | [07](../requirements/07-Agentic-Infrastructure.md) § Scenario Generation | Existing + aligned |
| Operator copilots (supervised, evidence-linked) | [07](../requirements/07-Agentic-Infrastructure.md) § Operator Copilot | Integrated May 29 |
| Monte Carlo / experiment system | [07](../requirements/07-Agentic-Infrastructure.md) § Experiment Agent | Integrated May 29 |
| Simulation API, import/export, telemetry | [08](../requirements/08-Agentic-Architecture.md) § Interchange | Integrated May 29 |
| Clean-room legal boundaries | [01-Project-Overview](../requirements/01-Project-Overview.md) § Goal 5 | Integrated May 29 |
| Build roadmap phases 1–5 | [07](../requirements/07-Agentic-Infrastructure.md) § Build Phase Alignment | Integrated May 29 |
| NL/MCP mission editor patterns | [11-Agentic-Mission-Editor](../requirements/11-Agentic-Mission-Editor.md) | Pre-existing parity |

---

## near-future-tech-research.md

| Research finding | Requirement updates | Status |
|------------------|---------------------|--------|
| CCA archetypes, Fury parameters, Quarterback bonus | [09](../requirements/09-Near-Future-Technologies.md) §1 | Integrated May 29 |
| Swarm tiers; max 500 v1.0 | [09](../requirements/09-Near-Future-Technologies.md) §1; Resolved Decisions | Integrated May 29 |
| Replicator attritables, C-UAS, JADC2 node, hypersonic defense, SOSUS | [09](../requirements/09-Near-Future-Technologies.md) *(new entities)* | Integrated May 29 |
| DEW thermal/power modeling | [09](../requirements/09-Near-Future-Technologies.md) §2; Resolved Decisions | Integrated May 29 |
| Quantum as doctrine unlock | [09](../requirements/09-Near-Future-Technologies.md) §3 | Integrated May 29 |
| CEW learning curve, cyber cascade timers | [09](../requirements/09-Near-Future-Technologies.md) §4 | Integrated May 29 |
| Signature management subsystem | [09](../requirements/09-Near-Future-Technologies.md) §5 | Integrated May 29 |
| TL-0–TL-3 framework | [09](../requirements/09-Near-Future-Technologies.md) § Technology Level | Integrated May 29 |
| Performance envelope table | [09](../requirements/09-Near-Future-Technologies.md) § Reference | Integrated May 29 |
| Limited SDA / ASAT escalation | [09](../requirements/09-Near-Future-Technologies.md) §3; Resolved Decisions | Integrated May 29 |

**Downstream GDD candidates:** sensor-detection-ew.md (quantum doctrine), engagement-fire-control.md (hypersonic alert), policy-roe-emcon-wra.md (CCA autonomy modes)

---

## speculative-systems-research.md

| Research finding | Requirement updates | Status |
|------------------|---------------------|--------|
| TL-3–TL-5 extended framework | [10](../requirements/10-Speculative-Systems.md) § Technology Level | Integrated May 29 |
| MaRV migration to doc 09 | [10](../requirements/10-Speculative-Systems.md) § Migration note | Integrated May 29 |
| 5-tier escalation ladder | [10](../requirements/10-Speculative-Systems.md) § Escalation Ladder | Integrated May 29 |
| LAWS ROE modes | [10](../requirements/10-Speculative-Systems.md) §3; [04](../requirements/04-Agent-Delegation.md) § Autonomy | Integrated May 29 |
| Kessler meter, DE-ASAT tiers | [10](../requirements/10-Speculative-Systems.md) §4 | Integrated May 29 |
| BCI operator failure modes | [10](../requirements/10-Speculative-Systems.md) §5 | Integrated May 29 |
| Gap systems (PQC, hypersonic intercept, deepfake IW, quantum nav, autolog) | [10](../requirements/10-Speculative-Systems.md) *(new)* | Integrated May 29 |
| TRL reference table | [10](../requirements/10-Speculative-Systems.md) § TRL Reference | Integrated May 29 |
| BLACK_PROJECT_MODE / no impossible physics | [10](../requirements/10-Speculative-Systems.md) § Resolved Decisions | Integrated May 29 |
| Mosaic Warfare / OODA dominance | [10](../requirements/10-Speculative-Systems.md) §6 | Integrated May 29 |

---

## Open gaps (post-integration)

Items identified in research but not yet fully specified in requirements 13–20:

| Gap | Suggested owner doc | Priority |
|-----|---------------------|----------|
| `HYPERSONIC_ALERT` game state UI | 20 Command & Control UI | P1 |
| `KESSLER_RISK_METER` global sim state | 18 Combat Domains or 19 Cyber/Comms | P1 |
| JADC2 node as damageable entity | 19 Cyber & Comms | P1 |
| Monte Carlo experiment schema | 17 Replay/AAR + ADR-003 extension | P2 |
| Public DB intake GitHub workflow | 06 + tooling epic | P2 |

---

## Next workflow steps

1. Run `/design-review` on updated docs 01, 04, 06–10 for cross-consistency
2. Propagate TL framework into [12-Terms-Glossary](../requirements/12-Terms-Glossary.md)
3. Update GDDs (sensor, engagement, policy) with new mechanics from doc 09
4. Run `/military-requirements-impact` on new gap entities before database schema work
