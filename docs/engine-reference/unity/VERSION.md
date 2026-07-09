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

Project Aegis targets **Unity 6.3 LTS** (not 6.0 LTS — EOS Oct 2026). Aligns with `Tech-Stack.md`, DOTS/ECS per doc 08, and Dedicated Server headless runs.

**Render / input (project truth):** **Built-in Forward** render pipeline + **legacy Input Manager**. Do **not** assume URP/HDRP or the new Input System without Project Settings proof and human approval. Source of truth: [`unity/ProjectAegis/.claude/README.md`](../../unity/ProjectAegis/.claude/README.md).

## Core Packages (pin in Unity project)

Pinned in `unity/ProjectAegis/Packages/manifest.json` (cite [`Tech-Stack.md`](../../Tech-Stack.md) — do not invent versions):

| Package | Version (6000.3) | Role |
|---------|------------------|------|
| `com.unity.entities` | 1.4.6 | ECS world, fixed-step groups |
| `com.unity.burst` | 1.8.29 | Hot-path jobs |
| `com.unity.entities.graphics` | 1.4.20 | Client rendering only |
| `com.unity.ui` | 2.0.0 | UI Toolkit (C2 presentation) |
| `com.unity.addressables` | 2.3.16 | Addressables / content loading |
| `com.unity.ai.assistant` | 2.13.0-pre.2 | Unity AI Assistant — **present, not agent-owned** |
| `com.unity.ai.inference` | 2.6.1 | Unity Inference — **present, not agent-owned** |

**Not a direct dependency yet:** `com.ivanmurzak.unity.mcp` — OpenUPM scopes only; install via `npx unity-mcp-cli install-plugin ./unity/ProjectAegis` (see Tech-Stack / Claude-Agent-Setup).

See [dots-ecs-notes.md](dots-ecs-notes.md) for timestep, headless, and migration notes.

## Knowledge Gaps (agent checklist)

- Verify API against **6000.3** docs, not 2022.3 or early 6000.0.
- `Entities.ForEach` / `IAspect` — obsolete; use `IJobEntity` / `SystemAPI.Query`.
- Deterministic sim: `FixedStepSimulationSystemGroup` + `World.SetTime` for batch/replay.
- Headless: Dedicated Server target + server-only system filters; pure rules stay in `ProjectAegis.Sim` (no UnityEngine).
- C2 gates: prefer headless `PlayModeSmokeHarnessTests` (**≥20/20**) over Editor Play Mode.

## References

- [Unity 6.3 LTS release](https://unity.com/blog/unity-6-3-lts-is-now-available)
- [Unity 6 support policy](https://unity.com/releases/unity-6/support)
- [Entities 1.4 upgrade guide](https://docs.unity3d.com/Packages/com.unity.entities@1.4/manual/upgrade-guide.html)
- Project invariants: [`unity/ProjectAegis/.claude/README.md`](../../unity/ProjectAegis/.claude/README.md)

**Last verified:** 2026-07-09 (S90 agent/skill P0 sync — package table aligned with Tech-Stack + manifest)
