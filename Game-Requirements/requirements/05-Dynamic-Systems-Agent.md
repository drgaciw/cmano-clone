# 05 - Dynamic Speculative Systems Agent

**Last Updated:** May 28, 2026

## Purpose
Create an autonomous agent that continuously monitors open-source intelligence (X.com, defense publications, research papers, and military forums) for emerging and speculative military technologies, then proposes new systems for integration into the game database with full human oversight.

## Vision
A living, evolving technology database that stays ahead of real-world developments. The agent acts as a dedicated “future warfare researcher” that discovers new concepts, generates realistic performance data, and keeps the game’s near-future and speculative content fresh and relevant without requiring constant manual research from the development team.

## Functional Requirements

### 1. Continuous Monitoring & Discovery
- Monitors X.com (Twitter) in real time using advanced search queries focused on:
  - Keywords: hypersonic, directed energy, drone swarm, loyal wingman, quantum radar, cognitive EW, autonomous underwater vehicle, railgun, anti-satellite, etc.
  - Accounts: defense analysts, think tanks, military journals, aerospace companies, and open-source intelligence accounts
- Scans additional sources (when permitted): defense news sites, arXiv papers, patent databases, and official DoD/DoD contractor releases
- Detects emerging trends and novel system concepts before they become mainstream

### 2. Proposal Generation
- When a promising new system is identified, the agent automatically creates a structured proposal including:
  - System name and category
  - Brief description and real-world context
  - Estimated technological readiness level (TRL)
  - Proposed gameplay role and mechanics
  - Suggested performance parameters (range, speed, signature, cost, etc.)

### 3. Automatic Stat Generation (with Human Approval)
- Uses source material + military domain knowledge to generate initial stats
- Clearly marks all generated values as “AI-proposed” with confidence scores
- Requires explicit human approval before any data is written to the live database
- Maintains full audit trail of every proposal and approval

### 4. Integration Workflow
1. Agent discovers new concept
2. Agent generates proposal + draft stats
3. Human reviews and approves/rejects/modifies
4. Approved system is added to the Database Intelligence Layer
5. System becomes available in Future Combat Mode or as an optional technology level

## Non-Functional Requirements

- Must respect rate limits and terms of service of all monitored platforms
- All proposals must include source citations for transparency
- Agent must never add unapproved data to the live game database
- Low false-positive rate — only high-quality, relevant concepts should be proposed

## Agentic Capabilities

- The Dynamic Speculative Systems Agent itself is fully agentic and can be prompted by Claude/Cursor to:
  - “Search for new hypersonic concepts from the last 30 days”
  - “Generate a speculative railgun system for 2035 based on current trends”
  - “Compare this new drone swarm idea against our existing swarm mechanics”
- Works in close coordination with the Database Intelligence Layer and Balance Tuning Agent

## Technical Considerations

- Built as a background service (can run 24/7)
- Uses X API (or approved scraping tools) + RSS feeds + optional LLM summarization
- Stores proposals in a staging database before human review
- Integrates with Unity-MCP so Claude can review and approve proposals directly inside the editor

## Future Extensibility

- Add more data sources (academic journals, defense contractor white papers, FOIA releases)
- Support for multi-language monitoring (Russian, Chinese technical discussions)
- Community-submitted speculative systems with automated vetting
- Automatic generation of 3D models or icons for new systems (future)

## Open Questions / Decisions Needed

1. Should the agent have a “confidence threshold” below which it does not even propose a system?
2. How often should the agent run (real-time vs. daily digest)?
3. Should there be a public “Speculative Systems” feed that players can browse and vote on?

---

**Status:** Ready for implementation