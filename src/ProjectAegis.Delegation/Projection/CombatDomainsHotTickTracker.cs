namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Tick-stamped combat-domain engagement tracker for ASSET-021 hosts.
/// Pure projection DTO — no sim hot-path, no Unity dependency.
/// Feeds <see cref="CombatDomainsHotTickPanelBinder.BindFromTracker"/>.
/// </summary>
public sealed class CombatDomainsHotTickTracker
{
    private readonly Dictionary<string, CombatDomainHudEngagement> _engagements =
        new(StringComparer.OrdinalIgnoreCase);

    public int LastSimTick { get; private set; }

    public CombatDomainsHotTickTracker()
    {
        ResetIdleMap();
    }

    /// <summary>Last observed engagement map (default domains always present).</summary>
    public IReadOnlyDictionary<string, CombatDomainHudEngagement> SnapshotEngagements()
    {
        var copy = new Dictionary<string, CombatDomainHudEngagement>(
            CombatDomainsHotTickPanelBinder.DefaultDomainKeys.Length,
            StringComparer.OrdinalIgnoreCase);
        foreach (var key in CombatDomainsHotTickPanelBinder.DefaultDomainKeys)
        {
            copy[key] = _engagements.TryGetValue(key, out var e) ? e : CombatDomainHudEngagement.Idle;
        }

        return copy;
    }

    public void Clear()
    {
        LastSimTick = 0;
        ResetIdleMap();
    }

    public void SetEngagement(string domainKey, CombatDomainHudEngagement engagement)
    {
        if (string.IsNullOrWhiteSpace(domainKey))
        {
            return;
        }

        Promote(domainKey.Trim(), engagement);
    }

    /// <summary>Promote domains listed as active tags to Engaged.</summary>
    public void ObserveActiveDomainTags(int simTick, IEnumerable<string> activeDomainTags)
    {
        if (activeDomainTags is null)
        {
            throw new ArgumentNullException(nameof(activeDomainTags));
        }

        LastSimTick = simTick;
        foreach (var tag in activeDomainTags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            Promote(tag.Trim(), CombatDomainHudEngagement.Engaged);
        }
    }

    /// <summary>
    /// Derive domain engagement from message-log categories (headless host path).
    /// Kill/hit → Surface; COMMS/weapon → Air; contact → Subsurface; policy → Land degraded;
    /// magazine → Facility engaged.
    /// </summary>
    public void ObserveMessageLog(int simTick, IReadOnlyList<MessageLogLine> lines)
    {
        if (lines is null)
        {
            throw new ArgumentNullException(nameof(lines));
        }

        LastSimTick = simTick;
        foreach (var line in lines)
        {
            ApplyCategory(line.Category);
        }
    }

    private void ApplyCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return;
        }

        switch (category.Trim().ToUpperInvariant())
        {
            case "KILL_CONFIRMED":
            case "HIT":
                Promote("Surface", CombatDomainHudEngagement.Engaged);
                break;
            case "COMMS":
            case "WEAPON_LAUNCH":
                Promote("Air", CombatDomainHudEngagement.Engaged);
                break;
            case "CONTACT":
            case "CONTACT_CHANGE":
                Promote("Subsurface", CombatDomainHudEngagement.Engaged);
                break;
            case "POLICY_DENIAL":
                Promote("Land", CombatDomainHudEngagement.Degraded);
                break;
            case "MAGAZINE":
                Promote("Facility", CombatDomainHudEngagement.Engaged);
                break;
            case "MISSION":
            case "MISSION_TRANSITION":
                Promote("Mine", CombatDomainHudEngagement.Engaged);
                break;
        }
    }

    /// <summary>
    /// Promotion rule: Engaged wins over Degraded wins over Idle (not enum ordinal).
    /// </summary>
    private void Promote(string domainKey, CombatDomainHudEngagement next)
    {
        if (!TryCanonicalDomainKey(domainKey, out var key))
        {
            return;
        }

        if (!_engagements.TryGetValue(key, out var current))
        {
            _engagements[key] = next;
            return;
        }

        if (Rank(next) > Rank(current))
        {
            _engagements[key] = next;
        }
    }

    private static int Rank(CombatDomainHudEngagement e) => e switch
    {
        CombatDomainHudEngagement.Engaged => 2,
        CombatDomainHudEngagement.Degraded => 1,
        _ => 0,
    };

    private static bool TryCanonicalDomainKey(string domainKey, out string key)
    {
        key = string.Empty;
        if (string.IsNullOrWhiteSpace(domainKey))
        {
            return false;
        }

        foreach (var candidate in CombatDomainsHotTickPanelBinder.DefaultDomainKeys)
        {
            if (string.Equals(candidate, domainKey, StringComparison.OrdinalIgnoreCase))
            {
                key = candidate;
                return true;
            }
        }

        return false;
    }

    private void ResetIdleMap()
    {
        _engagements.Clear();
        foreach (var key in CombatDomainsHotTickPanelBinder.DefaultDomainKeys)
        {
            _engagements[key] = CombatDomainHudEngagement.Idle;
        }
    }
}
