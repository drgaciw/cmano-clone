# Unity — Version Reference

| Field | Value |
|-------|-------|
| **Engine** | Unity 6 |
| **Editor Version** | **6000.3.14f1** (Unity 6.3 LTS) |
| **Project Pinned** | 2026-05-29 |
| **LTS Support Until** | December 2027 (Unity 6.3 LTS) |
| **LLM Knowledge Cutoff** | May 2025 |
| **Risk Level** | **MEDIUM** — Unity 6.3 LTS post-dates cutoff; use this doc + official 6000.3 manuals |

## Project Choice

Project Aegis targets **Unity 6.3 LTS** (not 6.0 LTS — EOS Oct 2026). Aligns with `Tech-Stack.md`
and Dedicated Server headless runs. **World state is a managed, headless-first sim — not DOTS/ECS**
(ADR-005 reversed 2026-07-07; see [unity integration review](../../reports/unity-integration-review-2026-07-07.md) §3).
Unity is the presentation/build layer (UI Toolkit C2 + player builds) over the deterministic
`ProjectAegis.Sim`.

**Render / input (project truth):** **Built-in Forward** render pipeline + **legacy Input Manager**. Do **not** assume URP/HDRP or the new Input System without Project Settings proof and human approval. Source of truth: [`unity/ProjectAegis/.claude/README.md`](../../unity/ProjectAegis/.claude/README.md).

## Core Packages (pin in Unity project)

Pinned in `unity/ProjectAegis/Packages/manifest.json` (cite [`Tech-Stack.md`](../../Tech-Stack.md) — do not invent versions):

| Package | Version (6000.3) | Role |
|---------|------------------|------|
| `com.unity.addressables` | 2.3.16 | Content / asset loading (APP-6 atlas, map) |
| `com.unity.ui` | 2.0.0 | UI Toolkit (C2 panels) |
| `com.unity.burst` | 1.8.29 | Transitive dep of `com.unity.ai.inference` (Sentis); no direct hot-path use |
| `com.unity.ai.assistant` | 2.13.0-pre.2 | Unity AI Assistant — **present, not agent-owned** |
| `com.unity.ai.inference` | 2.6.1 | Unity Inference — **present, not agent-owned** |
| `com.ivanmurzak.unity.mcp` | 0.82.4 | Unity-MCP editor bridge |

> **DOTS/ECS removed (2026-07-07):** `com.unity.entities` and `com.unity.entities.graphics` were
> dropped — never used, and world state is a managed headless-first sim (ADR-005 reversed). The
> [dots-ecs-notes.md](dots-ecs-notes.md) migration notes are historical only.

## Knowledge Gaps (agent checklist)

- Verify API against **6000.3** docs, not 2022.3 or early 6000.0.
- Deterministic sim runs in the managed `ProjectAegis.Sim` tick (`SimTickPipeline`), **not** a Unity
  `FixedStepSimulationSystemGroup` — pure rules stay in `ProjectAegis.Sim` (no UnityEngine).
- Headless batch (25k @ 1000×) runs under `dotnet` (no Unity player loop / Burst); throughput comes
  from managed data-oriented parallelism, not ECS. (`IJobEntity` / `SystemAPI.Query` notes were
  dropped with the Entities packages.)
- C2 gates: prefer headless `PlayModeSmokeHarnessTests` (**≥20/20**) over Editor Play Mode.

## References

- [Unity 6.3 LTS release](https://unity.com/blog/unity-6-3-lts-is-now-available)
- [Unity 6 support policy](https://unity.com/releases/unity-6/support)
- [Unity Integration Review (2026-07-07)](../../reports/unity-integration-review-2026-07-07.md) — DOTS reversal rationale
- Project invariants: [`unity/ProjectAegis/.claude/README.md`](../../unity/ProjectAegis/.claude/README.md)

**Last verified:** 2026-07-09 (merge `main` into PR #238 — package table matches manifest; ADR-005 managed-sim)
