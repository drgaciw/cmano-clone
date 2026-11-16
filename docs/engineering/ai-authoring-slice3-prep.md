# AI Slice 3 authoring prep (DRG-763 / AI-S3-0)

**Status:** Prep only — specs + fixtures. Not an implementation of DRG-740/747.  
**Base:** `ab90063b` (`origin/main` tip at authoring).  
**Linear:** [DRG-763](https://linear.app/drgamtd-workspace/issue/DRG-763/ai-s3-0-slice-3-authoring-prep-specs-fake-client-fixtures-owner-tbd)

## Why this exists

Owner authorized Slice 3 **authoring prep** (specs, tickets, fixtures) while **DRG-740** / **DRG-747** remain `hold:owner-go` pending **ADR-015** ([DRG-727](https://linear.app/drgamtd-workspace/issue/DRG-727/ai-dec-1-owner-decision-adr-015-labelling-of-agent-authored-scenario)).

This document freezes the fake-client contract so DRG-740 can implement against repository fixtures without paid providers.

## Hard fences

| Fence | Rule |
| --- | --- |
| Paid providers | **Forbidden** in this prep and until a separate owner budget ack |
| LiveEval settings | **Do not inherit** — DRG-745 ledger is independent |
| Secrets | Never commit; never print |
| Data assembly | No provider SDK / AI packages in `ProjectAegis.Data` |
| Propose lane | Model output → typed proposed ops only; human Submit |
| Merge | Draft PR only; no merge without **drg** explicit approval |
| DRG-746 | Deferred (Surface + GitNexus on `OnScenarioFinalized`) |

## Owner-TBD ledger (authoring)

Unset until owner decides. Do **not** copy LiveEval choices.

| Field | Value |
| --- | --- |
| Provider | **owner-TBD** |
| Chat / authoring model | **owner-TBD** |
| Budget ceiling (USD) | **owner-TBD** |
| Credential store | **owner-TBD** (env / user-secrets only) |
| ADR-015 outcome | **owner-TBD** (Accept / Reject) — blocks ship of 740/747 |
| Allow paid switch | Not defined for authoring yet |

## Target host shape (future DRG-740 — not implemented here)

Proposed exclusive surface when hold clears:

- `src/ProjectAegis.Ai.Host/Authoring/**` (NEW)
- Tests: `src/ProjectAegis.Ai.Host.Tests/Authoring/**` (NEW)

Read-only dependencies (public APIs only):

- `AiAuthoringServices` keyword stubs remain fallback
- `ScenarioDocumentEditor`, `ScenarioManifest`, `ScenarioEditVersionGuard`
- `ScenarioValidationEngine`
- Fake / scripted chat client patterns from `Ai.Host` (AAR / FakeChat)

### Behaviour contract

1. **Input:** allowed unit/target IDs from open scenario, supported mission types, catalog refs, authoring schema.
2. **Output:** same result shape family as `AiAuthoringServices.NlScaffold`, converted to typed proposed operations on the **Propose** lane.
3. **Validation:** run `ScenarioValidationEngine` before surfacing.
4. **Idempotency:** each proposal carries an idempotency key; retry of an accepted proposal does not apply twice.
5. **Stale revision:** refuse when `expectedEditVersion != currentEditVersion` (`ScenarioEditVersionGuard.ConflictCode`).
6. **Provenance:** every applied op carries `ProvenanceTag` with `Source = "ai"`. Player-facing label follows ADR-015 once decided (not assumed here).
7. **Untrusted text:** scenario/unit strings are data, never executable instructions (`embedded-instructions` fixture).

## Fixture pack

Path: [`tests/ai-fixtures/authoring/`](../../tests/ai-fixtures/authoring/)

| Case id | Expected outcome class |
| --- | --- |
| `valid-draft` | Valid proposed draft + diagnostics empty / non-blocking |
| `unknown-ids` | Actionable diagnostics; no silent invent of IDs |
| `ambiguous-request` | Clarification / refuse-to-guess diagnostics |
| `unsupported-role` | Role capability refusal (mission role not allowed) |
| `duplicate-op` | Second apply with same idempotency key is no-op / refused |
| `stale-revision` | `CONFLICT` / clear stale message |
| `embedded-instructions` | Treat injection text as data; do not follow |

Content class: repository-public synthetic Baltic-style IDs (`u1`, `hostile-1`). Track A / proprietary `*.db3` **forbidden**.

## Ticket map

| Issue | Role |
| --- | --- |
| DRG-763 | This prep (specs + fixtures) |
| DRG-740 | Host Authoring implementation — **held** |
| DRG-747 | CLI `--provider` / diff — **held**, blocked on 740 |
| DRG-727 | ADR-015 decision — **required before ship** |
| DRG-746 | Hindsight — **deferred** |
| DRG-745 | LiveEval — scaffold Done; paid path separate owner-TBD |

## Acceptance for this prep PR

1. Spec + fixture set land; consumers can load cases without host Authoring code.
2. Fixtures are repository-public; no Track A material.
3. Owner-TBD ledger lists provider, model, budget, credentials, ADR-015 as unset.
4. No merge without **drg**.
