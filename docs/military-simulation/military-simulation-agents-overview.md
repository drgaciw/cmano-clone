# Military Simulation Agents Overview

**Project:** cmano-clone (Military Simulation Game)
**Agent Ecosystem:** 8 new specialized agents
**Skills:** 10 new military simulation skills

---

## Introduction

This document provides an overview of the military simulation game requirements analysis agents and skills added to Project Aegis. These agents are designed to integrate seamlessly with the existing 3-tier agent structure and provide domain-specific expertise for military simulation development.

---

## Agent Architecture

### Tier 2: Department Lead Agents (Sonnet)

Two new domain lead agents oversee military simulation areas:

#### `military-simulation-architect`

**Role:** Domain Lead for Military Simulation Systems

**Description:** Oversees military simulation architecture, including tactical systems, command hierarchies, combat mechanics, and simulation fidelity. Coordinates between military simulation specialists and existing game design teams.

**Operational Scope:**
- Military simulation system architecture decisions
- Tactical simulation accuracy vs. gameplay balance trade-offs
- Command and control system design
- Combat mechanics and fidelity standards
- Coordination of military simulation specialists

**Reports to:** `technical-director`, `game-designer`
**Coordinates:** All military simulation specialist agents

#### `requirements-analyst`

**Role:** Domain Lead for Requirements Analysis

**Description:** Leads comprehensive requirements analysis for military simulation features, including technical feasibility, performance constraints, and implementation complexity assessment.

**Operational Scope:**
- Requirements feasibility analysis
- Technical constraint evaluation
- Performance impact assessment
- Implementation complexity estimation
- Risk assessment for new features

**Reports to:** `technical-director`, `producer`
**Coordinates:** `simulation-parameter-analyst`, `gameplay-mechanics-analyst`, `technical-constraints-analyst`

---

### Tier 3: Specialist Agents (Sonnet)

Six new specialist agents provide domain-specific analysis:

#### `simulation-parameter-analyst`

**Role:** Military Simulation Parameters Specialist

**Description:** Analyzes and validates military simulation parameters including weapon systems, platform capabilities, sensor ranges, detection probabilities, engagement envelopes, and tactical behaviors.

**Reports to:** `military-simulation-architect`, `requirements-analyst`
**Works with:** `military-research-specialist` for source verification, `gameplay-mechanics-analyst` for parameter balance

#### `gameplay-mechanics-analyst`

**Role:** Gameplay Mechanics & Balance Specialist

**Description:** Analyzes gameplay mechanics for military simulation games, focusing on balance, player engagement, tactical depth, and learning curves. Researches genre conventions and innovative mechanics.

**Reports to:** `military-simulation-architect`, `game-designer`
**Works with:** `simulation-genre-analyst` for genre research, `simulation-parameter-analyst` for parameter balance

#### `technical-constraints-analyst`

**Role:** Technical Feasibility & Constraints Specialist

**Description:** Analyzes technical constraints for military simulation features, including performance requirements, computational complexity, memory limitations, and platform compatibility.

**Reports to:** `requirements-analyst`, `technical-director`
**Works with:** `performance-analyst` for optimization guidance, `engine-programmer` for implementation feasibility

#### `military-research-specialist`

**Role:** Military Technology & Doctrine Researcher

**Description:** Conducts research on military technologies, doctrines, and historical operations to inform simulation accuracy and authenticity. Uses internet research to gather authoritative sources.

**Reports to:** `military-simulation-architect`
**Works with:** `simulation-parameter-analyst` for parameter validation, `simulation-genre-analyst` for genre context

#### `simulation-genre-analyst`

**Role:** Military Simulation Genre Researcher

**Description:** Researches the military simulation genre to identify conventions, innovations, market trends, and player expectations. Analyzes competing products and genre evolution.

**Reports to:** `game-designer`, `creative-director`
**Works with:** `gameplay-mechanics-analyst` for mechanic context, `user-experience-military-analyst` for UX patterns

#### `user-experience-military-analyst`

**Role:** Military Simulation UX Specialist

**Description:** Analyzes user experience specific to military simulation games, focusing on information density, command complexity, and player cognitive load. Researches UX patterns in the genre.

**Reports to:** `ux-designer`, `military-simulation-architect`
**Works with:** `simulation-genre-analyst` for genre patterns, `gameplay-mechanics-analyst` for mechanic context

---

## Skill System

### Military Research Skills

#### `/military-database-research`

**Agent:** `military-research-specialist`
**Purpose:** Research authoritative military databases and specifications for accurate simulation parameters

**Outputs:** Structured research reports with specifications, sources, and implementation notes

**Argument modes:** `[topic: platform | weapon | sensor | doctrine | operation]`

#### `/historical-analysis-research`

**Agent:** `military-research-specialist`
**Purpose:** Research historical military operations for validation and scenario design

**Outputs:** Historical analysis with validation points and scenario insights

**Argument modes:** `[operation | era | conflict]`

---

### Genre Research Skills

#### `/genre-convention-analysis`

**Agent:** `simulation-genre-analyst`
**Purpose:** Analyze military simulation genre conventions and standards

**Outputs:** Genre convention analysis with standards and recommendations

**Argument modes:** `[subgenre: naval | air | ground | combined | comprehensive]`

#### `/competitive-intelligence`

**Agent:** `simulation-genre-analyst`
**Purpose:** Research competing military simulation products and features

**Outputs:** Competitive intelligence report with strategic insights

**Argument modes:** `[focus: features | mechanics | ui | market | all]`

---

### Analysis Skills

#### `/simulation-accuracy-validation`

**Agent:** `simulation-parameter-analyst`
**Purpose:** Validate simulation parameters against real-world data

**Outputs:** Validation report with discrepancies and recommendations

**Argument modes:** `[parameter type | all]`

#### `/gameplay-balance-analysis`

**Agent:** `gameplay-mechanics-analyst`
**Purpose:** Analyze gameplay balance for military simulation systems

**Outputs:** Balance analysis report with tuning recommendations

**Argument modes:** `[system | mechanic | all]`

#### `/technical-feasibility-assessment`

**Agent:** `technical-constraints-analyst`
**Purpose:** Assess technical feasibility of proposed simulation features

**Outputs:** Feasibility assessment with risk analysis and recommendations

**Argument modes:** `[feature | system | requirement]`

---

### UX Research Skills

#### `/ux-military-pattern-research`

**Agent:** `user-experience-military-analyst`
**Purpose:** Research UX patterns specific to military simulation interfaces

**Outputs:** UX pattern research report with implementation recommendations

**Argument modes:** `[interface type | all]`

---

### Integration Skills

#### `/military-requirements-impact`

**Agent:** `requirements-analyst`
**Purpose:** Analyze impact of proposed requirements on existing systems

**Outputs:** Impact analysis report with affected systems and recommendations

**Argument modes:** `[requirement | change]`

#### `/genre-innovation-opportunity`

**Agent:** `simulation-genre-analyst`
**Purpose:** Identify innovation opportunities within military simulation genre

**Outputs:** Innovation opportunity analysis with prioritized recommendations

**Argument modes:** `[focus: mechanics | systems | ui | all]`

---

## Integration with Existing Infrastructure

### Agent Coordination

**With Existing Leadership:**
- `military-simulation-architect` coordinates with `technical-director` and `game-designer`
- `requirements-analyst` coordinates with `producer` and `technical-director`
- All specialists follow existing collaborative protocols

**With Existing Specialists:**
- `simulation-parameter-analyst` works with `systems-designer` and `ai-programmer`
- `gameplay-mechanics-analyst` works with `game-designer` and `economy-designer`
- `technical-constraints-analyst` works with `engine-programmer` and `performance-analyst`
- `user-experience-military-analyst` works with `ux-designer` and `ui-programmer`

### Workflow Integration

**Requirements Analysis:**
- Use existing `/design-system` and `/architecture-decision` workflows
- Military specialists provide domain-specific input
- Integration skills ensure compatibility with existing architecture

**Research Integration:**
- Skills can be invoked independently or as part of larger workflows
- Research outputs feed into existing design and architecture processes
- Documentation follows existing template standards

---

## Internet Research Capabilities

### Research Sources and Strategies

The agents leverage internet research through the `WebFetch` tool to access:

**Authoritative Military Sources:**
- Jane's Defense Publications
- Official military technical manuals
- Defense industry publications
- Government defense databases
- Academic military research

**Gaming and Industry Sources:**
- Game developer forums and discussions
- Industry analysis and market reports
- Player community feedback and discussions
- Game review and analysis publications
- Game development conferences and talks

**Technical and Academic Sources:**
- Academic papers on simulation and modeling
- Technical documentation and standards
- Open-source military simulation projects
- Research on computational military modeling

### Research Quality Assurance

**Source Evaluation Criteria:**
- Authority and credibility of source
- Publication date and relevance
- Cross-reference validation
- Peer review or editorial oversight
- Domain expertise of authors

**Documentation Standards:**
- Always cite sources with URLs
- Note publication dates and version information
- Indicate confidence levels in gathered data
- Highlight discrepancies between sources
- Recommend primary sources when available

---

## File Structure

### Agent Files

```
.claude/agents/
├── military-simulation-architect.md
├── requirements-analyst.md
├── simulation-parameter-analyst.md
├── gameplay-mechanics-analyst.md
├── technical-constraints-analyst.md
├── military-research-specialist.md
├── simulation-genre-analyst.md
└── user-experience-military-analyst.md
```

### Skill Files

```
.claude/skills/
├── military-database-research/
│   └── SKILL.md
├── historical-analysis-research/
│   └── SKILL.md
├── genre-convention-analysis/
│   └── SKILL.md
├── competitive-intelligence/
│   └── SKILL.md
├── simulation-accuracy-validation/
│   └── SKILL.md
├── gameplay-balance-analysis/
│   └── SKILL.md
├── technical-feasibility-assessment/
│   └── SKILL.md
├── ux-military-pattern-research/
│   └── SKILL.md
├── military-requirements-impact/
│   └── SKILL.md
└── genre-innovation-opportunity/
    └── SKILL.md
```

### Documentation Files

```
docs/military-simulation/
├── military-simulation-agents-overview.md
├── military-research-sources-guide.md
└── genre-conventions-reference.md
```

---

## Usage Guidelines

### When to Use Military Simulation Agents

**Use `military-simulation-architect` when:**
- Making military simulation architecture decisions
- Balancing authenticity vs. gameplay trade-offs
- Designing command and control systems
- Setting combat mechanics and fidelity standards
- Coordinating military simulation specialists

**Use `requirements-analyst` when:**
- Analyzing requirements feasibility
- Evaluating technical constraints
- Assessing performance impact
- Estimating implementation complexity
- Assessing risks for new features

**Use `simulation-parameter-analyst` when:**
- Analyzing weapon system parameters
- Validating platform capabilities
- Verifying sensor system modeling
- Calculating detection and engagement
- Tuning electronic warfare parameters

**Use `gameplay-mechanics-analyst` when:**
- Analyzing combat mechanic balance
- Designing player progression systems
- Assessing tactical decision depth
- Optimizing learning curves
- Researching genre conventions

**Use `technical-constraints-analyst` when:**
- Analyzing performance budgets
- Assessing computational complexity
- Evaluating memory footprint
- Identifying real-time simulation constraints
- Analyzing platform compatibility

**Use `military-research-specialist` when:**
- Researching military technologies
- Analyzing historical operations
- Studying tactical doctrines
- Gathering platform and weapon specs
- Verifying military protocols

**Use `simulation-genre-analyst` when:**
- Analyzing genre conventions
- Researching market trends
- Analyzing competitor products
- Studying player expectations
- Identifying innovation opportunities

**Use `user-experience-military-analyst` when:**
- Analyzing information density
- Evaluating command complexity
- Assessing cognitive load
- Researching UX patterns
- Optimizing learning curves

---

## Success Metrics

### Quantitative Metrics

- **Agent Coverage:** 8 new agents created
- **Skill Coverage:** 10 new skills created
- **Research Sources:** Access to 20+ authoritative sources
- **Integration:** 100% compatibility with existing infrastructure
- **Testing:** All agents and skills pass validation tests

### Qualitative Metrics

- **Research Quality:** High-quality, credible research outputs
- **Analysis Accuracy:** Accurate simulation parameter analysis
- **Genre Understanding:** Deep understanding of military simulation conventions
- **Integration Success:** Seamless integration with existing workflows
- **User Satisfaction:** Agents and skills meet requirements analysis needs

---

## Conclusion

The military simulation game requirements analysis agents and skills provide Project Aegis with specialized domain expertise that integrates seamlessly with existing infrastructure while providing unique value to military simulation development. All agents and skills are non-redundant, complement existing capabilities, and follow established protocols and conventions.