namespace ProjectAegis.Delegation.EngageExplainContract;

using System.Globalization;
using System.Text;

/// <summary>Replay-stable canonical fingerprint for engagement explanation rows (DRG-215).</summary>
public static class EngageExplainContractFingerprint
{
    /// <summary>
    /// Same inputs yield the same string. Invariant culture; ordinal ordering; no wall clock.
    /// </summary>
    public static string Compute(EngageExplainContractDto? contract)
    {
        if (contract is null || ReferenceEquals(contract, EngageExplainContractDto.Empty))
        {
            return "eec:empty";
        }

        var builder = new StringBuilder();
        builder.Append("eec:wp=");
        builder.Append(contract.WhyPermitted ?? string.Empty);
        builder.Append("|ww=");
        builder.Append(contract.WhyWithheld ?? string.Empty);
        builder.Append("|wf=");
        builder.Append(contract.WeaponFamilyId);
        builder.Append("|cid=");
        builder.Append(contract.CorrelationId);
        builder.Append("|st=");
        builder.Append(contract.SimTime.ToString("R", CultureInfo.InvariantCulture));
        return builder.ToString();
    }
}
