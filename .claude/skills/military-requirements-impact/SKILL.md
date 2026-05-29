---
name: military-requirements-impact
description: "Analyzes impact of proposed military simulation requirements on existing systems, architecture, and performance. Identifies dependencies and conflicts. Use for requirements impact analysis, dependency identification, or conflict resolution."
argument-hint: "[requirement | change]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash, WebFetch, AskUserQuestion
model: sonnet
agent: requirements-analyst
---

# Military Requirements Impact

This skill evaluates how proposed military simulation requirements affect existing game systems, architecture, and performance, identifying dependencies and conflicts.

**Output:** Impact analysis report with affected systems and recommendations.

**Argument modes:**

**Impact mode:** `$ARGUMENTS[0]` (requirement or change)

- **Specific requirement** — Analyze impact of a specific requirement
- **Proposed change** — Analyze impact of a proposed change to existing requirements
- **System** — Analyze impact of adding or modifying a system

---

## Phase 1: Parse Proposed Requirement

Emit one line before starting: `"Analyzing requirements impact..."` -- this confirms the skill is running.

### Understand the proposal
- What is the requirement or change being proposed?
- What are the functional requirements?
- What are the non-functional requirements? (performance, memory, compatibility)
- What are the acceptance criteria?

### Read relevant documents
- Read design documents from `design/gdd/` for related systems
- Read existing architecture documents if available
- Read technical preferences and constraints
- Note every file read -- they will appear in the Data Sources section

---

## Phase 2: Identify Affected Systems

**System identification:**
- Which systems are directly affected? (primary impact)
- Which systems are indirectly affected? (secondary impact)
- Which systems are potentially affected? (tertiary impact)
- Which systems are unaffected? (to provide context)

**Dependency analysis:**
- What are the dependencies on this requirement?
- What depends on this requirement?
- Are there circular dependencies?
- What are the integration points?

---

## Phase 3: Assess Technical Impact

**Performance impact:**
- Computational complexity impact (CPU, GPU)
- Memory footprint impact
- I/O and network requirements impact
- Real-time constraints impact

**Architecture impact:**
- Does this require architectural changes?
- Does this introduce new systems or components?
- Does this change existing system boundaries?
- Does this affect data flow or communication patterns?

**Integration impact:**
- What integration points are affected?
- Are there API changes required?
- Are there data format changes required?
- Are there protocol changes required?

**Scalability impact:**
- Does this affect entity count scalability?
- Does this affect map size scalability?
- Does this affect duration scalability?
- Does this affect multiplayer synchronization?

---

## Phase 4: Identify Conflicts and Risks

**Conflicts:**
- Does this conflict with existing requirements?
- Does this conflict with technical constraints?
- Does this conflict with architecture decisions?
- Does this conflict with performance budgets?

**Risks:**
- **Technical risks:** Unknown technical challenges, API limitations, performance issues
- **Integration risks:** Conflicts with existing systems, breaking changes
- **Schedule risks:** Dependencies on other work, underestimation
- **Scope risks:** Requirements changing during development, feature creep

---

## Phase 5: Output Impact Analysis Report

```markdown
# Requirements Impact Analysis: [Requirement or Change]

### Data Sources Analyzed
- [List of files read]

### Impact Summary
| Dimension | Impact Level | Affected Systems | Status |
|-----------|--------------|------------------|--------|
| Technical | [High/Medium/Low] | [list] | [OK/CONCERN/BLOCKING] |
| Architecture | [High/Medium/Low] | [list] | [OK/CONCERN/BLOCKING] |
| Integration | [High/Medium/Low] | [list] | [OK/CONCERN/BLOCKING] |
| Performance | [High/Medium/Low] | [list] | [OK/CONCERN/BLOCKING] |

### Overall Impact: [HIGH / MEDIUM / LOW]

### Affected Systems

#### Primary Impact (Direct)
| System | Impact Type | Description | Priority |
|--------|-------------|-------------|----------|
| [system] | [new/modify/deprecate] | [description] | [High/Medium/Low] |

#### Secondary Impact (Indirect)
| System | Impact Type | Description | Priority |
|--------|-------------|-------------|----------|
| [system] | [new/modify/deprecate] | [description] | [High/Medium/Low] |

#### Tertiary Impact (Potential)
| System | Impact Type | Description | Priority |
|--------|-------------|-------------|----------|
| [system] | [new/modify/deprecate] | [description] | [High/Medium/Low] |

### Dependencies
| Dependency | Type | Description | Impact |
|------------|------|-------------|--------|
| [dependency] | [depends-on/depended-by] | [description] | [High/Medium/Low] |

### Integration Points
| Integration Point | Change Required | Complexity | Risk |
|-------------------|-----------------|------------|------|
| [point] | [change] | [High/Medium/Low] | [High/Medium/Low] |

### Performance Impact
| Aspect | Current | Proposed | Change | Impact |
|--------|---------|----------|--------|--------|
| [aspect] | [current] | [proposed] | [change] | [impact] |

### Architecture Impact
- [architectural changes required]
- [new systems or components]
- [system boundary changes]
- [data flow or communication pattern changes]

### Conflicts
| Conflict | Type | Impact | Resolution |
|----------|------|--------|------------|
| [conflict] | [technical/architecture/performance] | [High/Medium/Low] | [resolution] |

### Risks
| Category | Risk | Likelihood | Impact | Mitigation |
|----------|------|------------|--------|------------|
| [category] | [description] | [High/Medium/Low] | [High/Medium/Low] | [strategy] |

### Recommendations
| Priority | Recommendation | Rationale | Impact |
|----------|----------------|-----------|--------|
| [High/Medium/Low] | [recommendation] | [why] | [impact] |

### Affected Design Documents
| Document | Impact | Action Required |
|----------|--------|-----------------|
| [document] | [High/Medium/Low] | [action] |

### Affected ADRs (Architecture Decision Records)
| ADR | Impact | Action Required |
|-----|--------|-----------------|
| [ADR] | [High/Medium/Low] | [action] |
```

---

## Phase 6: Present Summary and Ask to Write

Present a compact summary before writing:

```markdown
## Impact Analysis Summary
Overall impact: [HIGH / MEDIUM / LOW]
Systems affected: [N] (Primary: [X], Secondary: [Y], Tertiary: [Z])
Dependencies: [N]
Integration points: [N]
Conflicts: [N]
High-priority recommendations: [N]
```

Before asking to write, show a **Impact Preview**:
- List every primary affected system
- Show conflicts and high-priority recommendations
- Note any blocking issues

Use `AskUserQuestion`:
- "Ready to write the impact analysis report?"
  - "Yes — write to `docs/requirements/impact-[requirement]-[date].md`"
  - "Show me the full analysis preview first (don't write yet)"
  - "Cancel — I'll handle this manually"

---

## Phase 7: Write the Impact Analysis Report

If approved, write `docs/requirements/impact-[requirement]-[date].md` with the structure defined in Phase 5. Create the `docs/requirements/` directory if it does not exist. Use the current date for [date] in YYYY-MM-DD format.

Confirm the file was written, then end with: "Impact analysis saved. Address conflicts and implement recommendations as needed."

---

## Phase 8: Offer Next Steps

After writing the report, use `AskUserQuestion`:

- Prompt: "Impact analysis complete. What would you like to do next?"
  - Options:
    - "Address highest-priority conflicts — coordinate with `technical-director`"
    - "Update affected design documents — run `/propagate-design-change`"
    - "Create new ADRs if needed — run `/architecture-decision`"
    - "Stop here — I'll review the findings first"

---

## Collaborative Protocol

1. **Impact is multidimensional** — technical, architectural, integration, and performance all matter
2. **Dependencies flow both ways** — what depends on this and what this depends on
3. **Conflicts must be resolved** — don't proceed without addressing blocking conflicts
4. **Risks must be mitigated** — identify and plan for risks early
5. **Integration points are critical** — changes here have ripple effects
6. **Architecture matters** — requirements changes can have architectural implications