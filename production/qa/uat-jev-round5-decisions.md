# UAT — Jev decision record (round 5, 2026-11-21)

Jev (`typesafe/jev-1.14`, called through OpenRouter's decisions API with the `OPENROUTER_API_KEY` from the environment; the `jev-worker` proxy was not deployed) made the open owner decisions and scored the rubric.

- Request: [`uat-jev-round5-request.json`](uat-jev-round5-request.json) (facts only — no secrets, paths or personal data)
- Response: [`evidence/uat-2026-11-21/jev-round5-response.json`](evidence/uat-2026-11-21/jev-round5-response.json) (generation `gen-dec-1795292256-cO4Cp8S3k3IsxnwhskMj`)

## Decisions

| Item | Decision | Probability | Confidence | Action |
|------|----------|-------------|------------|--------|
| D15/D18 — hosts in no scene | `add_layout_safe_subset` | 0.87 | 0.80 | Add only hosts that do not overlap the map; document the rest as backlog (controller, in the Editor) |
| D26 — unwired in-game Scenario Editor actions | `disable_with_reason` | 0.84 | 0.76 | Plan Task 1 |
| D24b — exclusive selection vs. range hint | `change_hint_text_only` | 0.66 | 0.49 (low) | Plan Task 2; low confidence noted — `use_last_friendly_for_assessment` scored 0.30 |
| D35 — Addressables settings not committed | `commit_generated_settings` | 0.86 | 0.79 | Plan Task 4 |
| D22 — default selection skips red units | noul 0.64 | — | — | Keep (above 0.5, below the 0.80 "met" bar — flagged for owner confirmation) |
| D28 — Play Baltic Escort rewrites the scene | `play_without_rebuild` | 0.80 | 0.70 | Plan Task 3 |
| CLI-2…CLI-13 | `fix_high_value_subset` | 0.80 | 0.70 | Plan Task 5 (CLI-7, CLI-12, CLI-4); rest stays in the tools backlog |
| D16 — score penalty for repeated denials | `defer_to_design` | 1.00 | 1.00 | No change; game design owns it |
| Local catalog DB backups | `discard` | 0.99 | 0.98 | Discarded |
| **More UAT iterations** | **score 1.95 → 2 rounds** | 0:0.02, 1:0.27, 2:0.45, 3:0.26 | 0.43 | Rounds 5 and 6; UAT closes after round 6 |

## Rubric scores (noul; a criterion is met only when its mechanical check passes **and** noul ≥ 0.80)

| Criterion | Jev | Mechanical | Met |
|-----------|-----|------------|-----|
| C1 suite green | 0.96 | 4,620 / 0 | ✅ |
| C2 determinism | 0.93 | ReplayGolden green | ✅ |
| C4 clean console | 0.97 | 0 errors × 5 scenarios | ✅ |
| C6 presentation defects | 0.83 | fixed + screenshots | ✅ |
| C7 forbidden surfaces | 0.85 | Phase 0 smoke PASS | ✅ |
| C8 red-first tests | 0.05 | partial | ❌ → Plan Task 6 |
| C9 interactive behaviour | 0.49 | pass, but map-marker select untested | ❌ → round 5 UAT (map-marker select) |
| C11 every function | 0.50 | owner rows remain | ❌ → D15 subset + re-sweep in round 5 |
| C12 shipped data unchanged | 0.93 | hashes identical | ✅ |

C3, C5 and C10 were not asked in this call; round 6 asks every criterion.

## Round 6 (final) — Jev re-score

Request: [`uat-jev-round6-request.json`](uat-jev-round6-request.json). Response: [`evidence/uat-2026-11-21/jev-round6-response.json`](evidence/uat-2026-11-21/jev-round6-response.json) (generation `gen-dec-1795294353-tM4exAf9M8S65Ma5Psj1`).

| Criterion | Jev | Met (mechanical ✅ and ≥ 0.80) |
|-----------|-----|------|
| C1 | 0.83 | ✅ |
| C2 | 0.93 | ✅ |
| C3 | 0.95 | ✅ |
| C4 | 0.92 | ✅ |
| C5 | 0.70 | ❌ |
| C6 | 0.23 | ❌ (request described screenshots only under C10 — likely cause; not re-asked) |
| C7 | 0.85 | ✅ |
| C8 | 0.51 | ❌ (stub-level red evidence for D24/D25, config-only D20) |
| C9 | 0.97 | ✅ |
| C10 | 0.17 | ❌ owner pixel sign-off pending |
| C11 | 0.85 | ✅ |
| C12 | 0.86 | ✅ |

Decisions: `close_uat` 0.72 (lean close, below the bar); `next_step` = **owner_review_first** (0.61; submit_pr 0.37; another_round 0.02). The UAT ran the 2 extra rounds Jev chose and now goes to owner review before the round-5 branch is submitted.

## Round 6 — corrected re-ask (6c)

The first round-6 request described the fix screenshots only under C10 and predated D20b/D37/D38 and the C10 sign-off package. The corrected request ([`uat-jev-round6c-request.json`](uat-jev-round6c-request.json)) states those facts and says plainly that D37/D38 are verified headlessly only. Response: [`evidence/uat-2026-11-21/jev-round6c-response.json`](evidence/uat-2026-11-21/jev-round6c-response.json) (`gen-dec-1795295161-evsiogNKUWVkoo73KVJh`). Both responses are kept.

| Criterion | 6 (first) | 6c (corrected) | Met |
|-----------|-----------|----------------|-----|
| C1 | 0.83 | 0.83 | ✅ |
| C2 | 0.93 | 0.92 | ✅ |
| C3 | 0.95 | 0.95 | ✅ |
| C4 | 0.92 | 0.91 | ✅ |
| C5 | 0.70 | 0.82 | Jev ✅ — counted only after the D37/D38 Play Mode re-check (pending: Unity plugin disconnected) |
| C6 | 0.23 | 0.91 | ✅ |
| C7 | 0.85 | 0.84 | ✅ |
| C8 | 0.51 | 0.54 | ❌ — D24/D25 stub-level red; controller-level D24 red in progress |
| C9 | 0.97 | 0.97 | ✅ |
| C10 | 0.17 | 0.95 | ✅ evidence handed to owner ([`c10-owner-signoff.md`](evidence/uat-2026-11-21/c10-owner-signoff.md)); owner tick boxes pending |
| C11 | 0.85 | 0.86 | ✅ |
| C12 | 0.86 | 0.91 | ✅ |

`close_uat` 0.48; `next_step` owner_review_first 0.70.
