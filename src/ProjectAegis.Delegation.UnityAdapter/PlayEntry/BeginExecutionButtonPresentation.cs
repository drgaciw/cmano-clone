namespace ProjectAegis.Delegation.UnityAdapter.PlayEntry;

/// <summary>
/// Begin Execution button model (S123-06 W3-MODE-01): disabled with a player-facing reason
/// until <see cref="BeginExecutionGate.CanBegin"/>.
/// </summary>
public sealed record BeginExecutionButtonPresentation(bool IsEnabled, string Label, string? BlockedReasonLabel)
{
    public const string BeginLabel = "BEGIN EXECUTION";

    public static string ReasonLabelFor(string reasonCode) => reasonCode switch
    {
        PlayEntryErrorCodes.NoPackage => "Load a scenario package",
        PlayEntryErrorCodes.NotPlanning => "Execution already started",
        PlayEntryErrorCodes.ModeRequired => "Select a simulation mode",
        PlayEntryErrorCodes.SideRequired => "Select a play side",
        _ => reasonCode,
    };

    public static BeginExecutionButtonPresentation Project(BeginExecutionGate gate)
    {
        if (gate == null)
        {
            throw new ArgumentNullException(nameof(gate));
        }

        if (gate.CanBegin)
        {
            return new BeginExecutionButtonPresentation(true, BeginLabel, null);
        }

        var labels = new string[gate.BlockedReasons.Count];
        for (var i = 0; i < labels.Length; i++)
        {
            labels[i] = ReasonLabelFor(gate.BlockedReasons[i]);
        }

        return new BeginExecutionButtonPresentation(false, BeginLabel, string.Join(" · ", labels));
    }
}
