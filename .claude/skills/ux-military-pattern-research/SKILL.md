---
name: ux-military-pattern-research
description: "Researches UX patterns in military simulation games, focusing on information density, command interfaces, and cognitive load management. Use for UI pattern research, cognitive load analysis, or military interface study."
argument-hint: "[interface type | all]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, WebFetch, AskUserQuestion
model: sonnet
agent: user-experience-military-analyst
---

# UX Military Pattern Research

This skill analyzes UX patterns in military simulation games, focusing on information density, command interfaces, and cognitive load management.

**Output:** UX pattern research report with implementation recommendations.

**Argument modes:**

**Research focus:** `$ARGUMENTS[0]` (interface type)

- **`tactical`** — Tactical display interfaces (radars, maps, situational awareness)
- **`command`** — Command and control interfaces (orders, hierarchies, coordination)
- **`information`** — Information presentation interfaces (data, logs, reports)
- **`all`** — Comprehensive analysis across all interface types

---

## Phase 1: Define Interface Types

Emit one line before starting: `"Researching military simulation UX patterns..."` -- this confirms the skill is running.

### Research scope
- What interface types to research? (tactical, command, information, all)
- What's the purpose? (alignment, differentiation, innovation)
- What are the constraints? (platform, input methods, screen real estate)

---

## Phase 2: Study Genre UI/UX Patterns

Study UI/UX patterns in genre-leading titles:

**Interface categories:**

**Tactical displays:**
- Radar and sensor displays
- Map and tactical interfaces
- Situational awareness presentation
- Threat and contact displays

**Command interfaces:**
- Order and command input
- Command hierarchies and organization
- Unit selection and control
- Coordination and communication interfaces

**Information presentation:**
- Data displays and readouts
- Logs and reports
- Status indicators
- Alerts and notifications

---

## Phase 3: Analyze Information Density and Presentation

**Information density analysis:**
- How much information is displayed simultaneously?
- How is information prioritized and hierarchized?
- What information is always visible vs. on-demand?
- How do games handle information overload?

**Presentation methods:**
- What visual metaphors are used? (radar circles, map symbols, color coding)
- How is information grouped and organized?
- What are the standard information layouts?
- How is dynamic vs. static information handled?

**Cognitive load management:**
- How do games reduce cognitive load?
- What progressive disclosure techniques are used?
- How is information filtered and prioritized?
- What are the standard simplification approaches?

---

## Phase 4: Analyze Command Complexity and Interaction

**Command input methods:**
- How are commands issued? (mouse, keyboard, gamepad, voice)
- What are the standard command patterns?
- How is command discovery handled?
- What are the shortcut and hotkey conventions?

**Command complexity:**
- How deep are command hierarchies?
- How are complex commands structured?
- What contextual command patterns exist?
- How are command errors and corrections handled?

**Interaction patterns:**
- What are the standard selection patterns?
- How is drag-and-drop used?
- What are the context menu conventions?
- How are multi-select and bulk operations handled?

---

## Phase 5: Identify Best Practices and Patterns

**Best practices for information density:**
- [pattern and description]
- [pattern and description]

**Best practices for command complexity:**
- [pattern and description]
- [pattern and description]

**Best practices for cognitive load management:**
- [pattern and description]
- [pattern and description]

**Standard UI elements:**
- [element and description]
- [element and description]

**Common interaction patterns:**
- [pattern and description]
- [pattern and description]

---

## Phase 6: Output UX Pattern Research Report

```markdown
# UX Military Pattern Research: [Interface Type]

## Research Summary
[Brief overview of UX patterns researched and key findings]

## Titles Analyzed
[List of games analyzed]

## Information Density Analysis

### Standard Information Density
- [description of standard density levels]

### Information Prioritization
- [how information is prioritized]

### Always-Visible vs. On-Demand
| Category | Always-Visible | On-Demand |
|----------|----------------|-----------|
| [category] | [items] | [items] |

### Information Overload Management
- [techniques used]
- [techniques used]

## Presentation Methods

### Visual Metaphors
| Metaphor | Usage | Variations |
|----------|-------|------------|
| [metaphor] | [how used] | [variations] |

### Information Layouts
- [standard layout pattern]
- [standard layout pattern]

### Dynamic vs. Static Information
- [how dynamic info is handled]
- [how static info is handled]

## Cognitive Load Management

### Progressive Disclosure Techniques
- [technique]
- [technique]

### Information Filtering
- [filtering approach]
- [filtering approach]

### Simplification Approaches
- [approach]
- [approach]

## Command Complexity Analysis

### Command Input Methods
| Method | Usage | Pros | Cons |
|--------|-------|------|------|
| [method] | [how used] | [pros] | [cons] |

### Command Hierarchy Depth
- [depth and structure]

### Contextual Command Patterns
- [pattern]
- [pattern]

### Command Error Handling
- [how errors are handled]

## Interaction Patterns

### Selection Patterns
- [pattern]
- [pattern]

### Drag-and-Drop Usage
- [how used]

### Context Menu Conventions
- [convention]
- [convention]

### Multi-Select and Bulk Operations
- [how handled]

## Best Practices

### Information Density
- [best practice]
- [best practice]

### Command Complexity
- [best practice]
- [best practice]

### Cognitive Load Management
- [best practice]
- [best practice]

## Standard UI Elements
- [element and usage]
- [element and usage]

## Common Interaction Patterns
- [pattern and usage]
- [pattern and usage]

## Implementation Recommendations
| Recommendation | Rationale | Impact |
|----------------|-----------|--------|
| [recommendation] | [why] | [impact] |

## Innovation Opportunities
| Opportunity | Differentiation | Feasibility |
|-------------|-----------------|-------------|
| [opportunity] | [description] | [feasibility] |
```

---

## Phase 7: Present Summary and Ask to Write

Present a compact summary before writing:

```markdown
## UX Pattern Research Summary
Titles analyzed: [N]
Best practices identified: [M]
Standard UI elements: [P]
Innovation opportunities: [Q]
```

Before asking to write, show a **Pattern Preview**:
- List top 3-5 best practices
- Show standard UI elements count
- Note any high-priority innovation opportunities

Use `AskUserQuestion`:
- "Ready to write the UX pattern research report?"
  - "Yes — write to `docs/research/ux-military-patterns-[type]-[date].md`"
  - "Show me the full report preview first (don't write yet)"
  - "Cancel — I'll handle this manually"

---

## Phase 8: Write the UX Pattern Research Report

If approved, write `docs/research/ux-military-patterns-[type]-[date].md` with the structure defined in Phase 6. Create the `docs/research/` directory if it does not exist. Use the current date for [date] in YYYY-MM-DD format.

Confirm the file was written, then end with: "UX pattern research saved. Use recommendations to inform UI design decisions."

---

## Phase 9: Offer Next Steps

After writing the report, use `AskUserQuestion`:

- Prompt: "UX pattern research complete. What would you like to do next?"
  - Options:
    - "Apply findings to UI design — coordinate with `ux-designer`"
    - "Analyze genre conventions — run `/genre-convention-analysis`"
    - "Research information density specifically — analyze another interface type"
    - "Stop here — I'll review the findings first"

---

## Collaborative Protocol

1. **Genre patterns are guidelines** -- highlight when breaking them is justified
2. **Information density is the core challenge** -- focus on how games manage it
3. **Cognitive load matters** -- understand how successful games reduce it
4. **Command complexity must be managed** -- deep hierarchies need smart interfaces
5. **Innovation requires understanding** -- you can't innovate effectively without understanding patterns first
6. **Context is critical** -- explain why patterns exist and what problems they solve