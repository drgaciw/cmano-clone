using System.Text.Json;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-319 local attention Score for synthetic <c>aegis.jev.state.v0</c> projections.
/// Ignores <c>expected*</c> oracle fields. Does not enqueue orders or recommend weapons.
/// </summary>
public static class JevAttentionBinder
{
    /// <summary>Fixture schema this binder accepts.</summary>
    public const string StateSchema = "aegis.jev.state.v0";

    /// <summary>Score one projection row. Pure; no I/O.</summary>
    public static string Score(JevAttentionCue cue)
    {
        var iff = cue.Iff ?? string.Empty;
        var hostile = iff.Equals("hostile", StringComparison.OrdinalIgnoreCase);
        var friendly = iff.Equals("friendly", StringComparison.OrdinalIgnoreCase);
        var unknown = iff.Equals("unknown", StringComparison.OrdinalIgnoreCase);
        var neutral = iff.Equals("neutral", StringComparison.OrdinalIgnoreCase);
        var urgency = cue.UrgencyHint ?? string.Empty;

        if (hostile && cue.Closing && cue.RangeNm <= 12d)
        {
            return JevAttentionLevel.Immediate;
        }

        if (hostile && (urgency.Equals("critical", StringComparison.OrdinalIgnoreCase)
            || urgency.Equals("imminent", StringComparison.OrdinalIgnoreCase)))
        {
            return JevAttentionLevel.Immediate;
        }

        if (unknown && cue.Closing && cue.RangeNm < 18d)
        {
            return JevAttentionLevel.Focus;
        }

        if (urgency.Equals("urgent", StringComparison.OrdinalIgnoreCase) && cue.Closing)
        {
            return JevAttentionLevel.Focus;
        }

        if (unknown && !cue.Closing && cue.RangeNm >= 15d && cue.RangeNm <= 28d)
        {
            return JevAttentionLevel.Watch;
        }

        if (urgency.Equals("elevated", StringComparison.OrdinalIgnoreCase))
        {
            return JevAttentionLevel.Watch;
        }

        if (friendly && !cue.Closing && cue.RangeNm >= 35d)
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
        var modelScore = !disclosure.Visible;
        var selected = ReadSelectedUnit(root);
        var rows = new List<JevAttentionRow>();
        if (root.TryGetProperty("roster", out var roster) && roster.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in roster.EnumerateArray())
            {
                rows.Add(ProjectEntry(entry, selected, modelScore));
            }
        }
        else
        {
            rows.Add(ProjectEntry(root, selected, modelScore));
        }

        return new JevAttentionPanel(
            OptionalString(root, "fixtureId"),
            modelScore,
            Sort(rows),
            disclosure);
    }

    private static JevAttentionRow ProjectEntry(JsonElement entry, string? selectedUnitId, bool modelScore)
    {
        var unitId = RequiredString(entry, "unitId");
        var contactId = OptionalString(entry, "contactId");
        if (!modelScore)
        {
            return BuildRow(unitId, contactId, null);
        }

        var contact = ObjectOrEmpty(entry, "contact");
        var flags = ObjectOrEmpty(entry, "flags");
        var roe = ObjectOrEmpty(entry, "roe");
        var iff = OptionalString(contact, "iff") ?? OptionalString(contact, "classHint");
        var cue = new JevAttentionCue(
            iff,
            OptionalDouble(contact, "rangeNm"),
            OptionalBool(contact, "closing"),
            OptionalString(flags, "urgencyHint"),
            OptionalBool(roe, "holdFire"),
            OptionalBool(flags, "roeConflict"),
            string.Equals(selectedUnitId, unitId, StringComparison.Ordinal));
        return BuildRow(unitId, contactId, Score(cue));
    }

    private static JevAttentionRow BuildRow(string unitId, string? contactId, string? level)
    {
        var shown = level ?? JevAttentionLevel.Unavailable;
        var highlight = JevAttentionLevel.Highlights(level);
        var css = JevAttentionLevel.CssClass(level);
        var contact = string.IsNullOrEmpty(contactId) ? string.Empty : $"  {contactId}";
        return new JevAttentionRow(
            unitId,
            contactId,
            level,
            JevAttentionLevel.Rank(level),
            highlight,
            css,
            $"ATTN {shown}  {unitId}{contact}");
    }

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

    private static string? ReadSelectedUnit(JsonElement root)
    {
        if (!root.TryGetProperty("playerInfoModel", out var player) || player.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return OptionalString(player, "selectedUnitId");
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

    private static double OptionalDouble(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return 0d;
        }

        return value.ValueKind == JsonValueKind.Number ? value.GetDouble() : 0d;
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
    double RangeNm,
    bool Closing,
    string? UrgencyHint,
    bool HoldFire,
    bool RoeConflict,
    bool PlayerAlreadyLooking);
