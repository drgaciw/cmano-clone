# Ai.Host Authoring (DRG-740 / AI-S3-1)

Propose-lane mission authoring with **fake/scripted** model drafts only.

| Item | Value |
| --- | --- |
| Paid providers | **Forbidden** until owner-TBD ledger is set |
| Provider / model / budget | **owner-TBD** (do not inherit LiveEval) |
| Data stubs | `AiAuthoringServices` remains keyword fallback — not edited |
| ADR-015 | Accepted — Propose results carry `agent-assisted` transparency label |
| Submit | Never automatic; `SubmitAllowedWithoutHuman` is always false |

Fixtures: `tests/ai-fixtures/authoring/**`.
