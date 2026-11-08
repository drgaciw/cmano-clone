# Sprint 90 — Agent / Skill P0 Sync

**Dates:** 2026-07-09 → 2026-07-14 (est. 3–5 days)  
**Lead:** unity-specialist / producer (local closeout)  
**Program:** S89–S92 Post-Editor Engineering Hygiene + Asset Spec Production — sprint 2 of 4  
**Authority:** [`future-sprint-roadpmap-07092026.md`](../../docs/reports/future-sprint-roadpmap-07092026.md) §3/§6, [`roadmap-execute-plan-07092026.md`](../../docs/reports/roadmap-execute-plan-07092026.md) §3/§4 (S90), [`post-editor-hygiene-scope-boundary-2026-07-09.md`](../post-editor-hygiene-scope-boundary-2026-07-09.md), [`tech-stack-agent-skill-recommendations-2026-07-08.md`](../../docs/reports/tech-stack-agent-skill-recommendations-2026-07-08.md) §A/§B/§E/§F P0, [`Tech-Stack.md`](../../Tech-Stack.md), AGENTS.md, this file.

**Roadmap approval:** User approved S89–S92 roadmap **2026-07-09**.  
**Plan approval:** User approved write of this plan **2026-07-09**.

**Predecessor:** S89 COMPLETE (`smoke-sprint-89-closeout-2026-07-09.md`).  
**Successor:** S91 asset spec production (plan published; dispatch after this closeout).  
**QA plan:** [`production/qa/qa-plan-sprint-90-agent-skill-sync-2026-07-09.md`](../qa/qa-plan-sprint-90-agent-skill-sync-2026-07-09.md)

## Sprint Goal

Close tech-stack P0 agent/skill/doc drift (recs **A1–A3 / B1–B3**): legacy Input Manager truth in Unity agents, honest Unity MCP package state in Claude-Agent-Setup, and VERSION.md package table parity with Tech-Stack. **Docs / agent YAML only** — no sim/runtime code, no MCP plugin install, no Launch advance. Stage stays **Release**.

## Capacity

| Dimension | Value |
|-----------|-------|
| Total days | 5 |
| Buffer (20%) | 1 day |
| Available | 4 days |
| Parallel tracks | 3 (2 cloud + 1 local closeout) |

## Tracks (parallel after baseline)

| Track | Stack prefix | Worktree | Env | Story | Owner |
|-------|--------------|----------|-----|-------|-------|
| Agent YAML (A1–A2; A3 via B1) | `stack/sprint90/agents` | `.worktrees/stack/sprint90/agents` | **Cloud** | S90-01 | unity-ui-specialist / unity-specialist |
| Skill/doc fixes (B1–B3) | `stack/sprint90/skills` | `.worktrees/stack/sprint90/skills` | **Cloud** | S90-02 | unity-specialist / docs |
| Closeout | `stack/sprint90/closeout` | `.worktrees/stack/sprint90/closeout` | **Local** | S90-03 | producer |

**Wave order:** Phase 0 baseline → (S90-01 ∥ S90-02) → S90-03 closeout

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S90-01 | Agent YAML — A1 `unity-ui-specialist` + A2 `unity-specialist` (A3 satisfied when B1 lands) | unity-ui-specialist / unity-specialist | 1.5 | Phase 0; S89 complete | Default = **legacy Input Manager**; propose `com.unity.inputsystem` only after ProjectSettings/`activeInputHandler` proof + human approval; A2 description drops Input System as normal subsystem; MCP routing → `unity/ProjectAegis/.claude/skills/` + README; AI Assistant/Inference cited as **present, not agent-owned** |
| S90-02 | Docs/skills — B1 Claude-Agent-Setup + B2 VERSION.md + B3 optional ProjectAegis when-not notes | unity-specialist / docs | 2 | Phase 0; S89 complete | Setup doc: OpenUPM scopes yes; `com.ivanmurzak.unity.mcp` **not** claimed as direct `manifest.json` dep until `npx unity-mcp-cli install-plugin`; checklist unchecked until installed; VERSION.md lists UI Toolkit 2.0.0, Addressables 2.3.16, AI packages + Built-in RP + legacy Input; last-verified date; optional high-traffic skill “when-not: use headless PlayModeSmoke for C2 gates” |
| S90-03 | S90 closeout — gates, smoke, sprint-status, gt submit | producer | 1.5 | S90-01, S90-02 | `smoke-sprint-90-closeout-2026-07-*.md`; gates ≥1599/0f, 6/6, 20/20, hash, ZERO bridge; sprint-status COMPLETE; forward note S91 dispatch ready |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S90-04 | Light `team-unity` / `team-ui` ProjectAegis README cross-link (P1 slice) | unity-specialist | 0.5 | S90-01 | Phase-1 context lists `unity/ProjectAegis/.claude/README.md`; no new agents |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S90-05 | CLAUDE.md agent count honesty (49 → on-disk count) | producer | 0.25 | S90-03 | Count matches `.claude/agents/*.md`; Unity engine set only note |

## Carryover from Previous Sprint

| Task | Reason | New Estimate |
|------|--------|--------------|
| Tech-stack P0 A1–A3 / B1–B3 | Roadmap S90 theme; S89 deferred to this sprint | Owned by S90-01 / S90-02 |
| S89-04 / S89-05 backlog | Optional hygiene leftovers | Leave on S89; do not pull |

## Hard Gates (S90)

| Gate | Command / check | Pass criterion |
|------|-----------------|---------------|
| Build | `dotnet build ProjectAegis.sln` | 0 errors |
| Tests | `dotnet test ProjectAegis.sln -v minimal` | **≥1599 / 0 failed** |
| Replay | `--filter ReplayGoldenSuiteTests` | **6/6** |
| C2 | `--filter PlayModeSmokeHarnessTests` | **≥20/20** |
| Hash | `rg -l '17144800277401907079' tests/ data/` | 18 paths |
| Bridge | No `DelegationBridge.cs` hotpath edits | **ZERO** |
| Catalog | `CatalogWriteGate` | **extend-only** |
| Scope | Agent YAML + docs under Game-Requirements / engine-reference / `.claude` | No MCP plugin install; no sim code; no Launch |

**GitNexus:** Docs/agent-only stacks expect **low** `detect_changes` risk. Impact preflight still required if any symbol edit sneaks in.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Agents still cite false MCP-in-manifest after partial PR | Medium | High | S90-01 + S90-02 merge before closeout; grep audit for `com.ivanmurzak.unity.mcp` + “Input System” prefer language |
| Scope creep into P1 agent rewrites (A4–A7) | Medium | Medium | Must-have = P0 only; S90-04 is optional light cross-link |
| Accidental Unity package / ProjectSettings edit | Low | Critical | Docs-only stacks; no `manifest.json` dependency add |

## Dependencies on External Factors

- S89 COMPLETE — **done 2026-07-09**
- Tech-Stack.md authoritative for package pins
- Graphite `gt` stacks; no Unity Editor required

## Definition of Done for this Sprint

- [x] All Must Have tasks completed
- [x] All tasks pass acceptance criteria
- [x] QA plan exists — [`production/qa/qa-plan-sprint-90-agent-skill-sync-2026-07-09.md`](../qa/qa-plan-sprint-90-agent-skill-sync-2026-07-09.md)
- [x] Standing gates RUN+READ at closeout (≥1599/0f, 6/6, 20/20, hash, ZERO bridge)
- [x] Smoke closeout doc published — [`smoke-sprint-90-closeout-2026-07-09.md`](../qa/smoke-sprint-90-closeout-2026-07-09.md)
- [x] sprint-status.yaml updated (S90 COMPLETE)
- [x] No S1 or S2 bugs introduced
- [x] Stage remains **Release**
- [x] Ready for S91 track dispatch

## Standing Rules (from boundary)

- Stage = **Release** throughout S89–S92.
- Docs/agent YAML only for S90 Must Haves.
- No `DelegationBridge` hotpath; extend-only `CatalogWriteGate`.
- Baltic hash frozen (`17144800277401907079`).
- Do **not** install Unity MCP plugin as part of this sprint (doc honesty only).

## Next

S91-01 ∥ S91-02 (QA plan already published) → S92 hygiene gate. `gt submit` when user requests.

**Scope check:** If work expands beyond P0 A1–A3/B1–B3, run `/scope-check` against post-editor hygiene epic.

---
*Created via `/sprint-plan` for S90 after S89–S92 roadmap approval (2026-07-09). Implemented + closed 2026-07-09. Review mode: lean (PR-SPRINT skipped). Graphite-first. Superpowers.*
