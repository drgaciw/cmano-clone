# Agentic Infrastructure (AAR, Batch, Memory)

> **Status:** Draft — Sprint 8  
> **Last Updated:** 2026-06-02  
> **Requirements:** [07-Agentic-Infrastructure.md](../../Game-Requirements/requirements/07-Agentic-Infrastructure.md)  
> **Depends on:** [order-log-replay.md](order-log-replay.md), [simulation-core-time.md](simulation-core-time.md)

## Overview

Headless and editor tooling for **agent-vs-agent** research: batch scenario runs, CSV score export, optional Hindsight memory banks, and AAR agents that read order logs—not live sim state.

## Player Fantasy

Designers and AI agents replay the same fight the player saw, with every point and denial traced to `sequenceId`.

## Detailed Rules

### Batch runner (P0)

- Input: scenario id list, seed grid, tick count.
- Output: `LossesScoringCsvExporter` rows + fingerprint per run.

### Hindsight memory (P1)

- Optional hook on `DecisionLog` append (shipped behind config).
- Banks: dev-memory, balance-tuning (see `tools/hindsight/`).

### AAR agents (P1)

- Read-only access to order log + message log projections.
- No mutation of sim or delegation state during AAR.

## Formulas

Batch throughput target: `runsPerMinute >= 60` for Baltic 6-tick headless on dev hardware.

## Edge Cases

- Hindsight disabled: fingerprints unchanged.
- Batch partial failure: emit CSV row with `score=-1` and error column (P1).

## Dependencies

- Order log & replay, scoring-losses, delegation bridge.

## Tuning Knobs

| Knob | Effect |
|------|--------|
| `ticks` | Replay depth |
| Hindsight retain cadence | Memory write volume |

## Acceptance Criteria

- [x] CSV export row on `BalticReplayHarness.Result`
- [ ] Batch CLI documents 10+ scenarios (P1)
- [ ] AAR skill produces diff report vs golden fingerprint (P1)