using System.Text.Json;

namespace ProjectAegis.Ai.Host.Aar;

/// <summary>Parses model JSON into <see cref="AarModelDraft"/>.</summary>
public static class AarModelDraftParser
{
    /// <summary>
    /// Tries to parse assistant text. Returns false on malformed or incomplete JSON.
    /// </summary>
    public static bool TryParse(string? assistantText, out AarModelDraft? draft)
    {
        draft = null;
        if (string.IsNullOrWhiteSpace(assistantText))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(assistantText);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!root.TryGetProperty("summary", out var summaryEl)
                || summaryEl.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var summary = summaryEl.GetString() ?? string.Empty;
            var outcomes = ReadStringArray(root, "outcomes");
            var uncertainties = ReadStringArray(root, "uncertainties");
            var citations = ReadStringArray(root, "citations");
            if (outcomes is null || uncertainties is null || citations is null)
            {
                return false;
            }

            draft = new AarModelDraft(summary, outcomes, uncertainties, citations);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
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
            if (item.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            list.Add(item.GetString() ?? string.Empty);
        }

        return list;
    }
}
