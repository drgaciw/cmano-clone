# unity-csharp-architect — Milestone Roadmap

**Program:** Unity C# Architect Skill  
**Kickoff:** 2026-08-11  
**Git home:** `production/agentic/skills/unity-csharp-architect/`  
**Linear project:** [*Unity C# Architect Skill*](https://linear.app/drgamtd-workspace/project/unity-c-architect-skill-7265dc770ee2)  
**Linear epic:** [DRG-124](https://linear.app/drgamtd-workspace/issue/DRG-124) (UCA-01…UCA-10 = DRG-125…134)  
**Notion:** *unity-csharp-architect Skill — Design & Sprint Roadmap (2026-08-11)*  
**Post-S97 program note:** `production/agentic/post-s97-uca-agent-capability-train-2026-08-11.md`  
**Cadence policy:** outcome milestones, **not** Linear cycles (see `linear-usage-contract.md` §7)

---

## Milestone table

| ID | Outcome | Exit criteria | Linear outcome |
| --- | --- | --- | --- |
| **UCA-M0** | Spec + skeleton + cross-tool artifacts | This tree + kickoff note on a branch; Notion design page; Linear project + issues | [DRG-118](https://linear.app/drgamtd-workspace/issue/DRG-118) |
| **UCA-M1** | Core skill doctrine | `SKILL.md` status `m1-doctrine` (final **v1** at M5); `references/presentation-boundary.md` + `headless-command-ui.md` complete; presentation cites **ADR-010/007/001** (not Git ADR-018 datalink) | [DRG-119](https://linear.app/drgamtd-workspace/issue/DRG-119) |
| **UCA-M2** | Structure reference pack | `asmdefs-and-layers`, `scriptableobjects-data`, `mono-anti-patterns`, `performance-unity`, `testing-unity` | [DRG-120](https://linear.app/drgamtd-workspace/issue/DRG-120) |
| **UCA-M3** | Aegis-specific playbooks | `aegis-unity-map.md`, `editor-vs-runtime.md`; map real repo paths | [DRG-121](https://linear.app/drgamtd-workspace/issue/DRG-121) |
| **UCA-M4** | Agent gates | `checklists/pr-finish.md`, `review-gates.md`; optional soft CI/`rg` lint documented | [DRG-122](https://linear.app/drgamtd-workspace/issue/DRG-122) |
| **UCA-M5** | Dogfood + closeout | One real Unity PR uses checklist; AAR note; skill marked v1; Linear project completed | [DRG-123](https://linear.app/drgamtd-workspace/issue/DRG-123) ✅ |

---

## Executable issues (under epic DRG-124)

| ID | Linear | Lane | Milestone | Surface |
| --- | --- | --- | --- | --- |
| **UCA-01** | DRG-125 | A | M0 | ROADMAP + scaffold freeze |
| **UCA-02** | DRG-126 | A | M1 | `SKILL.md` status `m1-doctrine` (phases / triggers / handoffs; final **v1** at M5) |
| **UCA-03** | DRG-127 | A | M1 | `references/presentation-boundary.md` |
| **UCA-04** | DRG-128 | A | M1 | `references/headless-command-ui.md` |
| **UCA-05** | DRG-129 | B | M2 | `references/asmdefs-and-layers.md` |
| **UCA-06** | DRG-130 | B | M2 | `references/scriptableobjects-data.md` |
| **UCA-07** | DRG-131 | B | M2 | `references/mono-anti-patterns.md` |
| **UCA-08** | DRG-132 | B | M2 | `performance-unity.md` + `testing-unity.md` |
| **UCA-09** | DRG-133 | C | M3 | Aegis map + editor-vs-runtime |
| **UCA-10** | DRG-134 | D | M4→M5 | Finish gates + dogfood closeout |

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
- Nordic-Baltic product expansion (REQ-NB-*) — separate product backlog
- Phase N reopen / Launch stage advance

---

## Defaults until human override

1. Skill path stays under `production/agentic/skills/`
2. CI: checklist-first; hard lint optional in UCA-M4
3. ADR numbers are cited, not re-hosted (Linear usage contract: issues are pointers)
4. Git = files · Linear = status · Notion = design

---

## ADR citation correction (UCA-M1)

UCA-M0 text sometimes called presentation boundary **“ADR-018”**. In git, **ADR-018** is sensor-side-picture-datalink. Presentation boundary doctrine cites **ADR-010 §2–3**, **ADR-007**, **ADR-001** (and **ADR-006** for no-SQLite-from-UI). See `references/presentation-boundary.md`.

## Version contract

| Milestone | `SKILL.md` `metadata.status` |
| --- | --- |
| UCA-M0 | `scaffold` |
| **UCA-M1** | **`m1-doctrine`** (doctrine complete; not final v1) |
| UCA-M2…M4 | still `m1-doctrine` until dogfood |
| **UCA-M5** | **`v1`** after dogfood + AAR — **landed 2026-08-11** |

Do not require `v1` as an M1 exit criterion. Dogfood AAR: [`AAR-uca-m5-dogfood-2026-08-11.md`](AAR-uca-m5-dogfood-2026-08-11.md).



---

## UCA-M5 closeout (2026-08-11)

| Item | Evidence |
| --- | --- |
| Dogfood Unity PR | MapPictureBridge + `MapPictureBridgeTests` — cites `checklists/pr-finish.md` |
| AAR | [`AAR-uca-m5-dogfood-2026-08-11.md`](AAR-uca-m5-dogfood-2026-08-11.md) |
| Skill status | `metadata.status: v1` · version `1.0.0` |
| Linear | DRG-123 / DRG-134 / epic DRG-124 close with PR path evidence |

**Program Done** when Linear project *Unity C# Architect Skill* is Completed and Notion design page is marked Complete.
