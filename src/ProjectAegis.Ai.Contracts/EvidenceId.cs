namespace ProjectAegis.Ai.Contracts;

/// <summary>
/// Stable evidence identity: <c>{fingerprint-prefix}:{SequenceId}</c> derived from the order log.
/// </summary>
public static class EvidenceId
{
    /// <summary>Builds an evidence ID from an order-log fingerprint prefix and sequence id.</summary>
    public static string From(string fingerprintPrefix, ulong sequenceId)
    {
        if (string.IsNullOrWhiteSpace(fingerprintPrefix))
        {
            throw new ArgumentException("Fingerprint prefix is required.", nameof(fingerprintPrefix));
        }

        return fingerprintPrefix + ":" + sequenceId.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
