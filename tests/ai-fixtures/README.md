# AI fixtures (DRG-732 / Wave 0-C)

Machine-readable order-log fixtures for evidence-linked AI acceptance.
Expected facts and evidence IDs are stored **separately** from any prose.

## Provenance

| Field | Value |
| --- | --- |
| Produced for | DRG-732 |
| Base commit | `7109a8a25e1fa02842d3d85d9740e0321a3a2399` (`origin/main` tip at fixture authoring) |
| Seed namespace | `20261115` (per-case seeds = `$SEED_BASE` + case ordinal) |
| Content class | Repository-public synthetic Baltic-style unit IDs (`u1`, `hostile-1`) only |
| Track A / `*.db3` | **Forbidden** — none present |
| Baltic v2 goldens | **Untouched** — do not modify `tests/regression/replay-golden-baltic*` |

## Recipe

1. Each case folder holds:
   - `case.json` — identity, seed, purpose, viewing-side defaults
   - `order-log.json` — chronological synthetic entries (`sequenceId`, `kind`, `simTime`, `side`, payload fields)
   - `expected-facts.json` — authoritative numbers/outcomes the host must preserve
   - `expected-evidence-ids.json` — IDs of form `{fingerprintPrefix}:{sequenceId}`
2. Fingerprint prefixes are **synthetic** (`aifix-<caseId>`) for Wave 0. Later evidence-export (DRG-736) may regenerate logs through public harness APIs and refresh prefixes without editing Baltic goldens.
3. Consumers must treat all scenario/unit text as **untrusted data** (see `embedded-instructions` and `conflicting-sources`).
4. Side filter: use `viewingSide` before prompt construction; `two-sided-hidden` encodes red-only rows that must not appear for blue.

## Cases

See `manifest.json`.
