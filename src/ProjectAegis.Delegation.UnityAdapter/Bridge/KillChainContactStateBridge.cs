namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;

/// <summary>
/// DRG-179: headless C2 façade for kill-chain contact-state.
/// Read-only (ADR-010 §2–3). Hosts bind <see cref="BindPanel"/>; Unity rendering is DRG-180.
/// Never mutates sim authority. Does not touch <c>DelegationBridge</c>.
/// </summary>
public static class KillChainContactStateBridge
{
    /// <summary>
    /// Projects kill-chain contact state from a world snapshot + order log.
    /// Fire-control comes from <see cref="ISimWorldSnapshot.HasFireControlTrackOnPrimaryContact"/>,
    /// keyed to the primary hostile id — not from UI selection.
    /// </summary>
    public static KillChainContactSnapshot Build(ISimWorldSnapshot snapshot, DecisionLog log)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        var currentTick = snapshot.SimTime <= 0 ? 0UL : (ulong)snapshot.SimTime;
        return KillChainContactStateProjection.Project(
            log,
            currentTick,
            new SnapshotFireControl(snapshot));
    }

    /// <summary>Maps an already-projected kill-chain snapshot to C2 panel labels.</summary>
    public static KillChainContactPanelState BindPanel(KillChainContactSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return KillChainContactPanelBinder.Bind(snapshot);
    }

    private sealed class SnapshotFireControl : IKillChainFireControlSource
    {
        private readonly string? _primaryId;
        private readonly bool _hasTrack;

        public SnapshotFireControl(ISimWorldSnapshot snapshot)
        {
            _primaryId = snapshot.PrimaryHostileContactId?.Value;
            _hasTrack = snapshot.HasFireControlTrackOnPrimaryContact && !string.IsNullOrEmpty(_primaryId);
        }

        public bool HasFireControlTrack(string contactId, string targetId)
        {
            if (!_hasTrack || _primaryId is null)
            {
                return false;
            }

            return string.Equals(targetId, _primaryId, StringComparison.Ordinal)
                || string.Equals(contactId, _primaryId, StringComparison.Ordinal);
        }
    }
}
