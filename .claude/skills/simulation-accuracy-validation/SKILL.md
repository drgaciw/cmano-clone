---
name: simulation-accuracy-validation
description: "Validates simulation parameters against real-world data to ensure authenticity and identify discrepancies. Use for parameter validation, authenticity verification, or discrepancy identification."
argument-hint: "[parameter type | all]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, WebFetch, AskUserQuestion
model: sonnet
agent: simulation-parameter-analyst
---

# Simulation Accuracy Validation

This skill cross-references simulation parameters with authoritative sources to validate accuracy, identify discrepancies, and recommend adjustments for authenticity.

**Output:** Validation report with discrepancies and recommendations.

**Argument modes:**

**Validation mode:** `$ARGUMENTS[0]` (parameter type or system)

- **`all`** — Validate all simulation parameters
- **`weapon`** — Validate weapon system parameters
- **`platform`** — Validate platform capabilities
- **`sensor`** — Validate sensor systems
- **`ballistics`** — Validate ballistic and trajectory calculations
- **`engagement`** — Validate detection and engagement calculations

---

## Phase 1: Load Parameters

Emit one line before starting: `"Validating simulation parameters..."` -- this confirms the skill is running.

### Read simulation parameters
- Read parameter files from `assets/data/` and `design/balance/`
- Read relevant design documents from `design/gdd/`
- Note every file read -- they will appear in the Data Sources section

### Identify validation scope
- What parameter types or systems to validate?
- What level of tolerance is acceptable? (exact match, close approximation, gameplay-acceptable)
- How will discrepancies be handled? (auto-correct, flag for review, document only)

---

## Phase 2: Research Authoritative Sources

For each parameter or system being validated:

**Search authoritative sources:**
- Jane's Defense Publications
- Official military technical manuals
- Defense industry specifications
- Government defense databases (publicly available)
- Academic military research

**Gather real-world equivalents:**
- Exact specifications when available
- Performance ranges when exact values vary
- Operational characteristics
- Technical limitations and constraints

---

## Phase 3: Compare and Validate

Compare simulation parameters with real-world equivalents:

**Parameter validation checks:**
- Exact match: simulation value matches real-world value exactly
- Close approximation: simulation value within X% of real-world value
- Intentional deviation: simulation value differs for gameplay reasons
- Undocumented deviation: simulation value differs without documented rationale

**Identify discrepancies:**
- Exact value mismatches
- Missing parameters (real-world has value, simulation doesn't)
- Extra parameters (simulation has value, real-world doesn't)
- Out-of-range values (simulation value outside real-world range)

**Assess impact:**
- How does the discrepancy affect gameplay vs. authenticity?
- Is the discrepancy intentional or accidental?
- What are the trade-offs if corrected?

---

## Phase 4: Output Validation Report

```markdown
## Simulation Accuracy Validation: [Parameter Type or System]

### Data Sources Analyzed
- [List of files read]

### Validation Summary
| Parameter Type | Parameters Validated | Exact Matches | Close Approximations | Discrepancies |
|----------------|---------------------|---------------|----------------------|--------------|
| [category] | [N] | [X] | [Y] | [Z] |

### Health Summary: [HEALTHY / CONCERNS / CRITICAL ISSUES]

### Exact Matches
[Parameters that match real-world values exactly]

### Close Approximations
| Parameter | Real-World Value | Simulation Value | Deviation | Status |
|-----------|------------------|-------------------|-----------|--------|
| [parameter] | [real-world] | [simulation] | [X%] | [ACCEPTABLE] |

### Discrepancies
| Parameter | Real-World Value | Simulation Value | Deviation | Type | Impact |
|-----------|------------------|-------------------|-----------|------|--------|
| [parameter] | [real-world] | [simulation] | [X%] | [intentional/undocumented] | [gameplay/authenticity] |

### Missing Parameters
[Parameters that exist in real-world but not in simulation]

### Extra Parameters
[Parameters that exist in simulation but not in real-world]

### Recommendations
| Priority | Issue | Suggested Adjustment | Rationale | Impact |
|----------|-------|----------------------|-----------|--------|
| [High/Medium/Low] | [description] | [adjustment] | [why] | [gameplay/authenticity] |

### Sources
[Citations of authoritative sources used for validation]
```

---

## Phase 5: Present Summary and Ask to Write

Present a compact summary before writing:

```markdown
## Validation Summary
Parameters validated: [N]
Exact matches: [X] ([Y]%)
Close approximations: [M] ([Z]%)
Discrepancies: [P] ([Q]%)
Health: [HEALTHY / CONCERNS / CRITICAL ISSUES]
```

Before asking to write, show a **Discrepancy Preview**:
- List every discrepancy as a one-line bullet
- Show missing parameters and extra parameters counts
- Note any critical issues

Use `AskUserQuestion`:
- "Ready to write the validation report?"
  - "Yes — write to `design/balance/simulation-accuracy-[type]-[date].md`"
  - "Show me the full report preview first (don't write yet)"
  - "Cancel — I'll handle this manually"

---

## Phase 6: Write the Validation Report

If approved, write `design/balance/simulation-accuracy-[type]-[date].md` with the structure defined in Phase 4. Create the `design/balance/` directory if it does not exist. Use the current date for [date] in YYYY-MM-DD format.

Confirm the file was written, then end with: "Validation report saved. Review discrepancies and implement recommended adjustments as needed."

---

## Phase 7: Offer Next Steps

After writing the report, use `AskUserQuestion`:

- Prompt: "Validation complete. What would you like to do next?"
  - Options:
    - "Fix highest-priority discrepancies now — coordinate with `simulation-parameter-analyst`"
    - "Research missing parameters using `/military-database-research`"
    - "Validate another parameter type — specify weapon, platform, sensor, ballistics, engagement, or all"
    - "Stop here — I'll review the findings first"

---

## Collaborative Protocol

1. **Authenticity vs. gameplay trade-offs** -- deviations may be intentional for fun factor
2. **Document all deviations** -- intentional or not, note the rationale
3. **Source credibility matters** -- use authoritative sources only
4. **Tolerance varies by domain** -- some values must be exact, others can be approximate
5. **Assess impact carefully** -- how will corrections affect gameplay?
6. **Prioritize by impact** -- fix critical authenticity issues first