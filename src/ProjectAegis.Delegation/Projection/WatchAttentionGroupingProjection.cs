namespace ProjectAegis.Delegation.Projection;

using Watch;

/// <summary>A presentation-only group retaining individual event ids for acknowledgement and camera focus.</summary>
public sealed record WatchAttentionGroup(
    string GroupId,
    WatchAttentionPriority Priority,
    ulong FirstTriggerTick,
    int Count,
    int UnresolvedCount,
    string FocusSubjectId,
    IReadOnlyList<string> SubjectIds,
    IReadOnlyList<string> EventIds,
    string Summary);

/// <summary>Groups related visible cards without changing their acknowledgement or dismissal state.</summary>
public static class WatchAttentionGroupingProjection
{
    /// <summary>Returns groups in priority, earliest tick, and ordinal id order. Null input is empty.</summary>
    public static IReadOnlyList<WatchAttentionGroup> Project(IReadOnlyList<WatchAttentionCard>? cards)
    {
        if (cards is null || cards.Count == 0)
        {
            return Array.Empty<WatchAttentionGroup>();
        }

        var grouped = new Dictionary<string, List<WatchAttentionCard>>(StringComparer.Ordinal);
        for (var i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (card.IsDismissed)
            {
                continue;
            }

            var key = string.IsNullOrWhiteSpace(card.GroupingKey)
                ? "event:" + card.EventId
                : "group:" + card.Kind + ":" + card.GroupingKey;
            if (!grouped.TryGetValue(key, out var members))
            {
                members = new List<WatchAttentionCard>();
                grouped.Add(key, members);
            }

            members.Add(card);
        }

        var result = new List<WatchAttentionGroup>(grouped.Count);
        foreach (var pair in grouped)
        {
            var members = pair.Value;
            members.Sort(CompareCards);
            var priority = members[0].Priority;
            var firstTick = members[0].TriggerTick;
            var unresolved = 0;
            WatchAttentionCard? focus = null;
            var subjects = new List<string>();
            var seenSubjects = new HashSet<string>(StringComparer.Ordinal);
            var eventIds = new string[members.Count];
            for (var i = 0; i < members.Count; i++)
            {
                var member = members[i];
                firstTick = Math.Min(firstTick, member.TriggerTick);
                if (member.IsUnresolved)
                {
                    unresolved++;
                    focus ??= member;
                }

                if (seenSubjects.Add(member.SubjectId))
                {
                    subjects.Add(member.SubjectId);
                }

                eventIds[i] = member.EventId;
            }

            focus ??= members[0];
            var kindLabel = focus.Kind == WatchAttentionKind.OwnSideLossOrDamage
                ? "Own-unit damage or loss"
                : "Contact update";
            result.Add(new WatchAttentionGroup(
                pair.Key, priority, firstTick, members.Count, unresolved, focus.SubjectId,
                subjects.ToArray(), eventIds,
                members.Count == 1
                    ? $"{kindLabel}: {focus.SubjectId}"
                    : $"{members.Count} related alerts ({unresolved} need attention)"));
        }

        result.Sort(static (a, b) =>
        {
            var priority = a.Priority.CompareTo(b.Priority);
            if (priority != 0)
            {
                return priority;
            }

            var tick = a.FirstTriggerTick.CompareTo(b.FirstTriggerTick);
            return tick != 0 ? tick : string.Compare(a.GroupId, b.GroupId, StringComparison.Ordinal);
        });
        return result;
    }

    private static int CompareCards(WatchAttentionCard a, WatchAttentionCard b)
    {
        var priority = a.Priority.CompareTo(b.Priority);
        if (priority != 0)
        {
            return priority;
        }

        var tick = a.TriggerTick.CompareTo(b.TriggerTick);
        return tick != 0 ? tick : string.Compare(a.EventId, b.EventId, StringComparison.Ordinal);
    }
}
