# Epic: Order Log Replay Checkpoints Slice

> **Status:** MVP Complete (harness checkpoints + CLI)  
> **Priority:** P1 — deepens replay leg  
> **Created:** 2026-06-02  
> **Depends on:** `WORLD_HASH` slice (complete), Baltic harness CLI  
> **GDD:** [order-log-replay.md](../../design/gdd/order-log-replay.md)  
> **Design blocker:** Requirements review **C1** (DecisionLog vs full order log union)

## Goal

Checkpoint store + scrub-to-tick API: canonical hash payload scope, interval from scenario data, CLI `REPLAY_CHECKPOINT=` for gate.

## Acceptance

1. Checkpoints emitted every N ticks (config-driven).
2. Harness can restore state hash at checkpoint boundary.
3. Golden baseline updated; `/replay-verify` PASS.