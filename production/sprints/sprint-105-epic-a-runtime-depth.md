# Sprint 105 — Epic A Runtime Depth (Approved C2)

**Dates:** 2026-07-17 → 2026-07-21  
**Stage:** **Release** · **Not Launch**  
**Program:** Epic A — Approved C2 runtime depth after S104 hosts

## Goal

Turn Approved assets into **live runtime APIs** (trackers, selection depth, category class map), not only USS/UXML wires. Maximize headless C# + tests on independent surfaces.

## Must Have

| ID | Task | Acceptance |
|----|------|------------|
| S105-01 / A1 | CombatDomainsHotTickTracker + binder BindFromTracker; host uses tracker; full domain UXML rows | Tests green; host maps Air…Facility |
| S105-02 / A2 | MessageLog category class map; SelectBySequenceId / SelectByUnitId; host APIs | Headless tests green |
| S105-03 | Residual dual retest | SYN-T12 dual + MD-001 PASS |
| S105-04 | Suite floor | ≥1638 live (prefer ≥1717) |

## Non-goals

Launch · invent Approved · DelegationBridge hotpath mutation · Baltic hash reopen

---
*S105 lean plan — 2026-07-17.*
