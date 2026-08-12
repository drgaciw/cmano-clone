# Review gates

**Skill:** `unity-csharp-architect` · **Parent:** [`../SKILL.md`](../SKILL.md) §6–8 (phases / finish / verdicts)  
**Program:** UCA-M4 · **Audience:** peer reviewers, humans, `/c-sharp-reviewer` on Unity/C# architecture PRs  
**Implements:** DRG-122 (UCA-M4 agent gates)

> **Law (one line):** Review against **seams** (snapshot wall, command path, assembly edges, zero-touch) — not “does it compile / green CI.”

**Complements:** [`pr-finish.md`](pr-finish.md) (agent **self-test** before Done). This file is the **reviewer** prompt set for APPROVED / CHANGES REQUIRED nuance. Architect finish language on this skill remains **PASS / FAIL / BLOCKED**.

**Related:** [`../references/presentation-boundary.md`](../references/presentation-boundary.md) · [`../references/headless-command-ui.md`](../references/headless-command-ui.md) · [`../references/asmdefs-and-layers.md`](../references/asmdefs-and-layers.md) · [`../references/mono-anti-patterns.md`](../references/mono-anti-patterns.md) · [`../references/editor-vs-runtime.md`](../references/editor-vs-runtime.md) · [`../references/performance-unity.md`](../references/performance-unity.md) · [`../references/testing-unity.md`](../references/testing-unity.md) · [`../references/aegis-unity-map.md`](../references/aegis-unity-map.md) · `/c-sharp-reviewer` · `/c-sharp-engineer`

---

## 0. ADR citation (non-negotiable)

| Topic | Cite | Do **not** cite |
| --- | --- | --- |
| **Presentation wall** (UI is client; snapshot/projection; map read-only) | **ADR-010** §2–3, **ADR-007**, **ADR-001** | **Git ADR-018** |
| Catalog / SQLite boundary | **ADR-006** | Opening DB from UI chrome |
| Platform Excel / PE write gate | **ADR-011** | — |
| Editor topology (in-client vs Scenario Lab shell) | **ADR-017** | Forked editor core |
| Sensor side-picture / datalink | **Git ADR-018** | As a presentation boundary |

UCA-M0 and some older notes mislabeled presentation as “ADR-018.” In git, **ADR-018** is **sensor side-picture / datalink** — **not** the presentation wall. Flag wrong citations as **BLOCKER** when they justify a seam decision; as **SHOULD-FIX** when they are PR-body noise only.

---

## 1. Role map (who uses what)

| Role | Artifact | Language | Job |
| --- | --- | --- | --- |
| **Implementer / architect agent** | [`pr-finish.md`](pr-finish.md) | **PASS / FAIL / BLOCKED** | Self-test seams before claiming Done |
| **`/c-sharp-reviewer`** | **This file** + language/Unity checklist | **APPROVED** / **CHANGES REQUIRED** (+ optional suggestions) | Peer audit of seams + C# hygiene |
| **Human reviewer** | This file (skim gates §3) | APPROVED / CHANGES REQUIRED | Same seam questions; escalate BLOCKED to program decision |
| **`unity-csharp-architect` finish report** | Parent [`SKILL.md`](../SKILL.md) §8 | **PASS / FAIL / BLOCKED** only | Architecture posture for the change |

| Do | Do not |
| --- | --- |
| Ask seam questions below even when CI is green | Rubber-stamp on compile + suite green alone |
| Map reviewer verdict ↔ architect keywords (§6) | Invent a third verdict dialect mid-PR |
| Escalate zero-touch / missing contract as BLOCKED | “Ship with a follow-up” on presentation wall breaks |
| Prefer headless proof over Play Mode screenshots | Accept Play Mode as sole architecture evidence |

---

## 2. How to run a review (short)

1. **Classify** the diff: presentation / command / projection / assembly / Editor / perf / tests.  
2. **Open** the matching reference(s) from the Related list — do not re-host ADRs.  
3. **Walk gates §3** — every applicable question gets a Yes / No / N/A with a path or line.  
4. **Assign severity** (§4) per finding.  
5. **Comment** with templates (§5) when helpful.  
6. **Verdict** (§6). One primary: APPROVED or CHANGES REQUIRED (or hold for decision → architect **BLOCKED**).  
7. Optional: run soft `rg` patterns (§7) — **informational only**.

---

## 3. Gate prompts (questions the reviewer asks)

Answer each applicable gate. A single **No** on a BLOCKER gate → **CHANGES REQUIRED** (architect **FAIL**). Ambiguity that needs program decision → hold merge; architect **BLOCKED**.

### G1 — Presentation wall (snapshot / projection)

> **Law:** MonoBehaviours and UI hosts read **projections / snapshots only** — never live ECS chunks, session guts, or write handles (ADR-010 §2–3, ADR-001).

- [ ] Does any MB / panel host hold or cache live sim entities, `EntityQuery`, session, or orchestrator internals?
- [ ] Do new UI fields extend a **projection / bridge contract** (`*Bridge.Build`, `*Projection`, `IReadOnly*`) instead of piercing the wall?
- [ ] Are bound rows treated as **immutable / `IReadOnly*`** for the bind window?
- [ ] Is frame work limited to interpolate / camera / selection / USS — **not** fake sim steps in `Update`?
- [ ] PR cites **ADR-010 / 007 / 001** for this wall — **not** Git ADR-018?

**Refs:** `presentation-boundary.md` · `aegis-unity-map.md` · `docs/engineering/c2-projection-layer.md`

### G2 — Command path (enqueue façade vs `IOrderSink` from view)

> **Law:** Player intent → **command / enqueue façade** → queue → drain → `IOrderSink.ApplyOrder`. Views do **not** call `IOrderSink` or append authority.

- [ ] Does write intent go through `C2PlayerCommandBridge.TryIssue` / `HumanController.Enqueue` / `DelegationBridge.TryEnqueueHumanOrder` (or an approved façade)?
- [ ] Is there **no** MB / UI Toolkit handler calling `IOrderSink.ApplyOrder` directly?
- [ ] Is there **no** panel-side mutation of world truth (alive, contacts, magazines, poses as authority)?
- [ ] Does CLI/MCP/Unity share the same command model for any new authoritative action (or is a waiver + ADR/PR note present)?

**Refs:** `headless-command-ui.md` · `mono-anti-patterns.md` · `aegis-unity-map.md`

### G3 — DecisionLog / map read-only (ADR-007)

> **Law:** Map and C2 chrome **project** the log; they **never** append.

- [ ] Is there **zero** `DecisionLog.Append` (or equivalent order-log write) from UI, binder, or map host code?
- [ ] Does map UI use `MapPictureBridge.Build` → `MapPictureProjection` / `MapSymbolEntry` bind?
- [ ] Is selection on `C2PresentationController` (presentation-only)?
- [ ] Catalog / SQLite: no open from presentation (ADR-006)?

**Refs:** ADR-007 · ADR-006 · `presentation-boundary.md`

### G4 — DelegationBridge zero-touch

> **Law:** `DelegationBridge` **hotpath** is frozen through Release v1.

- [ ] Does the diff touch `DelegationBridge` tick / hotpath methods?
- [ ] If yes: is there an **explicit program waiver**? (Else **BLOCKER**.)
- [ ] Is new behavior in projections, **new** adapter types, hosts, or command façades instead?

**Refs:** `SKILL.md` §1.2 · `asmdefs-and-layers.md`

### G5 — Asmdef edges / `UnityEngine` in headless

> **Law:** Headless `src/ProjectAegis.{Data,Sim,Delegation,Delegation.UnityAdapter}` stay **engine-free**. New assemblies need an **edge list**.

- [ ] Any new/changed asmdef includes a PR **edge list**?
- [ ] No `UnityEngine` / UnityEditor in headless / UnityAdapter projects?
- [ ] No UI → sim-internals edge; no circular asmdefs?
- [ ] Editor code in Editor asmdef (`includePlatforms: ["Editor"]`); Runtime never references Editor?
- [ ] Plugin DLLs remain **netstandard2.1** publish outputs?

**Refs:** `asmdefs-and-layers.md` · ADR-001 · ADR-010

### G6 — MonoBehaviour hygiene

> **Law:** MBs are **thin scene shells** — lifecycle, bind, intent capture.

- [ ] Host is thin — not sim + UI + I/O + selection + orders in one type?
- [ ] No production `Find*` / `FindObjectOfType` / `FindObjectsByType` / `Resources.Load` for services?
- [ ] Components cached in `Awake` from serialize/inject?
- [ ] Unity objects checked with `== null` (fake-null)?
- [ ] `[SerializeField] private`; no public mutable gameplay authority fields?
- [ ] No new scene-only mutable singleton for game state without written waiver?

**Refs:** `mono-anti-patterns.md` · `/c-sharp-engineer`

### G7 — Editor vs Runtime

> **Law:** Editor tools must **not** own live sim / `DecisionLog` authority.

- [ ] Change classified: Editor chrome vs headless authoring vs Runtime execution?
- [ ] Editor does **not** append `DecisionLog`, step sim, or call `IOrderSink.ApplyOrder` as “preview authority”?
- [ ] No Editor → Runtime presentation shortcut that skips command / projection contracts?
- [ ] Authoring vs execution split honored?

**Refs:** `editor-vs-runtime.md` · ADR-010 / 007 / 001 · ADR-017

### G8 — Authoring headless twin

> **Law:** Prefer **headless authoring** before `EditorWindow` chrome (ADR-010).

- [ ] For new Editor logic: headless twin / presenter / CLI verb exists (or absence justified)?
- [ ] Scenario/package truth still in **Data** contracts?
- [ ] Window code stays chrome + bind?

**Refs:** `editor-vs-runtime.md` · `headless-command-ui.md` · ADR-011

### G9 — Hot-path allocations / pooling

> **Law:** Projection rebuild on **sim-tick / dirty** boundary; frame path is interpolate + pool apply.

- [ ] No new per-frame LINQ / closures / string concat / `new List<>` on host hot paths?
- [ ] Map/list UI reuses pools (`MapSymbolPool` style)?
- [ ] Req 20 rich C2 bind still under budget when bind surface grew?
- [ ] Intentional one-shot allocs called out in PR body?

**Refs:** `performance-unity.md` · `mono-anti-patterns.md`

### G10 — Test placement (headless first)

> **Law:** Prove pure logic with **`dotnet test`** first; Play Mode **last**.

- [ ] New pure / adapter logic has headless tests under `src/*Tests` where practical?
- [ ] Play Mode not used as the **only** architecture proof?
- [ ] If headless is impossible: PR note states why and residual risk?

**Refs:** `testing-unity.md` · `headless-command-ui.md`

### G11 — DI / SOLID / immutability (`/c-sharp-engineer`)

- [ ] Dependencies injected at composition root — no new mutable game-state singletons?
- [ ] Presenters/bridges depend on abstractions (`ISimWorldSnapshot`, command façade, DTOs)?
- [ ] SRP: view binds/forwards; presenter/bridge owns logic?
- [ ] Shared projection rows not mutated by views?

**Refs:** `SKILL.md` §6 Phase 2 · `mono-anti-patterns.md`

### G12 — Async safety (if present)

- [ ] No `async void` except UI event handlers?
- [ ] Long UI work cancels on panel close / host disable?
- [ ] After await: re-bind from **fresh** projection?
- [ ] No `await` inside authoritative sim tick paths?

**Refs:** `headless-command-ui.md` · `/c-sharp-reviewer`

### G13 — ADR citation correctness

| Claim | Required citation |
| --- | --- |
| Presentation / UI client / snapshot wall | **ADR-010**, **ADR-001**, **ADR-007** |
| Map / C2 read-only projection | **ADR-007** (+ 010/001 as needed) |
| Catalog / no SQLite from UI | **ADR-006** |
| Platform Editor Excel | **ADR-011** |
| Shared core / Scenario Lab topology | **ADR-017** |
| Sensor datalink / side-picture | **Git ADR-018** only for that domain |

- [ ] Presentation discussion never labeled **ADR-018**?
- [ ] PR body links this skill + applicable ADRs?

---

## 4. Severity table

| Severity | Meaning | Merge impact | Typical examples |
| --- | --- | --- | --- |
| **BLOCKER** | Seam / ADR / zero-touch / correctness break | **Must fix** | MB → `IOrderSink`; `DecisionLog` from UI; live ECS on host; `DelegationBridge` hotpath without waiver; `UnityEngine` in headless; Editor play authority; ADR-018 used to justify presentation |
| **SHOULD-FIX** | Standards / missing proof | **Must fix** unless waived | Production `Find*`; god-MB; missing edge list; Play Mode as sole test; unexplained hot-path alloc; missing headless twin |
| **NIT** | Style / polish | Optional | Naming, docs, non-blocking structure |

Any **BLOCKER** ⇒ **CHANGES REQUIRED**. Unwaived **SHOULD-FIX** ⇒ **CHANGES REQUIRED**. **NIT**-only may still **APPROVED**.

---

## 5. Suggested review comment templates

### BLOCKER

```text
[BLOCKER · G2] View calls IOrderSink.ApplyOrder. Route intent through C2PlayerCommandBridge / enqueue façade (ADR-010). See review-gates G2.
```

```text
[BLOCKER · G3] DecisionLog write from map/UI host. Map is read-only projection (ADR-007).
```

```text
[BLOCKER · G1] Host caches live session/ECS. Bind IReadOnly* from *Bridge.Build / *Projection (ADR-010/001).
```

```text
[BLOCKER · G4] DelegationBridge hotpath edit without program waiver (zero-touch through Release v1).
```

```text
[BLOCKER · G5] UnityEngine reference in headless/UnityAdapter. Keep src/ engine-free.
```

```text
[BLOCKER · G13] Presentation justified as “ADR-018”. Cite ADR-010/007/001. Git ADR-018 is datalink.
```

### SHOULD-FIX

```text
[SHOULD-FIX · G6] Production FindObjectOfType for service location. Inject at composition root.
```

```text
[SHOULD-FIX · G10] Logic-only change proven only in Play Mode. Add headless/dotnet test first.
```

```text
[SHOULD-FIX · G5] New/changed asmdef missing edge list. See asmdefs-and-layers.md.
```

```text
[SHOULD-FIX · G9] Per-frame LINQ/alloc on bind path. Pool / dirty-flag / reuse.
```

```text
[SHOULD-FIX · G8] EditorWindow owns business rules with no headless authoring twin.
```

### NIT

```text
[NIT · G11] Prefer IReadOnlyList bind surface for immutability clarity — optional.
```

```text
[NIT · G13] PR body should link unity-csharp-architect skill + ADR-010.
```

---

## 6. Verdict mapping

| Reviewer | Architect finish ([`pr-finish.md`](pr-finish.md) / `SKILL.md` §8) | When |
| --- | --- | --- |
| **APPROVED** | **PASS** | No BLOCKER; no unwaived SHOULD-FIX |
| **APPROVED** (with suggestions) | **PASS** | NIT-only (or waived SHOULD-FIX) |
| **CHANGES REQUIRED** | **FAIL** | Any BLOCKER or unwaived SHOULD-FIX on skill law |
| Hold / need decision | **BLOCKED** | Zero-touch conflict, absent contract, undelivered dependency |

**Rules:**

1. Do **not** APPROVE with an open BLOCKER.  
2. Architect finish language remains **PASS / FAIL / BLOCKED**.  
3. **BLOCKED** is not a soft FAIL — stop and get a decision.  
4. Green product suites alone never imply APPROVED for architecture PRs.

### Reviewer report skeleton (optional)

```markdown
## Architecture review (UCA review-gates)

### Scope
- Paths: …
- Classification: presentation | command | projection | assembly | Editor | perf | tests

### Gate results
- G1–G13: PASS | FAIL | N/A (list as needed)

### Findings
- [BLOCKER] …
- [SHOULD-FIX] …
- [NIT] …

### Soft CI (optional)
- Ran / skipped soft-ci-rg patterns: …

### Verdict
- Reviewer: APPROVED | CHANGES REQUIRED
- Architect mapping: PASS | FAIL | BLOCKED
```

---

## 7. Soft CI (optional `rg` patterns)

Hard lint for these gates is **optional** in UCA-M4.

| Policy | Detail |
| --- | --- |
| **Soft only** | Patterns in [`soft-ci-rg.md`](soft-ci-rg.md) are **advisory** |
| **Must not block product suite floors** | Do **not** fail Release / product CI floors solely on architecture soft-lint hits |
| **Not a substitute for review** | Green soft CI ≠ APPROVED; red soft CI ≠ automatic FAIL without judgment |
| **False positives** | Expected near tests, fakes, documented host composition — triage with gate context |

**See:** [`soft-ci-rg.md`](soft-ci-rg.md).

---

## 8. See also

| Doc | Use |
| --- | --- |
| [`pr-finish.md`](pr-finish.md) | Agent self-test (PASS/FAIL/BLOCKED) |
| [`soft-ci-rg.md`](soft-ci-rg.md) | Optional advisory `rg` patterns |
| [`../SKILL.md`](../SKILL.md) | Parent doctrine; §6–8 |
| [`../references/presentation-boundary.md`](../references/presentation-boundary.md) | Snapshot wall (**not** Git ADR-018) |
| [`../references/headless-command-ui.md`](../references/headless-command-ui.md) | Intent → command → engine |
| [`../references/asmdefs-and-layers.md`](../references/asmdefs-and-layers.md) | Edges, engine-free headless |
| [`../references/mono-anti-patterns.md`](../references/mono-anti-patterns.md) | Thin hosts; Find/alloc/null |
| [`../references/editor-vs-runtime.md`](../references/editor-vs-runtime.md) | EditorWindow vs Runtime |
| [`../references/performance-unity.md`](../references/performance-unity.md) | Budgets, pooling |
| [`../references/testing-unity.md`](../references/testing-unity.md) | Headless → Play Mode |
| [`../references/aegis-unity-map.md`](../references/aegis-unity-map.md) | Path index |
| `docs/architecture/adr-010-headless-first-command-driven-ui.md` | UI is a client |
| `docs/architecture/adr-007-c2-map-presentation.md` | Map presentation |
| `docs/architecture/adr-001-sim-assembly-boundary.md` | Snapshot in / Order out |
| `docs/architecture/adr-006-data-layer-boundary.md` | No SQLite from presentation |
| `docs/architecture/adr-018-sensor-side-picture-datalink.md` | **Datalink** — not presentation |
| `/c-sharp-reviewer` · `/c-sharp-engineer` | Review companion · implement under contract |

**UCA-M4 note:** Reviewer gates only. Soft lint must never become a hard product-suite floor.
