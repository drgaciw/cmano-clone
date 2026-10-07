namespace ProjectAegis.Delegation.UnityAdapter.PlayEntry;

using ProjectAegis.Delegation.Core;

public sealed record PlaySideOption(PlaySide Side, string Label, bool IsSelected);

/// <summary>
/// Play-side picker model (S123-05 W2-MODE-01). Hidden until a mode is selected; offers
/// <see cref="PlayEntrySession.OfferedSides"/> for that mode. In AvA the pick is an observer perspective.
/// </summary>
public sealed record PlaySidePickerPresentation(
    bool IsVisible,
    bool IsEnabled,
    string Prompt,
    IReadOnlyList<PlaySideOption> Options)
{
    public const string CommandPrompt = "Command side";
    public const string ObservePrompt = "Observe side (agents command both sides)";

    public static PlaySidePickerPresentation Hidden { get; } =
        new(false, false, string.Empty, Array.Empty<PlaySideOption>());

    public static string LabelFor(PlaySide side) => side switch
    {
        PlaySide.Friendly => "Blue (friendly)",
        PlaySide.Opposing => "Red (opposing)",
        _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Unknown play side."),
    };

    public static PlaySidePickerPresentation Project(PlayEntryState state)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (!state.HasPackage || !state.Mode.HasValue)
        {
            return Hidden;
        }

        var offered = PlayEntrySession.OfferedSides(state.Mode.Value);
        var options = new PlaySideOption[offered.Count];
        for (var i = 0; i < offered.Count; i++)
        {
            options[i] = new PlaySideOption(offered[i], LabelFor(offered[i]), state.Side == offered[i]);
        }

        return new PlaySidePickerPresentation(
            true,
            state.IsPlanning,
            state.Mode.Value == SimulationModeKind.AgentVsAgent ? ObservePrompt : CommandPrompt,
            options);
    }
}
