namespace ProjectAegis.Data.Scenario.Campaign;

/// <summary>
/// Pure completion API for campaign membership (CMD-27.12). No I/O; returns new documents.
/// </summary>
public static class CampaignProgress
{
    /// <summary>
    /// Mark the member with <paramref name="scenarioId"/> as completed.
    /// Matching is ordinal-ignore-case; unknown ids leave the document unchanged.
    /// </summary>
    public static CampaignDocument MarkComplete(CampaignDocument doc, string scenarioId)
    {
        if (doc is null)
        {
            throw new ArgumentNullException(nameof(doc));
        }

        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            return doc;
        }

        var target = scenarioId.Trim();
        var members = doc.Members ?? Array.Empty<CampaignScenarioMember>();
        var changed = false;
        var next = new CampaignScenarioMember[members.Count];
        for (var i = 0; i < members.Count; i++)
        {
            var m = members[i];
            if (string.Equals(m.ScenarioId, target, StringComparison.OrdinalIgnoreCase) && !m.Completed)
            {
                next[i] = m with { Completed = true };
                changed = true;
            }
            else
            {
                next[i] = m;
            }
        }

        return changed ? doc with { Members = next } : doc;
    }

    /// <summary>True when every member is completed (empty campaigns are not complete).</summary>
    public static bool IsCampaignComplete(CampaignDocument doc)
    {
        if (doc?.Members is null || doc.Members.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < doc.Members.Count; i++)
        {
            if (!doc.Members[i].Completed)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// First incomplete member by sequence (then scenario id), or null when all complete / empty.
    /// </summary>
    public static string? NextScenarioId(CampaignDocument doc)
    {
        if (doc?.Members is null || doc.Members.Count == 0)
        {
            return null;
        }

        CampaignScenarioMember? next = null;
        foreach (var m in doc.Members)
        {
            if (m.Completed)
            {
                continue;
            }

            if (next is null ||
                m.Sequence < next.Sequence ||
                (m.Sequence == next.Sequence &&
                 string.Compare(m.ScenarioId, next.ScenarioId, StringComparison.Ordinal) < 0))
            {
                next = m;
            }
        }

        return next?.ScenarioId;
    }
}
