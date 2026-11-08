namespace ProjectAegis.Data.Scenario.Campaign;

using System.Text;
using System.Text.Json;

/// <summary>
/// Deterministic campaign JSON writer (stable property order, LF, no BOM).
/// Property order: campaignId, title, synopsis, members[{scenarioId, sequence, completed, displayTitle?}].
/// </summary>
public static class CampaignDocumentJsonWriter
{
    public static string Serialize(CampaignDocument document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            WriteDocument(writer, document);
        }

        return Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    public static void WriteToFile(CampaignDocument document, string path)
    {
        var json = Serialize(document);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void WriteDocument(Utf8JsonWriter w, CampaignDocument doc)
    {
        w.WriteStartObject();
        w.WriteString("campaignId", doc.CampaignId ?? string.Empty);
        w.WriteString("title", doc.Title ?? string.Empty);
        w.WriteString("synopsis", doc.Synopsis ?? string.Empty);

        w.WritePropertyName("members");
        w.WriteStartArray();
        foreach (var member in (doc.Members ?? Array.Empty<CampaignScenarioMember>())
                     .OrderBy(m => m.Sequence)
                     .ThenBy(m => m.ScenarioId, StringComparer.Ordinal))
        {
            w.WriteStartObject();
            w.WriteString("scenarioId", member.ScenarioId ?? string.Empty);
            w.WriteNumber("sequence", member.Sequence);
            w.WriteBoolean("completed", member.Completed);
            if (!string.IsNullOrWhiteSpace(member.DisplayTitle))
            {
                w.WriteString("displayTitle", member.DisplayTitle);
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
        w.WriteEndObject();
    }
}
