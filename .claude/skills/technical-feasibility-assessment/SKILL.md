---
name: technical-feasibility-assessment
description: "Assesses technical feasibility of proposed simulation features, including performance implications, implementation complexity, and platform compatibility. Use for feature feasibility, constraint impact analysis, or scalability testing guidance."
argument-hint: "[feature | system | requirement]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash, WebFetch, AskUserQuestion
model: sonnet
agent: technical-constraints-analyst
---

# Technical Feasibility Assessment

This skill evaluates technical constraints, performance implications, and implementation complexity for proposed military simulation features.

**Output:** Feasibility assessment with risk analysis and recommendations.

**Argument modes:**

**Assessment mode:** `$ARGUMENTS[0]` (feature, system, or requirement)

- **Specific feature** — Assess feasibility of a specific feature or mechanic
- **System** — Assess feasibility of an entire system
- **Requirement** — Assess feasibility of a design requirement or constraint

---

## Phase 1: Analyze Proposed Feature

Emit one line before starting: `"Assessing technical feasibility..."` -- this confirms the skill is running.

### Read relevant documents
- Read design documents from `design/gdd/` for the feature/system
- Read existing architecture documents if available
- Read technical preferences and constraints
- Note every file read -- they will appear in the Data Sources section

### Understand requirements
- What is the feature or system being proposed?
- What are the functional requirements?
- What are the non-functional requirements? (performance, memory, compatibility)
- What are the constraints and dependencies?

---

## Phase 2: Identify Technical Constraints

Analyze technical constraints systematically:

**Performance constraints:**
- Computational complexity (Big O analysis)
- Per-frame cost (fixed vs. variable)
- CPU vs. GPU utilization
- Real-time requirements (frame budget, latency)
- Worst-case scenarios (entity count, interaction complexity)

**Memory constraints:**
- Memory footprint (static vs. dynamic allocation)
- Allocation patterns and rates
- Memory bandwidth requirements
- Streaming and loading strategies
- Platform-specific memory limits

**Platform constraints:**
- Hardware limitations (CPU, GPU, memory)
- OS and API restrictions
- Input device capabilities
- Display limitations (resolution, refresh rate)

**Scalability constraints:**
- Entity count scalability
- Map size scalability
- Duration scalability
- Multiplayer synchronization overhead

---

## Phase 3: Estimate Complexity and Risk

**Implementation complexity:**
- New code vs. modification vs. integration
- Number of systems affected
- Dependency depth and breadth
- Cross-team coordination required
- Testing and validation scope

**Risk assessment:**
- **Technical risks:** Unknown technical challenges, API limitations, performance issues
- **Integration risks:** Conflicts with existing systems, breaking changes
- **Schedule risks:** Dependencies on other work, underestimation
- **Scope risks:** Feature creep, requirements changing during development

---

## Phase 4: Output Feasibility Assessment

```markdown
## Technical Feasibility Assessment: [Feature or System]

### Data Sources Analyzed
- [List of files read]

### Feasibility Summary
| Dimension | Assessment | Impact | Status |
|-----------|------------|--------|--------|
| Performance | [High/Medium/Low impact] | [description] | [PASS/CONCERN/FAIL] |
| Memory | [High/Medium/Low impact] | [description] | [PASS/CONCERN/FAIL] |
| Platform | [High/Medium/Low impact] | [description] | [PASS/CONCERN/FAIL] |
| Scalability | [High/Medium/Low impact] | [description] | [PASS/CONCERN/FAIL] |

### Overall Feasibility Rating: [FEASIBLE / CHALLENGING / NOT FEASIBLE]

### Performance Analysis
| Aspect | Complexity | Per-Frame Cost | Impact | Status |
|--------|------------|----------------|--------|--------|
| [aspect] | [Big O] | [cost] | [description] | [PASS/CONCERN/FAIL] |

### Memory Analysis
| Aspect | Footprint | Allocation Pattern | Impact | Status |
|--------|----------|-------------------|--------|--------|
| [aspect] | [size] | [pattern] | [description] | [PASS/CONCERN/FAIL] |

### Platform Compatibility
| Platform | Constraints | Workarounds | Status |
|----------|------------|-------------|--------|
| [platform] | [constraints] | [workarounds] | [PASS/CONCERN/FAIL] |

### Scalability Assessment
| Dimension | Scaling Behavior | Bottleneck | Status |
|-----------|------------------|------------|--------|
| [dimension] | [linear/non-linear] | [bottleneck] | [PASS/CONCERN/FAIL] |

### Implementation Complexity
| Factor | Assessment | Impact |
|--------|------------|--------|
| Code type | [new/modify/integration] | [description] |
| Systems affected | [N] systems | [list] |
| Dependencies | [description] | [description] |
| Coordination needed | [description] | [description] |
| Testing scope | [description] | [description] |

### Risk Assessment
| Category | Risk | Likelihood | Impact | Mitigation |
|----------|------|------------|--------|------------|
| [category] | [description] | [High/Medium/Low] | [High/Medium/Low] | [strategy] |

### Recommendations
| Priority | Issue | Suggested Approach | Rationale |
|----------|-------|-------------------|-----------|
| [High/Medium/Low] | [description] | [approach] | [why] |

### Feasibility Verdict
**[FEASIBLE / CHALLENGING / NOT FEASIBLE]**

If CHALLENGING: [explain challenges and required workarounds]
If NOT FEASIBLE: [explain why and suggest alternatives]
```

---

## Phase 5: Present Summary and Ask to Write

Present a compact summary before writing:

```markdown
## Feasibility Assessment Summary
Overall rating: [FEASIBLE / CHALLENGING / NOT FEASIBLE]
Performance: [PASS/CONCERN/FAIL]
Memory: [PASS/CONCERN/FAIL]
Platform: [PASS/CONCERN/FAIL]
Scalability: [PASS/CONCERN/FAIL]
High-priority risks: [N]
```

Before asking to write, show a **Risk Preview**:
- List every high-priority risk as a one-line bullet
- Show any FAIL statuses
- Note any critical constraints

Use `AskUserQuestion`:
- "Ready to write the feasibility assessment?"
  - "Yes — write to `docs/technical/feasibility-[feature]-[date].md`"
  - "Show me the full assessment preview first (don't write yet)"
  - "Cancel — I'll handle this manually"

---

## Phase 6: Write the Feasibility Assessment

If approved, write `docs/technical/feasibility-[feature]-[date].md` with the structure defined in Phase 4. Create the `docs/technical/` directory if it does not exist. Use the current date for [date] in YYYY-MM-DD format.

Confirm the file was written, then end with: "Feasibility assessment saved. Review findings and adjust design as needed."

---

## Phase 7: Offer Next Steps

After writing the assessment, use `AskUserQuestion`:

- Prompt: "Feasibility assessment complete. What would you like to do next?"
  - Options:
    - "Address highest-priority risks — coordinate with `technical-director`"
    - "Assess alternative approaches — iterate on the design"
    - "Create requirements impact analysis — run `/military-requirements-impact`"
    - "Stop here — I'll review the findings first"

---

## Collaborative Protocol

1. **Feasibility is not a binary** — challenging is different from infeasible
2. **Constraints are real** — don't ignore hardware or platform limitations
3. **Performance budgets matter** — frame time is a non-negotiable constraint
4. **Scalability is critical** — consider worst-case scenarios, not average cases
5. **Risk management is key** — identify and mitigate early, not late
6. **Alternatives exist** — if an approach isn't feasible, suggest others that are