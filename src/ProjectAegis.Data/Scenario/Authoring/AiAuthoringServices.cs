namespace ProjectAegis.Data.Scenario.Authoring;

using ProjectAegis.Data.Validation;

/// <summary>
/// AI-assisted authoring services (track 5/5): NlScaffold, ConstraintPlacementAssistant, SmokeTestAgent, Explain.
/// "NL brief to draft scenario scaffold" + "provenance tags" on AI output.
/// ConstraintPlacementAssistant refuses invalid placements (no host/map combo allowed).
/// All deterministic for isolated real tests; evidence tied.
/// </summary>
public static class AiAuthoringServices
{
    public sealed record ScaffoldResult(
        ScenarioDocumentDto DraftDocument,
        string[] Sides,
        string[] Missions,
        string[] Objectives,
        ManifestBuilder.ProvenanceTag[] ProvenanceTags,
        string Explanation);

    /// <summary>
    /// NlScaffold (NL brief to draft scenario scaffold): produces sides/missions/objectives + provenance tags.
    /// Uses keyword heuristics (realistic stubs for Baltic-style).
    /// </summary>
    public static ScaffoldResult NlScaffold(string naturalLanguageBrief, string? baseDbRef = "baltic_patrol")
    {
        var brief = (naturalLanguageBrief ?? string.Empty).ToLowerInvariant();
        var sides = new List<string> { "Blue", "Red" };
        var missions = new List<string>();
        var objectives = new List<string>();
        var provTags = new List<ManifestBuilder.ProvenanceTag>
        {
            new ManifestBuilder.ProvenanceTag("nl-scaffold", "ai", "nl-scaffold-v1", brief.Length > 20 ? brief.Substring(0, 20) + "..." : brief)
        };

        // Draft base document
        var editor = ScenarioDocumentEditor.CreateNew(baseDbRef, 42, "ai-scaffolded");
        // Add provenance hint via metadata (non breaking: extra ignored in base dto)
        // We attach via draft missions only; provenance recorded separately.

        if (brief.Contains("patrol") || brief.Contains("defend") || brief.Contains("baltic"))
        {
            missions.Add("patrol-blue-1");
            objectives.Add("Patrol key chokepoint, detect hostiles");
            // Use public API to populate draft without editing editor internals
            // (will be saved by caller if needed)
            editor.AddPatrolMission("patrol-blue-1", new[] { "u1" }, new[]
            {
                new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 }
            });
        }

        if (brief.Contains("strike") || brief.Contains("attack") || brief.Contains("offense"))
        {
            missions.Add("strike-blue-1");
            objectives.Add("Strike high-value targets then egress");
            editor.AddStrikeMission("strike-blue-1", new[] { "u1" }, new[] { "hostile-1" });
        }

        if (brief.Contains("support") || brief.Contains("tanker"))
        {
            missions.Add("support-1");
            objectives.Add("Provide tanker support for blue air");
        }

        if (missions.Count == 0)
        {
            missions.Add("patrol-default");
            objectives.Add("Default patrol per brief keywords");
            editor.AddPatrolMission("patrol-default", new[] { "u1" }, new[]
            {
                new ScenarioWaypointDto { Lat = 57.0, Lon = 19.5 },
                new ScenarioWaypointDto { Lat = 57.05, Lon = 19.6 }
            });
        }

        if (brief.Contains("red") || brief.Contains("opfor"))
        {
            sides.Add("Opfor");
            missions.Add("red-patrol-1");
            objectives.Add("Red force patrol and contest area");
        }

        var draft = editor.ToDto();
        var explain = $"Scaffolded from NL brief using keyword parse. Draft contains {missions.Count} missions across {sides.Count} sides. Evidence: brief='{brief.Substring(0, Math.Min(60, brief.Length))}'";

        return new ScaffoldResult(draft, sides.ToArray(), missions.ToArray(), objectives.ToArray(), provTags.ToArray(), explain);
    }

    /// <summary>
    /// ConstraintPlacementAssistant: refuses invalid unit-host or map placement. Returns (allowed, reason).
    /// For "ConstraintPlacementAssistant (refuses invalid)".
    /// </summary>
    public static (bool Allowed, string Reason, ManifestBuilder.ProvenanceTag? Tag) CheckPlacement(
        string unitId, string? hostId, double lat, double lon, string mapBounds = "baltic")
    {
        // Simple deterministic rules (refuse known invalids)
        bool validHost = string.IsNullOrEmpty(hostId) || hostId.StartsWith("base-") || hostId == "carrier-1" || hostId == "airbase-blue";
        bool validMap = lat >= 54 && lat <= 60 && lon >= 18 && lon <= 25; // rough baltic

        if (!validHost)
        {
            var tag = new ManifestBuilder.ProvenanceTag("constraint-refused", "ai", "constraint-placement-assistant", $"invalid host {hostId} for {unitId}");
            return (false, $"Invalid host '{hostId}' for unit {unitId} (ConstraintPlacementAssistant refusal)", tag);
        }
        if (!validMap)
        {
            var tag = new ManifestBuilder.ProvenanceTag("constraint-refused", "ai", "constraint-placement-assistant", $"out of map {lat},{lon}");
            return (false, $"Placement {lat},{lon} outside {mapBounds} bounds (ConstraintPlacementAssistant refusal)", tag);
        }

        var okTag = new ManifestBuilder.ProvenanceTag("placement-accepted", "ai", "constraint-placement-assistant", $"valid for {unitId}");
        return (true, "Placement accepted by constraints", okTag);
    }

    public sealed record SmokeTestReport(
        bool Passed,
        IReadOnlyList<string> Issues,
        string EvidenceSummary,
        ManifestBuilder.ProvenanceTag Tag);

    /// <summary>
    /// SmokeTestAgent: automated smoke checks for trivial wins, impossible objectives, orphaned units.
    /// </summary>
    public static SmokeTestReport RunSmokeTestAgent(ScenarioDocumentDto document, string? policyContext = null)
    {
        var issues = new List<string>();
        var missions = document.Missions;
        var meta = document.Metadata;

        if (missions.Count == 0)
        {
            issues.Add("No missions defined - trivial empty scenario");
        }

        foreach (var m in missions)
        {
            if (m.Type.Equals("Patrol", StringComparison.OrdinalIgnoreCase) && m.PatrolZone.Count < 2)
                issues.Add($"Mission {m.Id} has degenerate patrol zone");
            if (m.Type.Equals("Strike", StringComparison.OrdinalIgnoreCase) && m.TargetIds.Count == 0)
                issues.Add($"Strike {m.Id} has no targets - impossible objective");
            if (m.AssignedUnitIds.Count == 0)
                issues.Add($"Mission {m.Id} has orphaned (no units)");
        }

        bool passed = issues.Count == 0;
        var summary = $"SmokeTestAgent ran on {missions.Count} missions; DBRef={meta.DbRef ?? "default"}. PolicyContext={policyContext ?? "none"}";
        var tag = new ManifestBuilder.ProvenanceTag("smoke-test", "ai", "smoketest-agent-v1", string.Join(";", issues.Take(3)));

        return new SmokeTestReport(passed, issues, summary, tag);
    }

    public sealed record ExplainResult(string Explanation, string[] EvidenceLines, ManifestBuilder.ProvenanceTag Tag);

    /// <summary>
    /// Explain with evidence: ties suggestion/state to scenario evidence and rule checks.
    /// </summary>
    public static ExplainResult ExplainWithEvidence(string question, ScenarioDocumentDto document, ValidationReport? report = null)
    {
        var evidence = new List<string> { $"Document has {document.Missions.Count} missions, EditVersion={document.Metadata.EditVersion}" };
        if (report != null)
        {
            evidence.Add($"Validation passed={report.Passed}, findings={report.Findings.Count}");
            foreach (var f in report.Findings.Take(3))
                evidence.Add($"Evidence: {f.Code} {f.Severity} {f.Message}");
        }
        if (question.Contains("why", StringComparison.OrdinalIgnoreCase) || question.Contains("patrol"))
        {
            evidence.Add("Patrol zones drive detection windows per sim rules");
        }

        var expl = $"Explanation for '{question}': based on current document state and validation. See evidence.";
        var tag = new ManifestBuilder.ProvenanceTag("explain-evidence", "ai", "explain-agent", question);
        return new ExplainResult(expl, evidence.ToArray(), tag);
    }
}
