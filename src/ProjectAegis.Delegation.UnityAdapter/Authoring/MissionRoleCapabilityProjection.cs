namespace ProjectAegis.Delegation.UnityAdapter.Authoring;

using Data.Scenario.Authoring;

/// <summary>Display row for one authored mission's runtime support (DRG-345).</summary>
public sealed class MissionRoleCapabilityRow
{
    /// <summary>Creates a row. Prefer <see cref="MissionRoleCapabilityProjection.Project"/>.</summary>
    public MissionRoleCapabilityRow(
        string missionId,
        string roleLabel,
        string backendId,
        MissionRoleExecution execution,
        string statusLabel,
        string? findingCode,
        string message)
    {
        MissionId = missionId;
        RoleLabel = roleLabel;
        BackendId = backendId;
        Execution = execution;
        StatusLabel = statusLabel;
        FindingCode = findingCode;
        Message = message;
    }

    /// <summary>Authored mission id.</summary>
    public string MissionId { get; }

    /// <summary><c>Type</c> or <c>Type / SupportRole</c>.</summary>
    public string RoleLabel { get; }

    /// <summary>Backend the row was assessed against.</summary>
    public string BackendId { get; }

    /// <summary>Manifest verdict.</summary>
    public MissionRoleExecution Execution { get; }

    /// <summary>Short status text for a badge or tooltip.</summary>
    public string StatusLabel { get; }

    /// <summary>Stable finding code when the role is not executed; otherwise <c>null</c>.</summary>
    public string? FindingCode { get; }

    /// <summary>Actionable disclosure (identical to the validation finding message).</summary>
    public string Message { get; }

    /// <summary>True when this mission keeps Export and Play closed.</summary>
    public bool BlocksExportAndPlay => Execution != MissionRoleExecution.Executed;
}

/// <summary>Display row for one built-in mission template's runtime support (DRG-345).</summary>
public sealed class MissionTemplateCapabilityRow
{
    /// <summary>Creates a row. Prefer <see cref="MissionRoleCapabilityProjection.ProjectTemplates"/>.</summary>
    public MissionTemplateCapabilityRow(string templateId, string displayName, MissionRoleExecution execution, string statusLabel)
    {
        TemplateId = templateId;
        DisplayName = displayName;
        Execution = execution;
        StatusLabel = statusLabel;
    }

    /// <summary>Template id from <see cref="MissionTemplateCatalog"/>.</summary>
    public string TemplateId { get; }

    /// <summary>Template display name.</summary>
    public string DisplayName { get; }

    /// <summary>Manifest verdict for the mission the template produces.</summary>
    public MissionRoleExecution Execution { get; }

    /// <summary>Short status text for the template picker.</summary>
    public string StatusLabel { get; }
}

/// <summary>
/// Presentation projection of <see cref="MissionRoleCapabilityManifest"/> (DRG-345 / proposed AME-6.11).
/// Pure and deterministic: rows follow manifest order; labels are formatting only and never change a verdict.
/// </summary>
public static class MissionRoleCapabilityProjection
{
    /// <summary>One row per authored mission, ordered by mission id (ordinal).</summary>
    public static IReadOnlyList<MissionRoleCapabilityRow> Project(
        ScenarioDocumentDto document,
        string backendId = MissionRoleCapabilityManifest.DefaultBackendId) =>
        MissionRoleCapabilityManifest.AssessDocument(document, backendId)
            .Select(a => new MissionRoleCapabilityRow(
                a.MissionId,
                a.SupportRole == null ? a.MissionType : a.MissionType + " / " + a.SupportRole,
                a.BackendId,
                a.Execution,
                StatusLabel(a.Execution, a.BackendId),
                a.FindingCode,
                a.Message))
            .ToArray();

    /// <summary>One row per built-in template, in <see cref="MissionTemplateCatalog.All"/> order.</summary>
    public static IReadOnlyList<MissionTemplateCapabilityRow> ProjectTemplates(
        string backendId = MissionRoleCapabilityManifest.DefaultBackendId) =>
        MissionTemplateCatalog.All
            .Select(t =>
            {
                var execution = MissionRoleCapabilityManifest
                    .Assess(MissionTemplateCatalog.Materialize(t.TemplateId, t.TemplateId), backendId)
                    .Execution;
                return new MissionTemplateCapabilityRow(t.TemplateId, t.DisplayName, execution, StatusLabel(execution, backendId));
            })
            .ToArray();

    /// <summary>Badge text for a verdict.</summary>
    public static string StatusLabel(MissionRoleExecution execution, string backendId) => execution switch
    {
        MissionRoleExecution.Executed => $"Executed by {backendId}",
        MissionRoleExecution.NotExecuted => $"Not executed by {backendId} — Export and Play blocked",
        _ => $"Unknown role for {backendId} — Export and Play blocked",
    };
}
