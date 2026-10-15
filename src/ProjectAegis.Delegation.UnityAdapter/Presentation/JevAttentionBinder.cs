using System.Text.Json;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-319 attention Score for synthetic <c>aegis.jev.state.v0</c> projections.
/// Ignores <c>expected*</c> oracle fields. Does not enqueue orders or recommend weapons.
/// <see cref="JevAttentionPanel.ModelScoreAvailable"/> is true only when every row carries a
/// pinned <c>jevDecision</c>. Otherwise the level is the local heuristic.
/// </summary>
public static class JevAttentionBinder
{
    /// <summary>Fixture schema this binder accepts.</summary>
    public const string StateSchema = "aegis.jev.state.v0";

    /// <summary>
    /// Score one projection row from local rules. Pure; no I/O.
    /// A missing or non-numeric range does not match a proximity band.
    /// A unit the player is already watching steps down one band.
    /// </summary>
    public static string Score(JevAttentionCue cue)
    {
        var level = ScoreThreat(cue);
        return cue.PlayerAlreadyLooking ? DemoteAttended(level) : level;
    }

    /// <summary>Sort high score first. Equal ranks keep unit-id order.</summary>
    public static IReadOnlyList<JevAttentionRow> Sort(IReadOnlyList<JevAttentionRow> rows)
    {
        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        var copy = new JevAttentionRow[rows.Count];
        for (var i = 0; i < rows.Count; i++)
        {
            copy[i] = rows[i];
        }

        Array.Sort(copy, CompareRows);
        return copy;
    }

    /// <summary>Project a synthetic state document. Does not read oracle <c>expected*</c> fields.</summary>
    public static JevAttentionPanel Project(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Jev state JSON is required.", nameof(json));
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Jev state must be a JSON object.");
        }

        var schema = OptionalString(root, "schema");
        if (!string.Equals(schema, StateSchema, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Jev attention binder accepts schema {StateSchema} only.");
        }

        RejectPrivatePayload(root);
        var disclosure = JevDisclosureBinder.Project(ReadJevDecide(root));
        var skillUp = !disclosure.Visible;
        var selected = ReadPlayerId(root, "selectedUnitId");
        var camera = ReadPlayerId(root, "cameraFocusId");
        var scored = new List<(JevAttentionRow Row, bool FromModel)>();
        if (root.TryGetProperty("roster", out var roster) && roster.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in roster.EnumerateArray())
            {
                scored.Add(ProjectEntry(entry, selected, camera, skillUp));
            }
        }
        else
        {
            scored.Add(ProjectEntry(root, selected, camera, skillUp));
        }

        var modelScore = skillUp && scored.Count > 0;
        var rows = new List<JevAttentionRow>(scored.Count);
        for (var i = 0; i < scored.Count; i++)
        {
            rows.Add(scored[i].Row);
            if (!scored[i].FromModel)
            {
                modelScore = false;
            }
        }

        return new JevAttentionPanel(
            OptionalString(root, "fixtureId"),
            modelScore,
            Sort(rows),
            disclosure);
    }

    private static string ScoreThreat(JevAttentionCue cue)
    {
        var iff = cue.Iff ?? string.Empty;
        var hostile = iff.Equals("hostile", StringComparison.OrdinalIgnoreCase);
        var friendly = iff.Equals("friendly", StringComparison.OrdinalIgnoreCase);
        var unknown = iff.Equals("unknown", StringComparison.OrdinalIgnoreCase);
        var neutral = iff.Equals("neutral", StringComparison.OrdinalIgnoreCase);
        var urgency = cue.UrgencyHint ?? string.Empty;

        if (hostile && cue.Closing && cue.RangeNm is <= 12d)
        {
            return JevAttentionLevel.Immediate;
        }

        if (hostile && (urgency.Equals("critical", StringComparison.OrdinalIgnoreCase)
            || urgency.Equals("imminent", StringComparison.OrdinalIgnoreCase)))
        {
            return JevAttentionLevel.Immediate;
        }

        if (unknown && cue.Closing && cue.RangeNm is < 18d)
        {
            return JevAttentionLevel.Focus;
        }

        if (urgency.Equals("urgent", StringComparison.OrdinalIgnoreCase) && cue.Closing)
        {
            return JevAttentionLevel.Focus;
        }

        if (unknown && !cue.Closing && cue.RangeNm is >= 15d and <= 28d)
        {
            return JevAttentionLevel.Watch;
        }

        if (urgency.Equals("elevated", StringComparison.OrdinalIgnoreCase))
        {
            return JevAttentionLevel.Watch;
        }

        if (friendly && !cue.Closing && cue.RangeNm is >= 35d)
        {
            return JevAttentionLevel.Ignore;
        }

        if (neutral || cue.HoldFire || cue.RoeConflict)
        {
            return JevAttentionLevel.Low;
        }

        if (friendly)
        {
            return JevAttentionLevel.Ignore;
        }

        return JevAttentionLevel.Low;
    }

    /// <summary>One band down. The player already has eyes on this unit.</summary>
    private static string DemoteAttended(string level) => level switch
    {
        JevAttentionLevel.Immediate => JevAttentionLevel.Focus,
        JevAttentionLevel.Focus => JevAttentionLevel.Watch,
        JevAttentionLevel.Watch => JevAttentionLevel.Low,
        JevAttentionLevel.Low => JevAttentionLevel.Ignore,
        _ => level,
    };

    private static (JevAttentionRow Row, bool FromModel) ProjectEntry(
        JsonElement entry,
        string? selectedUnitId,
        string? cameraFocusId,
        bool skillUp)
    {
        var unitId = RequiredString(entry, "unitId");
        var contactId = OptionalString(entry, "contactId");
        if (!skillUp)
        {
            return (BuildRow(unitId, contactId, null), false);
        }

        var modelLevel = ReadModelLevel(entry);
        if (modelLevel is not null)
        {
            return (BuildRow(unitId, contactId, modelLevel), true);
        }

        var contact = ObjectOrEmpty(entry, "contact");
        var flags = ObjectOrEmpty(entry, "flags");
        var roe = ObjectOrEmpty(entry, "roe");
        var iff = OptionalString(contact, "iff") ?? OptionalString(contact, "classHint");
        var looking = string.Equals(selectedUnitId, unitId, StringComparison.Ordinal)
            || string.Equals(cameraFocusId, unitId, StringComparison.Ordinal);
        var cue = new JevAttentionCue(
            iff,
            OptionalDouble(contact, "rangeNm"),
            OptionalBool(contact, "closing"),
            OptionalString(flags, "urgencyHint"),
            OptionalBool(roe, "holdFire"),
            OptionalBool(flags, "roeConflict"),
            looking);
        return (BuildRow(unitId, contactId, Score(cue)), false);
    }

    /// <summary>
    /// A real Jev result names the pinned model and a known <c>attention_priority</c> band.
    /// Skill availability alone is not a result.
    /// </summary>
    private static string? ReadModelLevel(JsonElement entry)
    {
        if (!entry.TryGetProperty("jevDecision", out var decision) || decision.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var model = OptionalString(decision, "model");
        if (!string.Equals(model, JevDisclosurePresentation.PinnedOpenRouterModel, StringComparison.Ordinal)
            && !string.Equals(model, JevDisclosurePresentation.PinnedModel, StringComparison.Ordinal))
        {
            return null;
        }

        return CanonicalLevel(OptionalString(decision, "attention_priority"));
    }

    private static string? CanonicalLevel(string? level)
    {
        if (level is null)
        {
            return null;
        }

        if (level.Equals(JevAttentionLevel.Immediate, StringComparison.OrdinalIgnoreCase))
        {
            return JevAttentionLevel.Immediate;
        }

        if (level.Equals(JevAttentionLevel.Focus, StringComparison.OrdinalIgnoreCase))
        {
            return JevAttentionLevel.Focus;
        }

        if (level.Equals(JevAttentionLevel.Watch, StringComparison.OrdinalIgnoreCase))
        {
            return JevAttentionLevel.Watch;
        }

        if (level.Equals(JevAttentionLevel.Low, StringComparison.OrdinalIgnoreCase))
        {
            return JevAttentionLevel.Low;
        }

        if (level.Equals(JevAttentionLevel.Ignore, StringComparison.OrdinalIgnoreCase))
        {
            return JevAttentionLevel.Ignore;
        }

        return null;
    }

    private static JevAttentionRow BuildRow(string unitId, string? contactId, string? level) =>
        new(
            unitId,
            contactId,
            level,
            JevAttentionLevel.Rank(level),
            JevAttentionLevel.Highlights(level),
            JevAttentionLevel.CssClass(level));

    private static int CompareRows(JevAttentionRow left, JevAttentionRow right)
    {
        var byRank = right.Rank.CompareTo(left.Rank);
        if (byRank != 0)
        {
            return byRank;
        }

        return string.Compare(left.UnitId, right.UnitId, StringComparison.Ordinal);
    }

    private static bool? ReadJevDecide(JsonElement root)
    {
        if (!root.TryGetProperty("skillAvailability", out var skill) || skill.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!skill.TryGetProperty("jevDecide", out var flag))
        {
            return null;
        }

        return flag.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private static string? ReadPlayerId(JsonElement root, string name)
    {
        if (!root.TryGetProperty("playerInfoModel", out var player) || player.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return OptionalString(player, name);
    }

    private static void RejectPrivatePayload(JsonElement root)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (property.Name.Equals("decisionLog", StringComparison.OrdinalIgnoreCase)
                || property.Name.Equals("scenarioXml", StringComparison.OrdinalIgnoreCase)
                || property.Name.Equals("secret", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Jev VS1 accepts projection fixtures only. Private scenario or log payloads are refused.");
            }
        }
    }

    private static JsonElement ObjectOrEmpty(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var child) && child.ValueKind == JsonValueKind.Object)
        {
            return child;
        }

        return default;
    }

    private static string RequiredString(JsonElement element, string name)
    {
        var value = OptionalString(element, name);
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Jev state is missing {name}.");
        }

        return value;
    }

    private static string? OptionalString(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    /// <summary>Null when the field is missing or not a number. Zero stays zero.</summary>
    private static double? OptionalDouble(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;
    }

    private static bool OptionalBool(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return false;
        }

        return value.ValueKind == JsonValueKind.True;
    }
}

/// <summary>Projection cues used to score attention. Oracle <c>expected*</c> fields are not inputs.</summary>
public sealed record JevAttentionCue(
    string? Iff,
    double? RangeNm,
    bool Closing,
    string? UrgencyHint,
    bool HoldFire,
    bool RoeConflict,
    bool PlayerAlreadyLooking);
