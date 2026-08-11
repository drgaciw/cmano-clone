# UCA-M0 Kickoff — unity-csharp-architect skill (2026-08-11)

## Summary

Future-sprint program to author a **senior Unity/C# architecture skill** for agent-assisted Aegis work. UCA-M0 lands the skeleton and cross-tool pointers only.

## Sources of truth

| Layer | Location |
| --- | --- |
| **Git (files)** | `production/agentic/skills/unity-csharp-architect/` |
| **Linear (status)** | Project *Unity C# Architect Skill* |
| **Notion (design)** | Hub page *unity-csharp-architect Skill — Design & Sprint Roadmap (2026-08-11)* |

## Why

Unity UI / Editor / Adapter work has matured (UI maturity waves, PE, C2 chrome), but architecture rules live in ADRs and tribal memory. Agents need a single load-on-demand skill (same role as browser `building-games`, but for Unity C# + Aegis ADRs).

## Anchors

- ADR-018 — Unity Presentation Boundary
- ADR-010 — Headless-First / Command-Driven UI
- ADR-007 — C2 Map Presentation (related)
- `production/agentic/linear-usage-contract.md`
- `production/agentic/linear-parallel-dispatch-playbook.md`

## Exit (UCA-M0)

- [x] Skill scaffold + ROADMAP in git
- [x] Kickoff note (this file)
- [x] Notion design page
- [x] Linear project + delivery issues
- [ ] Human ack of skill path (optional gate before UCA-M1)

## Next

Start **UCA-M1** when engineering capacity allows: expand `SKILL.md` and write `references/presentation-boundary.md` + `headless-command-ui.md` using parallel lanes A–D from `ROADMAP.md`.
