# Epic: EW Noise Jam Headless Slice

> **Status:** Complete  
> **Created:** 2026-06-02  
> **Depends on:** [pd-detection-loop](../pd-detection-loop/EPIC.md)

## Goal

Scenario **noise jamming** reduces detection Pd via `jamStrength`; prove deterministic suppress / allow detect in replay.

## Acceptance

1. `jammers[]` in scenario JSON applies to sorted Pd trials.
2. High jam → no `ContactChange`; zero jam → detect (basePd 1).
3. Detection tick hash stable across runs.
4. 120+ tests green.