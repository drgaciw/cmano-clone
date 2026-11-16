# Retrieval pilot (DRG-741)

Throwaway LiveEval lane comparing **keyword** lookup vs **offline/fake concept embeddings** on one fixture query set.

## Hard fences

- **No paid providers / embedding APIs / live keys**
- LiveEval stays **outside** `ProjectAegis.sln`
- Track A / `*.db3` sources refused before index
- Embedded-instruction documents are indexed as **data**
- Read-only on catalog / `DatabaseIntelligenceOrchestrator` (no write-gate calls)

## Fixtures

`tests/ai-fixtures/retrieval/**` — corpus, queries, synonyms, Track A refuse sample, revision bump.

## Run

```bash
dotnet test src/ProjectAegis.Ai.LiveEval/ProjectAegis.Ai.LiveEval.csproj --filter RetrievalPilot
```

## Gate (DRG-729 Accepted B Balanced)

| Metric | Threshold |
| --- | --- |
| ΔRecall@5 vs keyword | ≥ +0.10 |
| CitationPrecision@5 | ≥ 0.85 |
| k | 5 |

`N < ~40` → provisional pass only.

Offline embeddings here are deterministic concept vectors + a fixture synonym map — not a production vector store.
