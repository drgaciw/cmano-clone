namespace ProjectAegis.Delegation.Hindsight;

/// <summary>Canonical Hindsight memory bank identifiers for Project Aegis agents and runs.</summary>
public static class HindsightBankIds
{
    public const string BalanceTuning = "balance-tuning";

    public const string DevRepo = "dev-cmano-clone";

    public static string DevStory(string storySlug) => $"dev-story-{Slug(storySlug)}";

    public static string DevPullRequest(int prNumber) => $"dev-pr-{prNumber}";

    public static string AgentDecision(string personalitySlug, string agentId) =>
        $"agent-{Slug(personalitySlug)}-{Slug(agentId)}";

    public static string Aar(string scenarioSlug, string runId) =>
        $"aar-{Slug(scenarioSlug)}-{Slug(runId)}";

    public static string AgentExperience(string agentId) =>
        $"agent-xp-{Slug(agentId)}";

    internal static string Slug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        return value.Trim().ToLowerInvariant().Replace(' ', '-');
    }
}
