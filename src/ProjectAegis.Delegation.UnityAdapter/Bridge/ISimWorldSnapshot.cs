namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Core;

/// <summary>
/// Per-tick world snapshot supplied by the sim/ECS layer (Unity systems implement this).
/// </summary>
public interface ISimWorldSnapshot
{
    double SimTime { get; }

    int ContactCount { get; }

    int ActiveEngagementCount { get; }

    /// <summary>
    /// Returns whether a registered member target is alive. If unknown, return false.
    /// </summary>
    bool IsMemberAlive(TargetId memberId);

    /// <summary>Primary hostile contact for engage/sensor MVP (null when <see cref="ContactCount"/> is 0).</summary>
    TargetId? PrimaryHostileContactId { get; }

    /// <summary>Fire-control quality track on <see cref="PrimaryHostileContactId"/>.</summary>
    bool HasFireControlTrackOnPrimaryContact { get; }

    /// <summary>Primary observer radar EMCON allows active illumination (Active = true).</summary>
    bool ObserverRadarEmconActive { get; }
}
