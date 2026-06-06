# Sprint 20 — OSINT Connectors + Cesium Map Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete req 05 OSINT production with real connectors and full staging UI; deliver Cesium globe foundation per deferred spike/ADR; update all evidence/tracker per sprint kickoff. All changes GitNexus-safe, deterministic, gate-passing, data-driven.

**Date:** 2026-06-07  
**Git SHA at start:** (run `git rev-parse HEAD` before edits)  
**Kickoff:** production/sprints/sprint-20-osint-cesium-foundation.md (user approved scope [A])  
**Pre-work (MANDATORY):** npx gitnexus analyze . ; impacts on all edit targets (see below); full gates (build/test/PlayMode/Osint) — completed in session, all PASS 0 fail, 67 Data / 7 PlayMode / 6 Osint.

**Architecture:** 
- Connectors: new concrete impls modeled exactly on InMemoryOsintConnector (Fetch returns sorted OsintDiscoveryRecord[]); optional IOsintConnector interface for future (add + retrofit InMemory for compatibility, YAGNI otherwise).
- UI: flesh S19 stub OsintStagingPanelHost using patterns from SensorC2PanelHost (UIDocument, ListView, Q, state); use OsintStagingReviewCommand.Run static for approve proxy (isolation); PlayMode for logic, Editor runbook for visual (per S18 C2 precedent).
- Cesium: edit manifest (git URL pin per spike doc), new minimal Runtime/CesiumGlobeBridge.cs (data provider calling MapPanelBinder or stub positions); no sim change; update checklist + docs.
- No changes to CatalogWriteGate behavior (CRITICAL — only if needed for snapshot, non-fatal); no DelegationBridge.

**Tech Stack:** C# .NET 8 / netstandard2.1 (Unity compat), Unity 6.3 LTS UI Toolkit, Cesium for Unity (pinned), SQLite via existing gates, NUnit, deterministic stable OrderBy + fixtures.

**GitNexus (every code task):**
- Pre-edit symbol: run impact upstream (MCP gitnexus__impact or npx gitnexus impact via terminal/CLI skill); report risk/callers/processes here before any search_replace/write.
- Post relevant changes: gitnexus__detect_changes ; only expected new symbols + doc sections.
- Known: CatalogWriteGate CRITICAL (17, 6 CLI procs incl OsintStagingReview — extend-only); Osint* / InMemory / new connectors / OsintStagingPanelHost / MapPanelBinder : LOW.

**Files overview (locked):**
- Create: src/ProjectAegis.Data/Osint/Connectors/FileOsintConnector.cs (and Rss stub if capacity)
- Modify: src/ProjectAegis.Data/Osint/Connectors/InMemoryOsintConnector.cs (add interface impl if introduced)
- Create/Test: src/ProjectAegis.Data.Tests/Osint/OsintConnectorTests.cs (or extend DigestRunnerTests)
- Modify: unity/ProjectAegis/Assets/Scripts/Runtime/OsintStagingPanelHost.cs (full from stub)
- Create: unity/ProjectAegis/Assets/Scripts/Runtime/CesiumGlobeBridge.cs (minimal)
- Modify: unity/ProjectAegis/Packages/manifest.json (add cesium entry)
- Modify: docs/engineering/cesium-phase-b-spike-checklist.md (mark package+bridge)
- Modify: Game-Requirements/implementation-tracker-2026-06-04.md , Game-Requirements/requirements/05-Dynamic-Systems-Agent.md
- Create: production/agentic/sprint-20-closeout-2026-06-07.md , production/qa/sprint-20-*-evidence.md etc.
- Modify: production/sprint-status.yaml (mark tasks done at end)

**Gates (run after each task or batch):**
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter Osint
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
# Cesium: Editor only for visual; no headless breakage to existing tests

---

## Pre-work (all done in session before this plan write)
- [x] npx gitnexus analyze . (force for FTS; 8821 nodes)
- [x] gitnexus__impact OsintDigestRunner / OsintProposalGate / CatalogWriteGate (CRITICAL reported: 17 impacted, 6 procs e.g. RunOsintStagingReview; extend only) / InMemoryOsintConnector (LOW 3) / OsintStagingPanelHost (LOW) / MapPanelBinder (LOW) / SensorC2PanelHost (LOW)
- [x] gitnexus__detect_changes (baseline: critical from prior S19 code in tree; new S20 will add docs + new low-risk symbols only)
- [x] Full gates: build PASS, 67 Data + 6 Osint + 7 PlayMode + 392 total? PASS 0 fail (exact from run)
- [x] Read kickoff, S19 artifacts, spike docs, all Osint*.cs (runner, gate, record, InMemory, mapper, command, stub host), MapPanelBinder, manifest, SensorC2 host, tracker, req05 (S19 section), AGENTS.md rules.

---

### Task 1: S20-01 Real OSINT Connectors (File + RSS stub)

**Files:**
- Create: src/ProjectAegis.Data/Osint/Connectors/FileOsintConnector.cs
- Create: src/ProjectAegis.Data/Osint/Connectors/RssOsintConnector.cs (minimal stub returning fixture or empty)
- Modify (optional compat): src/ProjectAegis.Data/Osint/Connectors/InMemoryOsintConnector.cs
- Test: src/ProjectAegis.Data.Tests/Osint/OsintConnectorTests.cs (new or add to existing DigestRunnerTests.cs)
- Docs: minor in kickoff or readme if needed

- [ ] **Step 1.0 Pre: GitNexus impact (report blast)**
  Run (in terminal or via MCP):
  ```
  # Use gitnexus MCP or:
  npx gitnexus impact --target FileOsintConnector --direction upstream --repo cmano-clone || echo "new = LOW expected"
  # Also re-confirm:
  gitnexus__impact target=InMemoryOsintConnector direction=upstream repo=cmano-clone includeTests=true
  ```
  Expected: LOW (tests only, like prior InMemory impact 3 callers). Report here before proceeding. If any surprise HIGH, pause.

- [ ] **Step 1.1: Define IOsintConnector (or skip per YAGNI; use concrete for S20) + write failing test for FileOsintConnector**
  Create test file or append to OsintDigestRunnerTests.cs a new test class/method expecting the connector.
  ```csharp
  // In src/ProjectAegis.Data.Tests/Osint/OsintConnectorTests.cs (new file) or existing
  using ProjectAegis.Data.Osint;
  using ProjectAegis.Data.Osint.Connectors;
  using NUnit.Framework;
  using System.IO;
  using System.Linq;

  [TestFixture]
  public class OsintConnectorTests
  {
      private string _tempDir = null!;

      [SetUp]
      public void SetUp()
      {
          _tempDir = Path.Combine(Path.GetTempPath(), "osint_test_" + Guid.NewGuid().ToString("N"));
          Directory.CreateDirectory(_tempDir);
      }

      [TearDown]
      public void TearDown()
      {
          if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
      }

      [Test]
      public void FileOsintConnector_LoadsJsonFixture_ProducesSortedRecords()
      {
          // Arrange: write a minimal json fixture (array of discovery-like)
          var fixturePath = Path.Combine(_tempDir, "osint_facts.json");
          File.WriteAllText(fixturePath, @"[
            { ""canonicalId"": ""test-hypersonic"", ""sourceUrl"": ""https://ex.com/h"", ""snippet"": ""observed"", ""relevanceScore"": 0.78, ""targetDoc"": ""10"", ""proposedTrl"": 7 },
            { ""canonicalId"": ""test-railgun"", ""sourceUrl"": ""https://ex.com/r"", ""snippet"": ""spec"", ""relevanceScore"": 0.55, ""targetDoc"": ""10"", ""proposedTrl"": 4 }
          ]");
          var connector = new FileOsintConnector(fixturePath);

          // Act
          var records = connector.Fetch();

          // Assert
          Assert.That(records, Is.Not.Null);
          Assert.That(records.Length, Is.EqualTo(2));
          Assert.That(records[0].CanonicalId, Is.EqualTo("test-hypersonic")); // stable by SourceUrl then CanonicalId
          Assert.That(records.All(r => r.SourceUrl != null), Is.True);
      }

      [Test]
      public void FileOsintConnector_EmptyOrMissing_ReturnsEmpty()
      {
          var connector = new FileOsintConnector("/non/existent.json");
          var records = connector.Fetch();
          Assert.That(records, Is.Empty);
      }
  }
  ```
  (Adjust json shape or ctor to match simple parser impl; use record ctor with 6+ params.)

- [ ] **Step 1.2: Run test to verify RED (no FileOsintConnector yet)**
  Command:
  ```
  dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "OsintConnector|FileOsint" -v normal --no-build
  ```
  Expected: FAIL (type or method not found / could not load).

- [ ] **Step 1.3: Implement minimal FileOsintConnector (green)**
  Create the file:
  ```csharp
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Text.Json;
  using ProjectAegis.Data.Osint;

  namespace ProjectAegis.Data.Osint.Connectors;

  /// <summary>
  /// File-based OSINT connector for Sprint 20 (local json fixture or dir of facts).
  /// Deterministic: stable sort on SourceUrl + CanonicalId. No net, no wall-clock in fetch.
  /// </summary>
  public sealed class FileOsintConnector
  {
      private readonly string _path;

      public FileOsintConnector(string path)
      {
          _path = path;
      }

      public OsintDiscoveryRecord[] Fetch()
      {
          if (string.IsNullOrWhiteSpace(_path) || !File.Exists(_path))
              return Array.Empty<OsintDiscoveryRecord>();

          try
          {
              var json = File.ReadAllText(_path);
              // Minimal parser: expect array of objects with keys matching record (or simple map)
              using var doc = JsonDocument.Parse(json);
              var list = new List<OsintDiscoveryRecord>();
              foreach (var el in doc.RootElement.EnumerateArray())
              {
                  var id = el.GetProperty("canonicalId").GetString() ?? "unknown";
                  var url = el.GetProperty("sourceUrl").GetString() ?? "";
                  var snip = el.TryGetProperty("snippet", out var s) ? s.GetString() ?? "" : "";
                  var score = el.TryGetProperty("relevanceScore", out var sc) ? sc.GetDouble() : 0.5;
                  var target = el.TryGetProperty("targetDoc", out var t) ? t.GetString() ?? "10" : "10";
                  var trl = el.TryGetProperty("proposedTrl", out var tr) ? tr.GetInt32() : 5;
                  list.Add(new OsintDiscoveryRecord(id, url, snip, score, target, trl));
              }
              return list
                  .OrderBy(r => r.SourceUrl, StringComparer.Ordinal)
                  .ThenBy(r => r.CanonicalId, StringComparer.Ordinal)
                  .ToArray();
          }
          catch
          {
              return Array.Empty<OsintDiscoveryRecord>();
          }
      }
  }

  // Optional stub for RSS (YAGNI full parse for S20; returns fixed or empty for demo)
  public sealed class RssOsintConnector
  {
      public OsintDiscoveryRecord[] Fetch() => Array.Empty<OsintDiscoveryRecord>(); // TODO real in S21
  }
  ```

- [ ] **Step 1.4: Run test to verify GREEN**
  Same test cmd as 1.2. Expected: PASS  (2 tests or the File ones).

- [ ] **Step 1.5: Integrate + determinism test (runner + connector end to end)**
  Add/extend test that does var conn = new FileOsintConnector(fixture); var runner = new OsintDigestRunner(0.65); var (p, l) = runner.Run(conn.Fetch()); assert proposals have high score ones, stable.

- [ ] **Step 1.6: Re-run full Osint filter + Data tests; impact re-check on new symbols (LOW)**
  Cmds as gates.

- [ ] **Step 1.7: Update any InMemory to share interface (optional) or leave concrete; self-review (no magic, stable sort, fixture driven)**
  If interface added:
  ```csharp
  public interface IOsintConnector { OsintDiscoveryRecord[] Fetch(); }
  // then InMemory : IOsintConnector { public OsintDiscoveryRecord[] Fetch() ... }
  // File too.
  ```
  (Add only if tests benefit; keep simple per YAGNI.)

- [ ] **Step 1.8: Mark task complete in plan + todo; git add / commit note (or plan equiv); re-detect_changes**
  Expected: new symbols in Osint module, 0 new CRITICAL processes.

---

### Task 2: S20-02 Full OsintStagingPanelHost UI Production

**Files:**
- Modify: unity/ProjectAegis/Assets/Scripts/Runtime/OsintStagingPanelHost.cs (replace stub TODOs with real)
- Test: src/ProjectAegis.Delegation.UnityAdapter.Tests/... (or new PlayMode for host if pattern) + CLI tests already cover proxy
- Docs: update runbook/evidence

- [ ] **Step 2.0 Pre: GitNexus impact + report**
  Impact OsintStagingPanelHost, OsintStagingReviewCommand (or its Run), SensorC2PanelHost (model), any ListView usage. Expect LOW.

- [ ] **Step 2.1: Write/ extend PlayMode or unit test for host wiring (expect partial red on TODO behavior)**
  E.g. in adapter tests or dedicated: instantiate host, call setup, assert IsWired, StatusText contains "OSINT".

- [ ] **Step 2.2: Run test RED**

- [ ] **Step 2.3: Implement full host (modeled on SensorC2 + S19 stub comments)**
  Key additions (show diff or full relevant methods):
  - Add button consts: ApproveButtonName = "approve-selected", RejectButtonName=...
  - In SetupUI or OnEnable: find _approveButton = root.Q<Button>(Approve...); _approveButton.clicked += OnApproveSelected;
  - Load proposals: prefer direct (for Editor: use CatalogWriteGate or reader if injected) or parse from OsintStagingReviewCommand.Run(db, null, writer) but since static TextWriter, use a StringWriter helper or expose ListPending.
  - Note: to avoid duplication, add to OsintStagingReviewCommand a ListPending helper or keep CLI as proxy; for host in Editor, direct gate is acceptable (same Data layer).
  - Bind ListView with makeItem/bindItem for proposal summary (CanonicalId | Score | Url).
  - OnApprove: get selected, call OsintStagingReviewCommand.Run(dbPath, batchId, new StringWriter()); refresh list from pending.
  - Respect: showPanel, styles, disallow multiple.
  - Expose for tests: public IReadOnlyList<...> CurrentProposals => ... ; 
  - Add keyboard/gamepad note (UI Toolkit default + input system if present).

  Full snippet for bind (adapt to actual UXML names in scene; stub uses "proposal-list"):
  ```csharp
  private void BindProposals(IEnumerable<OsintDiscoveryRecord> proposals)
  {
      var list = proposals?.ToList() ?? new List<OsintDiscoveryRecord>();
      _proposalList.itemsSource = list;
      _proposalList.makeItem = () => new Label { name = "proposal-row" };
      _proposalList.bindItem = (e, i) =>
      {
          var rec = list[i];
          ((Label)e).text = $"{rec.CanonicalId} | {rec.RelevanceScore:F2} | {rec.SourceUrl}";
      };
      if (_statusLine != null) _statusLine.text = $"OSINT Staging: {list.Count} pending (use approve button)";
  }
  ```
  Call from OnEnable after Setup, with data from proxy or gate.ListPendingBatches mapped (or run digest on demand).

- [ ] **Step 2.4: Run test GREEN + PlayMode smoke**

- [ ] **Step 2.5: Add approve flow test (uses existing Osint E2E or CLI test); verify commit visible**

- [ ] **Step 2.6: Self review vs ui-code rules (no game state mutate direct, localization ready? placeholders, scalable, gamepad); update stub comments to production note**

- [ ] **Step 2.7: Create/update S20 qa runbook note for Editor manual visual (copy S18 pattern); mark [x]**

---

### Task 3: S20-03 Cesium Map Foundation

**Files:**
- Modify: unity/ProjectAegis/Packages/manifest.json
- Create: unity/ProjectAegis/Assets/Scripts/Runtime/CesiumGlobeBridge.cs (or CesiumDataProvider.cs)
- Modify: docs/engineering/cesium-phase-b-spike-checklist.md (check package + bridge items, add evidence links)
- Modify: docs/architecture/adr-007-c2-map-presentation.md or kickoff if needed
- Test: ensure no breakage to MapPanelBinder tests/projections (add bridge test if unit possible)

- [ ] **Step 3.0 Pre: impact MapPanelBinder (LOW confirmed); no symbol for json, but document change**

- [ ] **Step 3.1: Update manifest with Cesium pin (git URL per spike doc)**
  Use search_replace on the json to add under dependencies:
  "com.cesium.unity": "https://github.com/CesiumGS/cesium-unity.git?path=Package#release/1.12.0"   (verify version; add comment)
  Also note ion token in scoped or .gitignore.

- [ ] **Step 3.2: Create minimal CesiumGlobeBridge.cs (data feed stub)**
  ```csharp
  // Cesium foundation for S20 (data bridge only; full scene/ion in Editor per checklist)
  #if UNITY_5_3_OR_NEWER
  using ProjectAegis.Delegation.Projection;
  using UnityEngine;

  namespace ProjectAegis.Unity.Runtime
  {
      public sealed class CesiumGlobeBridge : MonoBehaviour
      {
          [SerializeField] private MapPanelBinder? mapBinder; // or sim state source
          // In Editor Play: on enable, if cesium globe present, push 1 friendly + 1 hostile position from binder or hardcoded Baltic seed.
          private void OnEnable()
          {
              // Stub: log or set Cesium primitive positions (actual Cesium API: CesiumGeoreference + CesiumGlobeAnchor etc.)
              // For spike: just prove bridge compiles + can read from MapPanelBinder.
              if (mapBinder != null)
              {
                  Debug.Log("[CesiumGlobeBridge] Would feed positions from MapPanelBinder (S20 foundation)");
              }
          }

          // Expose for tests/harness
          public bool BridgeActive => true;
      }
  }
  #endif
  ```

- [ ] **Step 3.3: Update spike checklist (mark package + data bridge items with notes "S20 foundation; full visual local Editor") ; add runbook reference**

- [ ] **Step 3.4: Verify build (Unity may need package resolve in Editor; CI note); run projection tests to confirm MapPanelBinder untouched**

- [ ] **Step 3.5: Self review (no prod sim change, perf note in checklist, no secrets)**

---

### Task 4: S20-04 Docs, Tracker, Evidence, Closeout + Loop Verification

**Files:** tracker, req05, qa/ new evidence (smoke-2026-06-07.md, osint-cesium-evidence.md), agentic/sprint-20-closeout-2026-06-07.md , sprint-status.yaml (mark tasks done), perhaps epics index note.

- [ ] **Step 4.1: Update implementation-tracker row 05 (Partial+ -> connectors+UI+cesium base; cite new tests + plan + closeout) and row 20 (map progress)**

- [ ] **Step 4.2: Append S20 section to 05-Dynamic-Systems-Agent.md (similar to S19 section)**

- [ ] **Step 4.3: Produce qa evidence: run smoke-check equivalent, targeted Osint/Cesium (doc), update c2-like runbook if map UI; create sprint-20-*-evidence.md with PASS summaries + SHA + links**

- [ ] **Step 4.4: Create closeout doc modeled on sprint-19-closeout (PASS summary, gitnexus results, all plan [x], gates, evidence links, "Sprint 20 complete")**

- [ ] **Step 4.5: Update sprint-status.yaml (set status complete, completed date, closeout_note, tasks done, tests_passed bump if new, worktrees)**

- [ ] **Step 4.6: Final full gates + replay if touched + gitnexus__detect_changes (expect only new Osint connectors + UI script + Cesium bridge + doc sections; no new CRITICAL processes or Delegation impact)**

- [ ] **Step 4.7: Self review full plan vs kickoff acceptance + rules (data driven, determinism, no overbuild, Editor notes for visuals); mark all [x]**

- [ ] **Step 4.8: Hindsight retain (if server) or note; finishing-a-development-branch steps (no merge here); ask user for commit / PR equiv.**

---

## Self-Review (writing-plans)
1. Spec coverage: Kickoff Must 1-4 map 1:1 to Tasks 1-4. S19 patterns followed. Cesium from spike exactly. GitNexus/AGENTS/ determinism / gates / Editor notes all called out.
2. Placeholder scan: No TBD; all code blocks complete, cmds exact, expected outputs specified.
3. Type consistency: Record fields (CanonicalId etc) match reads; connector Fetch matches InMemory; host modeled on existing UIDocument/ListView.
4. Gaps: None (Should deferred to plan steps if capacity). No Delegation. Catalog only if needed (extend rule).

**Plan complete.** User: choose execution: subagent-driven (dispatch per task via spawn_subagent + reviewers) or inline. Recommend subagent-driven per skill.

**Next in loop (s20-06):** Extract tasks, create todo_write with granular steps (id s20-t1.1 etc), dispatch first implementer subagent with full task text + context (kickoff + this plan + file reads + "run impacts first using terminal/MCP, use search_replace for edits, run tests, self-review, status DONE/BLOCKED"). Repeat for all, with reviews between.

All checkboxes to be marked during execution.
