# VS1 fixtures — attention Score + skill-unavailable disclosure

**Linear:** [DRG-319](https://linear.app/drgamtd-workspace/issue/DRG-319/jev-vs1-attention-score-disclosure-chrome)  
**Surface:** `surface/c2-presentation` (chrome only)  
**Questions:** `attention_priority` (Score 5), `skill_unavailable_disclosure` (Noul)  
**Model pin:** `jev-1.13.x` / OpenRouter `typesafe/jev-1.13`  
**Schema:** `aegis.jev.state.v0` (projection-only; synthetic IDs)

## Privacy

These files are **synthetic projection fixtures** only. They must never include scenario XML/JSON dumps, DecisionLog/sim internals, absolute repo paths, secrets, doctrine corpora, or private Notion/git evidence. Hosted API calls still require privacy ACK for any non-synthetic payload.

## Score levels (`attention_priority`)

| Level | Meaning (chrome) |
|-------|------------------|
| `ignore` | No highlight / bottom of sort |
| `low` | Soft / optional glance |
| `watch` | Secondary panel / soft highlight |
| `focus` | Primary panel / strong highlight |
| `immediate` | Top of attention sort / interrupt chrome |

## Fixtures

| File | Purpose | Expected Score | Expected disclosure |
|------|---------|----------------|---------------------|
| `attention_immediate.json` | Single hostile closing contact; player looking elsewhere | `immediate` | off (`jevDecide: true`) |
| `attention_ignore.json` | Distant friendly; player already focused | `ignore` | off |
| `attention_mixed_roster.json` | 5 units varied urgency/closing/ROE for sort order | see `expectedSortOrderHint` / per-row `expectedAttentionLevel` | off |
| `skill_unavailable.json` | `skillAvailability.jevDecide: false` disclosure path | n/a (fail-soft heuristic OK) | **on** |
| `skill_available.json` | Healthy decide path | ~`watch` | **off** |
| `questions_vs1.json` | Question payloads matching research inventory | — | — |

### Mixed roster expected sort (high → low)

1. `u-fixture-imm` → `immediate`  
2. `u-fixture-focus` → `focus`  
3. `u-fixture-watch` → `watch`  
4. `u-fixture-low` → `low`  
5. `u-fixture-ignore` → `ignore`

## Out of scope (VS1)

- No order cards, no weapon recommend, no façade enqueue  
- No product C#/Unity impl in this prep  
- No DelegationBridge hotpath; presentation binder only (future)  
- Impl blocked on **CMANO pre-launch GO**

## Dry-run / future binder call

`decide.py` today only knows router decision IDs (`route_execution`, …) and has **no** `--questions-file` flag. Do **not** call live OpenRouter for these product fixtures from the shadow router.

When the Presentation Jev binder lands (after CMANO GO), call Decisions API roughly as:

```bash
# Pseudocode — cloud/impl agent, not current decide.py
# POST https://openrouter.ai/api/alpha/decisions
# body: {
#   "model": "typesafe/jev-1.13",
#   "state": <contents of attention_immediate.json minus expected* fields>,
#   "questions": <questions_vs1.json .questions>
# }
```

For local JSON sanity only (no API):

```bash
python3 -c "import json,pathlib; p=pathlib.Path('/workspace/jev/fixtures/vs1');
[json.load(open(f)) for f in p.glob('*.json')]; print('ok', len(list(p.glob('*.json'))))"
```

## Glue reminders (impl later)

- Optional: `C2TopBarPanelHost`  
- Existing: AdviceBridge / EngageExplain / CommandReview stack  
- **NEW:** Presentation Jev binder (AttentionScore + disclosure) + tests  
- Pin `jev-1.13.x`; projection-only state; privacy ACK boundary
