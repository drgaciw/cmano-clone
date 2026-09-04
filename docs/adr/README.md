# Architectural Decision Records (ADR)

This directory contains the Architectural Decision Records (ADRs) for Project Aegis, authored using the [Markdown Architectural Decision Records (MADR)](https://adr.github.io/madr/) format.

For early foundational system architecture decisions (ADR-001 through ADR-022), see [`docs/architecture/`](../architecture/).

## Decision Index

| ADR | Title | Status | Date | Area |
|---|---|---|---|---|
| [ADR-0012](adr-0012-defer-toe-orbat-builder-requirement.md) | Defer Formal Requirements for Dedicated TO&E / ORBAT Builder/Editor | Accepted | 2026-06-20 | Mission Editor / TO&E |
| [S41-03](s41-structural-debt-decision-telemetry-osint.md) | Structural Debt Characterization — Decision (60%), Telemetry (67%), Osint (68%) | Accepted | 2026-06-20 | Structural Debt |
| [ADR-023](ADR-023-lethal-autonomy-opt-in-phase-n.md) | De-scoping Lethal Autonomy Phase Opt-In to Phase N and Interim Safety Gate (HOL-04B) | Accepted | 2026-09-04 | Delegation / Governance |
| [ADR-024](ADR-024-catalog-balance-write-gate.md) | Catalog Balance Write Gate Alignment with Extend-Only Threshold | Accepted | 2026-09-04 | Data / Platform Catalog |
| [ADR-025](ADR-025-speculative-tech-level-decoupling.md) | Decoupling of Technology Level 5 and Black Project Mode Enforcement | Accepted | 2026-09-04 | Sim / Speculative Systems |
| [ADR-026](ADR-026-cec-swarm-us-nato-filtering.md) | Generic Side-Affiliated CEC Swarm Participation Without Runtime Nationality Filtering | Accepted | 2026-09-04 | Sim / CEC & Swarms |
| [ADR-027](ADR-027-cumulative-salvo-budget-accounting.md) | Cumulative Salvo Budget Accounting across Engagement Windows | Accepted | 2026-09-04 | Sim / Policy & WRA |

## MADR Guidelines

Every decision record in this repository follows the MADR template:
- **Status**: Current lifecycle state (`Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded`).
- **Context and Problem Statement**: The forces, audit findings, and technical dilemmas prompting the decision.
- **Decision Drivers**: Key architectural and product priorities guiding the choice.
- **Considered Options**: The technical alternatives evaluated.
- **Decision Outcome**: The chosen path and its justification.
  - **Positive Consequences**: Architectural and operational benefits.
  - **Negative Consequences**: Trade-offs, limitations, and operational burdens.
- **Pros and Cons of the Options**: Comparative trade-off analysis.
- **Implementation Notes**: Specific classes, methods, or test suites governing enforcement.
- **References**: Linear issues, requirement documents, and related ADRs.
