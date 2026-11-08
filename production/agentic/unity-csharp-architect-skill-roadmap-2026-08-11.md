# UCA-M0 Kickoff — unity-csharp-architect skill (2026-08-11)

## Summary

Future-sprint program to author a **senior Unity/C# architecture skill** for agent-assisted Aegis work. UCA-M0 lands the skeleton and cross-tool pointers only.

## Sources of truth

| Layer | Location |
| --- | --- |
| **Git (files)** | `production/agentic/skills/unity-csharp-architect/` |
| **Linear (status)** | Project [*Unity C# Architect Skill*](https://linear.app/drgamtd-workspace/project/unity-c-architect-skill-7265dc770ee2) |
| **Linear epic** | [DRG-124](https://linear.app/drgamtd-workspace/issue/DRG-124) — children UCA-01…10 (DRG-125…134) |
| **Notion (design)** | Hub page *unity-csharp-architect Skill — Design & Sprint Roadmap (2026-08-11)* |
| **Post-S97 program** | `production/agentic/post-s97-uca-agent-capability-train-2026-08-11.md` |

## Why

Unity UI / Editor / Adapter work has matured (UI maturity waves, PE, C2 chrome), but architecture rules live in ADRs and tribal memory. Agents need a single load-on-demand skill (same role as browser `building-games`, but for Unity C# + Aegis ADRs).

## Anchors

- ADR-018 — Unity Presentation Boundary
- ADR-010 — Headless-First / Command-Driven UI
- ADR-007 — C2 Map Presentation (related)
- `production/agentic/linear-usage-contract.md`
- `production/agentic/linear-parallel-dispatch-playbook.md`
- `/c-sharp-engineer` concerns: layering, DI, SOLID, immutability, allocations, async, testing

## Exit (UCA-M0)

- [x] Skill scaffold + ROADMAP in git
- [x] Kickoff note (this file)
- [x] Notion design page
- [x] Linear project + delivery issues (DRG-118…123)
- [x] Linear epic + executable children (DRG-124…134)
- [x] Post-S97 program roadmap note
- [ ] PR #471 merge to main
- [ ] Human ack of skill path (optional gate before UCA-M1)

## Next

1. Merge PR #471 → close DRG-118 / UCA-01 (DRG-125).
2. Start **UCA-M1** when engineering capacity allows: expand `SKILL.md` and write `references/presentation-boundary.md` + `headless-command-ui.md` (DRG-126…128) using parallel lanes A–D from `ROADMAP.md`.
