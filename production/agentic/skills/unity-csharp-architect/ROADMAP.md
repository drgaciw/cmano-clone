# unity-csharp-architect — Milestone Roadmap

**Program:** Unity C# Architect Skill  
**Kickoff:** 2026-08-11  
**Git home:** `production/agentic/skills/unity-csharp-architect/`  
**Linear:** project *Unity C# Architect Skill*  
**Notion:** *unity-csharp-architect Skill — Design & Sprint Roadmap (2026-08-11)*  
**Cadence policy:** outcome milestones, **not** Linear cycles (see `linear-usage-contract.md` §7)

---

## Milestone table

| ID | Outcome | Exit criteria |
| --- | --- | --- |
| **UCA-M0** | Spec + skeleton + cross-tool artifacts | This tree + kickoff note on a branch; Notion design page; Linear project + issues |
| **UCA-M1** | Core skill doctrine | `SKILL.md` v1 (no longer scaffold); `references/presentation-boundary.md` + `headless-command-ui.md` complete |
| **UCA-M2** | Structure reference pack | `asmdefs-and-layers`, `scriptableobjects-data`, `mono-anti-patterns`, `performance-unity`, `testing-unity` |
| **UCA-M3** | Aegis-specific playbooks | `aegis-unity-map.md`, `editor-vs-runtime.md`; map real repo paths |
| **UCA-M4** | Agent gates | `checklists/pr-finish.md`, `review-gates.md`; optional soft CI/`rg` lint documented |
| **UCA-M5** | Dogfood + closeout | One real Unity PR uses checklist; AAR note; skill marked v1; Linear project completed |

---

## Parallel lanes (UCA-M1 onward)

File-disjoint lanes for `linear-parallel-dispatch-playbook.md`:

| Lane | Owns |
| --- | --- |
| **A — Doctrine** | `SKILL.md`, `presentation-boundary.md`, `headless-command-ui.md` |
| **B — Structure** | `asmdefs-and-layers.md`, `mono-anti-patterns.md`, `scriptableobjects-data.md`, `performance-unity.md`, `testing-unity.md` |
| **C — Aegis map** | `aegis-unity-map.md`, `editor-vs-runtime.md` |
| **D — Gates** | `checklists/*` |

**Surface rule:** no agent edits outside its lane paths. No CRITICAL hub / sim-core product work under this program unless a separate product issue requires it.

---

## Soft date guide (not cycle boxes)

| Window | Focus |
| --- | --- |
| Kickoff week | UCA-M0 land |
| Next capacity after Release train | UCA-M1–M2 |
| Follow-on | UCA-M3–M4 |
| After first dogfood PR | UCA-M5 closeout |

Dates on Linear are **soft**; exit criteria above are hard.

---

## Non-goals

- Rewriting the .NET sim core
- Shipping a player-facing Unity feature as part of this program
- Full DOTS-in-Unity playbook (v1: presentation remains non-authoritative; sim ECS stays headless)
- Browser / TanStack skills (different product surface)

---

## Defaults until human override

1. Skill path stays under `production/agentic/skills/`
2. CI: checklist-first; hard lint optional in UCA-M4
3. ADR numbers are cited, not re-hosted (Linear usage contract: issues are pointers)
