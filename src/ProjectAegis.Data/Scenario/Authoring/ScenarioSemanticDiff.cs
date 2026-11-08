namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Pure semantic diff of two canonical scenario documents (AME-7.3 Partial+ / ME-W3 Track W3-c).
/// Summarizes mission, side, ORBAT unit, timeline, and event id-level changes only.
/// </summary>
public static class ScenarioSemanticDiff
{
    /// <summary>Returned when the two documents have no id-level semantic differences.</summary>
    public const string NoChanges = "no semantic changes";

    /// <summary>Delimiter between sorted bullets in <see cref="Summarize"/>.</summary>
    public const string Separator = "; ";

    /// <summary>
    /// Human-readable summary of mission/side/timeline/event/orbat unit id changes between two documents.
    /// Deterministic: bullets sorted with <see cref="StringComparer.Ordinal"/> and joined by "; ".
    /// </summary>
    /// <param name="before">Baseline document.</param>
    /// <param name="after">Comparison document.</param>
    /// <returns>
    /// Sorted bullets such as <c>mission +id (Type)</c>, <c>unit -id</c>, <c>timeline ~m tick 1→2</c>,
    /// or <see cref="NoChanges"/> when empty.
    /// </returns>
    public static string Summarize(ScenarioDocumentDto before, ScenarioDocumentDto after)
    {
        if (before is null)
        {
            throw new ArgumentNullException(nameof(before));
        }

        if (after is null)
        {
            throw new ArgumentNullException(nameof(after));
        }

        var bullets = new List<string>();
        AppendMissionDiffs(before, after, bullets);
        AppendUnitDiffs(before, after, bullets);
        AppendSideDiffs(before, after, bullets);
        AppendTimelineDiffs(before, after, bullets);
        AppendEventDiffs(before, after, bullets);

        if (bullets.Count == 0)
        {
            return NoChanges;
        }

        bullets.Sort(StringComparer.Ordinal);
        return string.Join(Separator, bullets);
    }

    private static void AppendMissionDiffs(
        ScenarioDocumentDto before,
        ScenarioDocumentDto after,
        List<string> bullets)
    {
        var beforeMap = ToLastById(
            before.Missions ?? Array.Empty<ScenarioMissionDto>(),
            m => m.Id);
        var afterMap = ToLastById(
            after.Missions ?? Array.Empty<ScenarioMissionDto>(),
            m => m.Id);

        foreach (var id in SortedKeys(beforeMap, afterMap))
        {
            var hasBefore = beforeMap.TryGetValue(id, out var b);
            var hasAfter = afterMap.TryGetValue(id, out var a);

            if (!hasBefore && hasAfter)
            {
                bullets.Add($"mission +{id} ({a!.Type})");
            }
            else if (hasBefore && !hasAfter)
            {
                bullets.Add($"mission -{id}");
            }
            else if (hasBefore && hasAfter &&
                     !string.Equals(b!.Type, a!.Type, StringComparison.Ordinal))
            {
                bullets.Add($"mission ~{id} type {b.Type}→{a.Type}");
            }
        }
    }

    private static void AppendUnitDiffs(
        ScenarioDocumentDto before,
        ScenarioDocumentDto after,
        List<string> bullets)
    {
        var beforeIds = ToIdSet(before.Orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>(), u => u.Id);
        var afterIds = ToIdSet(after.Orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>(), u => u.Id);
        AppendSetDiffs("unit", beforeIds, afterIds, bullets);
    }

    private static void AppendSideDiffs(
        ScenarioDocumentDto before,
        ScenarioDocumentDto after,
        List<string> bullets)
    {
        var beforeIds = ToIdSet(before.Sides ?? Array.Empty<ScenarioSideDto>(), s => s.Id);
        var afterIds = ToIdSet(after.Sides ?? Array.Empty<ScenarioSideDto>(), s => s.Id);
        AppendSetDiffs("side", beforeIds, afterIds, bullets);
    }

    private static void AppendEventDiffs(
        ScenarioDocumentDto before,
        ScenarioDocumentDto after,
        List<string> bullets)
    {
        var beforeIds = ToIdSet(before.Events ?? Array.Empty<ScenarioEventDto>(), e => e.Id);
        var afterIds = ToIdSet(after.Events ?? Array.Empty<ScenarioEventDto>(), e => e.Id);
        AppendSetDiffs("event", beforeIds, afterIds, bullets);
    }

    private static void AppendTimelineDiffs(
        ScenarioDocumentDto before,
        ScenarioDocumentDto after,
        List<string> bullets)
    {
        var beforeMap = ToLastById(
            before.OperationsTimeline ?? Array.Empty<ScenarioOperationTimelineEntryDto>(),
            t => t.MissionId);
        var afterMap = ToLastById(
            after.OperationsTimeline ?? Array.Empty<ScenarioOperationTimelineEntryDto>(),
            t => t.MissionId);

        foreach (var missionId in SortedKeys(beforeMap, afterMap))
        {
            var hasBefore = beforeMap.TryGetValue(missionId, out var b);
            var hasAfter = afterMap.TryGetValue(missionId, out var a);

            if (!hasBefore && hasAfter)
            {
                bullets.Add($"timeline +{missionId}");
            }
            else if (hasBefore && !hasAfter)
            {
                bullets.Add($"timeline -{missionId}");
            }
            else if (hasBefore && hasAfter && b!.ActivateAtTick != a!.ActivateAtTick)
            {
                bullets.Add($"timeline ~{missionId} tick {b.ActivateAtTick}→{a.ActivateAtTick}");
            }
        }
    }

    private static void AppendSetDiffs(
        string kind,
        HashSet<string> beforeIds,
        HashSet<string> afterIds,
        List<string> bullets)
    {
        var all = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var id in beforeIds)
        {
            all.Add(id);
        }

        foreach (var id in afterIds)
        {
            all.Add(id);
        }

        foreach (var id in all)
        {
            var inBefore = beforeIds.Contains(id);
            var inAfter = afterIds.Contains(id);
            if (!inBefore && inAfter)
            {
                bullets.Add($"{kind} +{id}");
            }
            else if (inBefore && !inAfter)
            {
                bullets.Add($"{kind} -{id}");
            }
        }
    }

    private static Dictionary<string, T> ToLastById<T>(
        IEnumerable<T> items,
        Func<T, string> idSelector)
    {
        var map = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            var id = idSelector(item) ?? string.Empty;
            map[id] = item;
        }

        return map;
    }

    private static HashSet<string> ToIdSet<T>(IEnumerable<T> items, Func<T, string> idSelector)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            set.Add(idSelector(item) ?? string.Empty);
        }

        return set;
    }

    private static IEnumerable<string> SortedKeys<T>(
        Dictionary<string, T> a,
        Dictionary<string, T> b)
    {
        var keys = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var k in a.Keys)
        {
            keys.Add(k);
        }

        foreach (var k in b.Keys)
        {
            keys.Add(k);
        }

        return keys;
    }
}
