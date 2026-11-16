using System.Text.Json;

namespace ProjectAegis.Ai.Host.Authoring;

/// <summary>Parses fixture <c>provider-script.json</c> into an <see cref="AuthoringModelDraft"/>.</summary>
public static class AuthoringModelDraftParser
{
    /// <summary>Deserializes a provider-script JSON document.</summary>
    public static AuthoringModelDraft Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("provider-script root must be an object.");
        }

        var status = GetString(root, "scriptedStatus") ?? "Ok";
        var clarification = GetString(root, "clarificationPrompt");
        var forbidden = ReadStringArray(root, "forbiddenInOutput");
        var ops = new List<AuthoringProposedOperation>();

        if (root.TryGetProperty("proposedOperations", out var opsEl)
            && opsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var opEl in opsEl.EnumerateArray())
            {
                if (opEl.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var waypoints = ReadWaypoints(opEl);
                ops.Add(new AuthoringProposedOperation(
                    Op: GetString(opEl, "op") ?? string.Empty,
                    MissionId: GetString(opEl, "missionId") ?? string.Empty,
                    AssignedUnitIds: ReadStringArray(opEl, "assignedUnitIds") ?? Array.Empty<string>(),
                    TargetIds: ReadStringArray(opEl, "targetIds"),
                    Waypoints: waypoints,
                    Role: GetString(opEl, "role")));
            }
        }

        return new AuthoringModelDraft(
            ScriptedStatus: status,
            ProposedOperations: ops,
            ClarificationPrompt: clarification,
            ForbiddenInOutput: forbidden);
    }

    private static IReadOnlyList<AuthoringWaypoint>? ReadWaypoints(JsonElement opEl)
    {
        if (!opEl.TryGetProperty("waypoints", out var el) || el.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var list = new List<AuthoringWaypoint>();
        foreach (var wp in el.EnumerateArray())
        {
            if (wp.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var lat = wp.TryGetProperty("lat", out var latEl) ? latEl.GetDouble() : 0;
            var lon = wp.TryGetProperty("lon", out var lonEl) ? lonEl.GetDouble() : 0;
            list.Add(new AuthoringWaypoint(lat, lon));
        }

        return list;
    }

    private static IReadOnlyList<string>? ReadStringArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var list = new List<string>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                list.Add(item.GetString() ?? string.Empty);
            }
        }

        return list;
    }

    private static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;
}
