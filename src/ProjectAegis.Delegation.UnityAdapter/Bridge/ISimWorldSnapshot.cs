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

    /// <summary>Whether the primary hostile contact has been confirmed destroyed/killed.
    /// Used by patrol policies (e.g. PatrolCandidateEngagePolicy) to pre-filter Engage proposals for AAR remediation (S57-03).
    /// Additive; defaults to false.
    /// </summary>
    bool PrimaryHostileDestroyed => false;

    /// <summary>Primary blue-force contact for red-side engage victim selection (Baltic v3).</summary>
    TargetId? PrimaryBlueForceContactId => null;

    /// <summary>Whether the primary blue-force contact has been destroyed (red-side patrol policies).</summary>
    bool PrimaryBlueForceContactDestroyed => false;

    /// <summary>
    /// Optional shooter→preferred-hostile map for multi-domain concurrent engage.
    /// Default null preserves single-primary-hostile MVP behaviour.
    /// </summary>
    IReadOnlyDictionary<string, string>? PreferredHostileByShooter => null;
}
