# Wave 7 — Gauntlet productization triage (2026-08-02)

**HEAD baseline:** `a2c4c49` (Winchester hard gate + mutant 09 merged).  
**Program:** Wave 7 track W7-d (docs + residual measurement; no Launch).

## Closed since logistics sprint

| Item | Evidence |
|------|----------|
| EMCON fail-closed + real `emcon.units` | #377 |
| Bingo hard gate + mutant 08 load-bearing | #378, #380 |
| Shotgun soft multi-salvo gate | #379 |
| Winchester hard gate + mutant 09 | #381 |
| Required tokens include JOKER/BINGO/SHOTGUN/WINCHESTER + abort codes | `tools/qa-gauntlet/expected-tokens.json` |

## Residual — measure, don't paper

### 1. Expect-regen discipline

- Runbook: `tools/qa-gauntlet/README-expect-regen.md`
- Post-Winchester re-bless already updated anchors and Baltic magazine expectations (`WINCHESTER_ORDNANCE` supersedes pre-launch-only `NO_AMMO` when ledger empty).
- **Rule unchanged:** never bless to silence unexplained fingerprint drift; link PR/story in commit body.

### 2. T5 discriminative strength

- Historical concern: T5 scenarios may be weakly discriminative (pass even under mild mutants).
- **Wave 7 action:** record outcome from saboteur subset that includes tier-5 (catalog uses tiers 1/3/5 × seed 42). If a defect is only caught by ReplayGolden and never by ladder oracles, file as residual oracle strength — do not lower goldens.

### 3. Stress axes honesty

- Tools: `tools/qa-gauntlet/{stress_axes,apply_stress_axes,verify_stress_axes,plan_stress_matrix}.py` + READMEs.
- Logistics stress remains **config-driven** via burn model + magazine knobs; runtime-provable only when pins emit order-log tokens (already true for joker-bingo and shotgun-winchester scenarios).
- Do not claim universal logistics coverage for every ladder tier — claim pin coverage.

### 4. Calibration durability

- After full saboteur on tip (W7-a), publish dated calibration under `production/qa/gauntlet/calibration-2026-08-02-wave7/` with PROVENANCE (W7-b).
- Target kill rate: **9/9 defects**, control `00` survives.

## Explicit non-goals (this track)

- No new sim physics
- No Joker hard gate
- No Shotgun hard gate
- No Launch / commercial gate
- No `DelegationBridge` hotpath edits

## Exit for W7-d

- [x] This triage memo committed on `wave7/productize`
- [ ] Linked from closeout after W7-a/W7-b complete
