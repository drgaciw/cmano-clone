namespace ProjectAegis.Delegation.UnityAdapter.PlayEntry;

using ProjectAegis.Delegation.Core;

public sealed record C2ModeOption(SimulationModeKind Kind, string Label, bool IsSelected);

/// <summary>
/// C2 top-bar simulation mode selector model (S123-04 DRG-246 MODE-01): Human / Mixed / AvA over
/// <see cref="SimulationModeKind"/>. Selection is submitted via <see cref="PlayEntrySession.TrySelectMode"/>;
/// enabled only while a loaded package is in Planning.
/// </summary>
public sealed record C2ModeSelectorPresentation(
    IReadOnlyList<C2ModeOption> Options,
    bool IsEnabled,
    string TopBarModeLabel)
{
    public const string UnsetLabel = "Unset";

    private static readonly SimulationModeKind[] OfferedModes =
    {
        SimulationModeKind.Human,
        SimulationModeKind.Mixed,
        SimulationModeKind.AgentVsAgent,
    };

    public static string LabelFor(SimulationModeKind mode) => mode switch
    {
        SimulationModeKind.Human => "Human",
        SimulationModeKind.Mixed => "Mixed",
        SimulationModeKind.AgentVsAgent => "AvA",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown simulation mode."),
    };

    public static C2ModeSelectorPresentation Project(PlayEntryState state)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var options = new C2ModeOption[OfferedModes.Length];
        for (var i = 0; i < OfferedModes.Length; i++)
        {
            var kind = OfferedModes[i];
            options[i] = new C2ModeOption(kind, LabelFor(kind), state.Mode == kind);
        }

        return new C2ModeSelectorPresentation(
            options,
            state.IsPlanning,
            state.Mode.HasValue ? LabelFor(state.Mode.Value) : UnsetLabel);
    }
}
