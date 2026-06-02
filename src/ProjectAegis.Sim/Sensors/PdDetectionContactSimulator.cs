namespace ProjectAegis.Sim.Sensors;

using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

/// <summary>Pd-driven contact appearances (replaces schedule seeds when detection trials present).</summary>
public sealed class PdDetectionContactSimulator
{
    private readonly SimSeed _seed;
    private readonly ScenarioDetectionTrial[] _trials;
    private readonly IReadOnlyDictionary<string, EmconState>? _unitRadarEmcon;
    private readonly HashSet<string> _detectedContacts = new(StringComparer.Ordinal);
    private string? _primaryTargetId;
    private bool _primaryHasTrack;

    public PdDetectionContactSimulator(
        SimSeed seed,
        IReadOnlyList<ScenarioDetectionTrial> trials,
        IReadOnlyDictionary<string, EmconState>? unitRadarEmcon = null)
    {
        _seed = seed;
        _unitRadarEmcon = unitRadarEmcon;
        _trials = trials.ToArray();
    }

    public int ActiveCount => _detectedContacts.Count;

    public string? PrimaryTargetId => _primaryTargetId;

    public bool PrimaryHasFireControlTrack => _primaryHasTrack;

    public IReadOnlyList<ContactTransition> Tick(ulong simTick, double simTime)
    {
        var rolls = DeterministicDetectionLoop.RollTick(
            _seed,
            simTick,
            _trials,
            _unitRadarEmcon,
            _detectedContacts);

        var transitions = new List<ContactTransition>();
        foreach (var roll in rolls)
        {
            if (!roll.Detected || _detectedContacts.Contains(roll.Trial.ContactId))
            {
                continue;
            }

            _detectedContacts.Add(roll.Trial.ContactId);
            UpdatePrimary(roll.Trial);
            transitions.Add(new ContactTransition(
                simTick,
                simTime,
                roll.Trial.ObserverId,
                roll.Trial.ContactId,
                roll.Trial.TargetId,
                ContactLifecycleState.Unknown,
                ContactLifecycleState.Detected));
        }

        return transitions;
    }

    private void UpdatePrimary(ScenarioDetectionTrial trial)
    {
        if (_primaryTargetId == null ||
            string.Compare(trial.TargetId, _primaryTargetId, StringComparison.Ordinal) < 0)
        {
            _primaryTargetId = trial.TargetId;
            _primaryHasTrack = true;
        }
    }
}