# Ai.Host Catalog (DRG-748 / AI-S4-2)

Evidence-fed `PlatformDesignAssistant` briefs staged through **additive** CatalogWriteGate `Propose*` overloads.

| Item | Value |
| --- | --- |
| Paid providers | **Forbidden** (offline/fake evidence only) |
| Track A / `*.db3` | **Refused** |
| Embedded instructions | **Rejected** (evidence ≠ instructions) |
| Viewing-side isolation | Enforced before brief build |
| Write path | New `Propose*` / `Approve*` overloads only; existing bodies untouched |
| Approve | Human only — `SubmitAllowedWithoutHuman` is always false |
| PlatformDesignAssistant | Read-only consumer (not edited) |

Fixtures: inline Host.Tests + optional `tests/ai-fixtures/retrieval/**` (read-only).
