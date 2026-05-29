---
name: military-database-research
description: "Researches authoritative military databases and specifications for accurate simulation parameters. Use for weapon system specs, platform capabilities, sensor data, tactical doctrine information, or military operation details."
argument-hint: "[topic: platform | weapon | sensor | doctrine | operation]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, WebFetch, AskUserQuestion
model: sonnet
agent: military-research-specialist
---

# Military Database Research

This skill conducts targeted research using military databases, technical specifications, and authoritative sources to gather accurate data for simulation parameters, platform capabilities, and weapon systems.

**Output:** Structured research report with specifications, sources, and implementation notes.

**Argument modes:**

**Research mode:** `$ARGUMENTS[0]` (research topic)

- **`platform`** — Aircraft, ships, submarines, ground vehicles, facilities
- **`weapon`** — Missiles, guns, torpedoes, bombs, electronic warfare systems
- **`sensor`** — Radar, sonar, ESM, optical sensors, communication systems
- **`doctrine`** — Military tactics, procedures, command structures, protocols
- **`operation`** — Historical operations, battles, campaigns, scenarios

---

## Phase 1: Accept Research Topic

Emit one line before starting: `"Researching military database for [topic]..."` -- this confirms the skill is running.

Then gather context:

### Research scope
- What specific topic are you researching? (platform type, weapon system, sensor class, doctrine, operation)
- What level of detail is needed? (overview, technical specs, operational characteristics)
- What era or timeframe? (modern, cold war, WWII, specific historical period)
- How will this research be used? (parameter tuning, scenario design, authenticity verification)

### Research parameters
- Are there specific sources to include or avoid?
- What's the priority: completeness vs. speed?
- Are there conflicting requirements? (authenticity vs. gameplay balance)

---

## Phase 2: Research Authoritative Sources

Search authoritative military sources systematically:

**Authoritative Sources:**
- Jane's Defense Publications
- Official military technical manuals and publications
- Defense industry databases and specifications
- Government defense databases (publicly available)
- Academic military research and papers

**Search Strategy:**
1. **Primary sources first:** Official military publications, manufacturer specifications
2. **Cross-reference validation:** Find multiple independent sources
3. **Note discrepancies:** Highlight conflicting information between sources
4. **Source credibility assessment:** Evaluate authority, date, expertise

**Research domains:**

**Platform research:**
- Performance specifications (speed, range, endurance)
- Operational capabilities (payload, sensors, weapons)
- Technical characteristics (dimensions, displacement, power plant)
- Service history and variants

**Weapon research:**
- Technical specifications (range, warhead, guidance, propulsion)
- Performance characteristics (accuracy, reliability, effectiveness)
- Operational use (launch platforms, targets, employment doctrine)
- Comparison with similar systems

**Sensor research:**
- Technical specifications (range, frequency, resolution, accuracy)
- Operational characteristics (detection probability, false alarm rate)
- Countermeasure effectiveness
- Integration with other systems

**Doctrine research:**
- Tactical doctrine and procedures
- Command and control structures
- Standard operating procedures
- Historical evolution and variations

**Operation research:**
- Timeline and participants
- Tactical decisions and outcomes
- Technical factors (sensors, weapons, communications)
- Lessons learned and implications

---

## Phase 3: Format Findings

Organize research findings into a structured format:

```markdown
# Military Database Research: [Topic]

## Research Summary
[Brief overview of what was researched and key findings]

## Specifications
[Technical specifications, organized by category]

### Performance
[Range, speed, accuracy, etc.]

### Operational
[Capabilities, limitations, employment doctrine]

### Technical
[Dimensions, weight, power requirements, etc.]

## Sources
[Source citations with URLs and credibility assessment]

### Primary Sources
- [Source name] -- [URL] -- [Confidence level: High/Medium/Low] -- [Notes]

### Secondary Sources
- [Source name] -- [URL] -- [Confidence level: High/Medium/Low] -- [Notes]

## Discrepancies
[Notes on conflicting information between sources and assessment]

## Implementation Notes
[Recommendations for applying this research to the game]

### Parameter Recommendations
[Suggested parameter values with rationale]

### Authenticity vs. Gameplay Trade-offs
[Notes on where real-world values may need adjustment for gameplay]

### Confidence Assessment
[Overall confidence in the research findings]

## Data Quality Assessment
- Source credibility: [High/Medium/Low]
- Cross-reference validation: [High/Medium/Low]
- Completeness: [High/Medium/Low]
- Overall confidence: [High/Medium/Low]
```

---

## Phase 4: Present Summary and Ask to Write

Present a compact summary before writing:

```markdown
## Research Summary: [Topic]
Sources consulted: [N] primary, [M] secondary
Key findings: [2-3 key specifications or findings]
Confidence level: [High/Medium/Low]
Discrepancies found: [Yes/No]
```

Before asking to write, show a **Finding Preview**:
- List the top 3-5 key specifications or findings as bullet points
- Show source count and confidence level
- Note any significant discrepancies

This gives the user enough context to judge scope before committing to writing the file.

Use `AskUserQuestion`:
- "Ready to write the research report?"
  - "Yes — write to `docs/research/military-database-[topic]-[date].md`"
  - "Show me the full report preview first (don't write yet)"
  - "Cancel — I'll handle this manually"

If the user picks "Show me the full report preview", output the complete report as a fenced markdown block. Then ask again with the same three options.

---

## Phase 5: Write the Research Report

If approved, write `docs/research/military-database-[topic]-[date].md` with the structure defined in Phase 3. Create the `docs/research/` directory if it does not exist. Use the current date for [date] in YYYY-MM-DD format.

Confirm the file was written, then end with: "Research report saved. Use `/simulation-accuracy-validation` to apply these findings to simulation parameters."

---

## Phase 6: Offer Next Steps

After writing the report, use `AskUserQuestion`:

- Prompt: "Research complete. What would you like to do next?"
  - Options:
    - "Validate simulation parameters using this research — run `/simulation-accuracy-validation`"
    - "Research another topic — specify platform, weapon, sensor, doctrine, or operation"
    - "Apply findings to parameter tuning — coordinate with `simulation-parameter-analyst`"
    - "Stop here — I'll review the research first"

---

## Collaborative Protocol

1. **Research quality first** -- prioritize authoritative sources over quantity
2. **Cross-reference everything** -- never rely on a single source
3. **Note credibility clearly** -- always indicate confidence levels and source quality
4. **Flag discrepancies** -- highlight conflicting information rather than resolving them yourself
5. **Contextualize for game development** -- explain relevance to simulation implementation
6. **Document limitations** -- acknowledge classified information, outdated data, or missing details