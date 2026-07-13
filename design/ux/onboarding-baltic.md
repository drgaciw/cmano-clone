# Onboarding — Baltic Patrol (NPE)

> **Status:** Stub — Sprint 35 (S35-06)  
> **Last Updated:** 2026-06-19  
> **Scope:** Minimal first-run copy for Baltic vertical slice — presentation only; no full tutorial system  
> **Related:** [c2-command-post.md](c2-command-post.md), [difficulty-curve.md](../difficulty-curve.md), [interaction-patterns.md](interaction-patterns.md)

---

## Mission Goal

**Patrol the Baltic theater, maintain situational awareness, and classify hostile contacts before engaging.**

You command a friendly patrol force in a near-future NATO scenario. Success means:

1. **See** the picture — map symbols, OOB tree, and CONTACTS tab stay synchronized.
2. **Classify** hostile tracks (`◆`) before committing fires.
3. **Execute** when ready — press **Begin Execution** to leave planning and let the sim tick.
4. **Adapt** when comms degrade — read the top-bar COMMS legend and message log for policy denials.

**Recommended NPE path:** `baltic-patrol-classify` → short `baltic-patrol` run → optional `baltic-patrol-comms` (facilitated).

---

## First Actions (first 5 minutes)

| Step | Where to look | What to do |
|------|---------------|--------------|
| 1 | OOB tab (left drawer) | Confirm friendly unit is selected; note patrol unit name. |
| 2 | Map (center) | Find friendly `■` and hostile `◆` symbols; selection ring follows clicks. |
| 3 | CONTACTS tab | Open hostile row; confirm CONTACT summary matches map symbol. |
| 4 | Top bar | Read `PHASE: Planning`; note `COMMS:` state and inline legend row. |
| 5 | Begin Execution | When posture is set, click **Begin Execution** to start the tactical tick. |
| 6 | Message log | Watch CONTACT, MISSION, and COMMS lines; log is your AAR trail. |

**In-game hint (S35-06):** Message log header shows one-line mission stub — `MISSION: Patrol Baltic — classify hostile ◆ contacts, then Begin Execution.`

---

## COMMS Primer

Top-bar legend (text + color, not color-only):

| State | Player meaning |
|-------|----------------|
| **NOMINAL** | Full picture — live tracks at full opacity. |
| **DEGRADED** | Stale contacts — reduced symbol opacity; ghost `(lag N)` duplicates may appear. |
| **DENIED** | No new engagements — map dimmed; attack menu blocked; policy denial in log. |

See [c2-map-placeholder.md §Comms degradation](c2-map-placeholder.md#comms-degradation-p1--implemented) for map symbology detail.

---

## Catalog → Sim Lag (curator note)

When browsing platform **COMMS** fittings or the **LINK CATALOG**, `LatencyMsNominal` on each link drives **share-lag ticks** in simulation (S34-07). Helper text appears under the platform catalog comms section; isolated fixture: `baltic-patrol-datalink-catalog-latency`.

---

## Out of Scope (S35-06)

- Full guided tutorial or dismissible coach marks (future Polish).
- Delegation badges and trust signals (S36+).
- Live Editor click-feel validation (headless proxy + facilitated think-aloud only).

---

## Acceptance Trace

- [x] Mission goal + first-action table authored
- [x] COMMS legend cross-linked to top-bar UXML copy
- [x] One-line C2 entry hint in `MessageLogPanel.uxml`
- [x] Datalink lag vocabulary linked to catalog helper (S34-07)