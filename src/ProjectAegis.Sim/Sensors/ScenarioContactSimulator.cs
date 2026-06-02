namespace ProjectAegis.Sim.Sensors;

using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

/// <summary>Deterministic scenario contact appearances (MVP: Unknown → Detected).</summary>
public sealed class ScenarioContactSimulator
{
    private readonly ScenarioContactSeed[] _seeds;
    private readonly IReadOnlyDictionary<string, EmconState>? _unitRadarEmcon;
    private readonly HashSet<string> _active = new(StringComparer.Ordinal);
    private string? _primaryTargetId;
    private bool _primaryHasTrack;

    public ScenarioContactSimulator(
        IReadOnlyList<ScenarioContactSeed> seeds,
        IReadOnlyDictionary<string, EmconState>? unitRadarEmcon = null)
    {
        _unitRadarEmcon = unitRadarEmcon;
        _seeds = seeds
            .OrderBy(s => s.ObserverId, StringComparer.Ordinal)
            .ThenBy(s => s.TargetId, StringComparer.Ordinal)
            .ThenBy(s => s.ContactId, StringComparer.Ordinal)
            .ToArray();
    }

    public int ActiveCount => _active.Count;

    public string? PrimaryTargetId => _primaryTargetId;

    public bool PrimaryHasFireControlTrack => _primaryHasTrack;

    public IReadOnlyList<ContactTransition> Tick(ulong simTick, double simTime)
    {
        if (_seeds.Length == 0)
        {
            return Array.Empty<ContactTransition>();
        }

        var transitions = new List<ContactTransition>();
        foreach (var seed in _seeds)
        {
            if (seed.AppearAtTick != simTick || _active.Contains(seed.ContactId))
            {
                continue;
            }

            if (seed.RequiresActiveRadar &&
                ScenarioEmconResolver.ResolveRadar(seed.ObserverId, _unitRadarEmcon) != EmconState.Active)
            {
                continue;
            }

            _active.Add(seed.ContactId);
            UpdatePrimary(seed);
            transitions.Add(new ContactTransition(
                simTick,
                simTime,
                seed.ObserverId,
                seed.ContactId,
                seed.TargetId,
                ContactLifecycleState.Unknown,
                ContactLifecycleState.Detected));
        }

        return transitions;
    }

    private void UpdatePrimary(ScenarioContactSeed seed)
    {
        if (_primaryTargetId == null ||
            string.Compare(seed.TargetId, _primaryTargetId, StringComparison.Ordinal) < 0)
        {
            _primaryTargetId = seed.TargetId;
            _primaryHasTrack = seed.HasFireControlTrack;
        }
        else if (seed.TargetId == _primaryTargetId)
        {
            _primaryHasTrack = seed.HasFireControlTrack;
        }
    }
}