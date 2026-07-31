# Gauntlet logistics variables — Joker, Bingo, Shotgun, Winchester

Aviation doctrine labels used as **gauntlet testing variables** (fingerprint-proven).

| Variable | Doctrine meaning | Engine signal | Scenario pin |
|----------|------------------|---------------|--------------|
| **Joker** | Fuel warning above Bingo — finish the task soon and prepare to leave | `FuelStateChange` …`\|JOKER\|` (burn model) | `gauntlet-t3-logistics-joker-bingo` |
| **Bingo** | Low-fuel deadline — RTB or tanker **immediately** | `FuelStateChange` …`\|BINGO\|` | same |
| **Shotgun** | Pre-briefed minimum remaining / defensive ordnance | `OrdnanceStateChange` …`\|SHOTGUN\|` | `gauntlet-t2-ordnance-shotgun-winchester` |
| **Winchester** | Out of weapons (all bombs/missiles/rounds expended) | `OrdnanceStateChange` …`\|WINCHESTER\|` (+ often `NO_AMMO`) | same |

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
