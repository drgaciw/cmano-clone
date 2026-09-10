using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

/// <summary>Combines advisory text into bounded, accessible command-review sections.</summary>
public static class CommandReviewDashboard
{
    /// <summary>Projects status sections without consulting or modifying live simulation objects.</summary>
    public static IReadOnlyList<CommandReviewSection> Build(StatusFrame? status, bool problemsOnly = false)
    {
        if (status == null) return Array.Empty<CommandReviewSection>();
        var display = StatusPresenter.Build(status, problemsOnly ? StatusDisplayFilter.ProblemsOnly : StatusDisplayFilter.All);
        return Array.AsReadOnly(new[] {
            Section("sensors", "Sensor confidence and provenance", display.ContactRows.Select(r => r.Text)),
            Section("emissions", "Emissions and electronic warfare", display.EmissionRows.Concat(display.ElectronicWarfareRows).Select(r => r.Text)),
            Section("damage", "Platform damage and recovery", display.UnitRows.Select(r => r.Text)),
            Section("status-timeline", "Damage and comms timeline", display.TimelineRows.Reverse().Select(r => r.Text)),
        });
    }

    /// <summary>Combines independently grounded current advice, status and coordination facts.</summary>
    public static IReadOnlyList<CommandReviewSection> Build(StatusFrame? status, AdviceFrame? advice,
        CoordinationSnapshot coordination, bool problemsOnly = false)
    {
        var sections = Build(status, problemsOnly).ToList();
        if (advice != null)
        {
            var confidence = advice.Confidence.HasValue
                ? advice.Confidence.Value.ToString("P0", System.Globalization.CultureInfo.InvariantCulture) : "UNKNOWN";
            sections.Insert(0, Section("advice", "Threat and weapon advice — advisory only", new[] {
                $"Contact {advice.ContactId} | t={advice.SimTime:0.###} | {advice.Availability}",
                $"Assessment confidence: {confidence} | contact confidence: {advice.ContactConfidenceLabel}",
                advice.Rationale, advice.Fallback,
                "Recommendations do not authorize release. Use the existing engagement controls for an explicit fire decision."
            }.Concat(advice.HardConstraints.Select(x => "Constraint: " + x))
             .Concat(advice.PolicyConstraints.Select(x => "Policy: " + x))
             .Concat(advice.Assumptions.Select(x => "Assumption: " + x))
             .Concat(advice.Evidence.Select(x => "Evidence: " + x))));
            sections.Insert(1, Section("resources", "Resource ranking and alternatives",
                advice.Alternatives.Concat(advice.ResourceCommitments)));
            sections.Add(Section("mission-advice", "Current runtime and mission package evidence", advice.MissionPackageFacts));
        }
        sections.Add(Section("groups", "Task groups, responsibility and mission intent",
            coordination.Groups.SelectMany(group => new[] {
                group.Coordination.StatusLine, group.Intent.StatusLine,
                "Members: " + string.Join(", ", group.Coordination.Members),
                "Constraints: " + string.Join(", ", group.Intent.Constraints),
                "Retask advice: " + group.Intent.AdvisoryRetask
            }.Concat(group.Effects.Select(x => $"{x.UnitId} | role {x.Role?.ToString() ?? "UNKNOWN"} | {x.State} | coverage {x.Coverage.Status}: {x.Coverage.Label}"))
             .Concat(group.Gaps.Select(x => $"GAP {x.Code} | {x.UnitId} | {x.Detail}")))));
        return sections.AsReadOnly();
    }

    /// <summary>Caps visible rows while retaining an explicit truncation disclosure.</summary>
    public static CommandReviewSection Section(string id, string title, IEnumerable<string> lines)
    {
        var rows = lines.Take(201).ToList();
        if (rows.Count == 201) rows[200] = "Additional rows omitted from this summary; narrow the selection or inspect the timeline.";
        if (rows.Count == 0) rows.Add("No recorded facts for this view.");
        return new CommandReviewSection(id, title, rows.AsReadOnly());
    }
}
