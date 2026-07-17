namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless binder for ASSET-021 Combat Domains Hot-Tick HUD rows.
/// Maps domain activity into display labels + USS state class suffixes (no Unity dependency).
/// </summary>
public static class CombatDomainsHotTickPanelBinder
{
    public static readonly string[] DefaultDomainKeys = { "Air", "Surface", "Subsurface", "Land", "Mine", "Facility" };

    /// <summary>Idle panel for all default combat domains.</summary>
    public static CombatDomainsHotTickPanelState BindIdle()
    {
        var rows = new List<CombatDomainDisplayRow>(DefaultDomainKeys.Length);
        foreach (var key in DefaultDomainKeys)
        {
            rows.Add(ToRow(key, CombatDomainHudEngagement.Idle));
        }

        return new CombatDomainsHotTickPanelState(rows);
    }

    /// <summary>
    /// Bind from a domain→engagement map (keys: Air, Surface, Subsurface, …).
    /// Unknown keys are ignored; missing default keys remain Idle.
    /// </summary>
    public static CombatDomainsHotTickPanelState BindFromDomainActivity(
        IReadOnlyDictionary<string, CombatDomainHudEngagement> domainActivity)
    {
        if (domainActivity is null)
        {
            throw new ArgumentNullException(nameof(domainActivity));
        }

        var rows = new List<CombatDomainDisplayRow>(DefaultDomainKeys.Length);
        foreach (var key in DefaultDomainKeys)
        {
            var engagement = CombatDomainHudEngagement.Idle;
            foreach (var kv in domainActivity)
            {
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    engagement = kv.Value;
                    break;
                }
            }

            rows.Add(ToRow(key, engagement));
        }

        return new CombatDomainsHotTickPanelState(rows);
    }

    /// <summary>
    /// Promote domains that appear in a set of active domain labels (e.g. from hot-tick telemetry tags) to Engaged.
    /// </summary>
    public static CombatDomainsHotTickPanelState BindFromActiveDomainTags(IEnumerable<string> activeDomainTags)
    {
        if (activeDomainTags is null)
        {
            throw new ArgumentNullException(nameof(activeDomainTags));
        }

        var map = new Dictionary<string, CombatDomainHudEngagement>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in activeDomainTags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            map[tag.Trim()] = CombatDomainHudEngagement.Engaged;
        }

        return BindFromDomainActivity(map);
    }

    private static CombatDomainDisplayRow ToRow(string domainKey, CombatDomainHudEngagement engagement)
    {
        var (label, css) = engagement switch
        {
            CombatDomainHudEngagement.Engaged => ("ENGAGED", "combat-domains-hot-tick__state--engaged"),
            CombatDomainHudEngagement.Degraded => ("DEGRADED", "combat-domains-hot-tick__state--degraded"),
            _ => ("IDLE", "combat-domains-hot-tick__state"),
        };

        return new CombatDomainDisplayRow(
            DomainKey: domainKey,
            DisplayName: domainKey.ToUpperInvariant(),
            StateLabel: label,
            StateCssClass: css);
    }
}

public enum CombatDomainHudEngagement
{
    Idle = 0,
    Engaged = 1,
    Degraded = 2,
}

public sealed record CombatDomainDisplayRow(
    string DomainKey,
    string DisplayName,
    string StateLabel,
    string StateCssClass);

public sealed record CombatDomainsHotTickPanelState(IReadOnlyList<CombatDomainDisplayRow> Rows);
