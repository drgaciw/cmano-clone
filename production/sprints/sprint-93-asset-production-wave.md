# Sprint 93 — Asset Production Wave

**Dates:** 2026-07-09 → 2026-07-16 (est. 3–5 days)  
**Lead:** art-director / producer (local closeout)  
**Program:** Post–S92 forward — first binary asset wave (dashboard short-term item 5)  
**Authority:** [`docs/superpowers/plans/2026-07-09-short-term-dashboard-wave.md`](../../docs/superpowers/plans/2026-07-09-short-term-dashboard-wave.md), [`s93-asset-production-scope-boundary-2026-07-09.md`](../s93-asset-production-scope-boundary-2026-07-09.md), [`smoke-sprint-91-closeout-2026-07-09.md`](../qa/smoke-sprint-91-closeout-2026-07-09.md), [`design/assets/asset-manifest.md`](../../design/assets/asset-manifest.md), [`design/art/art-bible.md`](../../design/art/art-bible.md), AGENTS.md

**Roadmap approval:** User approved full S93 sprint **2026-07-09**.  
**Plan approval:** User approved write via dashboard wave plan **2026-07-09**.  
**QA plan:** [`production/qa/qa-plan-sprint-93-asset-production-2026-07-09.md`](../qa/qa-plan-sprint-93-asset-production-2026-07-09.md)

> **Predecessor:** S91 COMPLETE — ASSET-001…003 **Specced**; manifest 38/0/0. S93 moves priority children to **Done** with on-disk binaries.

## Sprint Goal

Produce first binary wave for ASSET-001…003 priority children (C2 tokens/atlas, Baltic theater refs, store capsules/logos), bump manifest statuses, and close S93 with standing gates held. **Assets + docs only** — no Addressables bulk import, no store upload, no C# hotpath. Stage stays **Release**.

## Capacity

| Dimension | Value |
|-----------|-------|
| Total days | 5 |
| Buffer (20%) | 1 day |
| Available | 4 days |
| Parallel tracks | 3 (cloud) + 1 local closeout |

## Tracks (parallel after baseline)

| Track | Stack prefix | Worktree | Env | Story | Owner |
|-------|--------------|----------|-----|-------|-------|
| C2 + tokens | `stack/sprint93/asset-c2` | `.worktrees/stack/sprint93/asset-c2` | **Cloud** | S93-01 | technical-artist |
| Store capsules | `stack/sprint93/asset-store` | `.worktrees/stack/sprint93/asset-store` | **Cloud** | S93-02 | art-director |
| Baltic refs | `stack/sprint93/asset-baltic` | `.worktrees/stack/sprint93/asset-baltic` | **Cloud** | S93-03 | art-director |
| Closeout | `stack/sprint93/closeout` | `.worktrees/stack/sprint93/closeout` | **Local** | S93-04 | producer |

**Wave order:** Phase 0 baseline (day 1) → (S93-01 ∥ S93-02 ∥ S93-03) → S93-04 closeout

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S93-01 | C2 binary wave — APP-6 atlas, AegisTokens.uss, top bar USS | technical-artist | 2 | Phase 0 | Files at `production/assets/c2/`; art bible §3 colors; ASSET-004/014/005 manifest → **Done**; no C# hotpath |
| S93-02 | Store capsule wave — main/small capsules + logo variants | art-director | 2 | Phase 0 | 616×353 + 231×87 PNGs; ASSET-023–025 **Done**; E7 prep only; cites commercial-launch boundary |
| S93-03 | Baltic refs — theater framing + band-B overlay sheet | art-director | 1.5 | Phase 0 | ASSET-018 PNG + ASSET-019 spec sheet at `production/assets/baltic/`; manifest bumps |
| S93-04 | S93 closeout — manifest, gates, smoke, sprint-status | producer | 1.5 | S93-01..03 | ≥6 Done / ≥3 In Production; smoke closeout; gates ≥1599/0f, 6/6, 20/20, hash, ZERO bridge |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S93-05 | Roadmap §8 + dashboard item 5 closed | producer | 0.5 | S93-04 | `future-sprint-roadpmap-07092026.md` §8; project-dashboard short-term updated |

## Carryover from S91

| Item | S91 state | S93 action |
|------|-----------|------------|
| ASSET-001…003 | Specced | Umbrellas → **In Production**; P0 children → **Done** |
| ASSET-027+ screenshots | Specced (capture deferred) | Phase C Editor PNG — not S93 blocker |
| Manifest 38/0/0 | S91 closeout | Target ≥6 Done, ≥3 In Production |

## Hard Gates (S93)

| Gate | Command / check | Pass criterion |
|------|-----------------|---------------|
| Build | `dotnet build ProjectAegis.sln` | 0 errors |
| Tests | `dotnet test ProjectAegis.sln -v minimal` | **≥1599 / 0 failed** |
| Replay | `--filter ReplayGoldenSuiteTests` | **6/6** |
| C2 | `--filter PlayModeSmokeHarnessTests` | **≥20/20** |
| Hash | `rg -l '17144800277401907079' tests/ data/` | 18 paths |
| Bridge | No `DelegationBridge.cs` hotpath edits | **ZERO** |
| Scope | Assets under `production/assets/` + manifest/docs | No Addressables bulk; no store upload |

## Definition of Done

- [ ] All Must Have tasks completed
- [ ] Standing gates RUN+READ at closeout
- [ ] Smoke closeout published
- [ ] sprint-status.yaml `s93_status` + `s93_complete`
- [ ] Stage remains **Release**
- [ ] Dashboard short-term item 5 resolved

## Standing Rules

- Stage = **Release** throughout S93.
- GitNexus preflight on §5 CRITICALs before any symbol edit (assets/docs expect **low** risk).
- No `DelegationBridge` hotpath; extend-only on `CatalogWriteGate`.
- Baltic hash frozen (`17144800277401907079`).
- Placeholder binaries acceptable; manifest bumps only when files exist on disk.

---
*Created for S93 dashboard wave (2026-07-09). Graphite-first. Superpowers.*
