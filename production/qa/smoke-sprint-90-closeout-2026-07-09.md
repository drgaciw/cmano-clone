# Smoke Closeout — Sprint 90 (Agent/Skill P0 Sync)

**Date:** 2026-07-09  
**Sprint:** S90 (S89–S92 program sprint 2)  
**Status:** **S90 COMPLETE**

**Authority:** [`post-editor-hygiene-scope-boundary-2026-07-09.md`](../post-editor-hygiene-scope-boundary-2026-07-09.md), [`sprint-90-agent-skill-sync.md`](../sprints/sprint-90-agent-skill-sync.md), [`qa-plan-sprint-90-agent-skill-sync-2026-07-09.md`](qa-plan-sprint-90-agent-skill-sync-2026-07-09.md), [`tech-stack-agent-skill-recommendations-2026-07-08.md`](../../docs/reports/tech-stack-agent-skill-recommendations-2026-07-08.md), AGENTS.md

---

## Track completion

| Track | Story | Status | Deliverable |
|-------|-------|--------|-------------|
| Agent YAML P0 | S90-01 | **COMPLETE** | `unity-ui-specialist` legacy Input Manager; `unity-specialist` description/MCP/AI package honesty |
| Skill/doc P0 | S90-02 | **COMPLETE** | Claude-Agent-Setup MCP scopes-only; VERSION.md package table; skill when-not C2 gates |
| Closeout | S90-03 | **COMPLETE** | This smoke + gates log |
| README cross-link | S90-04 | **COMPLETE** | Tech-Stack + AGENTS → `unity/ProjectAegis/.claude/README.md` |
| CLAUDE.md count | S90-05 | **COMPLETE** | 67 agents; Unity engine set only |

---

## Standing gates (RUN+READ)

Evidence: [`production/qa/evidence/gates-sprint-90-closeout-2026-07-09.log`](evidence/gates-sprint-90-closeout-2026-07-09.log)

| Gate | Result |
|------|--------|
| Build | **0e/0w** |
| Full suite | **1599/0f** (311 Sim + 260 Del + 286 UA + 24 Excel + 102 Cli + 616 Data) |
| ReplayGolden | **6/6** |
| C2 proxy | **20/20** |
| Hash `17144800277401907079` | **18** paths |
| UA engage filter | **3/3** |
| DelegationBridge | **ZERO** (docs-only sprint) |
| Docs honesty | No “Prefer Input System”; no false MCP-in-deps; VERSION has UI Toolkit 2.0.0 |
| GitNexus `detect_changes` | **low** / 0 affected processes (docs sections) |
| Stage | **Release** |

**Verdict: ALL PASS**

---

## S90-01 — Agent YAML

- A1: `unity-ui-specialist` defaults to **legacy Input Manager**; Input System only with proof + approval
- A2: `unity-specialist` description + stack table (Input, AI packages present/not owned, MCP pending)
- Headless floors bumped to **≥20/20** in both agents

## S90-02 — Skill/doc

- B1: Claude-Agent-Setup — scopes yes; MCP **not** direct dep; checklist unchecked until install-plugin
- B2: VERSION.md — UI Toolkit, Addressables, AI packages + Built-in/legacy Input; last-verified 2026-07-09
- B3: `tests-run` / `editor-application-set-state` when-not strengthened for C2 PlayModeSmoke gates

## Exit checklist

- [x] Must-have stories S90-01..03 complete
- [x] Should/nice S90-04..05 complete
- [x] Standing gates PASS
- [x] QA plan criteria met
- [x] sprint-status.yaml updated
- [x] Stage remains **Release**
- [ ] `gt submit` docs stack (when user requests)

---

## Next

**S91** — Asset production specs ASSET-001…003. Plan: [`sprint-91-asset-spec-production.md`](../sprints/sprint-91-asset-spec-production.md).

---
*S90 closeout. verification-before RUN+READ on all gate claims. Docs-only; ZERO bridge.*
