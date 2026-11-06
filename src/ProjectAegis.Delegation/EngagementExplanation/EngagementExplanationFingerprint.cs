namespace ProjectAegis.Delegation.EngagementExplanation;

using System.Globalization;
using System.Text;

/// <summary>Replay-stable canonical fingerprint for engagement explanation surfaces (DRG-168).</summary>
public static class EngagementExplanationFingerprint
{
    /// <summary>Same inputs yield the same string. Invariant culture; fixed row order; no wall clock.</summary>
    public static string Compute(EngagementExplanationSurface? surface)
    {
        if (surface is null || ReferenceEquals(surface, EngagementExplanationSurface.Empty))
        {
            return "eex:empty";
        }

        var builder = new StringBuilder();
        builder.Append("eex:s=");
        builder.Append((int)surface.Status);
        builder.Append('|');
        builder.Append(surface.ShooterId);
        builder.Append(',');
        builder.Append(surface.TargetId);
        builder.Append(',');
        builder.Append(surface.CorrelationId);
        builder.Append(',');
        builder.Append(surface.SimTime.ToString("R", CultureInfo.InvariantCulture));
        builder.Append("|h=");
        builder.Append(surface.Headline);
        AppendConstraints(builder, "|hc=", surface.HardConstraints);
        AppendConstraints(builder, "|dc=", surface.DoctrineConstraints);
        builder.Append("|cc=");
        builder.Append(surface.ContactConfidence);
        builder.Append("|w=");
        builder.Append(surface.Weapon);
        builder.Append("|fs=");
        builder.Append(surface.FiringSolution);
        builder.Append("|a=");
        builder.Append(surface.ActionableReason ?? string.Empty);
        builder.Append("|x=");
        builder.Append(surface.ExplanationRef);
        return builder.ToString();
    }

    private static void AppendConstraints(
        StringBuilder builder,
        string label,
        IReadOnlyList<EngagementConstraint> constraints)
    {
        builder.Append(label);
        builder.Append(constraints.Count);
        for (var i = 0; i < constraints.Count; i++)
        {
            var row = constraints[i];
            builder.Append(';');
            builder.Append((int)row.Kind);
            builder.Append(',');
            builder.Append(row.Name);
            builder.Append(',');
            builder.Append((int)row.State);
            builder.Append(',');
            builder.Append(row.Code ?? string.Empty);
        }
    }
}
