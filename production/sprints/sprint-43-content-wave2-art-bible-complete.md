# Sprint 43 — Content Wave 2 + Art Bible Complete (B1 + B2)

**Dates:** ~TBD (~10–12 days)  
**Trunk:** `main` @ (post-S42)  
**Predecessor:** Sprint 42 — COMPLETE (B1 wave 1, art bible 1–4)  
**Stage:** Release Enablement (Track B)  
**Authority:** Post-gate boundary doc + [`docs/reports/future-sprint-roadpmap.md`](../../docs/reports/future-sprint-roadpmap.md) §9 (B1 wave 2 + B2 complete). **Requires scope-expansion decision** (must be recorded before S42; inherited).

> **OUT-OF-BOUNDARY — PLANNING ONLY** until scope gate recorded and S42 complete.

## Sprint Goal

Complete B1 content scope (Engage/features batch + Platform/scenario remainder) and B2 (full 9-section art bible + asset specs). Evidence/playtest cadence continues. Exit: B1+B2 epic criteria met per gate scope.

## Capacity

- Total days: 12
- Buffer: 2 days
- **Effective dev-days**: **10**
- **Parallelism**: **5 tracks**

## Tasks

### Must Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S43-01 | **Re-baseline** | c-sharp-devops-engineer | 1 | S42-06 | Tests PASS; GitNexus @ tip |
| S43-02 | **QA plan sprint 43** | team-qa | 1 | S43-01 | Blocks waves |
| S43-03 | **Content Engage/features batch** — remaining Partial → committed (per gate) | gameplay-programmer + team-simulation | 3 | S43-02 | Replay-gated; tracker updated |
| S43-04 | **Content Platform/scenario remainder** — close B1 | team-data | 2.5 | S43-02 | B1 exit checklist |
| S43-05 | **Art bible sections 5–9 + asset specs** | art-director | 2.5 | S43-02 | Full 9-section bible + specs |
| S43-06 | **Closeout** | c-sharp-devops-engineer | 0.5 | S43-03+ | Smoke PASS; B1+B2 exit noted |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S43-07 | **Evidence + playtest cadence** | team-qa | 1.5 | S43-03 | Local Editor PNGs where applicable |

## Definition of Done

- [ ] B1 content complete per scope decision
- [ ] B2 art bible 9 sections + asset specs complete
- [ ] Replay 6/6; proxy 18/18+; tests monotonic
- [ ] Evidence/playtest doc for wave 2

## Parallel Execution Model

| Track | Stack prefix | Agent env | Stories |
|-------|--------------|-----------|---------|
| Content Engage | `stack/sprint43/content-engage` | Cloud + determinism review | S43-03 |
| Content remainder | `stack/sprint43/content-remainder` | Cloud | S43-04 |
| Art bible complete | `stack/sprint43/art-bible-complete` | Cloud | S43-05 |
| Evidence | `stack/sprint43/evidence` | **Local** | S43-07 |
| Closeout | `stack/sprint43/closeout` | Local | S43-06 |

## Related Artifacts

Roadmap §9; kickoff `production/agentic/sprint-43-parallel-kickoff-2026-06-20.md`.

---

*Planning only. Requires scope gate + S42 completion.*
