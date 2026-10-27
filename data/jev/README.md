# Jev rubrics (`data/jev/`)

Pinned System One question packs for **off-tick advisory** classification only.

| File | Use |
|------|-----|
| [`uat-v1.questions.json`](uat-v1.questions.json) | Unity UAT evidence packet (C2 chrome + Baltic classify play-through) |

## Thresholds (`uat-v1`)

| Primitive | Rule |
|-----------|------|
| **Choice** (`failure_class`) | Confidence **&lt; 0.5** → human review; do not auto-tag. |
| **Noul** (`needs_code_change`) | Probability *is* the uncertainty — no second confidence field; do not reuse the Choice floor. |
| **Score** (`severity`) | Rank / threshold only; do not treat interpolated values as linear severity. |

Always include Choice escape options (`none`, `other`) so the model is never forced into a wrong closed enum.

## Invariants

- Never call from `Tick`, `DecisionPipeline`, ROE, or `DecisionLog`.
- Never override human PASS/FAIL on the UAT table.
- Do not put screenshots, catalogs, coordinates, secrets, or proprietary CMO `.db3` extracts in `state`.
- The rubric targets `jev-1.13.0`. Use the TypeSafe route for that explicit pin;
  Cloudflare and Vercel use moving provider aliases (see the CLI routing table).

CLI: [`tools/jev/`](../../tools/jev/README.md).
