# Hard-case reflective replay pool

Policies or defect signatures that found **real** pressure (`sim-code` /
useful `scenario-data`, or rare coverage edges). Forge `--phase e` and `pre`
re-sample 1–2 entries (prefer hard + rare) into the next tier plan.

## Layout

- `*.policy.json` — optional snapshot of a hard policy (or symlink note to
  `data/scenarios/…`)
- `*.signature.md` — short defect/scenario signature for replay without full JSON

## Bootstrap

Empty at forge v1 seed (2026-07-23). Populate via `/qa-gauntlet-forge`
`--phase post-oracle` when a useful fail is triaged.

Do **not** store secrets. Do **not** reference DelegationBridge patches here.
