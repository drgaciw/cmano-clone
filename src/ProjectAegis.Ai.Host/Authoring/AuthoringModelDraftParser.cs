using System.Text.Json;

namespace ProjectAegis.Ai.Host.Authoring;

/// <summary>Parses fixture <c>provider-script.json</c> into an <see cref="AuthoringModelDraft"/>.</summary>
public static class AuthoringModelDraftParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Deserializes a provider-script JSON document.</summary>
    public static AuthoringModelDraft Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var dto = JsonSerializer.Deserialize<ProviderScriptDto>(json, Options)
            ?? throw new InvalidOperationException("provider-script JSON deserialized to null.");

        var ops = new List<AuthoringProposedOperation>();
        if (dto.ProposedOperations is not null)
        {
            foreach (var op in dto.ProposedOperations)
            {
                IReadOnlyList<string> units = op.AssignedUnitIds is { Count: > 0 }
                    ? op.AssignedUnitIds
                    : Array.Empty<string>();
                ops.Add(new AuthoringProposedOperation(
                    Op: op.Op ?? string.Empty,
                    MissionId: op.MissionId ?? string.Empty,
                    AssignedUnitIds: units,
                    TargetIds: op.TargetIds,
                    Waypoints: op.Waypoints?.Select(w => new AuthoringWaypoint(w.Lat, w.Lon)).ToArray(),
                    Role: op.Role));
            }
        }

        return new AuthoringModelDraft(
            ScriptedStatus: dto.ScriptedStatus ?? "Ok",
            ProposedOperations: ops,
            ClarificationPrompt: dto.ClarificationPrompt,
            ForbiddenInOutput: dto.ForbiddenInOutput);
    }

    private sealed class ProviderScriptDto
    {
        public string? ScriptedStatus { get; set; }

        public string? ClarificationPrompt { get; set; }

        public List<string>? ForbiddenInOutput { get; set; }

        public List<OpDto>? ProposedOperations { get; set; }
    }

    private sealed class OpDto
    {
        public string? Op { get; set; }

        public string? MissionId { get; set; }

        public List<string>? AssignedUnitIds { get; set; }

        public List<string>? TargetIds { get; set; }

        public List<WpDto>? Waypoints { get; set; }

        public string? Role { get; set; }
    }

    private sealed class WpDto
    {
        public double Lat { get; set; }

        public double Lon { get; set; }
    }
}
