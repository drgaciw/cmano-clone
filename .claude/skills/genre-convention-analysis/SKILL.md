---
name: genre-convention-analysis
description: "Analyzes military simulation genre conventions and standards to understand player expectations, standard mechanics, and genre-defining features. Use for genre research, convention analysis, UI/UX patterns, or differentiation strategies."
argument-hint: "[subgenre: naval | air | ground | combined | comprehensive]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, WebFetch, AskUserQuestion
model: sonnet
agent: simulation-genre-analyst
---

# Genre Convention Analysis

This skill researches and analyzes established conventions in military simulation games to understand player expectations, standard mechanics, and genre-defining features.

**Output:** Genre convention analysis with standards and recommendations.

**Argument modes:**

**Analysis mode:** `$ARGUMENTS[0]` (subgenre focus)

- **`naval`** — Naval warfare simulations (CMANO, Dangerous Waters, etc.)
- **`air`** — Air combat simulations (DCS, Falcon BMS, etc.)
- **`ground`** — Ground warfare simulations (Arma, Steel Beasts, etc.)
- **`combined`** — Combined arms simulations (DCS World with combined ops, etc.)
- **`comprehensive`** — Analysis across all subgenres

---

## Phase 1: Define Analysis Scope

Emit one line before starting: `"Analyzing military simulation genre conventions..."` -- this confirms the skill is running.

Then gather context:

### Analysis scope
- What subgenre are you analyzing? (naval, air, ground, combined, comprehensive)
- What's the purpose? (alignment, differentiation, innovation, onboarding)
- What are the constraints? (timeframe, specific titles to include/exclude)

### Analysis goals
- Are you looking for conventions to follow or conventions to break?
- How will this analysis inform game decisions? (design, UX, marketing)
- What level of detail is needed? (overview vs. deep dive)

---

## Phase 2: Research Genre Standards

Research genre standards systematically:

**Landmark Titles Analysis:**
- Identify landmark titles in the subgenre
- Analyze their defining features and innovations
- Study their reception and legacy
- Document which features became conventions

**Core Conventions:**
- Identify standard mechanics present across multiple titles
- Analyze UI/UX patterns common in the genre
- Study information presentation approaches
- Document command and control interface standards

**Player Expectations:**
- Research community feedback and reviews
- Analyze common complaints and requests
- Study forum discussions and community feedback
- Identify what players consider "essential" features

**Standard vs. Innovative:**
- Categorize features as standard (expected) vs. innovative (novel)
- Analyze which innovations became standard over time
- Identify persistent standard features that define the genre
- Document which standards are flexible vs. rigid

---

## Phase 3: Analyze UI/UX Patterns

Study UI/UX patterns specific to the subgenre:

**Information Density:**
- How do genre titles manage information overload?
- What information is always displayed vs. what is on-demand?
- What are the standard information hierarchies?

**Interface Elements:**
- What are the standard interface elements? (radars, maps, tactical displays)
- How are command interfaces structured?
- What are the common interaction patterns?

**Learning Curve Management:**
- How do genre titles handle complexity for new players?
- What are the standard onboarding approaches?
- How do games balance depth with accessibility?

---

## Phase 4: Format Analysis

Organize findings into a structured format:

```markdown
# Genre Convention Analysis: [Subgenre]

## Analysis Summary
[Brief overview of the subgenre and key conventions]

## Landmark Titles
[List and analyze landmark titles]

### [Title Name]
- Release year: [year]
- Defining features: [list]
- Innovation impact: [description]
- Legacy: [how it influenced the genre]

## Core Conventions

### Standard Mechanics
[Conventions present across multiple titles]

### UI/UX Patterns
[Standard interface elements and patterns]

### Information Presentation
[How information is typically presented]

### Command Interfaces
[Standard command and control approaches]

## Player Expectations

### Essential Features
[Features players consider essential]

### Common Complaints
[Recurring complaints and requests]

### Community Preferences
[What the community values and expects]

## Standard vs. Innovative

### Standard Conventions
[Features that are genre standard]

### Innovative Approaches
[Novel approaches and their reception]

### Convention Flexibility
[Which conventions are flexible vs. rigid]

## Recommendations

### Alignment Opportunities
[Where to align with conventions]

### Differentiation Opportunities
[Where to differentiate from conventions]

### Innovation Opportunities
[Opportunities for genuine innovation]

## Sources
[Citations of sources including games, reviews, community feedback]
```

---

## Phase 5: Present Summary and Ask to Write

Present a compact summary before writing:

```markdown
## Genre Convention Analysis Summary: [Subgenre]
Titles analyzed: [N]
Core conventions identified: [M]
Standard vs. innovative ratio: [X]% standard / [Y]% innovative
```

Before asking to write, show a **Convention Preview**:
- List the top 5 core conventions
- Show standard vs. innovative breakdown
- Note key player expectations

This gives the user enough context to judge scope before committing to writing the file.

Use `AskUserQuestion`:
- "Ready to write the genre analysis?"
  - "Yes — write to `docs/research/genre-convention-[subgenre]-[date].md`"
  - "Show me the full analysis preview first (don't write yet)"
  - "Cancel — I'll handle this manually"

If the user picks "Show me the full analysis preview", output the complete analysis as a fenced markdown block. Then ask again with the same three options.

---

## Phase 6: Write the Genre Analysis

If approved, write `docs/research/genre-convention-[subgenre]-[date].md` with the structure defined in Phase 4. Create the `docs/research/` directory if it does not exist. Use the current date for [date] in YYYY-MM-DD format.

Confirm the file was written, then end with: "Genre analysis saved. Use this analysis to inform design decisions and differentiation strategies."

---

## Phase 7: Offer Next Steps

After writing the analysis, use `AskUserQuestion`:

- Prompt: "Genre analysis complete. What would you like to do next?"
  - Options:
    - "Identify innovation opportunities — run `/genre-innovation-opportunity`"
    - "Analyze competitor products — run `/competitive-intelligence`"
    - "Apply findings to UX design — coordinate with `user-experience-military-analyst`"
    - "Analyze another subgenre — specify naval, air, ground, combined, or comprehensive"

---

## Collaborative Protocol

1. **Genre conventions are guidelines** -- highlight when breaking them is justified
2. **Player expectations matter** -- understand what players expect before deciding to violate conventions
3. **Distinguish standard from essential** -- some conventions are nice-to-haves, others are genre-defining
4. **Innovation requires understanding** -- you can't innovate effectively without understanding conventions first
5. **Context is critical** -- explain why conventions exist and what problems they solve
6. **Differentiation strategy** -- help the user decide where to align vs. where to differentiate