# Architecture docs

Living architecture for Project Aegis. Start with [architecture.md](architecture.md) (assembly map + ADR index through ADR-031) and [architecture-traceability-index.md](architecture-traceability-index.md).

| ADR | Title | Status |
|-----|-------|--------|
| [ADR-031](adr-031-one-ci-system-github-actions-hosted.md) | One CI system: GitHub Actions hosted; Buildkite retired after parity | **Proposed** |

## ADR status-change convention (DRG-771 / CMA-28)

**ADR Status changes go in their own docs-only PR, never inside a feature PR, and never with backdating.**

| Rule | Detail |
|------|--------|
| Separate PR | Flip `Proposed` → `Accepted` / `Deferred` / `Superseded` / `Rejected` only in a PR whose Surface is ADR markdown (and optional index/README pointers). Do not bury Status edits inside code, test, or Unity feature PRs. |
| No backdating | Record the actual decision date (CT) when Status changes. Do not rewrite historical Status lines to look accepted earlier than the decision. |
| Index sync | When Status changes, update the matching row in [architecture.md](architecture.md) § ADR Index in the **same** docs-only PR (or an immediately following docs-only PR). Do not leave the index stale across a Status flip. |
| Traceability | Prefer a one-line note in [architecture-traceability-index.md](architecture-traceability-index.md) when an ADR gains or loses coverage weight; still docs-only. |

Recorded 2026-11-16 under owner go *"Accept jev's calls"* (DRG-771 §8 item 5 / Lane A). This file is the home for the convention; feature lanes must not restate or weaken it.
