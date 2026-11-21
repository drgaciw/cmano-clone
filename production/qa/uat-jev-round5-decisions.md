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

## Round 6d — after the D37/D38 Play Mode re-check

Request: [`uat-jev-round6d-request.json`](uat-jev-round6d-request.json). Response: [`evidence/uat-2026-11-21/jev-round6d-response.json`](evidence/uat-2026-11-21/jev-round6d-response.json) (`gen-dec-1795296557-l8zdfHnFebZ1nW0EFNLF`).

New evidence: the Unity Editor exited at 15:20:09 and was relaunched. The D25 PlayMode test went RED on the real pre-fix toolbar code (`Expected: String containing "ISSUED: hold → u1"`, `But was: <string.Empty>`) and GREEN again after the revert. D37 and D38 were verified in Play Mode. The same screenshot exposed D39: the Doctrine override dropdown was clipped.

| Item | Jev |
|------|-----|
| C5 | 0.67 (down from 0.82) |
| C8 | 0.54 |
| C10 | 0.52 |
| D39 | **fix_now** (0.89; backlog 0.11) |
| more_rounds | **1.09 → 1 more round** (0: 0.14, 1: 0.63, 2: 0.23) |
| close_uat | 0.23 |

## Round 7 — the extra round

Request: [`uat-jev-round7-request.json`](uat-jev-round7-request.json). Response: [`evidence/uat-2026-11-21/jev-round7-response.json`](evidence/uat-2026-11-21/jev-round7-response.json) (`gen-dec-1795297395-a0t8X7ZzMNze7VhFmWiW`).

| Criterion | Jev | Met (mechanical ✅ and ≥ 0.80) |
|-----------|-----|------|
| C1 | 0.98 | ✅ |
| C2 | 0.96 | ✅ |
| C3 | 0.97 | ✅ |
| C4 | 0.97 | ✅ |
| C5 | 0.48 | ❌ |
| C6 | 0.91 | ✅ |
| C7 | 0.96 | ✅ |
| C8 | 0.74 | ❌ |
| C9 | 0.96 | ✅ |
| C10 | 0.76 | ❌ owner sign-off |
| C11 | 0.70 | ❌ |
| C12 | 0.90 | ✅ |

Decisions:
- **more_rounds: 0** (score 0.21; p0 0.82).
- **next_step: owner_review_first** (0.92).
- close_uat 0.64.

Controller corrections found after this call:
- The request left out the hostile-symbol and overlay facts that C5 asks about (`ENVELOPES 2`, `DATALINKS 1`, hostile ■ and contact ◆ on the map).
- It left out the per-panel classification of the 95 skipped sweep controls behind C11.
- It **overstated C8**. Two scene-config fixes had no red test: `f787b3a2` (toolbar added to the scene) and `716b3dfd` (Doctrine `showPanel: 1`). Scene guard tests are being added red-first against each commit's parent.
- New D40 (pre-existing): the Play Mode smoke feed logs a synthetic `emcon Active → Passive` row that changes no unit.

## Round 7b — single corrected re-ask (final)

Request: [`uat-jev-round7b-request.json`](uat-jev-round7b-request.json). Response: [`evidence/uat-2026-11-21/jev-round7b-response.json`](evidence/uat-2026-11-21/jev-round7b-response.json) (`gen-dec-1795297659-qowa6KQo7ETSQgSgOask`).

The round 7 request is kept unchanged. This re-ask added only true facts that the round 7 request had left out or stated wrongly:
- C5: hostile symbols and the `ENVELOPES 2` / `DATALINKS 1` overlay counts.
- C11: the per-panel classification of the skipped controls.
- C8: an honest account. The two scene-config fixes had no test until scene guard tests `5672ea45` were added and shown red on the real parent scenes.
- D40 was added as a decision.

This is the last call; the result is accepted as returned.

| Criterion | 7 | 7b | Met |
|-----------|---|----|-----|
| C1 | 0.98 | 0.97 | ✅ |
| C2 | 0.96 | 0.95 | ✅ |
| C3 | 0.97 | 0.95 | ✅ |
| C4 | 0.97 | 0.86 | ✅ |
| C5 | 0.48 | 0.91 | ✅ |
| C6 | 0.91 | 0.89 | ✅ |
| C7 | 0.96 | 0.95 | ✅ |
| C8 | 0.74 | 0.72 | ❌ |
| C9 | 0.96 | 0.95 | ✅ |
| C10 | 0.76 | 0.72 | ❌ owner sign-off |
| C11 | 0.70 | 0.77 | ❌ |
| C12 | 0.90 | 0.91 | ✅ |

Decisions:
- **more_rounds 0** (score 0.45; p0 0.59, p1 0.37).
- **next_step owner_review_first** (0.76; submit_pr 0.20).
- **D40 backlog** (0.63).
- close_uat 0.47.

**Outcome:** the UAT stops here by Jev's decision. Three criteria are still below the bar:
- **C10:** needs the owner's tick boxes in [`c10-owner-signoff.md`](evidence/uat-2026-11-21/c10-owner-signoff.md).
- **C8 and C11:** stay below 0.80 on the full evidence. They are handed to owner review together with the backlog.
