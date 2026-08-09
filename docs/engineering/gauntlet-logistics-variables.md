# Gauntlet logistics variables — Joker, Bingo, Shotgun, Winchester

Aviation doctrine labels used as **gauntlet testing variables** (fingerprint-proven).

| Variable | Doctrine meaning | Engine signal | Scenario pin |
|----------|------------------|---------------|--------------|
| **Joker** | Fuel warning above Bingo — finish the task soon and prepare to leave | `FuelStateChange` …`\|JOKER\|` (burn model) | `gauntlet-t3-logistics-joker-bingo` |
| **Bingo** | Low-fuel deadline — RTB or tanker **immediately** | `FuelStateChange` …`\|BINGO\|` | same |
| **Shotgun** | Pre-briefed minimum remaining / defensive ordnance | `OrdnanceStateChange` …`\|SHOTGUN\|` | `gauntlet-t2-ordnance-shotgun-winchester` |
| **Winchester** | Out of weapons (all bombs/missiles/rounds expended) | `OrdnanceStateChange` …`\|WINCHESTER\|` + hard deny `WINCHESTER_ORDNANCE` | same |

## Policy knobs

### Fuel (Joker / Bingo)

Burn model (required for order-log emission):

```json
"logistics": {
  "jokerSimSeconds": 90,
  "bingoSimSeconds": 180,
  "fuelCapacityKg": 1000,
  "burnRateKgPerSecond": 100,
  "jokerFuelFraction": 0.25,
  "bingoFuelFraction": 0.10
}
```

Bands: remaining fraction ≤ bingo → **BINGO**; else ≤ joker → **JOKER**; else **NOMINAL**.
Time-band-only logistics (no capacity/burn) is UI-only via `FuelStateProjection` and does **not** fingerprint.

### Ordnance (Shotgun / Winchester)

```json
"engage": {
  "defaultMagazineRounds": 2,
  "shotgunRoundsThreshold": 1
}
```

After each successful fire: remaining ≤ 0 → **WINCHESTER**; else remaining ≤ `shotgunRoundsThreshold` → **SHOTGUN**; else **NOMINAL**.
Threshold `0` disables Shotgun (only Winchester at empty).

## Required run-wide tokens

`tools/qa-gauntlet/expected-tokens.json` requires `JOKER`, `BINGO`, `SHOTGUN`, and `WINCHESTER` once the logistics ladder pins above are in the shipped set.


## Doctrine gates (v1)

| Band | Fingerprint | Engage gate |
|------|-------------|-------------|
| Joker | `FuelStateChange` → JOKER | None (advisory) |
| Bingo | `FuelStateChange` → BINGO | **Hard deny** engage — `EngagementAbortReason.BingoFuel` / log code `BINGO_FUEL` |
| Shotgun | `OrdnanceStateChange` → SHOTGUN | **Soft deny** multi-salvo (`SalvoSize` > 1) — `ShotgunOrdnance` / `SHOTGUN_ORDNANCE` |
| Winchester | `OrdnanceStateChange` → WINCHESTER | **Hard deny** engage — `EngagementAbortReason.WinchesterOrdnance` / log code `WINCHESTER_ORDNANCE` |

Bingo pin (`gauntlet-t3-logistics-joker-bingo`) uses `pkKill=0` so post-Bingo engages are not masked by `TARGET_DESTROYED` (saboteur `08-bingo-gate-bypass` requires load-bearing `BINGO_FUEL`).

Winchester pin (`gauntlet-t2-ordnance-shotgun-winchester`) uses `pkKill=0` so post-empty engages hit the hard gate (`WINCHESTER_ORDNANCE`) rather than only pre-launch `NO_AMMO` / `TARGET_DESTROYED` (saboteur `09-winchester-gate-bypass` requires load-bearing `WINCHESTER_ORDNANCE`).

Gate wiring:
- Bingo: `FuelTimelineTracker.IsBingo` → `EngageContext.LogisticsBingoBlocked` → `LogisticsBingoEngageGate`
- Shotgun: live magazine rounds + `ShotgunRoundsThreshold` → `LogisticsShotgunEngageGate` (single-round residual still allowed)
- Winchester: tracked ledger rounds ≤ 0 → `LogisticsWinchesterEngageGate` → `WinchesterOrdnance` / `WINCHESTER_ORDNANCE` (hard deny after EMCON/FC; `MagazineLedger.TryGetRounds` is authoritative when seeded; unseeded mounts fall back to context rounds)

Saboteur: `08-bingo-gate-bypass` forces Bingo gate open (defect; should be caught by goldens/token_coverage).
Saboteur: `09-winchester-gate-bypass` forces Winchester gate open (defect; should be caught by goldens/token_coverage/victory_roe).

## Joker residual (Wave 7 — advisory by design)

**Contract:** Joker is **fingerprint + doctrine label only**. There is **no** engage hard deny and **no** soft multi-salvo gate for Joker.

| Assertion | Status |
|-----------|--------|
| `FuelStateChange` emits `JOKER` between NOMINAL and BINGO | **Required** (ladder pin / `requiredRunWide`) |
| Engage continues while Joker (pre-Bingo) | **Intended** — not load-bearing yet; pin oracle can be masked by Winchester/denials |
| No `JOKER_FUEL` / `EngagementAbortReason` for Joker | **Required** — do not invent without product ack |
| Saboteur anti-regression for spurious hard deny | **Deferred** until product wants a positive post-Joker engagement pin or mutant |

Rationale: aviation doctrine treats Joker as “finish the task soon,” not Bingo (“leave now”). Wave 7 keeps that honesty; Bingo/Winchester remain the load-bearing hard denies. A dedicated post-Joker/pre-Bingo successful-engage assertion is a follow-up residual (Codex P2 on #393), not a gate invent.
