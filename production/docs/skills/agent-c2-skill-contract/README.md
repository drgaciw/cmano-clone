# Agent-callable C2 skills (DRG-196)

Slice A contract for AGC-01 through AGC-04. Authorized models discover skills from `catalog.json`, fill the envelope in `envelopes/skill-envelope.schema.json`, and obey `CONTRACT.md`.

| Path | Role |
| --- | --- |
| `CONTRACT.md` | Lanes, authority, override, provenance |
| `catalog.json` | Discoverable skill list |
| `envelopes/skill-envelope.schema.json` | Mechanical envelope |
| `TEST-SPEC.md` | Retrieval / compliance cases |
| `verify-contract.ps1` | Headless assertion gate |
| `PROOF.md` | Isolation proof vs `origin/main` |
| `../c2-track-assessment/SKILL.md` | `c2.track.assess` |
| `../c2-datalink-reasoning/SKILL.md` | `c2.datalink.reason` |
| `../c2-sensor-to-shooter-pairing/SKILL.md` | `c2.pairing.recommend` |
| `../c2-explanation/SKILL.md` | `c2.explain` |

Implementer entry: `.claude/skills/agent-c2-skill-contract/SKILL.md`.

Read vs propose vs submit is three lanes, not three moods of one method. Details in `CONTRACT.md`.
