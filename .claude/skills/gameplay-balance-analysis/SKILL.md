---
name: gameplay-balance-analysis
description: "Analyzes gameplay balance for military simulation mechanics, considering authenticity vs. playability, difficulty curves, and player agency. Use for combat balance, progression analysis, or strategic depth assessment."
argument-hint: "[system | mechanic | all]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash, WebFetch, AskUserQuestion
model: sonnet
agent: gameplay-mechanics-analyst
---

# Gameplay Balance Analysis

This skill evaluates balance in military simulation mechanics, considering authenticity vs. playability, difficulty curves, and player agency.

**Output:** Balance analysis report with tuning recommendations.

**Argument modes:**

**Analysis mode:** `$ARGUMENTS[0]` (system or mechanic)

- **`all`** — Analyze all gameplay systems and mechanics
- **`combat`** — Analyze combat mechanics (weapons, damage, time-to-kill)
- **`progression`** — Analyze progression systems (unlocks, upgrades, power curves)
- **`tactical`** — Analyze tactical decision depth and strategic variety
- **`learning`** — Analyze learning curve and onboarding

---

## Phase 1: Load Gameplay Data

Emit one line before starting: `"Analyzing gameplay balance..."` -- this confirms the skill is running.

### Read gameplay data
- Read balance data files from `assets/data/` and `design/balance/`
- Read relevant design documents from `design/gdd/`
- Note every file read -- they will appear in the Data Sources section

### Identify analysis scope
- What systems or mechanics to analyze?
- What balance criteria apply? (fun, fairness, strategic depth, authenticity)
- Are there specific balance concerns to investigate?

---

## Phase 2: Perform Balance Analysis

Run domain-specific checks:

**Combat balance:**
- Calculate DPS and time-to-kill for all weapons/abilities
- Check for dominant strategies (options that are strictly better)
- Identify degenerate strategies (exploits, unintended gameplay patterns)
- Verify counterplay options exist for every option
- Check authenticity vs. playability trade-offs

**Progression balance:**
- Plot power curves and progression rates
- Check for dead zones (no meaningful progression for too long)
- Check for power spikes (sudden jumps in capability)
- Verify that all upgrades are meaningful choices
- Analyze unlock pacing and reward timing

**Tactical depth:**
- Assess decision frequency and consequence
- Verify meaningful strategic variety exists
- Check for rock-paper-scissors relationships
- Analyze information availability and fog of war
- Evaluate strategic vs. tactical vs. operational layers

**Learning curve:**
- Analyze onboarding complexity and tutorial effectiveness
- Assess progressive disclosure of mechanics
- Evaluate cognitive load at different gameplay stages
- Check accessibility for new players vs. depth for mastery

---

## Phase 3: Output Balance Report

```markdown
## Balance Analysis: [System or Mechanic]

### Data Sources Analyzed
- [List of files read]

### Balance Summary
| System/Mechanic | Status | Key Findings |
|-----------------|--------|--------------|
| [system] | [HEALTHY / CONCERNS / CRITICAL ISSUES] | [findings] |

### Health Summary: [HEALTHY / CONCERNS / CRITICAL ISSUES]

### Dominant Strategies Found
- [Strategy description and why it is problematic]

### Degenerate Strategies Found
- [Strategy description and why it is problematic]

### Counterplay Analysis
| Option | Counterplay Available | Quality | Notes |
|--------|----------------------|---------|-------|
| [option] | [Yes/No] | [Good/Fair/Poor] | [notes] |

### Tactical Depth Assessment
| Dimension | Score | Notes |
|-----------|-------|-------|
| Decision frequency | [High/Medium/Low] | [notes] |
| Strategic variety | [High/Medium/Low] | [notes] |
| Counterplay quality | [High/Medium/Low] | [notes] |

### Progression Analysis
- [Power curve description]
- [Dead zones identified]
- [Power spikes identified]
- [Unlock pacing assessment]

### Learning Curve Assessment
- [Onboarding complexity]
- [Progressive disclosure effectiveness]
- [Accessibility vs. depth balance]

### Authenticity vs. Playability Trade-offs
| Parameter | Real-World Value | Simulation Value | Deviation | Rationale |
|-----------|------------------|-------------------|-----------|-----------|
| [parameter] | [value] | [value] | [deviation] | [why deviated] |

### Recommendations
| Priority | Issue | Suggested Fix | Impact |
|----------|-------|--------------|--------|
| [High/Medium/Low] | [description] | [adjustment] | [gameplay/authenticity/fun] |
```

---

## Phase 4: Present Summary and Ask to Write

Present a compact summary before writing:

```markdown
## Balance Analysis Summary
Systems analyzed: [N]
Health status: [HEALTHY / CONCERNS / CRITICAL ISSUES]
Dominant strategies: [X]
Degenerate strategies: [Y]
Recommendations: [Z] (High: [A], Medium: [B], Low: [C])
```

Before asking to write, show a **Balance Preview**:
- List every dominant or degenerate strategy as a one-line bullet
- Show high-priority recommendations only
- Note any critical issues

Use `AskUserQuestion`:
- "Ready to write the balance analysis report?"
  - "Yes — write to `design/balance/gameplay-balance-[system]-[date].md`"
  - "Show me the full report preview first (don't write yet)"
  - "Cancel — I'll handle this manually"

---

## Phase 5: Write the Balance Report

If approved, write `design/balance/gameplay-balance-[system]-[date].md` with the structure defined in Phase 3. Create the `design/balance/` directory if it does not exist. Use the current date for [date] in YYYY-MM-DD format.

Confirm the file was written, then end with: "Balance report saved. Implement recommended adjustments as needed."

---

## Phase 6: Fix & Verify Cycle

After presenting the report, use `AskUserQuestion`:

- Prompt: "Balance analysis complete. What would you like to do next?"
  - Options:
    - "Fix highest-priority issue now — walk me through it"
    - "Re-run analysis after making changes — validate no new issues introduced"
    - "Stop here — I'll review the findings manually"

If fixing an issue:
- Ask which issue to address first (refer to the Recommendations table by priority row)
- Guide the user to update the relevant balance file in `assets/data/` or `design/balance/`
- After each fix, offer to re-run the relevant balance checks to verify no new issues were introduced
- If the fix changes a parameter defined in a GDD, remind the user:
  > "This parameter is defined in a design document. Run `/propagate-design-change [path]` on the affected GDD to find downstream impacts before committing."

---

## Collaborative Protocol

1. **Fun matters most** -- authenticity serves fun, not the other way around
2. **Player agency is critical** -- players should always have meaningful choices
3. **Counterplay prevents frustration** -- every option should have viable counterplay
4. **Tactical depth > micromanagement** -- strategic decisions matter more than clicks
5. **Learning curves should be progressive** -- introduce complexity gradually
6. **Balance is iterative** -- expect to adjust as the game evolves