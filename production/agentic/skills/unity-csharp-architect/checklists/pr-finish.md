# PR finish checklist — Unity / C# architecture

**Skill:** `unity-csharp-architect` · **Parent:** [`../SKILL.md`](../SKILL.md) §7 (finish) · §8 (verdict)  
**Program:** UCA-M4 · **Lane:** D (gates) · **Implements:** DRG-122 / DRG-134 (M4 portion)  
**Audience:** agents claiming **Done** on UnityAdapter, presentation, MonoBehaviour, asmdef, Editor, or C2/UI work

> **Law (one line):** UI is a **client** — read only via snapshot / projection / `*Bridge`; write only via command façade; thin hosts; headless tests first; presentation = **ADR-010 / 007 / 001** — never Git **ADR-018**.

**Related:** [`review-gates.md`](review-gates.md) · [`soft-ci-rg.md`](soft-ci-rg.md) · [`../references/presentation-boundary.md`](../references/presentation-boundary.md) · [`../references/headless-command-ui.md`](../references/headless-command-ui.md) · [`../references/asmdefs-and-layers.md`](../references/asmdefs-and-layers.md) · [`../references/mono-anti-patterns.md`](../references/mono-anti-patterns.md) · [`../references/performance-unity.md`](../references/performance-unity.md) · [`../references/testing-unity.md`](../references/testing-unity.md) · [`../references/editor-vs-runtime.md`](../references/editor-vs-runtime.md) · [`../references/aegis-unity-map.md`](../references/aegis-unity-map.md)

**Authority:** When this file exists, it **replaces** the temporary checklist in [`SKILL.md` §7](../SKILL.md#7-finish-checklist). Do **not** invent a parallel list. Run this checklist before claiming Done.

**Verdict keywords (from SKILL §8):** exactly one of **PASS** / **FAIL** / **BLOCKED**.

---

## 1. When to run

Run **every** item that applies to the diff before:

- marking the issue / PR Done  
- handing to `/c-sharp-reviewer` for merge path  
- stating architecture finish language (**PASS** / **FAIL** / **BLOCKED**)

| Change class | Minimum sections |
| --- | --- |
| Presentation / bind / map / C2 chrome | §2.1, §2.2, §2.4, §2.6, §2.7 |
| New or changed command / order path | §2.1–§2.2, §2.6, §2.7 |
| asmdef / assembly / plugin surface | §2.3, §2.7 |
| MonoBehaviour / host / DI / hot path | §2.4, §2.6, §2.7 |
| EditorWindow / authoring | §2.5, §2.6, §2.7 |
| Catalog / SQLite touch | §2.1 (+ ADR-006), §2.7 |

N/A items: mark **N/A** with one-line reason (do not delete the gate).

---

## 2. Full checklist

### 2.1 Presentation / snapshot (ADR-010 §2–3, ADR-007, ADR-001)

- [ ] No MonoBehaviour reads live sim chunks, ECS queries, or caches session / orchestrator internals  
- [ ] Reads go through **snapshot** / **projection** / `*Bridge` only — not sim internals  
- [ ] Map / C2 stay **read-only** re: `DecisionLog` and world truth (ADR-007) — UI never appends or mutates authority  
- [ ] Selection lives on `C2PresentationController` (presentation-only) — not as sim input authority  
- [ ] Bind window uses `IReadOnly*` / immutable projection rows (or equivalent immutable DTOs) — views do not mutate shared buffers  
- [ ] New panel fields extend the **projection / bridge contract** — no wall pierce  
- [ ] Interpolation / camera / layout are presentation-only — no fake sim steps in `Update`  
- [ ] Catalog / SQLite: presentation never opens DB directly (**ADR-006** if catalog path touched)

### 2.2 Command path (ADR-010)

- [ ] New UI path that mutates authority issues a **command** via `C2PlayerCommandBridge` / `HumanController.Enqueue` / approved player-command façade  
- [ ] **Not** MB → `IOrderSink.ApplyOrder` (or equivalent direct sink apply from view / binder)  
- [ ] Map UI never writes `DecisionLog` / order log  
- [ ] Presentation-only actions (camera, basemap, panel layout, selection focus) are **not** on the command bus and do not enter replay hashes  
- [ ] If the path issues **no** command: approved exception is **documented** in PR (why pure presentation; residual risk)  
- [ ] Authoritative actions remain driveable from CLI / MCP / headless test where the domain already has parity (no Unity-only authority)

### 2.3 Assemblies / zero-touch

- [ ] No `DelegationBridge` **hotpath** edits unless program waiver explicitly reopens that invariant  
- [ ] Change lands in an **existing** allowed assembly when possible  
- [ ] If assemblies / asmdefs changed: **edge list** in PR (who depends on whom; platforms; why not existing)  
- [ ] No `UnityEngine` in `ProjectAegis.Data` / `Sim` / `Delegation` / `Delegation.UnityAdapter`  
- [ ] No UI → sim-internals edge; no circular asmdefs  
- [ ] Editor code not referenced from player / Runtime assemblies

### 2.4 MonoBehaviour / DI / alloc (c-sharp-engineer)

- [ ] Host is a **thin view**: lifecycle + bind + intent only — not a god-MB  
- [ ] No production `Find*` / `FindObjectOfType` / `FindObjectsByType` / `Resources.Load`  
- [ ] Components cached in `Awake` from serialize / inject — not looked up on hot paths  
- [ ] Hot path: no unexplained per-frame LINQ / closures / string concat / `new` lists  
- [ ] Unity objects checked with Unity `== null` (fake-null aware) — not C# `is null` / `?.` alone on `UnityEngine.Object`  
- [ ] DI at composition root; no new mutable game-state singletons (scene host singleton only with written waiver)  
- [ ] `[SerializeField] private` for inspector refs; no public authority fields  
- [ ] Deeper budgets checked against [`performance-unity.md`](../references/performance-unity.md) when bind / map / pool volume changed

### 2.5 Editor vs runtime

- [ ] Editor-only code lives in Editor asmdef with `includePlatforms: ["Editor"]`  
- [ ] Authoring logic prefers **Data Authoring** / **UnityAdapter Authoring** (+ CLI) **headless first** before `EditorWindow` chrome  
- [ ] No Editor `DecisionLog` write or live sim step as “preview authority”  
- [ ] No Editor `IOrderSink.ApplyOrder` / enqueue bypass that fakes play semantics  
- [ ] No Editor → Runtime presentation shortcut that skips command / projection contracts  
- [ ] Authoring vs execution split honored (document / session vs `BeginExecution` + tick)

### 2.6 Testing

- [ ] Pure logic covered by **headless** / EditMode where practical — **`dotnet test` first**  
- [ ] Play Mode is **last-mile** only (host lifecycle, UI Toolkit smoke) — not sole architecture proof  
- [ ] Tests map to the correct project:  
  - `src/ProjectAegis.Delegation.UnityAdapter.Tests` (bridges, presentation, façades)  
  - `src/ProjectAegis.Data.Tests` / Delegation / Sim tests as appropriate  
  - `unity/ProjectAegis/Assets/Tests` (EditMode / Play Mode host only)  
- [ ] New bridge / presenter has tests next to existing Bridge / Presentation folders when practical  
- [ ] Command-path tests use façade / enqueue — not UI→sink hacks  
- [ ] Perf-sensitive bind changes update or re-run `C2PanelPerfBenchTests` (or document N/A)  
- [ ] PR lists test commands / filters run

### 2.7 ADR / PR hygiene

- [ ] ADR citations for presentation: **ADR-010 / 001 / 007** — **not** Git **ADR-018** (datalink / sensor side-picture)  
- [ ] **ADR-006** cited if catalog / SQLite path touched  
- [ ] **ADR-011** / **ADR-017** cited if PE Excel / editor topology applies  
- [ ] PR body links: this skill + relevant ADR path(s) + **this checklist**  
- [ ] If adapter **public surface** changed: Plugin DLL refresh documented (`netstandard2.1` publish + copy script) — **do not** commit `net8.0` outputs into `Assets/Plugins`  
- [ ] No new scene-only singleton / Odin-on-core / structural exception without waiver or ADR (see §4)

---

## 3. Verdict + paste template

Report **exactly one** primary verdict:

| Keyword | Meaning |
| --- | --- |
| **PASS** | Seams, ADRs, tests, and this checklist satisfied; ready for review merge path |
| **FAIL** | Violation of presentation / command / assembly laws or checklist; must fix before merge |
| **BLOCKED** | Cannot proceed — missing decision, zero-touch conflict, absent projection contract, or dependency on undelivered milestone / ref |

Reviewer-style nuance (APPROVED / CHANGES REQUIRED) may come from `/c-sharp-reviewer`; **architect finish language** remains **PASS** / **FAIL** / **BLOCKED**.

### Paste into PR / Linear

```markdown
## unity-csharp-architect — PR finish (UCA-M4)

**Checklist:** `production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md`
**Skill:** unity-csharp-architect
**ADRs:** ADR-010, ADR-007, ADR-001 (add ADR-006 / 011 / 017 if applicable — never ADR-018 for presentation)

**Verdict:** PASS | FAIL | BLOCKED

**Evidence:**
- Presentation reads: snapshot / *Bridge / IReadOnly* only — …
- Command path: C2PlayerCommandBridge / HumanController.Enqueue / N/A (reason) — …
- Assemblies: edge list N/A | linked; DelegationBridge hotpath untouched | waived — …
- MB / DI / alloc: thin host; no Find*/Resources; hot path clean — …
- Editor: N/A | Editor asmdef + headless authoring first — …
- Tests: `dotnet test …` (filters); Play Mode: none | smoke only — …
- Plugins: N/A | netstandard2.1 copy documented (not committed net8.0) — …

**Waivers / N/A:** (none | list with PR note / ADR pointer)
```

**PASS** requires every applicable box checked (or N/A with reason) and no open FAIL items.  
**FAIL** if any hard gate is violated.  
**BLOCKED** if work cannot finish without a decision, waiver, or missing contract — stop and escalate; do not claim Done.

---

## 4. Exceptions / waivers

| Situation | Required |
| --- | --- |
| No command issued (pure presentation) | PR note: why not authority; residual risk |
| Headless proof impossible | PR note + isolate Unity-only surface; unit-test any extractable core ([`headless-command-ui.md`](../references/headless-command-ui.md) §8) |
| `DelegationBridge` hotpath touch | Explicit program waiver reopening zero-touch; otherwise **BLOCKED** |
| New scene-only singleton / mutable UI authority | Written waiver; prefer composition root |
| New asmdef | Edge list in PR (template in [`asmdefs-and-layers.md`](../references/asmdefs-and-layers.md)) |
| Structural exception (scene authority, Odin on core, CLI parity break) | New / amended ADR + architecture review — not a silent skip |
| Adapter public API change | Document plugin publish/copy; do not commit `net8.0` plugin DLLs |

Waivers do **not** weaken ADR laws. Prefer fixing the seam over documenting a permanent hole.

---

## 5. See also

| Doc | Use |
| --- | --- |
| [`../SKILL.md`](../SKILL.md) §7–§8 | Parent finish authority + verdict keywords |
| [`review-gates.md`](review-gates.md) | Human / peer review prompts |
| [`soft-ci-rg.md`](soft-ci-rg.md) | Optional local `rg` patterns — **not** a hard CI gate |
| [`../references/presentation-boundary.md`](../references/presentation-boundary.md) | Snapshot wall; DecisionLog; immutability |
| [`../references/headless-command-ui.md`](../references/headless-command-ui.md) | Intent → command → engine; waiver path |
| [`../references/asmdefs-and-layers.md`](../references/asmdefs-and-layers.md) | Allowed edges; plugin netstandard2.1 |
| [`../references/mono-anti-patterns.md`](../references/mono-anti-patterns.md) | Thin host; Find/Resources; Unity null |
| [`../references/performance-unity.md`](../references/performance-unity.md) | Frame vs sim budgets; Req 20 bind |
| [`../references/testing-unity.md`](../references/testing-unity.md) | Placement matrix; headless first |
| [`../references/editor-vs-runtime.md`](../references/editor-vs-runtime.md) | Editor asmdef; no Editor authority |
| [`../references/aegis-unity-map.md`](../references/aegis-unity-map.md) | “Where does X live?” path index |
| `docs/architecture/adr-010-headless-first-command-driven-ui.md` | UI is a client |
| `docs/architecture/adr-007-c2-map-presentation.md` | Map read-only projection |
| `docs/architecture/adr-001-sim-assembly-boundary.md` | Snapshot in / Order out |
| `docs/architecture/adr-006-data-layer-boundary.md` | No SQLite from presentation |
| `docs/architecture/adr-018-sensor-side-picture-datalink.md` | **Not** presentation — do not cite for UI seams |
| `docs/engineering/c2-projection-layer.md` | Projection → Binder → State |
| `src/ProjectAegis.Delegation.UnityAdapter/` | Bridges, `C2PresentationController`, façades |
| `/c-sharp-engineer` · `/c-sharp-reviewer` | Implement under contract · review gates |

**UCA-M4 note:** This file is the authoritative agent finish gate for Unity/C# architecture work. Soft CI patterns in [`soft-ci-rg.md`](soft-ci-rg.md) are optional local aids — checklist-first; hard lint is not required by this program.
