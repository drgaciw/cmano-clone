# Post-S97 Roadmap — Agent Capability Train (UCA)

**As of:** hub 2026-08-11  
**Stage:** Release (unchanged)  
**Authority:** `linear-usage-contract.md` · `linear-parallel-dispatch-playbook.md` · skill tree under `production/agentic/skills/unity-csharp-architect/`

---

## 1. Closed product trains

| Train | Status | Evidence |
| --- | --- | --- |
| **S89–S97** | **CLOSED** + human acks | Agentic workflow series; do not reopen |
| **S110–S114 Release Product Progress** | **CLOSED** (engineering + human ack) | Gate + smoke + phrase `release product progress program complete` (2026-08-11) |

**Hold posture:** remain on **Release**. Residual/SWARM/waterline queues cleared for new *planned* work. Product feature sprints continue only from the **existing product backlog** (separate dispatch), not this document.

**Out of this train:** Nordic-Baltic expansion **REQ-NB-01…REQ-NB-10** — research-only; **0/10** on product sprint roadmaps. Track separately if product prioritizes.

---

## 2. Next train (non-product, high-leverage)

**Program:** **unity-csharp-architect** skill (**UCA**)  
**Why now:** Unity presentation / Editor / Adapter work matured (UI maturity, PE, C2 chrome), but architecture rules still live in ADRs + tribal memory. Agents need one load-on-demand skill for Unity C# + Aegis ADRs (presentation boundary (ADR-010/007/001), ADR-010 headless/command UI).

| Layer | Location |
| --- | --- |
| **Git (files)** | `production/agentic/skills/unity-csharp-architect/` |
| **Linear (delivery)** | Project [*Unity C# Architect Skill*](https://linear.app/drgamtd-workspace/project/unity-c-architect-skill-7265dc770ee2) |
| **Linear epic** | [DRG-124](https://linear.app/drgamtd-workspace/issue/DRG-124) |
| **Executable issues** | DRG-125…134 (UCA-01…UCA-10) |
| **Outcome milestones** | DRG-118…123 (UCA-M0…M5) |
| **Notion (design)** | [*unity-csharp-architect Skill — Design & Sprint Roadmap (2026-08-11)*](https://app.notion.com/p/3b9f7cb4e4df813a9798cc7d1f86aa20) |
| **Tracking** | PR **#471** (UCA-M0 scaffold); issue **#472** |

Cadence = **outcome milestones**, not Linear cycles.

---

## 3. Milestones UCA-M0 … UCA-M5

| ID | Outcome | Linear |
| --- | --- | --- |
| **UCA-M0** | Spec + skeleton + cross-tool artifacts | DRG-118 |
| **UCA-M1** | Core doctrine — SKILL.md `m1-doctrine` (final **v1** at M5); presentation-boundary + headless-command-ui | DRG-119 |
| **UCA-M2** | Structure pack — asmdefs, SO, mono, perf, testing | DRG-120 |
| **UCA-M3** | Aegis playbooks — aegis-unity-map + editor-vs-runtime | DRG-121 |
| **UCA-M4** | Agent gates — pr-finish + review-gates; optional soft CI | DRG-122 |
| **UCA-M5** | Dogfood + closeout — one Unity PR; AAR; skill v1 | DRG-123 |

---

## 4. Executable children (epic DRG-124)

| ID | Linear | Lane | Milestone | Surface |
| --- | --- | --- | --- | --- |
| **UCA-01** | DRG-125 | A | M0 | ROADMAP + scaffold freeze |
| **UCA-02** | DRG-126 | A | M1 | `SKILL.md` v1 phases/triggers/handoffs |
| **UCA-03** | DRG-127 | A | M1 | `references/presentation-boundary.md` |
| **UCA-04** | DRG-128 | A | M1 | `references/headless-command-ui.md` |
| **UCA-05** | DRG-129 | B | M2 | `references/asmdefs-and-layers.md` |
| **UCA-06** | DRG-130 | B | M2 | `references/scriptableobjects-data.md` |
| **UCA-07** | DRG-131 | B | M2 | `references/mono-anti-patterns.md` |
| **UCA-08** | DRG-132 | B | M2 | `performance-unity.md` + `testing-unity.md` |
| **UCA-09** | DRG-133 | C | M3 | Aegis map + editor-vs-runtime |
| **UCA-10** | DRG-134 | D | M4→M5 | Finish gates + dogfood closeout |

**c-sharp-engineer concerns** (encoded in ACs): layering, DI, SOLID, immutability, allocations, async, testing. Skill authors doctrine; product C# stays on separate product issues.

---

## 5. Parallel lanes (file-disjoint)

Root: `production/agentic/skills/unity-csharp-architect/`  
**Surface rule:** no agent edits outside its lane paths.

| Lane | Owns |
| --- | --- |
| **A — Doctrine** | `SKILL.md`, `references/presentation-boundary.md`, `references/headless-command-ui.md` |
| **B — Structure** | `references/asmdefs-and-layers.md`, `mono-anti-patterns.md`, `scriptableobjects-data.md`, `performance-unity.md`, `testing-unity.md` |
| **C — Aegis map** | `references/aegis-unity-map.md`, `references/editor-vs-runtime.md` |
| **D — Gates** | `checklists/*` |

Dispatch via `linear-parallel-dispatch-playbook.md` from **UCA-M1** onward (M0 is single-lane land).

---

## 6. Explicit non-goals (this train)

- **Phase N** reopen / fiction (stay backlog until product re-opens)
- **Launch** / commercial / stage advance
- **Product feature sprints** (sim, gauntlet, catalog, player-facing Unity) — separate backlog
- **REQ-NB-01…10** Nordic-Baltic theater expansion — product research, not UCA
- Rewriting headless .NET sim core; full DOTS-in-Unity playbook (v1: presentation non-authoritative)
- Browser / TanStack skill surfaces

---

## 7. Definition of Done (UCA train)

Train is **Done** when all are true:

1. **UCA-M0…M5** exit criteria met and merged to `main` (docs under skill tree + dogfood evidence).
2. `SKILL.md` status **v1** at **train** Done (UCA-M5); M1 lands `m1-doctrine`. References + checklists loadable by agents by M4.
3. At least **one real Unity PR** on main used `checklists/pr-finish.md` (linked from AAR).
4. Linear project *Unity C# Architect Skill* **completed**; DRG-118…123 + DRG-124…134 Done with PR evidence.
5. No product/sim behavior changes claimed under UCA issues; stage still **Release**.
6. Non-goals held: no Phase N, Launch, REQ-NB product scope, or product-sprint scope creep in UCA PRs.

---

## 8. Immediate next

1. **Merge PR #471** (`docs(agentic): unity-csharp-architect skill roadmap + skeleton (UCA-M0)`) → mark **DRG-118 / UCA-01 / UCA-M0** Done.  
2. **Dispatch UCA-M1+** on file-disjoint lanes **A/B/C/D** (priority: Lane A doctrine → B structure; C/D can parallel once M1 skeleton exists). Start with DRG-126…128.  
3. Product work (if any) remains a **separate** backlog dispatch — not stacked into UCA PRs.

---

## Pointers

- Skill roadmap: `production/agentic/skills/unity-csharp-architect/ROADMAP.md`
- Kickoff: `production/agentic/unity-csharp-architect-skill-roadmap-2026-08-11.md`
- S114 ack: `production/agentic/s114-human-ack-record-2026-08-11.md` (if present)
- Series context: `production/agentic/agentic-workflow-sprint-series-2026-08-09.md`
- Product future roadmap (S94+): `docs/reports/future-sprint-roadmap-07142026.md` — **does not** own UCA; this file does

---

**End of post-s97-uca-agent-capability-train-2026-08-11.md**
