# UCA-A — unity-csharp-architect post-v1 adoption train

**As of:** 2026-08-12  
**Status:** **CLOSED** (UCA-A0…A5 complete)  
**Stage:** Release (unchanged)  
**Prior train:** UCA-M0…M5 **CLOSED** — skill `metadata.status: v1` / `1.0.0` (PR #477, 2026-08-11)  
**Authority:** `linear-usage-contract.md` · `linear-parallel-dispatch-playbook.md` · skill `production/agentic/skills/unity-csharp-architect/`

---

## 1. Purpose

Make **v1** skill the **default architecture gate** on Unity/C# agent work:

1. Load `unity-csharp-architect`
2. Run `checklists/pr-finish.md`
3. Paste **PASS / FAIL / BLOCKED** into the PR body
4. Cite presentation as **ADR-010 / 007 / 001** — **never** Git **ADR-018** (sensor side-picture / datalink)

Does **not** reopen closed epic [DRG-124](https://linear.app/drgamtd-workspace/issue/DRG-124).

---

## 2. Tracker

| Layer | Location |
| --- | --- |
| **Linear project** | [*UCA Adoption (UCA-A)*](https://linear.app/drgamtd-workspace/project/uca-adoption-uca-a-085666c9d310) |
| **Epic** | [DRG-135](https://linear.app/drgamtd-workspace/issue/DRG-135) |
| **Scaffold** | [DRG-136](https://linear.app/drgamtd-workspace/issue/DRG-136) (UCA-A0) |
| **Routing** | [DRG-137](https://linear.app/drgamtd-workspace/issue/DRG-137) (UCA-A1 / Lane E) |
| **Citation** | [DRG-138](https://linear.app/drgamtd-workspace/issue/DRG-138) (UCA-A2 / Lane F) |
| **Soft CI** | [DRG-139](https://linear.app/drgamtd-workspace/issue/DRG-139) (UCA-A3 / Lane G) |
| **Product dogfood** | [DRG-140](https://linear.app/drgamtd-workspace/issue/DRG-140) / [DRG-141](https://linear.app/drgamtd-workspace/issue/DRG-141) (UCA-A4 / Lane H) |
| **Closeout** | [DRG-142](https://linear.app/drgamtd-workspace/issue/DRG-142) (UCA-A5) |
| **Notion (prior design)** | [unity-csharp-architect Skill — Design & Sprint Roadmap](https://app.notion.com/p/3b9f7cb4e4df813a9798cc7d1f86aa20) |
| **Skill** | `production/agentic/skills/unity-csharp-architect/` |
| **M5 AAR** | `production/agentic/skills/unity-csharp-architect/AAR-uca-m5-dogfood-2026-08-11.md` |
| **UCA-A AAR** | `production/agentic/skills/unity-csharp-architect/AAR-uca-a-adoption-2026-08-12.md` |

---

## 3. Outcome milestones

| ID | Outcome | Exit |
| --- | --- | --- |
| **UCA-A0** | Scaffold | This file + Linear project/epic/issues + Notion pointer |
| **UCA-A1** | Routing embeds skill | `AGENTS.md` + `local-cloud-agent-routing.md` require skill + pr-finish |
| **UCA-A2** | Citation hygiene | Live design pointers: ADR-010/007/001 only for presentation |
| **UCA-A3** | Soft CI posture | Decision recorded: advisory only; no product-floor hard lint |
| **UCA-A4** | Product consume | ≥2 product Unity PRs with pr-finish **PASS** **or** explicit park |
| **UCA-A5** | Closeout | Project Completed; epic Done; stage Release |

---

## 4. Parallel lanes (file-disjoint)

| Lane | Owns | Issues |
| --- | --- | --- |
| **E — Routing** | `AGENTS.md`, `production/agentic/local-cloud-agent-routing.md` | DRG-137 |
| **F — Citation** | Notion design + this file citation section | DRG-138 |
| **G — CI posture** | `checklists/soft-ci-rg.md` decision block | DRG-139 |
| **H — Product consume** | Product Unity surfaces only (named before dispatch) | DRG-140, DRG-141 |

**Collision ban:** Projection cluster, `unity/ProjectAegis/Assets/**` single-owner across H tracks; `DelegationBridge` / `SimulationSession` zero-touch without product waiver.

---

## 5. ADR citation law (UCA-A2)

| Topic | Cite | Do **not** cite |
| --- | --- | --- |
| Presentation wall (UI is client; snapshot/projection; map read-only) | **ADR-010** §2–3, **ADR-007**, **ADR-001** | **Git ADR-018** |
| Sensor side-picture / datalink | **Git ADR-018** | As a presentation boundary |

Skill doctrine already correct at v1; this train fixes **live pointers** (Notion / historical epic wording).

---

## 6. Soft CI posture (UCA-A3 decision — 2026-08-12)

**Decision:** Patterns in `checklists/soft-ci-rg.md` remain **advisory only**.

- Checklist-first (`pr-finish.md`) is the architecture finish gate.
- Soft `rg` hits inform humans/agents; they must **not** become required GitHub or Buildkite statuses that fail product suite floors.
- Enabling hard lint requires a **separate product decision issue** (not UCA-A scope) and explicit workflow changes.

---

## 7. Wave plan

| Wave | Tracks | Notes |
| --- | --- | --- |
| **0** | Scaffold | This file + Linear + Notion |
| **1** | E + F + G parallel | Docs-only; no product code |
| **2** | H1 + H2 (≤2) | After product names Surfaces |
| **3** | A5 closeout | Serial |

---

## 8. Non-goals

- Reopen DRG-124 / change skill status off `v1` without doctrine change
- Phase N / Launch / REQ-NB product scope
- Invent product features under UCA-A issues
- Hard CI on product floors without separate product issue
- Concurrent H tracks on the same surface cluster

---

## 9. Definition of Done (train)

1. UCA-A0…A3 met with Git path evidence  
2. UCA-A4 Done or parked with product-ack  
3. DRG-135 Done; Linear project Completed  
4. Stage still **Release**

---

## 10. Closeout (UCA-A5 — 2026-08-12)

| Gate | Evidence |
| --- | --- |
| UCA-A0 scaffold | PR [#478](https://github.com/drgaciw/cmano-clone/pull/478) @ `eef91b57` · [DRG-136](https://linear.app/drgamtd-workspace/issue/DRG-136) Done |
| UCA-A1 routing | `AGENTS.md` + `local-cloud-agent-routing.md` · [DRG-137](https://linear.app/drgamtd-workspace/issue/DRG-137) Done |
| UCA-A2 citation | Notion + kickoff §5 ADR law · [DRG-138](https://linear.app/drgamtd-workspace/issue/DRG-138) Done |
| UCA-A3 soft CI | `checklists/soft-ci-rg.md` advisory-only · [DRG-139](https://linear.app/drgamtd-workspace/issue/DRG-139) Done |
| UCA-A4 product dogfood | PR [#479](https://github.com/drgaciw/cmano-clone/pull/479) @ `79ddaedf` — OobTreeBridge + MessageLogBridge · [DRG-140](https://linear.app/drgamtd-workspace/issue/DRG-140) / [DRG-141](https://linear.app/drgamtd-workspace/issue/DRG-141) Done |
| Skill status | **v1** / `1.0.0` unchanged (no doctrine change) |
| Stage | **Release** (unchanged) |
| AAR | `production/agentic/skills/unity-csharp-architect/AAR-uca-a-adoption-2026-08-12.md` |

**Train DoD:** met. Linear project *UCA Adoption (UCA-A)* → Completed. Epic [DRG-135](https://linear.app/drgamtd-workspace/issue/DRG-135) Done.

**Default gate going forward:** UnityAdapter / MB / C2 / Editor / asmdef / presentation work loads `unity-csharp-architect`, runs `checklists/pr-finish.md`, pastes **PASS / FAIL / BLOCKED**, cites **ADR-010 / 007 / 001** (never Git ADR-018 for presentation).

---

**End of uca-a-adoption-train-2026-08-12.md**
