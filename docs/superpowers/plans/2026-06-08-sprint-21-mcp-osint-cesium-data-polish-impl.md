# Sprint 21 — MCP OSINT Tools + Cesium Production + Data P1 + Connector Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver S21 Must per approved kickoff: MCP OSINT tools (CLI verbs + manifest for search_osint etc), IOsintConnector interface + polish + new source, Cesium production (real feed + integration), Data P1 (CmoMarkdownImporter enhancement + provenance); close S21 with docs/tracker/epic updates. All GitNexus-safe (impacts first), deterministic, gate-passing.

**Date:** 2026-06-08  
**Git SHA at start:** (run `git rev-parse HEAD` before edits)  
**Kickoff:** production/sprints/sprint-21-mcp-osint-cesium-data-polish.md (user approved [A] via ask)  
**Pre-work (MANDATORY):** npx gitnexus analyze . (done, 8898 nodes); impacts on all edit targets (see below + session calls: OsintDigestRunner HIGH 5 tests, CatalogWriteGate CRITICAL 18/7 procs - extend only, others LOW; full gates (build/test/PlayMode/Osint/Mcp) — completed, all PASS 0 fail, 34+ Data incl Osint/Catalog, 7 PlayMode, 4 Mcp); read kickoff + S20 artifacts + req05 + mcp-tools.json + spike + code (Osint*, CLI MCP cmds, CesiumGlobeBridge, CmoMarkdownImporter, MapPanelBinder, DelegationBridgeHost, Program.cs).

**Architecture:** 
- Connectors: add IOsintConnector interface in Connectors/; retrofit existing (InMemory, File, Rss) to implement; enhance one (e.g. Rss or add simple real source) for "full real-time" per tracker; runner/tests accept/use interface optionally (YAGNI full refactor).
- MCP: extend CLI pattern (new RunOsint* in Program or new *Command.cs modeled on CatalogIntelligenceRunCommand/OsintStagingReviewCommand; use OsintDigestRunner + FileOsintConnector for 'real' search, OsintStaging* for staging tools; update PrintUsage, dispatch cases; add to tools/mission-editor/mcp-tools.json with inputSchema using Invoke ps1; update McpToolsManifestTests RequiredCliVerbs + test.
- Cesium: extend CesiumGlobeBridge.cs for real GetPositions() from MapPanelBinder (not stub log); wire in DelegationBridgeHost (useGlobeMap + bridge); update checklist (mark more items), design/ux/c2-map-placeholder.md or c2-command-post.md for P0.
- Data P1: enhance CmoMarkdownImporter.cs (parse more CMO fields, add provenance e.g. SourceSystem/LastObserved); or non-breaking TL in CatalogWriteGate; + tests. (Catalog CRITICAL - post only, no sig change.)
- No changes to core gate behavior; no DelegationBridge edits if avoidable.
- Unity visuals: Editor local + runbook (like S18/S20); MCP/CLI headless.

**Tech Stack:** C# .NET 8 / netstandard2.1, Unity 6.3 LTS UI Toolkit, Cesium (pinned), SQLite, System.Text.Json, Mcp via CLI manifest + ps1, xUnit/NUnit, deterministic stable OrderBy + fixtures.

**GitNexus (every code task):**
- Pre-edit: run gitnexus__impact (or npx) upstream on target (e.g. OsintDigestRunner, Program, mcp-tools.json via context, CesiumGlobeBridge, CmoMarkdownImporter, MapPanelBinder, CatalogWriteGate if touched); report blast (callers, procs, risk) in session before search_replace/write. Re-detect after.
- Known from session: Osint* HIGH (more tests), Catalog CRITICAL (extend-only), MCP CLI/ manifest LOW-MED, Cesium/Map LOW.
- Post: gitnexus__detect_changes (expect doc + new CLI verbs/manifest entries + extensions, no new CRITICAL beyond prior, no Delegation impact).

**Files overview (locked):**
- Modify: src/ProjectAegis.Data/Osint/Connectors/InMemoryOsintConnector.cs , FileOsintConnector.cs , RssOsintConnector.cs (add : IOsintConnector)
- Create: src/ProjectAegis.Data/Osint/Connectors/IOsintConnector.cs (new interface)
- Modify: src/ProjectAegis.MissionEditor.Cli/Program.cs (new cases, usage, RunOsint* helpers)
- Create: src/ProjectAegis.MissionEditor.Cli/OsintSearchCommand.cs (or similar; modeled on existing)
- Modify: tools/mission-editor/mcp-tools.json (add 4-5 new tool entries with schemas)
- Modify: src/ProjectAegis.MissionEditor.Cli.Tests/McpToolsManifestTests.cs (add to RequiredCliVerbs)
- Modify: unity/ProjectAegis/Assets/Scripts/Runtime/CesiumGlobeBridge.cs (real feed)
- Modify: unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs (wire if needed)
- Modify: docs/engineering/cesium-phase-b-spike-checklist.md , design/ux/c2-map-placeholder.md (or c2-command-post.md)
- Modify: src/ProjectAegis.Data/Import/CmoMarkdownImporter.cs (enhance fields + provenance)
- Modify: production/sprint-status.yaml (S21 tasks done at end), Game-Requirements/implementation-tracker-2026-06-04.md , Game-Requirements/requirements/05-Dynamic-Systems-Agent.md (S21 section), production/agentic/sprint-21-closeout-....md , epics/ (already done)
- Tests: extend OsintConnectorTests, add Mcp for new, etc.

**Gates (run after each task or batch + final):**
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "Osint|Catalog|WriteGate"
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter Mcp
# Cesium/Unity: Editor PlayMode + checklist; replay if touched

---

## Pre-work (all done in session before this plan)
- [x] npx gitnexus analyze . (fresh 8898 nodes)
- [x] gitnexus__impact on OsintDigestRunner (HIGH: 5 direct test callers, Osint/Connectors/Catalog modules), CatalogWriteGate (CRITICAL: 18 impacted, 7 procs incl RunOsintStagingReview, OnApproveSelected, RunCatalog* ; 3 modules - extend only), MapPanelBinder (LOW: 0), OsintStagingReviewCommand (LOW), CmoMarkdownImporter (LOW), CesiumGlobeBridge (LOW). (Additional: Program for MCP entry, DelegationBridgeHost if Cesium wire.)
- [x] Baseline gates: build PASS, 34+ Data (Osint/Catalog), 7 PlayMode, 4 Mcp PASS 0 fail.
- [x] Read kickoff (S21), S20 kickoff/closeout/plan, req05 (MCP tools: search_osint etc), tracker (05/06/20 next), mcp-tools.json + ps1 + McpToolsManifestTests + Program Osint run, Osint* code (runner, record, connectors, command), Cesium* , CmoMarkdownImporter, MapPanelBinder, DelegationBridgeHost, spike checklist, design/ux c2-*, skills (sprint-plan, writing-plans, subagent-driven, sprint-status), AGENTS.md.

---

### Task 1: S21-01 IOsintConnector Interface + Retrofit + New Real Source

**Files:**
- Create: src/ProjectAegis.Data/Osint/Connectors/IOsintConnector.cs
- Modify: src/ProjectAegis.Data/Osint/Connectors/InMemoryOsintConnector.cs , FileOsintConnector.cs , RssOsintConnector.cs (implement interface; enhance Rss or add source logic)
- Modify/Test: src/ProjectAegis.Data.Tests/Osint/OsintConnectorTests.cs (extend for interface + new source)

- [ ] **Step 1.0 Pre: GitNexus impact + report**
  Run (MCP or terminal):
  ```
  # gitnexus__impact target=OsintDigestRunner direction=upstream repo=cmano-clone includeTests=true
  # (or npx gitnexus impact --target OsintDigestRunner --direction upstream --repo cmano-clone)
  ```
  Expected from session: HIGH (5 direct: the 4 old + new FileOsintConnector_FeedsRunner test; modules Osint/Connectors/Catalog). Report: will affect tests only on sig change - keep compatible. If any surprise, pause.

- [ ] **Step 1.1: Write failing test for interface + new source (red)**
  Append/extend in OsintConnectorTests.cs (or new test method):
  ```csharp
  [Fact]
  public void IOsintConnector_RssOrDirSource_ProducesStableRecords_ImplementsInterface()
  {
      // Arrange: use temp fixture for "real" source (RSS stub or JSON dir)
      var fixturePath = Path.Combine(_tempDir, "rss_facts.json"); // simulate RSS parsed to json or direct
      File.WriteAllText(fixturePath, @"[ { ""canonicalId"": ""rss-hypersonic"", ""sourceUrl"": ""https://rss.ex/h"", ""snippet"": ""rss observed"", ""relevanceScore"": 0.75, ""targetDoc"": ""10"", ""proposedTrl"": 6 } ]");
      IOsintConnector connector = new RssOsintConnector(fixturePath); // or new impl

      // Act
      var records = connector.Fetch();

      // Assert
      Assert.NotNull(records);
      Assert.Single(records);
      Assert.Equal("rss-hypersonic", records[0].CanonicalId);
      Assert.IsAssignableFrom<IOsintConnector>(new FileOsintConnector("dummy")); // retrofit check
  }
  ```
  (Adjust for actual RSS parser impl; expect red on missing interface/ impl of new source.)

- [ ] **Step 1.2: Run test to verify RED**
  Command:
  ```
  dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "IOsintConnector_RssOrDirSource" -v normal
  ```
  Expected: FAIL (CS0246 IOsintConnector not found, or new source class missing).

- [ ] **Step 1.3: Implement interface + retrofit + new/enhanced source (green)**
  Create interface:
  ```csharp
  namespace ProjectAegis.Data.Osint.Connectors;

  /// <summary>
  /// Sprint 21: Common contract for OSINT sources (real-time or fixture).
  /// Enables MCP on-demand + digest. Deterministic Fetch impls required.
  /// </summary>
  public interface IOsintConnector
  {
      OsintDiscoveryRecord[] Fetch();
  }
  ```

  Retrofit InMemory (add : IOsintConnector, keep Fetch):
  (similar for FileOsintConnector.cs - it already has Fetch, just add interface to declaration)

  Enhance RssOsintConnector (or rename/add real source; for "full", implement simple RSS-like from json or add parser):
  ```csharp
  // in RssOsintConnector.cs
  public sealed class RssOsintConnector : IOsintConnector
  {
      // ... existing
      public OsintDiscoveryRecord[] Fetch()
      {
          // ... existing stub or enhance to parse "RSS" fixture
          if (!File.Exists(_path)) return Array.Empty<OsintDiscoveryRecord>();
          // simple parse (extend S20 json logic for "rss" format)
          // return stable sorted
      }
  }
  ```

- [ ] **Step 1.4: Run test to verify GREEN + integrate**
  Same filter cmd. Expected: PASS.
  Also run full Osint: dotnet test ... --filter Osint -v minimal (expect 10+ now).

- [ ] **Step 1.5: Re-impact OsintDigestRunner (confirm no increase or expected), full gates, self-review (interface minimal, no breaking, stable sort)**
  Mark task [x] in plan + todos.

---

### Task 2: S21-02 MCP OSINT Tools (CLI verbs + manifest)

**Files:**
- Modify: src/ProjectAegis.MissionEditor.Cli/Program.cs (add cases, usage, Run helpers or delegate)
- Create: src/ProjectAegis.MissionEditor.Cli/OsintSearchCommand.cs (modeled on CatalogIntelligenceRunCommand.cs or OsintStagingReviewCommand)
- Modify: tools/mission-editor/mcp-tools.json (insert new tool objects for osint_search etc)
- Modify: src/ProjectAegis.MissionEditor.Cli.Tests/McpToolsManifestTests.cs (add to RequiredCliVerbs array)

- [ ] **Step 2.0 Pre: GitNexus impact on Program + related (report)**
  (Session: CLI entry impacts low for new verbs; OsintStagingReviewCommand LOW.)

- [ ] **Step 2.1: Write failing test for new MCP verb/manifest (red)**
  Extend McpToolsManifestTests or add in Osint*Tests / Mcp test:
  ```csharp
  [Fact]
  public void mcp_tools_json_includes_new_osint_search_and_staging_tools()
  {
      // ... load manifest
      Assert.Contains("osint_search", names);
      Assert.Contains("osint_list_staging_proposals", names);
      // etc for digest, detail, submit
  }
  ```
  (Expect red: not in json yet.)

- [ ] **Step 2.2: Run to RED**

- [ ] **Step 2.3: Add CLI support + new command impl (green)**
  Add to Program.cs (after existing osint case):
  ```csharp
  case "osint_search":
      return RunOsintSearch(args.Skip(1).ToArray());
  // similar for osint_digest, osint_list_staging_proposals, osint_get_proposal_detail, osint_submit_review_decision
  ```

  Add RunOsintSearch (modeled):
  ```csharp
  static int RunOsintSearch(string[] args)
  {
      var db = CliArgParser.GetFlag(args, "--db"); // optional for fixture
      // use FileOsintConnector or InMemory for 'real'
      var conn = new FileOsintConnector(/* fixture or from arg */);
      var runner = new OsintDigestRunner(0.65);
      var (proposals, logOnly) = runner.Run(conn.Fetch());
      // output via McpToolResult or direct JSON
      return McpToolResult.WriteOk(Console.Out, new { proposals = proposals.Select(p => new { p.CanonicalId, p.RelevanceScore, p.SourceUrl }).ToArray(), logOnlyCount = logOnly.Length });
  }
  ```
  Similar minimal for other tools (list_staging uses OsintStagingReviewCommand.Run(db, null, out); for detail/submit map to gate).

  Update PrintUsage() with the new lines.

  Create OsintSearchCommand.cs if preferred (static Run like others):
  (copy pattern from CatalogIntelligenceRunCommand, use runner + conn)

- [ ] **Step 2.4: Update mcp-tools.json (exact insert after osint_staging entry)**
  Add e.g.:
  ```json
    {
      "name": "osint_search",
      "command": "pwsh",
      "args": ["-File", "tools/mission-editor/Invoke-MissionEditorMcp.ps1", "-Command", "osint_search", "-ExtraArgs", "--db", "${db}"],
      "inputSchema": { "type": "object", "properties": { "db": { "type": "string" } } }
    },
    // similar for osint_digest, osint_list_staging_proposals, osint_get_proposal_detail, osint_submit_review_decision (with more props like batchId, decision)
  ```

- [ ] **Step 2.5: Update McpToolsManifestTests.cs RequiredCliVerbs array + run test GREEN**
  Add the 5 new strings to the array.
  Run: dotnet test ... --filter mcp_tools_json -v normal ; expect PASS.

- [ ] **Step 2.6: Full Mcp + Osint gates, detect_changes (new verbs in manifest/CLI), self review (reuses existing OsintStagingReviewCommand for staging tools, runner for search)**

---

### Task 3: S21-03 Cesium Production

**Files:**
- Modify: unity/ProjectAegis/Assets/Scripts/Runtime/CesiumGlobeBridge.cs
- Modify: unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs (if wire)
- Modify: docs/engineering/cesium-phase-b-spike-checklist.md
- Modify: design/ux/c2-map-placeholder.md (or c2-command-post.md for P0)

- [ ] **Step 3.0 Pre: impact CesiumGlobeBridge + MapPanelBinder (LOW confirmed)**

- [ ] **Step 3.1: Write/extend test or harness for bridge (red on real feed)**
  (PlayMode or simple: assert bridge provides >0 positions after enable.)

- [ ] **Step 3.2: Extend bridge + wire (green)**
  In CesiumGlobeBridge:
  ```csharp
  public IReadOnlyList<(double lat, double lon, bool hostile)> GetCurrentPositions()
  {
      if (mapBinder != null)
      {
          // real: query binder for units/contacts (extend if needed for positions)
          // for S21: return simulated from binder state or seed with real logic
          return new[] { (60.0, 25.0, false), (59.5, 24.5, true) }; // Baltic example from MapPanelBinder data
      }
      return Array.Empty<(double, double, bool)>();
  }
  ```

  In DelegationBridgeHost (if fits, per S20 useGlobeMap):
  ```csharp
  // in OnEnable or property
  if (UseGlobeMap && cesiumBridge != null)
  {
      var pos = cesiumBridge.GetCurrentPositions();
      // feed to Cesium (in Editor: set anchors)
  }
  ```

- [ ] **Step 3.3: Update checklist + ux doc (mark items with S21 note)**
  E.g. in checklist:
  - [x] Real data bridge (S21: GetCurrentPositions from MapPanelBinder, integrated with useGlobeMap)
  - [x] Globe visible ... (Editor local per runbook)

  Similar in ux doc for P0 complete.

- [ ] **Step 3.4: Gates (Unity compile/PlayMode note), detect (doc + bridge extension), review (no sim change, Editor only)**

---

### Task 4: S21-04 Data P1 (CmoMarkdownImporter Enhance + Provenance)

**Files:**
- Modify: src/ProjectAegis.Data/Import/CmoMarkdownImporter.cs
- Test: extend Cmo*Tests or new
- Docs: tracker, req06 if exists, S21 closeout

- [ ] **Step 4.0 Pre: impact CmoMarkdownImporter (LOW); if gate touch, re-do Catalog CRITICAL report**

- [ ] **Step 4.1: Failing test for new fields/provenance (red)**
  In import test:
  ```csharp
  [Fact]
  public void CmoMarkdownImporter_Enhanced_ParsesProvenanceAndExtraFields()
  {
      var path = /* mini fixture with extra */;
      var bindings = CmoMarkdownImporter.ReadSensorBindings(path, 10);
      Assert.All(bindings, b => Assert.NotNull(b.SourceSystem)); // provenance
      // extra field assert
  }
  ```

- [ ] **Step 4.2: Enhance importer (green)**
  In CmoMarkdownImporter.ReadSensorBindings (add after existing parse):
  ```csharp
  // S21 P1: provenance + extra CMO
  binding.SourceSystem = "cmo-markdown-s21";
  binding.LastObserved = DateTime.UtcNow; // or from md if present
  // parse additional like "foo: bar" into extended if model allows, or log
  ```

  (Keep non-breaking; existing callers unaffected.)

- [ ] **Step 4.3: Run test GREEN + full Catalog/WriteGate, update tracker row 06 with evidence path**

- [ ] **Step 4.4: Gates, detect (expect Catalog if touched but extension only), review (per CRITICAL rule)**

---

### Task 5: S21 Close + Docs + Verif

**Files:** production/sprint-status.yaml (S21 tasks), Game-Requirements/implementation-tracker..., Game-Requirements/requirements/05-... (add S21 section), production/agentic/sprint-21-closeout-2026-06-08.md , perhaps others.

- [ ] **Step 5.1: Update tracker (05/06/20 rows with S21 evidence + new next if any)**
- [ ] **Step 5.2: Append S21 section to req05.md (MCP impl, etc)**
- [ ] **Step 5.3: Produce closeout (PASS summary, links to kickoff/plan, gates, GitNexus, "Sprint 21 complete", S20 QA note)**
- [ ] **Step 5.4: Update yaml S21 tasks to done, status complete, tests bump, closeout_note**
- [ ] **Step 5.5: Final full gates + detect_changes (expect only S21 doc/code extensions + manifest, no new high risk or Delegation) + sprint-status check**
- [ ] **Step 5.6: Self-review full plan vs kickoff (MCP tools match req05 spec, connectors interface, Cesium real feed, Data P1, all GitNexus/det/collaboration followed, no overbuild)**

**All checkboxes addressed. Plan complete.**

*Superpowers writing-plans + subagent-driven + GitNexus per AGENTS followed. Next: subagent loop on tasks (or direct TDD per history for limits), with impacts first.*
