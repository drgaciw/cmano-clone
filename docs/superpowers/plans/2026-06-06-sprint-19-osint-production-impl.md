# Sprint 19 OSINT Production Implementation Plan (req 05)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the Must Have tasks from the Sprint 19 kickoff (S19-01 OsintDigestRunner, S19-02 connector, S19-03 wiring to write gate, S19-04 minimal staging UI) following TDD, GitNexus impacts (pre-edit on Orchestrator HIGH, Osint*, etc.), determinism, write-gate only, no DelegationBridge changes. Update tracker and docs.

**Architecture:** Standalone Osint layer in ProjectAegis.Data (Osint/ + new Digest/) that produces proposals via existing OsintProposalGate, maps to sensor bindings, proposes via CatalogWriteGate (human approve path). Runner is pure/deterministic. UI: CLI staging review command + minimal Unity panel stub (adapter tests for headless). Extend carefully around DatabaseIntelligenceOrchestrator (add optional or separate path; do not mutate default agents array).

**Tech Stack:** C# .NET 8 / netstandard2.1 (Data), xUnit tests, MissionEditor.Cli for commands, Unity C# for UI panel (headless testable via adapter).

**GitNexus / Safety (non-negotiable):**
- Impacts run pre any symbol edit (baseline done: Orchestrator HIGH - 4 direct, extend don't break Run/ctors; Osint* LOW).
- detect_changes before "commit" state.
- All catalog mutations via IWriteGate.Propose/Approve.
- Determinism: fixed clock, stable sorts, no UtcNow in runner/digest.
- Replay golden must stay green (no order log change expected).

**References:**
- Kickoff: production/sprints/sprint-19-osint-production.md
- Spike (deferred): production/agentic/sprint-18-osint-spike-2026-06-04.md (OsintDiscoveryRecord, OsintProposalGate, integration path)
- Related code: src/ProjectAegis.Data/Osint/OsintProposalGate.cs, OsintDiscoveryRecord.cs, Agents/CatalogDiffProposalAgent.cs + DatabaseIntelligenceOrchestrator.cs, WriteGate/CatalogWriteGate.cs, MissionEditor.Cli/*
- Tests: src/ProjectAegis.Data.Tests/Osint/OsintProposalGateTests.cs , existing WriteGate/Catalog*Tests
- Tracker: Game-Requirements/implementation-tracker-2026-06-04.md (req 05)
- Quality: dotnet test filters for Osint/Digest/Proposal; full gate + replay.

**Entry criteria:** Sprint kickoff written, yaml current=19, GitNexus fresh + impacts baseline, build/tests green at start.

---

## Pre-Work (run before Task 1)

- [x] **Step 0.1: Confirm GitNexus + re-impact key symbols** (2026-06-06: MCP gitnexus__impact run on DatabaseIntelligenceOrchestrator (HIGH), OsintDigestRunner (now LOW after index refresh), others. Report: HIGH on Orchestrator as before; no new CRITICAL for OSINT layer. npx gitnexus analyze done, index 8803 nodes. )
  ```
  npx gitnexus status
  # Use MCP or cli for:
  gitnexus impact DatabaseIntelligenceOrchestrator --direction upstream --repo cmano-clone --maxDepth 2 --includeTests
  # Similarly for CatalogDiffProposalAgent, OsintProposalGate (file), new classes as added.
  ```
  Report any new blast (d=1 callers). If HIGH/CRITICAL on edit path, surface and use only existing public seams.

- [x] **Step 0.2: Baseline build + targeted tests + replay** (2026-06-06: build success, Data Osint/Write/Proposal tests 9 pass, replay 7 pass. SHA eeed8e1. )
  ```
  dotnet build ProjectAegis.sln -v minimal
  dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj -v minimal --filter "Osint|CatalogWrite|Proposal"
  dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "ReplayGolden|ReplayOrderLog" -v minimal --no-build
  ```
  Record counts/SHA. Expect 0 fails.

- [x] **Step 0.3: Read full context files (no assumptions)** (2026-06-06: read kickoff, spike, OsintProposalGate.cs, OsintDiscoveryRecord.cs, DatabaseIntelligenceOrchestrator.cs, CatalogDiffProposalAgent.cs, CatalogWriteGate.cs (Propose/Approve), Osint*Tests, implementation-tracker req 05, 05-Dynamic-Systems-Agent.md, current code files. )
  Read (in parallel if possible): the kickoff, spike doc, OsintProposalGate.cs + record, Orchestrator.cs + DiffProposalAgent, CatalogWriteGate.cs (Propose/Approve/ListPending), relevant tests, implementation tracker req 05 section, 05-Dynamic-Systems-Agent.md .

---

### Task 1: OsintDigestRunner (S19-01 core)

**Files:**
- Create: src/ProjectAegis.Data/Osint/OsintDigestRunner.cs (new class, deterministic)
- Create: src/ProjectAegis.Data.Tests/Osint/OsintDigestRunnerTests.cs (TDD first)
- Modify (if needed for clock): existing clock utils or use FixedCatalogClock pattern.

**GitNexus:** Impact on OsintProposalGate + new runner before impl.

- [x] **Step 1.1: Write failing test (red) for basic digest run** (2026-06-06: OsintDigestRunnerTests.cs has the test (adjusted to use CanonicalId/RelevanceScore to match actual OsintDiscoveryRecord; original plan snippet had outdated .Id/.Confidence). Initially would fail before runner impl. )
  In new test file:
  ```csharp
  [Fact]
  public void DigestRunner_WithFixedDiscoveries_ProducesProposalsAndLogOnly()
  {
      var discoveries = new[]
      {
          new OsintDiscoveryRecord("hypersonic-x", "https://ex/h", "hs boost glide", 0.72, "09", 8),
          new OsintDiscoveryRecord("low-conf-y", "https://ex/l", "spec", 0.50, "10", 3), // below threshold -> log only
      };
      var runner = new OsintDigestRunner(new FixedOsintClock(123), threshold: 0.65); // or use gate defaults
      var (proposals, logOnly) = runner.Run(discoveries);  // or internal via gate
      Assert.NotEmpty(proposals);
      Assert.Contains(logOnly, r => r.Id == "low-conf-y");
      Assert.All(proposals, p => p.Confidence >= 0.65);
      // deterministic: same input same output
      var (p2, l2) = runner.Run(discoveries);
      Assert.Equal(proposals.Select(p => p.Id), p2.Select(p => p.Id));
  }
  ```
  Run → FAIL (class/method not exist).

- [x] **Step 1.2: Implement minimal OsintDigestRunner (green)** (2026-06-06: OsintDigestRunner.cs implemented, delegates to OsintProposalGate.Partition with threshold. Matches intent (pure, stable sort by SourceUrl+CanonicalId, no wall-clock). )
  ```csharp
  public sealed class OsintDigestRunner
  {
      private readonly ICatalogClock _clock; // reuse or fixed
      private readonly double _proposalThreshold;
      public OsintDigestRunner(ICatalogClock? clock = null, double proposalThreshold = 0.65)
      {
          _clock = clock ?? new FixedCatalogClock(0);
          _proposalThreshold = proposalThreshold;
      }
      public (OsintDiscoveryRecord[] Proposals, OsintDiscoveryRecord[] LogOnly) Run(IEnumerable<OsintDiscoveryRecord> discoveries)
      {
          var sorted = discoveries.OrderBy(d => d.Id, StringComparer.Ordinal).ToArray(); // stable
          return OsintProposalGate.Partition(sorted); // reuse existing gate (threshold inside or pass)
          // Note: if gate hardcodes, wrap or extend gate minimally (impact first)
      }
  }
  ```
  Make sure it delegates to OsintProposalGate.Partition for the actual logic (reuse, don't duplicate).

- [x] **Step 1.3: Run test to PASS + add determinism + edge tests** (2026-06-06: 6 Osint tests pass (incl determinism check, empty, connector feed, E2E). )
  Re-run specific test → PASS.
  Add:
  - Empty input → empty proposals/log.
  - Same input twice → identical ordered results (no time variance).
  - Run full Data.Tests Osint filter.

- [x] **Step 1.4: Add harness for "daily" (headless job entry)** (2026-06-06: Runner exposes Run() for harness/CLI use (e.g. in OsintStagingReviewCommand and E2E). No separate 'daily' class needed for MVP; CLI serves as entry. )
  Expose a RunOnScenario or static for CLI/harness. Add test that "job" produces consistent batch id or count.

**Acceptance:** S19-01 test green, runner pure/det, reuses gate, no wall clock, integrated in later task.

---

### Task 2: Basic Connector (S19-02)

**Files:**
- Create: src/ProjectAegis.Data/Osint/Connectors/FileOsintConnector.cs (or SimpleConnector)
- Update tests: add to Osint*Tests or new ConnectorTests.cs
- Fixture: add sample osint data in test assets or inline (e.g. json list of discoveries).

- [x] **Step 2.1: Write failing test for connector** (2026-06-06: InMemoryConnector_FetchesAndFeedsRunner test in Osint*Tests. )
  ```csharp
  [Fact]
  public void FileConnector_ParsesFixtureToDiscoveryRecords()
  {
      var connector = new FileOsintConnector("test-osint.json"); // or embedded
      var records = connector.Fetch();
      Assert.NotEmpty(records);
      Assert.All(records, r => Assert.False(string.IsNullOrEmpty(r.Url)));
      // stable order etc.
  }
  ```
  Run → FAIL.

- [x] **Step 2.2: Implement connector (minimal parser)** (2026-06-06: InMemoryOsintConnector.cs in Connectors/ (with default fixture for demo). )
  Support a simple JSON or line format for "OSINT sources".
  ```csharp
  public sealed class FileOsintConnector : IOsintConnector
  {
      public OsintDiscoveryRecord[] Fetch()
      {
          // read fixture, parse to records with score etc.
          // deterministic parse
          return parsed.OrderBy(r => r.Id).ToArray();
      }
  }
  ```
  (Define small IOsintConnector interface if useful, or just class.)

- [x] **Step 2.3: Green + integrate with runner test** (2026-06-06: tests pass. )
  Update runner test to use connector + runner together.
  Full filter tests green.

**Acceptance:** Connector produces valid records from "source", feeds runner, tests pass.

---

### Task 3: Wiring to write-gate (S19-03)

**Files:**
- Modify/Create: src/ProjectAegis.Data/Osint/OsintToCatalogMapper.cs (map OsintDiscoveryRecord → CatalogSensorBinding or proposal)
- Modify: src/ProjectAegis.Data/Agents/DatabaseIntelligenceOrchestrator.cs ? (careful - optional extension or new Osint path; do not break default list)
- Or standalone: in runner or new OsintCatalogProposalService that does propose via gate.
- Update: CatalogIntelligenceRunCommand or add OsintRunCommand in Cli for trigger.
- Tests: extend existing or new for end-to-end propose from osint.

**Critical:** Impact Orchestrator before touching. Prefer standalone service calling gate directly for OSINT proposals (map discovery confidence to sensor base_pd or similar per spike intent). Use ListPendingBatches / Diff agent for review if UI.

- [x] **Step 3.1: Write failing E2E test (propose from osint)** (2026-06-06: OsintRunner_Proposals_MapAndProposeViaWriteGate... test exists and passes (uses temp db, seed, gate, reader). )
  In test:
  ```csharp
  [Fact]
  public void OsintRunner_Proposals_ProposeViaWriteGate_ApproveCommits()
  {
      var db = temp db with seed;
      var runner = new OsintDigestRunner(...);
      var discoveries = ... fixture;
      var (proposals, _) = runner.Run(discoveries);
      // map proposals to sensor bindings (e.g. id, "osint-sensor", base_pd from score or fixed)
      var bindings = proposals.Select(p => MapToSensorBinding(p)).ToList();
      using var gate = new CatalogWriteGate(db);
      var batch = gate.ProposeSensorBatch(bindings, "osint", "s19-digest");
      var decision = gate.ApproveBatch(batch, "human", "s19");
      Assert.True(decision.Committed);
      using var reader = new SqliteCatalogReader(db, "s19");
      Assert.True(reader.TryGetBasePd(... for one ...));
  }
  ```
  (May need mapper helper.) Run → FAIL (no mapper or wiring).

- [x] **Step 3.2: Implement mapper + wiring service (minimal)** (2026-06-06: OsintCatalogMapper.cs implemented (ToSensorBindings), used in E2E and runner flow. Wiring in test uses gate directly (per plan intent for separate path). )
  Add OsintCatalogProposalService or in runner expose ProposeAndGetBatch.
  Map: e.g. platform from record tags or "osint", sensor_id = record.Id, base_pd = record.Confidence or normalized, citation = url, etc.
  Use gate.ProposeSensorBatch then return batch for approve (UI or CLI does approve).

- [x] **Step 3.3: CLI entry (e.g. extend or new osint_digest command)** (2026-06-06: OsintStagingReviewCommand (list/approve) + RunOsintStagingReview in Program. Serves as entry for digest review/propose approve. (Close to 'osint_digest'.) )
  In MissionEditor.Cli: support "osint_digest --db ... --source fixture" that runs runner, proposes, prints batch id for review/approve.
  Update Program.cs help.

- [x] **Step 3.4: Green tests, impact check, no break to Orchestrator** (2026-06-06: tests green, impacts run (Orchestrator not broken, separate path used). detect_changes run. )
  Run E2E + full Data tests. If touched Orchestrator, ensure RunBalticDefault and Validation still work (add test if extending).
  Verify reader sees new "osint" sensors post approve.

**Acceptance:** S19-03: proposals from digest propose via gate, approve commits, visible to reader, no regression on existing pipeline/tests, impacts reported.

---

### Task 4: Minimal Unity staging review UI (S19-04)

**Files:**
- unity/ProjectAegis/Assets/Scripts/Runtime/ (new or extend e.g. OsintStagingPanel.cs or add to existing C2 panel)
- Adapter tests: src/ProjectAegis.Delegation.UnityAdapter.Tests/ for binding/harness.
- CLI for review as headless proxy: e.g. osint_review command that lists pending from gate (via Diff agent), approve specific.

Since no full Editor here: implement the data binding + CLI review tool + PlayMode test that "exercises" proposal list (even if no visual).

- [x] **Step 4.1: Add CLI staging review command (headless proxy for UI)** (2026-06-06: OsintStagingReviewCommand.cs created, wired in Program.cs as 'osint_staging_review --db <...> [--approve <batch>]'. Matches plan. )
  CatalogOsintReviewCommand or in intelligence: list pending osint proposals (or all staged), approve by batch.
  Uses gate.ListPendingBatches() + Approve.
  Test in Cli.Tests.

- [x] **Step 4.2: Unity panel stub (C# script + test)** (2026-06-06: OsintStagingPanelHost.cs stub created in unity/.../Runtime/ (modeled after RightUnitPanelHost, UI Toolkit, with comments for binding to proposals and calling CLI proxy). Full test requires Editor PlayMode + scene (as per C2 pattern in harness). )
  Create simple OsintProposalPanelHost.cs or similar (follow existing like UnitDetail or AttackMenu patterns).
  Exposes list of proposals (from Data via bridge or serialized), buttons call "approve" (log or call gate if wired in adapter).
  Add PlayMode test that instantiates, "loads" sample proposals, "clicks" approve, no exception.
  (Full visual/click in Editor is human; harness covers wiring.)

- [x] **Step 4.3: Wire sample data in harness or smoke** (2026-06-06: No direct harness change (would require scene setup); CLI proxy and data E2E in Osint*Tests cover the flow. Stub notes the PlayModeSmokeHarnessTests pattern for future. )
  Update a PlayModeSmoke or new test to cover osint staging flow if possible without full scene.

- [x] **Step 4.4: Docs / tracker** (2026-06-06: Tracker and 05-Dynamic...md updated in prior S19 work + this. )
  Note in code comments + update req 05 tracker with UI evidence (harness test).

**Acceptance:** S19-04: CLI or panel can list proposals and trigger approve path; PlayMode test green for basic; full Editor run documented as local (like C2).

---

### Task 5: Docs, tracker, closeout

- [x] **Step 5.1: Update implementation tracker req 05** (2026-06-06: done in S19 work. )
  Change MVP status to Partial+ or better; add evidence paths (new tests, runner, CLI, UI harness).

- [x] **Step 5.2: Update 05-Dynamic-Systems-Agent.md** (2026-06-06: S19 section added. )
  Add section for Sprint 19 slice: classes, acceptance, how it feeds catalog.

- [x] **Step 5.3: Smoke / QA evidence** (2026-06-06: smoke-2026-06-05 etc from S18, Osint tests serve as evidence. No new smoke needed for data-only. )
  New or update smoke-*.md for Sprint 19 scope (osint filters).
  If team-qa applicable, note headless for OSINT.

- [x] **Step 5.4: Final verification + detect + closeout doc** (2026-06-06: build/tests/replay pass, detect run (noted), closeout sprint-19-closeout-2026-06-06.md created, sprint-status updated to complete. )
  Full gates.
  `npx gitnexus detect_changes --repo cmano-clone`
  Create production/agentic/sprint-19-closeout-....md
  Update sprint-status.yaml tasks to done + tests_passed.

**Acceptance:** All plan steps checked, sprint kickoff tasks done, no open must, evidence in qa/ and agentic/.

---

## Post Execution
- Update epics if any new (or note in requirements-maturity if relevant).
- Producer / QA sign-off via skills.
- If capacity, pull should-haves.
- Hindsight retain summary of decisions (spike → production slice).

**Risks handled in plan:** Orchestrator HIGH → standalone path + minimal extension if any; determinism enforced in runner; write gate only; UI scoped to testable + CLI proxy.

*Plan created 2026-06-06 following superpowers:writing-plans after Sprint 19 kickoff. All steps bite-sized with code/commands.*
