namespace ProjectAegis.Delegation.TargetabilityAccept;

using System.Text;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.SensorToShooter;
using ProjectAegis.Delegation.Skills;

/// <summary>Replay-stable canonical fingerprint for targetability acceptance snapshots (DRG-219).</summary>
public static class TargetabilityAcceptFingerprint
{
    /// <summary>
    /// Same inputs yield the same string. Invariant culture; ordinal ordering; no wall clock.
    /// </summary>
    public static string Compute(TargetabilityAcceptSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Contacts.Count == 0)
        {
            return "tac:empty";
        }

        var builder = new StringBuilder();
        builder.Append("tac:c=");
        builder.Append(snapshot.Contacts.Count);
        for (var i = 0; i < snapshot.Contacts.Count; i++)
        {
            AppendContact(builder, snapshot.Contacts[i]);
        }

        return builder.ToString();
    }

    private static void AppendContact(StringBuilder builder, TargetabilityAcceptContactRow row)
    {
        builder.Append('|');
        builder.Append(row.ContactId);
        builder.Append(',');
        builder.Append(row.TargetId);
        builder.Append(',');
        builder.Append((int)row.Disposition);
        builder.Append(',');
        builder.Append(row.WithheldCauseCode);
        builder.Append(',');
        builder.Append(ContactProvenanceFingerprint.Compute(
            row.Provenance is null
                ? ContactProvenanceSnapshot.Empty
                : new ContactProvenanceSnapshot(new[] { row.Provenance })));
        builder.Append(',');
        builder.Append(row.SensorToShooter is null
            ? "sts:empty"
            : SensorToShooterProjection.ComputeFingerprint(
                new SensorToShooterSnapshot(new[] { row.SensorToShooter })));
        builder.Append(',');
        builder.Append(FormatAuthority(row.Authority));
    }

    private static string FormatAuthority(C2AuthorityProjection authority)
    {
        var builder = new StringBuilder();
        builder.Append("auth:");
        builder.Append((int)authority.Roe.Roe);
        builder.Append(',');
        builder.Append((int)authority.Roe.TargetingDisposition);
        builder.Append(',');
        builder.Append(authority.Roe.TargetingReasonCode ?? string.Empty);
        builder.Append(',');
        builder.Append(authority.Roe.EngageAllowedByRoe ? '1' : '0');
        builder.Append(',');
        builder.Append((int)authority.Targeting.Disposition);
        builder.Append(',');
        builder.Append(authority.Targeting.ReasonCode ?? string.Empty);
        builder.Append(',');
        builder.Append(authority.Targeting.PendingApproval?.ToString() ?? string.Empty);
        var actions = authority.Actions
            .OrderBy(a => (int)a.Action)
            .ToArray();
        for (var i = 0; i < actions.Length; i++)
        {
            var action = actions[i];
            builder.Append(';');
            builder.Append((int)action.Action);
            builder.Append(',');
            builder.Append((int)action.Disposition);
            builder.Append(',');
            builder.Append(action.ReasonCode ?? string.Empty);
        }

        return builder.ToString();
    }
}
