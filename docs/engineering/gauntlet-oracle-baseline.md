# Gauntlet oracle CI baseline

The `gauntlet-oracle` GitHub Actions job is a deterministic cross-tier smoke test. It runs the
nine staged policies against the committed multi-domain catalog using seed `42` and `10`
ticks. Those inputs are centralized as `GAUNTLET_ORACLE_SEED` and
`GAUNTLET_ORACLE_TICKS` in
[`.github/workflows/gauntlet-oracle.yml`](../../.github/workflows/gauntlet-oracle.yml).

This profile is intentionally distinct from the authoritative ladder budgets (T1–T5:
6/10/16/24/40 ticks). An envelope that is valid for a ladder run is not automatically valid
for the CI smoke profile.

## July 17, 2026 recalibration

The T3/T5 ranges were recalibrated after the policies moved from older single-hostile runs to
the current catalog-backed multi-domain ORBAT:

| Policy | CI observation (seed 42, ticks 10) | Current relevant envelope |
|---|---:|---:|
| `gauntlet-t3-emcon-phases` | denials `10` | denials `8..18`, score `0..300` |
| `gauntlet-t5-roe-change` | denials `20`, score `100` | denials `14..28`, score `50..170` |

The ranges retain fail-closed behavioral gates: T3 still requires the air and subsurface
shooters to emit `True|Launched`; T5 still requires the `CommsStateChange` and `Degraded`
fingerprint evidence. The numeric update does not weaken those gates.

## Provenance artifact

Every CI run writes `artifacts/gauntlet-oracle/baseline-context.json` and uploads it with the
other oracle artifacts. It records:

- repository commit SHA;
- seed and tick count;
- SHA-256 of `assets/data/catalog/baltic_patrol.db`;
- SHA-256 and path of every staged policy;
- SHA-256 of the generated `results.csv`.

When a bound fails, compare this file with the last green run before changing an envelope. A
catalog or policy hash change is an explicit baseline-review signal; an unchanged input set
points instead toward code behavior, nondeterminism, or infrastructure.

## Review rule

Do not loosen a bound merely to make CI green. Recalibrate only after a real batch run explains
the movement, then record the observed values and preserve the fingerprint gates. Review the
baseline when any of these change:

- the catalog checksum or staged policy checksum;
- the seed or tick count;
- scenario ORBAT, ROE, EMCON, injects, or scoring behavior;
- deterministic simulation logic that affects kills, missiles, denials, or score.

For the full regeneration procedure, see
[`tools/qa-gauntlet/README-expect-regen.md`](../../tools/qa-gauntlet/README-expect-regen.md).
