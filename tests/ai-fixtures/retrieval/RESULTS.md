## DRG-741 retrieval pilot results

| Retriever | Recall@5 | CitationPrecision@5 | N |
| --- | ---: | ---: | ---: |
| keyword | 0.643 | 0.476 | 42 |
| offline-embedding | 1.000 | 0.937 | 42 |

| Gate (Accepted B Balanced) | Value |
| --- | --- |
| ΔRecall@5 (need ≥ +0.10) | 0.357 (pass) |
| CitationPrecision@5 (need ≥ 0.85) | 0.937 (pass) |
| Query count | 42 (not provisional) |
| Overall | PASS |
| Paid path avoided | yes |
| Track A refused | `Track A / proprietary-derived source refused for LiveEval egress: trackA` |
| Revision invalidation | yes (`e74ccebc5d7e…` → `9f916fd72261…`) |
| Embedded-instruction as data | yes |

PASS: offline embedding meets Accepted B Balanced thresholds.
