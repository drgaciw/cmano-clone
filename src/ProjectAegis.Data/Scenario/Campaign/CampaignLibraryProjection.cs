namespace ProjectAegis.Data.Scenario.Campaign;

using System.Text.Json;

/// <summary>
/// Projects a campaign document / path into a <see cref="CampaignLibraryEntry"/> with
/// membership feasibility (CMD-27.12). Never throws for a single bad file.
/// </summary>
public static class CampaignLibraryProjection
{
    /// <summary>Project an already-loaded document (tests / in-memory).</summary>
    public static CampaignLibraryEntry Project(
        CampaignDocument document,
        string sourcePath,
        IReadOnlySet<string>? availableScenarioIds = null)
    {
        if (document is null)
        {
            return ProjectUnavailable(
                CampaignIdFromPath(sourcePath),
                sourcePath,
                CampaignLibraryReasons.SchemaError);
        }

        var campaignId = string.IsNullOrWhiteSpace(document.CampaignId)
            ? CampaignIdFromPath(sourcePath)
            : document.CampaignId.Trim();
        var title = string.IsNullOrWhiteSpace(document.Title) ? campaignId : document.Title.Trim();
        var members = document.Members ?? Array.Empty<CampaignScenarioMember>();
        var memberCount = members.Count;
        var completedCount = 0;
        for (var i = 0; i < members.Count; i++)
        {
            if (members[i].Completed)
            {
                completedCount++;
            }
        }

        var nextScenarioId = CampaignProgress.NextScenarioId(document);
        var reason = EvaluateFeasibility(document, availableScenarioIds);

        return new CampaignLibraryEntry(
            CampaignId: campaignId,
            Title: title,
            MemberCount: memberCount,
            CompletedCount: completedCount,
            NextScenarioId: nextScenarioId,
            Available: reason is null,
            UnavailableReason: reason,
            SourcePath: sourcePath ?? string.Empty);
    }

    /// <summary>Load from disk and project. Catches IO / parse failures as unavailable rows.</summary>
    public static CampaignLibraryEntry ProjectFromPath(
        string path,
        IReadOnlySet<string>? availableScenarioIds = null)
    {
        var campaignId = CampaignIdFromPath(path);
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return ProjectUnavailable(campaignId, path ?? string.Empty, CampaignLibraryReasons.FileUnreadable);
            }

            CampaignDocument document;
            try
            {
                document = CampaignDocumentJsonLoader.LoadFromFile(path);
            }
            catch (JsonException)
            {
                return ProjectUnavailable(campaignId, path, CampaignLibraryReasons.SchemaError);
            }
            catch (InvalidDataException)
            {
                return ProjectUnavailable(campaignId, path, CampaignLibraryReasons.SchemaError);
            }
            catch (IOException)
            {
                return ProjectUnavailable(campaignId, path, CampaignLibraryReasons.FileUnreadable);
            }
            catch (UnauthorizedAccessException)
            {
                return ProjectUnavailable(campaignId, path, CampaignLibraryReasons.FileUnreadable);
            }

            return Project(document, path, availableScenarioIds);
        }
        catch
        {
            return ProjectUnavailable(campaignId, path ?? string.Empty, CampaignLibraryReasons.FileUnreadable);
        }
    }

    public static CampaignLibraryEntry ProjectUnavailable(
        string campaignId,
        string sourcePath,
        string reason)
    {
        var id = string.IsNullOrWhiteSpace(campaignId) ? Path.GetFileName(sourcePath) ?? "unknown" : campaignId;
        return new CampaignLibraryEntry(
            CampaignId: id,
            Title: id,
            MemberCount: 0,
            CompletedCount: 0,
            NextScenarioId: null,
            Available: false,
            UnavailableReason: reason,
            SourcePath: sourcePath ?? string.Empty);
    }

    /// <summary>
    /// Campaign id from path: strip <c>.campaign.json</c> (or first extension) only — never encodes sequence.
    /// </summary>
    public static string CampaignIdFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "unknown";
        }

        var name = Path.GetFileName(path);
        if (name.EndsWith(".campaign.json", StringComparison.OrdinalIgnoreCase))
        {
            return name[..^".campaign.json".Length];
        }

        // First extension strip (matches ScenarioLibraryProjection for *.campaign alone).
        return Path.GetFileNameWithoutExtension(path);
    }

    /// <summary>
    /// Pre-load feasibility: MEMBER_MISSING when any member scenario id is absent from the index.
    /// Returns null when available. Prefer unavailable (blocking) over partial available.
    /// </summary>
    public static string? EvaluateFeasibility(
        CampaignDocument document,
        IReadOnlySet<string>? availableScenarioIds)
    {
        if (document is null)
        {
            return CampaignLibraryReasons.SchemaError;
        }

        if (availableScenarioIds is null)
        {
            // Cannot verify membership without a scenarios index — do not invent MEMBER_MISSING.
            return null;
        }

        var members = document.Members ?? Array.Empty<CampaignScenarioMember>();
        foreach (var member in members)
        {
            if (string.IsNullOrWhiteSpace(member.ScenarioId))
            {
                return CampaignLibraryReasons.MemberMissing;
            }

            if (!ScenarioIdPresent(availableScenarioIds, member.ScenarioId))
            {
                return CampaignLibraryReasons.MemberMissing;
            }
        }

        return null;
    }

    /// <summary>
    /// Match campaign member scenario ids against library-derived ids
    /// (<c>baltic-patrol.scenario</c>) and bare basenames (<c>baltic-patrol</c>).
    /// </summary>
    public static bool ScenarioIdPresent(IReadOnlySet<string> availableScenarioIds, string scenarioId)
    {
        if (availableScenarioIds.Contains(scenarioId))
        {
            return true;
        }

        // Member may use bare stem while index has "*.scenario" form (or vice versa).
        if (!scenarioId.EndsWith(".scenario", StringComparison.OrdinalIgnoreCase))
        {
            if (availableScenarioIds.Contains(scenarioId + ".scenario"))
            {
                return true;
            }
        }
        else
        {
            var bare = scenarioId[..^".scenario".Length];
            if (availableScenarioIds.Contains(bare))
            {
                return true;
            }
        }

        return false;
    }
}
