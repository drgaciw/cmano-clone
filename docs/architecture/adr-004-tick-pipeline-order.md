# ADR-004: Deterministic Tick Pipeline Ordering

**Status:** Accepted  
**Date:** 2026-05-29

## Context

Detection, policy, delegation, and engagement must not reorder arbitrarily between runs.

## Decision

Adopt fixed pipeline in `architecture.md` § Tick Pipeline. Detection before `ObservedState`; policy before engagement; logging last.

## Consequences

- All systems document their step number.
- CI golden scenario asserts step-boundary hashes (optional per step).
