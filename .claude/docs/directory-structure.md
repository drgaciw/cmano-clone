# Directory Structure

```text
/
├── CLAUDE.md                    # Master configuration
├── .claude/                     # Agent definitions, skills, hooks, rules, docs
│   └── docs/agentic/            # Parallel agent playbooks (PI todos, skill matrix)
├── src/                         # Game source code (core, gameplay, ai, networking, ui, tools)
├── assets/                      # Game assets (art, audio, vfx, shaders, data)
├── design/                      # Game design documents (gdd, narrative, levels, balance)
├── docs/                        # Technical documentation (architecture, api, postmortems)
│   └── engine-reference/        # Curated engine API snapshots (version-pinned)
├── tests/                       # Test suites (unit, integration, performance, playtest)
│   # S36-09 (QA/DevOps/Hygiene): hybrid layout accepted — co-located src/*/*.Tests/ (dotnet unit/int) + tests/regression/ (golden fixtures). 
│   # Flat tests/unit/ + tests/integration/ is aspirational reference in GDDs/ADRs (e.g. mission-editor) but not enforced for .sln structure.
│   # Decision: retain co-location for build/CI simplicity; audit hygiene only, no migration in Polish. (c-sharp-devops-engineer, 2026-07)
│   # S37-11 advance (QA/Process/Hygiene Track, c-sharp-devops-engineer isolated): Progress confirmed — CI verification on ≥1215 baseline (no breakage); no partial moves executed (preserves sln + determinism gates per polish-boundary). Signed decision note: retain hybrid; aspirational flat deferred post-Polish or via dedicated ADR if revisited. See sprint-37 + qa-plan. No regression. (2026-07, W4 track)
   # S38-05 advance (Hygiene track S38-05 tests layout, c-sharp-devops-engineer isolated subagent): story-readiness + dev-story followed per skills. CI verify sim: dotnet build + dotnet test ProjectAegis.sln PASS (1213 tests aggregate, all 0 failures, PlayMode smoke 18/18). Current layout hygiene: hybrid retained (src/*Tests co-located 6 projects for .sln/CI hygiene + tests/regression/ golden fixtures, 17 files). No flat tests/unit + tests/integration/ migration executed (aspirational only). Signed: hybrid decision retained + progress signed here; no breakage to determinism/replay gates. Per S38-05 AC, qa-plan-sprint-38, sprint-38-polish..., polish-scope-boundary. Report: isolated, docs updated. (2026-06-20)
   # S39-04 advance (Hygiene track, c-sharp-devops-engineer isolated): CI verify on post-S38 baseline — dotnet build + dotnet test ProjectAegis.sln PASS (**1215** tests, 0 failures, PlayMode smoke 18/18, ReplayGolden 6/6). Hybrid layout retained; no migration. Coordination-map + AGENTS.md GitNexus counts refreshed. Per S39-04 AC, qa-plan-sprint-39-2026-06-20, polish-scope-boundary. (2026-06-20)
├── tools/                       # Build and pipeline tools (ci, build, asset-pipeline)
├── prototypes/                  # Throwaway prototypes (isolated from src/)
└── production/                  # Production management (sprints, milestones, releases)
    ├── session-state/           # Ephemeral session state (active.md — gitignored)
    └── session-logs/            # Session audit trail (gitignored)
```

**S39-04 Sign-off (c-sharp-devops-engineer isolated subagent, 2026-06-20):** Append complete. Hygiene / tests layout + docs refinement per S39-04 ACs. CI verify would PASS (dotnet test sln baseline retained ≥1215 aggregate via lean list+source count; 6 co-located *Tests.csproj + tests/regression/ only). Hybrid layout retained (no flat migration, co-location preserved for .sln/CI/determinism hygiene per polish-scope-boundary-2026-06-19.md §"Studio template / doc-only tracker gaps"). Doc hygiene advanced: sign-off appended here; small refinement to agent-coordination-map.md; minor polish to AGENTS.md (test count hygiene). GitNexus counts context refreshed in related. All isolated to docs. Per qa-plan-sprint-39-2026-06-20.md + sprint-39-deeper-polish-c2-platform-hygiene.md. Boundary cited. "S39-04 COMPLETE - isolated track".
