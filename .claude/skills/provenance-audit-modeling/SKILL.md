---
name: provenance-audit-modeling
description: "Model field-level provenance and audit trails for ProjectAegis.Data: source facts, interpreted values, gameplay abstractions, confidence, reviewer, TL/TRL gates, reversible changes, and human approval states."
argument-hint: "[entity|schema|diff-bundle|full]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Bash, Task, AskUserQuestion
model: sonnet
agent: database-modeler
---

# Provenance and Audit Modeling

Use this skill when adding or reviewing any Project Aegis database entity that
represents military facts, interpreted values, or gameplay abstractions.

## Phase 1: Classify the Data

For each field classify it as:

- `source_fact` — directly cited value.
- `interpreted_value` — analyst/agent inference from evidence.
- `gameplay_abstraction` — simplified simulation parameter.

Fields may not silently cross categories. If a gameplay abstraction is derived from
source facts, model the derivation link.

## Phase 2: Provenance Requirements

Every persisted field or value group must capture:

- Source/citation ID or explicit "unknown" reason.
- Confidence score and confidence basis.
- Actor identity and actor type.
- Reviewer and review status.
- Revision date as audit metadata.
- TL and TRL gate where relevant.
- Rationale for normalization or abstraction.

## Phase 3: Audit and Rollback Model

Require append-only change records with:

- Batch/transaction ID.
- Before/after values.
- Schema version and DB release version.
- Approval state: proposed, approved, rejected, superseded, rolled_back.
- Rollback target and dependency warnings.

## Phase 4: Agent Workflow Gate

Validate Requirement 06's workflow:

retrieval → entity resolution → diff proposal → rules validation → human review.

No agent output becomes canonical data until approved. Large normalizations require
human confirmation unless within a documented tolerance band.

## Output

Produce a provenance model table and identify any fields missing evidence,
confidence, review, TL/TRL, or rollback support.
