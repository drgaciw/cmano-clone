---
name: unity-csharp-architect
description: >
  Senior Unity C# architecture for Project Aegis (cmano-clone): presentation
  boundary (ADR-018), headless/command-driven UI (ADR-010), assembly definitions,
  ScriptableObject data ownership, MonoBehaviour anti-patterns, editor vs
  runtime split, performance budgets, and agent finish self-tests. Use whenever
  writing or reviewing Unity C#, MonoBehaviours, UnityAdapter presenters,
  EditorWindows, C2/UI chrome, or assembly graphs. Triggers on "Unity",
  "MonoBehaviour", "asmdef", "UnityAdapter", "presentation boundary",
  "EditorWindow", "C# architecture", "snapshot projection".
metadata:
  short-description: "Aegis Unity/C# architecture: ADR-010/018, asmdefs, finish checks"
  status: scaffold  # UCA-M0 — flesh out in UCA-M1+
  version: 0.1.0-draft
---

# Unity C# Architect (Project Aegis)

> **Status:** Scaffold for **UCA-M0**. Doctrine stubs only — expand in UCA-M1–M4.
> Design wiki: Notion *unity-csharp-architect Skill — Design & Sprint Roadmap*.
> Roadmap: [`ROADMAP.md`](ROADMAP.md).

Teach agents to write **architecturally correct** Unity C# for Aegis — not just
compiling MonoBehaviours. Simulation truth lives in headless .NET; Unity is a
**presentation shell** over read-only projections.

**Load this skill when:** any Unity presentation, editor tool, adapter, C# UI
chrome, or assembly-structure work is in scope.

**Do not use this skill for:** pure sim-core / gauntlet / catalog data work with
no Unity surface (use existing sim/data playbooks instead).

---

## 0. Two worlds (non-negotiable)

| World | Owns | Clock | Test without Editor? |
| --- | --- | --- | --- |
| **Simulation** | Authoritative state, commands, determinism | Sim / fixed step | **Yes — prefer this** |
| **Presentation** | Snapshots, interpolation, input capture → commands | Frame rate | Prefer headless presenters first |

**Law:** MonoBehaviours must not hold live write access to sim internals.
See ADR-018 (presentation boundary) and ADR-010 (headless / command-driven UI).

---

## 1. Open references (load on demand)

Paths are relative to this skill directory. **UCA-M1–M3** fill these files.

| Reference | When |
| --- | --- |
| `references/presentation-boundary.md` | Any MB / UI that needs sim state |
| `references/headless-command-ui.md` | Player intent, presenters, command bus |
| `references/asmdefs-and-layers.md` | New assemblies or dependency edges |
| `references/scriptableobjects-data.md` | Designer data vs runtime state |
| `references/mono-anti-patterns.md` | Review or refactor of existing MBs |
| `references/editor-vs-runtime.md` | EditorWindow / authoring tools |
| `references/performance-unity.md` | Hot paths, GC, pooling |
| `references/testing-unity.md` | Where to put tests |
| `references/aegis-unity-map.md` | "Where does X live in this repo?" |
| `checklists/pr-finish.md` | Before claiming Done |
| `checklists/review-gates.md` | Human / peer review prompts |

Until those files exist, treat §2–§6 below as the temporary source of truth.

---

## 2. Presentation boundary (ADR-018 distilled)

1. Presentation **reads projections / snapshots only**.
2. No caching of live ECS chunks or session internals on MonoBehaviours.
3. **Interpolation** is presentation-only; never "fake" sim steps in `Update`.
4. If a screen needs a new field, extend the **projection contract** — do not
   reach through the wall.

---

## 3. Command-driven UI (ADR-010 distilled)

1. Player intent → **command** → engine.
2. Presenters prefer **engine-free** logic (UnityAdapter / Authoring patterns).
3. If it can be proven headless, prove it headless before Play Mode.
4. Document any exception (new ADR or explicit waiver in the PR).

---

## 4. Assemblies & layers

1. New code lands in an existing **allowed** assembly when possible.
2. New asmdefs require an edge list in the PR (who depends on whom).
3. Forbidden shapes: UI → sim internals; Editor → runtime presentation shortcuts;
   circular asmdefs.
4. Prefer established `ProjectAegis.*` namespaces.

---

## 5. MonoBehaviour hygiene

| Prefer | Avoid |
| --- | --- |
| Thin view + presenter / projection bind | God MonoBehaviour |
| Explicit inject / service locator at composition root | `FindObjectOfType` sprawl |
| Event / command for intent | Direct sim mutation from `Update` |
| Pooling + no alloc in hot paths | Per-frame LINQ / string concat |

---

## 6. Finish checklist (agent — temporary until `checklists/pr-finish.md`)

- [ ] No MB reads live sim chunks or caches session internals
- [ ] New UI path issues a **command** or cites an approved exception
- [ ] Asmdef edges documented if assemblies changed
- [ ] Pure logic covered by EditMode or headless test where practical
- [ ] No new scene-only singleton without written waiver
- [ ] Hot path: no unexplained per-frame allocations
- [ ] PR body links this skill + relevant ADR(s)

---

## 7. Relationship to other Aegis skills / packs

| Pack | Domain |
| --- | --- |
| **This skill** | Unity / C# **architecture** |
| Platform Design Assistant skill pack | Catalog / archetype **content** assistant |
| Parallel dispatch playbook | **How** multi-agent work is split |
| Linear usage contract | Tracker truthfulness |

Do not merge PDA catalog skills into this file — different failure modes.

---

## 8. Implementation roadmap

See [`ROADMAP.md`](ROADMAP.md). Milestone IDs: **UCA-M0…UCA-M5**.
