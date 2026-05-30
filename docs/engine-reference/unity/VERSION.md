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

## Core Packages (pin in Unity project when created)

| Package | Version (6000.3) | Role |
|---------|------------------|------|
| `com.unity.entities` | 1.4.6 | ECS world, fixed-step groups |
| `com.unity.burst` | 1.8.29 | Hot-path jobs |
| `com.unity.entities.graphics` | 1.4.20 | Client rendering only |

See [dots-ecs-notes.md](dots-ecs-notes.md) for timestep, headless, and migration notes.

## Knowledge Gaps (agent checklist)

- Verify API against **6000.3** docs, not 2022.3 or early 6000.0.
- `Entities.ForEach` / `IAspect` — obsolete; use `IJobEntity` / `SystemAPI.Query`.
- Deterministic sim: `FixedStepSimulationSystemGroup` + `World.SetTime` for batch/replay.
- Headless: Dedicated Server target + server-only system filters; pure rules stay in `ProjectAegis.Sim` (no UnityEngine).

## References

- [Unity 6.3 LTS release](https://unity.com/blog/unity-6-3-lts-is-now-available)
- [Unity 6 support policy](https://unity.com/releases/unity-6/support)
- [Entities 1.4 upgrade guide](https://docs.unity3d.com/Packages/com.unity.entities@1.4/manual/upgrade-guide.html)

**Last verified:** 2026-05-29
