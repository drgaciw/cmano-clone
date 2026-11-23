using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

public sealed record MessageCategoryPolicyRow(
    string Category,
    bool Log,
    bool Pause,
    bool MapBalloon,
    bool DropToRealTime,
    string ActionTokens);

/// <summary>
/// DRG-408: projects <see cref="MessageCategoryPolicy"/> into sorted rows for UI binding.
/// </summary>
public static class MessageCategoryPolicyBinder
{
    public static IReadOnlyList<MessageCategoryPolicyRow> Bind(MessageCategoryPolicy? policy)
    {
        if (policy is null)
        {
            return Array.Empty<MessageCategoryPolicyRow>();
        }

        var categories = policy.Categories;
        var rows = new MessageCategoryPolicyRow[categories.Count];
        for (var i = 0; i < categories.Count; i++)
        {
            var category = categories[i];
            var rule = policy.GetRule(category);
            rows[i] = new MessageCategoryPolicyRow(
                category,
                rule.Log,
                rule.Pause,
                rule.MapBalloon,
                rule.DropToRealTime,
                rule.FormatActionTokens());
        }

        return rows;
    }
}
