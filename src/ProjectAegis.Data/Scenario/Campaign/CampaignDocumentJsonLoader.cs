namespace ProjectAegis.Data.Scenario.Campaign;

using System.Text.Json;

/// <summary>Loads <see cref="CampaignDocument"/> from <c>*.campaign.json</c> artifacts.</summary>
public static class CampaignDocumentJsonLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static CampaignDocument LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return LoadFromJson(json)
            ?? throw new InvalidDataException($"Invalid campaign document: {path}");
    }

    public static CampaignDocument? LoadFromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<CampaignDocumentDto>(json, Options);
        if (dto is null)
        {
            return null;
        }

        return ToDocument(dto);
    }

    public static bool TryLoadFromFile(string path, out CampaignDocument? document)
    {
        document = null;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            document = LoadFromFile(path);
            return document is not null;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidDataException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static CampaignDocument ToDocument(CampaignDocumentDto dto)
    {
        var members = (dto.Members ?? new List<CampaignScenarioMemberDto>())
            .Select(m => new CampaignScenarioMember(
                ScenarioId: m.ScenarioId?.Trim() ?? string.Empty,
                Sequence: m.Sequence,
                Completed: m.Completed,
                DisplayTitle: string.IsNullOrWhiteSpace(m.DisplayTitle) ? null : m.DisplayTitle.Trim()))
            .OrderBy(m => m.Sequence)
            .ThenBy(m => m.ScenarioId, StringComparer.Ordinal)
            .ToArray();

        return new CampaignDocument(
            CampaignId: dto.CampaignId?.Trim() ?? string.Empty,
            Title: dto.Title?.Trim() ?? string.Empty,
            Synopsis: dto.Synopsis?.Trim() ?? string.Empty,
            Members: members);
    }

    private sealed class CampaignDocumentDto
    {
        public string? CampaignId { get; set; }
        public string? Title { get; set; }
        public string? Synopsis { get; set; }
        public List<CampaignScenarioMemberDto>? Members { get; set; }
    }

    private sealed class CampaignScenarioMemberDto
    {
        public string? ScenarioId { get; set; }
        public int Sequence { get; set; }
        public bool Completed { get; set; }
        public string? DisplayTitle { get; set; }
    }
}
