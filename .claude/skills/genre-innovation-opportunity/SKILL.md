---
name: genre-innovation-opportunity
description: "Researches genre conventions and market gaps to identify opportunities for innovation and differentiation in military simulation gameplay. Use for innovation identification, differentiation strategy, or market positioning."
argument-hint: "[focus: mechanics | systems | ui | all]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, WebFetch, AskUserQuestion
model: sonnet
agent: simulation-genre-analyst
---

# Genre Innovation Opportunity

This skill researches genre conventions and market gaps to identify opportunities for innovation and differentiation in military simulation gameplay.

**Output:** Innovation opportunity analysis with prioritized recommendations.

**Argument modes:**

**Analysis focus:** `$ARGUMENTS[0]` (focus area)

- **`mechanics`** — Analyze gameplay mechanics for innovation opportunities
- **`systems`** — Analyze game systems for innovation opportunities
- **`ui`** — Analyze UI/UX for innovation opportunities
- **`all`** — Comprehensive analysis across all dimensions

---

## Phase 1: Analyze Current Genre Conventions

Emit one line before starting: `"Analyzing genre innovation opportunities..."` -- this confirms the skill is running.

### Genre convention analysis
- What are the standard conventions in the genre?
- What are the established mechanics and systems?
- What are the standard UI/UX patterns?
- What do players expect from games in this genre?

---

## Phase 2: Identify Underserved Needs

Research player needs and desires:

**Community feedback:**
- What are players asking for that games don't provide?
- What are common complaints about existing games?
- What features are consistently requested?
- What frustrations do players express?

**Market analysis:**
- What market gaps exist?
- What player segments are underserved?
- What needs are unmet by current offerings?

**Emerging technologies:**
- What new technologies could enable new gameplay?
- What emerging techniques could be applied?
- What hardware or software advances are relevant?

---

## Phase 3: Brainstorm Innovative Approaches

Brainstorm innovative approaches aligned with genre:

**Mechanic innovations:**
- What mechanics don't exist yet?
- How can existing mechanics be improved or combined?
- What new player experiences can be created?
- How can authenticity and fun be balanced in new ways?

**System innovations:**
- What systems don't exist yet?
- How can existing systems be integrated or expanded?
- What new simulation systems could be added?
- How can systems be made more dynamic or responsive?

**UI/UX innovations:**
- What UI/UX challenges remain unsolved?
- How can information density be managed better?
- How can command complexity be reduced?
- How can learning curves be improved?

---

## Phase 4: Evaluate Feasibility and Differentiation

**Feasibility assessment:**
- Is this technically feasible given our constraints?
- What are the implementation requirements?
- What are the technical risks?
- What are the resource requirements?

**Differentiation assessment:**
- How unique is this innovation?
- Would competitors have difficulty copying this?
- Would players value this innovation?
- Does this align with our unique value proposition?

**Market potential:**
- Is there market demand for this innovation?
- How would this be received by players?
- Would this attract new players to the genre?
- Would this differentiate us meaningfully?

---

## Phase 5: Output Innovation Opportunity Analysis

```markdown
# Genre Innovation Opportunity Analysis

## Analysis Summary
[Brief overview of innovation opportunities and key findings]

## Genre Conventions Analyzed
- [convention]
- [convention]

## Underserved Needs Identified
- [need]
- [need]

### Community Feedback
- [feedback]
- [feedback]

### Market Gaps
- [gap]
- [gap]

### Emerging Technologies
- [technology]
- [technology]

## Innovation Opportunities

### Mechanic Innovations
| Opportunity | Description | Underserved Need | Feasibility | Differentiation | Market Potential |
|-------------|-------------|------------------|-------------|-----------------|------------------|
| [opportunity] | [description] | [need] | [High/Medium/Low] | [High/Medium/Low] | [High/Medium/Low] |

### System Innovations
| Opportunity | Description | Underserved Need | Feasibility | Differentiation | Market Potential |
|-------------|-------------|------------------|-------------|-----------------|------------------|
| [opportunity] | [description] | [need] | [High/Medium/Low] | [High/Medium/Low] | [High/Medium/Low] |

### UI/UX Innovations
| Opportunity | Description | Underserved Need | Feasibility | Differentiation | Market Potential |
|-------------|-------------|------------------|-------------|-----------------|------------------|
| [opportunity] | [description] | [need] | [High/Medium/Low] | [High/Medium/Low] | [High/Medium/Low] |

## Prioritized Recommendations

### High Priority (High Feasibility + High Differentiation + High Market Potential)
| Priority | Opportunity | Rationale | Implementation Guidance |
|----------|-------------|-----------|------------------------|
| 1 | [opportunity] | [why prioritize] | [guidance] |
| 2 | [opportunity] | [why prioritize] | [guidance] |

### Medium Priority
| Priority | Opportunity | Rationale | Implementation Guidance |
|----------|-------------|-----------|------------------------|
| 3 | [opportunity] | [why prioritize] | [guidance] |
| 4 | [opportunity] | [why prioritize] | [guidance] |

### Low Priority
| Priority | Opportunity | Rationale | Implementation Guidance |
|----------|-------------|-----------|------------------------|
| 5 | [opportunity] | [why prioritize] | [guidance] |
| 6 | [opportunity] | [why prioritize] | [guidance] |

## Differentiation Strategy
- [how these innovations differentiate the game]
- [unique value proposition]
- [market positioning]

## Implementation Considerations
| Opportunity | Technical Risks | Resource Requirements | Dependencies |
|-------------|-----------------|----------------------|--------------|
| [opportunity] | [risks] | [requirements] | [dependencies] |

## Sources
[Citations of sources including community feedback, market analysis, competitor analysis]
```

---

## Phase 6: Present Summary and Ask to Write

Present a compact summary before writing:

```markdown
## Innovation Opportunity Analysis Summary
Innovation opportunities: [N]
High-priority recommendations: [M]
Medium-priority recommendations: [P]
Low-priority recommendations: [Q]
```

Before asking to write, show a **Opportunity Preview**:
- List top 3-5 innovation opportunities
- Show high-priority recommendations
- Note key differentiators

Use `AskUserQuestion`:
- "Ready to write the innovation opportunity analysis?"
  - "Yes — write to `docs/research/innovation-opportunity-[date].md`"
  - "Show me the full analysis preview first (don't write yet)"
  - "Cancel — I'll handle this manually"

---

## Phase 7: Write the Innovation Opportunity Analysis

If approved, write `docs/research/innovation-opportunity-[date].md` with the structure defined in Phase 5. Create the `docs/research/` directory if it does not exist. Use the current date for [date] in YYYY-MM-DD format.

Confirm the file was was written, then end with: "Innovation opportunity analysis saved. Use recommendations to guide differentiation strategy."

---

## Phase 8: Offer Next Steps

After writing the analysis, use `AskUserQuestion`:

- Prompt: "Innovation opportunity analysis complete. What would you like to do next?"
  - Options:
    - "Assess feasibility of high-priority innovations — run `/technical-feasibility-assessment`"
    - "Analyze competitive landscape — run `/competitive-intelligence`"
    - "Develop differentiation strategy — coordinate with `creative-director`, `game-designer`"
    - "Stop here — I'll review the findings first"

---

## Collaborative Protocol

1. **Innovation must be meaningful** -- distinguish in ways players value
2. **Feasibility matters** -- innovative ideas must be implementable
3. **Differentiation must be defensible** -- can competitors copy this easily?
4. **Market potential is critical** -- will players actually want this?
5. **Underserved needs drive innovation** -- solve real problems players have
6. **Genre conventions inform, don't dictate** -- understand conventions before breaking them