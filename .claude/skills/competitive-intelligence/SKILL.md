---
name: competitive-intelligence
description: "Researches competing military simulation products and features to identify strengths, weaknesses, innovative features, and market positioning opportunities. Use for competitive analysis, market positioning, or feature innovation."
argument-hint: "[focus: features | mechanics | ui | market | all]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, WebFetch, AskUserQuestion
model: sonnet
agent: simulation-genre-analyst
---

# Competitive Intelligence

This skill conducts competitive analysis of military simulation games, identifying strengths, weaknesses, innovative features, and market positioning opportunities.

**Output:** Competitive intelligence report with strategic insights.

**Argument modes:**

**Analysis focus:** `$ARGUMENTS[0]` (focus area)

- **`features`** — Analyze product features and capabilities
- **`mechanics`** — Analyze gameplay mechanics and systems
- **`ui`** — Analyze user interface and UX patterns
- **`market`** — Analyze market positioning and reception
- **`all`** — Comprehensive analysis across all dimensions

---

## Phase 1: Identify Competitors

Emit one line before starting: `"Analyzing competitive landscape..."` -- this confirms the skill is running.

### Competitor identification
- What subgenre? (naval, air, ground, combined arms)
- What are the key competitors in this space?
- What's the analysis focus? (features, mechanics, UI, market, all)

---

## Phase 2: Research Competitor Products

Research competitors systematically:

**Product identification:**
- Identify key competitors in the military simulation space
- Research product features, mechanics, and reception
- Analyze innovative approaches and unique selling points
- Compare against project capabilities and goals

**Research dimensions:**

**Features analysis:**
- What features do competitors offer?
- What features are unique to each competitor?
- What features are standard across all competitors?
- What features are missing from all competitors?

**Mechanics analysis:**
- What mechanics do competitors use?
- How do mechanics differ between competitors?
- What are the strengths and weaknesses of each approach?
- What innovations have competitors introduced?

**UI/UX analysis:**
- How do competitors handle information density?
- What are the standard UI patterns in the genre?
- What UI innovations have competitors introduced?
- How do competitors manage learning curves?

**Market analysis:**
- What's the market reception of each competitor?
- What are players praising and complaining about?
- What are the sales and player counts?
- What are the unique selling propositions?

---

## Phase 3: Identify Market Gaps and Opportunities

Analyze competitive landscape for opportunities:

**Market gaps:**
- What player needs are underserved?
- What features are missing from all competitors?
- What mechanics are underdeveloped in the genre?
- What UI/UX challenges remain unsolved?

**Innovation opportunities:**
- What can we do differently than all competitors?
- What innovations are feasible given our constraints?
- What unique value proposition can we offer?
- How can we differentiate meaningfully?

**Strategic positioning:**
- Where does our project fit in the competitive landscape?
- What's our unique value proposition?
- What competitors are we targeting for displacement?
- What niche can we occupy?

---

## Phase 4: Output Competitive Intelligence Report

```markdown
# Competitive Intelligence Report

## Analysis Summary
[Brief overview of the competitive landscape and key findings]

## Competitors Analyzed
| Competitor | Subgenre | Release Year | Market Position | Key Strengths | Key Weaknesses |
|------------|----------|--------------|-----------------|---------------|----------------|
| [name] | [subgenre] | [year] | [position] | [strengths] | [weaknesses] |

## Feature Analysis
### Standard Features (present in all competitors)
- [feature]
- [feature]

### Unique Features (specific to competitors)
| Competitor | Unique Features | Differentiation |
|------------|------------------|-----------------|
| [name] | [features] | [description] |

### Missing Features (absent from all competitors)
- [feature]
- [feature]

## Mechanic Analysis
### Standard Mechanics
- [mechanic]

### Competitor-Specific Mechanics
| Competitor | Mechanics | Strengths | Weaknesses |
|------------|-----------|-----------|------------|
| [name] | [mechanics] | [strengths] | [weaknesses] |

### Innovations Introduced
| Competitor | Innovation | Impact | Adopted by Others |
|------------|------------|--------|-------------------|
| [name] | [innovation] | [impact] | [Yes/No] |

## UI/UX Analysis
### Standard UI Patterns
- [pattern]

### UI Innovations
| Competitor | Innovation | Reception |
|------------|------------|-----------|
| [name] | [innovation] | [good/fair/poor] |

### Learning Curve Management
| Competitor | Approach | Effectiveness |
|------------|----------|--------------|
| [name] | [approach] | [effective/ineffective] |

## Market Analysis
### Market Reception
| Competitor | Reception | Player Count | Key Praise | Key Complaints |
|------------|-----------|--------------|------------|----------------|
| [name] | [reception] | [count] | [praise] | [complaints] |

### Unique Selling Propositions
| Competitor | USP | Effectiveness |
|------------|-----|---------------|
| [name] | [USP] | [effective/ineffective] |

## Market Gaps
### Underserved Player Needs
- [need]
- [need]

### Missing Features
- [feature]
- [feature]

### Underdeveloped Mechanics
- [mechanic]
- [mechanic]

### Unsolved UI/UX Challenges
- [challenge]
- [challenge]

## Innovation Opportunities
| Priority | Opportunity | Differentiation | Feasibility | Impact |
|----------|-------------|-----------------|-------------|--------|
| [High/Medium/Low] | [opportunity] | [description] | [feasibility] | [impact] |

## Strategic Recommendations
| Recommendation | Rationale | Impact |
|----------------|-----------|--------|
| [recommendation] | [why] | [impact] |

## Strategic Positioning
- Our unique value proposition: [description]
- Target competitors for displacement: [list]
- Niche to occupy: [description]
- Differentiation strategy: [description]

## Sources
[Citations of sources including reviews, forums, sales data]
```

---

## Phase 5: Present Summary and Ask to Write

Present a compact summary before writing:

```markdown
## Competitive Intelligence Summary
Competitors analyzed: [N]
Market gaps identified: [M]
Innovation opportunities: [P]
Strategic recommendations: [Q]
```

Before asking to write, show a **Opportunity Preview**:
- List top 3-5 market gaps or innovation opportunities
- Show strategic positioning summary
- Note any high-priority recommendations

Use `AskUserQuestion`:
- "Ready to write the competitive intelligence report?"
  - "Yes — write to `docs/research/competitive-intelligence-[date].md`"
  - "Show me the full report preview first (don't write yet)"
  - "Cancel — I'll handle this manually"

---

## Phase 6: Write the Competitive Intelligence Report

If approved, write `docs/research/competitive-intelligence-[date].md` with the structure defined in Phase 4. Create the `docs/research/` directory if it does not exist. Use the current date for [date] in YYYY-MM-DD format.

Confirm the file was written, then end with: "Competitive intelligence saved. Use insights to inform differentiation and positioning strategy."

---

## Phase 7: Offer Next Steps

After writing the report, use `AskUserQuestion`:

- Prompt: "Competitive intelligence complete. What would you like to do next?"
  - Options:
    - "Identify innovation opportunities — run `/genre-innovation-opportunity`"
    - "Analyze genre conventions — run `/genre-convention-analysis`"
    - "Apply findings to positioning strategy — coordinate with `creative-director`, `game-designer`"
    - "Stop here — I'll review the findings first"

---

## Collaborative Protocol

1. **Competitors inform, don't dictate** -- understand what others do, but don't copy blindly
2. **Market gaps are opportunities** -- underserved needs are where we can differentiate
3. **Differentiation must be meaningful** -- distinguish in ways players value
4. **Innovation requires understanding** -- you can't innovate effectively without understanding what exists
5. **Positioning is strategic** -- know where you fit in the competitive landscape
6. **Player feedback matters** -- understand what players praise and complain about