namespace ProjectAegis.Delegation.C2Network;

using System.Text;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;

/// <summary>Deterministic fingerprint for <see cref="C2NetworkHealthSnapshot"/> (replay-stable projection).</summary>
public static class C2NetworkHealthFingerprint
{
    public static string Compute(C2NetworkHealthSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        var sb = new StringBuilder(512);
        sb.Append("C2NetworkHealth|");
        sb.Append(snapshot.NetworkHealth);
        sb.Append('|');
        sb.Append(snapshot.CommsState);
        sb.Append('|');
        sb.Append(snapshot.CommsNodeId);

        foreach (var link in snapshot.Links.OrderBy(l => l.FromUnitId, StringComparer.Ordinal)
                     .ThenBy(l => l.ToUnitId, StringComparer.Ordinal)
                     .ThenBy(l => l.LinkType, StringComparer.Ordinal))
        {
            sb.Append("|L|");
            sb.Append(link.FromUnitId);
            sb.Append('|');
            sb.Append(link.ToUnitId);
            sb.Append('|');
            sb.Append(link.LinkType);
            sb.Append('|');
            sb.Append(link.Health);
            sb.Append('|');
            sb.Append(link.StalenessTicks);
            sb.Append('|');
            sb.Append(link.IsLiveCapability ? '1' : '0');
            foreach (var unitId in link.AffectedContributorUnitIds.OrderBy(id => id, StringComparer.Ordinal))
            {
                sb.Append('|');
                sb.Append(unitId);
            }
        }

        foreach (var contributor in snapshot.LastKnownContributors
                     .OrderBy(c => c.UnitId, StringComparer.Ordinal)
                     .ThenBy(c => c.ContactId, StringComparer.Ordinal))
        {
            sb.Append("|C|");
            sb.Append(contributor.UnitId);
            sb.Append('|');
            sb.Append(contributor.ContactId);
            sb.Append('|');
            sb.Append(contributor.TargetId);
            sb.Append('|');
            sb.Append(contributor.LifecycleState);
            sb.Append('|');
            sb.Append(contributor.LastKnownSimTick);
            sb.Append('|');
            sb.Append(contributor.IsLiveCapability ? '1' : '0');
        }

        foreach (var lost in snapshot.LostPaths
                     .OrderBy(p => p.FromUnitId, StringComparer.Ordinal)
                     .ThenBy(p => p.ToUnitId, StringComparer.Ordinal)
                     .ThenBy(p => p.LinkType, StringComparer.Ordinal))
        {
            sb.Append("|P|");
            sb.Append(lost.FromUnitId);
            sb.Append('|');
            sb.Append(lost.ToUnitId);
            sb.Append('|');
            sb.Append(lost.LinkType);
            sb.Append('|');
            sb.Append(lost.LastKnownSimTick);
        }

        return sb.ToString();
    }
}
