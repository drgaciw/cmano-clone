namespace ProjectAegis.Delegation.TargetabilityAccept;

using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.SensorToShooter;
using ProjectAegis.Delegation.Skills;

/// <summary>Slice A acceptance disposition for a contact track.</summary>
public enum TargetabilityAcceptDisposition
{
    Permitted = 0,
    Withheld = 1,
}

/// <summary>Stable named cause codes for withheld targetability. Never silent when withheld.</summary>
public static class TargetabilityAcceptCauseCodes
{
    public const string None = "None";
    public const string Stale = "Stale";
    public const string CatalogMiss = "CatalogMiss";
    public const string SilentComms = "SilentComms";
    public const string MissingProvenance = "MissingProvenance";
    public const string LostSensor = "LostSensor";
    public const string StaleTrack = "StaleTrack";
    public const string NoFireControl = "NoFireControl";
    public const string NoEligibleShooter = "NoEligibleShooter";
    public const string DegradedTrack = "DegradedTrack";
    public const string WeaponsTight = C2AuthorityProjector.ReasonWeaponsTight;
    public const string RoeHoldFire = C2AuthorityProjector.ReasonRoeHoldFire;
    public const string NoFireControlAuthority = C2AuthorityProjector.ReasonNoFireControl;
    public const string SharedTrackNoRelease = C2AuthorityProjector.ReasonSharedTrackNoRelease;
    public const string WeaponsReleaseRequired = C2AuthorityProjector.ReasonWeaponsReleaseRequired;
    public const string ApprovalRequired = C2AuthorityProjector.ReasonApprovalRequired;
}

/// <summary>Child projector fingerprints folded into the acceptance snapshot.</summary>
public sealed record TargetabilityAcceptChildFingerprints(
    string Provenance,
    string SensorToShooter,
    string Authority);

/// <summary>
/// DRG-219: one contact row composing provenance, sensor-to-shooter, and authority projections.
/// </summary>
public sealed record TargetabilityAcceptContactRow(
    string ContactId,
    string TargetId,
    TargetabilityAcceptDisposition Disposition,
    string WithheldCauseCode,
    ContactProvenanceState? Provenance,
    SensorToShooterChain? SensorToShooter,
    C2AuthorityProjection Authority);

/// <summary>Replay-stable acceptance snapshot for Combat UX Slice A headless exit gate.</summary>
public sealed record TargetabilityAcceptSnapshot(IReadOnlyList<TargetabilityAcceptContactRow> Contacts)
{
    public static TargetabilityAcceptSnapshot Empty { get; } =
        new(Array.Empty<TargetabilityAcceptContactRow>());
}
