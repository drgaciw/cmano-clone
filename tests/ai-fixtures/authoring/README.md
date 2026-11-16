# AI authoring fixtures (DRG-763 / AI-S3-0 prep)

Fake-client / scripted-response fixtures for Slice 3 **host authoring** acceptance.
No paid providers. Provider/model/budget/credentials remain **owner-TBD**.

## Provenance

| Field | Value |
| --- | --- |
| Produced for | DRG-763 (prep for DRG-740) |
| Spec | `docs/engineering/ai-authoring-slice3-prep.md` |
| Base commit | `ab90063b4adfa074ed7873765daf3cba905bdf51` |
| Seed namespace | `20261116` (per-case seeds = `$SEED_BASE` + ordinal) |
| Content class | Repository-public synthetic Baltic-style IDs (`u1`, `hostile-1`) |
| Track A / `*.db3` | **Forbidden** |
| Baltic v2 goldens | **Untouched** |

## Recipe

Each case folder holds:

| File | Role |
| --- | --- |
| `case.json` | Identity, seed, purpose, open-scenario pin, expected outcome class |
| `request.json` | Natural-language brief + allowed IDs + expected edit version |
| `provider-script.json` | Fake/scripted model body (or refuse) — **no live provider** |
| `expected.json` | Valid draft shape **or** actionable diagnostics / conflict code |

Consumers must treat all scenario/unit text as **untrusted data** (see `embedded-instructions`).

Sibling evidence fixtures under `tests/ai-fixtures/` (Wave 0 order-logs) are **orthogonal** — do not mix schemas.

## Cases

See `manifest.json`.
