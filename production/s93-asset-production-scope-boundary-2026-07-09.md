# S93 Asset Production Scope Boundary — First Binary Wave

**Date:** 2026-07-09  
**Status:** Published before S93 track dispatch  
**Authority:** [`docs/superpowers/plans/2026-07-09-short-term-dashboard-wave.md`](../docs/superpowers/plans/2026-07-09-short-term-dashboard-wave.md), [`post-editor-hygiene-scope-boundary-2026-07-09.md`](post-editor-hygiene-scope-boundary-2026-07-09.md) (carryover invariants), [`smoke-sprint-91-closeout-2026-07-09.md`](qa/smoke-sprint-91-closeout-2026-07-09.md), [`design/assets/asset-manifest.md`](../design/assets/asset-manifest.md), AGENTS.md

---

## Purpose

S91 delivered **production-ready specs** (ASSET-001…003 umbrellas **Specced**; manifest **38 Specced / 0 In Production / 0 Done**). S93 delivers the **first binary asset wave** for priority children under those umbrellas — placeholders acceptable where generation prompts exist. **No sim hotpath edits.** Stage remains **Release**.

---

## Standing Invariants (unchanged)

| Gate | Pass criterion |
|------|----------------|
| Build | 0 errors |
| Tests | **≥1599 / 0 failed** |
| ReplayGolden | **6/6** |
| C2 proxy | **≥20/20** |
| Hash | `17144800277401907079` preserved (18 paths) |
| DelegationBridge | **ZERO** hotpath edits |
| CatalogWriteGate | **extend-only** |

**GitNexus §5 CRITICALs:** ScenarioDocumentEditor 233, CatalogWriteGate 186, DelegationBridge 145, PatrolCandidateEngagePolicy 113, BalticReplayHarness 54.

---

## In Scope / Out of Scope

| In scope | Out of scope |
|----------|--------------|
| Binary/placeholder production for ASSET-001…003 **priority children** (004, 005, 014, 018, 019, 023–025) | Addressables bulk import |
| Manifest status **Specced → In Production → Done** (per produced asset) | Store upload / E7 execution |
| `production/assets/` tree (capsules, tokens, theater refs) | `DelegationBridge` / sim hotpath edits |
| USS token files per art bible §3/§8 | Baltic reopen / hash change |
| Generation-prompt execution for store capsules | Full 38-asset wave |
| Editor PNG captures deferred to Phase C (ASSET-027+) | Launch stage advance |

---

## Priority Production Set (S93 MVP)

| Umbrella | P0 children | Output location |
|----------|-------------|-----------------|
| ASSET-001 C2 suite | ASSET-004 APP-6 atlas, ASSET-014 `AegisTokens.uss`, ASSET-005 top bar USS | `production/assets/c2/` |
| ASSET-002 Baltic theater | ASSET-018 map framing PNG, ASSET-019 band-B overlay spec sheet | `production/assets/baltic/` |
| ASSET-003 Store pack | ASSET-023 main capsule, ASSET-024 small capsule, ASSET-025 logo/header variants | `production/assets/store/` |

---

## Exit Criterion

First binary asset wave landed for all three umbrellas; manifest honest (files on disk at documented paths); standing gates held; stage **Release**. Screenshot captures (ASSET-027+) remain Phase C / Editor host.

**Cites:** S91 closeout + post-editor boundary + commercial-launch boundary (E7 prep only).
