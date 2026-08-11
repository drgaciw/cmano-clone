---
name: unity-csharp-architect
description: >
  Senior Unity C# architecture for Project Aegis (cmano-clone): presentation
  boundary (ADR-010 §2–3, ADR-007, ADR-001), headless/command-driven UI
  (ADR-010), assembly definitions, ScriptableObject data ownership,
  MonoBehaviour anti-patterns, editor vs runtime split, performance budgets,
  and agent finish self-tests. Use whenever writing or reviewing Unity C#,
  MonoBehaviours, UnityAdapter presenters/bridges, EditorWindows, C2/UI chrome,
  assembly graphs, snapshots, or projection bind paths. Triggers on
  "UnityAdapter", "MonoBehaviour", "asmdef", "snapshot", "presentation", "C2",
  "EditorWindow", "MapPicture", "ISimWorldSnapshot", "C# architecture".
metadata:
  short-description: "Aegis Unity/C# architecture: ADR-010/001/007, asmdefs, finish gates"
  status: m1-doctrine  # UCA-M1 — not final v1 until UCA-M5 dogfood
  version: 0.2.0-m1
  path-to-v1: >
    Final status `v1` lands only at UCA-M5 after one real Unity PR dogfoods
    checklists/pr-finish.md, AAR is filed, and ROADMAP exit criteria are met.
    See ROADMAP.md (UCA-M2…M5) and post-s97-uca-agent-capability-train note.
---

# Unity C# Architect (Project Aegis)

> **Status:** **UCA-M1 doctrine** (`metadata.status: m1-doctrine`). Not final **v1** until **UCA-M5** dogfood + closeout.
> Roadmap: [`ROADMAP.md`](ROADMAP.md). Design wiki: Notion *unity-csharp-architect Skill — Design & Sprint Roadmap*.
> Do not re-host full ADRs here — cite paths only.

Teach agents to write **architecturally correct** Unity C# for Aegis — not just compiling MonoBehaviours. Simulation truth lives in headless .NET; Unity is a **presentation shell** over read-only projections and command seams.

| Use this skill | Do **not** use this skill |
| --- | --- |
| UnityAdapter / Bridge / Presentation work | Pure sim-core / gauntlet / catalog with **no** Unity surface |
| MonoBehaviour, EditorWindow, C2 chrome | Product feature scoping (use product backlog issues) |
| asmdef / assembly graph changes | Sensor-side picture / datalink merge (**Git ADR-018** — sim domain, not presentation) |
| Snapshot / projection / command UI paths | Nordic-Baltic product expansion (REQ-NB-*) |

**ADR correction (non-negotiable):** presentation boundary = **ADR-010 §2–3**, **ADR-007**, **ADR-001**. **Git ADR-018** = sensor-side-picture-datalink. Never call presentation “ADR-018”.

---

## 0. Load triggers

Load this skill when the task or diff mentions any of:

`UnityAdapter` · `MonoBehaviour` · `asmdef` · `snapshot` · `presentation` · `C2` · `EditorWindow` · `MapPicture` · `ISimWorldSnapshot` · `IOrderSink` · `DelegationBridge` · `Bridge/` · `Presentation/`

---

## 1. Two worlds (non-negotiable)

| | **Simulation** | **Presentation** |
| --- | --- | --- |
| **Owns** | Authoritative world truth, orders, order log, determinism | Snapshots → projections, interpolation, input capture → commands, selection/camera/layout |
| **Clock** | Sim / fixed step | Frame rate |
| **Assemblies (examples)** | `ProjectAegis.Sim`, `ProjectAegis.Delegation`, `ProjectAegis.Data` | `ProjectAegis.Delegation.UnityAdapter` (Bridge, Presentation); Unity player/editor hosts |
| **Contracts in** | `Order`, order-log entries, policy/ROE | `ISimWorldSnapshot` (read), `IOrderSink` / commands (write intent), projection DTOs |
| **Test without Editor?** | **Yes — prefer this** | Prefer headless presenters/bridges first (`dotnet test`) |

**Law:** MonoBehaviours must not hold live write access to sim internals. UI is a **client**, not an authority (ADR-010 §2).

### 1.1 ADR laws (cite, do not restate full ADRs)

| ADR | Path | Law for agents |
| --- | --- | --- |
| **ADR-010** | `docs/architecture/adr-010-headless-first-command-driven-ui.md` | Headless core authoritative; UI renders projections + submits commands; assembly seams via DTOs/adapters |
| **ADR-001** | `docs/architecture/adr-001-sim-assembly-boundary.md` | Delegation consumes **`ISimWorldSnapshot`** and emits **`Order` only** |
| **ADR-007** | `docs/architecture/adr-007-c2-map-presentation.md` | Map is **read-only** projection (`MapPictureProjection` / `MapSymbolEntry`); never writes sim/log |
| **Git ADR-018** | `docs/architecture/adr-018-sensor-side-picture-datalink.md` | **Not** presentation — sensor-side picture / datalink sharing (sim). Do not cite for UI seams |

Engineering companion: `docs/engineering/c2-projection-layer.md`.

### 1.2 Real repo anchors (paths only)

| Anchor | Role |
| --- | --- |
| `src/ProjectAegis.Delegation.UnityAdapter/` | Adapter seam — **no `UnityEngine`**; `dotnet test` host |
| `…/Bridge/` | `ISimWorldSnapshot`, `IOrderSink`, `*Bridge`, tick facade |
| `…/Presentation/` | `C2PresentationController`, selection, graph highlights (presentation-only) |
| `MapPictureBridge.Build` | `Build(snapshot, registry, log, layoutSeed)` → map symbols |
| `MapPictureProjection` | Pure projection in `ProjectAegis.Delegation/Projection/` |
| `DelegationBridge` | **Zero-touch hotpath** through Release v1 — no new hotpath logic |
| `docs/engineering/c2-projection-layer.md` | Projection → Binder → State layering |

---

## 2. Open references (load on demand)

Paths relative to this skill directory. **UCA-M1** owns doctrine + first two refs; later milestones fill the rest. Until a file exists, use §3–§7 and the cited ADRs as temporary source of truth.

| Reference | When to open |
| --- | --- |
| `references/presentation-boundary.md` | Any MB / UI that needs sim state; snapshot/projection seams |
| `references/headless-command-ui.md` | Player intent, presenters, command bus, ADR-010 flows |
| `references/asmdefs-and-layers.md` | New assemblies or dependency edges |
| `references/scriptableobjects-data.md` | Designer data vs runtime state ownership |
| `references/mono-anti-patterns.md` | Review or refactor of existing MonoBehaviours |
| `references/editor-vs-runtime.md` | EditorWindow / authoring tools vs player loop |
| `references/performance-unity.md` | Hot paths, GC, pooling, frame budgets |
| `references/testing-unity.md` | Where tests live (EditMode / headless / PlayMode) |
| `references/aegis-unity-map.md` | “Where does X live in this repo?” |
| `checklists/pr-finish.md` | Before claiming Done |
| `checklists/review-gates.md` | Human / peer review prompts |

---

## 3. Presentation boundary (ADR-010 §2–3, ADR-007, ADR-001)

Distilled laws — full text stays in ADRs + `references/presentation-boundary.md`.

1. Presentation **reads projections / snapshots only** — never live ECS chunks or session internals on MonoBehaviours.
2. **Write path** = commands / orders via approved sinks (`IOrderSink`, player-command bridges) — not field mutation.
3. **Interpolation / camera / selection** are presentation-only; never “fake” sim steps in `Update`.
4. Need a new field on a panel? Extend the **projection contract** (or bridge that calls it) — do not reach through the wall.
5. Map path: `MapPictureBridge.Build` → `MapPictureProjection` → bind; map UI **never** appends to `DecisionLog` or mutates world truth (ADR-007).
6. Assembly boundary: Delegation sees **`ISimWorldSnapshot` in**, **`Order` out** (ADR-001).
7. **`DelegationBridge` zero-touch hotpath** — new behavior goes in core projections, new adapter types, or presentation hosts — not the bridge hotpath.

| Good | Bad |
| --- | --- |
| Bind `IC2PresentationFeed` / `*Bridge` outputs | Cache live sim entities on a MB |
| `C2PresentationController` for selection | Selection as authoritative sim input |
| Extend `*Projection` + headless test | `FindObjectOfType` into sim session from UI |
| Command via `C2PlayerCommandBridge` / order sink | Direct order-log write from panel code |

---

## 4. Command-driven UI (ADR-010 distilled)

1. Player intent → **command** → engine (testable outside Unity).
2. Presenters prefer **engine-free** logic under `ProjectAegis.Delegation.UnityAdapter` (Bridge / Presentation) — no `UnityEngine` in that project.
3. Prove headless first (`dotnet test` on adapter/delegation tests) before Play Mode.
4. Document exceptions (new ADR or explicit PR waiver).
5. UI validation state is **not** export/play authority.

---

## 5. Assemblies, MonoBehaviours, data

### 5.1 Assemblies & layers

1. Prefer existing **allowed** assemblies (`ProjectAegis.*` namespaces).
2. New asmdefs require an **edge list** in the PR (who depends on whom).
3. Forbidden: UI → sim internals; Editor → runtime presentation shortcuts that skip commands; circular asmdefs; `UnityEngine` in headless core / UnityAdapter.
4. Layering for C2 panels: **Projection → Binder → State** (see `c2-projection-layer.md`), then thin Unity host.

### 5.2 MonoBehaviour hygiene

| Prefer | Avoid |
| --- | --- |
| Thin view + presenter / projection bind | God MonoBehaviour |
| Inject at composition root | `FindObjectOfType` / `Find` sprawl |
| Event / command for intent | Direct sim mutation from `Update` |
| Pooling; no alloc on hot paths | Per-frame LINQ / string concat |
| `[SerializeField] private` | Public mutable gameplay fields as authority |

### 5.3 ScriptableObjects

SO assets hold **designer/config data**, not live authoritative run state. Runtime authority stays in sim + order log; SO is input to validation/load, not a second world model.

---

## 6. Operating phases (architect → implement → review)

Agents run **at least** these phases. Architecture doctrine lives here; product C# implementation is **not** owned by this skill’s authors mid-flight — hand off.

### Phase 1 — Architect (this skill)

1. Classify change: presentation-only / command / projection / assembly / editor.
2. Open needed references (§2); cite ADR-010 / 001 / 007 as applicable (**not** ADR-018 for UI).
3. Sketch seam: snapshot → projection/bridge → binder → host; write path = command only.
4. Note zero-touch: if change would edit `DelegationBridge` hotpath → **BLOCKED** unless program explicitly reopens that invariant.
5. Produce a short contract: types, assemblies, tests (headless first).

**Handoff →** `/c-sharp-engineer` (or `team-csharp` implementation lane) with the contract.

### Phase 2 — Implement (`/c-sharp-engineer`)

Implement against the contract. **c-sharp-engineer concerns (always):**

| Concern | Expectation |
| --- | --- |
| **Layering** | Plain testable C# for logic; MB only at lifecycle / scene boundary |
| **DI** | Inject at composition root; no new mutable singletons for game state |
| **SOLID** | Depend on abstractions (`ISimWorldSnapshot`, `IOrderSink`, panel bridges); SRP on presenters vs views |
| **Testing** | Expose seams; headless/EditMode tests for pure logic; do not skip Logic-story tests |

Also respect engineer house rules: no allocs on hot paths, cache components in `Awake`, no production `Find*`/`Resources.Load`, immutability where practical.

**Handoff →** `/c-sharp-reviewer` (or `team-csharp` review lane) + this skill’s finish checklist.

### Phase 3 — Review (`/c-sharp-reviewer`)

Review against ADR laws, asmdef edges, allocation/hotpath rules, and `checklists/review-gates.md` when present. Map findings to verdict keywords (§8).

Optional Phase 0 (spike) is allowed for unknown seams; still ends in Phase 1 contract before product code.

---

## 7. Finish checklist

**When `checklists/pr-finish.md` exists, that file is authoritative** — run it and do not invent a parallel list.

**Temporary checklist** (until `checklists/pr-finish.md` lands in UCA-M4):

- [ ] No MB reads live sim chunks or caches session internals
- [ ] New UI path issues a **command** or cites an approved exception
- [ ] Reads go through snapshot / projection / `*Bridge` — not sim internals
- [ ] Map/C2 paths stay read-only re: order log and world truth (ADR-007)
- [ ] No `DelegationBridge` hotpath edits (zero-touch) unless explicitly waived
- [ ] Asmdef edges documented if assemblies changed
- [ ] Pure logic covered by EditMode or headless test where practical
- [ ] No new scene-only singleton without written waiver
- [ ] Hot path: no unexplained per-frame allocations
- [ ] ADR citations correct: presentation = ADR-010/001/007 — **not** ADR-018
- [ ] PR body links this skill + relevant ADR(s)

---

## 8. Verdict keywords

Use exactly one primary verdict in architecture / finish reports:

| Keyword | Meaning |
| --- | --- |
| **PASS** | Seams, ADRs, tests, and checklist satisfied; ready for review merge path |
| **FAIL** | Violation of presentation/command/assembly laws or checklist; must fix before merge |
| **BLOCKED** | Cannot proceed — missing decision, zero-touch conflict, absent projection contract, or dependency on undelivered milestone/ref |

Reviewer-style nuance (APPROVED / CHANGES REQUIRED) may appear from `/c-sharp-reviewer`; **architect finish language** for this skill remains **PASS / FAIL / BLOCKED**.

---

## 9. Relationship to other Aegis packs

| Pack | Domain |
| --- | --- |
| **This skill** | Unity / C# **architecture** |
| `/c-sharp-engineer` | Implementation under architect contract |
| `/c-sharp-reviewer` | Code review gates |
| Platform Design Assistant skill pack | Catalog / archetype **content** |
| Parallel dispatch playbook | **How** multi-agent work is split |
| Linear usage contract | Tracker truthfulness |

Do not merge PDA catalog skills or product feature scope into this file.

---

## 10. Implementation roadmap

See [`ROADMAP.md`](ROADMAP.md). Milestone IDs: **UCA-M0…UCA-M5**.

| Milestone | Skill posture |
| --- | --- |
| **UCA-M0** | Scaffold (done) |
| **UCA-M1** | **This file** — doctrine `m1-doctrine` |
| **UCA-M2…M4** | Fill references + checklists |
| **UCA-M5** | Dogfood → flip status to **`v1`** |

---

## 11. Non-goals

- Rewriting headless .NET sim core or reopening `DelegationBridge` hotpath by default
- Shipping player-facing product features under UCA issues
- Full DOTS-in-Unity authoritative playbook (v1: presentation non-authoritative)
- Re-hosting full ADR text
- Treating **Git ADR-018** as a Unity presentation boundary
