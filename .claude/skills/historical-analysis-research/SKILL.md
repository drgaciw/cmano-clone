---
name: historical-analysis-research
description: "Researches historical military operations to extract lessons, validate simulation models, and design authentic scenarios. Use for operation analysis, validation point identification, or scenario design insights."
argument-hint: "[operation | era | conflict]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, WebFetch, AskUserQuestion
model: sonnet
agent: military-research-specialist
---

# Historical Analysis Research

This skill analyzes historical military operations to extract lessons, validate simulation models, and design authentic scenarios. Focuses on operational decisions, outcomes, and technical factors.

**Output:** Historical analysis with validation points and scenario insights.

**Argument modes:**

**Analysis mode:** `$ARGUMENTS[0]` (operation, era, or conflict)

- **Specific operation** — Analyze a specific military operation or battle
- **Era** — Analyze operations within a specific era (WWII, Cold War, modern)
- **Conflict** — Analyze operations within a specific conflict or war

---

## Phase 1: Identify Historical Operation

Emit one line before starting: `"Researching historical operation..."` -- this confirms the skill is running.

### Research scope
- What specific operation, era, or conflict to analyze?
- What level of detail is needed? (overview, tactical analysis, technical focus)
- What's the purpose? (validation, scenario design, authenticity)
- Are there specific aspects to focus on? (tactics, technology, decisions)

---

## Phase 2: Research Historical Sources

Search historical sources systematically:

**Primary sources:**
- Official military histories and after-action reports
- Government archives and declassified documents
- Commanders' memoirs and diaries
- Unit histories and battle logs

**Secondary sources:**
- Academic historical research and papers
- Veteran oral histories
- Military history publications
- Documented analyses and retrospectives

**Research dimensions:**

**Operational context:**
- Timeline and sequence of events
- Participants and order of battle
- Objectives and strategic goals
- Terrain and environmental factors

**Tactical decisions:**
- Command decisions and rationale
- Tactical approaches and doctrines
- Key decision points and outcomes
- Successes and failures

**Technical factors:**
- Weapons systems and capabilities
- Sensor and detection systems
- Communications and command systems
- Logistics and support considerations

**Outcomes and lessons:**
- Operational outcomes and casualties
- Strategic impact and consequences
- Lessons learned and doctrinal changes
- Influence on subsequent operations

---

## Phase 3: Extract Simulation Insights

Extract insights relevant to simulation development:

**Validation points:**
- Technical parameters to validate (weapon ranges, sensor capabilities)
- Tactical behaviors to model (doctrine, decision-making)
- Environmental factors to simulate (terrain, weather)

**Scenario design insights:**
- Objective structures and mission types
- Force composition and balance
- Tactical challenges and player decisions
- Victory conditions and outcomes

**Authenticity considerations:**
- What made this operation distinct?
- What technical factors were decisive?
- What tactical lessons apply to simulation?

---

## Phase 4: Output Historical Analysis

```markdown
# Historical Analysis: [Operation, Era, or Conflict]

## Research Summary
[Brief overview of the historical operation and key findings]

## Sources
[Source citations with URLs and credibility assessment]

### Primary Sources
- [Source name] -- [URL] -- [Confidence level: High/Medium/Low] -- [Notes]

### Secondary Sources
- [Source name] -- [URL] -- [Confidence level: High/Medium/Low] -- [Notes]

## Operational Context
- Timeline: [description]
- Participants: [list]
- Objectives: [description]
- Terrain: [description]
- Environmental factors: [description]

## Tactical Analysis
- Command decisions: [description]
- Tactical approaches: [description]
- Key decision points: [list with outcomes]
- Successes: [list]
- Failures: [list]

## Technical Factors
- Weapons systems: [description]
- Sensor systems: [description]
- Communications: [description]
- Logistics: [description]

## Outcomes and Lessons
- Outcomes: [description]
- Strategic impact: [description]
- Lessons learned: [list]
- Doctrinal changes: [description]

## Simulation Validation Points
| Parameter | Real-World Value | Source | Validation Priority |
|-----------|------------------|--------|---------------------|
| [parameter] | [value] | [source] | [High/Medium/Low] |

## Scenario Design Insights
- Mission types: [list]
- Force composition: [description]
- Tactical challenges: [list]
- Player decisions: [list]
- Victory conditions: [description]

## Authenticity Considerations
- Distinctive features: [list]
- Decisive factors: [list]
- Tactical lessons: [list]
```

---

## Phase 5: Present Summary and Ask to Write

Present a compact summary before writing:

```markdown
## Historical Analysis Summary
Operation: [name]
Key findings: [2-3 key findings]
Validation points: [N]
Scenario insights: [M]
```

Before asking to write, show a **Finding Preview**:
- List the top 3-5 key findings
- Show validation points count and scenario insights count
- Note any distinctive features

Use `AskUserQuestion`:
- "Ready to write the historical analysis?"
  - "Yes — write to `docs/research/historical-analysis-[operation]-[date].md`"
  - "Show me the full analysis preview first (don't write yet)"
  - "Cancel — I'll handle this manually"

---

## Phase 6: Write the Historical Analysis

If approved, write `docs/research/historical-analysis-[operation]-[date].md` with the structure defined in Phase 4. Create the `docs/research/` directory if it does not exist. Use the current date for [date] in YYYY-MM-DD format.

Confirm the file was written, then end with: "Historical analysis saved. Use validation points with `/simulation-accuracy-validation` and scenario insights for scenario design."

---

## Phase 7: Offer Next Steps

After writing the analysis, use `AskUserQuestion`:

- Prompt: "Historical analysis complete. What would you like to do next?"
  - Options:
    - "Validate simulation parameters using this research — run `/simulation-accuracy-validation`"
    - "Design a scenario based on this operation — coordinate with `military-simulation-architect`"
    - "Research another operation — specify operation, era, or conflict"
    - "Stop here — I'll review the findings first"

---

## Collaborative Protocol

1. **Primary sources are best** -- official reports and firsthand accounts are most credible
2. **Context matters** -- understand why decisions were made, not just what decisions were made
3. **Multiple perspectives** -- accounts from different sides or units may conflict
4. **Technical vs. tactical** -- distinguish between technical factors and tactical decisions
5. **Lessons over trivia** -- focus on insights that apply to simulation design
6. **Authenticity vs. fun** -- historical accuracy supports fun, but doesn't override it