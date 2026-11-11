namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

using System.Collections.ObjectModel;
using Core;
using Roe;
using SensorToShooter;
using Bridge;

/// <summary>
/// Composes native current runtime evidence for advice without reconstructing an engage context.
/// Weapon, range, policy and scarcity scores remain absent when the runtime has no exact inputs.
/// </summary>
public static class AdviceRuntimeEvidenceBridge
{
    /// <summary>Returns selected-contact facts tied to the exact Slice A frame and simulation clock.</summary>
    public static AdviceEvidenceSource? Build(
        DelegationBridge bridge,
        ISimWorldSnapshot snapshot,
        SliceAContactFrame contacts,
        string selectedContactId)
    {
        if (bridge is null) throw new ArgumentNullException(nameof(bridge));
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
        if (contacts is null) throw new ArgumentNullException(nameof(contacts));
        if (string.IsNullOrWhiteSpace(selectedContactId) || !double.IsFinite(snapshot.SimTime) || snapshot.SimTime < 0
            || contacts.SimTime is null || contacts.SimTime.Value != snapshot.SimTime)
            return null;

        var contact = contacts.Contacts.FirstOrDefault(c => string.Equals(c.ContactId, selectedContactId, StringComparison.Ordinal));
        if (contact is null) return null;
        var facts = new List<string>
        {
            $"contact:{contact.ContactId}",
            $"target:{contact.TargetId}",
            $"observer:{contact.ObserverId}",
            $"contact-last-report:{contact.LastSimTime:R}",
        };

        var provenance = contacts.Provenance.Contacts.FirstOrDefault(p => string.Equals(p.ContactId, selectedContactId, StringComparison.Ordinal));
        if (provenance is not null)
        {
            facts.Add($"provenance-confidence:{provenance.Confidence}");
            facts.Add($"provenance-freshness:{provenance.Freshness}:age-ticks={provenance.AgeTicks}");
            facts.Add($"reporting-link:{(provenance.OutOfCommsUnknown ? "unknown-out-of-comms" : "reported")}");
        }

        var chain = contacts.Chains.Chains.FirstOrDefault(c => string.Equals(c.ContactId, selectedContactId, StringComparison.Ordinal));
        if (chain is not null)
        {
            facts.Add($"sensor-to-shooter:{(chain.IsComplete ? "complete" : "broken")}:cause={chain.PrimaryCauseLabel}");
            foreach (var link in chain.Links)
                facts.Add($"chain-link:{link.Kind}:{(link.IsLinked ? "linked" : "broken")}:{link.UnitId ?? "unknown"}:{link.Detail ?? link.CauseLabel}");

            var shooter = chain.Links.FirstOrDefault(l => l.Kind == SensorToShooterLinkKind.EligibleShooter && l.IsLinked)?.UnitId;
            if (!string.IsNullOrEmpty(shooter) && bridge.Registry.TryGetBinding(new TargetId(shooter), out _))
            {
                facts.Add($"eligible-shooter:{shooter}");
                if (bridge.Session?.Magazines is { } magazines)
                {
                    var entityId = OrderActionMapper.TargetIdToUlong(new TargetId(shooter));
                    facts.Add(magazines.TryGetRounds(entityId, 0, out var rounds)
                        ? $"tracked-magazine:mount=0:rounds={rounds}"
                        : "tracked-magazine:mount=0:unknown");
                }
            }
        }

        if (contacts.Authorities.TryGetValue(selectedContactId, out var authority))
            facts.Add($"authority:{authority.Targeting.Disposition}:{authority.Targeting.ReasonCode ?? "none"}");
        else facts.Add("authority:unknown:no-implicit-permission");

        facts.Add("weapon-range-and-resource-scores:unknown-no-exact-runtime-inputs");
        return new AdviceEvidenceSource(selectedContactId, snapshot.SimTime, null, null,
            new ReadOnlyCollection<string>(facts), ModelAvailable: true);
    }
}
